using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf;

/// <summary>
/// 通過点の列から、ベジエ曲線を描く WPF の <see cref="Path"/> / <see cref="PathGeometry"/> を作るヘルパー。
/// </summary>
public static class GraphicHelper
{
    /// <summary>
    /// 通過点 <paramref name="points"/> を順に通る開いた曲線の <see cref="Path"/> を作る。
    /// </summary>
    public static Path BezierPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        => new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPathGeometry(points) };

    /// <summary>
    /// 通過点 <paramref name="points"/> を順に通り、最後の点から最初の点へ戻って 1 周する閉じた曲線の <see cref="Path"/> を作る。
    /// </summary>
    public static Path BezierPolygonPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        => new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPolygonPathGeometry(points) };

    /// <summary>
    /// 通過点 <paramref name="points"/> をすべて通る、開いた 3 次スプライン曲線のジオメトリーを作る。
    /// 最初と最後の区間も含め、n − 1 個の区間すべてが 3 次ベジエ曲線になる。
    /// 点が 2 個なら 1 本の直線 (直線状のベジエ曲線) になる。
    /// </summary>
    public static PathGeometry BezierPathGeometry(IList<Point> points)
    {
        var pathGeometry = new PathGeometry();

        if (points.Count >= 2) {
            // bezierPoints は [points[0], 制御点1, 制御点2, points[1], 制御点1, 制御点2, points[2], …] の形
            var bezierPoints = points.ToSplineBezier().ToArray();
            pathGeometry.Figures.Add(ToPathFigure(bezierPoints, isClosed: false));
        }
        return pathGeometry;
    }

    /// <summary>
    /// 通過点 <paramref name="points"/> をすべて通って最初の点に戻る、閉じた 3 次スプライン曲線のジオメトリーを作る。
    /// 最後の点 → 最初の点を含む n 個の区間すべてが 3 次ベジエ曲線になり、最初の点でも滑らかにつながる。
    /// 点が 2 個のときは 1 本の直線になる。
    /// </summary>
    public static PathGeometry BezierPolygonPathGeometry(IList<Point> points)
    {
        var pathGeometry = new PathGeometry();

        // 2 点だけでは曲がる向きが決まらないので、直線にする
        if (points.Count == 2) {
            var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
            pathFigure.Segments.Add(new LineSegment(points[1], true));

            pathGeometry.Figures.Add(pathFigure);
        }
        else if (points.Count >= 3) {
            // ToClosedSplineBezier() の結果は points[0] から始まり、最後の区間で points[0] に戻る。
            // 最後のベジエ曲線の終点は始点と同じ位置なので、IsClosed = true で足される閉じ線は長さ 0 になる。
            // 閉じた図形として扱われるので、始点のつなぎ目も線の結合 (StrokeLineJoin) で描かれる
            var bezierPoints = points.ToClosedSplineBezier().ToArray();
            pathGeometry.Figures.Add(ToPathFigure(bezierPoints, isClosed: true));
        }
        return pathGeometry;
    }

    // [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の点列を、つながった BezierSegment の PathFigure にする。
    // bezierPoints[0] は始点なので、index 1 から [制御点1, 制御点2, 終点] の 3 個ずつ BezierSegment にする
    static PathFigure ToPathFigure(Point[] bezierPoints, bool isClosed)
    {
        var pathFigure = new PathFigure { StartPoint = bezierPoints[0], IsClosed = isClosed };
        for (var index = 1; index + 2 < bezierPoints.Length; index += 3)
            pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));
        return pathFigure;
    }

    //public static void DrawBezierPolygon(Graphics graphics, Pen pen, IList<PointF> points)
    //{
    //    if (points.Count == 2) {
    //        graphics.DrawLines(pen, points.ToArray());
    //    }
    //    else if (points.Count >= 3) {
    //        var bezierPoints = points.ToBezierPolygon().ToArray();
    //        if (bezierPoints.Length >= 4)
    //            graphics.DrawBeziers(pen, bezierPoints);
    //    }
    //}

    //public static void DrawBezierLine(Graphics graphics, Pen pen, IList<PointF> points)
    //{
    //    if (points.Count < 2)
    //        return;

    //    graphics.DrawLine(pen, points[0], points[1]);

    //    var bezierPoints = points.ToBezierPolyline().ToArray();
    //    if (bezierPoints.Length >= 4)
    //        graphics.DrawBeziers(pen, bezierPoints);

    //    //for (var index = 0; index < points.Length - 3; index++) {
    //    //    var bezierPoints = Geo.ToBezier(points[index], points[index + 1], points[index + 2], points[index + 3]).ToArray();
    //    //    if (bezierPoints.Length >= 4)
    //    //        graphics.DrawBezier(pen, bezierPoints[0], bezierPoints[1], bezierPoints[2], bezierPoints[3]);
    //    //}

    //    if (points.Count > 2)
    //        graphics.DrawLine(pen, points[points.Count - 2], points[points.Count - 1]);
    //}
}

/// <summary>
/// 幾何計算の補助関数。
/// </summary>
public static class Geo
{
    /// <summary>
    /// 4 つの通過点 P0〜P3 から、中央の区間 P1 → P2 を描く 3 次ベジエ曲線の
    /// [始点 P1, 制御点 C1, 制御点 C2, 終点 P2] を返す (<see cref="GeometryExtensions.ToBezierPolyline"/> の 1 区間分)。
    /// </summary>
    /// <param name="point1">P0: 区間の手前の点 (P1 での向きを決めるためだけに使う)。</param>
    /// <param name="point2">P1: 区間の始点。</param>
    /// <param name="point3">P2: 区間の終点。</param>
    /// <param name="point4">P3: 区間の先の点 (P2 での向きを決めるためだけに使う)。</param>
    public static IEnumerable<Point> ToBezier(Point point1, Point point2, Point point3, Point point4)
    {
        // Catmull-Rom スプラインの接線 (P2 − P0) / 2 と、3 次ベジエの「制御点は端点から接線の 1/3 の位置」を掛けて 1/2 × 1/3 = 1/6
        const double a = 1.0 / 6.0;

        yield return point2;
        // C1 = P1 + (P2 − P0) / 6: P1 での向きを、両隣を結ぶ弦 P0 → P2 と平行にする
        yield return point2.Add(point3.Subtract(point1).Multiply(a));
        // C2 = P2 − (P3 − P1) / 6: P2 での向きを、両隣を結ぶ弦 P1 → P3 と平行にする
        yield return point3.Add(point2.Subtract(point4).Multiply(a));
        yield return point3;
    }

    /// <summary>
    /// 2 つの値の平均を返す。
    /// </summary>
    public static double Average(double value1, double value2)
        => (value1 + value2) / 2.0;
}

/// <summary>
/// <see cref="Point"/> などに対するベクトル演算と、通過点の列をベジエ曲線用の点列に変換する拡張メソッド。
/// <see cref="Point"/> を位置ベクトルとして扱い、加減算・スカラー倍で計算する (接線のようなベクトルも <see cref="Point"/> で表す)。
/// </summary>
/// <remarks>
/// 通過点の列の変換には、すべての点を通る 3 次スプライン曲線 (<c>ToSplineBezier</c> / <c>ToClosedSplineBezier</c>、描画に使う) と、
/// 比較用に残している Catmull-Rom 版 (<c>ToBezierPolyline</c> / <c>ToBezierPolygon</c>) がある。
/// どちらも、各通過点での接線 m から 3 次ベジエの制御点を P ± m / 3 として求める点は同じで、違うのは接線の求め方だけである。
/// </remarks>
public static class GeometryExtensions
{
    extension(Rect rect)
    {
        /// <summary>
        /// 矩形の中心を返す。
        /// </summary>
        public Point Center()
            => new Point { X = Geo.Average(rect.Left, rect.Right), Y = Geo.Average(rect.Top, rect.Bottom) };
    }

    extension(Point point1)
    {
        /// <summary>
        /// 2 点間の距離を返す。
        /// </summary>
        public double Distance(Point point2)
            => point1.Subtract(point2).Absolute();

        /// <summary>
        /// <paramref name="point2"/> から <paramref name="point1"/> へ向かうベクトル (point1 − point2) を返す。
        /// </summary>
        public Point Subtract(Point point2)
            => new Point(point1.X - point2.X, point1.Y - point2.Y);
    }

    extension(Point point)
    {
        /// <summary>
        /// 2 つの点をベクトルとして足す。
        /// </summary>
        public Point Add(Point size)
            => new Point(point.X + size.X, point.Y + size.Y);

        /// <summary>
        /// ベクトルを <paramref name="rate"/> 倍する。
        /// </summary>
        public Point Multiply(double rate)
            => new Point(point.X * rate, point.Y * rate);

        /// <summary>
        /// ベクトルの長さを返す。
        /// </summary>
        public double Absolute()
            => (point.X.Square() + point.Y.Square()).SquareRoot();

        /// <summary>
        /// 点の間引き用の判定。<paramref name="point"/> が、通過点の列 <paramref name="points"/> の最後の点から
        /// <paramref name="MinimumDistance"/> 以上離れていれば (または列が空なら) true を返す。
        /// 近すぎる点まで通過点にすると、マウスの細かい揺れで曲線が波打つため。
        /// </summary>
        public bool IsValidForBezier(IList<Point> points, double MinimumDistance)
            => points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance;
    }

    extension(double value)
    {
        /// <summary>
        /// 2 乗を返す。
        /// </summary>
        public double Square()
            => value * value;

        /// <summary>
        /// 平方根を返す。
        /// </summary>
        public double SquareRoot()
            => Math.Sqrt(value);
    }

    extension(IList<Point> points)
    {
        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、3 次ベジエ曲線用の点列
        /// [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] に変換する。
        /// 前後に点がそろっている内側の n − 3 区間 (P1 → P2 〜 P(n−3) → P(n−2)) だけを変換するので、
        /// 結果は P1 から始まり P(n−2) で終わる。最初の区間 (P0 → P1) と最後の区間 (P(n−2) → P(n−1)) は
        /// 片側の点が無く向きが決まらないので含まない (描くなら呼び出し側で直線にする)。
        /// 描画には 3 次スプライン曲線 (<c>ToSplineBezier</c>) を使っていて、この Catmull-Rom 版は比べるために残している。
        /// </summary>
        /// <returns>点が 4 個以上なら 1 + 3 × (n − 3) 個の点。4 個未満ならベジエの区間が作れないので、通過点をそのまま返す。</returns>
        public IEnumerable<Point> ToBezierPolyline()
        {
            if (points.Count < 4) {
                foreach (var point in points)
                    yield return point;
            } else { 
                // 1/6 = Catmull-Rom の接線 (1/2) × ベジエの制御点の位置 (1/3)
                const double a = 1.0 / 6.0;
                yield return points[1];
                // 区間 P[i+1] → P[i+2] ごとに、制御点 2 つと終点を返す (始点は 1 つ前の区間の終点と共有する)
                for (var index = 0; index < points.Count - 3; index++) {
                    // C1 = P[i+1] + (P[i+2] − P[i]) / 6: 始点での向きを、両隣を結ぶ弦 P[i] → P[i+2] と平行にする
                    yield return points[index + 1].Add(points[index + 2].Subtract(points[index + 0]).Multiply(a));
                    // C2 = P[i+2] − (P[i+3] − P[i+1]) / 6: 終点での向きを、両隣を結ぶ弦 P[i+1] → P[i+3] と平行にする。
                    // 次の区間の C1 も同じ弦と平行になるので、通過点の前後で向きがそろい、つなぎ目が滑らかになる
                    yield return points[index + 2].Add(points[index + 1].Subtract(points[index + 3]).Multiply(a));
                    yield return points[index + 2];
                }
            }
        }

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、最後の点から最初の点へ戻って 1 周する閉じた曲線用の点列
        /// [始点, 制御点1, 制御点2, 終点, …] に変換する。結果は P1 から始まり、n 区間を経て P1 に戻る。
        /// </summary>
        /// <returns>点が 3 個以上なら 1 + 3 × n 個の点。3 個未満なら通過点をそのまま返す。</returns>
        public IEnumerable<Point> ToBezierPolygon()
        {
            if (points.Count < 3)
                return points;
            // 末尾に先頭の 3 点 (P0, P1, P2) を付け足して [P0, …, P(n−1), P0, P1, P2] とし、開いた曲線と同じ変換をする。
            // n + 3 個の点から n 区間 (P1 → P2, …, P(n−1) → P0, P0 → P1) ができ、
            // 開いた曲線では直線になる区間にも前後の点がそろうので、すべての区間がベジエ曲線になって 1 周つながる
            var polygonPoints = new List<Point>(points);
            for (var index = 0; index < 3; index++)
                polygonPoints.Add(points[index]);
            return polygonPoints.ToBezierPolyline();
        }

        /// <summary>
        /// すべての通過点を通る自然 3 次スプライン曲線の、各通過点での接線 (パラメーター t についての微分) を返す。
        /// </summary>
        /// <remarks>
        /// 各区間をパラメーター t (0〜1、どの区間も同じ幅) の 3 次式とし、つなぎ目で 1 階微分と 2 階微分が一致するという条件から、
        /// 内側の点では m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1)) が成り立つ。
        /// 両端には自然スプラインの条件 (端点で 2 階微分が 0) を足す: 2·m0 + m1 = 3·(P1 − P0)、m(n−2) + 2·m(n−1) = 3·(P(n−1) − P(n−2))。
        /// 係数行列は三重対角なので、トーマス法で O(n) で解ける。
        /// 両隣の接線が自分と同じと仮定すると m(i) = (P(i+1) − P(i−1)) / 2 になり、Catmull-Rom (<c>ToBezierPolyline</c>) の接線に一致する。
        /// </remarks>
        public Point[] ToSplineTangents()
            => SolveSplineTangents(points);

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、すべての点を通る自然 3 次スプライン曲線用の点列
        /// [P0, 制御点1, 制御点2, P1, 制御点1, 制御点2, P2, …, P(n−1)] に変換する。
        /// 区間 P(i) → P(i+1) の制御点は、接線 m (<c>ToSplineTangents</c>) から 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 とする。
        /// </summary>
        /// <returns>点が 2 個以上なら 1 + 3 × (n − 1) 個の点。2 個未満なら通過点をそのまま返す。</returns>
        public IEnumerable<Point> ToSplineBezier()
            => points.Count < 2 ? points : ToHermiteBezier(points, points.ToSplineTangents(), isClosed: false);

        /// <summary>
        /// 通過点の列を環状とみなした、閉じた (周期) 3 次スプライン曲線の、各通過点での接線を返す。
        /// </summary>
        /// <remarks>
        /// 最後の点の次を最初の点とみなし、すべての i について m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1)) (添字は n を法とする) を解く。
        /// 係数行列は左下と右上の角にも 1 が入る巡回三重対角になるので、Sherman-Morrison の公式で、
        /// 角を除いた三重対角の連立方程式 (トーマス法) に分けて解く。点が 3 個未満のときは開いた曲線の接線を返す。
        /// </remarks>
        public Point[] ToClosedSplineTangents()
            => points.Count < 3 ? points.ToSplineTangents() : SolveClosedSplineTangents(points);

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、すべての点を通り最後の点から最初の点へ戻って 1 周する、閉じた 3 次スプライン曲線用の点列
        /// [P0, 制御点1, 制御点2, P1, …, P(n−1), 制御点1, 制御点2, P0] に変換する。
        /// 制御点の求め方は <c>ToSplineBezier</c> と同じで、接線に <c>ToClosedSplineTangents</c> を使う。
        /// </summary>
        /// <returns>点が 3 個以上なら 1 + 3 × n 個の点。3 個未満なら通過点をそのまま返す。</returns>
        public IEnumerable<Point> ToClosedSplineBezier()
            => points.Count < 3 ? points : ToHermiteBezier(points, points.ToClosedSplineTangents(), isClosed: true);
    }

    // 自然 3 次スプラインの各通過点での接線を、三重対角の連立方程式を解いて求める
    static Point[] SolveSplineTangents(IList<Point> points)
    {
        var count    = points.Count;
        var lower    = new double[count];
        var diagonal = new double[count];
        var upper    = new double[count];
        var rightX   = new double[count];
        var rightY   = new double[count];
        for (var index = 0; index < count; index++) {
            // 内側の点: m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1))。
            // 両端 (自然スプラインの条件): 2·m0 + m1 = 3·(P1 − P0)、m(n−2) + 2·m(n−1) = 3·(P(n−1) − P(n−2))。
            // 端では、無い方の隣の点の代わりに自分自身を使うと、右辺が同じ式で書ける
            var previous    = Math.Max(index - 1, 0        );
            var next        = Math.Min(index + 1, count - 1);
            lower[index]    = index == 0                         ? 0.0 : 1.0;
            upper[index]    = index == count - 1                 ? 0.0 : 1.0;
            diagonal[index] = index == 0 || index == count - 1 ? 2.0 : 4.0;
            var right       = points[next].Subtract(points[previous]).Multiply(3.0);
            (rightX[index], rightY[index]) = (right.X, right.Y);
        }
        // x 座標と y 座標は同じ係数行列なので、右辺だけを替えて 2 回解く
        return ToPoints(SolveTridiagonal(lower, diagonal, upper, rightX), SolveTridiagonal(lower, diagonal, upper, rightY));
    }

    // 閉じた曲線 (点が 3 個以上) の各通過点での接線を、巡回三重対角の連立方程式 A·m = r として解く。
    // A は対角 4、その左右 1 で、角 A[0, n−1] と A[n−1, 0] も 1 になる。
    // Sherman-Morrison の公式: A = T + u·vᵀ (u = (γ, 0, …, 0, 1)ᵀ、v = (1, 0, …, 0, 1/γ)ᵀ) と分けると、
    // T (角の無い三重対角) について T·w = r と T·z = u を解いて、m = w − (vᵀw / (1 + vᵀz))·z になる
    static Point[] SolveClosedSplineTangents(IList<Point> points)
    {
        // γ を対角と同じ大きさ (符号は逆) にすると、T の両端の対角が 4 − γ = 8、4 − 1/γ = 4.25 になり、対角優位が保たれる
        const double gamma = -4.0;

        var count    = points.Count;
        var lower    = new double[count];
        var diagonal = new double[count];
        var upper    = new double[count];
        var rightX   = new double[count];
        var rightY   = new double[count];
        var u        = new double[count];
        for (var index = 0; index < count; index++) {
            lower[index]    = index == 0         ? 0.0 : 1.0;
            upper[index]    = index == count - 1 ? 0.0 : 1.0;
            diagonal[index] = 4.0;
            // 右辺 3·(P(i+1) − P(i−1))。添字は環状 (n を法とする) にたどる
            var right       = points[(index + 1) % count].Subtract(points[(index + count - 1) % count]).Multiply(3.0);
            (rightX[index], rightY[index]) = (right.X, right.Y);
        }
        // A から u·vᵀ を引いた残りが T。u·vᵀ は (0, 0) に γ、(n−1, n−1) に 1/γ、二つの角に 1 を持つ
        diagonal[0]         -= gamma;
        diagonal[count - 1] -= 1.0 / gamma;
        u[0]                 = gamma;
        u[count - 1]         = 1.0;

        var x = SolveTridiagonal(lower, diagonal, upper, rightX);
        var y = SolveTridiagonal(lower, diagonal, upper, rightY);
        var z = SolveTridiagonal(lower, diagonal, upper, u);
        // vᵀz と vᵀw (w は x 座標・y 座標それぞれの解) から、z をどれだけ引けばよいかを求める
        var denominator = 1.0 + z[0] + z[count - 1] / gamma;
        var factorX     = (x[0] + x[count - 1] / gamma) / denominator;
        var factorY     = (y[0] + y[count - 1] / gamma) / denominator;
        for (var index = 0; index < count; index++) {
            x[index] -= factorX * z[index];
            y[index] -= factorY * z[index];
        }
        return ToPoints(x, y);
    }

    // 三重対角の連立方程式 lower[i]·x(i−1) + diagonal[i]·x(i) + upper[i]·x(i+1) = right[i] を、
    // トーマス法 (前進消去と後退代入) で解く。lower[0] と upper[n−1] は使わない。
    // 対角成分が優位な行列を前提にしているので、ピボット選択はしない
    static double[] SolveTridiagonal(double[] lower, double[] diagonal, double[] upper, double[] right)
    {
        var count = diagonal.Length;
        var x     = new double[count];
        if (count == 0)
            return x;
        var reducedUpper = new double[count];   // 前進消去後の upper (対角を 1 に正規化したもの)
        var reducedRight = new double[count];   // 前進消去後の right
        // 前進消去: 各行から 1 つ上の行の lower 倍を引き、左側の係数を消していく
        for (var index = 0; index < count; index++) {
            var previousUpper   = index == 0 ? 0.0 : reducedUpper[index - 1];
            var previousRight   = index == 0 ? 0.0 : reducedRight[index - 1];
            var denominator     = diagonal[index] - lower[index] * previousUpper;
            reducedUpper[index] = upper[index] / denominator;
            reducedRight[index] = (right[index] - lower[index] * previousRight) / denominator;
        }
        // 後退代入: 最後の行から順に、1 つ下の行の解を使って求める
        x[count - 1] = reducedRight[count - 1];
        for (var index = count - 2; index >= 0; index--)
            x[index] = reducedRight[index] - reducedUpper[index] * x[index + 1];
        return x;
    }

    // x 座標の列と y 座標の列を、点 (ベクトル) の列にまとめる
    static Point[] ToPoints(double[] x, double[] y)
        => [.. x.Zip(y, (pointX, pointY) => new Point(pointX, pointY))];

    // 通過点と各通過点での接線から、ベジエ曲線用の点列 [P0, 制御点1, 制御点2, P1, …] を作る。
    // 区間 P(i) → P(i+1) の制御点は 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 (3 次エルミート曲線をベジエで表したもの)。
    // isClosed が true なら、最後に P(n−1) → P0 の区間も加えて閉じる
    static IEnumerable<Point> ToHermiteBezier(IList<Point> points, IList<Point> tangents, bool isClosed)
    {
        const double a = 1.0 / 3.0;

        var count        = points.Count;
        var segmentCount = isClosed ? count : count - 1;
        yield return points[0];
        for (var index = 0; index < segmentCount; index++) {
            var next = (index + 1) % count;
            yield return points[index].Add     (tangents[index].Multiply(a));
            yield return points[next ].Subtract(tangents[next ].Multiply(a));
            yield return points[next ];
        }
    }
}
