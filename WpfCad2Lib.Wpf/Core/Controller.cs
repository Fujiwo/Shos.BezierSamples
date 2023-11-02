using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf
{
    public class Controller : IMouseAccepter
    {
        Command command = null;

        public Model               Model             { private get; set; }
        public UIElementCollection ElementCollection { private get; set; }

        public Command Command
        {
            set
            {
                command                   = value;
                Debug.Assert(Model  != null);
                command.Model             = Model;
                Debug.Assert(ElementCollection != null);
                command.ElementCollection = ElementCollection;
                command.OnStart();
            }
        }

        public void OnClick(Point position)
        {
            Debug.WriteLine("Controller.OnClick({0}, {1})", position.X, position.Y);

            if (command != null && command.canClick)
                command.OnClick(position);
        }

        public void OnDragStart(Point position)
        {
            if (command != null && command.canDrag)
                command.OnDragStart(position);
        }

        public void OnDragging(Point position)
        {
            if (command != null && command.canDrag)
                command.OnDragging(position);
        }

        public void OnDragEnd(Point position)
        {
            if (command != null && command.canDrag)
                command.OnDragEnd(position);
        }
    }
}
