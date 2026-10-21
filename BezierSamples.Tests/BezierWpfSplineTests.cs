using System.Windows;
using System.Windows.Media;
using BezierWpf;
using Xunit;

namespace BezierSamples.Tests;

/// <summary>
/// BezierWpf の 3 次スプライン曲線 (<see cref="GeometryExtensions"/> / <see cref="GraphicHelper"/>) のテスト。
/// </summary>
public class BezierWpfSplineTests
{
    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(200)]
    public void OpenSpline_PassesThroughPointsAndIsC2(int count)
    {
        var points = BezierAssert.SamplePoints(count);
        BezierAssert.IsC2Spline(points, points.ToSplineBezier().ToArray(), isClosed: false);
    }

    [Fact]
    public void OpenSpline_HasZeroSecondDerivativeAtBothEnds()
    {
        var points = BezierAssert.SamplePoints(10);
        var bezier = points.ToSplineBezier().ToArray();
        BezierAssert.Equal(new Vector(), BezierAssert.StartSecondDerivative(bezier, 0));
        BezierAssert.Equal(new Vector(), BezierAssert.EndSecondDerivative  (bezier, 8));
    }

    [Fact]
    public void OpenSpline_OfTwoPoints_IsStraightLine()
    {
        Point[] points = [new(10, 20), new(70, 50)];
        var bezier = points.ToSplineBezier().ToArray();
        BezierAssert.Equal(new Point(30, 30), bezier[1]);
        BezierAssert.Equal(new Point(50, 40), bezier[2]);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(200)]
    public void ClosedSpline_PassesThroughPointsAndIsC2AllAround(int count)
    {
        var points = BezierAssert.SamplePoints(count);
        BezierAssert.IsC2Spline(points, points.ToClosedSplineBezier().ToArray(), isClosed: true);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Spline_MatchesWpfCad2Lib(bool isClosed)
    {
        // 同じアルゴリズムを別々に実装しているので、同じ点列からは同じ制御点になるはず
        var points   = BezierAssert.SamplePoints(25);
        var expected = isClosed ? WpfCad2Lib.Wpf.GeoExtensions.ToClosedSplineBezier(points).ToArray() : WpfCad2Lib.Wpf.GeoExtensions.ToSplineBezier(points).ToArray();
        var actual   = isClosed ? points.ToClosedSplineBezier().ToArray()                               : points.ToSplineBezier().ToArray();
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < actual.Length; index++)
            BezierAssert.Equal(expected[index], actual[index]);
    }

    [Fact]
    public void BezierPathGeometry_ConsistsOfBezierSegmentsThroughAllPoints()
    {
        var points = BezierAssert.SamplePoints(6);
        var figure = Assert.Single(GraphicHelper.BezierPathGeometry(points).Figures);
        Assert.False(figure.IsClosed);
        BezierAssert.Equal(points[0], figure.StartPoint);
        Assert.Equal(points.Count - 1, figure.Segments.Count);
        for (var index = 0; index < figure.Segments.Count; index++)
            BezierAssert.Equal(points[index + 1], Assert.IsType<BezierSegment>(figure.Segments[index]).Point3);
    }

    [Fact]
    public void BezierPolygonPathGeometry_IsClosedAndReturnsToFirstPoint()
    {
        var points = BezierAssert.SamplePoints(5);
        var figure = Assert.Single(GraphicHelper.BezierPolygonPathGeometry(points).Figures);
        Assert.True(figure.IsClosed);
        Assert.Equal(points.Count, figure.Segments.Count);
        BezierAssert.Equal(points[0], Assert.IsType<BezierSegment>(figure.Segments[^1]).Point3);
    }
}
