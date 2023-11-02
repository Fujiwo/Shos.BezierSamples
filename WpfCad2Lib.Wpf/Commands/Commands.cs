using System.Windows;

namespace WpfCad2Lib.Wpf
{
    public class EditCommand : Command
    {
        public override bool canClick { get { return true; } }
        public override bool canDrag { get { return false; } }
    
        public override void OnClick(Point position)
        {
            Model.Select(ElementCollection, position);
        }
    }

    public class LineCommand : AddCommand
    {
        protected override Figure CreateFigure()
        {
            return new LineFigure();
        }
    }

    public class RectangleCommand : AddCommand
    {
        protected override Figure CreateFigure()
        {
            return new RectangleFigure();
        }
    }
    
    public class EllipseCommand : AddCommand
    {
        protected override Figure CreateFigure()
        {
            return new EllipseFigure();
        }
    }

    public class CurveCommand : AddCommand
    {
        protected override Figure CreateFigure()
        {
            return new CurveFigure();
        }
    }

    public class ArrowCommand : AddCommand
    {
        protected override Figure CreateFigure()
        {
            return new ArrowFigure();
        }
    }

    public class TextCommand : AddCommand
    {
        static string text = string.Empty;

        public override bool canClick { get { return true ; } }
        public override bool canDrag  { get { return false; } }

        public override bool OnStart()
        {
            text = "あいうえお";
            return true;
        }

        protected override Figure CreateFigure()
        {
            return new TextFigure { Text = text };
        }
    }
}
