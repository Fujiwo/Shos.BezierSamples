using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BezierWinForm
{
    /// <summary>
    /// 通過点の列をベジエ曲線に変換して描く処理をまとめたもの。
    /// </summary>
    public static class GraphicHelper
    {
        /// <summary>
        /// 通過点 points をすべて通り、最後の点から最初の点へ戻って 1 周する閉じた曲線を描く。
        /// </summary>
        public static void DrawBezierPolygon(Graphics graphics, Pen pen, IList<PointF> points)
        {
            // 2 点では曲線の向きが決められないので、2 点を結ぶ直線を描く。
            if (points.Count == 2) {
                graphics.DrawLines(pen, points.ToArray());
            }
            else if (points.Count >= 3) {
                // 閉じた曲線ではすべての区間がベジエ曲線になるので、直線の区間は無い。
                var bezierPoints = points.ToBezierPolygon().ToArray();
                if (bezierPoints.Length >= 4)
                    graphics.DrawBeziers(pen, bezierPoints);
            }
        }

        /// <summary>
        /// 通過点 points をすべて通る、開いた滑らかな線を描く。
        /// </summary>
        /// <remarks>
        /// 最初の区間 (P0 → P1) と最後の区間 (P(n−2) → P(n−1)) は片側の隣の点が無く、
        /// 曲線の向きが決まらないので直線で描き、その間の区間をベジエ曲線で描く。
        /// </remarks>
        public static void DrawBezierLine(Graphics graphics, Pen pen, IList<PointF> points)
        {
            if (points.Count < 2)
                return;

            // 最初の区間 P0 → P1 を直線で描く。
            graphics.DrawLine(pen, points[0], points[1]);

            // 内側の区間 P1 → … → P(n−2) をベジエ曲線で描く。
            // DrawBeziers は [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の点列を受け取り、
            // 各区間の終点を次の区間の始点として、つながった曲線を描く。
            // 通過点が 3 個以下だとベジエにできる区間が無く、変換結果が 4 点未満になるので描かない。
            var bezierPoints = points.ToBezierPolyline().ToArray();
            if (bezierPoints.Length >= 4)
                graphics.DrawBeziers(pen, bezierPoints);

            //for (var index = 0; index < points.Length - 3; index++) {
            //    var bezierPoints = Geometry.ToBezier(points[index], points[index + 1], points[index + 2], points[index + 3]).ToArray();
            //    if (bezierPoints.Length >= 4)
            //        graphics.DrawBezier(pen, bezierPoints[0], bezierPoints[1], bezierPoints[2], bezierPoints[3]);
            //}

            // 最後の区間 P(n−2) → P(n−1) を直線で描く (通過点が 2 個なら最初の区間と同じなので描かない)。
            if (points.Count > 2)
                graphics.DrawLine(pen, points[points.Count - 2], points[points.Count - 1]);
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
    /// PointF を「位置」、SizeF を「2 点間の差 (ベクトル)」として使い分ける。
    /// </remarks>
    public static class GeometryExtensions
    {
        /// <summary>
        /// 2 点間の距離を返す。
        /// </summary>
        public static float Distance(this PointF point1, PointF point2)
        { return point1.Subtract(point2).Absolute(); }

        /// <summary>
        /// 位置 point をベクトル size だけずらした位置を返す。
        /// </summary>
        public static PointF Add(this PointF point, SizeF size)
        { return new PointF(point.X + size.Width, point.Y + size.Height); }

        //public static PointF Subtract(this PointF point, SizeF size)
        //{ return new PointF(point.X - size.Width, point.Y - size.Height); }

        /// <summary>
        /// point2 から point1 へ向かうベクトル (point1 − point2) を返す。
        /// </summary>
        public static SizeF Subtract(this PointF point1, PointF point2)
        { return new SizeF(point1.X - point2.X, point1.Y - point2.Y); }

        /// <summary>
        /// ベクトルを rate 倍したベクトルを返す。
        /// </summary>
        public static SizeF Multiply(this SizeF size, float rate)
        { return new SizeF(size.Width * rate, size.Height * rate);  }

        /// <summary>
        /// ベクトルの長さを返す。
        /// </summary>
        public static float Absolute(this SizeF size)
        { return (size.Width.Square() + size.Height.Square()).SquareRoot(); }

        /// <summary>
        /// 2 乗を返す。
        /// </summary>
        public static float Square(this float value)
        { return value * value; }

        /// <summary>
        /// 平方根を返す。
        /// </summary>
        public static float SquareRoot(this float value)
        { return (float)Math.Sqrt(value); }

        /// <summary>
        /// point を通過点の列 points の末尾に追加してよいかどうかを返す (点の間引きの判定)。
        /// </summary>
        /// <returns>
        /// points が空か、末尾の点から MinimumDistance 以上離れていれば true。
        /// 近すぎる点を除くことで、マウスの細かい揺れで曲線が波打つのを防ぐ。
        /// </returns>
        public static bool IsValidForBezier(this PointF point, IList<PointF> points, float MinimumDistance)
        { return points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance; }

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
        /// 最初の区間 P0 → P1 と最後の区間 P(n−2) → P(n−1) は含まないので、呼び出し側で直線として描く。
        /// 通過点が 4 個未満のときはベジエにできる区間が無いので、通過点をそのまま返す。
        /// </remarks>
        /// <returns>Graphics.DrawBeziers にそのまま渡せる形式の点列。</returns>
        public static IEnumerable<PointF> ToBezierPolyline(this IList<PointF> points)
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
        public static IEnumerable<PointF> ToBezierPolygon(this IList<PointF> points)
        {
            if (points.Count < 3)
                return points;
            var polygonPoints = new List<PointF>(points);
            for (var index = 0; index < 3; index++)
                polygonPoints.Add(points[index]);
            return polygonPoints.ToBezierPolyline();
        }
    }
}
