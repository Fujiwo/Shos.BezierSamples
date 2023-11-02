using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace WpfCad2Lib.Wpf
{
    public abstract class Command : IMouseAccepter
    {
        public Model               Model             { protected get; set; }
        public UIElementCollection ElementCollection { protected get; set; }

        public virtual bool canClick { get { return false; } }
        public virtual bool canDrag  { get { return true ; } }

        public virtual bool OnStart() { return true; }

        public virtual void OnClick    (Point position) {}
        public virtual void OnDragStart(Point position) {}
        public virtual void OnDragging (Point position) {}
        public virtual void OnDragEnd  (Point position) {}
    }

    public abstract class AddCommand : Command
    {
        protected Point  StartPosition { get; private set; }
        protected Figure Figure        { get; private set; }

        public override void OnClick(Point position)
        {
            if (Figure == null)
                InitializeFigure();
            if (Figure.IsValid) {
                Figure.AddElements(ElementCollection, position);
                if (Model != null)
                    Model.Add(Figure);
            }
            Figure = null;
        }

        public override void OnDragStart(Point position)
        {
            StartPosition = position;
            Figure        = null;
        }

        public override void OnDragging(Point position)
        {
            if (Figure == null) {
                InitializeFigure();
                Figure.AddElements(ElementCollection, StartPosition);
            }
            Figure.SetNext(position);
        }

        public override void OnDragEnd(Point position)
        {
            Figure.SetNext(position);
            if (!Figure.IsValid)
                Figure.RemoveElements(ElementCollection);
            else if (Model != null)
                Model.Add(Figure);
            Figure   = null;
        }

        protected abstract Figure CreateFigure();

        void InitializeFigure()
        {
            Figure = CreateFigure();
            if (Model != null)
                Figure.Attribute = (FigureAttribute)Model.FigureAttribute.Clone();
        }
    }
}
