using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    /// <summary>
    /// View.xaml の相互作用ロジック
    /// </summary>
    public partial class View : UserControl
    {
        MouseEventConverter mouseEventConverter = new MouseEventConverter();
        IMouseAccepter      controller          = null;

        public IMouseAccepter Controller
        {
            set
            {
                controller       = value;
                controller.Model = canvas.Children;
                InitializeInputEventHandlers();
            }
        }

        public View()
        {
            InitializeComponent();
            InitializeMouseEventHandlers();

            //canvas.Children.Add(new Line { X1 = 100, Y1 = 200, X2 = 300, Y2 = 400, Stroke = new SolidColorBrush(Colors.Black) });

            //var pointS = new Point { X = 0, Y = 0 };
            //var pointE = new Point { X = 400, Y = 400 };

            //var point1 = new Point { X = 200, Y = 250 };
            //var point2 = new Point { X = 195, Y = 5 };
            //var point3 = new Point { X = 100, Y = 40 };
            //var point4 = new Point { X = 160, Y = 90 };

            //var pathFigure = new PathFigure { StartPoint = point1, IsClosed = false };
            //pathFigure.Segments.Add(new LineSegment(pointS, true));
            //pathFigure.Segments.Add(new BezierSegment(point2, point3, point4, true));

            //pathFigure.Segments.Add(new LineSegment(pointE, true));

            //var pathGeometry = new PathGeometry();
            //pathGeometry.Figures.Add(pathFigure);

            //var path = new Path { Stroke = new SolidColorBrush(Colors.Black) };
            //path.Data = pathGeometry;

            //canvas.Children.Add(path);
        }

        //static Random random = new Random();

        //Point GetRandomPoint(Rect area)
        //{
        //    return new Point {
        //        X = area.Left + area.Width  * random.NextDouble(),
        //        Y = area.Top  + area.Height * random.NextDouble()
        //    };
        //}

        //Point GetRandomPoint(double maximum)
        //{
        //    return new Point {
        //        X = (random.NextDouble() - 0.5) * 2.0 * maximum,
        //        Y = (random.NextDouble() - 0.5) * 2.0 * maximum
        //    };
        //}

        //void StartButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var points = new PointCollection();
        //    var area = new Rect { X = 0.0, Y = 0.0, Width = canvas.ActualWidth, Height = canvas.ActualHeight };

        //    var point = GetRandomPoint(area);
        //    points.Add(point);

        //    var count = random.Next(10);

        //    for (int index = 0; index < count; index++) {
        //        var d = GetRandomPoint(Geometry.Average(area.Width, area.Height) / 10.0);
        //        var newPoint = point.Add(d);
        //        if (area.Contains(newPoint)) {
        //            point = newPoint;
        //            points.Add(point);
        //        }
        //    }

        //    var polyline = new Polyline { Visibility = Visibility.Visible, Points = points, Stroke = new SolidColorBrush(Colors.Black) };
        //    canvas.Children.Add(polyline);

        //    var path1 = GraphicHelper.BezierPath(points, Colors.Red);
        //    canvas.Children.Add(path1);

        //    var path2 = GraphicHelper.BezierPolygonPath(points, Colors.Green);
        //    canvas.Children.Add(path2);
        //}

        void InitializeMouseEventHandlers()
        {
            canvas.MouseLeftButtonDown += mouseEventConverter.OnMouseLeftButtonDown;
            canvas.MouseMove           += mouseEventConverter.OnMouseMove          ;
            canvas.MouseLeftButtonUp   += mouseEventConverter.OnMouseLeftButtonUp  ;
        }

        void InitializeInputEventHandlers()
        {
            mouseEventConverter.Click     += controller.OnClick    ;
            mouseEventConverter.DragStart += controller.OnDragStart;
            mouseEventConverter.Dragging  += controller.OnDragging ;
            mouseEventConverter.DragEnd   += controller.OnDragEnd  ;
        }
    }
}
