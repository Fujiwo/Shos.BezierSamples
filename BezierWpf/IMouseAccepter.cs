using System.Windows;
using System.Windows.Controls;

namespace BezierWpf
{
    public interface IMouseAccepter
    {
        UIElementCollection Model { set; }

        void OnClick(Point position);
        void OnDragStart(Point position);
        void OnDragging(Point position);
        void OnDragEnd(Point position);
    }
}
