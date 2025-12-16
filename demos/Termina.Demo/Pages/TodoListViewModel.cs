using System.Reactive.Linq;
using Termina.Input;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// ViewModel for a todo list demo.
/// Contains only state and business logic - no UI concerns.
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
    [Reactive] private bool _showPriorityModal;
    [Reactive] private string _pendingTaskText = "";

    public override void OnActivated()
    {
        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);
    }

    /// <summary>
    /// Called by Page when text input is submitted.
    /// </summary>
    public void OnTextInputSubmitted(string text)
    {
        if (!string.IsNullOrWhiteSpace(text))
        {
            PendingTaskText = text.Trim();
            ShowPriorityModal = true;
            StatusMessage = "Select priority with ↑/↓ or number keys, Enter to confirm";
        }
        else
        {
            CancelAddItem();
        }
    }

    /// <summary>
    /// Called by Page when priority is selected.
    /// </summary>
    public void OnPrioritySelected(string priority)
    {
        var newItems = Items.ToList();
        var displayText = priority != "Normal" ? $"[{priority}] {PendingTaskText}" : PendingTaskText;
        newItems.Add(new TodoItem(displayText, false));
        Items = newItems;
        SelectedIndex = Items.Count - 1;
        StatusMessage = $"Added: {displayText}";

        // Clean up state
        ShowPriorityModal = false;
        IsAddingItem = false;
        PendingTaskText = "";
    }

    /// <summary>
    /// Called by Page when priority selection is cancelled.
    /// </summary>
    public void OnPriorityCancelled()
    {
        ShowPriorityModal = false;
    }

    /// <summary>
    /// Called by Page when add modal is dismissed.
    /// </summary>
    public void OnAddModalDismissed()
    {
        CancelAddItem();
    }

    /// <summary>
    /// Called by Page when priority modal is dismissed.
    /// </summary>
    public void OnPriorityModalDismissed()
    {
        ShowPriorityModal = false;
        IsAddingItem = false;
        StatusMessage = "Cancelled adding item";
    }

    private void HandleKeyPress(KeyPressed key)
    {
        // Don't handle keys when in modal mode - let the modals handle input
        if (IsAddingItem || ShowPriorityModal)
            return;

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

    private void StartAddingItem()
    {
        IsAddingItem = true;
        ShowPriorityModal = false;
        PendingTaskText = "";
        StatusMessage = "Enter task name and press Enter, or Escape to cancel";
    }

    private void CancelAddItem()
    {
        IsAddingItem = false;
        ShowPriorityModal = false;
        PendingTaskText = "";
        StatusMessage = "Cancelled adding item";
    }

    private void ToggleSelected()
    {
        if (Items.Count == 0) return;

        var item = Items[SelectedIndex];
        var newItem = item with { IsCompleted = !item.IsCompleted };

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
