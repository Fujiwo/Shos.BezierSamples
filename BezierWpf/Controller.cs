using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    public class Controller : IMouseAccepter
    {
        Command? command = null;

        public UIElementCollection? Model { private get; set; }

        public Command? Command
        {
            private get
            {
                return command;
            }
            set
            {
                command = value;
                if (command is not null)
                    command.Model = Model;
            }
        }

        public void OnClick(Point position)
        {
            if (Command is not null)
                Command.OnClick(position);
        }

        public void OnDragStart(Point position)
        {
            if (Command is not null)
                Command.OnDragStart(position);
        }

        public void OnDragging(Point position)
        {
            if (Command is not null)
                Command.OnDragging(position);
        }

        public void OnDragEnd(Point position)
        {
            if (Command is not null)
                Command.OnDragEnd(position);
        }
    }
}
