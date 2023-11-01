using System.Windows;
using System.Windows.Input;

namespace BezierWpf
{
    public partial class MainWindow : Window
    {
        public static readonly ICommand LineCommand      = new RoutedCommand("LineCommand"     , typeof(MainWindow));
        public static readonly ICommand RectangleCommand = new RoutedCommand("RectangleCommand", typeof(MainWindow));
        public static readonly ICommand BezierCommand    = new RoutedCommand("BezierCommand"   , typeof(MainWindow));
        
        Controller controller = new Controller();

        public MainWindow()
        {
            InitializeComponent();
            view.Controller = controller;
            controller.Command = new EditCommand();
        }

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
