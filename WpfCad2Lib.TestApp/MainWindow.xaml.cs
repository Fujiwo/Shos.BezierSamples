using WpfCad2Lib.Wpf;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfCad2Lib.TestApp;

/// <summary>
/// WpfCad2Lib の動作確認用のメイン ウィンドウ。
/// XAML に置いた <see cref="View"/> (作図領域) を <see cref="MVC"/> で Model / Controller と結線し、
/// メニューの操作で描画モード (<see cref="MVC.Command"/>) や描画属性を切り替える。
/// </summary>
public partial class MainWindow : Window
{
    // 「図形」メニューの各項目に割り当てる RoutedCommand。
    // XAML の CommandBinding で Executed ハンドラー (FigureSelect など) と結び付け、MenuItem.Command から {x:Static} で参照する。
    // Click イベントで直接つながずにコマンドにしておくと、同じ操作をツールバーやショートカット キーにも割り当てられる
    /// <summary>「選択」モードに切り替えるコマンド。</summary>
    public static readonly ICommand EditCommand      = new RoutedCommand("EditCommand"     , typeof(MainWindow));
    /// <summary>「線分」の作図モードに切り替えるコマンド。</summary>
    public static readonly ICommand LineCommand      = new RoutedCommand("LineCommand"     , typeof(MainWindow));
    /// <summary>「矩形」の作図モードに切り替えるコマンド。</summary>
    public static readonly ICommand RectangleCommand = new RoutedCommand("RectangleCommand", typeof(MainWindow));
    /// <summary>「楕円」の作図モードに切り替えるコマンド。</summary>
    public static readonly ICommand EllipseCommand   = new RoutedCommand("EllipseCommand"  , typeof(MainWindow));
    /// <summary>「曲線」の作図モードに切り替えるコマンド。</summary>
    public static readonly ICommand CurveCommand     = new RoutedCommand("CurveCommand"    , typeof(MainWindow));
    /// <summary>「矢印」の作図モードに切り替えるコマンド。</summary>
    public static readonly ICommand ArrowCommand     = new RoutedCommand("ArrowCommand"    , typeof(MainWindow));
    /// <summary>「文字列」の配置モードに切り替えるコマンド。</summary>
    public static readonly ICommand TextCommand      = new RoutedCommand("TextCommand"     , typeof(MainWindow));

    // View・Model・Controller をまとめて持つ。描画モードの切り替え (mvc.Command) と描画属性 (mvc.Model.FigureAttribute) の窓口
    readonly MVC                    mvc;

    /// <summary>
    /// ウィンドウを作り、作図領域を結線して背景画像を表示する。
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        // InitializeComponent() で XAML の View (x:Name="view") ができた後に結線する。最初は「選択」モードで始める
        mvc = new MVC(view, new EditCommand());
        var imagePath = GetImagePath();
        if (System.IO.File.Exists(imagePath)) {
            var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
            view.Image.Source = bitmap;
            // 画像の大きさをモデル領域 (作図の座標系の範囲) にする。ズームの基準や選択の「近さ」もこの大きさから決まる
            SetModelArea(new Rect(new Size(bitmap.Width, bitmap.Height)));
        } else {
            // 背景画像が無くても起動できるよう、Model の既定の領域を使う
            SetModelArea(mvc.Model.Area);
        }
    }

    // 背景画像のパス。ビルド時に出力フォルダーへコピーされた Resources\sample.jpg を、実行ファイルの場所から探す
    static string GetImagePath()
    {
        var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var directory = System.IO.Path.GetDirectoryName(location) ?? AppContext.BaseDirectory;
        var path = System.IO.Path.Combine(directory, @"Resources\sample.jpg");
        return path;
    }

    // 以下の Figure～ は「図形」メニューのコマンドの Executed ハンドラー。
    // 描画モードの切り替えは mvc.Command に新しい Command を設定するだけでよい (以後のマウス操作はそのコマンドが処理する)
    void FigureSelect(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new EditCommand();

    void FigureLine(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new LineCommand();

    void FigureRectangle(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new RectangleCommand();

    void FigureEllipse(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new EllipseCommand();

    void FigureCurve(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new CurveCommand();

    void FigureArrow(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new ArrowCommand();

    void FigureText(object sender, ExecutedRoutedEventArgs e)
        => mvc.Command = new TextCommand();

    // 表示文字列 (Header) ではなく Tag で分岐する
    void SettingSize(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem) {
            switch (menuItem.Tag as string) {
                case "Large":
                    mvc.Model.FigureAttribute.StrokeThickness = Common.DefaultStrokeThickness * 2.0;
                    mvc.Model.FigureAttribute.FontSize        = Common.DefaultFontSize        * 2.0;
                    break;
                case "Medium":
                    mvc.Model.FigureAttribute.StrokeThickness = Common.DefaultStrokeThickness;
                    mvc.Model.FigureAttribute.FontSize        = Common.DefaultFontSize       ;
                    break;
                case "Small":
                    mvc.Model.FigureAttribute.StrokeThickness = Common.DefaultStrokeThickness / 2.0;
                    mvc.Model.FigureAttribute.FontSize        = Common.DefaultFontSize        / 2.0;
                    break;
            }
        }
    }

    // MenuItem.Tag に XAML で {x:Static Colors.Red} などの Color を入れてあり、プロパティ パターンで取り出す。
    // 線と塗りの両方の色を変える。これから描く図形にだけ効き、描き済みの図形は変わらない
    void SettingColor(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: Color color })
            mvc.Model.FigureAttribute.SetColor(color);
    }

    // ズームとパンを初期状態 (モデル領域全体が収まる表示) に戻す
    void DisplayHome(object sender, RoutedEventArgs e)
        => view.Home();

    // PDF の表示は未実装 (メニュー項目だけ残している)
    void FilePdf(object sender, RoutedEventArgs e)
    {
        //var openFileDialog = new OpenFileDialog { DefaultExt = ".pdf", Filter = "PDF ファイル (.pdf)|*.pdf" };
        //openFileDialog.ShowDialog();
        //if (!string.IsNullOrEmpty(openFileDialog.FileName))
        //    view.PdfFilePath = openFileDialog.FileName;
    }

    // モデル領域を設定し、その範囲を示す枠を View に描く
    void SetModelArea(Rect modelArea)
    {
        mvc.Model.Area = modelArea;
        view.AddModelAreaRect();
    }
}
