using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf;

/// <summary>
/// MVC の Controller。<see cref="View"/> から受け取ったクリック/ドラッグを、現在の <see cref="Command"/> (描画モード) に委譲する。
/// 自分ではモードごとの処理を持たず、Command を差し替えるだけで振る舞いが変わる (Command パターン)。
/// </summary>
public class Controller : IMouseAcceptor
{
    Command? command = null;

    /// <summary>
    /// 図形を追加する先 (Canvas の子要素)。<see cref="View.Controller"/> の設定時に View が渡す。
    /// 設定した値は現在の Command にも渡す。
    /// </summary>
    // Command を設定した後に Model を設定しても Command に伝わるようにする
    public UIElementCollection? Model
    {
        private get => field;
        set
        {
            field = value;
            command?.Model = field;
        }
    } = null;

    /// <summary>
    /// 現在の描画モード。メニューの選択に応じて <see cref="MainWindow"/> が差し替える。
    /// 新しい Command には、描き込む先として現在の Model を渡す。
    /// </summary>
    public Command? Command
    {
        private get => command;
        set
        {
            command = value;
            command?.Model = Model;
        }
    }

    // 以下の 4 つは、どれも現在の Command にそのまま委譲する (Command が未設定なら何もしない)

    /// <summary>
    /// クリックを現在の Command に委譲する。
    /// </summary>
    public void OnClick(Point position) => Command?.OnClick(position);

    /// <summary>
    /// ドラッグの開始を現在の Command に委譲する。
    /// </summary>
    public void OnDragStart(Point position) => Command?.OnDragStart(position);

    /// <summary>
    /// ドラッグ中の移動を現在の Command に委譲する。
    /// </summary>
    public void OnDragging(Point position) => Command?.OnDragging(position);

    /// <summary>
    /// ドラッグの終了を現在の Command に委譲する。
    /// </summary>
    public void OnDragEnd(Point position) => Command?.OnDragEnd(position);
}
