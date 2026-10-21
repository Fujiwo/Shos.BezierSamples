using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BezierWinForm;

/// <summary>
/// 通過点の列をベジエ曲線に変換して描く処理をまとめたもの。
/// </summary>
public static class GraphicHelper
{
    /// <summary>
    /// 通過点 points をすべて通り、最後の点から最初の点へ戻って 1 周する閉じた 3 次スプライン曲線を描く。
    /// </summary>
    public static void DrawBezierPolygon(Graphics graphics, Pen pen, IList<PointF> points)
    {
        // 2 点では曲線の向きが決められないので、2 点を結ぶ直線を描く。
        if (points.Count == 2) {
            graphics.DrawLines(pen, points.ToArray());
        }
        else if (points.Count >= 3) {
            // 閉じた曲線では、最後の点 → 最初の点を含む n 区間すべてがベジエ曲線になる。
            graphics.DrawBeziers(pen, points.ToClosedSplineBezier().ToArray());
        }
    }

    /// <summary>
    /// 通過点 points をすべて通る、開いた 3 次スプライン曲線を描く。
    /// </summary>
    /// <remarks>
    /// 最初の区間 (P0 → P1) と最後の区間 (P(n−2) → P(n−1)) も含め、n − 1 個の区間すべてをベジエ曲線で描く。
    /// 両端の向きは、自然スプラインの条件 (端点で曲がり具合 = 2 階微分が 0) から決まる。
    /// </remarks>
    public static void DrawBezierLine(Graphics graphics, Pen pen, IList<PointF> points)
    {
        if (points.Count < 2)
            return;

        // 2 点だけなら直線になる (ベジエ曲線にしても直線になるので、そのまま線を引く)。
        if (points.Count == 2) {
            graphics.DrawLine(pen, points[0], points[1]);
            return;
        }

        // DrawBeziers は [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の点列を受け取り、
        // 各区間の終点を次の区間の始点として、つながった曲線を描く。
        graphics.DrawBeziers(pen, points.ToSplineBezier().ToArray());

        //for (var index = 0; index < points.Length - 3; index++) {
        //    var bezierPoints = Geometry.ToBezier(points[index], points[index + 1], points[index + 2], points[index + 3]).ToArray();
        //    if (bezierPoints.Length >= 4)
        //        graphics.DrawBezier(pen, bezierPoints[0], bezierPoints[1], bezierPoints[2], bezierPoints[3]);
        //}
    }
}

/// <summary>
/// ベジエ曲線の 1 区間ぶんの計算。
/// </summary>
public static class Geometry
{
    /// <summary>
    /// 連続する 4 つの通過点 P0 (point1) 〜 P3 (point4) から、
    /// 真ん中の区間 P1 → P2 を描く 3 次ベジエ曲線の 4 点 [始点 P1, 制御点1, 制御点2, 終点 P2] を返す。
    /// </summary>
    /// <remarks>
    /// 点列全体をまとめて変換する <see cref="GeometryExtensions.ToBezierPolyline"/> の 1 区間ぶんと同じ計算。
    /// </remarks>
    public static IEnumerable<PointF> ToBezier(PointF point1, PointF point2, PointF point3, PointF point4)
    {
        // 制御点を通過点から離す割合 1/6 (= Catmull-Rom の接線の 1/2 × ベジエの制御点の 1/3)。
        const float a = 1.0f / 3.0f / 2.0f;

        yield return point2;
        // 制御点1 = P1 + (P2 − P0) / 6。P1 での向きを弦 P0 → P2 と平行にする。
        yield return point2.Add(point3.Subtract(point1).Multiply(a));
        // 制御点2 = P2 − (P3 − P1) / 6。P2 での向きを弦 P1 → P3 と平行にする。
        yield return point3.Add(point2.Subtract(point4).Multiply(a));
        yield return point3;
    }
}

/// <summary>
/// 点 (PointF) とベクトル (SizeF) の演算、および通過点の列をベジエ曲線の点列に変換する拡張メソッド。
/// </summary>
/// <remarks>
/// PointF を「位置」、SizeF を「2 点間の差 (ベクトル)」として使い分ける (各通過点での接線も SizeF で表す)。
/// 通過点の列の変換には、すべての点を通る 3 次スプライン曲線 (ToSplineBezier / ToClosedSplineBezier、描画に使う) と、
/// 比較用に残している Catmull-Rom 版 (ToBezierPolyline / ToBezierPolygon) がある。
/// どちらも、各通過点での接線 m から 3 次ベジエの制御点を P ± m / 3 として求める点は同じで、違うのは接線の求め方だけである。
/// </remarks>
public static class GeometryExtensions
{
    extension(PointF point1)
    {
        /// <summary>
        /// 2 点間の距離を返す。
        /// </summary>
        public float Distance(PointF point2)
            => point1.Subtract(point2).Absolute();

        /// <summary>
        /// point2 から point1 へ向かうベクトル (point1 − point2) を返す。
        /// </summary>
        public SizeF Subtract(PointF point2)
            => new SizeF(point1.X - point2.X, point1.Y - point2.Y);
    }

    extension(PointF point)
    {
        /// <summary>
        /// 位置 point をベクトル size だけずらした位置を返す。
        /// </summary>
        public PointF Add(SizeF size)
            => new PointF(point.X + size.Width, point.Y + size.Height);

        //public static PointF Subtract(this PointF point, SizeF size)
        //{ return new PointF(point.X - size.Width, point.Y - size.Height); }

        /// <summary>
        /// point を通過点の列 points の末尾に追加してよいかどうかを返す (点の間引きの判定)。
        /// </summary>
        /// <returns>
        /// points が空か、末尾の点から MinimumDistance 以上離れていれば true。
        /// 近すぎる点を除くことで、マウスの細かい揺れで曲線が波打つのを防ぐ。
        /// </returns>
        public bool IsValidForBezier(IList<PointF> points, float MinimumDistance)
            => points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance;
    }

    extension(SizeF size)
    {
        /// <summary>
        /// ベクトルを rate 倍したベクトルを返す。
        /// </summary>
        public SizeF Multiply(float rate)
            => new SizeF(size.Width * rate, size.Height * rate);

        /// <summary>
        /// ベクトルの長さを返す。
        /// </summary>
        public float Absolute()
            => (size.Width.Square() + size.Height.Square()).SquareRoot();
    }

    extension(float value)
    {
        /// <summary>
        /// 2 乗を返す。
        /// </summary>
        public float Square()
            => value * value;

        /// <summary>
        /// 平方根を返す。
        /// </summary>
        public float SquareRoot()
            => (float)Math.Sqrt(value);
    }

    extension(IList<PointF> points)
    {
        //public static IList<PointF> ForBezier(this IList<PointF> points, float minimumDistance)
        //{
        //    var newPoints = new List<PointF>();
        //    foreach (var point in points) {
        //        if (point.IsValidForBezier(newPoints, minimumDistance))
        //            newPoints.Add(point);
        //    }
        //    return newPoints;
        //}

        //public static IEnumerable<PointF> ToBezierPolyline(this IList<PointF> points, float minimumDistance)
        //{ return points.ForBezier(minimumDistance).ToBezierPolyline(); }

        //public static IEnumerable<PointF> ToBezierPolygon(this IList<PointF> points, float minimumDistance)
        //{ return points.ForBezier(minimumDistance).ToBezierPolygon(); }

        /// <summary>
        /// 通過点の列を、ベジエ描画用の点列 [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] に変換する。
        /// </summary>
        /// <remarks>
        /// 前後に隣の点がそろっている内側の区間 P1 → P2, P2 → P3, …, P(n−3) → P(n−2) (n − 3 区間) だけを変換する。
        /// 最初の区間 P0 → P1 と最後の区間 P(n−2) → P(n−1) は含まないので、描くなら呼び出し側で直線にする。
    /// 描画には 3 次スプライン曲線 (ToSplineBezier) を使っていて、この Catmull-Rom 版は比べるために残している。
        /// 通過点が 4 個未満のときはベジエにできる区間が無いので、通過点をそのまま返す。
        /// </remarks>
        /// <returns>Graphics.DrawBeziers にそのまま渡せる形式の点列。</returns>
        public IEnumerable<PointF> ToBezierPolyline()
        {
            if (points.Count < 4) {
                foreach (var point in points)
                    yield return point;
            } else { 
                // 制御点を通過点から離す割合 1/6。
                // Catmull-Rom スプラインの接線 (P[i+2] − P[i]) / 2 に、
                // 3 次ベジエで端点から接線の 1/3 の位置に制御点を置く規則を掛けて 1/2 × 1/3 = 1/6 になる。
                const float a = 1.0f / 3.0f / 2.0f;
                // 最初のベジエ区間の始点は P1 (P0 → P1 は直線で別に描く)。
                yield return points[1];
                // 区間 P[i+1] → P[i+2] ごとに、制御点 2 個と終点を返す。
                for (var index = 0; index < points.Count - 3; index++) {
                    // 制御点1 = P[i+1] + (P[i+2] − P[i]) / 6
                    // 始点 P[i+1] での向きを、両隣を結ぶ弦 P[i] → P[i+2] と平行にする。
                    yield return points[index + 1].Add(points[index + 2].Subtract(points[index + 0]).Multiply(a));
                    // 制御点2 = P[i+2] − (P[i+3] − P[i+1]) / 6 (= P[i+2] + (P[i+1] − P[i+3]) / 6)
                    // 終点 P[i+2] での向きを、両隣を結ぶ弦 P[i+1] → P[i+3] と平行にする。
                    // 隣の区間とは通過点での向きがそろうので、つなぎ目が滑らかになる。
                    yield return points[index + 2].Add(points[index + 1].Subtract(points[index + 3]).Multiply(a));
                    // 終点 P[i+2]。次の区間の始点も兼ねる。
                    yield return points[index + 2];
                }
            }
        }

        /// <summary>
        /// 通過点の列を、1 周して閉じた曲線を描くためのベジエ描画用の点列に変換する。
        /// </summary>
        /// <remarks>
        /// 末尾に先頭の 3 点 P0, P1, P2 を付け足して [P0, …, P(n−1), P0, P1, P2] とし、
        /// <see cref="ToBezierPolyline"/> で変換する。
        /// こうすると、ToBezierPolyline が直線として除く両端の区間 (P0 → P1 と P1 → P2) を除いても、
        /// P1 → P2 → … → P(n−1) → P0 → P1 の n 区間すべてが前後の点のそろったベジエ曲線になり、1 周つながる。
        /// 通過点が 3 個未満のときは閉じた曲線にできないので、通過点をそのまま返す。
        /// </remarks>
        public IEnumerable<PointF> ToBezierPolygon()
        {
            if (points.Count < 3)
                return points;
            var polygonPoints = new List<PointF>(points);
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
        /// 係数行列は三重対角なので、トーマス法で O(n) で解ける。誤差を減らすため、途中の計算は double で行う。
        /// 両隣の接線が自分と同じと仮定すると m(i) = (P(i+1) − P(i−1)) / 2 になり、Catmull-Rom (ToBezierPolyline) の接線に一致する。
        /// </remarks>
        public SizeF[] ToSplineTangents()
            => SolveSplineTangents(points);

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、すべての点を通る自然 3 次スプライン曲線のベジエ描画用の点列
        /// [P0, 制御点1, 制御点2, P1, 制御点1, 制御点2, P2, …, P(n−1)] に変換する。
        /// </summary>
        /// <remarks>
        /// 区間 P(i) → P(i+1) の制御点は、接線 m (ToSplineTangents) から 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 とする。
        /// 通過点が 2 個未満のときは、通過点をそのまま返す。
        /// </remarks>
        /// <returns>Graphics.DrawBeziers にそのまま渡せる形式の点列 (1 + 3 × (n − 1) 個)。</returns>
        public IEnumerable<PointF> ToSplineBezier()
            => points.Count < 2 ? points : ToHermiteBezier(points, points.ToSplineTangents(), isClosed: false);

        /// <summary>
        /// 通過点の列を環状とみなした、閉じた (周期) 3 次スプライン曲線の、各通過点での接線を返す。
        /// </summary>
        /// <remarks>
        /// 最後の点の次を最初の点とみなし、すべての i について m(i−1) + 4·m(i) + m(i+1) = 3·(P(i+1) − P(i−1)) (添字は n を法とする) を解く。
        /// 係数行列は左下と右上の角にも 1 が入る巡回三重対角になるので、Sherman-Morrison の公式で、
        /// 角を除いた三重対角の連立方程式 (トーマス法) に分けて解く。通過点が 3 個未満のときは開いた曲線の接線を返す。
        /// </remarks>
        public SizeF[] ToClosedSplineTangents()
            => points.Count < 3 ? points.ToSplineTangents() : SolveClosedSplineTangents(points);

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、すべての点を通り最後の点から最初の点へ戻って 1 周する、閉じた 3 次スプライン曲線のベジエ描画用の点列
        /// [P0, 制御点1, 制御点2, P1, …, P(n−1), 制御点1, 制御点2, P0] に変換する。
        /// </summary>
        /// <remarks>
        /// 制御点の求め方は ToSplineBezier と同じで、接線に ToClosedSplineTangents を使う。
        /// 通過点が 3 個未満のときは閉じた曲線にできないので、通過点をそのまま返す。
        /// </remarks>
        /// <returns>Graphics.DrawBeziers にそのまま渡せる形式の点列 (1 + 3 × n 個)。</returns>
        public IEnumerable<PointF> ToClosedSplineBezier()
            => points.Count < 3 ? points : ToHermiteBezier(points, points.ToClosedSplineTangents(), isClosed: true);
    }

    // 自然 3 次スプラインの各通過点での接線を、三重対角の連立方程式を解いて求める
    static SizeF[] SolveSplineTangents(IList<PointF> points)
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
            rightX[index]   = 3.0 * ((double)points[next].X - points[previous].X);
            rightY[index]   = 3.0 * ((double)points[next].Y - points[previous].Y);
        }
        // x 座標と y 座標は同じ係数行列なので、右辺だけを替えて 2 回解く
        return ToSizes(SolveTridiagonal(lower, diagonal, upper, rightX), SolveTridiagonal(lower, diagonal, upper, rightY));
    }

    // 閉じた曲線 (点が 3 個以上) の各通過点での接線を、巡回三重対角の連立方程式 A·m = r として解く。
    // A は対角 4、その左右 1 で、角 A[0, n−1] と A[n−1, 0] も 1 になる。
    // Sherman-Morrison の公式: A = T + u·vᵀ (u = (γ, 0, …, 0, 1)ᵀ、v = (1, 0, …, 0, 1/γ)ᵀ) と分けると、
    // T (角の無い三重対角) について T·w = r と T·z = u を解いて、m = w − (vᵀw / (1 + vᵀz))·z になる
    static SizeF[] SolveClosedSplineTangents(IList<PointF> points)
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
            var next        = (index + 1) % count;
            var previous    = (index + count - 1) % count;
            rightX[index]   = 3.0 * ((double)points[next].X - points[previous].X);
            rightY[index]   = 3.0 * ((double)points[next].Y - points[previous].Y);
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
        return ToSizes(x, y);
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

    // x 成分の列と y 成分の列を、ベクトル (SizeF) の列にまとめる
    static SizeF[] ToSizes(double[] x, double[] y)
    => [.. x.Zip(y, (width, height) => new SizeF((float)width, (float)height))];

    // 通過点と各通過点での接線から、ベジエ描画用の点列 [P0, 制御点1, 制御点2, P1, …] を作る。
    // 区間 P(i) → P(i+1) の制御点は 制御点1 = P(i) + m(i) / 3、制御点2 = P(i+1) − m(i+1) / 3 (3 次エルミート曲線をベジエで表したもの)。
    // isClosed が true なら、最後に P(n−1) → P0 の区間も加えて閉じる
    static IEnumerable<PointF> ToHermiteBezier(IList<PointF> points, IList<SizeF> tangents, bool isClosed)
    {
        const float a = 1.0f / 3.0f;

        var count        = points.Count;
        var segmentCount = isClosed ? count : count - 1;
        yield return points[0];
        for (var index = 0; index < segmentCount; index++) {
            var next = (index + 1) % count;
            yield return points[index].Add(tangents[index].Multiply( a));
            yield return points[next ].Add(tangents[next ].Multiply(-a));
            yield return points[next ];
        }
    }
}
