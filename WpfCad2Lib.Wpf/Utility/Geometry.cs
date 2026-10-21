using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// パン (表示のスクロール) の方向を表す。
/// </summary>
public enum Direction
{
    Left, Right, Up, Down
}

/// <summary>
/// 通過点の列から、ベジエ曲線を描く WPF の <see cref="Path"/> / <see cref="PathGeometry"/> を作るヘルパー。
/// </summary>
public static class GeoHelper
{
    /// <summary>
    /// 通過点 <paramref name="points"/> を順に滑らかにつなぐ、開いた曲線の <see cref="Path"/> を作る。
    /// </summary>
    public static Path BezierPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        => new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPathGeometry(points) };

    /// <summary>
    /// 通過点 <paramref name="points"/> を滑らかにつなぎ、最後の点から最初の点へ戻って閉じた曲線の <see cref="Path"/> を作る。
    /// </summary>
    public static Path BezierPolygonPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        => new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPolygonPathGeometry(points) };

    /// <summary>
    /// 通過点の列を、開いた曲線の <see cref="PathGeometry"/> にする。
    /// 内側の区間は 3 次ベジエ曲線、最初と最後の区間は直線になる。
    /// 点が 2 個未満のときは空のジオメトリを返す。
    /// </summary>
    public static PathGeometry BezierPathGeometry(IList<Point> points)
    {
        var pathGeometry = new PathGeometry();

        if (points.Count >= 2) {
            // 最初の区間 P0 → P1 は、P0 の手前の点が無いので P0 での向きが決まらない。そのため直線で描く
            var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
            pathFigure.Segments.Add(new LineSegment(points[1], true));

            // ToBezierPolyline() は [P1, C1, C2, 終点, C1, C2, 終点, …] を返す (点が 4 個以上の場合)。
            // 先頭の P1 は直前の LineSegment の終点として描き済みなので、index = 1 から 3 個ずつ BezierSegment にする
            var bezierPoints = points.ToBezierPolyline().ToArray();
            if (bezierPoints.Length >= 4) {
                for (int index = 1; index < bezierPoints.Length; index += 3)
                    pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));
            }
            // 最後の区間も、終点の先の点が無いので直線で描く。
            // 点が 3 個のときはベジエ区間が無く、P0 → P1 → P2 の折れ線になる
            if (points.Count > 2)
                pathFigure.Segments.Add(new LineSegment(points[points.Count - 1], true));

            pathGeometry.Figures.Add(pathFigure);
        }
        return pathGeometry;
    }

    /// <summary>
    /// 通過点の列を、閉じた曲線の <see cref="PathGeometry"/> にする。
    /// 点の列を環状につなぐので、すべての区間 (最後の点 → 最初の点を含む) がベジエ曲線になる。
    /// 点が 2 個のときは線分、2 個未満のときは空のジオメトリを返す。
    /// </summary>
    public static PathGeometry BezierPolygonPathGeometry(IList<Point> points)
    {
        var pathGeometry = new PathGeometry();

        if (points.Count == 2) {
            var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
            pathFigure.Segments.Add(new LineSegment(points[1], true));

            pathGeometry.Figures.Add(pathFigure);
        }
        else if (points.Count >= 3) {
            // ToBezierPolygon() の結果は points[1] から始まり、points[0] → points[1] の区間で閉じる
            var bezierPoints = points.ToBezierPolygon().ToArray();
            var pathFigure   = new PathFigure { StartPoint = bezierPoints[0], IsClosed = true };

            for (int index = 1; index + 2 < bezierPoints.Length; index += 3)
                pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));

            pathGeometry.Figures.Add(pathFigure);
        }
        return pathGeometry;
    }
}

/// <summary>
/// ベジエ変換と、点・線分・矩形の幾何計算。
/// </summary>
public static class Geo
{
    /// <summary>
    /// 連続する 4 つの通過点 P0〜P3 から、中央の区間 P1 → P2 を表す 3 次ベジエ曲線の
    /// [始点 P1, 制御点 C1, 制御点 C2, 終点 P2] を返す。
    /// </summary>
    /// <remarks>
    /// C1 = P1 + (P2 − P0) / 6、C2 = P2 − (P3 − P1) / 6 とする (Catmull-Rom スプラインと同じ考え方)。
    /// 通過点での接線を両隣を結ぶ弦の 1/2 とし (Catmull-Rom)、3 次ベジエでは制御点を端点から接線の 1/3 の位置に置くので、
    /// 1/2 × 1/3 = 1/6 になる。隣の区間とは通過点での向きがそろうので、つなぎ目が滑らかになる。
    /// </remarks>
    public static IEnumerable<Point> ToBezier(Point point1, Point point2, Point point3, Point point4)
    {
        const double a = 1.0 / 6.0;

        yield return point2;
        // C1: P1 から、弦 P0 → P2 の向きに 1/6 だけ進んだ点
        yield return point2 + Vector.Multiply(point3 - point1, a);
        // C2: P2 から、弦 P1 → P3 と逆向きに 1/6 だけ戻った点 (P2 − (P3 − P1) / 6 と同じ)
        yield return point3 + Vector.Multiply(point2 - point4, a);
        yield return point3;
    }

    /// <summary>
    /// 2 つの値の平均を返す。
    /// </summary>
    public static double Average(double value1, double value2)
        => (value1 + value2) / 2.0;

    /// <summary>
    /// 2 点の中点を返す。
    /// </summary>
    public static Point Center(Point point1, Point point2)
        => point1 + Vector.Multiply(point2 - point1, 0.5);

    /// <summary>
    /// 線分 <paramref name="point1"/> → <paramref name="point2"/> の上で、
    /// <paramref name="point1"/> から距離 <paramref name="offset"/> だけ <paramref name="point2"/> 側へ進んだ点を返す。
    /// </summary>
    /// <remarks>2 点が同じ位置のときは向きが決まらないので <paramref name="point1"/> を返す。</remarks>
    public static Point PointByOffset(Point point1, Point point2, double offset)
    {
        var distance = point1.Distance(point2);
        if (distance == 0.0)
            return point1;
        // 距離を線分の長さに対する割合 t に直し、内分点 (1 − t)·P1 + t·P2 を求める
        var parameter = offset / distance;
        return point1.Multiply(1.0 - parameter).Add(point2.Multiply(parameter));
    }

    /// <summary>
    /// <paramref name="point2"/> から、直線 <paramref name="point1"/> → <paramref name="point2"/> に垂直な向きへ
    /// 距離 <paramref name="offset"/> だけ離れた点を返す。
    /// </summary>
    /// <remarks>
    /// 方向ベクトル d = P2 − P1 を 90° 回した (−d.Y, d.X) を単位ベクトルにして使う。
    /// Y 軸が下向きの画面座標では、<paramref name="offset"/> が正なら進行方向の右側、負なら左側になる。
    /// 2 点が同じ位置のときは向きが決まらないので <paramref name="point2"/> を返す。
    /// </remarks>
    public static Point PointByDistance(Point point1, Point point2, double offset)
    {
        var difference = point2 - point1;
        var distance   = difference.Length;

        return distance == 0.0
               ? point2
               : new Point(point2.X - offset * difference.Y / distance, point2.Y + offset * difference.X / distance);
    }

    extension(IEnumerable<Point> points)
    {
        /// <summary>
        /// 点の列を囲む最小の矩形 (外接矩形) を返す。点が 1 つも無いときは null を返す。
        /// </summary>
        public Rect? ToRect()
        {
            var hasPoint = false;
            double minimumX = double.MaxValue, minimumY = double.MaxValue, maximumX = -double.MaxValue, maximumY = -double.MaxValue;

            foreach (var point in points) {
                hasPoint = true;
                if (point.X < minimumX) minimumX = point.X;
                if (point.X > maximumX) maximumX = point.X;
                if (point.Y < minimumY) minimumY = point.Y;
                if (point.Y > maximumY) maximumY = point.Y;
            }
            if (!hasPoint)
                return null;
            return new Rect(minimumX, minimumY, maximumX - minimumX, maximumY - minimumY);
        }
    }

    extension(Rect rect)
    {
        /// <summary>
        /// 矩形の 4 つの頂点を、左上から反時計回り (画面上で左上 → 左下 → 右下 → 右上) の順に返す。
        /// </summary>
        public IEnumerable<Point> ToPoints()
        {
            yield return rect.TopLeft    ;
            yield return rect.BottomLeft ;
            yield return rect.BottomRight;
            yield return rect.TopRight   ;
        }
    }

    /// <summary>
    /// 点 <paramref name="point"/> と線分 <paramref name="line"/> の最短距離を返す。
    /// </summary>
    /// <remarks>
    /// 点から線分に下ろした垂線の足が線分の外に出る場合は、近い方の端点までの距離になる。
    /// 線分の始点と終点が同じ位置だと最後の割り算が 0 / 0 になるので、長さ 0 の線分は渡さない前提である。
    /// </remarks>
    public static double Distance(Point point, LineGeometry line)
    {
        // d: 線分の向き (S → E)、d1: 点 → 始点 S、d2: 点 → 終点 E
        var d  = line.EndPoint   - line.StartPoint;
        var d1 = line.StartPoint - point;
        var d2 = line.EndPoint   - point;
        // 内積の符号で、垂線の足が線分のどこに来るかを調べる
        var f0 = d * d1;
        var f1 = d * d2;

        // f0 > 0: 点が始点より手前 (S の外側) にある → 始点までの距離
        // f1 < 0: 点が終点より先 (E の外側) にある → 終点までの距離
        // それ以外: 垂線の足が線分上にある → 外積の大きさ |d × d1| (平行四辺形の面積) を底辺 |d| で割った高さ
        return (f0 > 0.0)
               ? d1.Length
               : (f1 < 0.0 ? d2.Length : System.Math.Abs(d.Y * d1.X - d.X * d1.Y) / d.Length);
    }

    /// <summary>
    /// 点 <paramref name="point"/> と矩形 <paramref name="rect"/> の距離を返す。
    /// </summary>
    /// <param name="point">距離を測る点。</param>
    /// <param name="rect">対象の矩形。</param>
    /// <param name="isFilled">
    /// true のときは矩形を塗りつぶした面として扱い、内側の点の距離を 0 にする。
    /// false のときは枠線として扱い、内側の点でも最も近い辺までの距離を返す。
    /// </param>
    public static double Distance(Point point, Rect rect, bool isFilled = false)
    {
        if (isFilled && rect.Contains(point))
            return 0.0;

        // 4 辺をそれぞれ線分とみなし、その中で最も近い辺までの距離を求める
        var minimumDistance = double.MaxValue;
        var minX = rect.Left  ;
        var minY = rect.Top   ;
        var maxX = rect.Right ;
        var maxY = rect.Bottom;

        var distance = Distance(point, new LineGeometry(new Point(minX, minY), new Point(minX, maxY)));
        if (distance < minimumDistance)
            minimumDistance = distance;
        distance = Distance(point, new LineGeometry(new Point(minX, maxY), new Point(maxX, maxY)));
        if (distance < minimumDistance)
            minimumDistance = distance;
        distance = Distance(point, new LineGeometry(new Point(maxX, maxY), new Point(maxX, minY)));
        if (distance < minimumDistance)
            minimumDistance = distance;
        distance = Distance(point, new LineGeometry(new Point(maxX, minY), new Point(minX, minY)));
        if (distance < minimumDistance)
            minimumDistance = distance;

        return minimumDistance;
    }

    /// <summary>
    /// 点 <paramref name="point"/> と、点の列を順に結んだ折れ線との最短距離を返す。
    /// 点が 2 個未満で線分が作れないときは <see cref="double.MaxValue"/> を返す。
    /// </summary>
    /// <remarks>
    /// 曲線図形の当たり判定では、実際のベジエ曲線ではなく通過点を結んだ折れ線で近似している。
    /// 通過点は細かく取られるので、選択の判定にはこれで十分な精度になる。
    /// </remarks>
    public static double Distance(Point point, IEnumerable<Point> points)
    {
        var    minimumDistance = double.MaxValue;
        Point? startPoint      = null;
        foreach (var aPoint in points) {
            if (startPoint is not null) {
                var distance = Distance(point, new LineGeometry(startPoint.Value, aPoint));
                if (distance < minimumDistance)
                    minimumDistance = distance;
            }
            startPoint = aPoint;
        }
        return minimumDistance;
    }
}

//public class Linea
//{
//    public Point Start { get; set; }
//    public Point End   { get; set; }

//    public Point Center
//    {
//        get { return Geometry.Center(Start, End); }
//    }
//}

/// <summary>
/// <see cref="Point"/> / <see cref="Rect"/> / <see cref="Matrix"/> の拡張メソッドと、通過点の列をベジエ曲線の点列に変換する拡張メソッド。
/// </summary>
public static class GeoExtensions
{
    extension(Point point1)
    {
        /// <summary>
        /// 2 点間の距離を返す。
        /// </summary>
        public double Distance(Point point2)
            => (point1 - point2).Length;

        /// <summary>
        /// 2 点の座標を成分ごとに足した点を返す (位置ベクトルとしての和)。
        /// </summary>
        public Point Add(Point point2)
            => new Point(point1.X + point2.X, point1.Y + point2.Y);
    }

    extension(Point point)
    {
        /// <summary>
        /// 座標を <paramref name="rate"/> 倍した点を返す (位置ベクトルとしての定数倍)。
        /// </summary>
        public Point Multiply(double rate)
            => new Point(point.X * rate, point.Y * rate);

        /// <summary>
        /// 点 <paramref name="point"/> を通過点として追加してよいかを返す。
        /// 最後の通過点から <paramref name="MinimumDistance"/> 以上離れていれば (または通過点がまだ無ければ) true になる。
        /// </summary>
        /// <remarks>近すぎる点を足すと制御点が密集して曲線がガタつくので、間引くために使う。</remarks>
        public bool IsValidForBezier(IList<Point> points, double MinimumDistance)
            => points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance;
    }

    extension(Rect rect)
    {
        /// <summary>
        /// 矩形の中心を返す。
        /// </summary>
        public Point Center()
            => Geo.Center(rect.TopLeft, rect.BottomRight);
    }

    extension(Matrix matrix)
    {
        /// <summary>
        /// 矩形を行列で変換した矩形を返す。
        /// </summary>
        /// <remarks>
        /// 左上と右下の 2 頂点だけを変換するので、拡大縮小と平行移動だけの行列 (ズーム / パン) を前提にしている。
        /// 回転を含む行列では正しい外接矩形にならない。
        /// </remarks>
        public Rect Transform(Rect rect)
        {
            var topLeft     = matrix.Transform(rect.TopLeft    );
            var bottomRight = matrix.Transform(rect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }
    }

    extension(IList<Point> points)
    {
        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、3 次ベジエ曲線の点列
        /// [始点 P1, C1, C2, 終点 P2, C1, C2, 終点 P3, …, C1, C2, 終点 P(n−2)] に変換する。
        /// </summary>
        /// <remarks>
        /// <para>
        /// 区間 P(i+1) → P(i+2) の制御点は、その両隣の点も使って
        /// C1 = P(i+1) + (P(i+2) − P(i)) / 6、C2 = P(i+2) − (P(i+3) − P(i+1)) / 6 とする。
        /// 1/6 は、Catmull-Rom スプラインの接線 (両隣を結ぶ弦の 1/2) と、3 次ベジエで制御点を接線の 1/3 の位置に置くことを掛け合わせた値である。
        /// </para>
        /// <para>
        /// 前後に点がそろっている内側の n − 3 区間だけを返す。最初の区間 P0 → P1 と最後の区間 P(n−2) → P(n−1) は、
        /// 片側の点が無くて端点での向きが決まらないので含めない。呼び出し側で直線として描く (<see cref="GeoHelper.BezierPathGeometry"/>)。
        /// </para>
        /// <para>点が 4 個未満のときはベジエ区間が作れないので、入力の点をそのまま返す。</para>
        /// </remarks>
        public IEnumerable<Point> ToBezierPolyline()
        {
            if (points.Count < 4) {
                foreach (var point in points)
                    yield return point;
            } else {
                const double a = 1.0 / 6.0;
                // 最初のベジエ区間の始点。P0 → P1 の直線の終点でもある
                yield return points[1];
                // index は区間 P(index+1) → P(index+2) の 1 つ手前の点 P(index) を指す
                for (var index = 0; index < points.Count - 3; index++) {
                    // C1: P(i+1) から、弦 P(i) → P(i+2) の向きに 1/6
                    yield return points[index + 1] + Vector.Multiply(points[index + 2] - points[index + 0], a);
                    // C2: P(i+2) から、弦 P(i+1) → P(i+3) と逆向きに 1/6
                    yield return points[index + 2] + Vector.Multiply(points[index + 1] - points[index + 3], a);
                    // 区間の終点。次の区間の始点も兼ねるので、1 区間あたり 3 点ずつ増える
                    yield return points[index + 2];
                }
            }
        }

        /// <summary>
        /// 通過点の列 P0〜P(n−1) を、最後の点から最初の点へ戻って閉じる曲線のベジエ点列に変換する。
        /// 返す点列は P1 から始まり、P1 → P2, …, P(n−1) → P0, P0 → P1 の n 区間すべてがベジエ曲線になる。
        /// </summary>
        /// <remarks>
        /// 点の列を環状とみなすと、どの区間にも前後の点がそろう。
        /// そこで末尾に先頭の 3 点 P0, P1, P2 を付け足した n + 3 個の列を <see cref="ToBezierPolyline"/> に渡すと、
        /// 内側の (n + 3) − 3 = n 区間、つまり一周分がちょうどベジエ曲線になる。
        /// 点が 3 個未満のときは入力の点をそのまま返す。
        /// </remarks>
        public IEnumerable<Point> ToBezierPolygon()
        {
            if (points.Count < 3)
                return points;
            // [P0, …, P(n−1), P0, P1, P2] を作る
            var polygonPoints = new List<Point>(points);
            for (var index = 0; index < 3; index++)
                polygonPoints.Add(points[index]);
            return polygonPoints.ToBezierPolyline();
        }
    }
}
