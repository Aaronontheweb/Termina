namespace Termina.Input;

/// <summary>
/// Low-level keyboard input event.
/// Contains the raw ConsoleKeyInfo from the keyboard.
/// </summary>
public sealed record KeyPressed(ConsoleKeyInfo KeyInfo) : IInputEvent;
