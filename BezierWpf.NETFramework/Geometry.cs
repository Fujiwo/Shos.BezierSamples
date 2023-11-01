using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    public static class GraphicHelper
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

        //public static void DrawBezierPolygon(Graphics graphics, Pen pen, IList<PointF> points)
        //{
        //    if (points.Count == 2) {
        //        graphics.DrawLines(pen, points.ToArray());
        //    }
        //    else if (points.Count >= 3) {
        //        var bezierPoints = points.ToBezierPolygon().ToArray();
        //        if (bezierPoints.Length >= 4)
        //            graphics.DrawBeziers(pen, bezierPoints);
        //    }
        //}

        //public static void DrawBezierLine(Graphics graphics, Pen pen, IList<PointF> points)
        //{
        //    if (points.Count < 2)
        //        return;

        //    graphics.DrawLine(pen, points[0], points[1]);

        //    var bezierPoints = points.ToBezierPolyline().ToArray();
        //    if (bezierPoints.Length >= 4)
        //        graphics.DrawBeziers(pen, bezierPoints);

        //    //for (var index = 0; index < points.Length - 3; index++) {
        //    //    var bezierPoints = Geometry.ToBezier(points[index], points[index + 1], points[index + 2], points[index + 3]).ToArray();
        //    //    if (bezierPoints.Length >= 4)
        //    //        graphics.DrawBezier(pen, bezierPoints[0], bezierPoints[1], bezierPoints[2], bezierPoints[3]);
        //    //}

        //    if (points.Count > 2)
        //        graphics.DrawLine(pen, points[points.Count - 2], points[points.Count - 1]);
        //}
    }

    public static class Geometry
    {
        public static IEnumerable<Point> ToBezier(Point point1, Point point2, Point point3, Point point4)
        {
            const float a = 1.0f / 3.0f / 2.0f;

            yield return point2;
            yield return point2.Add(point3.Subtract(point1).Multiply(a));
            yield return point3.Add(point2.Subtract(point4).Multiply(a));
            yield return point3;
        }

        public static double Average(double value1, double value2)
        { return (value1 + value2) / 2.0; }
    }

    public static class GeometryExtensions
    {
        public static Point Center(this Rect rect)
        { return new Point { X = Geometry.Average(rect.Left, rect.Right), Y = Geometry.Average(rect.Top, rect.Bottom) }; }

        public static double Distance(this Point point1, Point point2)
        { return point1.Subtract(point2).Absolute(); }

        public static Point Add(this Point point, Point size)
        { return new Point(point.X + size.X, point.Y + size.Y); }

        public static Point Subtract(this Point point1, Point point2)
        { return new Point(point1.X - point2.X, point1.Y - point2.Y); }

        public static Point Multiply(this Point point, double rate)
        { return new Point(point.X * rate, point.Y * rate);  }

        public static double Absolute(this Point point)
        { return (point.X.Square() + point.Y.Square()).SquareRoot(); }

        public static double Square(this double value)
        { return value * value; }

        public static double SquareRoot(this double value)
        { return Math.Sqrt(value); }

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
                    yield return points[index + 1].Add(points[index + 2].Subtract(points[index + 0]).Multiply(a));
                    yield return points[index + 2].Add(points[index + 1].Subtract(points[index + 3]).Multiply(a));
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
