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
    [Reactive] private bool _isAddingItem;
    [Reactive] private string _newItemText = "";

    public override void OnActivated()
    {
        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);
    }

    private void HandleKeyPress(KeyPressed key)
    {
        // Handle input differently when in text entry mode
        if (IsAddingItem)
        {
            HandleTextEntryKeyPress(key);
            return;
        }

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
                StartAddingItem();
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

    private void HandleTextEntryKeyPress(KeyPressed key)
    {
        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.Enter:
                ConfirmAddItem();
                break;
            case ConsoleKey.Escape:
                CancelAddItem();
                break;
            case ConsoleKey.Backspace:
                if (NewItemText.Length > 0)
                {
                    NewItemText = NewItemText[..^1];
                }
                break;
            default:
                // Add printable characters
                if (key.KeyInfo.KeyChar != '\0' && !char.IsControl(key.KeyInfo.KeyChar))
                {
                    NewItemText += key.KeyInfo.KeyChar;
                }
                break;
        }
    }

    private void StartAddingItem()
    {
        IsAddingItem = true;
        NewItemText = "";
        StatusMessage = "Type task name, Enter to add, Escape to cancel";
    }

    private void ConfirmAddItem()
    {
        if (!string.IsNullOrWhiteSpace(NewItemText))
        {
            var newItems = Items.ToList();
            newItems.Add(new TodoItem(NewItemText.Trim(), false));
            Items = newItems;
            SelectedIndex = Items.Count - 1;
            StatusMessage = $"Added: {NewItemText.Trim()}";
        }
        else
        {
            StatusMessage = "Cancelled - empty task name";
        }

        IsAddingItem = false;
        NewItemText = "";
    }

    private void CancelAddItem()
    {
        IsAddingItem = false;
        NewItemText = "";
        StatusMessage = "Cancelled adding item";
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
