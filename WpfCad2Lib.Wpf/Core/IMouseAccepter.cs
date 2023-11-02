using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf
{
    public interface IMouseAccepter
    {
        Model               Model             { set; }
        UIElementCollection ElementCollection { set; }
        
        void OnClick    (Point position);
        void OnDragStart(Point position);
        void OnDragging (Point position);
        void OnDragEnd  (Point position);
    }
}
