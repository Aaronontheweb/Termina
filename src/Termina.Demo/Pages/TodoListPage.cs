// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Extensions;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Demo.Pages;

/// <summary>
/// Page for the todo list demo.
/// Demonstrates list components and state binding with the new declarative layout API.
/// </summary>
public class TodoListPage : ReactivePage<TodoListViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        return Layouts.Vertical()
            .WithChild(
                new PanelNode()
                    .WithTitle("Todo List Demo")
                    .WithBorder(BorderStyle.Double)
                    .WithBorderColor(Color.Cyan)
                    .WithContent(
                        ViewModel.ItemsChanged
                            .CombineLatest(ViewModel.SelectedIndexChanged, (items, selectedIdx) => (items, selectedIdx))
                            .Select(tuple => BuildTodoList(tuple.items, tuple.selectedIdx))
                            .AsLayout())
                    .Fill())
            .WithChild(
                // Show text input row when adding, otherwise show help text
                ViewModel.IsAddingItemChanged
                    .CombineLatest(ViewModel.NewItemTextChanged, (isAdding, text) => (isAdding, text))
                    .Select(tuple => tuple.isAdding
                        ? BuildTextInputRow(tuple.text)
                        : new TextNode("[↑/↓] Navigate [Space] Toggle [A] Add [D] Delete [C] Counter [Q] Quit")
                            .WithForeground(Color.BrightBlack))
                    .AsLayout()
                    .Height(1))
            .WithChild(
                ViewModel.StatusMessageChanged
                    .Select(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Height(1));
    }

    private static ILayoutNode BuildTextInputRow(string text)
    {
        return Layouts.Horizontal()
            .WithChild(
                new TextNode("New task: ")
                    .WithForeground(Color.Yellow)
                    .Width(11))
            .WithChild(
                new TextNode(text + "▌")
                    .WithForeground(Color.White)
                    .Fill());
    }

    private static ILayoutNode BuildTodoList(IReadOnlyList<TodoItem> items, int selectedIndex)
    {
        if (items.Count == 0)
        {
            return new TextNode("\n  No items - press [A] to add\n")
                .WithForeground(Color.DarkGray)
                .Italic();
        }

        var completedCount = items.Count(i => i.IsCompleted);
        var statsText = $"{completedCount}/{items.Count} completed";

        var container = Layouts.Vertical()
            .WithChild(
                new TextNode($"\n  {statsText}\n")
                    .WithForeground(Color.Gray));

        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var isSelected = i == selectedIndex;
            var checkbox = item.IsCompleted ? "[✓]" : "[ ]";
            var text = $"  {checkbox} {item.Description}";

            var textNode = new TextNode(text);

            if (isSelected)
            {
                textNode = textNode.WithForeground(Color.Black).WithBackground(Color.Green);
            }
            else if (item.IsCompleted)
            {
                textNode = textNode.WithForeground(Color.DarkGray);
            }
            else
            {
                textNode = textNode.WithForeground(Color.White);
            }

            container = container.WithChild(textNode.Height(1));
        }

        return container;
    }
}
