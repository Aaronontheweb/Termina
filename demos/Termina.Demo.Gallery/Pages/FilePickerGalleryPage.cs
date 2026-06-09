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
/// Gallery page showcasing FilePickerNode capabilities.
/// Demonstrates two pickers side by side: single file selection and folder-only selection.
/// </summary>
public class FilePickerGalleryPage : ReactivePage<FilePickerGalleryViewModel>
{
    private FilePickerNode _filePicker = null!;
    private FilePickerNode _folderPicker = null!;
    private int _focusedPickerIndex;

    public override void OnNavigatedTo()
    {
        base.OnNavigatedTo();

        // Don't register Escape at the page level — FilePickerNode needs Escape
        // for clearing filters. Navigate back via the picker's Cancelled observable.
        KeyBindings.Register(ConsoleKey.Tab, CycleFocus);

        _filePicker.SelectionConfirmed
            .Subscribe(paths => ViewModel.OnFileSelected(paths))
            .DisposeWith(Subscriptions);

        _filePicker.Cancelled
            .Subscribe(_ => Navigate("/menu"))
            .DisposeWith(Subscriptions);

        _folderPicker.SelectionConfirmed
            .Subscribe(paths => ViewModel.OnFolderSelected(paths))
            .DisposeWith(Subscriptions);

        _folderPicker.Cancelled
            .Subscribe(_ => Navigate("/menu"))
            .DisposeWith(Subscriptions);

        _focusedPickerIndex = 0;
        Focus.PushFocus(_filePicker);
    }

    private void CycleFocus()
    {
        _focusedPickerIndex = (_focusedPickerIndex + 1) % 2;
        IFocusable target = _focusedPickerIndex == 0 ? _filePicker : _folderPicker;
        Focus.PushFocus(target);
        ViewModel.OnFocusChanged(_focusedPickerIndex);
    }

    public override ILayoutNode BuildLayout()
    {
        var startPath = Environment.CurrentDirectory;

        _filePicker = new FilePickerNode(startPath)
            .WithMode(FilePickerMode.Files)
            .WithSelectionMode(FilePickerSelectionMode.Single)
            .WithHighlightColors(Color.Black, Color.Green)
            .WithFillHeight();

        _folderPicker = new FilePickerNode(startPath)
            .WithMode(FilePickerMode.Directories)
            .WithSelectionMode(FilePickerSelectionMode.Single)
            .WithHighlightColors(Color.Black, Color.Yellow)
            .WithFillHeight();

        return Layouts.Vertical()
            .WithChild(BuildHeader())
            .WithChild(BuildPickerGrid().Fill())
            .WithChild(BuildStatusBar());
    }

    private static ILayoutNode BuildHeader()
    {
        return new PanelNode()
            .WithTitle("FilePickerNode Gallery")
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Color.Magenta)
            .WithContent(
                new TextNode("Browse the filesystem with keyboard navigation, fuzzy filtering (type to search), and directory traversal")
                    .WithForeground(Color.Gray))
            .Height(4);
    }

    private GridNode BuildPickerGrid()
    {
        var grid = new GridNode()
            .WithColumns(
                SizeConstraint.Percentage(50),
                SizeConstraint.Percentage(50))
            .WithRows(SizeConstraint.FillRemaining())
            .WithGridLines(BorderStyle.Single)
            .WithGridLineColor(Color.BrightBlack);

        grid.SetCell(0, 0,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Single File Select")
                        .WithForeground(Color.BrightGreen)
                        .Bold()
                        .Height(1))
                .WithChild(
                    new TextNode("Enter on file to select, Enter on folder to open")
                        .WithForeground(Color.DarkGray)
                        .Height(2))
                .WithChild(_filePicker));

        grid.SetCell(0, 1,
            Layouts.Vertical()
                .WithChild(
                    new TextNode("Folder Select")
                        .WithForeground(Color.BrightYellow)
                        .Bold()
                        .Height(1))
                .WithChild(
                    new TextNode("Space on folder to select, Enter to browse into")
                        .WithForeground(Color.DarkGray)
                        .Height(2))
                .WithChild(_folderPicker));

        return grid;
    }

    private ILayoutNode BuildStatusBar()
    {
        return Layouts.Horizontal()
            .WithChild(
                ViewModel.StatusMessage
                    .Select<string, ILayoutNode>(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Fill())
            .WithChild(
                new TextNode("[Tab] Switch Picker  [Esc] Menu")
                    .WithForeground(Color.BrightBlack)
                    .NoWrap()
                    .WidthAuto())
            .Height(1);
    }
}
