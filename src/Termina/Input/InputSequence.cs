// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

namespace Termina.Input;

/// <summary>
/// Buffered terminal escape-sequence text passed between the tokenizer/parser and decoders.
/// </summary>
internal readonly record struct InputSequence
{
    private readonly string? _text;

    public InputSequence(string text)
    {
        _text = text ?? throw new ArgumentNullException(nameof(text));
    }

    public string Text => _text ?? string.Empty;

    public int Length => Text.Length;

    public bool IsEmpty => Length == 0;

    public char this[int index] => Text[index];

    public override string ToString() => Text;

    public static implicit operator InputSequence(string text) => new(text);
}
