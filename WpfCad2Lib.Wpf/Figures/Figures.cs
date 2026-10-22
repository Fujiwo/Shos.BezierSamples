using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 線分の図形。ドラッグの開始位置が始点、現在位置が終点になる。
/// </summary>
public class LineFigure : Figure
{
    // 当たり判定 (Geo.Distance) にそのまま渡せるよう、始点と終点を LineGeometry で持つ
    readonly LineGeometry position = new();

    /// <summary>
    /// 長さが 0 でなければ有効とする。無効な図形はドラッグ終了時に破棄されるので、クリックしただけの線分は残らない。
    /// </summary>
    public override bool IsValid => position.StartPoint != position.EndPoint;

    /// <summary>
    /// 始点と終点を対角とする矩形。<see cref="Figure.IsNear"/> で詳しい距離計算をする前の、大まかな絞り込みに使われる。
    /// </summary>
    protected override Rect? Bounds => new Rect(position.StartPoint, position.EndPoint);

    /// <summary>
    /// 点と線分の最短距離を返す。
    /// </summary>
    protected override double GetDistance(Point point, double nearDistance)
        => Geo.Distance(point, position);

    /// <summary>
    /// 選択時に、始点と終点にマーカーを表示する。
    /// </summary>
    protected override IEnumerable<Point> SelectorPoints
    {
        get
        {
            yield return position.StartPoint;
            yield return position.EndPoint  ;
        }
    }

    /// <summary>
    /// 表示用の WPF の <see cref="Line"/> を 1 つ作る。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new Line()];

    /// <summary>
    /// ドラッグ開始時に始点を設定する。
    /// </summary>
    protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
    {
        position.StartPoint = point;
        var element         = elements.First();
        ((Line)element).X1  = point.X;
        ((Line)element).Y1  = point.Y;
    }

    /// <summary>
    /// ドラッグ中に終点を現在位置へ動かす。
    /// </summary>
    protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
    {
        position.EndPoint  = point;
        var element        = elements.First();
        ((Line)element).X2 = point.X;
        ((Line)element).Y2 = point.Y;
    }
}

/// <summary>
/// 矩形と楕円に共通の基底クラス。ドラッグの開始位置と現在位置を対角とする矩形で位置と大きさを決める。
/// 表示する WPF の要素 (<see cref="Rectangle"/> / <see cref="Ellipse"/>) は派生クラスで作る。
/// </summary>
public abstract class RectFigure : Figure
{
    /// <summary>図形の位置と大きさを決める外接矩形。</summary>
    protected Rect Position { get; private set; } = new();
    // ドラッグを開始した位置。現在位置との 2 点から毎回矩形を作り直す
    Point startPosition;

    /// <summary>
    /// 幅と高さがどちらも 0 でなければ有効とする。
    /// </summary>
    public override bool IsValid => Position.Width != 0.0 && Position.Height != 0.0;

    /// <summary>
    /// 図形の矩形そのもの。
    /// </summary>
    protected override Rect? Bounds => Position;

    /// <summary>
    /// 点と矩形の枠線との距離を返す。
    /// </summary>
    protected override double GetDistance(Point point, double nearDistance)
        => Geo.Distance(point, Position);

    /// <summary>
    /// 選択時に、矩形の 4 頂点にマーカーを表示する。
    /// </summary>
    protected override IEnumerable<Point> SelectorPoints => Position.ToPoints();

    /// <summary>
    /// ドラッグ開始位置を覚え、大きさ 0 の矩形を置く。
    /// </summary>
    protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
    {
        startPosition = point;
        Set(elements, point);
    }

    /// <summary>
    /// 開始位置と現在位置を対角とする矩形に更新する。
    /// </summary>
    protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
    {
        Set(elements, point);
    }

    void Set(IEnumerable<FrameworkElement> elements, Point point)
    {
        // Rect(Point, Point) は 2 点の大小を正規化するので、左上以外の方向へドラッグしても幅・高さは負にならない
        Position = new Rect(startPosition, point);
        Set(elements.First(), Position);
    }

    // Canvas 上の要素は、添付プロパティ Canvas.Left / Top で位置を、Width / Height で大きさを指定する
    void Set(FrameworkElement element, Rect position)
    {
        Canvas.SetLeft(element, position.Left);
        Canvas.SetTop (element, position.Top );
        element.Width  = position.Width;
        element.Height = position.Height;
    }
}

/// <summary>
/// 矩形の図形。
/// </summary>
public class RectangleFigure : RectFigure
{
    /// <summary>
    /// 表示用の WPF の <see cref="Rectangle"/> を 1 つ作る。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new Rectangle()];
}

/// <summary>
/// 楕円の図形。矩形に内接する楕円を描く。
/// </summary>
public class EllipseFigure : RectFigure
{
    Rect OutlineBounds
    {
        get
        {
            // WPF の Ellipse は、線の中心を線幅の半分だけレイアウト矩形の内側に置く。
            var stroke = Attribute.StrokeThickness;
            return new Rect(Position.X + stroke / 2, Position.Y + stroke / 2,
                System.Math.Max(0, Position.Width - stroke), System.Math.Max(0, Position.Height - stroke));
        }
    }

    /// <summary>線幅による内側への補正も反映した輪郭の範囲。</summary>
    protected override Rect? Bounds => OutlineBounds;

    /// <summary>表示と同じ楕円の輪郭を折れ線にして、点との距離を求める。</summary>
    protected override double GetDistance(Point point, double nearDistance)
    {
        var bounds = OutlineBounds;
        if (bounds.Width == 0 || bounds.Height == 0) {
            var nearest = new Point(
                System.Math.Clamp(point.X, bounds.Left, bounds.Right),
                System.Math.Clamp(point.Y, bounds.Top, bounds.Bottom));
            return (point - nearest).Length;
        }
        var tolerance = System.Math.Max(1e-6, System.Math.Min(0.001, nearDistance * 0.001));
        var outline = new EllipseGeometry(bounds).GetFlattenedPathGeometry(tolerance, ToleranceType.Absolute);
        var minimumDistance = double.MaxValue;
        foreach (var path in outline.Figures) {
            var points = new[] { path.StartPoint }
                .Concat(path.Segments.OfType<PolyLineSegment>().SelectMany(segment => segment.Points))
                .Append(path.StartPoint);
            minimumDistance = System.Math.Min(minimumDistance, Geo.Distance(point, points));
        }
        return minimumDistance;
    }

    /// <summary>
    /// 表示用の WPF の <see cref="Ellipse"/> を 1 つ作る。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new Ellipse()];
}

/// <summary>
/// 自由曲線の図形。ドラッグの軌跡を通過点として記録し、すべての通過点を通る 3 次スプライン曲線 (3 次ベジエ曲線の列) を描く。
/// </summary>
public class CurveFigure : Figure
{
    // 直前の通過点からこの距離より離れたときだけ、新しい通過点として記録する。
    // マウスの細かい揺れまで拾うと、通過点が密集して曲線がガタつき、点数も無駄に増えるため
    const double sensitivity = 8.0;
    // ドラッグ中に解き直す、末尾の区間の数 (AppendSegment)。
    // これより前の接線は固定するが、その影響は 0.27⁶ ≈ 0.04% 程度なので、見た目は変わらない
    const int partialRebuildSegmentCount = 6;
    // 記録した通過点の列
    readonly List<Point>  position = [];
    // 各通過点での 3 次スプライン曲線の接線。position と同じ数だけ持つ (ジオメトリを作るまでは空)
    readonly List<Vector> tangents = [];
    // 表示中の PathGeometry の中身。区間 P(j) → P(j+1) が Segments[j] にあたる。
    // 差分更新 (AppendSegment) のために保持する。まだ作っていないときは null
    PathFigure?           pathFigure;

    /// <summary>
    /// 通過点の外接矩形。
    /// </summary>
    protected override Rect? Bounds => position.Count == 0 ? null : position.ToRect();

    /// <summary>
    /// 通過点が 3 個以上あれば有効とする。
    /// </summary>
    public override bool IsValid => position.Count > 2;

    /// <summary>
    /// 点と、通過点を結んだ折れ線との距離を返す (ベジエ曲線そのものではなく折れ線で近似する)。
    /// </summary>
    protected override double GetDistance(Point point, double nearDistance)
        => position.Count == 0 ? double.MaxValue : Geo.Distance(point, position);

    /// <summary>
    /// 選択時に、曲線の始点と終点にマーカーを表示する。
    /// </summary>
    protected override IEnumerable<Point> SelectorPoints
    {
        get
        {
            if (IsValid) {
                yield return position[0];
                yield return position[position.Count - 1];
            }
        }
    }

    /// <summary>
    /// 表示用の WPF の <see cref="Path"/> を 1 つ作る。ジオメトリ (Data) は点が 3 個そろった時点で設定する。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new Path()];

    /// <summary>
    /// ドラッグ開始位置を最初の通過点にする。
    /// </summary>
    protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
    {
        position.Clear();
        position.Add(point);
        pathFigure = null;
    }

    /// <summary>
    /// ドラッグ中、直前の通過点から <c>sensitivity</c> より離れたら通過点を追加し、表示を差分更新する。
    /// </summary>
    protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
    {
        if (position.Count > 0) {
            var lastPoint = position[position.Count - 1];
            if ((point - lastPoint).Length > sensitivity) {
                position.Add(point);
                AppendSegment((Path)elements.First());
            }
        }
    }

    /// <summary>
    /// ドラッグの終点は間隔に関係なく必ず通るようにする (直前の点に近すぎる場合はその点を置き換える)。
    /// </summary>
    protected override void SetLast(IEnumerable<FrameworkElement> elements, Point point)
    {
        var lastPoint = position[position.Count - 1];
        if (position.Count >= 2 && (point - lastPoint).Length <= sensitivity)
            position[position.Count - 1] = point;
        else if (point != lastPoint)
            position.Add(point);
        Rebuild((Path)elements.First());
    }

    // 点を 1 つ足したときに、表示を差分更新する。
    // 3 次スプライン曲線では、点を 1 つ足すと全区間の接線が少しずつ変わるので、正確には全体を作り直す必要がある。
    // しかしドラッグ中に毎回全体を作り直すと、点数 n に対して全体で O(n²) になる。
    // 1 点の変化の影響は 1 区間離れるごとに約 0.27 倍に減るので、ドラッグ中は末尾の partialRebuildSegmentCount 区間だけを
    // 解き直して差し替え、それより前の接線は固定する。ドラッグを終えたとき (SetLast) に、全体を正確に作り直す
    void AppendSegment(Path element)
    {
        var count = position.Count;
        // まだジオメトリが無い、または点が少なくて末尾だけ解き直しても全体と変わらないなら、全体を作り直す
        if (pathFigure is null || count <= partialRebuildSegmentCount + 1) {
            Rebuild(element);
            return;
        }
        // 接線を前回の値に固定する通過点。ここから末尾までの接線だけを解き直す
        var start           = count - 1 - partialRebuildSegmentCount;
        var partialTangents = position.ToSplineTangents(start, tangents[start]);
        tangents.RemoveRange(start, tangents.Count - start);
        tangents.AddRange(partialTangents);
        // P(start + 1) から先の接線が変わったので、区間 P(start) → P(start + 1) から末尾までを作り直す。
        // 新しい点 P(count − 1) への区間もここで足される
        while (pathFigure.Segments.Count > start)
            pathFigure.Segments.RemoveAt(pathFigure.Segments.Count - 1);
        for (var index = start; index < count - 1; index++)
            pathFigure.Segments.Add(ToBezierSegment(index));
    }

    // 通過点の列から、接線と PathGeometry 全体を正確に作り直す。
    // SetLast では最後の点を置き換えることがあり、ドラッグ中の近似 (AppendSegment) も解消したいので、こちらを使う
    void Rebuild(Path element)
    {
        tangents.Clear();
        if (position.Count <= 2) {
            pathFigure = null;
            return;
        }
        tangents.AddRange(position.ToSplineTangents());
        pathFigure = new PathFigure { StartPoint = position[0], IsClosed = false };
        for (var index = 0; index < position.Count - 1; index++)
            pathFigure.Segments.Add(ToBezierSegment(index));
        var pathGeometry = new PathGeometry();
        pathGeometry.Figures.Add(pathFigure);
        // 以後の差分更新は、この pathFigure の Segments を直接書き換える (表示にもそのまま反映される)
        element.Data = pathGeometry;
    }

    // 区間 P(index) → P(index + 1) の 3 次ベジエ曲線を、両端の接線から作る。
    // 制御点は C1 = P(index) + m(index) / 3、C2 = P(index + 1) − m(index + 1) / 3 (GeoExtensions.ToSplineBezier と同じ)
    BezierSegment ToBezierSegment(int index)
        => new(position[index] + tangents[index] / 3.0, position[index + 1] - tangents[index + 1] / 3.0, position[index + 1], true);
}

/// <summary>
/// 矢印の図形。ドラッグの開始位置を矢の根元 (尾)、現在位置を矢の先端とする、塗りつぶした 6 角形で描く。
/// </summary>
/// <remarks>
/// 頂点は、尾 → 軸の左端 → 矢じりの左端 → 先端 → 矢じりの右端 → 軸の右端 の順。
/// 軸は尾の 1 点から矢じりの付け根に向かって太くなる、細長い三角形になる。
/// </remarks>
public class ArrowFigure : Figure
{
    // 矢じりの大きさを決める基準の長さの上限。矢印をいくら長くしても、矢じりと軸の太さはこれ以上大きくならない
    const double rate0 = 200.0;
    // 矢じりの長さ (先端から付け根までの距離) の、基準の長さに対する割合
    const double rate1 = 0.2;
    // 矢じりの半分の幅 (軸の中心線から矢じりの左右の端までの距離) の割合。矢じりの幅は基準の長さの 0.3 倍になる
    const double rate2 = 0.15;
    // 付け根での軸の半分の太さの割合。軸の太さは付け根で基準の長さの 0.2 倍になる
    const double rate3 = 0.1;
    // 現在は使っていない
    const double rate4 = 0.25;

    // 尾 (StartPoint) と先端 (EndPoint)。当たり判定はこの線分で近似する
    readonly LineGeometry position = new();

    /// <summary>
    /// 長さが 0 でなければ有効とする。
    /// </summary>
    public override bool IsValid => position.StartPoint != position.EndPoint;

    /// <summary>
    /// 矢印は塗りつぶして描く。
    /// </summary>
    public override bool IsFilled => true;

    /// <summary>
    /// 尾と先端を対角とする矩形。矢じりの幅の分ははみ出すが、絞り込みには十分である。
    /// </summary>
    protected override Rect? Bounds => new Rect(position.StartPoint, position.EndPoint);

    /// <summary>
    /// 点と、尾から先端までの線分との距離を返す。
    /// </summary>
    protected override double GetDistance(Point point, double nearDistance)
        => Geo.Distance(point, position);

    /// <summary>
    /// 選択時に、尾と先端にマーカーを表示する。
    /// </summary>
    protected override IEnumerable<Point> SelectorPoints
    {
        get
        {
            yield return position.StartPoint;
            yield return position.EndPoint;
        }
    }

    /// <summary>
    /// 表示用の WPF の <see cref="Polygon"/> を 1 つ作る。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new Polygon()];

    /// <summary>
    /// ドラッグ開始位置を尾にする。頂点は先端が決まる <see cref="SetNext(IEnumerable{FrameworkElement}, Point)"/> で作る。
    /// </summary>
    protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
    {
        position.StartPoint = point;
    }

    /// <summary>
    /// 先端を現在位置にして、6 角形の頂点を求め直す。
    /// </summary>
    protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
    {
        position.EndPoint         = point;

        // 矢印の長さを基準の長さとし、rate0 で頭打ちにする。
        // 短い矢印では矢じりも小さく、長い矢印でも矢じりが大きくなりすぎない
        var distance              = position.StartPoint.Distance(position.EndPoint);
        var maxDistance           = rate0;
        if (distance > maxDistance)
            distance = maxDistance;

        // 矢じりの付け根の中心: 先端から尾の方へ、矢じりの長さだけ戻った軸上の点
        var innerPoint            = Geo.PointByOffset  (position.EndPoint  , position.StartPoint,  distance * rate1);
        // 矢じりの左右の端: 付け根の中心から、軸に垂直な向きへ左右に矢じりの半幅だけ離れた点
        var leftPoint             = Geo.PointByDistance(position.StartPoint, innerPoint         , -distance * rate2);
        var rightPoint            = Geo.PointByDistance(position.StartPoint, innerPoint         ,  distance * rate2);
        // 軸の左右の端: 付け根の中心から矢じりの端へ向かう線上で、軸の半分の太さだけ離れた点
        var leftInnerPoint        = Geo.PointByOffset  (innerPoint         , leftPoint          ,  distance * rate3);
        var rightInnerPoint       = Geo.PointByOffset  (innerPoint         , rightPoint         ,  distance * rate3);

        // 尾 → 左側 → 先端 → 右側 の順に一周するよう並べる (Polygon は最後の点から最初の点へ自動で閉じる)
        PointCollection points    = [position.StartPoint, leftInnerPoint, leftPoint, position.EndPoint, rightPoint, rightInnerPoint];

        var element               = (Polygon)elements.First();
        element.Points            = points;
    }
}

/// <summary>
/// 文字列の図形。クリックした位置を左上として、袋文字 (<see cref="OutlineText"/>) で描く。
/// </summary>
public class TextFigure : Figure
{
    /// <summary>
    /// 文字列を指定しなかったときに表示する既定の文字列。
    /// </summary>
    public const string DefaultText = "テキスト";

    // 文字列の左上の位置
    Point  position = new();
    string text     = DefaultText;

    /// <summary>
    /// 表示する文字列。配置 (<see cref="SetFirst(IEnumerable{FrameworkElement}, Point)"/>) より前に設定する。
    /// </summary>
    public string Text
    {
        get => text;
        set => text = value;
    }

    /// <summary>
    /// 空でない文字列なら有効とする。
    /// </summary>
    public override bool IsValid => text.Length > 0;

    /// <summary>
    /// 文字の内側を塗りつぶして描く。
    /// </summary>
    public override bool IsFilled => true;

    /// <summary>
    /// 文字列の領域の概算。1 文字を FontSize 四方の正方形 (全角文字) とみなして、幅を FontSize × 文字数とする。
    /// 実際の字形を測らずに済むので軽いが、半角文字が多いと実際より広くなる。
    /// </summary>
    protected override Rect? Bounds => new Rect(position, new Size { Width = Attribute.FontSize * text.Length, Height = Attribute.FontSize });

    /// <summary>
    /// 文字列の領域を塗りつぶした矩形として扱い、内側なら距離 0、外側なら矩形の辺までの距離を返す。
    /// 文字の線の上でなくても、文字列の範囲内をクリックすれば選択できる。
    /// </summary>
    protected override double GetDistance(Point point, double nearDistance)
    {
        var bounds = Bounds;
        return bounds is null ? double.MaxValue : Geo.Distance(point, bounds.Value, true);
    }

    /// <summary>
    /// 選択時に、文字列の左上にマーカーを表示する。
    /// </summary>
    protected override IEnumerable<Point> SelectorPoints
    {
        get
        {
            yield return position;
        }
    }

    /// <summary>
    /// 表示用の <see cref="OutlineText"/> を 1 つ作る。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new OutlineText()];

    /// <summary>
    /// クリック位置に文字列を置く。文字列はクリックで配置するだけで、ドラッグでは動かさない。
    /// </summary>
    protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
    {
        position       = point;
        var element    = (OutlineText)elements.First();
        element.Text   = Text;
        Canvas.SetLeft(element, position.X);
        Canvas.SetTop (element, position.Y);
    }
}
