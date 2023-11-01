using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    public class Controller : IMouseAccepter
    {
        Command command = null;

        public UIElementCollection Model { private get; set; }

        public Command Command
        {
            private get
            {
                return command;
            }
            set
            {
                command = value;
                command.Model = Model;
            }
        }

        public void OnClick(Point position)
        {
            if (Command != null)
                Command.OnClick(position);
        }

        public void OnDragStart(Point position)
        {
            if (Command != null)
                Command.OnDragStart(position);
        }

        public void OnDragging(Point position)
        {
            if (Command != null)
                Command.OnDragging(position);
        }

        public void OnDragEnd(Point position)
        {
            if (Command != null)
                Command.OnDragEnd(position);
        }
    }
}
