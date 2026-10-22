using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// View から受け取ったクリック/ドラッグを、現在の <see cref="Command"/> に委譲する。
/// 自身は描画モードごとの処理を持たず、Command を差し替えることで振る舞いが変わる (State/Strategy パターン)。
/// </summary>
public class Controller : IMouseAcceptor
{
    Command? command;
    Command? dragCommand;

    /// <summary>操作対象の Model。View.Controller のセッターから設定される。</summary>
    public Model?               Model             { private get; set; }
    /// <summary>図形の描画先 (View の Canvas.Children)。View.Controller のセッターから設定される。</summary>
    public UIElementCollection? ElementCollection { private get; set; }

    /// <summary>
    /// 現在の Command。設定すると Model と描画先を Command に渡し、<see cref="Wpf.Command.OnStart"/> を呼ぶ。
    /// ドラッグ中の変更は次の操作から使い、現在のドラッグは開始時の Command で完了する。
    /// </summary>
    /// <remarks>Model と ElementCollection を先に設定しておく必要がある (MVC のコンストラクターがこの順で設定する)。</remarks>
    public Command Command
    {
        set
        {
            command = value;

            Debug.Assert(Model is not null);
            Debug.Assert(ElementCollection is not null);

            command.Model             = Model;
            command.ElementCollection = ElementCollection;
            command.OnStart();
        }
    }

    // クリックとドラッグ開始は、Command がその操作を受け付ける場合だけ委譲する。
    // 例えば EditCommand はドラッグを無視し、図形追加の Command はクリックを無視する。

    /// <summary>クリックを Command に委譲する。</summary>
    public void OnClick(Point position)
    {
        Debug.WriteLine($"Controller.OnClick({position.X}, {position.Y})");

        if (command is not null && command.CanClick)
            command.OnClick(position);
    }

    /// <summary>ドラッグ開始を Command に委譲する。</summary>
    public void OnDragStart(Point position)
    {
        dragCommand = command is not null && command.CanDrag ? command : null;
        dragCommand?.OnDragStart(position);
    }

    /// <summary>ドラッグ中の移動を Command に委譲する。</summary>
    public void OnDragging(Point position)
    {
        dragCommand?.OnDragging(position);
    }

    /// <summary>ドラッグ終了を Command に委譲する。</summary>
    public void OnDragEnd(Point position)
    {
        var endingCommand = dragCommand;
        dragCommand = null;
        endingCommand?.OnDragEnd(position);
    }
}
