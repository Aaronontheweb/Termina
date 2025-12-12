using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// ViewModel for a todo list demo.
/// Demonstrates list selection and state management.
/// </summary>
public partial class TodoListViewModel : ReactiveViewModel
{
    [Reactive] private IReadOnlyList<TodoItem> _items = new List<TodoItem>
    {
        new("Learn Termina reactive patterns", false),
        new("Build a TUI app", false),
        new("Deploy to production", false),
        new("Celebrate success", false)
    };

    [Reactive] private int _selectedIndex;
    [Reactive] private string _statusMessage = "Navigate with ↑/↓, Space to toggle, C for counter, Q to quit";

    public override void OnActivated()
    {
        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);
    }

    private void HandleKeyPress(KeyPressed key)
    {
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                if (SelectedIndex > 0)
                {
                    SelectedIndex--;
                    StatusMessage = $"Selected: {Items[SelectedIndex].Description}";
                }
                break;

            case ConsoleKey.DownArrow:
                if (SelectedIndex < Items.Count - 1)
                {
                    SelectedIndex++;
                    StatusMessage = $"Selected: {Items[SelectedIndex].Description}";
                }
                break;

            case ConsoleKey.Spacebar:
                ToggleSelected();
                break;

            case ConsoleKey.A:
                AddItem();
                break;

            case ConsoleKey.D:
                DeleteSelected();
                break;

            case ConsoleKey.C:
                Navigate("/counter");
                break;

            case ConsoleKey.Q:
                Shutdown();
                break;
        }
    }

    private void ToggleSelected()
    {
        if (Items.Count == 0) return;

        var item = Items[SelectedIndex];
        var newItem = item with { IsCompleted = !item.IsCompleted };

        // Create new list with updated item
        var newItems = Items.ToList();
        newItems[SelectedIndex] = newItem;
        Items = newItems;

        StatusMessage = newItem.IsCompleted
            ? $"Completed: {newItem.Description}"
            : $"Uncompleted: {newItem.Description}";
    }

    private void AddItem()
    {
        var newItems = Items.ToList();
        newItems.Add(new TodoItem($"New task {Items.Count + 1}", false));
        Items = newItems;
        StatusMessage = "Added new item";
    }

    private void DeleteSelected()
    {
        if (Items.Count == 0) return;

        var deleted = Items[SelectedIndex];
        var newItems = Items.ToList();
        newItems.RemoveAt(SelectedIndex);
        Items = newItems;

        // Adjust selected index if needed
        if (SelectedIndex >= Items.Count && Items.Count > 0)
        {
            SelectedIndex = Items.Count - 1;
        }

        StatusMessage = $"Deleted: {deleted.Description}";
    }
}

/// <summary>
/// A simple todo item.
/// </summary>
public record TodoItem(string Description, bool IsCompleted);
