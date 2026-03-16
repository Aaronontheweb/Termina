// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Clipboard;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Gallery page for clipboard copy and paste behavior.
/// </summary>
public sealed class ClipboardGalleryPage : ReactivePage<ClipboardGalleryViewModel>
{
    private readonly IClipboardService _clipboardService;
    private CopyableTextNode _oauthUrl = null!;
    private CopyableTextNode _accessToken = null!;
    private TextInputNode _pasteInput = null!;

    public ClipboardGalleryPage(IClipboardService clipboardService)
    {
        _clipboardService = clipboardService;
    }

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));
        KeyBindings.Register(ConsoleKey.Tab, CycleFocusForward);
        KeyBindings.Register(ConsoleKey.Tab, ConsoleModifiers.Shift, CycleFocusBackward);

        _pasteInput.TextChanged
            .Subscribe(text => ViewModel.SetStatus($"Input length: {text.Length}"))
            .DisposeWith(Subscriptions);

        Focus.SetFocus(_oauthUrl);
    }

    protected override void OnBound()
    {
        _oauthUrl = new CopyableTextNode(
                _clipboardService,
                "https://auth.openai.com/oauth/authorize?client_id=demo-client&redirect_uri=http://localhost:5199/callback&scope=api.model.read%20model.request")
            .WithForeground(Color.White)
            .WithHint("Press Enter to copy this URL");

        _accessToken = new CopyableTextNode(
                _clipboardService,
                "sk-demo-4a0f8ea7f0b34d1e9a6cde8f4ab0f1c2")
            .WithForeground(Color.White)
            .WithHint("Press Enter to copy this token");

        _pasteInput = new TextInputNode()
            .WithPlaceholder("Paste text here with Ctrl+Shift+V or your terminal paste shortcut...");
    }

    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Clipboard Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Green)
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(
                                new TextNode("\n  Validate terminal-native clipboard copy and paste behavior.\n")
                                    .WithForeground(Color.Gray))
                            .WithChild(new TextNode("  OAuth URL").WithForeground(Color.BrightCyan).Height(1))
                            .WithChild(Layouts.Horizontal().WithChild(new TextNode("  ").Width(2)).WithChild(_oauthUrl))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(new TextNode("  Access Token").WithForeground(Color.BrightCyan).Height(1))
                            .WithChild(Layouts.Horizontal().WithChild(new TextNode("  ").Width(2)).WithChild(_accessToken))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(new TextNode("  Paste Validation Input").WithForeground(Color.BrightCyan).Height(1))
                            .WithChild(Layouts.Horizontal().WithChild(new TextNode("  ").Width(2)).WithChild(_pasteInput).Height(1)))
                    .Fill())
            .WithChild(
                new TextNode("[Tab/Shift+Tab] Switch focus  [Enter] Copy focused value  [Ctrl+Shift+V] Paste  [Esc] Menu")
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
