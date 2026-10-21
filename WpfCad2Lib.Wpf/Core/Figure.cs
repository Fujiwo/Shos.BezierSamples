using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 図形の基底クラス。図形の位置 (モデル座標) と、それを画面に表示する WPF の要素 (Line や Path など) を持つ。
/// </summary>
/// <remarks>
/// 派生クラスが実装する要素生成の流れ:
/// <list type="number">
/// <item><see cref="GetInitialElements"/> で表示用の要素を作る (まだ位置は決まっていない)。</item>
/// <item><see cref="SetFirst(IEnumerable{FrameworkElement}, Point)"/> で最初の点 (ドラッグ開始位置やクリック位置) を設定する。</item>
/// <item><see cref="SetNext(IEnumerable{FrameworkElement}, Point)"/> をドラッグ中に繰り返し呼んで形を更新する。</item>
/// <item><see cref="SetLast(IEnumerable{FrameworkElement}, Point)"/> でドラッグ終了位置を設定して形を確定する。</item>
/// </list>
/// 選択されると、<see cref="SelectorPoints"/> の各点に小さな円 (選択マーク) を表示する。
/// 色・太さは <see cref="Attribute"/> が持ち、その変更通知を受けて要素に設定し直す。
/// </remarks>
public abstract class Figure
{
    FigureAttribute          attribute = new();
    // 画面に表示している図形本体の要素。Canvas に追加する前は null
    List<FrameworkElement>?   elements;
    // 選択中に表示している選択マーク。選択されていないときは null
    List<Shape>?              selectors;

    /// <summary>図形の属性 (線の色・塗りつぶしの色・線の太さ・文字の大きさ)。</summary>
    /// <remarks>
    /// 属性オブジェクトの変更通知 (PropertyChanged) を購読しており、プロパティを変えると表示に反映される。
    /// 属性オブジェクトごと差し替えた場合は、購読先を付け替えてから反映する。
    /// </remarks>
    public FigureAttribute Attribute
    {
        get => attribute;
        set
        {
            if (value == attribute)
                return;
            // 古い属性オブジェクトの購読をやめないと、古い方を変更したときにもこの図形が反応してしまう
            attribute.PropertyChanged -= OnAttributeChanged;
            attribute                  = value;
            attribute.PropertyChanged += OnAttributeChanged;
            ApplyAttribute();
        }
    }

    /// <summary>選択されているかどうか。<see cref="Select"/> で変更する。</summary>
    public bool IsSelected { get; private set; }

    /// <summary>図形として有効か (Model に追加してよいか)。大きさが 0 の図形などは false を返すよう派生クラスでオーバーライドする。</summary>
    public virtual bool IsValid => true;

    /// <summary>塗りつぶす図形かどうか。true のとき FigureAttribute.FillColor で塗りつぶす。</summary>
    public virtual bool IsFilled => false;

    /// <summary>図形を囲む矩形 (モデル座標)。近くにあるかどうかの大まかな判定に使う。位置が決まっていなければ null。</summary>
    protected abstract Rect? Bounds { get; }

    /// <summary>選択されたときに選択マークを表示する点 (線分なら両端など)。</summary>
    protected virtual IEnumerable<Point> SelectorPoints => [];

    /// <summary>既定の属性で図形を作り、その変更通知を購読する。</summary>
    protected Figure()
    {
        attribute.PropertyChanged += OnAttributeChanged;
    }

    /// <summary>表示用の要素を作って Canvas に追加し、<paramref name="firstPoint"/> を最初の点として設定する。</summary>
    /// <param name="elementCollection">追加先 (View の Canvas.Children)。</param>
    /// <param name="firstPoint">最初の点 (ドラッグ開始位置やクリック位置)。</param>
    public void AddElements(UIElementCollection elementCollection, Point firstPoint)
    {
        InitializeElements();
        if (elements is null)
            return;
        elements.ForEach(element => elementCollection.Add(element));
        SetFirst(elements, firstPoint);
    }

    /// <summary>表示用の要素を Canvas から取り除く。</summary>
    public void RemoveElements(UIElementCollection elementCollection)
    {
        if (elements is null)
            return;
        elements.ForEach(element => elementCollection.Remove(element));
        elements = null;
    }

    /// <summary>選択状態を変え、それに合わせて選択マークを Canvas に追加または削除する。</summary>
    /// <param name="elementCollection">選択マークの追加先 (View の Canvas.Children)。</param>
    /// <param name="isSelected">true なら選択し、false なら選択を外す。</param>
    public void Select(UIElementCollection elementCollection, bool isSelected = true)
    {
        // 状態が変わらないときに何もしないことで、選択マークが二重に追加されるのを防ぐ
        if (isSelected == IsSelected)
            return;
        IsSelected = isSelected;
        if (isSelected)
            AddSelectors   (elementCollection);
        else
            RemoveSelectors(elementCollection);
    }

    void AddSelectors(UIElementCollection elementCollection)
    {
        // GetSelectors() は遅延列挙なので、後で同じ要素を削除できるようリストに確定させて保持する
        selectors = GetSelectors().ToList();
        selectors.ForEach(element => elementCollection.Add(element));
    }

    void RemoveSelectors(UIElementCollection elementCollection)
    {
        if (selectors is null)
            return;
        selectors.ForEach(element => elementCollection.Remove(element));
        selectors = null;
    }

    /// <summary>ドラッグ中の位置で形を更新する。</summary>
    public void SetNext(Point point)
    {
        if (elements is null)
            return;
        SetNext(elements, point);
    }

    /// <summary>ドラッグ終了位置で形を確定する。</summary>
    public void SetLast(Point point)
    {
        if (elements is null)
            return;
        SetLast(elements, point);
    }

    /// <summary>
    /// <paramref name="point"/> がこの図形の近く (<paramref name="nearDistance"/> 以内) にあり、
    /// しかもこれまで調べた図形よりも近いかどうかを返す。
    /// </summary>
    /// <param name="point">調べる位置 (モデル座標)。</param>
    /// <param name="nearDistance">「近い」とみなす距離。</param>
    /// <param name="minimumDistance">これまでに見つかった最小の距離。より近ければこの図形までの距離に更新される。</param>
    /// <returns>この図形がこれまでで最も近い候補なら true。</returns>
    /// <remarks>全図形に対して順に呼ぶことで、最も近い図形を 1 つ選べる (CadData.Find)。</remarks>
    public bool IsNear(Point point, double nearDistance, ref double minimumDistance)
    {
        // まず外接矩形を nearDistance だけ広げた範囲で大まかに判定し、範囲内のときだけ正確な距離を計算する
        if (IsNearBounds(point, nearDistance)) {
            var distance = GetDistance(point, nearDistance);
            if (distance <= nearDistance && distance < minimumDistance) {
                minimumDistance = distance;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// <paramref name="point"/> から図形までの距離を返す。
    /// 既定では <paramref name="nearDistance"/> を返す (外接矩形の近くにあれば「ちょうど近い」とみなす)。
    /// </summary>
    protected virtual double GetDistance(Point point, double nearDistance) => nearDistance;

    /// <summary>外接矩形を <paramref name="nearDistance"/> だけ広げた範囲に <paramref name="point"/> が入っているかどうかを返す。</summary>
    protected virtual bool IsNearBounds(Point point, double nearDistance)
    {
        var bounds = Bounds;
        if (bounds is null)
            return false;
        var inflatedBounds = Rect.Inflate(bounds.Value, nearDistance, nearDistance);
        return inflatedBounds.Contains(point);
    }

    /// <summary>表示用の要素 (Line・Path など) を作って返す。位置はまだ設定しなくてよい。</summary>
    protected abstract IEnumerable<FrameworkElement> GetInitialElements();

    /// <summary>最初の点 (ドラッグ開始位置やクリック位置) を図形と要素に設定する。</summary>
    protected abstract void SetFirst(IEnumerable<FrameworkElement> elements, Point point);

    /// <summary>ドラッグ中の位置を図形と要素に設定する。既定では何もしない (クリックで置く図形用)。</summary>
    protected virtual void SetNext(IEnumerable<FrameworkElement> elements, Point point)
    {}

    /// <summary>ドラッグを終えた位置を図形と要素に設定する。既定では SetNext と同じ。</summary>
    protected virtual void SetLast(IEnumerable<FrameworkElement> elements, Point point)
        => SetNext(elements, point);

    void InitializeElements()
    {
        // 要素を作った時点で、現在の属性 (色・太さ) を適用しておく
        elements = GetInitialElements().ToList();
        ApplyAttribute();
    }
    
    // SelectorPoints の各点に、選択マーク (円) を作って返す
    IEnumerable<Shape> GetSelectors()
    {
        foreach (var selectorPoint in SelectorPoints) {
            var shape = GetSelector(selectorPoint);
            SetSelectorAttribute(shape);
            yield return shape;
        }
    }

    // selectorPoint を中心とする半径 SelectorRadius の円を作る
    static Shape GetSelector(Point selectorPoint)
    {
        var shape    = new Ellipse();
        Canvas.SetLeft(shape, selectorPoint.X - Common.SelectorRadius);
        Canvas.SetTop (shape, selectorPoint.Y - Common.SelectorRadius);
        shape.Width  =
        shape.Height = Common.SelectorRadius * 2.0;
        return shape;
    }

    void OnAttributeChanged(object? sender, PropertyChangedEventArgs e) => ApplyAttribute();

    // 画面上の要素はそのままに、属性 (色・太さなど) だけを設定し直す
    void ApplyAttribute()
    {
        if (elements is not null)
            attribute.SetAttribute(elements, IsFilled);
    }

    static void SetSelectorAttribute(Shape element)
    {
        element.Stroke          = Helper.CreateStrokeBrush(Common.SelectorStrokeColor);
        element.StrokeThickness = Common.SelectorStrokeThickness;
        element.Fill            = Helper.CreateFillBrush(Common.SelectorFillColor);
        element.Effect          = Common.DefaultShadowEffect;
    }
}
