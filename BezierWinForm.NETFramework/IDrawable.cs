using System;
using System.Drawing;

namespace BezierWinForm
{
    /// <summary>
    /// 自分自身を描画できるもの (図形や、ドラッグ中の曲線を持つ Controller) を表す。
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        /// 指定された Graphics に自分自身を描く。
        /// </summary>
        void Draw(Graphics graphics);
    }

    /// <summary>
    /// 描画内容が変わったことを通知するもの (Model と Controller) を表す。
    /// View はこの通知を受けて再描画する (Observer パターンの Subject 側)。
    /// </summary>
    public interface IDrawableSubject
    {
        /// <summary>
        /// 描画内容が変わったときに発生する。引数は変化した (描き直すべき) IDrawable。
        /// </summary>
        event Action<IDrawable> Update;
    }
}
