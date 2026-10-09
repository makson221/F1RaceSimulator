using F1RaceSimulator.Core.Geometry;

namespace F1RaceSimulator.Tests;

/// <summary>Тести ламаної лінії (схема траси).</summary>
public class GeometryTests
{
    private static Polyline Square() => new(new[]
    {
        new Vec2(0, 0), new Vec2(10, 0), new Vec2(10, 10), new Vec2(0, 10)
    }, isClosed: true);

    [Fact]
    public void ClosedSquare_HasPerimeterLength()
    {
        Assert.Equal(40, Square().Length, precision: 9);
    }

    [Fact]
    public void PointAt_ReturnsStartAndQuarterPoints()
    {
        var square = Square();
        Assert.Equal(new Vec2(0, 0), square.PointAt(0));
        Assert.Equal(new Vec2(10, 0), square.PointAt(0.25));
        Assert.Equal(new Vec2(10, 10), square.PointAt(0.5));
    }

    [Fact]
    public void PointAt_WrapsAroundForClosedLine()
    {
        var square = Square();
        Assert.Equal(square.PointAt(0.25), square.PointAt(1.25));
        Assert.Equal(square.PointAt(0.75), square.PointAt(-0.25));
    }

    [Fact]
    public void PointAt_ClampsForOpenLine()
    {
        var line = new Polyline(new[] { new Vec2(0, 0), new Vec2(10, 0) }, isClosed: false);
        Assert.Equal(new Vec2(10, 0), line.PointAt(2));
        Assert.Equal(new Vec2(0, 0), line.PointAt(-1));
        Assert.Equal(new Vec2(5, 0), line.PointAt(0.5));
    }

    [Fact]
    public void DirectionAt_PointsAlongTheLine()
    {
        Vec2 dir = Square().DirectionAt(0.125);   // середина першої сторони
        Assert.Equal(1, dir.X, precision: 6);
        Assert.Equal(0, dir.Y, precision: 6);
    }

    [Fact]
    public void Polyline_RequiresAtLeastTwoPoints()
    {
        Assert.Throws<ArgumentException>(() => new Polyline(new[] { new Vec2(1, 1) }, isClosed: false));
    }

    [Theory]
    [InlineData(1.25, 0.25)]
    [InlineData(-0.25, 0.75)]
    [InlineData(3.0, 0.0)]
    public void Wrap_ReturnsFractionalPart(double value, double expected)
    {
        Assert.Equal(expected, Polyline.Wrap(value), precision: 9);
    }
}
