// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CsCheck;
using Termina.Terminal;

namespace Termina.Tests.Terminal;

/// <summary>
/// Property-based tests for <see cref="DisplayWidth"/> (CsCheck).
/// </summary>
/// <remarks>
/// Each test states one rule that must hold for every generated input. CsCheck makes many random
/// inputs, and it shrinks a failure to the smallest input and prints the seed. The rules are
/// stated relative to <see cref="DisplayWidth.EnumerateCells"/>, which is the cell model that all
/// the other methods must agree with. A "cell" is one display unit (a letter, a letter with its
/// marks, or one emoji). A "boundary" is the point between two cells.
/// </remarks>
public class DisplayWidthPropertyTests
{
    private const int Iter = 10_000;
    private const char Esc = (char)0x1B;         // escape
    private const char Bel = (char)0x07;         // bell
    private const char Vs16 = (char)0xFE0F;      // emoji variation selector
    private const char Keycap = (char)0x20E3;    // combining keycap
    private const char Zwj = (char)0x200D;       // zero-width joiner
    private const char Cjk = (char)0x4E2D;       // a wide CJK character

    // Builds a string from Unicode code points. Keeps the source pure ASCII.
    private static string Cp(params int[] codepoints) => string.Concat(codepoints.Select(char.ConvertFromUtf32));

    // ---- Generators ----

    // Whole cells. Each item is one complete display unit.
    private static readonly string[] CellPalette =
    {
        "a", "B", "7", "#", " ", "z",              // narrow (1 column)
        "中", "文", "あ", "한", "Ａ",                // wide (2 columns)
        Cp(0x65, 0x0301), Cp(0x6F, 0x0308),        // base + combining mark
        Cp(0x1F600), Cp(0x1F389), Cp(0x20000),     // supplementary (surrogate pairs)
        Cp(0x2600, 0xFE0F), Cp(0x270B, 0xFE0F),    // emoji + variation selector (2 columns)
        Cp(0x31, 0xFE0F, 0x20E3),                  // keycap sequence (2 columns)
    };

    // A text built from whole cells. Boundaries are clean.
    private static readonly Gen<string> CellText =
        Gen.OneOfConst(CellPalette).List[0, 8].Select(parts => string.Concat(parts));

    // A hostile UTF-16 text: arbitrary code units, plus escapes, controls, and surrogates.
    private static readonly Gen<char> FuzzChar = Gen.OneOf(
        Gen.Char,
        Gen.OneOfConst(Esc, '[', ']', Bel, 'm', '\n', '\t', '\r', '\0', ' ', 'a', Cjk, Vs16, Keycap, Zwj),
        Gen.OneOfConst('\uD83D', '\uDE00', '\uD800', '\uDBFF', '\uDC00', '\uDFFF'));

    private static readonly Gen<string> FuzzText =
        FuzzChar.Array[0, 24].Select(chars => new string(chars));

    // Either kind of text. Every rule below holds for both, because each rule is stated against
    // the cell model of the actual text.
    private static readonly Gen<string> AnyText = Gen.OneOf(CellText, FuzzText);

    // Column and index arguments, including out-of-range values.
    private static readonly Gen<int> WidthArg = Gen.OneOf(Gen.Int[-3, 40], Gen.Const(0), Gen.Const(int.MaxValue));
    private static readonly Gen<int> IndexArg = Gen.OneOf(Gen.Int[-3, 40], Gen.Const(int.MaxValue));

    // A text plus one of its real cell boundaries.
    private static readonly Gen<(string Text, int Boundary)> TextWithBoundary =
        AnyText.SelectMany(s =>
        {
            var bounds = Boundaries(s);
            return Gen.Int[0, bounds.Count - 1].Select(k => (s, bounds[k]));
        });

    private static readonly Gen<(string Text, int N)> TextAndWidth =
        from s in AnyText from n in WidthArg select (s, n);

    // ---- Helpers ----

    private static List<int> Boundaries(string s)
    {
        var list = new List<int>();
        foreach (var cell in DisplayWidth.EnumerateCells(s))
            list.Add(cell.StartIndex);
        list.Add(s.Length); // the end of the text is always a boundary
        return list;
    }

    private static HashSet<int> BoundarySet(string s) => new(Boundaries(s));

    // True when the first cell of the text uses zero columns (a leading combining mark, format
    // character, or control character). SliceByColumns drops such a leading cell at column 0, but
    // TruncateToColumns keeps it. The equality properties scope this edge out.
    private static bool StartsWithZeroWidthCell(string s) =>
        s.Length > 0 && DisplayWidth.EnumerateCells(s).First().ColumnWidth == 0;

    private static List<string> CellTexts(string s) =>
        DisplayWidth.EnumerateCells(s).Select(c => c.Text).ToList();

    // True when the slice is one continuous run of whole cells taken in order from the source.
    private static bool IsContiguousCellRun(string s, string slice)
    {
        var whole = CellTexts(s);
        var part = CellTexts(slice);
        if (part.Count == 0)
            return true;
        for (var i = 0; i + part.Count <= whole.Count; i++)
            if (whole.Skip(i).Take(part.Count).SequenceEqual(part))
                return true;
        return false;
    }

    // ===== A. Cell decomposition =====

    [Fact]
    public void A1_Cells_PartitionTheText() =>
        // The cells are contiguous, cover the whole text, and rebuild it exactly.
        AnyText.Sample(s =>
        {
            var pos = 0;
            var cells = DisplayWidth.EnumerateCells(s).ToList();
            foreach (var c in cells)
            {
                Assert.Equal(pos, c.StartIndex);   // no gap and no overlap
                Assert.True(c.Length >= 1);
                pos += c.Length;
            }
            Assert.Equal(s.Length, pos);           // the cells reach the end
            Assert.Equal(s, string.Concat(cells.Select(c => c.Text)));
        }, iter: Iter);

    [Fact]
    public void A2_CellWidth_IsZeroOneOrTwo() =>
        // Every cell uses 0, 1, or 2 columns. No cell uses more.
        AnyText.Sample(s =>
        {
            foreach (var c in DisplayWidth.EnumerateCells(s))
                Assert.InRange(c.ColumnWidth, 0, 2);
        }, iter: Iter);

    [Fact]
    public void A3_SumOfCells_EqualsColumnCount() =>
        // The cell widths add up to the width of the whole text.
        AnyText.Sample(s =>
            Assert.Equal(DisplayWidth.GetColumnCount(s), DisplayWidth.EnumerateCells(s).Sum(c => c.ColumnWidth)),
            iter: Iter);

    [Fact]
    public void A4_CharWidth_MatchesStringWidth() =>
        // The char overload agrees with the string overload.
        Gen.Char.Sample(c =>
            Assert.Equal(DisplayWidth.GetColumnCount(c.ToString()), DisplayWidth.GetColumnCount(c)),
            iter: Iter);

    // ===== B. Width composition =====

    [Fact]
    public void B1_Width_IsAdditiveAtABoundary() =>
        // Split at a boundary. The two parts' widths add up to the width of the whole text.
        TextWithBoundary.Sample(t =>
        {
            var (s, i) = t;
            Assert.Equal(
                DisplayWidth.GetColumnCount(s),
                DisplayWidth.GetColumnCount(s[..i]) + DisplayWidth.GetColumnCount(s[i..]));
        }, iter: Iter);

    // ===== C. Left truncation =====

    [Fact]
    public void C1_Truncate_DoesNotExceedWidth() =>
        // The kept left part is never wider than N columns.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            Assert.True(DisplayWidth.GetColumnCount(DisplayWidth.TruncateToColumns(s, n)) <= Math.Max(0, n));
        }, iter: Iter);

    [Fact]
    public void C2_Truncate_IsAPrefixAndMatchesIndex() =>
        // The kept part is a real prefix of the text and matches the boundary index.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            var kept = DisplayWidth.TruncateToColumns(s, n);
            Assert.StartsWith(kept, s, StringComparison.Ordinal);
            Assert.Equal(s[..DisplayWidth.GetStringIndexForColumnCount(s, n)], kept);
        }, iter: Iter);

    [Fact]
    public void C3_Truncate_IsIdempotent() =>
        // Cutting the result again to the same width changes nothing.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            var once = DisplayWidth.TruncateToColumns(s, n);
            Assert.Equal(once, DisplayWidth.TruncateToColumns(once, n));
        }, iter: Iter);

    [Fact]
    public void C4_Truncate_KeepsAllWhenItFits() =>
        // If the text already fits in N columns (N >= 1), the cut keeps all of it.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            if (n >= 1 && DisplayWidth.GetColumnCount(s) <= n)
                Assert.Equal(s, DisplayWidth.TruncateToColumns(s, n));
        }, iter: Iter);

    [Fact]
    public void C5_Truncate_IsMaximal() =>
        // The cut keeps as much as possible: adding the next cell would exceed N.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            if (n < 1)
                return;
            var idx = DisplayWidth.GetStringIndexForColumnCount(s, n);
            if (idx >= s.Length)
                return; // the whole text fits, so there is no next cell
            var kept = DisplayWidth.GetColumnCount(s[..idx]);
            var next = DisplayWidth.EnumerateCells(s[idx..]).First();
            Assert.True(kept + next.ColumnWidth > n);
        }, iter: Iter);

    // ===== D. Slicing =====

    [Fact]
    public void D1_Slice_DoesNotExceedWidth() =>
        // A slice is never wider than N columns.
        (from s in AnyText from start in IndexArg from n in WidthArg select (s, start, n)).Sample(t =>
        {
            var (s, start, n) = t;
            Assert.True(DisplayWidth.GetColumnCount(DisplayWidth.SliceByColumns(s, start, n)) <= Math.Max(0, n));
        }, iter: Iter);

    [Fact]
    public void D2_Slice_IsContiguousCellRun() =>
        // A slice is one continuous run of whole cells, in order. It never drops a middle cell or
        // mixes cells. This is the fault that PR #351 fixed.
        (from s in AnyText from start in IndexArg from n in WidthArg select (s, start, n)).Sample(t =>
        {
            var (s, start, n) = t;
            var slice = DisplayWidth.SliceByColumns(s, start, n);
            Assert.True(IsContiguousCellRun(s, slice));
        }, iter: Iter);

    [Fact]
    public void D3_SliceFromZero_EqualsTruncate() =>
        // A slice from column 0 equals the left cut to the same width.
        // Known divergence: when the text begins with a zero-width cell, SliceByColumns drops it
        // while TruncateToColumns keeps it. That edge is scoped out and tracked as a follow-up.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            if (StartsWithZeroWidthCell(s))
                return;
            Assert.Equal(DisplayWidth.TruncateToColumns(s, n), DisplayWidth.SliceByColumns(s, 0, n));
        }, iter: Iter);

    [Fact]
    public void D4_Slices_RejoinAtABoundary() =>
        // Two slices split at a boundary rejoin into the whole text.
        TextWithBoundary.Sample(t =>
        {
            var (s, k) = t;
            if (StartsWithZeroWidthCell(s))
                return; // a leading zero-width cell is dropped by the left slice (see D3)
            var kCols = DisplayWidth.GetColumnCount(s[..k]);
            if (kCols < 1)
                return; // a zero-column left part meets the maxColumns <= 0 guard (a separate case)
            var left = DisplayWidth.SliceByColumns(s, 0, kCols);
            var right = DisplayWidth.SliceByColumns(s, kCols, int.MaxValue / 2);
            Assert.Equal(s, left + right);
        }, iter: Iter);

    [Fact]
    public void D5_Slice_DropsTheStraddlingWideCellAtStart() =>
        // If the start falls inside a wide cell, that one cell is dropped and the slice begins at
        // the next boundary.
        CellText.Sample(s =>
        {
            var cells = DisplayWidth.EnumerateCells(s).ToList();
            var col = 0;
            var wideIndex = -1;
            var wideCol = 0;
            for (var k = 0; k < cells.Count; k++)
            {
                if (cells[k].ColumnWidth == 2)
                {
                    wideIndex = k;
                    wideCol = col;
                    break;
                }
                col += cells[k].ColumnWidth;
            }
            if (wideIndex < 0)
                return; // this sample has no wide cell
            var startInside = wideCol + 1; // a column inside the wide cell
            var slice = DisplayWidth.SliceByColumns(s, startInside, int.MaxValue / 2);
            var expected = string.Concat(cells.Skip(wideIndex + 1).Select(c => c.Text));
            Assert.Equal(expected, slice);
        }, iter: Iter);

    [Fact]
    public void D6_TruncateStart_IsSuffixWithinWidthAndMaximal() =>
        // The right cut keeps a suffix that fits in N columns and keeps as much as possible.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            var kept = DisplayWidth.TruncateStartToColumns(s, n);
            Assert.True(DisplayWidth.GetColumnCount(kept) <= Math.Max(0, n));
            Assert.EndsWith(kept, s, StringComparison.Ordinal);
            if (n >= 1 && kept.Length < s.Length)
            {
                var leftPart = s[..^kept.Length];
                var prev = DisplayWidth.EnumerateCells(leftPart).Last();
                Assert.True(DisplayWidth.GetColumnCount(kept) + prev.ColumnWidth > n);
            }
        }, iter: Iter);

    // ===== E. Index and column mapping =====

    [Fact]
    public void E1_IndexForColumns_TiesToTruncateAndBoundary() =>
        // The index for N columns equals the length of the left cut, sits on a boundary, and fits.
        TextAndWidth.Sample(t =>
        {
            var (s, n) = t;
            var idx = DisplayWidth.GetStringIndexForColumnCount(s, n);
            Assert.Equal(DisplayWidth.TruncateToColumns(s, n).Length, idx);
            Assert.Contains(idx, BoundarySet(s));
            Assert.True(DisplayWidth.GetColumnCount(s[..idx]) <= Math.Max(0, n));
        }, iter: Iter);

    [Fact]
    public void E2_CursorColumn_IsMonotonic() =>
        // Moving the index forward never lowers the column.
        (from s in AnyText from i in IndexArg from j in IndexArg select (s, i, j)).Sample(t =>
        {
            var (s, i, j) = t;
            if (i > j)
                (i, j) = (j, i);
            Assert.True(DisplayWidth.CursorPositionToColumn(s, i) <= DisplayWidth.CursorPositionToColumn(s, j));
        }, iter: Iter);

    [Fact]
    public void E3_CursorColumn_Endpoints() =>
        // Index 0 gives column 0. The end (or past it) gives the full width.
        AnyText.Sample(s =>
        {
            Assert.Equal(0, DisplayWidth.CursorPositionToColumn(s, 0));
            Assert.Equal(DisplayWidth.GetColumnCount(s), DisplayWidth.CursorPositionToColumn(s, s.Length));
            Assert.Equal(DisplayWidth.GetColumnCount(s), DisplayWidth.CursorPositionToColumn(s, s.Length + 5));
        }, iter: Iter);

    [Fact]
    public void E4_CursorColumn_AtBoundary_EqualsPrefixWidth() =>
        // At a boundary, the column equals the width of the text before the index.
        TextWithBoundary.Sample(t =>
        {
            var (s, i) = t;
            Assert.Equal(DisplayWidth.GetColumnCount(s[..i]), DisplayWidth.CursorPositionToColumn(s, i));
        }, iter: Iter);

    // ===== F. Navigation and boundary safety =====

    [Fact]
    public void F1_Clamp_LandsOnBoundary() =>
        // Clamp lands on a cell boundary for any index, even an out-of-range index.
        (from s in AnyText from i in IndexArg select (s, i)).Sample(t =>
        {
            var (s, i) = t;
            Assert.Contains(DisplayWidth.ClampToTextElementBoundary(s, i), BoundarySet(s));
        }, iter: Iter);

    [Fact]
    public void F2_NextAndPrevious_LandOnBoundaries() =>
        // Next and Previous land on boundaries. The cursor never stops inside a cell.
        (from s in AnyText from i in IndexArg select (s, i)).Sample(t =>
        {
            var (s, i) = t;
            var set = BoundarySet(s);
            Assert.Contains(DisplayWidth.GetNextTextElementIndex(s, i), set);
            Assert.Contains(DisplayWidth.GetPreviousTextElementIndex(s, i), set);
        }, iter: Iter);

    [Fact]
    public void F3_NextAndPrevious_DirectionAndFixpoints() =>
        // Next moves forward and stops at the end. Previous moves back and stops at the start.
        (from s in AnyText from i in Gen.Int[0, 40] select (s, Math.Min(i, s.Length))).Sample(t =>
        {
            var (s, i) = t;
            Assert.True(DisplayWidth.GetNextTextElementIndex(s, i) >= i);
            Assert.True(DisplayWidth.GetPreviousTextElementIndex(s, i) <= i);
            Assert.Equal(s.Length, DisplayWidth.GetNextTextElementIndex(s, s.Length));
            Assert.Equal(0, DisplayWidth.GetPreviousTextElementIndex(s, 0));
        }, iter: Iter);

    [Fact]
    public void F4_Next_VisitsAllBoundariesInOrder() =>
        // Stepping with Next from the start visits every boundary in order. Previous mirrors it.
        AnyText.Sample(s =>
        {
            var ends = DisplayWidth.EnumerateCells(s).Select(c => c.StartIndex + c.Length).ToList();
            var forward = new List<int>();
            var idx = 0;
            while (idx < s.Length)
            {
                var next = DisplayWidth.GetNextTextElementIndex(s, idx);
                Assert.True(next > idx); // Next must make progress
                idx = next;
                forward.Add(idx);
            }
            Assert.Equal(ends, forward);

            var starts = DisplayWidth.EnumerateCells(s).Select(c => c.StartIndex).Reverse().ToList();
            var back = new List<int>();
            idx = s.Length;
            while (idx > 0)
            {
                var prev = DisplayWidth.GetPreviousTextElementIndex(s, idx);
                Assert.True(prev < idx); // Previous must make progress
                idx = prev;
                back.Add(idx);
            }
            Assert.Equal(starts, back);
        }, iter: Iter);

    [Fact]
    public void F5_TextElementAt_ReturnsContainingCell() =>
        // The cell at an index is the one that holds it. At or past the end it is a space.
        (from s in AnyText from i in IndexArg select (s, i)).Sample(t =>
        {
            var (s, i) = t;
            var got = DisplayWidth.GetTextElementAt(s, i);
            if (s.Length == 0 || i >= s.Length)
            {
                Assert.Equal(" ", got);
            }
            else
            {
                var ci = Math.Max(0, i);
                var cell = DisplayWidth.EnumerateCells(s).First(c => ci >= c.StartIndex && ci < c.StartIndex + c.Length);
                Assert.Equal(cell.Text, got);
            }
        }, iter: Iter);

    // ===== G. Sanitization =====

    [Fact]
    public void G1_Sanitize_RemovesControlAndEscape() =>
        // The cleaned text has no escape character and no control character.
        FuzzText.Sample(s =>
        {
            var clean = DisplayWidth.SanitizeTerminalText(s);
            Assert.False(clean.Contains(Esc));
            Assert.All(clean, ch => Assert.False(char.IsControl(ch)));
        }, iter: Iter);

    [Fact]
    public void G2_Sanitize_IsIdempotentAndKeepsCleanText() =>
        // Cleaning twice changes nothing. Text that is already clean does not change.
        FuzzText.Sample(s =>
        {
            var once = DisplayWidth.SanitizeTerminalText(s);
            Assert.Equal(once, DisplayWidth.SanitizeTerminalText(once));
            if (s.All(ch => ch != Esc && !char.IsControl(ch)))
                Assert.Equal(s, once);
        }, iter: Iter);

    // ===== H. Width classification =====

    [Fact]
    public void H1_WideRanges_UseTwoColumns() =>
        // Characters from wide ranges use 2 columns.
        Gen.OneOf(
            Gen.Int[0x4E00, 0x9FFF],   // CJK unified ideographs
            Gen.Int[0x3041, 0x3096],   // hiragana
            Gen.Int[0xAC00, 0xD7A3],   // Hangul syllables
            Gen.Int[0xFF21, 0xFF3A])   // fullwidth Latin capitals
        .Sample(cp => Assert.Equal(2, DisplayWidth.GetColumnCount(((char)cp).ToString())), iter: Iter);

    [Fact]
    public void H2_AsciiPrintable_UsesOneColumn() =>
        // A normal ASCII character uses 1 column.
        Gen.Int[0x20, 0x7E].Sample(cp => Assert.Equal(1, DisplayWidth.GetColumnCount(((char)cp).ToString())), iter: Iter);

    [Fact]
    public void H3_ZeroWidthCategories_UseZeroColumns() =>
        // A combining mark, a format character, or a control character uses 0 columns.
        Gen.OneOf(
            Gen.Int[0x0300, 0x036F],                        // combining marks
            Gen.OneOfConst(0x200B, 0x200D, 0x2060, 0xFEFF), // format characters
            Gen.Int[0x0000, 0x001F])                        // C0 control characters
        .Sample(cp => Assert.Equal(0, DisplayWidth.GetColumnCount(((char)cp).ToString())), iter: Iter);

    [Fact]
    public void H4_EmojiSequences_UseTwoColumns() =>
        // An emoji, an emoji with a style selector, and a keycap all use 2 columns.
        Gen.OneOfConst(Cp(0x1F600), Cp(0x1F389), Cp(0x2600, 0xFE0F), Cp(0x270B, 0xFE0F), Cp(0x31, 0xFE0F, 0x20E3), Cp(0x20000))
        .Sample(e => Assert.Equal(2, DisplayWidth.GetColumnCount(e)), iter: Iter);

    // ===== I. Totality =====

    [Fact]
    public void I1_NoMethodThrows_OnAnyInput() =>
        // No method throws for any text and any int argument.
        (from s in AnyText from a in IndexArg from b in WidthArg select (s, a, b)).Sample(t =>
        {
            var (s, a, b) = t;
            _ = DisplayWidth.GetColumnCount(s);
            _ = DisplayWidth.EnumerateCells(s).ToList();
            _ = DisplayWidth.SanitizeTerminalText(s);
            _ = DisplayWidth.TruncateToColumns(s, b);
            _ = DisplayWidth.TruncateStartToColumns(s, b);
            _ = DisplayWidth.SliceByColumns(s, a, b);
            _ = DisplayWidth.GetStringIndexForColumnCount(s, b);
            _ = DisplayWidth.CursorPositionToColumn(s, a);
            _ = DisplayWidth.ClampToTextElementBoundary(s, a);
            _ = DisplayWidth.GetPreviousTextElementIndex(s, a);
            _ = DisplayWidth.GetNextTextElementIndex(s, a);
            _ = DisplayWidth.GetTextElementAt(s, a);
        }, iter: Iter);
}
