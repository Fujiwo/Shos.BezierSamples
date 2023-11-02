namespace WpfCad2Lib.Wpf
{
    public class MVC
    {
        readonly Controller controller = new Controller();
        readonly Model      model      = new Model     ();
        readonly View       view;

        public Model Model { get { return model; } }

        public Command Command
        {
            set { controller.Command = value; }
        }

        public MVC(View view, Command defaultCommand)
        {
            this.view          = view;
            view.Model         = model;
            view.Controller    = controller;
            controller.Command = defaultCommand;
        }
    }
}
