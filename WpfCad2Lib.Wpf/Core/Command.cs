using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 描画モード (選択・線分・矩形・曲線など) の基底クラス。
/// Controller から委譲されたクリック/ドラッグを、モードごとの処理に置き換える。
/// 既定ではクリックを受け付けず、ドラッグだけを受け付ける。
/// </summary>
public abstract class Command : IMouseAcceptor
{
    /// <summary>操作対象の Model。Controller.Command のセッターから設定される。</summary>
    public Model?               Model             { protected get; set; }
    /// <summary>図形の描画先 (View の Canvas.Children)。Controller.Command のセッターから設定される。</summary>
    public UIElementCollection? ElementCollection { protected get; set; }

    /// <summary>クリックを受け付けるかどうか。false のとき Controller は OnClick を呼ばない。</summary>
    public virtual bool CanClick => false;
    /// <summary>ドラッグを受け付けるかどうか。false のとき Controller は OnDragStart / OnDragging / OnDragEnd を呼ばない。</summary>
    public virtual bool CanDrag  => true ;

    /// <summary>この Command が Controller に設定された直後に呼ばれる。モード開始時の初期化に使う。</summary>
    /// <returns>開始できた場合は true。(現在は戻り値を使う箇所はない)</returns>
    public virtual bool OnStart() => true;

    /// <inheritdoc/>
    public virtual void OnClick    (Point position) {}
    /// <inheritdoc/>
    public virtual void OnDragStart(Point position) {}
    /// <inheritdoc/>
    public virtual void OnDragging (Point position) {}
    /// <inheritdoc/>
    public virtual void OnDragEnd  (Point position) {}
}

/// <summary>
/// 図形を 1 つ追加する Command の基底クラス。派生クラスは <see cref="CreateFigure"/> で作る図形の種類だけを決める。
/// </summary>
/// <remarks>
/// ドラッグで追加する場合の流れ:
/// OnDragStart で開始位置を覚える → 最初の OnDragging で図形を作り (CreateFigure + 属性の複製)、
/// 開始位置で UIElement を Canvas に追加する (Figure.AddElements → SetFirst) → 以後の OnDragging で形を更新する (SetNext)
/// → OnDragEnd で形を確定し (SetLast)、有効なら Model に追加、無効 (大きさ 0 など) なら UIElement を取り除く。
/// クリックで追加する場合 (TextCommand など) は、OnClick で図形を作ってその位置に置き、すぐ Model に追加する。
/// </remarks>
public abstract class AddCommand : Command
{
    /// <summary>ドラッグを開始した位置 (ボタンを押した位置)。</summary>
    protected Point   StartPosition { get; private set; }
    /// <summary>作成中の図形。作成中でないときは null。</summary>
    protected Figure? Figure        { get; private set; }

    /// <summary>クリックした位置に図形を置き、Model に追加する。</summary>
    public override void OnClick(Point position)
    {
        if (ElementCollection is null)
            return;

        if (Figure is null)
            InitializeFigure();

        // クリックで置く図形は位置 1 点 (SetFirst) だけで形が決まり、有効かどうかも置く前に分かる
        // (TextFigure なら文字列が空でないか)。そのため要素の追加と Model への登録を一度に行う
        if (Figure is not null && Figure.IsValid) {
            Figure.AddElements(ElementCollection, position);
            Model?.Add(Figure);
        }

        Figure = null;
    }

    /// <summary>開始位置を覚える。図形はまだ作らない (最初の OnDragging で作る)。</summary>
    public override void OnDragStart(Point position)
    {
        StartPosition = position;
        Figure        = null;
    }

    /// <summary>初回は図形を作って Canvas に追加し、以後はマウス位置に合わせて形を更新する。</summary>
    public override void OnDragging(Point position)
    {
        if (ElementCollection is null)
            return;

        // 初回の OnDragging: 図形を作り、ドラッグ開始位置を最初の点として UIElement を Canvas に追加する
        if (Figure is null) {
            InitializeFigure();
            Figure?.AddElements(ElementCollection, StartPosition);
        }

        Figure?.SetNext(position);
    }

    /// <summary>形を確定し、有効な図形なら Model に追加する。無効なら Canvas から取り除く。</summary>
    public override void OnDragEnd(Point position)
    {
        if (ElementCollection is null || Figure is null)
            return;

        Figure.SetLast(position);
        // 始点と終点が同じ線分や幅 0 の矩形などは、画面からも消して Model には登録しない
        if (!Figure.IsValid)
            Figure.RemoveElements(ElementCollection);
        else
            Model?.Add(Figure);

        Figure = null;
    }

    /// <summary>この Command で追加する図形の新しいインスタンスを作る。</summary>
    protected abstract Figure CreateFigure();

    // 図形を作り、Model が持つ「現在の図形属性」(色・太さなど) の複製を設定する。
    // 複製するのは、後で Model 側の属性を変えたときに、作成済みの図形まで変わってしまわないようにするため。
    void InitializeFigure()
    {
        Figure = CreateFigure();
        if (Model is not null)
            Figure.Attribute = (FigureAttribute)Model.FigureAttribute.Clone();
    }
}
