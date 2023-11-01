using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace BezierWinForm
{
    public static class GraphicHelper
    {
        public static void DrawBezierPolygon(Graphics graphics, Pen pen, IList<PointF> points)
        {
            if (points.Count == 2) {
                graphics.DrawLines(pen, points.ToArray());
            }
            else if (points.Count >= 3) {
                var bezierPoints = points.ToBezierPolygon().ToArray();
                if (bezierPoints.Length >= 4)
                    graphics.DrawBeziers(pen, bezierPoints);
            }
        }

        public static void DrawBezierLine(Graphics graphics, Pen pen, IList<PointF> points)
        {
            if (points.Count < 2)
                return;

            graphics.DrawLine(pen, points[0], points[1]);

            var bezierPoints = points.ToBezierPolyline().ToArray();
            if (bezierPoints.Length >= 4)
                graphics.DrawBeziers(pen, bezierPoints);

            //for (var index = 0; index < points.Length - 3; index++) {
            //    var bezierPoints = Geometry.ToBezier(points[index], points[index + 1], points[index + 2], points[index + 3]).ToArray();
            //    if (bezierPoints.Length >= 4)
            //        graphics.DrawBezier(pen, bezierPoints[0], bezierPoints[1], bezierPoints[2], bezierPoints[3]);
            //}

            if (points.Count > 2)
                graphics.DrawLine(pen, points[points.Count - 2], points[points.Count - 1]);
        }
    }

    public static class Geometry
    {
        public static IEnumerable<PointF> ToBezier(PointF point1, PointF point2, PointF point3, PointF point4)
        {
            const float a = 1.0f / 3.0f / 2.0f;

            yield return point2;
            yield return point2.Add(point3.Subtract(point1).Multiply(a));
            yield return point3.Add(point2.Subtract(point4).Multiply(a));
            yield return point3;
        }
    }

    public static class GeometryExtensions
    {
        public static float Distance(this PointF point1, PointF point2)
        { return point1.Subtract(point2).Absolute(); }

        public static PointF Add(this PointF point, SizeF size)
        { return new PointF(point.X + size.Width, point.Y + size.Height); }

        //public static PointF Subtract(this PointF point, SizeF size)
        //{ return new PointF(point.X - size.Width, point.Y - size.Height); }

        public static SizeF Subtract(this PointF point1, PointF point2)
        { return new SizeF(point1.X - point2.X, point1.Y - point2.Y); }

        public static SizeF Multiply(this SizeF size, float rate)
        { return new SizeF(size.Width * rate, size.Height * rate);  }

        public static float Absolute(this SizeF size)
        { return (size.Width.Square() + size.Height.Square()).SquareRoot(); }

        public static float Square(this float value)
        { return value * value; }

        public static float SquareRoot(this float value)
        { return (float)Math.Sqrt(value); }

        public static bool IsValidForBezier(this PointF point, IList<PointF> points, float MinimumDistance)
        { return points.Count <= 0 || points[points.Count - 1].Distance(point) >= MinimumDistance; }

        //public static IList<PointF> ForBezier(this IList<PointF> points, float minimumDistance)
        //{
        //    var newPoints = new List<PointF>();
        //    foreach (var point in points) {
        //        if (point.IsValidForBezier(newPoints, minimumDistance))
        //            newPoints.Add(point);
        //    }
        //    return newPoints;
        //}

        //public static IEnumerable<PointF> ToBezierPolyline(this IList<PointF> points, float minimumDistance)
        //{ return points.ForBezier(minimumDistance).ToBezierPolyline(); }

        //public static IEnumerable<PointF> ToBezierPolygon(this IList<PointF> points, float minimumDistance)
        //{ return points.ForBezier(minimumDistance).ToBezierPolygon(); }

        public static IEnumerable<PointF> ToBezierPolyline(this IList<PointF> points)
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

        public static IEnumerable<PointF> ToBezierPolygon(this IList<PointF> points)
        {
            if (points.Count < 3)
                return points;
            var polygonPoints = new List<PointF>(points);
            for (var index = 0; index < 3; index++)
                polygonPoints.Add(points[index]);
            return polygonPoints.ToBezierPolyline();
        }
    }
}
