using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfCad2Lib.Wpf;
using Xunit;

namespace BezierSamples.Tests;

/// <summary>
/// WpfCad2Lib.Wpf の 3 次スプライン曲線 (<see cref="GeoExtensions"/> / <see cref="GeoHelper"/> / <see cref="CurveFigure"/>) のテスト。
/// </summary>
public class WpfCad2LibSplineTests
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

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    public void OpenSpline_HasZeroSecondDerivativeAtBothEnds(int count)
    {
        // 自然スプラインの条件
        var points = BezierAssert.SamplePoints(count);
        var bezier = points.ToSplineBezier().ToArray();
        BezierAssert.Equal(new Vector(), BezierAssert.StartSecondDerivative(bezier, 0));
        BezierAssert.Equal(new Vector(), BezierAssert.EndSecondDerivative  (bezier, count - 2));
    }

    [Fact]
    public void OpenSpline_OfTwoPoints_IsStraightLine()
    {
        Point[] points = [new(10, 20), new(70, 50)];
        var bezier = points.ToSplineBezier().ToArray();
        // 制御点が線分を 3 等分する位置にあれば、ベジエ曲線は直線になる
        BezierAssert.Equal(new Point(30, 30), bezier[1]);
        BezierAssert.Equal(new Point(50, 40), bezier[2]);
    }

    [Fact]
    public void OpenSpline_OfEquallySpacedCollinearPoints_HasEqualTangents()
    {
        // 等間隔に一直線に並んだ点では、どの点でも接線は隣の点への差に等しい
        Point[] points = [new(0, 0), new(10, 5), new(20, 10), new(30, 15), new(40, 20)];
        foreach (var tangent in points.ToSplineTangents())
            BezierAssert.Equal(new Vector(10, 5), tangent);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void OpenSpline_OfFewerThanTwoPoints_ReturnsInput(int count)
    {
        var points = BezierAssert.SamplePoints(count);
        Assert.Equal(points, points.ToSplineBezier());
        Assert.Equal(count, points.ToSplineTangents().Length);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(200)]
    public void ClosedSpline_PassesThroughPointsAndIsC2AllAround(int count)
    {
        var points = BezierAssert.SamplePoints(count);
        BezierAssert.IsC2Spline(points, points.ToClosedSplineBezier().ToArray(), isClosed: true);
    }

    [Fact]
    public void ClosedSpline_OfRegularPolygon_HasSymmetricTangents()
    {
        // 正多角形の頂点では、どの頂点の接線も同じ長さで、半径に垂直になる
        const int count  = 8;
        var       points = Enumerable.Range(0, count).Select(index => new Point(100 * Math.Cos(2 * Math.PI * index / count), 100 * Math.Sin(2 * Math.PI * index / count))).ToList();
        var       tangents = points.ToClosedSplineTangents();
        for (var index = 0; index < count; index++) {
            Assert.Equal(tangents[0].Length, tangents[index].Length, 9);
            Assert.Equal(0.0, Vector.Multiply(tangents[index], (Vector)points[index]), 9);
        }
    }

    [Fact]
    public void PartialTangents_WithExactStartTangent_EqualFullSolution()
    {
        // 始点の接線に全体の解の値を与えれば、それ以降の連立方程式は全体と同じなので、解も一致する
        var points = BezierAssert.SamplePoints(30);
        var full   = points.ToSplineTangents();
        const int start = 20;
        var partial = points.ToSplineTangents(start, full[start]);
        Assert.Equal(points.Count - start, partial.Length);
        for (var index = 0; index < partial.Length; index++)
            BezierAssert.Equal(full[start + index], partial[index]);
    }

    [Fact]
    public void PartialTangents_AfterAddingPoint_AreCloseToFullSolution()
    {
        // ドラッグ中の近似: 点を足したとき、6 区間より前の接線は前回の値のまま固定して解き直す。
        // 固定した接線の誤差は約 0.27 倍ずつ減るので、全体を解き直した結果との差はごく小さい
        var points   = BezierAssert.SamplePoints(40);
        var previous = points.Take(39).ToList().ToSplineTangents();
        const int start = 39 - 6;
        var partial  = points.ToSplineTangents(start, previous[start]);
        var full     = points.ToSplineTangents();
        for (var index = 0; index < partial.Length; index++)
            Assert.True((full[start + index] - partial[index]).Length < 0.05, $"index {start + index}: {(full[start + index] - partial[index]).Length}");
    }

    [Fact]
    public void BezierPathGeometry_ConsistsOfBezierSegmentsThroughAllPoints()
    {
        var points   = BezierAssert.SamplePoints(6);
        var geometry = GeoHelper.BezierPathGeometry(points);
        var figure   = Assert.Single(geometry.Figures);
        Assert.False(figure.IsClosed);
        BezierAssert.Equal(points[0], figure.StartPoint);
        Assert.Equal(points.Count - 1, figure.Segments.Count);
        for (var index = 0; index < figure.Segments.Count; index++)
            BezierAssert.Equal(points[index + 1], Assert.IsType<BezierSegment>(figure.Segments[index]).Point3);
    }

    [Fact]
    public void BezierPolygonPathGeometry_IsClosedAndReturnsToFirstPoint()
    {
        var points   = BezierAssert.SamplePoints(5);
        var figure   = Assert.Single(GeoHelper.BezierPolygonPathGeometry(points).Figures);
        Assert.True(figure.IsClosed);
        Assert.Equal(points.Count, figure.Segments.Count);
        BezierAssert.Equal(points[0], Assert.IsType<BezierSegment>(figure.Segments[^1]).Point3);
    }

    [Fact]
    public void CurveFigure_DuringAndAfterDrag_MatchesSplineThroughPoints()
        => StaThread.Run(() => {
            // ドラッグを再現する: 最初の点で AddElements (SetFirst)、以降は SetNext、最後に SetLast
            var points  = BezierAssert.SamplePoints(30);
            var canvas  = new Canvas();
            var figure  = new CurveFigure();
            figure.AddElements(canvas.Children, points[0]);
            foreach (var point in points.Skip(1).SkipLast(1))
                figure.SetNext(point);

            // ドラッグ中 (末尾だけ解き直す近似) でも、正確な曲線とのずれはピクセル未満
            var path       = Assert.IsType<Path>(Assert.Single(canvas.Children.OfType<Path>()));
            var dragging   = SegmentsOf(path);
            var exactSoFar = points.SkipLast(1).ToList().ToSplineBezier().ToArray();
            AssertBezierSegments(exactSoFar, dragging, tolerance: 0.5);

            // ドラッグを終えると、全体を正確に作り直す
            figure.SetLast(points[^1]);
            AssertBezierSegments(points.ToSplineBezier().ToArray(), SegmentsOf(path), BezierAssert.Tolerance);
        });

    static BezierSegment[] SegmentsOf(Path path)
        => [.. Assert.Single(Assert.IsType<PathGeometry>(path.Data).Figures).Segments.Cast<BezierSegment>()];

    static void AssertBezierSegments(Point[] expected, BezierSegment[] actual, double tolerance)
    {
        Assert.Equal((expected.Length - 1) / 3, actual.Length);
        for (var index = 0; index < actual.Length; index++) {
            BezierAssert.Equal(expected[3 * index + 1], actual[index].Point1, tolerance);
            BezierAssert.Equal(expected[3 * index + 2], actual[index].Point2, tolerance);
            BezierAssert.Equal(expected[3 * index + 3], actual[index].Point3, tolerance);
        }
    }
}
