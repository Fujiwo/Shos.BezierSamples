using System.Drawing;
using BezierWinForm;
using Xunit;
using WpfPoint = System.Windows.Point;

namespace BezierSamples.Tests;

/// <summary>
/// BezierWinForm の 3 次スプライン曲線 (<see cref="GeometryExtensions"/>) のテスト。
/// </summary>
/// <remarks>
/// WinForms 版は座標を float (PointF) で持つので、double の版より誤差が大きい。許容誤差もそれに合わせて大きく取る。
/// </remarks>
public class BezierWinFormSplineTests
{
    // float の座標 (数百ピクセル程度) で生じる丸め誤差の許容範囲
    const double Tolerance = 1e-3;

    static List<PointF> SamplePoints(int count)
        => [.. BezierAssert.SamplePoints(count).Select(point => new PointF((float)point.X, (float)point.Y))];

    static WpfPoint[] ToWpf(IEnumerable<PointF> points)
        => [.. points.Select(point => new WpfPoint(point.X, point.Y))];

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(200)]
    public void Spline_MatchesWpfCad2Lib(int count)
    {
        // 同じアルゴリズムを別々に実装しているので、同じ点列からは (float の誤差の範囲で) 同じ制御点になるはず。
        // 比べる側にも float に丸めた通過点を渡す
        var points   = SamplePoints(count);
        var expected = WpfCad2Lib.Wpf.GeoExtensions.ToSplineBezier(ToWpf(points)).ToArray();
        var actual   = ToWpf(points.ToSplineBezier());
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < actual.Length; index++)
            BezierAssert.Equal(expected[index], actual[index], Tolerance);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(200)]
    public void ClosedSpline_MatchesWpfCad2Lib(int count)
    {
        var points   = SamplePoints(count);
        var expected = WpfCad2Lib.Wpf.GeoExtensions.ToClosedSplineBezier(ToWpf(points)).ToArray();
        var actual   = ToWpf(points.ToClosedSplineBezier());
        Assert.Equal(expected.Length, actual.Length);
        for (var index = 0; index < actual.Length; index++)
            BezierAssert.Equal(expected[index], actual[index], Tolerance);
    }

    [Fact]
    public void Spline_ReturnsFormatForDrawBeziers()
    {
        // Graphics.DrawBeziers は 1 + 3k 個の点を受け取る
        var points = SamplePoints(7);
        Assert.Equal(1 + 3 * 6, points.ToSplineBezier().Count());
        Assert.Equal(1 + 3 * 7, points.ToClosedSplineBezier().Count());
    }

    [Fact]
    public void DrawBezierLine_DrawsWithoutError()
    {
        // 点の数ごとに、実際に描いて例外が出ないことを確かめる (DrawBeziers は点の数が合わないと例外を投げる)
        using var bitmap   = new Bitmap(800, 400);
        using var graphics = Graphics.FromImage(bitmap);
        foreach (var count in new[] { 0, 1, 2, 3, 4, 50 }) {
            GraphicHelper.DrawBezierLine   (graphics, Pens.Black, SamplePoints(count));
            GraphicHelper.DrawBezierPolygon(graphics, Pens.Black, SamplePoints(count));
        }
    }
}
