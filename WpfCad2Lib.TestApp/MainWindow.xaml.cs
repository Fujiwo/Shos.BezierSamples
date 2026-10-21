using WpfCad2Lib.Wpf;
using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfCad2Lib.TestApp
{
    public partial class MainWindow : Window
    {
        public static readonly ICommand EditCommand      = new RoutedCommand("EditCommand"     , typeof(MainWindow));
        public static readonly ICommand LineCommand      = new RoutedCommand("LineCommand"     , typeof(MainWindow));
        public static readonly ICommand RectangleCommand = new RoutedCommand("RectangleCommand", typeof(MainWindow));
        public static readonly ICommand EllipseCommand   = new RoutedCommand("EllipseCommand"  , typeof(MainWindow));
        public static readonly ICommand CurveCommand     = new RoutedCommand("CurveCommand"    , typeof(MainWindow));
        public static readonly ICommand ArrowCommand     = new RoutedCommand("ArrowCommand"    , typeof(MainWindow));
        public static readonly ICommand TextCommand      = new RoutedCommand("TextCommand"     , typeof(MainWindow));

        readonly MVC                    mvc;

        public MainWindow()
        {
            InitializeComponent();

            mvc = new MVC(view, new EditCommand());
            var imagePath = GetImagePath();
            if (System.IO.File.Exists(imagePath)) {
                var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                view.Image.Source = bitmap;
                SetModelArea(new Rect(new Size(bitmap.Width, bitmap.Height)));
            } else {
                // 背景画像が無くても起動できるよう、Model の既定の領域を使う
                SetModelArea(mvc.Model.Area);
            }
        }

        static string GetImagePath()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var directory = System.IO.Path.GetDirectoryName(location) ?? AppContext.BaseDirectory;
            var path = System.IO.Path.Combine(directory, @"Resources\sample.jpg");
            return path;
        }

        void FigureSelect(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new EditCommand();
        }

        void FigureLine(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new LineCommand();
        }

        void FigureRectangle(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new RectangleCommand();
        }

        void FigureEllipse(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new EllipseCommand();
        }

        void FigureCurve(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new CurveCommand();
        }

        void FigureArrow(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new ArrowCommand();
        }

        void FigureText(object sender, ExecutedRoutedEventArgs e)
        {
            mvc.Command = new TextCommand();
        }

        // 表示文字列 (Header) ではなく Tag で分岐する
        void SettingSize(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is not null) {
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

        void SettingColor(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem { Tag: Color color })
                mvc.Model.FigureAttribute.SetColor(color);
        }

        void DisplayHome(object sender, RoutedEventArgs e)
        {
            view.Home();
        }

        void FilePdf(object sender, RoutedEventArgs e)
        {
            //var openFileDialog = new OpenFileDialog { DefaultExt = ".pdf", Filter = "PDF ファイル (.pdf)|*.pdf" };
            //openFileDialog.ShowDialog();
            //if (!string.IsNullOrEmpty(openFileDialog.FileName))
            //    view.PdfFilePath = openFileDialog.FileName;
        }

        void SetModelArea(Rect modelArea)
        {
            mvc.Model.Area = modelArea;
            view.AddModelAreaRect();
        }
    }
}
