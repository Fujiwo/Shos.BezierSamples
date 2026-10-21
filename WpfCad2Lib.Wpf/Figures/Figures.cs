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
    Rect  position = new();
    // ドラッグを開始した位置。現在位置との 2 点から毎回矩形を作り直す
    Point startPosition;

    /// <summary>
    /// 幅と高さがどちらも 0 でなければ有効とする。
    /// </summary>
    public override bool IsValid => position.Width != 0.0 && position.Height != 0.0;

    /// <summary>
    /// 図形の矩形そのもの。
    /// </summary>
    protected override Rect? Bounds => position;

    /// <summary>
    /// 点と矩形の枠線との距離を返す。楕円もこの矩形で近似して判定する。
    /// </summary>
    protected override double GetDistance(Point point, double nearDistance)
        => Geo.Distance(point, position);

    /// <summary>
    /// 選択時に、矩形の 4 頂点にマーカーを表示する。
    /// </summary>
    protected override IEnumerable<Point> SelectorPoints => position.ToPoints();

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
        position = new Rect(startPosition, point);
        Set(elements.First(), position);
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
    /// <summary>
    /// 表示用の WPF の <see cref="Ellipse"/> を 1 つ作る。
    /// </summary>
    protected override IEnumerable<FrameworkElement> GetInitialElements()
        => [new Ellipse()];
}

/// <summary>
/// 自由曲線の図形。ドラッグの軌跡を通過点として記録し、それらを滑らかにつなぐベジエ曲線を描く。
/// </summary>
public class CurveFigure : Figure
{
    // 直前の通過点からこの距離より離れたときだけ、新しい通過点として記録する。
    // マウスの細かい揺れまで拾うと、通過点が密集して曲線がガタつき、点数も無駄に増えるため
    const double sensitivity = 8.0;
    // 記録した通過点の列
    readonly List<Point> position = [];
    // 表示中の PathGeometry の中身。差分更新 (AppendSegment) のために保持する。まだ作っていないときは null
    PathFigure?          pathFigure;

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

    // 点を 1 つ足すと、末尾の直線が曲線になり、その先に新しい直線が付く。
    // 毎回全体を作り直すと点数 n に対して O(n²) になるため、末尾だけを差し替える
    void AppendSegment(Path element)
    {
        // まだジオメトリが無い、または点が 3 個以下 (ベジエ区間が無い) なら、全体を作る方が簡単
        if (pathFigure is null || position.Count <= 3) {
            Rebuild(element);
            return;
        }
        // 新しい点を Pn とすると、これまで直線だった最後の区間 P(n−2) → P(n−1) は、先の点 Pn ができたので
        // 制御点 C2 が決まり、ベジエ曲線にできる。それより前の区間の制御点は前後 4 点だけで決まるので変わらない。
        // そこで [直線 P(n−2) → P(n−1)] を [ベジエ P(n−2) → P(n−1)] + [直線 P(n−1) → Pn] に置き換える
        var count        = position.Count;
        var bezierPoints = Geo.ToBezier(position[count - 4], position[count - 3], position[count - 2], position[count - 1]).ToArray();
        pathFigure.Segments.RemoveAt(pathFigure.Segments.Count - 1);
        // bezierPoints[0] (区間の始点) は直前のセグメントの終点なので使わない
        pathFigure.Segments.Add(new BezierSegment(bezierPoints[1], bezierPoints[2], bezierPoints[3], true));
        pathFigure.Segments.Add(new LineSegment(position[count - 1], true));
    }

    // 通過点の列から PathGeometry 全体を作り直す。
    // SetLast では最後の点を置き換えることがあり、そうすると最後のベジエ区間の制御点も変わるので、こちらを使う
    void Rebuild(Path element)
    {
        if (position.Count <= 2) {
            pathFigure = null;
            return;
        }
        var pathGeometry = GeoHelper.BezierPathGeometry(position);
        // BezierPathGeometry は点が 2 個以上なら PathFigure を 1 つだけ持つ。以後の差分更新はこの PathFigure に対して行う
        pathFigure       = pathGeometry.Figures[0];
        element.Data     = pathGeometry;
    }
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
