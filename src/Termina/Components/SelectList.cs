using Spectre.Console;
using Spectre.Console.Rendering;
using Termina.Input;

namespace Termina.Components;

// Business events for SelectList

/// <summary>
/// Command: Set the available options for the select list.
/// </summary>
public sealed record SetOptions(IReadOnlyList<string> Options);

/// <summary>
/// Command: Programmatically select a specific option by index.
/// </summary>
public sealed record SelectOption(int Index);

/// <summary>
/// Notification: User selected an option (pressed Enter).
/// </summary>
public sealed record OptionSelected(int Index, string Value);

/// <summary>
/// Notification: User changed the highlighted option (arrow keys).
/// </summary>
public sealed record OptionHighlighted(int Index, string Value);

/// <summary>
/// A selectable list component.
/// Handles arrow keys internally to navigate, emits business events when user selects.
/// </summary>
public sealed class SelectList : Component
{
    private List<string> _options = new();
    private int _highlightedIndex = 0;
    private string _title = "";

    public SelectList(string title = "")
    {
        _title = title;

        // Subscribe to business commands
        Subscribe<SetOptions>(OnSetOptions);
        Subscribe<SelectOption>(OnSelectOption);
    }

    private void OnSetOptions(SetOptions evt)
    {
        _options = evt.Options.ToList();
        _highlightedIndex = 0;
    }

    private void OnSelectOption(SelectOption evt)
    {
        if (_options.Count > 0)
        {
            _highlightedIndex = Math.Clamp(evt.Index, 0, _options.Count - 1);
        }
    }

    /// <summary>
    /// Handle keyboard input for navigation.
    /// </summary>
    public override void HandleInput(KeyPressed key)
    {
        if (_options.Count == 0) return;

        var oldIndex = _highlightedIndex;

        switch (key.KeyInfo.Key)
        {
            case ConsoleKey.UpArrow:
                _highlightedIndex = Math.Max(0, _highlightedIndex - 1);
                break;
            case ConsoleKey.DownArrow:
                _highlightedIndex = Math.Min(_options.Count - 1, _highlightedIndex + 1);
                break;
            case ConsoleKey.Enter:
                Emit(new OptionSelected(_highlightedIndex, _options[_highlightedIndex]));
                return;
        }

        // Emit highlight change if index changed
        if (oldIndex != _highlightedIndex)
        {
            Emit(new OptionHighlighted(_highlightedIndex, _options[_highlightedIndex]));
        }
    }

    /// <summary>
    /// Get the currently highlighted index.
    /// </summary>
    public int HighlightedIndex => _highlightedIndex;

    /// <summary>
    /// Get the currently highlighted value, or null if no options.
    /// </summary>
    public string? HighlightedValue => _options.Count > 0 ? _options[_highlightedIndex] : null;

    public override IRenderable Render()
    {
        if (_options.Count == 0)
        {
            return new Markup("[grey](no options)[/]");
        }

        var rows = _options.Select((opt, i) =>
            i == _highlightedIndex
                ? new Markup($"[bold blue]> {Markup.Escape(opt)}[/]")
                : new Markup($"  {Markup.Escape(opt)}"));

        IRenderable content = new Rows(rows.Cast<IRenderable>());

        if (!string.IsNullOrEmpty(_title))
        {
            content = new Panel(content)
                .Header($"[bold]{Markup.Escape(_title)}[/]")
                .Border(BoxBorder.Rounded);
        }

        return content;
    }
}
