using System.Reactive.Linq;
using Termina.Input;
using Termina.Layout;
using Termina.Reactive;
using Termina.Rendering;

namespace Termina.Demo.Pages;

/// <summary>
/// ViewModel for a todo list demo.
/// Demonstrates list selection and state management with modal dialogs.
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
    [Reactive] private bool _showPriorityModal;
    [Reactive] private string _pendingTaskText = "";

    // Modal and selection list instances (created once, reused)
    private ModalNode? _addModal;
    private ModalNode? _priorityModal;
    private SelectionListNode<string>? _priorityList;
    private TextInputNode? _textInput;

    public override void OnActivated()
    {
        // Subscribe to keyboard input
        Input.OfType<KeyPressed>()
            .Subscribe(HandleKeyPress)
            .DisposeWith(Subscriptions);

        // Create the text input for the add modal
        _textInput = new TextInputNode()
            .WithPlaceholder("Enter task description...");

        // Subscribe to text input submission
        _textInput.Submitted
            .Subscribe(text =>
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    PendingTaskText = text.Trim();
                    ShowPrioritySelection();
                }
                else
                {
                    CancelAddItem();
                }
            })
            .DisposeWith(Subscriptions);

        // Create priority selection list
        _priorityList = Layouts.SelectionList("High", "Medium", "Low")
            .WithMode(SelectionMode.Single)
            .WithShowNumbers(true)
            .WithHighlightColors(Terminal.Color.Black, Terminal.Color.Cyan)
            .WithOtherOption("Custom priority...");

        // Subscribe to priority selection
        _priorityList.SelectionConfirmed
            .Subscribe(selected =>
            {
                var priority = selected.FirstOrDefault() ?? "Normal";
                AddNewItem(PendingTaskText, priority);
            })
            .DisposeWith(Subscriptions);

        _priorityList.OtherSelected
            .Subscribe(customPriority =>
            {
                AddNewItem(PendingTaskText, customPriority);
            })
            .DisposeWith(Subscriptions);

        _priorityList.Cancelled
            .Subscribe(_ =>
            {
                // Go back to text input
                ShowPriorityModal = false;
                Focus.PopFocus();
            })
            .DisposeWith(Subscriptions);

        // Create modals
        _addModal = Layouts.Modal()
            .WithTitle("Add New Task")
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Terminal.Color.Cyan)
            .WithBackdrop(BackdropStyle.Dim)
            .WithPosition(ModalPosition.Center)
            .WithPadding(1)
            .WithContent(_textInput)
            .WithDismissOnEscape(true);

        _addModal.Dismissed
            .Subscribe(_ => CancelAddItem())
            .DisposeWith(Subscriptions);

        _priorityModal = Layouts.Modal()
            .WithTitle("Select Priority")
            .WithBorder(BorderStyle.Rounded)
            .WithBorderColor(Terminal.Color.Yellow)
            .WithBackdrop(BackdropStyle.Dim)
            .WithPosition(ModalPosition.Center)
            .WithPadding(1)
            .WithContent(_priorityList)
            .WithDismissOnEscape(true);

        _priorityModal.Dismissed
            .Subscribe(_ =>
            {
                ShowPriorityModal = false;
                IsAddingItem = false;
                Focus.PopFocus();
                StatusMessage = "Cancelled adding item";
            })
            .DisposeWith(Subscriptions);
    }

    /// <summary>
    /// Gets the modal for adding items (used by the Page).
    /// </summary>
    public ModalNode? AddModal => _addModal;

    /// <summary>
    /// Gets the modal for priority selection (used by the Page).
    /// </summary>
    public ModalNode? PriorityModal => _priorityModal;

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
        ShowPriorityModal = false;
        NewItemText = "";
        PendingTaskText = "";
        _textInput?.Clear();
        StatusMessage = "Enter task name and press Enter, or Escape to cancel";

        // Push focus to the modal for keyboard input
        if (_addModal != null)
        {
            Focus.PushFocus(_addModal);
        }
    }

    private void ShowPrioritySelection()
    {
        ShowPriorityModal = true;

        // Switch focus from text input modal to priority modal
        Focus.PopFocus(); // Remove add modal focus

        if (_priorityModal != null && _priorityList != null)
        {
            Focus.PushFocus(_priorityModal);
            Focus.PushFocus(_priorityList); // Selection list needs focus for keyboard input
        }

        StatusMessage = "Select priority with ↑/↓ or number keys, Enter to confirm";
    }

    private void AddNewItem(string description, string priority)
    {
        var newItems = Items.ToList();
        var displayText = priority != "Normal" ? $"[{priority}] {description}" : description;
        newItems.Add(new TodoItem(displayText, false));
        Items = newItems;
        SelectedIndex = Items.Count - 1;
        StatusMessage = $"Added: {displayText}";

        // Clean up modal state
        ShowPriorityModal = false;
        IsAddingItem = false;
        PendingTaskText = "";
        Focus.ClearFocus();
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
        ShowPriorityModal = false;
        NewItemText = "";
        PendingTaskText = "";
        Focus.ClearFocus();
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
