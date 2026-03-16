// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Input;
using Termina.Layout;
using Termina.Notifications;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Input;

public class PasteToastRoutingTests
{
    [Fact]
    public void ProcessEvent_ShowsToast_WhenFocusedPasteReceiverHandlesPaste()
    {
        var toastService = new TestToastService();
        var services = new TestServiceProvider(toastService);
        var app = new TerminaApplication(new VirtualTerminal(), services);
        app.RegisterRoute<TestPastePage, TestPasteViewModel>("/paste");
        app.NavigateTo("/paste");

        InvokeProcessEvent(app, new PasteEvent("hello"));

        Assert.Equal("Pasted 5 characters", toastService.LastMessage);
    }

    private static void InvokeProcessEvent(TerminaApplication app, object evt)
    {
        var method = typeof(TerminaApplication).GetMethod(
            "ProcessEvent",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        Assert.NotNull(method);
        method!.Invoke(app, [evt]);
    }

    private sealed class TestToastService : IToastService
    {
        private readonly Subject<ToastMessage?> _current = new();

        public string? LastMessage { get; private set; }

        public Observable<ToastMessage?> CurrentToast => _current;

        public void Show(string message, ToastOptions? options = null)
        {
            LastMessage = message;
            _current.OnNext(new ToastMessage(message, options?.Position ?? ToastPosition.BottomRight));
        }
    }

    private sealed class TestServiceProvider : IServiceProvider
    {
        private readonly IToastService _toastService;

        public TestServiceProvider(IToastService toastService)
        {
            _toastService = toastService;
        }

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(IToastService))
                return _toastService;

            if (serviceType == typeof(IEnumerable<IInputSource>))
                return Array.Empty<IInputSource>();

            return null;
        }
    }

    private sealed class TestPastePage : ReactivePage<TestPasteViewModel>
    {
        private readonly TextInputNode _input = new();

        public override void OnNavigatedTo()
        {
            base.OnNavigatedTo();
            Focus.PushFocus(_input);
        }

        public override ILayoutNode BuildLayout() => _input;
    }

    private sealed class TestPasteViewModel : ReactiveViewModel
    {
    }
}
