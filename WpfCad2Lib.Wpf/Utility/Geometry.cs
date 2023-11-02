using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf
{
    public enum Direction
    {
        Left, Right, Up, Down
    }

    public static class GeoHelper
    {
        public static Path BezierPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        {
            return new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPathGeometry(points) };
        }

        public static Path BezierPolygonPath(IList<Point> points, Color color, double strokeThickness = 1.0)
        {
            return new Path { Stroke = new SolidColorBrush(color), StrokeThickness = strokeThickness, Data = BezierPolygonPathGeometry(points) };
        }

        public static PathGeometry BezierPathGeometry(IList<Point> points)
        {
            var pathGeometry = new PathGeometry();

            if (points.Count >= 2) {
                var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
                pathFigure.Segments.Add(new LineSegment(points[1], true));

                var bezierPoints = points.ToBezierPolyline().ToArray();
                if (bezierPoints.Length >= 4) {
                    for (int index = 1; index < bezierPoints.Length; index += 3)
                        pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));
                }
                if (points.Count > 2)
                    pathFigure.Segments.Add(new LineSegment(points[points.Count - 1], true));

                pathGeometry.Figures.Add(pathFigure);
            }
            return pathGeometry;
        }

        public static PathGeometry BezierPolygonPathGeometry(IList<Point> points)
        {
            var pathGeometry = new PathGeometry();

            if (points.Count == 2) {
                var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };
                pathFigure.Segments.Add(new LineSegment(points[1], true));

                pathGeometry.Figures.Add(pathFigure);
            }
            else if (points.Count >= 3) {
                var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = true };

                var bezierPoints = points.ToBezierPolygon().ToArray();
                if (bezierPoints.Length >= 4) {
                    for (int index = 1; index < bezierPoints.Length; index += 3)
                        pathFigure.Segments.Add(new BezierSegment(bezierPoints[index], bezierPoints[index + 1], bezierPoints[index + 2], true));
                }
                if (points.Count > 2)
                    pathFigure.Segments.Add(new LineSegment(points[points.Count - 1], true));

                pathGeometry.Figures.Add(pathFigure);
            }
            return pathGeometry;
        }
    }

    public static class Geo
    {
        public static IEnumerable<Point> ToBezier(Point point1, Point point2, Point point3, Point point4)
        {
            const float a = 1.0f / 3.0f / 2.0f;

            yield return point2;
            yield return point2 + Vector.Multiply(point3 - point1, a);
            yield return point3 + Vector.Multiply(point2 - point4, a);
            yield return point3;
        }

        public static double Average(double value1, double value2)
        { return (value1 + value2) / 2.0; }

        public static Point Center(Point point1, Point point2)
        {
            return point1 + Vector.Multiply(point2 - point1, 0.5);
        }

        public static Point PointByOffset(Point point1, Point point2, double offset)
        {
            var distance = point1.Distance(point2);
            if (distance == 0.0)
                return point1;
            var parameter = offset / distance;
            return point1.Multiply(1.0 - parameter).Add(point2.Multiply(parameter));
        }
    
        public static Point PointByDistance(Point point1, Point point2, double offset)
        {
            var difference = point2 - point1;
            var distance   = difference.Length;

            return distance == 0.0
                   ? point2
                   : new Point(point2.X - offset * difference.Y / distance, point2.Y + offset * difference.X / distance);
        }

        public static Rect? ToRect(this IEnumerable<Point> points)
        {
            if (points.Count() <= 0)
                return null;

            double minimumX = double.MaxValue, minimumY = double.MaxValue, maximumX = -double.MaxValue, maximumY = -double.MaxValue;

            foreach (var point in points) {
                if (point.X < minimumX) minimumX = point.X;
                if (point.X > maximumX) maximumX = point.X;
                if (point.Y < minimumY) minimumY = point.Y;
                if (point.Y > maximumY) maximumY = point.Y;
            }
            return new Rect(minimumX, minimumY, maximumX - minimumX, maximumY - minimumY);
        }

        public static IEnumerable<Point> ToPoints(this Rect rect)
        {
            yield return rect.TopLeft    ;
            yield return rect.BottomLeft ;
            yield return rect.BottomRight;
            yield return rect.TopRight   ;
        }

        public static double Distance(Point point, LineGeometry line)
        {
            var d  = line.EndPoint   - line.StartPoint;
            var d1 = line.StartPoint - point;
            var d2 = line.EndPoint   - point;
            var f0 = d * d1;
            var f1 = d * d2;

            return (f0 > 0.0)
                   ? d1.Length
                   : (f1 < 0.0 ? d2.Length : System.Math.Abs(d.Y * d1.X - d.X * d1.Y) / d.Length);
        }

        public static double Distance(Point point, Rect rect, bool isFilled = false)
        {
            if (isFilled && rect.Contains(point))
                return 0.0;

            var minimumDistance = double.MaxValue;
            var minX = rect.Left  ;
            var minY = rect.Top   ;
            var maxX = rect.Right ;
            var maxY = rect.Bottom;

            var distance = Distance(point, new LineGeometry(new Point(minX, minY), new Point(minX, maxY)));
            if (distance < minimumDistance)
                minimumDistance = distance;
            distance = Distance(point, new LineGeometry(new Point(minX, maxY), new Point(maxX, maxY)));
            if (distance < minimumDistance)
                minimumDistance = distance;
            distance = Distance(point, new LineGeometry(new Point(maxX, maxY), new Point(maxX, minY)));
            if (distance < minimumDistance)
                minimumDistance = distance;
            distance = Distance(point, new LineGeometry(new Point(maxX, minY), new Point(minX, minY)));
            if (distance < minimumDistance)
                minimumDistance = distance;

            return minimumDistance;
        }

        public static double Distance(Point point, IEnumerable<Point> points)
        {
            var minimumDistance = double.MaxValue;
            if (points.Count() > 0) {
                var startPoint = points.First();
                points.Skip(1).ForEach(
                    aPoint => {
                        var distance = Distance(point, new LineGeometry(startPoint, aPoint));
                        if (distance < minimumDistance)
                            minimumDistance = distance;
                        startPoint = aPoint;
                    }
                );
            }
            return minimumDistance;
        }
    }

    //public class Linea
    //{
    //    public Point Start { get; set; }
    //    public Point End   { get; set; }

    //    public Point Center
    //    {
    //        get { return Geometry.Center(Start, End); }
    //    }
    //}

    public static class GeoExtensions
    {
        public static double Distance(this Point point1, Point point2)
        { return (point1 - point2).Length; }

        public static Point Add(this Point point1, Point point2)
        { return new Point(point1.X + point2.X, point1.Y + point2.Y); }

        public static Point Multiply(this Point point, double rate)
        { return new Point(point.X * rate, point.Y * rate); }
        
        public static Point Center(this Rect rect)
        {
            return Geo.Center(rect.TopLeft, rect.BottomRight);
        }

        public static Rect Transform(this Matrix matrix, Rect rect)
        {
            var topLeft     = matrix.Transform(rect.TopLeft    );
            var bottomRight = matrix.Transform(rect.BottomRight);
            return new Rect(topLeft, bottomRight);
        }

        public static bool IsValidForBezier(this Point point, IList<Point> points, double MinimumDistance)
        { return points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance; }

        public static IEnumerable<Point> ToBezierPolyline(this IList<Point> points)
        {
            if (points.Count < 4) {
                foreach (var point in points)
                    yield return point;
            } else { 
                const float a = 1.0f / 3.0f / 2.0f;
                yield return points[1];
                for (var index = 0; index < points.Count - 3; index++) {
                    yield return points[index + 1] + Vector.Multiply(points[index + 2] - points[index + 0], a);
                    yield return points[index + 2] + Vector.Multiply(points[index + 1] - points[index + 3], a);
                    yield return points[index + 2];
                }
            }
        }

        public static IEnumerable<Point> ToBezierPolygon(this IList<Point> points)
        {
            if (points.Count < 3)
                return points;
            var polygonPoints = new List<Point>(points);
            for (var index = 0; index < 3; index++)
                polygonPoints.Add(points[index]);
            return polygonPoints.ToBezierPolyline();
        }
    }
}
