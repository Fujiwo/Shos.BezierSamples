using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf
{
    public class LineFigure : Figure
    {
        readonly LineGeometry position = new LineGeometry();

        public override bool IsValid
        {
            get { return position.StartPoint != position.EndPoint; }
        }

        protected override Rect? Bounds
        {
            get { return new Rect(position.StartPoint, position.EndPoint); }
        }

        protected override double GetDistance(Point point, double nearDistance)
        {
            return Geo.Distance(point, position);
        }

        protected override IEnumerable<Point> SelectorPoints
        {
            get
            {
                yield return position.StartPoint;
                yield return position.EndPoint  ;
            }
        }

        protected override IEnumerable<FrameworkElement> GetInitialElements()
        {
            return new List<FrameworkElement> { new Line() };
        }

        protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
        {
            position.StartPoint = point;
            var element         = elements.First();
            ((Line)element).X1  = point.X;
            ((Line)element).Y1  = point.Y;
        }

        protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
        {
            position.EndPoint  = point;
            var element        = elements.First();
            ((Line)element).X2 = point.X;
            ((Line)element).Y2 = point.Y;
        }
    }

    public abstract class RectFigure : Figure
    {
        Rect  position = new Rect();
        Point startPosition;

        public override bool IsValid
        {
            get { return position.Width != 0.0 && position.Height != 0.0; }
        }

        protected override Rect? Bounds
        {
            get { return position; }
        }

        protected override double GetDistance(Point point, double nearDistance)
        {
            return Geo.Distance(point, position);
        }

        protected override IEnumerable<Point> SelectorPoints
        {
            get { return position.ToPoints(); }
        }

        protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
        {
            startPosition = point;
            Set(elements, point);
        }

        protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
        {
            Set(elements, point);
        }

        void Set(IEnumerable<FrameworkElement> elements, Point point)
        {
            position = new Rect(startPosition, point);
            Set(elements.First(), position);
        }

        void Set(FrameworkElement element, Rect position)
        {
            Canvas.SetLeft(element, position.Left);
            Canvas.SetTop (element, position.Top );
            element.Width  = position.Width;
            element.Height = position.Height;
        }
    }

    public class RectangleFigure : RectFigure
    {
        protected override IEnumerable<FrameworkElement> GetInitialElements()
        {
            return new List<FrameworkElement> { new Rectangle() };
        }
    }

    public class EllipseFigure : RectFigure
    {
        protected override IEnumerable<FrameworkElement> GetInitialElements()
        {
            return new List<FrameworkElement> { new Ellipse() };
        }
    }

    public class CurveFigure : Figure
    {
        const double sensitivity = 8.0;
        readonly List<Point> position = new List<Point>();
        PathFigure?          pathFigure;

        protected override Rect? Bounds
        {
            get { return position.Count == 0 ? null : position.ToRect(); }
        }

        public override bool IsValid
        {
            get { return position.Count > 2; }
        }

        protected override double GetDistance(Point point, double nearDistance)
        {
            return position.Count == 0 ? double.MaxValue : Geo.Distance(point, position);
        }

        protected override IEnumerable<Point> SelectorPoints
        {
            get
            {
                if (IsValid) {
                    yield return position[0];
                    yield return position[position.Count - 1];
                }
            }
        }

        protected override IEnumerable<FrameworkElement> GetInitialElements()
        {
            return new List<FrameworkElement> { new Path() };
        }

        protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
        {
            position.Clear();
            position.Add(point);
            pathFigure = null;
        }

        protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
        {
            if (position.Count > 0) {
                var lastPoint = position[position.Count - 1];
                if ((point - lastPoint).Length > sensitivity) {
                    position.Add(point);
                    AppendSegment((Path)elements.First());
                }
            }
        }

        // ドラッグの終点は間隔に関係なく必ず通るようにする (直前の点に近すぎる場合はその点を置き換える)
        protected override void SetLast(IEnumerable<FrameworkElement> elements, Point point)
        {
            var lastPoint = position[position.Count - 1];
            if (position.Count >= 2 && (point - lastPoint).Length <= sensitivity)
                position[position.Count - 1] = point;
            else if (point != lastPoint)
                position.Add(point);
            Rebuild((Path)elements.First());
        }

        // 点を 1 つ足すと、末尾の直線が曲線になり、その先に新しい直線が付く。
        // 毎回全体を作り直すと点数 n に対して O(n²) になるため、末尾だけを差し替える
        void AppendSegment(Path element)
        {
            if (pathFigure is null || position.Count <= 3) {
                Rebuild(element);
                return;
            }
            var count        = position.Count;
            var bezierPoints = Geo.ToBezier(position[count - 4], position[count - 3], position[count - 2], position[count - 1]).ToArray();
            pathFigure.Segments.RemoveAt(pathFigure.Segments.Count - 1);
            pathFigure.Segments.Add(new BezierSegment(bezierPoints[1], bezierPoints[2], bezierPoints[3], true));
            pathFigure.Segments.Add(new LineSegment(position[count - 1], true));
        }

        void Rebuild(Path element)
        {
            if (position.Count <= 2) {
                pathFigure = null;
                return;
            }
            var pathGeometry = GeoHelper.BezierPathGeometry(position);
            pathFigure       = pathGeometry.Figures[0];
            element.Data     = pathGeometry;
        }
    }

    public class ArrowFigure : Figure
    {
        const double rate0 = 200.0;
        const double rate1 = 0.2;
        const double rate2 = 0.15;
        const double rate3 = 0.1;
        const double rate4 = 0.25;
        
        readonly LineGeometry position = new LineGeometry();

        public override bool IsValid
        {
            get { return position.StartPoint != position.EndPoint; }
        }

        public override bool IsFilled
        {
            get { return true; }
        }
        
        protected override Rect? Bounds
        {
            get { return new Rect(position.StartPoint, position.EndPoint); }
        }
        
        protected override double GetDistance(Point point, double nearDistance)
        {
            return Geo.Distance(point, position);
        }

        protected override IEnumerable<Point> SelectorPoints
        {
            get
            {
                yield return position.StartPoint;
                yield return position.EndPoint;
            }
        }

        protected override IEnumerable<FrameworkElement> GetInitialElements()
        {
            return new List<FrameworkElement> { new Polygon() };
        }

        protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
        {
            position.StartPoint = point;
        }

        protected override void SetNext(IEnumerable<FrameworkElement> elements, Point point)
        {
            position.EndPoint         = point;

            var distance              = position.StartPoint.Distance(position.EndPoint);
            var maxDistance           = rate0;
            if (distance > maxDistance)
                distance = maxDistance;

            var innerPoint            = Geo.PointByOffset  (position.EndPoint  , position.StartPoint,  distance * rate1);
            var leftPoint             = Geo.PointByDistance(position.StartPoint, innerPoint         , -distance * rate2);
            var rightPoint            = Geo.PointByDistance(position.StartPoint, innerPoint         ,  distance * rate2);
            var leftInnerPoint        = Geo.PointByOffset  (innerPoint         , leftPoint          ,  distance * rate3);
            var rightInnerPoint       = Geo.PointByOffset  (innerPoint         , rightPoint         ,  distance * rate3);

            var points                = new PointCollection {position.StartPoint, leftInnerPoint, leftPoint, position.EndPoint, rightPoint, rightInnerPoint };

            var element               = (Polygon)elements.First();
            element.Points            = points;
        }
    }

    public class TextFigure : Figure
    {
        public const string DefaultText = "テキスト";

        Point  position = new Point();
        string text     = DefaultText;

        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        public override bool IsValid
        {
            get { return text.Length > 0; }
        }

        public override bool IsFilled
        {
            get { return true; }
        }
        
        protected override Rect? Bounds
        {
            get { return new Rect(position, new Size { Width = Attribute.FontSize * text.Length, Height = Attribute.FontSize }); }
        }
        
        protected override double GetDistance(Point point, double nearDistance)
        {
            var bounds = Bounds;
            return bounds is null ? double.MaxValue : Geo.Distance(point, bounds.Value, true);
        }

        protected override IEnumerable<Point> SelectorPoints
        {
            get
            {
                yield return position;
            }
        }

        protected override IEnumerable<FrameworkElement> GetInitialElements()
        {
            return new List<FrameworkElement> { new OutlineText() };
        }

        protected override void SetFirst(IEnumerable<FrameworkElement> elements, Point point)
        {
            position       = point;
            var element    = (OutlineText)elements.First();
            element.Text   = Text;
            Canvas.SetLeft(element, position.X);
            Canvas.SetTop (element, position.Y);
        }
    }
}
