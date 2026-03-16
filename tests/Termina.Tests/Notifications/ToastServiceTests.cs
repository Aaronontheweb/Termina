// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Time.Testing;
using R3;
using Termina.Notifications;

namespace Termina.Tests.Notifications;

public class ToastServiceTests
{
    [Fact]
    public void Show_SetsCurrentToast()
    {
        var timeProvider = new FakeTimeProvider();
        using var service = new ToastService(timeProvider);
        ToastMessage? current = null;
        using var subscription = service.CurrentToast.Subscribe(toast => current = toast);

        service.Show("Copied to clipboard");

        Assert.Equal("Copied to clipboard", current?.Message);
    }

    [Fact]
    public void Show_ClearsToastAfterDuration()
    {
        var timeProvider = new FakeTimeProvider();
        using var service = new ToastService(timeProvider);
        ToastMessage? current = null;
        using var subscription = service.CurrentToast.Subscribe(toast => current = toast);

        service.Show("Copied to clipboard", TimeSpan.FromSeconds(2));
        timeProvider.Advance(TimeSpan.FromSeconds(2));

        Assert.Null(current);
    }
}
