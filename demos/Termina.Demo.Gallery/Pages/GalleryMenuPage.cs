// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Components.Streaming;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Main menu page for the component gallery.
/// </summary>
public class GalleryMenuPage : ReactivePage<GalleryMenuViewModel>
{
    private SelectionListNode<GalleryMenuItem> _menuList = null!;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Page-level key binding for quit (capture phase)
        KeyBindings.Register(ConsoleKey.Q, () => ViewModel.Shutdown());

        _menuList.SelectionConfirmed
            .Subscribe(items =>
            {
                var item = items.FirstOrDefault();
                if (item != null)
                {
                    ViewModel.NavigateToGallery(item.Route);
                }
            })
            .DisposeWith(Subscriptions);

        Focus.PushFocus(_menuList);
    }

    public override ILayoutNode BuildLayout()
    {
        _menuList = new SelectionListNode<GalleryMenuItem>(
            ViewModel.MenuItems,
            item => new SelectionItemContent()
                .AddLine(new StaticTextSegment(item.Title, Color.BrightCyan, decoration: TextDecoration.Bold))
                .AddLine(new StaticTextSegment($"   {item.Description}", Color.Gray)))
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithVisibleRows(12);

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Termina Component Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Magenta)
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(
                                new TextNode("\n  Welcome to the Termina Component Gallery!\n  Explore different UI components and their capabilities.\n")
                                    .WithForeground(Color.White))
                            .WithChild(_menuList))
                    .Fill())
            .WithChild(
                new TextNode("[↑/↓] Navigate  [Enter] Select  [1-4] Quick Select  [Q] Quit")
                    .WithForeground(Color.BrightBlack)
                    .Height(1));
    }
}

/// <summary>
/// Represents a menu item in the gallery.
/// </summary>
public record GalleryMenuItem(string Title, string Description, string Route);
