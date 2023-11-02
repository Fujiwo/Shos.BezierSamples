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

            //new BitmapImage(new Uri("http://www.sokenss.co.jp/works/%E6%96%BD%E5%B7%A5%E4%BA%8B%E4%BE%8B%E2%91%A3.jpeg", UriKind.RelativeOrAbsolute));
            //var bitmap = new BitmapImage();
            //bitmap.BeginInit();
            //bitmap.UriSource = new Uri(@"sample.jpg", UriKind.Relative);
            //bitmap.EndInit();
            //image.Source = new BitmapImage(new Uri("http://www.sokenss.co.jp/works/%E6%96%BD%E5%B7%A5%E4%BA%8B%E4%BE%8B%E2%91%A3.jpeg", UriKind.RelativeOrAbsolute));
            //image.Source = new BitmapImage(new Uri("http://www.miyamoto-gumi.com/wp-content/uploads/2012/04/011.jpg", UriKind.RelativeOrAbsolute));
            //image.Source = new BitmapImage(new Uri(@"Resoures\sample.jpg", UriKind.RelativeOrAbsolute));
            //webBrowser.Navigate(new System.Uri(@"file:///C:/Users/g_kojima_fujio.FC/Dropbox/Source/WpfAppSample.2013.2.19/WpfCad2Lib.TestApp/Resources/sample.pdf", System.UriKind.RelativeOrAbsolute));
            //webBrowser.Navigate(new System.Uri(@"http://www.miyamoto-gumi.com/wp-content/uploads/2012/04/011.jpg", System.UriKind.RelativeOrAbsolute));

            mvc = new MVC(view, new EditCommand());
            var bitmap = new BitmapImage(new Uri(GetImagePath(), UriKind.Absolute));
            view.Image.Source = bitmap;
            SetModelArea(new Rect(new Size(bitmap.Width, bitmap.Height)));
        }

        static string GetImagePath()
        {
            var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(location), @"Resources\sample.jpg");
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
            if (menuItem != null) {
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
            if (menuItem != null) {
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
