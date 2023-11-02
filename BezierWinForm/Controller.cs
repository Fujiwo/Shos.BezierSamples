using System;
using System.ComponentModel;
using System.Drawing;

namespace BezierWinForm
{
    public partial class Controller : Component,  IDrawable, IDrawableSubject
    {
        public event Action<IDrawable>? Update;

        BezierLine? bezierLine = null;

        public Model? Model { get; set; }

        public Controller() => InitializeComponent();

        public Controller(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        public void OnMouseDown(PointF point)
            => bezierLine = new BezierLine();

        public void OnMouseMove(PointF point)
        {
            if (bezierLine is not null) {
                if (bezierLine.Add(point) && Update != null)
                    Update(this);
            }
        }

        public void OnMouseUp(PointF point)
        {
            if (bezierLine is not null) {
                bezierLine.Add(point);
                Model?.Add(bezierLine);
                bezierLine = null;
            }
        }

        public void Draw(Graphics graphics) => bezierLine?.Draw(graphics);
    }
}
