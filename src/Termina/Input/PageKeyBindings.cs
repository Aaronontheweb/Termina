// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Manages page-level key bindings that intercept input before focused components.
/// This implements a "capture phase" for keyboard input, allowing pages to handle
/// keys like Escape or Tab before they reach child components.
/// </summary>
/// <remarks>
/// <para>
/// Key bindings registered here take precedence over focused component handlers.
/// This solves the common problem where components consume keys (like Escape) that
/// pages need for navigation.
/// </para>
/// <para>
/// Bindings support modifier keys (Ctrl, Alt, Shift) for complex shortcuts.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // In a ReactivePage, register bindings in OnNavigatedTo:
/// KeyBindings.Register(ConsoleKey.Escape, () => ViewModel.Navigate("/menu"));
/// KeyBindings.Register(ConsoleKey.Tab, () => CycleFocus());
/// KeyBindings.Register(ConsoleKey.S, ConsoleModifiers.Control, () => Save());
/// </code>
/// </example>
public sealed class PageKeyBindings
{
    private readonly Dictionary<KeyBinding, Action> _bindings = new();

    /// <summary>
    /// Registers a key binding with optional modifiers.
    /// </summary>
    /// <param name="key">The key to bind.</param>
    /// <param name="handler">Action to execute when key is pressed.</param>
    public void Register(ConsoleKey key, Action handler)
    {
        Register(key, 0, handler);
    }

    /// <summary>
    /// Registers a key binding with specific modifiers.
    /// </summary>
    /// <param name="key">The key to bind.</param>
    /// <param name="modifiers">Required modifier keys (Ctrl, Alt, Shift).</param>
    /// <param name="handler">Action to execute when key combination is pressed.</param>
    public void Register(ConsoleKey key, ConsoleModifiers modifiers, Action handler)
    {
        var binding = new KeyBinding(key, modifiers);
        _bindings[binding] = handler;
    }

    /// <summary>
    /// Unregisters a key binding.
    /// </summary>
    /// <param name="key">The key to unbind.</param>
    /// <param name="modifiers">The modifier combination to unbind.</param>
    /// <returns>True if a binding was removed, false if none existed.</returns>
    public bool Unregister(ConsoleKey key, ConsoleModifiers modifiers = 0)
    {
        return _bindings.Remove(new KeyBinding(key, modifiers));
    }

    /// <summary>
    /// Attempts to handle a key press using registered bindings.
    /// </summary>
    /// <param name="keyInfo">The key press information.</param>
    /// <returns>True if a binding handled the key, false otherwise.</returns>
    public bool TryHandle(ConsoleKeyInfo keyInfo)
    {
        var binding = new KeyBinding(keyInfo.Key, keyInfo.Modifiers);

        if (_bindings.TryGetValue(binding, out var handler))
        {
            handler();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears all registered key bindings.
    /// </summary>
    public void Clear()
    {
        _bindings.Clear();
    }

    /// <summary>
    /// Gets the number of registered bindings.
    /// </summary>
    public int Count => _bindings.Count;

    /// <summary>
    /// Represents a key + modifier combination for binding lookup.
    /// </summary>
    private readonly record struct KeyBinding(ConsoleKey Key, ConsoleModifiers Modifiers);
}
