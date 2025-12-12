using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Components;
using Termina.Reactive;

namespace Termina.Demo.Pages;

/// <summary>
/// Page for the todo list demo.
/// Demonstrates list components and state binding.
/// </summary>
public class TodoListPage : ReactivePage<TodoListViewModel>
{
    private readonly SelectList _todoList = new()
    {
        Title = "[bold]Todo List[/]",
        SelectedColor = Color.Green
    };

    private readonly StatusBar _statusBar = new()
    {
        Hints = "[↑/↓] Navigate [Space] Toggle [A] Add [D] Delete [C] Counter [Q] Quit"
    };

    private IReadOnlyList<TodoItem> _items = Array.Empty<TodoItem>();

    protected override void OnBound()
    {
        // Subscribe to items changes
        ViewModel.ItemsChanged
            .Subscribe(items =>
            {
                _items = items;
                _todoList.Options = items.Select(FormatItem).ToList();
            })
            .DisposeWith(Subscriptions);

        // Subscribe to selected index changes
        ViewModel.SelectedIndexChanged
            .Subscribe(idx => _todoList.SelectedIndex = idx)
            .DisposeWith(Subscriptions);

        // Subscribe to status message changes
        ViewModel.StatusMessageChanged
            .Subscribe(msg => _statusBar.Message = msg)
            .DisposeWith(Subscriptions);
    }

    private static string FormatItem(TodoItem item)
    {
        var checkbox = item.IsCompleted ? "[✓]" : "[ ]";
        var style = item.IsCompleted ? "strikethrough dim" : "";
        return string.IsNullOrEmpty(style)
            ? $"{checkbox} {item.Description}"
            : $"{checkbox} [{style}]{item.Description}[/]";
    }

    public override IRenderable Render()
    {
        var statsText = _items.Count == 0
            ? "No items"
            : $"{_items.Count(i => i.IsCompleted)}/{_items.Count} completed";

        var header = new Markup($"[bold cyan]Todo List Demo[/] - {statsText}");

        return new Rows(
            header,
            new Text(""),
            _todoList.Render(),
            _statusBar.Render()
        );
    }
}
