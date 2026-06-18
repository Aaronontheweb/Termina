// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Clipboard;
using Termina.Extensions;
using Termina.Layout;
using Termina.Notifications;
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
    private readonly IToastService _toastService;
    private CopyableTextNode _oauthUrl = null!;
    private CopyableTextNode _accessToken = null!;
    private TextInputNode _pasteInput = null!;

    public ClipboardGalleryPage(IClipboardService clipboardService, IToastService toastService)
    {
        _clipboardService = clipboardService;
        _toastService = toastService;
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
                "https://auth.openai.com/oauth/authorize?client_id=demo-client&redirect_uri=http://localhost:5199/callback&scope=api.model.read%20model.request",
                _toastService)
            .WithForeground(Color.White)
            .WithFeedbackMode(CopyFeedbackMode.Toast)
            .WithToastPosition(ToastPosition.TopRight)
            .WithHint("Use Shift+Arrows to select, Enter or Ctrl+C to copy");

        _accessToken = new CopyableTextNode(
                _clipboardService,
                "sk-demo-4a0f8ea7f0b34d1e9a6cde8f4ab0f1c2",
                _toastService)
            .WithForeground(Color.White)
            .WithFeedbackMode(CopyFeedbackMode.InlineIndicator)
            .WithInlineIndicator("✓ Copied", Color.BrightGreen)
            .WithCopyBindings(new CopyKeyBinding(ConsoleKey.Enter), new CopyKeyBinding(ConsoleKey.C, ConsoleModifiers.Control))
            .WithHint("Use Ctrl+A to select all, Enter or Ctrl+C to copy");

        _pasteInput = new TextInputNode(frameProvider: RenderFrameProvider)
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
                            .WithChild(new TextNode("    Toast feedback at top-right").WithForeground(Color.BrightBlack).Height(1))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(new TextNode("  Access Token").WithForeground(Color.BrightCyan).Height(1))
                            .WithChild(Layouts.Horizontal().WithChild(new TextNode("  ").Width(2)).WithChild(_accessToken))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(new TextNode("    Inline green check after copy").WithForeground(Color.BrightBlack).Height(1))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(new TextNode("  Paste Validation Input").WithForeground(Color.BrightCyan).Height(1))
                            .WithChild(Layouts.Horizontal().WithChild(new TextNode("  ").Width(2)).WithChild(_pasteInput).Height(1))
                            .WithChild(new EmptyNode().Height(1))
                            .WithChild(new TextNode($"  Trace Log: {ViewModel.TraceFilePath}").WithForeground(Color.BrightBlack)))
                    .Fill())
            .WithChild(
                new TextNode("[Tab/Shift+Tab] Focus  [Shift+Arrows] Select  [Ctrl+A] All  [Enter/Ctrl+C] Copy  [Ctrl+Shift+V] Paste  [Esc] Menu")
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
