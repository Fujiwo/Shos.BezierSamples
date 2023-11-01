using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    public abstract class Command : IMouseAccepter
    {
        public UIElementCollection Model { protected get; set; }

        public virtual void OnClick    (Point position) {}
        public virtual void OnDragStart(Point position) {}
        public virtual void OnDragging (Point position) {}
        public virtual void OnDragEnd  (Point position) {}
    }

    public class EditCommand : Command
    { }

    public abstract class AddCommand : Command
    {
        const double StrokeThickness = 3.0;

        static Brush Stroke
        {
            get { return new SolidColorBrush(Colors.Black); }
        }

        protected Point StartPosition { get; private set; }
        protected Shape Shape { get; private set; }

        public override void OnDragStart(Point position)
        {
            StartPosition = position;
            Shape = null;
            OnStart();
        }

        public override void OnDragging(Point position)
        {
            if (Shape == null) {
                Shape = CreateShape();
                SetAttribute(Shape);
                if (Model != null)
                    Model.Add(Shape);
            }
            Reshape(position);
        }

        public override void OnDragEnd(Point position)
        {
            Reshape(position);
            Shape = null;
        }

        protected virtual void OnStart() {}
        protected abstract Shape CreateShape();
        protected abstract void Reshape(Point position);

        void SetAttribute(Shape shape)
        {
            shape.Stroke = Stroke;
            shape.StrokeThickness = StrokeThickness;
        }
    }

    public class LineCommand : AddCommand
    {
        protected override Shape CreateShape()
        {
            return new Line { X1 = StartPosition.X, Y1 = StartPosition.Y };
        }

        protected override void Reshape(Point position)
        {
            ((Line)Shape).X2 = position.X;
            ((Line)Shape).Y2 = position.Y;
        }
    }

    public class RectangleCommand : AddCommand
    {
        protected override Shape CreateShape()
        {
            return new Rectangle();
        }

        protected override void Reshape(Point position)
        {
            var rect = new Rect(StartPosition, position);
            ((Rectangle)Shape).Margin = new Thickness { Left = rect.Left, Top = rect.Top };
            ((Rectangle)Shape).Width  = rect.Width ;
            ((Rectangle)Shape).Height = rect.Height;
        }
    }

    public class BezierCommand : AddCommand
    {
        const double sensitivity = 16.0;

        List<Point> positions = null;

        protected override void OnStart()
        {
            positions  = new List<Point> { StartPosition };
        }

        protected override Shape CreateShape()
        {
            return new Path();
        }

        protected override void Reshape(Point position)
        {
            if (positions.Count > 0) {
                var lastPosition = positions[positions.Count - 1];
                if ((position - lastPosition).Length > sensitivity)
                    positions.Add(position);
            }
            if (positions.Count > 2)
                ((Path)Shape).Data = GraphicHelper.BezierPathGeometry(positions);
        }
    }
}
