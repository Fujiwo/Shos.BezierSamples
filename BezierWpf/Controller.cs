using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    public class Controller : IMouseAcceptor
    {
        Command?             command = null;
        UIElementCollection? model   = null;

        // Command を設定した後に Model を設定しても Command に伝わるようにする
        public UIElementCollection? Model
        {
            private get
            {
                return model;
            }
            set
            {
                model = value;
                if (command is not null)
                    command.Model = model;
            }
        }

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
