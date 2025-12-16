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
/// Demonstrates list components and state binding with modal dialogs.
/// </summary>
public class TodoListPage : ReactivePage<TodoListViewModel>
{
    public override ILayoutNode BuildLayout()
    {
        // Build the main content
        var mainContent = Layouts.Vertical()
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
                new TextNode("[↑/↓] Navigate [Space] Toggle [A] Add [D] Delete [C] Counter [Q] Quit")
                    .WithForeground(Color.BrightBlack)
                    .Height(1))
            .WithChild(
                ViewModel.StatusMessageChanged
                    .Select(msg => new TextNode(msg).WithForeground(Color.White))
                    .AsLayout()
                    .Height(1));

        // Build the layer with modal overlays
        // The stack layout renders children in order, with later children on top
        //
        // We use Layouts.Deferred() because modals are created in OnActivated (which runs
        // after BuildLayout) and we don't want the modal to be disposed when hidden.
        return Layouts.Stack()
            .WithChild(mainContent)
            .WithChild(
                // Show add modal when IsAddingItem is true and ShowPriorityModal is false
                ViewModel.IsAddingItemChanged
                    .CombineLatest(ViewModel.ShowPriorityModalChanged, (adding, showPriority) => adding && !showPriority)
                    .Select(showModal => showModal
                        ? Layouts.Deferred(() => ViewModel.AddModal)
                        : (ILayoutNode)Layouts.Empty())
                    .AsLayout())
            .WithChild(
                // Show priority selection modal when ShowPriorityModal is true
                ViewModel.ShowPriorityModalChanged
                    .Select(showPriority => showPriority
                        ? Layouts.Deferred(() => ViewModel.PriorityModal)
                        : (ILayoutNode)Layouts.Empty())
                    .AsLayout());
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
