// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Terminal;

/// <summary>
/// Represents a color for terminal rendering.
/// Supports default, 256-color, and true color (24-bit RGB) modes.
/// </summary>
public readonly struct Color : IEquatable<Color>
{
    /// <summary>
    /// The color mode.
    /// </summary>
    public ColorMode Mode { get; }

    /// <summary>
    /// For 256-color mode, the palette index (0-255).
    /// </summary>
    public byte Index { get; }

    /// <summary>
    /// Red component for RGB mode (0-255).
    /// </summary>
    public byte R { get; }

    /// <summary>
    /// Green component for RGB mode (0-255).
    /// </summary>
    public byte G { get; }

    /// <summary>
    /// Blue component for RGB mode (0-255).
    /// </summary>
    public byte B { get; }

    private Color(ColorMode mode, byte index = 0, byte r = 0, byte g = 0, byte b = 0)
    {
        Mode = mode;
        Index = index;
        R = r;
        G = g;
        B = b;
    }

    /// <summary>
    /// Default terminal color (no explicit color set).
    /// </summary>
    public static Color Default => new(ColorMode.Default);

    /// <summary>
    /// Create a color from a 256-color palette index.
    /// </summary>
    public static Color FromIndex(byte index) => new(ColorMode.Indexed, index);

    /// <summary>
    /// Create a color from RGB values.
    /// </summary>
    public static Color FromRgb(byte r, byte g, byte b) => new(ColorMode.Rgb, 0, r, g, b);

    /// <summary>
    /// Create a color from a hex string (e.g., "#FF0000" or "FF0000").
    /// </summary>
    public static Color FromHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length != 6)
            throw new ArgumentException("Hex color must be 6 characters", nameof(hex));

        var r = Convert.ToByte(hex[0..2], 16);
        var g = Convert.ToByte(hex[2..4], 16);
        var b = Convert.ToByte(hex[4..6], 16);
        return FromRgb(r, g, b);
    }

    // Standard colors (using 256-color palette indices)
    public static Color Black => FromIndex(0);
    public static Color Red => FromIndex(1);
    public static Color Green => FromIndex(2);
    public static Color Yellow => FromIndex(3);
    public static Color Blue => FromIndex(4);
    public static Color Magenta => FromIndex(5);
    public static Color Cyan => FromIndex(6);
    public static Color White => FromIndex(7);

    // Bright variants
    public static Color BrightBlack => FromIndex(8);
    public static Color BrightRed => FromIndex(9);
    public static Color BrightGreen => FromIndex(10);
    public static Color BrightYellow => FromIndex(11);
    public static Color BrightBlue => FromIndex(12);
    public static Color BrightMagenta => FromIndex(13);
    public static Color BrightCyan => FromIndex(14);
    public static Color BrightWhite => FromIndex(15);

    /// <summary>
    /// Get the ANSI escape sequence for this color as a foreground color.
    /// </summary>
    public string ToForegroundAnsi() => Mode switch
    {
        ColorMode.Default => AnsiCodes.ForegroundDefault,
        ColorMode.Indexed => AnsiCodes.Foreground256(Index),
        ColorMode.Rgb => AnsiCodes.ForegroundRgb(R, G, B),
        _ => AnsiCodes.ForegroundDefault
    };

    /// <summary>
    /// Get the ANSI escape sequence for this color as a background color.
    /// </summary>
    public string ToBackgroundAnsi() => Mode switch
    {
        ColorMode.Default => AnsiCodes.BackgroundDefault,
        ColorMode.Indexed => AnsiCodes.Background256(Index),
        ColorMode.Rgb => AnsiCodes.BackgroundRgb(R, G, B),
        _ => AnsiCodes.BackgroundDefault
    };

    public bool Equals(Color other) =>
        Mode == other.Mode && Index == other.Index && R == other.R && G == other.G && B == other.B;

    public override bool Equals(object? obj) => obj is Color other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Mode, Index, R, G, B);

    public static bool operator ==(Color left, Color right) => left.Equals(right);

    public static bool operator !=(Color left, Color right) => !left.Equals(right);

    public override string ToString() => Mode switch
    {
        ColorMode.Default => "Default",
        ColorMode.Indexed => $"Index({Index})",
        ColorMode.Rgb => $"RGB({R},{G},{B})",
        _ => "Unknown"
    };
}

/// <summary>
/// The mode of color specification.
/// </summary>
public enum ColorMode
{
    /// <summary>
    /// Use terminal's default color.
    /// </summary>
    Default,

    /// <summary>
    /// Use 256-color palette index.
    /// </summary>
    Indexed,

    /// <summary>
    /// Use 24-bit RGB color.
    /// </summary>
    Rgb
}
