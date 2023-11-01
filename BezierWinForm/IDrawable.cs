using System;
using System.Drawing;

namespace BezierWinForm
{
    public interface IDrawable
    {
        void Draw(Graphics graphics);
    }

    public interface IDrawableSubject
    {
        event Action<IDrawable> Update;
    }
}
