using System.Collections.Generic;
using System.Drawing;

namespace BezierWinForm
{
    public abstract class Figure : IDrawable
    {
        public abstract void Draw(Graphics graphics);
    }

    public class BezierLine : Figure
    {
        const float minimumDistance = 10.0f;

        List<PointF> position = new List<PointF>();

        public bool Add(PointF point)
        {
            if (point.IsValidForBezier(position, minimumDistance)) {
                position.Add(point);
                return true;
            }
            return false;

            //position.Add(point);
            //return true;
        }

        public override void Draw(Graphics graphics)
        {
            using (var pen = new Pen(Color.Black)) {
                //if (position.Count > 2)
                //    graphics.DrawCurve(pen, position.ToArray());
                GraphicHelper.DrawBezierLine(graphics, pen, position.ToArray());
                //GraphicHelper.DrawBezierPolygon(graphics, pen, position.ToArray());
            }
        }

    }
}
