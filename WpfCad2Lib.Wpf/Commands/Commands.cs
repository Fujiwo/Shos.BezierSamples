using System.Windows;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 選択モード。クリックした位置に最も近い図形を選択し (選択マークを表示し)、他の図形の選択を外す。
/// 近くに図形が無ければすべての選択を外す。ドラッグは受け付けない。
/// </summary>
public class EditCommand : Command
{
    /// <inheritdoc/>
    public override bool CanClick => true;
    /// <inheritdoc/>
    public override bool CanDrag => false;

    /// <summary>クリック位置の近くにある図形を選択する。</summary>
    public override void OnClick(Point position)
    {
        if (Model is not null && ElementCollection is not null)
            Model.Select(ElementCollection, position);
    }
}

// 以下の図形追加 Command は、どの図形を作るかだけを決める。追加の流れは AddCommand にある。

/// <summary>ドラッグで線分を追加する。</summary>
public class LineCommand : AddCommand
{
    /// <inheritdoc/>
    protected override Figure CreateFigure() => new LineFigure();
}

/// <summary>ドラッグで矩形を追加する。ドラッグの開始位置と現在位置が対角の 2 頂点になる。</summary>
public class RectangleCommand : AddCommand
{
    /// <inheritdoc/>
    protected override Figure CreateFigure() => new RectangleFigure();
}

/// <summary>ドラッグで楕円を追加する。ドラッグの開始位置と現在位置を対角とする矩形に内接する。</summary>
public class EllipseCommand : AddCommand
{
    /// <inheritdoc/>
    protected override Figure CreateFigure() => new EllipseFigure();
}

/// <summary>ドラッグで自由曲線を追加する。マウスの軌跡を通過点とし、ベジエ曲線でなめらかにつなぐ。</summary>
public class CurveCommand : AddCommand
{
    /// <inheritdoc/>
    protected override Figure CreateFigure() => new CurveFigure();
}

/// <summary>ドラッグで矢印を追加する。ドラッグの開始位置から現在位置へ向かう矢印になる。</summary>
public class ArrowCommand : AddCommand
{
    /// <inheritdoc/>
    protected override Figure CreateFigure() => new ArrowFigure();
}

/// <summary>クリックした位置に文字列 (袋文字) を追加する。ドラッグは受け付けない。</summary>
public class TextCommand : AddCommand
{
    /// <summary>追加する文字列。図形を作るたびにこの値が使われる。</summary>
    public string Text { get; set; } = TextFigure.DefaultText;

    /// <inheritdoc/>
    public override bool CanClick => true ;
    /// <inheritdoc/>
    public override bool CanDrag  => false;

    /// <inheritdoc/>
    protected override Figure CreateFigure() => new TextFigure { Text = Text };
}
