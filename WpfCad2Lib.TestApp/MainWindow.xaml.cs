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

        void SettingSize(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is not null) {
                switch (menuItem.Header.ToString()) {
                    case "大":
                        mvc.Model.FigureAttribute.StrokeThickness = Common.DefaultStrokeThickness * 2.0;
                        mvc.Model.FigureAttribute.FontSize        = Common.DefaultFontSize        * 2.0;
                        break;
                    case "中":
                        mvc.Model.FigureAttribute.StrokeThickness = Common.DefaultStrokeThickness;
                        mvc.Model.FigureAttribute.FontSize        = Common.DefaultFontSize       ;
                        break;
                    case "小":
                        mvc.Model.FigureAttribute.StrokeThickness = Common.DefaultStrokeThickness / 2.0;
                        mvc.Model.FigureAttribute.FontSize        = Common.DefaultFontSize        / 2.0;
                        break;
                }
            }
        }

        void SettingColor(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem is not null) {
                switch (menuItem.Header.ToString()) {
                    case "赤": mvc.Model.FigureAttribute.SetColor(Colors.Red  ); break;
                    case "緑": mvc.Model.FigureAttribute.SetColor(Colors.Green); break;
                    case "青": mvc.Model.FigureAttribute.SetColor(Colors.Blue ); break;
                    case "白": mvc.Model.FigureAttribute.SetColor(Colors.White); break;
                    case "黒": mvc.Model.FigureAttribute.SetColor(Colors.Black); break;
                }
            }
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
