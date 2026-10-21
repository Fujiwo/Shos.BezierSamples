using System;
using System.ComponentModel;
using System.Drawing;

namespace BezierWinForm
{
    public partial class Controller : Component,  IDrawable, IDrawableSubject
    {
        public event Action<IDrawable> Update;

        BezierLine bezierLine = null;

        public Model Model { get; set; }

        public Controller()
        {
            InitializeComponent();
        }

        public Controller(IContainer container)
        {
            container.Add(this);
            InitializeComponent();
        }

        public void OnMouseDown(PointF point)
        {
            bezierLine = new BezierLine();
            bezierLine.Add(point);
        }

        public void OnMouseMove(PointF point)
        {
            if (bezierLine != null) {
                if (bezierLine.Add(point) && Update != null)
                    Update(this);
            }
        }

        public void OnMouseUp(PointF point)
        {
            if (bezierLine != null) {
                bezierLine.AddLast(point);
                if (Model != null)
                    Model.Add(bezierLine);
                bezierLine = null;
            }
        }

        public void Draw(Graphics graphics)
        {
            if (bezierLine != null)
                bezierLine.Draw(graphics);
        }
    }
}
