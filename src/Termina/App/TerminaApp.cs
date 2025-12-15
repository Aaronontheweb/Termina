// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Termina.Focus;
using Termina.Input;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.App;

/// <summary>
/// A lightweight application runner for v2 Termina apps.
/// Manages the render loop, input handling, and region-based layout.
/// </summary>
public sealed class TerminaApp : IAsyncDisposable
{
    private readonly IAnsiTerminal _terminal;
    private readonly IInputSource _inputSource;
    private readonly RenderCoordinator _renderCoordinator;
    private readonly FocusManager _focusManager;
    private readonly Channel<object> _eventChannel;
    private readonly CancellationTokenSource _cts;
    private Task? _inputTask;
    private bool _running;

    /// <summary>
    /// Event raised when a key is pressed.
    /// Return true to indicate the key was handled.
    /// </summary>
    public event Func<ConsoleKeyInfo, bool>? OnKeyPressed;

    /// <summary>
    /// Event raised when the terminal is resized.
    /// </summary>
    public event Action<int, int>? OnResize;

    /// <summary>
    /// Event raised when a mouse event occurs.
    /// </summary>
    public event Action<MouseEvent>? OnMouse;

    /// <summary>
    /// Gets the render coordinator for registering regions and rendering content.
    /// </summary>
    public RenderCoordinator Renderer => _renderCoordinator;

    /// <summary>
    /// Gets the focus manager for managing focus between regions.
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
    /// Gets whether the application is currently running.
    /// </summary>
    public bool IsRunning => _running;

    /// <summary>
    /// Create a new TerminaApp with the specified terminal and input source.
    /// </summary>
    public TerminaApp(IAnsiTerminal terminal, IInputSource inputSource)
    {
        _terminal = terminal;
        _inputSource = inputSource;
        _renderCoordinator = new RenderCoordinator(terminal);
        _focusManager = new FocusManager();
        _eventChannel = Channel.CreateUnbounded<object>();
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Create a TerminaApp for headless/test use with a virtual terminal.
    /// </summary>
    public static TerminaApp CreateHeadless(int width = 80, int height = 24)
    {
        var terminal = new VirtualTerminal(width, height);
        var input = new VirtualInputSource();
        return new TerminaApp(terminal, input);
    }

    /// <summary>
    /// Create a TerminaApp for headless/test use with provided virtual terminal and input.
    /// </summary>
    public static TerminaApp CreateHeadless(VirtualTerminal terminal, VirtualInputSource input)
    {
        return new TerminaApp(terminal, input);
    }

    /// <summary>
    /// Create a TerminaApp for real console use.
    /// </summary>
    public static TerminaApp CreateConsole()
    {
        var terminal = new AnsiTerminal();
        var input = new ConsoleInputSource();
        return new TerminaApp(terminal, input);
    }

    /// <summary>
    /// Register a region with both the render coordinator and focus manager.
    /// </summary>
    public void RegisterRegion(Region region)
    {
        _renderCoordinator.RegisterRegion(region);
        if (region.Focusable)
            _focusManager.Register(region);
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

        // Initial layout and render
        _renderCoordinator.RecalculateLayout();
        _renderCoordinator.RefreshAll();

        // Process events until cancelled
        try
        {
            await foreach (var evt in _eventChannel.Reader.ReadAllAsync(token))
            {
                await ProcessEventAsync(evt);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            _running = false;
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

    private async Task ProcessEventAsync(object evt)
    {
        switch (evt)
        {
            case KeyPressed keyPressed:
                HandleKeyPressed(keyPressed.KeyInfo);
                break;

            case ResizeEvent resize:
                // For virtual terminal, we can resize; for real terminal, just recalculate
                if (_terminal is VirtualTerminal vt)
                    vt.Resize(resize.Width, resize.Height);
                _renderCoordinator.RecalculateLayout();
                _renderCoordinator.RefreshAll();
                OnResize?.Invoke(resize.Width, resize.Height);
                break;

            case MouseEvent mouse:
                OnMouse?.Invoke(mouse);
                break;
        }

        await Task.CompletedTask;
    }

    private void HandleKeyPressed(ConsoleKeyInfo key)
    {
        // Let app handle first
        if (OnKeyPressed?.Invoke(key) == true)
            return;

        // Default Tab/Shift+Tab handling for focus
        if (key.Key == ConsoleKey.Tab)
        {
            if (key.Modifiers.HasFlag(ConsoleModifiers.Shift))
                _focusManager.FocusPrevious();
            else
                _focusManager.FocusNext();
        }
    }

    /// <summary>
    /// Get the virtual terminal (for testing). Throws if not using a virtual terminal.
    /// </summary>
    public VirtualTerminal GetVirtualTerminal()
    {
        return _terminal as VirtualTerminal
            ?? throw new InvalidOperationException("Not using a virtual terminal");
    }

    /// <summary>
    /// Get the virtual input source (for testing). Throws if not using virtual input.
    /// </summary>
    public VirtualInputSource GetVirtualInput()
    {
        return _inputSource as VirtualInputSource
            ?? throw new InvalidOperationException("Not using a virtual input source");
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        if (_inputTask != null)
        {
            try
            {
                await _inputTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        await _renderCoordinator.DisposeAsync();
    }
}
