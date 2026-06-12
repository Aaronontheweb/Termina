// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Components.Streaming;
using Termina.Extensions;
using Termina.Layout;
using Termina.Notifications;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

public sealed class ToastGalleryPage : ReactivePage<ToastGalleryViewModel>
{
    private readonly IToastService _toastService;
    private SelectionListNode<ToastPreset> _presetList = null!;

    public ToastGalleryPage(IToastService toastService)
    {
        _toastService = toastService;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));

        _presetList.SelectionConfirmed
            .Subscribe(items =>
            {
                var item = items.FirstOrDefault();
                if (item != null)
                    ShowPreset(item);
            })
            .DisposeWith(Subscriptions);

        Focus.PushFocus(_presetList);
    }

    private void ShowPreset(ToastPreset preset)
    {
        _toastService.Show(preset.Message, new ToastOptions(
            Duration: TimeSpan.FromSeconds(3),
            Position: preset.Position,
            Color: preset.Color,
            Icon: preset.Icon));

        ViewModel.StatusMessage.Value = $"Shown: {preset.Name} toast at {preset.Position}";
    }

    public override ILayoutNode BuildLayout()
    {
        _presetList = new SelectionListNode<ToastPreset>(
            ViewModel.Presets,
            item =>
            {
                var colorLabel = item.Color.HasValue ? item.Color.Value.ToString() : "default";
                var iconLabel = item.Icon ?? "default";
                return new SelectionItemContent()
                    .AddLine(
                        new StaticTextSegment(item.Name, item.Color ?? Color.White, decoration: TextDecoration.Bold),
                        new StaticTextSegment($"  {item.Message}", Color.Gray))
                    .AddLine(new StaticTextSegment(
                        $"      color={colorLabel}  icon={iconLabel}  position={item.Position}",
                        Color.DarkGray));
            })
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Color.Black, Color.Cyan)
            .WithVisibleRows(8);

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Toast Notifications Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.BrightYellow)
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(
                                new TextNode("\n  Toast notifications with custom colors, icons, and positions.\n  Select a preset and press Enter to trigger it.\n")
                                    .WithForeground(Color.Gray))
                            .WithChild(_presetList))
                    .Fill())
            .WithChild(
                new TextNode("[Enter] Show Toast  [Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .NoWrap()
                    .Height(1))
            .WithChild(
                ViewModel.StatusMessage
                    .Select<string, ILayoutNode>(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Height(1));
    }
}
