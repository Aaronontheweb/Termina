// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Termina.Terminal;

/// <summary>
/// An immutable color gradient defined by a series of color stops.
/// Use <see cref="Sample"/> to interpolate a color at any position along the gradient.
/// </summary>
public sealed record Gradient
{
    private readonly (float Position, Color Color)[] _stops;

    private Gradient((float Position, Color Color)[] stops)
    {
        _stops = stops;
    }

    /// <summary>
    /// Create a gradient with evenly distributed color stops.
    /// At least 2 colors are required.
    /// </summary>
    public static Gradient Create() =>
        throw new ArgumentException("Gradient requires at least 2 colors.", "colors");

    /// <summary>
    /// Create a gradient with evenly distributed color stops.
    /// </summary>
    public static Gradient Create(params Color[] colors)
    {
        if (colors.Length < 2)
            throw new ArgumentException("Gradient requires at least 2 colors.", nameof(colors));

        var stops = new (float, Color)[colors.Length];
        for (var i = 0; i < colors.Length; i++)
            stops[i] = (i / (float)(colors.Length - 1), colors[i]);

        return new Gradient(stops);
    }

    /// <summary>
    /// Create a gradient with custom stop positions.
    /// </summary>
    public static Gradient Create(params (float position, Color color)[] stops)
    {
        if (stops.Length < 2)
            throw new ArgumentException("Gradient requires at least 2 stops.", nameof(stops));

        var sorted = stops.OrderBy(s => s.position).ToArray();
        return new Gradient(sorted);
    }

    /// <summary>
    /// Sample the interpolated color at position <paramref name="t"/> (clamped to 0–1).
    /// </summary>
    public Color Sample(float t)
    {
        t = Math.Clamp(t, 0f, 1f);

        if (t <= _stops[0].Position)
            return _stops[0].Color;

        if (t >= _stops[^1].Position)
            return _stops[^1].Color;

        for (var i = 0; i < _stops.Length - 1; i++)
        {
            var (pos0, color0) = _stops[i];
            var (pos1, color1) = _stops[i + 1];

            if (t >= pos0 && t <= pos1)
            {
                var segmentT = (t - pos0) / (pos1 - pos0);
                return Color.Lerp(color0, color1, segmentT);
            }
        }

        return _stops[^1].Color;
    }
}
