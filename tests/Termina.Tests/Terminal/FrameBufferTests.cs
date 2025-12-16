// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Termina.Terminal;

namespace Termina.Tests.Terminal;

/// <summary>
/// Tests for FrameBuffer - the 2D buffer used for diff-based rendering.
/// </summary>
public class FrameBufferTests
{
    [Fact]
    public void Constructor_InitializesWithCorrectDimensions()
    {
        var buffer = new FrameBuffer(80, 24);

        Assert.Equal(80, buffer.Width);
        Assert.Equal(24, buffer.Height);
    }

    [Fact]
    public void Constructor_InitializesAllCellsToEmpty()
    {
        var buffer = new FrameBuffer(10, 5);

        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                Assert.Equal(TerminalCell.Empty, buffer[x, y]);
            }
        }
    }

    [Fact]
    public void Indexer_SetsAndGetsCell()
    {
        var buffer = new FrameBuffer(10, 5);
        var cell = new TerminalCell('X', Color.Red, Color.Blue, TextDecoration.Bold);

        buffer[5, 2] = cell;

        Assert.Equal(cell, buffer[5, 2]);
    }

    [Fact]
    public void Indexer_ThrowsOnOutOfBounds()
    {
        var buffer = new FrameBuffer(10, 5);

        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[-1, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[10, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[0, -1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => buffer[0, 5]);
    }

    [Fact]
    public void GetSafe_ReturnsEmptyForOutOfBounds()
    {
        var buffer = new FrameBuffer(10, 5);
        buffer[0, 0] = TerminalCell.FromChar('X');

        Assert.Equal(TerminalCell.Empty, buffer.GetSafe(-1, 0));
        Assert.Equal(TerminalCell.Empty, buffer.GetSafe(10, 0));
        Assert.Equal(TerminalCell.Empty, buffer.GetSafe(0, -1));
        Assert.Equal(TerminalCell.Empty, buffer.GetSafe(0, 5));
        Assert.Equal(TerminalCell.FromChar('X'), buffer.GetSafe(0, 0));
    }

    [Fact]
    public void TrySet_ReturnsTrueForValidPosition()
    {
        var buffer = new FrameBuffer(10, 5);
        var cell = TerminalCell.FromChar('X');

        var result = buffer.TrySet(5, 2, cell);

        Assert.True(result);
        Assert.Equal(cell, buffer[5, 2]);
    }

    [Fact]
    public void TrySet_ReturnsFalseForOutOfBounds()
    {
        var buffer = new FrameBuffer(10, 5);
        var cell = TerminalCell.FromChar('X');

        Assert.False(buffer.TrySet(-1, 0, cell));
        Assert.False(buffer.TrySet(10, 0, cell));
        Assert.False(buffer.TrySet(0, -1, cell));
        Assert.False(buffer.TrySet(0, 5, cell));
    }

    [Fact]
    public void Clear_ResetsAllCellsToEmpty()
    {
        var buffer = new FrameBuffer(10, 5);

        // Set some cells
        buffer[0, 0] = TerminalCell.FromChar('A');
        buffer[5, 2] = TerminalCell.FromChar('B');
        buffer[9, 4] = TerminalCell.FromChar('C');

        // Clear
        buffer.Clear();

        // Verify all empty
        for (var y = 0; y < 5; y++)
        {
            for (var x = 0; x < 10; x++)
            {
                Assert.Equal(TerminalCell.Empty, buffer[x, y]);
            }
        }
    }

    [Fact]
    public void Fill_FillsRectangularRegion()
    {
        var buffer = new FrameBuffer(10, 5);
        var cell = TerminalCell.FromChar('#');

        buffer.Fill(2, 1, 3, 2, cell);

        // Check filled region
        Assert.Equal(cell, buffer[2, 1]);
        Assert.Equal(cell, buffer[3, 1]);
        Assert.Equal(cell, buffer[4, 1]);
        Assert.Equal(cell, buffer[2, 2]);
        Assert.Equal(cell, buffer[3, 2]);
        Assert.Equal(cell, buffer[4, 2]);

        // Check unfilled cells
        Assert.Equal(TerminalCell.Empty, buffer[1, 1]);
        Assert.Equal(TerminalCell.Empty, buffer[5, 1]);
        Assert.Equal(TerminalCell.Empty, buffer[2, 0]);
        Assert.Equal(TerminalCell.Empty, buffer[2, 3]);
    }

    [Fact]
    public void Fill_ClipsToBufferBounds()
    {
        var buffer = new FrameBuffer(10, 5);
        var cell = TerminalCell.FromChar('#');

        // Fill region that extends beyond bounds
        buffer.Fill(8, 3, 5, 5, cell);

        // Should only fill within bounds
        Assert.Equal(cell, buffer[8, 3]);
        Assert.Equal(cell, buffer[9, 3]);
        Assert.Equal(cell, buffer[8, 4]);
        Assert.Equal(cell, buffer[9, 4]);
    }

    [Fact]
    public void Resize_PreservesContentInOverlap()
    {
        var buffer = new FrameBuffer(10, 5);
        buffer[0, 0] = TerminalCell.FromChar('A');
        buffer[5, 2] = TerminalCell.FromChar('B');
        buffer[9, 4] = TerminalCell.FromChar('C');

        // Shrink to 8x3
        buffer.Resize(8, 3);

        Assert.Equal(8, buffer.Width);
        Assert.Equal(3, buffer.Height);
        Assert.Equal(TerminalCell.FromChar('A'), buffer[0, 0]);
        Assert.Equal(TerminalCell.FromChar('B'), buffer[5, 2]);
        // C was at (9,4) which is out of new bounds
    }

    [Fact]
    public void Resize_FillsNewAreaWithEmpty()
    {
        var buffer = new FrameBuffer(5, 3);
        buffer[0, 0] = TerminalCell.FromChar('A');

        // Grow to 10x5
        buffer.Resize(10, 5);

        Assert.Equal(10, buffer.Width);
        Assert.Equal(5, buffer.Height);
        Assert.Equal(TerminalCell.FromChar('A'), buffer[0, 0]);
        Assert.Equal(TerminalCell.Empty, buffer[5, 0]); // New column
        Assert.Equal(TerminalCell.Empty, buffer[0, 3]); // New row
        Assert.Equal(TerminalCell.Empty, buffer[9, 4]); // Corner
    }

    [Fact]
    public void Resize_NoOpForSameDimensions()
    {
        var buffer = new FrameBuffer(10, 5);
        buffer[5, 2] = TerminalCell.FromChar('X');

        buffer.Resize(10, 5);

        Assert.Equal(10, buffer.Width);
        Assert.Equal(5, buffer.Height);
        Assert.Equal(TerminalCell.FromChar('X'), buffer[5, 2]);
    }

    [Fact]
    public void CopyFrom_CopiesAllCells()
    {
        var source = new FrameBuffer(10, 5);
        source[0, 0] = TerminalCell.FromChar('A');
        source[5, 2] = new TerminalCell('B', Color.Red, Color.Blue, TextDecoration.Bold);
        source[9, 4] = TerminalCell.FromChar('C');

        var dest = new FrameBuffer(10, 5);
        dest.CopyFrom(source);

        Assert.Equal(source[0, 0], dest[0, 0]);
        Assert.Equal(source[5, 2], dest[5, 2]);
        Assert.Equal(source[9, 4], dest[9, 4]);
    }

    [Fact]
    public void CopyFrom_ThrowsOnDimensionMismatch()
    {
        var source = new FrameBuffer(10, 5);
        var dest = new FrameBuffer(8, 5);

        Assert.Throws<ArgumentException>(() => dest.CopyFrom(source));
    }

    [Fact]
    public void GetChangedCells_ReturnsEmptyForIdenticalBuffers()
    {
        var buffer1 = new FrameBuffer(10, 5);
        var buffer2 = new FrameBuffer(10, 5);

        // Both empty - no changes
        var changes = buffer1.GetChangedCells(buffer2).ToList();

        Assert.Empty(changes);
    }

    [Fact]
    public void GetChangedCells_ReturnsChangedPositions()
    {
        var buffer1 = new FrameBuffer(10, 5);
        buffer1[0, 0] = TerminalCell.FromChar('A');
        buffer1[5, 2] = TerminalCell.FromChar('B');

        var buffer2 = new FrameBuffer(10, 5);
        buffer2[0, 0] = TerminalCell.FromChar('X'); // Different
        buffer2[5, 2] = TerminalCell.FromChar('B'); // Same
        buffer2[9, 4] = TerminalCell.FromChar('C'); // New

        var changes = buffer1.GetChangedCells(buffer2).ToList();

        Assert.Contains((0, 0), changes);
        Assert.Contains((9, 4), changes);
        Assert.DoesNotContain((5, 2), changes);
    }

    [Fact]
    public void GetChangedCells_DetectsColorChanges()
    {
        var buffer1 = new FrameBuffer(10, 5);
        buffer1[0, 0] = new TerminalCell('A', Color.Red, Color.Default, TextDecoration.None);

        var buffer2 = new FrameBuffer(10, 5);
        buffer2[0, 0] = new TerminalCell('A', Color.Blue, Color.Default, TextDecoration.None);

        var changes = buffer1.GetChangedCells(buffer2).ToList();

        Assert.Single(changes);
        Assert.Equal((0, 0), changes[0]);
    }

    [Fact]
    public void GetChangedCells_DetectsDecorationChanges()
    {
        var buffer1 = new FrameBuffer(10, 5);
        buffer1[0, 0] = new TerminalCell('A', Color.Default, Color.Default, TextDecoration.None);

        var buffer2 = new FrameBuffer(10, 5);
        buffer2[0, 0] = new TerminalCell('A', Color.Default, Color.Default, TextDecoration.Bold);

        var changes = buffer1.GetChangedCells(buffer2).ToList();

        Assert.Single(changes);
    }

    [Fact]
    public void GetChangedRuns_GroupsConsecutiveChanges()
    {
        var buffer1 = new FrameBuffer(10, 3);
        buffer1[2, 0] = TerminalCell.FromChar('A');
        buffer1[3, 0] = TerminalCell.FromChar('B');
        buffer1[4, 0] = TerminalCell.FromChar('C');
        buffer1[7, 0] = TerminalCell.FromChar('X');

        var buffer2 = new FrameBuffer(10, 3);
        // Row 0 is all empty - so all 4 chars are different

        var runs = buffer1.GetChangedRuns(buffer2).ToList();

        // Should have 2 runs: one for ABC at x=2, one for X at x=7
        Assert.Equal(2, runs.Count);

        var firstRun = runs.First(r => r.StartX == 2);
        Assert.Equal(0, firstRun.Y);
        Assert.Equal(3, firstRun.Cells.Length);
        Assert.Equal('A', firstRun.Cells[0].Character);
        Assert.Equal('B', firstRun.Cells[1].Character);
        Assert.Equal('C', firstRun.Cells[2].Character);

        var secondRun = runs.First(r => r.StartX == 7);
        Assert.Equal(0, secondRun.Y);
        Assert.Single(secondRun.Cells);
        Assert.Equal('X', secondRun.Cells[0].Character);
    }

    [Fact]
    public void GetRowText_ReturnsCharactersAsString()
    {
        var buffer = new FrameBuffer(10, 3);
        buffer[0, 1] = TerminalCell.FromChar('H');
        buffer[1, 1] = TerminalCell.FromChar('e');
        buffer[2, 1] = TerminalCell.FromChar('l');
        buffer[3, 1] = TerminalCell.FromChar('l');
        buffer[4, 1] = TerminalCell.FromChar('o');

        var text = buffer.GetRowText(1);

        Assert.Equal("Hello     ", text); // Padded with spaces to width
    }

    [Fact]
    public void GetAllText_ReturnsAllRowsJoined()
    {
        var buffer = new FrameBuffer(5, 2);
        buffer[0, 0] = TerminalCell.FromChar('A');
        buffer[0, 1] = TerminalCell.FromChar('B');

        var text = buffer.GetAllText();
        var lines = text.Split(Environment.NewLine);

        Assert.Equal(2, lines.Length);
        Assert.Equal("A    ", lines[0]);
        Assert.Equal("B    ", lines[1]);
    }
}
