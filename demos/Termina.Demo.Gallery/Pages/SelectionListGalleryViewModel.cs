// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using Termina.Reactive;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// ViewModel for the SelectionList gallery page.
/// </summary>
public partial class SelectionListGalleryViewModel : ReactiveViewModel
{
    [Reactive] private string _statusMessage = "Select items and explore different SelectionList features";

    public IReadOnlyList<ServerInfo> Servers { get; } = new List<ServerInfo>
    {
        new("prod-us-east-1", "US East", "Online", 45),
        new("prod-us-west-2", "US West", "Online", 72),
        new("prod-eu-west-1", "EU West", "Degraded", 89),
        new("prod-ap-south-1", "AP South", "Online", 23),
        new("staging-us-east-1", "US East", "Online", 12),
        new("dev-local", "Local", "Offline", 0)
    };

    private static readonly string[] ListNames = { "Single Select", "Multi-Select", "Numbered List" };

    public void OnFocusChanged(int listIndex)
    {
        StatusMessage = $"Focus: {ListNames[listIndex]} - Use Tab to switch lists";
    }

    public void OnSingleSelection(string item)
    {
        StatusMessage = $"Single selected: {item}";
    }

    public void OnOtherSelected(string customValue)
    {
        StatusMessage = $"Custom value entered: \"{customValue}\"";
    }

    public void OnMultiSelection(IReadOnlyList<string> items)
    {
        StatusMessage = $"Multi-selected {items.Count} servers: {string.Join(", ", items)}";
    }

    public void OnNumberedSelection(string item)
    {
        StatusMessage = $"Numbered selection: {item}";
    }
}
