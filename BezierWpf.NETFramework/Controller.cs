using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    /// <summary>
    /// MVC の Controller。<see cref="View"/> から受け取ったクリック/ドラッグを、現在の <see cref="Command"/> (描画モード) に委譲する。
    /// 自分ではモードごとの処理を持たず、Command を差し替えるだけで振る舞いが変わる (Command パターン)。
    /// </summary>
    public class Controller : IMouseAcceptor
    {
        Command             command = null;
        Command             dragCommand;
        UIElementCollection model   = null;

        /// <summary>
        /// 図形を追加する先 (Canvas の子要素)。<see cref="View.Controller"/> の設定時に View が渡す。
        /// 設定した値は現在の Command にも渡す。
        /// </summary>
        // Command を設定した後に Model を設定しても Command に伝わるようにする
        public UIElementCollection Model
        {
            private get
            {
                return model;
            }
            set
            {
                model = value;
                if (command != null)
                    command.Model = model;
            }
        }

        /// <summary>
        /// 現在の描画モード。メニューの選択に応じて <see cref="MainWindow"/> が差し替える。
        /// 新しい Command には、描き込む先として現在の Model を渡す。
        /// ドラッグ中の変更は次の操作から使い、現在のドラッグは開始時の Command で完了する。
        /// </summary>
        public Command Command
        {
            private get
            {
                return command;
            }
            set
            {
                command = value;
                if (command != null)
                    command.Model = Model;
            }
        }

        /// <summary>
        /// クリックを現在の Command に委譲する。
        /// </summary>
        public void OnClick(Point position)
        {
            if (Command != null)
                Command.OnClick(position);
        }

        /// <summary>
        /// ドラッグの通知先を固定して開始する。
        /// </summary>
        public void OnDragStart(Point position)
        {
            dragCommand = Command;
            if (dragCommand != null)
                dragCommand.OnDragStart(position);
        }

        /// <summary>
        /// ドラッグ中の移動を開始時の Command に委譲する。
        /// </summary>
        public void OnDragging(Point position)
        {
            if (dragCommand != null)
                dragCommand.OnDragging(position);
        }

        /// <summary>
        /// 開始時の Command でドラッグを終了し、通知先の参照を解放する。
        /// </summary>
        public void OnDragEnd(Point position)
        {
            var endingCommand = dragCommand;
            dragCommand = null;
            if (endingCommand != null)
                endingCommand.OnDragEnd(position);
        }
    }
}
