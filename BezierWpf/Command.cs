using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BezierWpf
{
    public abstract class Command : IMouseAcceptor
    {
        public UIElementCollection? Model { protected get; set; }

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
            get { return Brushes.Black; }
        }

        protected Point StartPosition { get; private set; }
        protected Shape? Shape { get; private set; }

        public override void OnDragStart(Point position)
        {
            StartPosition = position;
            Shape = null;
            OnStart();
        }

        public override void OnDragging(Point position)
        {
            if (Shape is null) {
                Shape = CreateShape();
                SetAttribute(Shape);
                Model?.Add(Shape);
            }
            Reshape(position);
        }

        public override void OnDragEnd(Point position)
        {
            ReshapeLast(position);
            Shape = null;
        }

        protected virtual void OnStart() {}
        protected abstract Shape CreateShape();
        protected abstract void Reshape(Point position);

        protected virtual void ReshapeLast(Point position)
        {
            Reshape(position);
        }

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
            if (Shape is null)
                return;

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
            if (Shape is null)
                return;

            var rect = new Rect(StartPosition, position);
            Canvas.SetLeft(Shape, rect.Left);
            Canvas.SetTop (Shape, rect.Top );
            ((Rectangle)Shape).Width  = rect.Width ;
            ((Rectangle)Shape).Height = rect.Height;
        }
    }

    public class BezierCommand : AddCommand
    {
        const double sensitivity = 16.0;

        List<Point>? positions = null;

        protected override void OnStart()
            => positions  = new List<Point> { StartPosition };

        protected override Shape CreateShape() => new Path();

        protected override void Reshape(Point position)
        {
            if (positions is null || Shape is null)
                return;

            if (positions.Count > 0) {
                var lastPosition = positions[positions.Count - 1];
                if ((position - lastPosition).Length > sensitivity)
                    positions.Add(position);
            }
            if (positions.Count > 2)
                ((Path)Shape).Data = GraphicHelper.BezierPathGeometry(positions);
        }

        // ドラッグの終点は間隔に関係なく必ず通るようにする (直前の点に近すぎる場合はその点を置き換える)
        protected override void ReshapeLast(Point position)
        {
            if (positions is null || Shape is null)
                return;

            var lastPosition = positions[positions.Count - 1];
            if (positions.Count >= 2 && (position - lastPosition).Length <= sensitivity)
                positions[positions.Count - 1] = position;
            else if (position != lastPosition)
                positions.Add(position);
            if (positions.Count > 2)
                ((Path)Shape).Data = GraphicHelper.BezierPathGeometry(positions);
        }
    }
}
