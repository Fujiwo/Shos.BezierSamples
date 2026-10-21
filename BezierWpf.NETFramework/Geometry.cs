using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    /// <summary>
    /// 通過点の列から、ベジエ曲線を描く WPF の <see cref="Path"/> / <see cref="PathGeometry"/> を作るヘルパー。
    /// </summary>
    public static class GraphicHelper
    {
        /// <summary>
        /// 通過点 <paramref name="points"/> を順に通る開いた曲線の <see cref="Path"/> を作る。
        /// </summary>
        public static Path BezierPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        {
            return new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPathGeometry(points) };
        }

        /// <summary>
        /// 通過点 <paramref name="points"/> を順に通り、最後の点から最初の点へ戻って 1 周する閉じた曲線の <see cref="Path"/> を作る。
        /// </summary>
        public static Path BezierPolygonPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        {
            return new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPolygonPathGeometry(points) };
        }

        /// <summary>
        /// 通過点 <paramref name="points"/> を順に通る開いた曲線のジオメトリーを作る。
        /// 最初の区間 (points[0] → points[1]) と最後の区間は直線、その間の区間は 3 次ベジエ曲線になる。
        /// 点が 2 個なら 1 本の直線、3 個なら 2 本の直線 (折れ線) になる。
        /// </summary>
        public static PathGeometry BezierPathGeometry(IList<Point> points)
        {
            var pathGeometry = new PathGeometry();

            if (points.Count >= 2) {
                // 最初の区間は、手前の点 (points[-1]) が無く points[0] での向きが決まらないので直線で描く
                var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
                pathFigure.Segments.Add(new LineSegment(points[1], true));

                // bezierPoints は [points[1], 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] の形。
                // 先頭の points[1] は直前の LineSegment の終点 (= 現在位置) なので飛ばし、index 1 から 3 個ずつ BezierSegment にする。
                // 点が 3 個以下のときは通過点がそのまま返り、ベジエの区間は無い。
                var bezierPoints = points.ToBezierPolyline().ToArray();
                if (bezierPoints.Length >= 4) {
                    for (int index = 1; index < bezierPoints.Length; index += 3)
                        pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));
                }
                // 最後の区間も、先の点が無く終点での向きが決まらないので直線で描く。
                // ベジエ曲線は points[Count - 2] で終わっているので、そこから最後の点へ線を引く
                // (点が 3 個のときは points[1] から points[2] への直線になる)。
                if (points.Count > 2)
                    pathFigure.Segments.Add(new LineSegment(points[points.Count - 1], true));

                pathGeometry.Figures.Add(pathFigure);
            }
            return pathGeometry;
        }

        /// <summary>
        /// 通過点 <paramref name="points"/> を順に通って最初の点に戻る、閉じた曲線のジオメトリーを作る。
        /// 閉じた曲線では、どの区間にも前後の点があるので、すべての区間が 3 次ベジエ曲線になる。
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
                // ToBezierPolygon() の結果は points[1] から始まり、points[0] → points[1] の区間で閉じる
                // 最後のベジエ曲線の終点は始点 (points[1]) と同じ位置なので、IsClosed = true で足される閉じ線は長さ 0 になる。
                // 閉じた図形として扱われるので、始点のつなぎ目も線の結合 (StrokeLineJoin) で描かれる
                var bezierPoints = points.ToBezierPolygon().ToArray();
                var pathFigure   = new PathFigure { StartPoint = bezierPoints[0], IsClosed = true };

                // bezierPoints[0] は始点なので、index 1 から [制御点1, 制御点2, 終点] の 3 個ずつ BezierSegment にする
                for (int index = 1; index + 2 < bezierPoints.Length; index += 3)
                    pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));

                pathGeometry.Figures.Add(pathFigure);
            }
            return pathGeometry;
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
        { return (value1 + value2) / 2.0; }
    }

    /// <summary>
    /// <see cref="Point"/> などに対するベクトル演算と、通過点の列をベジエ曲線用の点列に変換する拡張メソッド。
    /// <see cref="Point"/> を位置ベクトルとして扱い、加減算・スカラー倍で計算する。
    /// </summary>
    public static class GeometryExtensions
    {
        /// <summary>
        /// 矩形の中心を返す。
        /// </summary>
        public static Point Center(this Rect rect)
        { return new Point { X = Geo.Average(rect.Left, rect.Right), Y = Geo.Average(rect.Top, rect.Bottom) }; }

        /// <summary>
        /// 2 点間の距離を返す。
        /// </summary>
        public static double Distance(this Point point1, Point point2)
        { return point1.Subtract(point2).Absolute(); }

        /// <summary>
        /// 2 つの点をベクトルとして足す。
        /// </summary>
        public static Point Add(this Point point, Point size)
        { return new Point(point.X + size.X, point.Y + size.Y); }

        /// <summary>
        /// <paramref name="point2"/> から <paramref name="point1"/> へ向かうベクトル (point1 − point2) を返す。
        /// </summary>
        public static Point Subtract(this Point point1, Point point2)
        { return new Point(point1.X - point2.X, point1.Y - point2.Y); }

        /// <summary>
        /// ベクトルを <paramref name="rate"/> 倍する。
        /// </summary>
        public static Point Multiply(this Point point, double rate)
        { return new Point(point.X * rate, point.Y * rate);  }

        /// <summary>
        /// ベクトルの長さを返す。
        /// </summary>
        public static double Absolute(this Point point)
        { return (point.X.Square() + point.Y.Square()).SquareRoot(); }

        /// <summary>
        /// 2 乗を返す。
        /// </summary>
        public static double Square(this double value)
        { return value * value; }

        /// <summary>
        /// 平方根を返す。
        /// </summary>
        public static double SquareRoot(this double value)
        { return Math.Sqrt(value); }

        /// <summary>
        /// 点の間引き用の判定。<paramref name="point"/> が、通過点の列 <paramref name="points"/> の最後の点から
        /// <paramref name="MinimumDistance"/> 以上離れていれば (または列が空なら) true を返す。
        /// 近すぎる点まで通過点にすると、マウスの細かい揺れで曲線が波打つため。
        /// </summary>
        public static bool IsValidForBezier(this Point point, IList<Point> points, double MinimumDistance)
        { return points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance; }

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、3 次ベジエ曲線用の点列
        /// [始点, 制御点1, 制御点2, 終点, 制御点1, 制御点2, 終点, …] に変換する。
        /// 前後に点がそろっている内側の n − 3 区間 (P1 → P2 〜 P(n−3) → P(n−2)) だけを変換するので、
        /// 結果は P1 から始まり P(n−2) で終わる。最初の区間 (P0 → P1) と最後の区間 (P(n−2) → P(n−1)) は
        /// 片側の点が無く向きが決まらないので含まない (呼び出し側が直線で描く)。
        /// </summary>
        /// <returns>点が 4 個以上なら 1 + 3 × (n − 3) 個の点。4 個未満ならベジエの区間が作れないので、通過点をそのまま返す。</returns>
        public static IEnumerable<Point> ToBezierPolyline(this IList<Point> points)
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
        public static IEnumerable<Point> ToBezierPolygon(this IList<Point> points)
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
    }
}
