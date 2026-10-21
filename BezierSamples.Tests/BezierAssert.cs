using System.Windows;
using Xunit;

namespace BezierSamples.Tests;

/// <summary>
/// ベジエ点列 [始点, C1, C2, 終点, C1, C2, 終点, …] が満たすべき性質を調べるための関数。
/// </summary>
/// <remarks>
/// 3 次ベジエ曲線 B0, B1, B2, B3 の端での微分は、次のとおり。
/// 1 階微分: C′(0) = 3·(B1 − B0)、C′(1) = 3·(B3 − B2)。
/// 2 階微分: C″(0) = 6·(B0 − 2·B1 + B2)、C″(1) = 6·(B1 − 2·B2 + B3)。
/// </remarks>
static class BezierAssert
{
    /// <summary>座標の比較に使う許容誤差。</summary>
    public const double Tolerance = 1e-9;

    /// <summary>区間 <paramref name="segment"/> の始点での 1 階微分。</summary>
    public static Vector StartDerivative(Point[] bezier, int segment)
        => 3.0 * (bezier[3 * segment + 1] - bezier[3 * segment]);

    /// <summary>区間 <paramref name="segment"/> の終点での 1 階微分。</summary>
    public static Vector EndDerivative(Point[] bezier, int segment)
        => 3.0 * (bezier[3 * segment + 3] - bezier[3 * segment + 2]);

    /// <summary>区間 <paramref name="segment"/> の始点での 2 階微分。</summary>
    public static Vector StartSecondDerivative(Point[] bezier, int segment)
        => 6.0 * ((bezier[3 * segment] - bezier[3 * segment + 1]) + (bezier[3 * segment + 2] - bezier[3 * segment + 1]));

    /// <summary>区間 <paramref name="segment"/> の終点での 2 階微分。</summary>
    public static Vector EndSecondDerivative(Point[] bezier, int segment)
        => 6.0 * ((bezier[3 * segment + 1] - bezier[3 * segment + 2]) + (bezier[3 * segment + 3] - bezier[3 * segment + 2]));

    /// <summary>2 つの点が許容誤差の範囲で一致することを確かめる。</summary>
    public static void Equal(Point expected, Point actual, double tolerance = Tolerance)
        => Assert.True((expected - actual).Length <= tolerance, $"expected {expected}, actual {actual}");

    /// <summary>2 つのベクトルが許容誤差の範囲で一致することを確かめる。</summary>
    public static void Equal(Vector expected, Vector actual, double tolerance = Tolerance)
        => Assert.True((expected - actual).Length <= tolerance, $"expected {expected}, actual {actual}");

    /// <summary>
    /// ベジエ点列が、通過点 <paramref name="points"/> をすべて順に通り、
    /// つなぎ目で 1 階微分と 2 階微分が一致する (C² 連続) ことを確かめる。
    /// </summary>
    public static void IsC2Spline(IList<Point> points, Point[] bezier, bool isClosed)
    {
        var segmentCount = isClosed ? points.Count : points.Count - 1;
        Assert.Equal(1 + 3 * segmentCount, bezier.Length);
        // 各区間の始点と終点が通過点になっている
        for (var segment = 0; segment <= segmentCount; segment++)
            Equal(points[segment % points.Count], bezier[3 * segment]);
        // つなぎ目 (区間 segment の終点 = 区間 segment + 1 の始点) で微分がそろう
        for (var segment = 0; segment < segmentCount - 1; segment++) {
            Equal(EndDerivative      (bezier, segment), StartDerivative      (bezier, segment + 1));
            Equal(EndSecondDerivative(bezier, segment), StartSecondDerivative(bezier, segment + 1));
        }
        if (isClosed) {
            // 閉じた曲線では、最後の区間の終点 (P0) と最初の区間の始点もつながる
            Equal(EndDerivative      (bezier, segmentCount - 1), StartDerivative      (bezier, 0));
            Equal(EndSecondDerivative(bezier, segmentCount - 1), StartSecondDerivative(bezier, 0));
        }
    }

    /// <summary>テストに使う、間隔と曲がり具合がばらばらな通過点の列 (隣り合う点は 8 より離れている)。</summary>
    public static List<Point> SamplePoints(int count)
    {
        var points = new List<Point>();
        for (var index = 0; index < count; index++)
            points.Add(new Point(30.0 * index + 7.0 * Math.Sin(index * 1.3), 120.0 + 80.0 * Math.Sin(index * 0.7) + 25.0 * Math.Cos(index * 2.1)));
        return points;
    }
}
