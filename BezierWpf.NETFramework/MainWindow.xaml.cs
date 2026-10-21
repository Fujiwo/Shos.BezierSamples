using System.Windows;
using System.Windows.Input;

namespace BezierWpf
{
    /// <summary>
    /// メイン ウィンドウ。View と Controller を結線し、メニューの選択に応じて Controller の Command (描画モード) を切り替える。
    /// </summary>
    public partial class MainWindow : Window
    {
        // メニュー項目から実行する WPF のコマンド (XAML の CommandBinding で FigureLine などのハンドラーに結び付ける)。
        // 図形を描く側の BezierWpf.Command とは別物で、ここではそれを切り替えるきっかけとして使う
        /// <summary>
        /// 描画モードを「線分」に切り替えるメニュー用のコマンド。
        /// </summary>
        public static readonly ICommand LineCommand      = new RoutedCommand("LineCommand"     , typeof(MainWindow));
        /// <summary>
        /// 描画モードを「矩形」に切り替えるメニュー用のコマンド。
        /// </summary>
        public static readonly ICommand RectangleCommand = new RoutedCommand("RectangleCommand", typeof(MainWindow));
        /// <summary>
        /// 描画モードを「曲線」に切り替えるメニュー用のコマンド。
        /// </summary>
        public static readonly ICommand BezierCommand    = new RoutedCommand("BezierCommand"   , typeof(MainWindow));
        
        Controller controller = new Controller();

        /// <summary>
        /// ウィンドウを初期化し、View に Controller を渡して、最初は何も描かない EditCommand にしておく。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            // View.Controller の設定で Controller.Model に Canvas の子要素が渡り、マウスのイベントが Controller に流れるようになる
            view.Controller = controller;
            controller.Command = new EditCommand();
        }

        // 以下は、メニュー選択時に Controller の Command を差し替える (Command パターン)。
        // Controller は受け取ったマウス操作を現在の Command に委譲するだけなので、モードを増やしても Controller は変わらない

        void FigureLine(object sender, ExecutedRoutedEventArgs e)
        {
            controller.Command = new LineCommand();
        }

        void FigureRectangle(object sender, ExecutedRoutedEventArgs e)
        {
            controller.Command = new RectangleCommand();
        }

        void FigureBezier(object sender, ExecutedRoutedEventArgs e)
        {
            controller.Command = new BezierCommand();
        }
    }
}
