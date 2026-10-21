using System.Collections.Generic;
using System.Drawing;

namespace BezierWinForm
{
    /// <summary>
    /// Model に保持される図形の基底クラス。
    /// </summary>
    public abstract class Figure : IDrawable
    {
        /// <summary>
        /// 指定された Graphics に図形を描く。
        /// </summary>
        public abstract void Draw(Graphics graphics);
    }

    /// <summary>
    /// ドラッグの軌跡から集めた通過点をすべて通る、滑らかな曲線。
    /// </summary>
    public class BezierLine : Figure
    {
        // 点の間引きの距離 (ピクセル)。直前の点からこれ以上離れたときだけ通過点を追加する。
        // マウスの細かい揺れまで拾うと、通過点が密集して曲線が波打つため。
        const float minimumDistance = 10.0f;

        // 曲線の通過点 (ドラッグした順)。
        List<PointF> position = new List<PointF>();

        /// <summary>
        /// 直前の通過点から minimumDistance 以上離れていれば、point を通過点として追加する。
        /// </summary>
        /// <returns>追加した (曲線の形が変わった) ときは true、近すぎて追加しなかったときは false。</returns>
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
        /// <summary>
        /// ドラッグを終えた位置 point を曲線の終点にする。
        /// </summary>
        public void AddLast(PointF point)
        {
            // 通過点が 2 個以上あるときは、直前の点を置き換えても始点は残る。
            // 通過点が 1 個 (始点だけ) のときは、始点を消さないよう置き換えずに追加する。
            // ただし、同じ位置の点を重ねて追加はしない (クリックしただけのときなど)。
            if (position.Count >= 2 && !point.IsValidForBezier(position, minimumDistance))
                position[position.Count - 1] = point;
            else if (position.Count == 0 || position[position.Count - 1] != point)
                position.Add(point);
        }

        /// <summary>
        /// 通過点をすべて通る 3 次スプライン曲線を描く。最初と最後の区間も含め、すべての区間がベジエ曲線になる。
        /// </summary>
        public override void Draw(Graphics graphics)
        {
            //if (position.Count > 2)
            //    graphics.DrawCurve(Pens.Black, position.ToArray());
            GraphicHelper.DrawBezierLine(graphics, Pens.Black, position.ToArray());
            //GraphicHelper.DrawBezierPolygon(graphics, Pens.Black, position.ToArray());
        }

    }
}
