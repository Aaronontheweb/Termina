using Termina.Terminal;

namespace Termina.Tests.Terminal;

public class GradientTests
{
    [Fact]
    public void Create_EvenlyDistributed_SampleAtZero_ReturnsFirstColor()
    {
        var g = Gradient.Create(Color.FromRgb(0, 0, 0), Color.FromRgb(255, 255, 255));
        var result = g.Sample(0f);
        Assert.Equal(Color.FromRgb(0, 0, 0), result);
    }

    [Fact]
    public void Create_EvenlyDistributed_SampleAtOne_ReturnsLastColor()
    {
        var g = Gradient.Create(Color.FromRgb(0, 0, 0), Color.FromRgb(255, 255, 255));
        var result = g.Sample(1f);
        Assert.Equal(Color.FromRgb(255, 255, 255), result);
    }

    [Fact]
    public void Create_EvenlyDistributed_SampleAtMidpoint()
    {
        var g = Gradient.Create(Color.FromRgb(0, 0, 0), Color.FromRgb(200, 100, 50));
        var result = g.Sample(0.5f);
        Assert.Equal(Color.FromRgb(100, 50, 25), result);
    }

    [Fact]
    public void Create_ThreeStops_SampleAtMidpoint_ReturnsMiddleColor()
    {
        var green = Color.FromRgb(0, 255, 0);
        var yellow = Color.FromRgb(255, 255, 0);
        var red = Color.FromRgb(255, 0, 0);
        var g = Gradient.Create(green, yellow, red);
        var result = g.Sample(0.5f);
        Assert.Equal(yellow, result);
    }

    [Fact]
    public void Create_ThreeStops_SampleAtQuarter()
    {
        var green = Color.FromRgb(0, 255, 0);
        var yellow = Color.FromRgb(255, 255, 0);
        var red = Color.FromRgb(255, 0, 0);
        var g = Gradient.Create(green, yellow, red);
        var result = g.Sample(0.25f);
        Assert.Equal(Color.FromRgb(127, 255, 0), result);
    }

    [Fact]
    public void Create_CustomStops_SamplesCorrectly()
    {
        var g = Gradient.Create(
            (0f, Color.FromRgb(0, 0, 0)),
            (0.8f, Color.FromRgb(200, 200, 200)),
            (1f, Color.FromRgb(255, 0, 0)));
        Assert.Equal(Color.FromRgb(0, 0, 0), g.Sample(0f));
        Assert.Equal(Color.FromRgb(200, 200, 200), g.Sample(0.8f));
        Assert.Equal(Color.FromRgb(255, 0, 0), g.Sample(1f));
    }

    [Fact]
    public void Sample_ClampsToZero()
    {
        var g = Gradient.Create(Color.FromRgb(100, 100, 100), Color.FromRgb(200, 200, 200));
        var result = g.Sample(-1f);
        Assert.Equal(Color.FromRgb(100, 100, 100), result);
    }

    [Fact]
    public void Sample_ClampsToOne()
    {
        var g = Gradient.Create(Color.FromRgb(100, 100, 100), Color.FromRgb(200, 200, 200));
        var result = g.Sample(2f);
        Assert.Equal(Color.FromRgb(200, 200, 200), result);
    }

    [Fact]
    public void Create_SingleColor_Throws()
    {
        Assert.Throws<ArgumentException>(() => Gradient.Create(Color.FromRgb(0, 0, 0)));
    }

    [Fact]
    public void Create_Empty_Throws()
    {
        Assert.Throws<ArgumentException>(() => Gradient.Create());
    }
}
