// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Channels;
using Termina.Focus;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.App;

/// <summary>
/// A reactive application runner for v2 Termina apps.
/// Integrates reactive pages, ViewModels, and region-based rendering.
/// </summary>
public sealed class ReactiveTerminaApp : IAsyncDisposable
{
    private readonly IAnsiTerminal _terminal;
    private readonly IInputSource _inputSource;
    private readonly RenderCoordinator _renderCoordinator;
    private readonly FocusManager _focusManager;
    private readonly Channel<object> _eventChannel;
    private readonly Subject<IInputEvent> _inputSubject;
    private readonly CancellationTokenSource _cts;

    private IReactivePage? _currentPage;
    private ReactiveViewModel? _currentViewModel;
    private Task? _inputTask;
    private bool _running;

    /// <summary>
    /// Gets the render coordinator.
    /// </summary>
    public RenderCoordinator Renderer => _renderCoordinator;

    /// <summary>
    /// Gets the focus manager.
    /// </summary>
    public FocusManager Focus => _focusManager;

    /// <summary>
    /// Gets the terminal width.
    /// </summary>
    public int Width => _terminal.Width;

    /// <summary>
    /// Gets the terminal height.
    /// </summary>
    public int Height => _terminal.Height;

    /// <summary>
    /// Gets the input event stream that ViewModels subscribe to.
    /// </summary>
    public IObservable<IInputEvent> Input => _inputSubject.AsObservable();

    /// <summary>
    /// Gets whether the application is running.
    /// </summary>
    public bool IsRunning => _running;

    /// <summary>
    /// Create a new ReactiveTerminaApp.
    /// </summary>
    public ReactiveTerminaApp(IAnsiTerminal terminal, IInputSource inputSource)
    {
        _terminal = terminal;
        _inputSource = inputSource;
        _renderCoordinator = new RenderCoordinator(terminal);
        _focusManager = new FocusManager();
        _eventChannel = Channel.CreateUnbounded<object>();
        _inputSubject = new Subject<IInputEvent>();
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Create a ReactiveTerminaApp for headless/test use.
    /// </summary>
    public static ReactiveTerminaApp CreateHeadless(int width = 80, int height = 24)
    {
        var terminal = new VirtualTerminal(width, height);
        var input = new VirtualInputSource();
        return new ReactiveTerminaApp(terminal, input);
    }

    /// <summary>
    /// Create a ReactiveTerminaApp for headless/test use with provided components.
    /// </summary>
    public static ReactiveTerminaApp CreateHeadless(VirtualTerminal terminal, VirtualInputSource input)
    {
        return new ReactiveTerminaApp(terminal, input);
    }

    /// <summary>
    /// Create a ReactiveTerminaApp for real console use.
    /// </summary>
    public static ReactiveTerminaApp CreateConsole()
    {
        var terminal = new AnsiTerminal();
        var input = new ConsoleInputSource();
        return new ReactiveTerminaApp(terminal, input);
    }

    /// <summary>
    /// Set the active page and ViewModel.
    /// </summary>
    /// <typeparam name="TPage">The page type.</typeparam>
    /// <typeparam name="TViewModel">The ViewModel type.</typeparam>
    /// <param name="page">The page instance.</param>
    /// <param name="viewModel">The ViewModel instance.</param>
    public void SetPage<TPage, TViewModel>(TPage page, TViewModel viewModel)
        where TPage : ReactivePageBase<TViewModel>
        where TViewModel : ReactiveViewModel
    {
        // Deactivate current page if any
        if (_currentPage != null)
        {
            _currentPage.OnNavigatingFrom();
            _currentViewModel?.OnDeactivating();
        }

        // Clear existing regions
        foreach (var region in _renderCoordinator.Regions.ToList())
        {
            _renderCoordinator.UnregisterRegion(region.Id);
        }
        _focusManager.Clear();

        // Bind ViewModel
        ((IBindableReactivePage)page).BindViewModel(viewModel);

        // Wire up ViewModel
        viewModel.WireUp(
            navigate: path => { /* TODO: routing integration */ },
            navigateWithParams: (path, parms) => { /* TODO: routing integration */ },
            shutdown: Stop,
            requestRedraw: () => _renderCoordinator.RefreshAll(),
            input: Input
        );

        // Register regions from page
        foreach (var region in page.GetRegions())
        {
            _renderCoordinator.RegisterRegion(region);
            if (region.Focusable)
                _focusManager.Register(region);
        }

        // Calculate layout
        _renderCoordinator.RecalculateLayout();

        // Wire up observables to regions
        page.WireRegions(_renderCoordinator);

        // Activate
        _currentPage = page;
        _currentViewModel = viewModel;
        viewModel.OnActivated();
        page.OnNavigatedTo();

        // Initial render
        _renderCoordinator.RefreshAll();
    }

    /// <summary>
    /// Start the application loop.
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        if (_running)
            throw new InvalidOperationException("App is already running");

        _running = true;

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        var token = linkedCts.Token;

        // Initialize terminal
        _terminal.ClearScreen();
        _terminal.SetCursorVisible(false);

        // Start input source
        _inputTask = _inputSource.RunAsync(_eventChannel.Writer, token);

        // Process events
        try
        {
            await foreach (var evt in _eventChannel.Reader.ReadAllAsync(token))
            {
                ProcessEvent(evt);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            _running = false;
            _inputSubject.OnCompleted();
            _terminal.SetCursorVisible(true);
            _terminal.ResetColors();
        }
    }

    /// <summary>
    /// Stop the application.
    /// </summary>
    public void Stop()
    {
        _cts.Cancel();
    }

    private void ProcessEvent(object evt)
    {
        switch (evt)
        {
            case KeyPressed keyPressed:
                // Handle Tab/Shift+Tab for focus
                if (keyPressed.KeyInfo.Key == ConsoleKey.Tab)
                {
                    if (keyPressed.KeyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                        _focusManager.FocusPrevious();
                    else
                        _focusManager.FocusNext();
                }
                // Forward to input stream for ViewModel
                _inputSubject.OnNext(keyPressed);
                break;

            case ResizeEvent resize:
                if (_terminal is VirtualTerminal vt)
                    vt.Resize(resize.Width, resize.Height);
                _renderCoordinator.RecalculateLayout();
                _renderCoordinator.RefreshAll();
                _inputSubject.OnNext(resize);
                break;

            case MouseEvent mouse:
                _inputSubject.OnNext(mouse);
                break;
        }
    }

    /// <summary>
    /// Get the virtual terminal (for testing).
    /// </summary>
    public VirtualTerminal GetVirtualTerminal()
    {
        return _terminal as VirtualTerminal
            ?? throw new InvalidOperationException("Not using a virtual terminal");
    }

    /// <summary>
    /// Get the virtual input source (for testing).
    /// </summary>
    public VirtualInputSource GetVirtualInput()
    {
        return _inputSource as VirtualInputSource
            ?? throw new InvalidOperationException("Not using a virtual input source");
    }

    public async ValueTask DisposeAsync()
    {
        Stop();

        _currentPage?.OnNavigatingFrom();
        _currentViewModel?.Dispose();

        if (_inputTask != null)
        {
            try { await _inputTask; }
            catch (OperationCanceledException) { }
        }

        _inputSubject.Dispose();
        await _renderCoordinator.DisposeAsync();
    }
}
