// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using R3;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Gallery.Pages;

/// <summary>
/// Gallery page showcasing TextInputNode and TextAreaNode capabilities.
/// </summary>
public class TextInputGalleryPage : ReactivePage<TextInputGalleryViewModel>
{
    private TextInputNode _basicInput = null!;
    private TextInputNode _placeholderInput = null!;
    private TextAreaNode _textArea = null!;

    private int _focusedInputIndex;
    private const int InputCount = 3;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Page-level key bindings (capture phase)
        KeyBindings.Register(ConsoleKey.Escape, () => Navigate("/menu"));
        KeyBindings.Register(ConsoleKey.Tab, CycleFocus);

        _basicInput.Submitted
            .Subscribe(text => ViewModel.OnBasicInputSubmitted(text))
            .DisposeWith(Subscriptions);

        _placeholderInput.Submitted
            .Subscribe(text => ViewModel.OnPlaceholderInputSubmitted(text))
            .DisposeWith(Subscriptions);

        _textArea.Submitted
            .Subscribe(text => ViewModel.OnTextAreaSubmitted(text))
            .DisposeWith(Subscriptions);

        _focusedInputIndex = 0;
        Focus.PushFocus(_basicInput);
    }

    private void CycleFocus()
    {
        _focusedInputIndex = (_focusedInputIndex + 1) % InputCount;
        IFocusable target = _focusedInputIndex switch
        {
            0 => _basicInput,
            1 => _placeholderInput,
            2 => _textArea,
            _ => _basicInput
        };
        Focus.SetFocus(target);
    }

    public override ILayoutNode BuildLayout()
    {
        _basicInput = new TextInputNode();

        _placeholderInput = new TextInputNode()
            .WithPlaceholder("Type something here...");

        _textArea = new TextAreaNode()
            .WithPlaceholder("Enter multi-line text...")
            .WithMaxHeight(6);

        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Text Input Gallery")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Blue)
                    .WithContent(
                        Layouts.Vertical()
                            .WithChild(
                                new TextNode("\n  Text input components for user data entry.\n")
                                    .WithForeground(Color.Gray))
                            .WithChild(
                                new TextNode("  Basic Input (single-line):")
                                    .WithForeground(Color.BrightCyan)
                                    .Height(1))
                            .WithChild(
                                Layouts.Horizontal()
                                    .WithChild(new TextNode("  ").Width(2))
                                    .WithChild(_basicInput.Fill())
                                    .Height(1))
                            .WithChild(new TextNode("").Height(1))
                            .WithChild(
                                new TextNode("  Input with Placeholder (single-line):")
                                    .WithForeground(Color.BrightCyan)
                                    .Height(1))
                            .WithChild(
                                Layouts.Horizontal()
                                    .WithChild(new TextNode("  ").Width(2))
                                    .WithChild(_placeholderInput.Fill())
                                    .Height(1))
                            .WithChild(new TextNode("").Height(1))
                            .WithChild(
                                new TextNode("  TextArea (multi-line, Ctrl+Enter = newline, Enter = submit):")
                                    .WithForeground(Color.BrightCyan)
                                    .Height(1))
                            .WithChild(
                                Layouts.Horizontal()
                                    .WithChild(new TextNode("  ").Width(2))
                                    .WithChild(_textArea)))
                    .Fill())
            .WithChild(
                new TextNode("[Tab] Switch field  [Enter] Submit  [Ctrl+Enter] New line (TextArea)  [Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .Height(1))
            .WithChild(
                ViewModel.StatusMessage
                    .Select<string, ILayoutNode>(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Height(1));
    }
}
