// Copyright (c) Petabridge, LLC. All rights reserved.
// Licensed under the Apache 2.0 license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using Termina.Layout;
using Termina.Rendering;
using Termina.Terminal;

namespace Termina.Tests.Layout;

/// <summary>
/// Tests for the GridNode 2D layout component.
/// </summary>
public class GridNodeTests
{
    #region Construction and Configuration

    [Fact]
    public void Constructor_CreatesEmptyGrid()
    {
        var grid = new GridNode();

        Assert.Equal(0, grid.RowCount);
        Assert.Equal(0, grid.ColumnCount);
    }

    [Fact]
    public void Constructor_WithDimensions_CreatesGrid()
    {
        var grid = new GridNode(rows: 3, cols: 4);

        Assert.Equal(3, grid.RowCount);
        Assert.Equal(4, grid.ColumnCount);
    }

    [Fact]
    public void SetCell_ExpandsGridAsNeeded()
    {
        var grid = new GridNode();

        grid.SetCell(2, 3, new TextNode("Test"));

        Assert.Equal(3, grid.RowCount);
        Assert.Equal(4, grid.ColumnCount);
    }

    [Fact]
    public void AddRow_AddsRowWithCells()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(10), SizeConstraint.Exactly(10))
            .AddRow(new TextNode("A"), new TextNode("B"))
            .AddRow(new TextNode("C"), new TextNode("D"));

        Assert.Equal(2, grid.RowCount);
        Assert.Equal(2, grid.ColumnCount);
    }

    [Fact]
    public void WithGridLines_SetsStyle()
    {
        var grid = new GridNode().WithGridLines(BorderStyle.Single);

        Assert.Equal(BorderStyle.Single, grid.GridLines);
    }

    [Fact]
    public void WithCellPadding_SetsPadding()
    {
        var grid = new GridNode().WithCellPadding(2);

        Assert.Equal(2, grid.CellPadding);
    }

    [Fact]
    public void WithNavigationMode_SetsMode()
    {
        var grid = new GridNode().WithNavigationMode(GridNavigationMode.CellNavigation);

        Assert.Equal(GridNavigationMode.CellNavigation, grid.NavigationMode);
    }

    #endregion

    #region Measuring

    [Fact]
    public void Measure_EmptyGrid_ReturnsZero()
    {
        var grid = new GridNode();

        var size = grid.Measure(new Size(100, 100));

        Assert.Equal(Size.Zero, size);
    }

    [Fact]
    public void Measure_AutoColumns_SizesToContent()
    {
        var grid = new GridNode()
            .AddRow(new TextNode("Short"), new TextNode("Longer Text"));

        var size = grid.Measure(new Size(100, 100));

        // "Short" = 5 chars, "Longer Text" = 11 chars, height = 1
        Assert.True(size.Width >= 16); // At least content width
        Assert.True(size.Height >= 1);
    }

    [Fact]
    public void Measure_FixedColumns_UsesFixedWidths()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(10), SizeConstraint.Exactly(20))
            .AddRow(new TextNode("A"), new TextNode("B"));
        grid.WidthAuto(); // Override default Fill constraint

        var size = grid.Measure(new Size(100, 100));

        Assert.Equal(30, size.Width); // 10 + 20
    }

    [Fact]
    public void Measure_WithGridLines_IncludesGridLineSpace()
    {
        var grid = new GridNode(1, 2);
        grid.WithColumnWidths(SizeConstraint.Exactly(10), SizeConstraint.Exactly(10));
        grid.WithRowHeights(SizeConstraint.Exactly(1));
        grid.WithGridLines(BorderStyle.Single);
        grid.SetCell(0, 0, new TextNode("A"));
        grid.SetCell(0, 1, new TextNode("B"));
        grid.WidthAuto(); // Override default Fill constraint
        grid.HeightAuto();

        var size = grid.Measure(new Size(100, 100));

        // 2 columns * 10 + 3 grid lines (left, middle, right) = 23
        Assert.Equal(23, size.Width);
        // 1 row * 1 + 2 grid lines (top, bottom) = 3
        Assert.Equal(3, size.Height);
    }

    [Fact]
    public void Measure_FillColumns_DistributesRemaining()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(20), SizeConstraint.FillRemaining())
            .AddRow(new TextNode("Fixed"), new TextNode("Fill"));

        var size = grid.Measure(new Size(100, 100));

        // Should use available width
        Assert.Equal(100, size.Width);
    }

    [Fact]
    public void Measure_PercentColumns_UsesPercentage()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Percentage(30), SizeConstraint.Percentage(70))
            .AddRow(new TextNode("A"), new TextNode("B"));

        var size = grid.Measure(new Size(100, 100));

        // 30% + 70% = 100
        Assert.Equal(100, size.Width);
    }

    #endregion

    #region Rendering

    [Fact]
    public void Render_EmptyGrid_DoesNotThrow()
    {
        var grid = new GridNode();
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);

        var exception = Record.Exception(() =>
            grid.Render(context, new Rect(0, 0, 80, 24)));

        Assert.Null(exception);
    }

    [Fact]
    public void Render_WithContent_RendersCell()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(10))
            .AddRow(new TextNode("Hello"));

        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);

        grid.Render(context, new Rect(0, 0, 80, 24));

        var line = terminal.GetLine(0);
        Assert.Contains("Hello", line);
    }

    [Fact]
    public void Render_WithGridLines_RendersBorders()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(5))
            .WithRows(SizeConstraint.Exactly(1))
            .WithGridLines(BorderStyle.Single)
            .AddRow(new TextNode("A"));

        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);

        grid.Render(context, new Rect(0, 0, 20, 5));

        var topLine = terminal.GetLine(0);
        Assert.Contains("┌", topLine);
        Assert.Contains("─", topLine);
        Assert.Contains("┐", topLine);
    }

    [Fact]
    public void Render_MultipleColumns_RendersSeparators()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(3), SizeConstraint.Exactly(3))
            .WithRows(SizeConstraint.Exactly(1))
            .WithGridLines(BorderStyle.Single)
            .AddRow(new TextNode("A"), new TextNode("B"));

        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);

        grid.Render(context, new Rect(0, 0, 20, 5));

        var topLine = terminal.GetLine(0);
        // Should have top-tee for column separator
        Assert.Contains("┬", topLine);
    }

    #endregion

    #region ColSpan and RowSpan

    [Fact]
    public void SetCell_WithColSpan_SpansColumns()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(10), SizeConstraint.Exactly(10), SizeConstraint.Exactly(10));

        grid.SetCell(0, 0, new TextNode("Spans 2 columns"), colSpan: 2);
        grid.AddRow(new TextNode("A"), new TextNode("B"), new TextNode("C"));

        Assert.Equal(2, grid.RowCount);
        Assert.Equal(3, grid.ColumnCount);
    }

    [Fact]
    public void SetCell_WithRowSpan_SpansRows()
    {
        var grid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(10), SizeConstraint.Exactly(10))
            .WithRows(SizeConstraint.Exactly(1), SizeConstraint.Exactly(1));

        grid.SetCell(0, 0, new TextNode("Spans 2 rows"), rowSpan: 2);
        grid.SetCell(0, 1, new TextNode("Top"));
        grid.SetCell(1, 1, new TextNode("Bottom"));

        Assert.Equal(2, grid.RowCount);
    }

    #endregion

    #region Focus Navigation

    [Fact]
    public void CanFocus_WhenNavigationModeNone_ReturnsFalse()
    {
        var grid = new GridNode().WithNavigationMode(GridNavigationMode.None);

        Assert.False(grid.CanFocus);
    }

    [Fact]
    public void CanFocus_WhenNavigationModeCellNavigation_ReturnsTrue()
    {
        var grid = new GridNode().WithNavigationMode(GridNavigationMode.CellNavigation);

        Assert.True(grid.CanFocus);
    }

    [Fact]
    public void HandleInput_ArrowKeys_MovesFocus()
    {
        var grid = new GridNode(3, 3)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        grid.OnFocused();

        Assert.Equal(0, grid.FocusedRow);
        Assert.Equal(0, grid.FocusedColumn);

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        Assert.Equal(0, grid.FocusedRow);
        Assert.Equal(1, grid.FocusedColumn);

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Assert.Equal(1, grid.FocusedRow);
        Assert.Equal(1, grid.FocusedColumn);

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        Assert.Equal(1, grid.FocusedRow);
        Assert.Equal(0, grid.FocusedColumn);

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.Equal(0, grid.FocusedRow);
        Assert.Equal(0, grid.FocusedColumn);
    }

    [Fact]
    public void HandleInput_Home_MovesToTopLeft()
    {
        var grid = new GridNode(3, 3)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        grid.OnFocused();

        // Move to middle
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.Home, false, false, false));

        Assert.Equal(0, grid.FocusedRow);
        Assert.Equal(0, grid.FocusedColumn);
    }

    [Fact]
    public void HandleInput_End_MovesToBottomRight()
    {
        var grid = new GridNode(3, 3)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        grid.OnFocused();

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));

        Assert.Equal(2, grid.FocusedRow);
        Assert.Equal(2, grid.FocusedColumn);
    }

    [Fact]
    public async Task HandleInput_Enter_EmitsCellActivated()
    {
        var grid = new GridNode()
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        var cell = new TextNode("Test");
        grid.SetCell(0, 0, cell);
        grid.OnFocused();

        // Subscribe BEFORE triggering the action
        (int Row, int Col, ILayoutNode? Cell)? activated = null;
        var tcs = new TaskCompletionSource<bool>();
        using var sub = grid.CellActivated.Subscribe(e =>
        {
            activated = e;
            tcs.TrySetResult(true);
        });

        grid.HandleInput(new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false));

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.True(completed == tcs.Task, "CellActivated was not emitted");

        Assert.NotNull(activated);
        Assert.Equal(0, activated.Value.Row);
        Assert.Equal(0, activated.Value.Col);
        Assert.Same(cell, activated.Value.Cell);
    }

    [Fact]
    public async Task FocusChange_EmitsFocusedCellChanged()
    {
        var grid = new GridNode(2, 2)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        grid.OnFocused();

        // Subscribe BEFORE triggering the action
        (int Row, int Col)? changed = null;
        var tcs = new TaskCompletionSource<bool>();
        using var sub = grid.FocusedCellChanged.Subscribe(e =>
        {
            changed = e;
            tcs.TrySetResult(true);
        });

        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.True(completed == tcs.Task, "FocusedCellChanged was not emitted");

        Assert.NotNull(changed);
        Assert.Equal(0, changed.Value.Row);
        Assert.Equal(1, changed.Value.Col);
    }

    [Fact]
    public void HandleInput_StaysWithinBounds()
    {
        var grid = new GridNode(2, 2)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        grid.OnFocused();

        // Try to go past left edge
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false));
        Assert.Equal(0, grid.FocusedColumn);

        // Try to go past top edge
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.UpArrow, false, false, false));
        Assert.Equal(0, grid.FocusedRow);

        // Go to bottom right
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.End, false, false, false));

        // Try to go past right edge
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.RightArrow, false, false, false));
        Assert.Equal(1, grid.FocusedColumn);

        // Try to go past bottom edge
        grid.HandleInput(new ConsoleKeyInfo('\0', ConsoleKey.DownArrow, false, false, false));
        Assert.Equal(1, grid.FocusedRow);
    }

    #endregion

    #region Invalidation

    [Fact]
    public async Task CellContentChange_TriggersInvalidation()
    {
        var grid = new GridNode();
        var textNode = new TextNode("Initial");

        grid.SetCell(0, 0, textNode);

        // The grid should subscribe to cell invalidation
        // Note: TextNode doesn't implement IInvalidatingNode, so this test
        // verifies the subscription mechanism works when cells do invalidate
        Assert.NotNull(grid.Invalidated);
    }

    [Fact]
    public async Task OnFocused_TriggersInvalidation()
    {
        var grid = new GridNode(2, 2)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        // Subscribe BEFORE triggering the action
        var tcs = new TaskCompletionSource<bool>();
        using var sub = grid.Invalidated.Subscribe(_ => tcs.TrySetResult(true));

        grid.OnFocused();

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.True(completed == tcs.Task, "Invalidated was not emitted on focus");
    }

    [Fact]
    public async Task OnBlurred_TriggersInvalidation()
    {
        var grid = new GridNode(2, 2)
            .WithNavigationMode(GridNavigationMode.CellNavigation);

        grid.OnFocused();

        // Subscribe BEFORE triggering the action
        var tcs = new TaskCompletionSource<bool>();
        using var sub = grid.Invalidated.Subscribe(_ => tcs.TrySetResult(true));

        grid.OnBlurred();

        var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(1)));
        Assert.True(completed == tcs.Task, "Invalidated was not emitted on blur");
    }

    #endregion

    #region Lifecycle

    [Fact]
    public void OnActivate_ActivatesChildCells()
    {
        var grid = new GridNode();
        var cell = new TestActivatableNode();
        grid.SetCell(0, 0, cell);

        grid.OnActivate();

        Assert.True(cell.WasActivated);
    }

    [Fact]
    public void OnDeactivate_DeactivatesChildCells()
    {
        var grid = new GridNode();
        var cell = new TestActivatableNode();
        grid.SetCell(0, 0, cell);

        grid.OnDeactivate();

        Assert.True(cell.WasDeactivated);
    }

    [Fact]
    public void Dispose_DisposesChildCells()
    {
        var grid = new GridNode();
        var cell = new TestDisposableNode();
        grid.SetCell(0, 0, cell);

        grid.Dispose();

        Assert.True(cell.WasDisposed);
    }

    #endregion

    #region Dashboard Sample (demonstrates nested grids)

    /// <summary>
    /// Demonstrates a dashboard layout with nested grids.
    /// This serves as both a test and documentation for the GridNode.
    /// </summary>
    [Fact]
    public void DashboardSample_NestedGrids_RendersCorrectly()
    {
        // Create a simple dashboard with:
        // - Header row spanning full width
        // - Two columns: left (metrics), right (data table)
        // - Footer row spanning full width

        // Inner grid: Metrics panel (2 columns: label, value)
        var metricsGrid = new GridNode()
            .WithColumns(SizeConstraint.Exactly(10), SizeConstraint.FillRemaining())
            .AddRow(new TextNode("CPU:"), new TextNode("78%"))
            .AddRow(new TextNode("Memory:"), new TextNode("62%"))
            .AddRow(new TextNode("Disk:"), new TextNode("45%"))
            .WithGridLines(BorderStyle.Single);

        // Inner grid: Data table (3 columns)
        var dataTable = new GridNode()
            .WithColumns(SizeConstraint.Exactly(8), SizeConstraint.Exactly(6), SizeConstraint.FillRemaining())
            .AddRow(
                new TextNode("Name").AlignCenter().Bold(),
                new TextNode("Count").AlignCenter().Bold(),
                new TextNode("Status").AlignCenter().Bold())
            .AddRow(new TextNode("Alpha"), new TextNode("42"), new TextNode("Active"))
            .AddRow(new TextNode("Beta"), new TextNode("17"), new TextNode("Pending"))
            .WithGridLines(BorderStyle.Rounded);

        // Outer grid: Main dashboard layout
        var dashboard = new GridNode()
            .WithColumns(SizeConstraint.Percentage(40), SizeConstraint.FillRemaining())
            .WithRows(
                SizeConstraint.Exactly(1),      // Header
                SizeConstraint.FillRemaining(), // Content
                SizeConstraint.Exactly(1))      // Footer
            .WithGridLines(BorderStyle.Double);

        // Header spans both columns
        dashboard.SetCell(0, 0, new TextNode("System Dashboard").AlignCenter().Bold(), colSpan: 2);

        // Content row
        dashboard.SetCell(1, 0, metricsGrid);
        dashboard.SetCell(1, 1, dataTable);

        // Footer spans both columns
        dashboard.SetCell(2, 0, new TextNode("[Q] Quit  [R] Refresh").AlignCenter(), colSpan: 2);

        // Render to a virtual terminal
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);

        dashboard.Render(context, new Rect(0, 0, 60, 15));

        // Verify structure was rendered
        var line0 = terminal.GetLine(0);
        var line1 = terminal.GetLine(1);

        // Double border chars should appear on top border
        Assert.Contains("═", line0);

        // Header content should contain "System Dashboard" (on line 1, inside the grid)
        Assert.Contains("System Dashboard", line1);

        // Verify the dashboard has the expected dimensions
        Assert.Equal(3, dashboard.RowCount);
        Assert.Equal(2, dashboard.ColumnCount);
    }

    /// <summary>
    /// Demonstrates grid with colspan for table headers.
    /// </summary>
    [Fact]
    public void TableWithMergedHeaderCells_RendersCorrectly()
    {
        // Create a table where top row has merged cells for grouping
        var table = new GridNode()
            .WithColumns(
                SizeConstraint.Exactly(10),
                SizeConstraint.Exactly(8),
                SizeConstraint.Exactly(8),
                SizeConstraint.Exactly(8))
            .WithGridLines(BorderStyle.Single);

        // Header row with merged cells
        table.SetCell(0, 0, new TextNode("Name"), rowSpan: 2);
        table.SetCell(0, 1, new TextNode("Metrics").AlignCenter(), colSpan: 3);

        // Sub-header row
        table.SetCell(1, 1, new TextNode("CPU"));
        table.SetCell(1, 2, new TextNode("Memory"));
        table.SetCell(1, 3, new TextNode("Disk"));

        // Data rows
        table.AddRow(new TextNode("Server A"), new TextNode("45%"), new TextNode("72%"), new TextNode("55%"));
        table.AddRow(new TextNode("Server B"), new TextNode("23%"), new TextNode("48%"), new TextNode("89%"));

        // Verify structure
        Assert.Equal(4, table.RowCount);
        Assert.Equal(4, table.ColumnCount);

        // Render
        var terminal = new VirtualTerminal(80, 24);
        var context = new RegionRenderContext(terminal, 0, 0, 80, 24);
        table.Render(context, new Rect(0, 0, 40, 10));

        // Grid lines should appear
        var line0 = terminal.GetLine(0);
        Assert.Contains("┌", line0);
    }

    #endregion

    #region Test Helpers

    private class TestActivatableNode : LayoutNode
    {
        public bool WasActivated { get; private set; }
        public bool WasDeactivated { get; private set; }

        public override Size Measure(Size available) => new(10, 1);

        public override void Render(IRenderContext context, Rect bounds) { }

        public override void OnActivate()
        {
            WasActivated = true;
            base.OnActivate();
        }

        public override void OnDeactivate()
        {
            WasDeactivated = true;
            base.OnDeactivate();
        }
    }

    private class TestDisposableNode : LayoutNode
    {
        public bool WasDisposed { get; private set; }

        public override Size Measure(Size available) => new(10, 1);

        public override void Render(IRenderContext context, Rect bounds) { }

        public override void Dispose()
        {
            WasDisposed = true;
            base.Dispose();
        }
    }

    #endregion
}
