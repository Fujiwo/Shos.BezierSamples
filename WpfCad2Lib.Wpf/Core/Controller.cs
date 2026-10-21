using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf
{
    public class Controller : IMouseAcceptor
    {
        Command? command;

        public Model?               Model             { private get; set; }
        public UIElementCollection? ElementCollection { private get; set; }

        public Command Command
        {
            set
            {
                command = value;

                Debug.Assert(Model is not null);
                Debug.Assert(ElementCollection is not null);

                command.Model             = Model;
                command.ElementCollection = ElementCollection;
                command.OnStart();
            }
        }

        public void OnClick(Point position)
        {
            Debug.WriteLine("Controller.OnClick({0}, {1})", position.X, position.Y);

            if (command is not null && command.CanClick)
                command.OnClick(position);
        }

        public void OnDragStart(Point position)
        {
            if (command is not null && command.CanDrag)
                command.OnDragStart(position);
        }

        public void OnDragging(Point position)
        {
            if (command is not null && command.CanDrag)
                command.OnDragging(position);
        }

        public void OnDragEnd(Point position)
        {
            if (command is not null && command.CanDrag)
                command.OnDragEnd(position);
        }
    }
}
