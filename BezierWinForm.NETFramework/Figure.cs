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

        // ドラッグの終点は間隔に関係なく必ず通るようにする (直前の点に近すぎる場合はその点を置き換える)
        public void AddLast(PointF point)
        {
            if (position.Count >= 2 && !point.IsValidForBezier(position, minimumDistance))
                position[position.Count - 1] = point;
            else if (position.Count == 0 || position[position.Count - 1] != point)
                position.Add(point);
        }

        public override void Draw(Graphics graphics)
        {
            //if (position.Count > 2)
            //    graphics.DrawCurve(Pens.Black, position.ToArray());
            GraphicHelper.DrawBezierLine(graphics, Pens.Black, position.ToArray());
            //GraphicHelper.DrawBezierPolygon(graphics, Pens.Black, position.ToArray());
        }

    }
}
