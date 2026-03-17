namespace Termina.Layout;

/// <summary>
/// Keyboard gesture for copy actions inside a copyable text node.
/// </summary>
public sealed record CopyKeyBinding(ConsoleKey Key, ConsoleModifiers Modifiers = 0);
