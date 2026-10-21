using System.Windows;

namespace WpfCad2Lib.Wpf
{
    public class EditCommand : Command
    {
        public override bool CanClick { get { return true; } }
        public override bool CanDrag { get { return false; } }
    
        public override void OnClick(Point position)
        {
            if (Model is not null && ElementCollection is not null)
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
        public string Text { get; set; } = TextFigure.DefaultText;

        public override bool CanClick { get { return true ; } }
        public override bool CanDrag  { get { return false; } }

        protected override Figure CreateFigure()
        {
            return new TextFigure { Text = Text };
        }
    }
}
