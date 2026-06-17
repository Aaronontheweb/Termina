namespace Termina.Layout;

public enum GraphStyle
{
    /// <summary>▁▂▃▄▅▆▇█ -- classic filled blocks from bottom</summary>
    Blocks,

    /// <summary>Only the top edge is drawn -- hollow inside</summary>
    Outline,

    /// <summary>Braille dots -- double vertical resolution per row</summary>
    Braille,

    /// <summary>ASCII _ . - ~ ^ * # @ fallback</summary>
    Ascii,
}
