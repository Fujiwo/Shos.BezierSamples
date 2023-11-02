using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf
{
    public abstract class Figure
    {
        FigureAttribute               attribute = new FigureAttribute();
        List<FrameworkElement>        elements  = null;
        List<Shape>                   selectors = null;
        int                           selected  = -1;

        public FigureAttribute Attribute
        {
            get { return attribute; }
            set { attribute = value; }
        }

        public bool IsSelected
        {
            get { return selected >= 0; }
            private set { selected = value ? int.MaxValue : -1; }
        }

        public virtual bool IsValid
        {
            get { return true; }
        }

        public virtual bool IsFilled
        {
            get { return false; }
        }

        protected abstract Rect? Bounds { get; }

        protected virtual IEnumerable<Point> SelectorPoints
        {
            get { return new Point[] { }; }
        }

        public void AddElements(UIElementCollection elementCollection)
        {
            SetElements();
            elements.ForEach(element => elementCollection.Add(element));
        }

        public void AddElements(UIElementCollection elementCollection, Point firstPoint)
        {
            InitializeElements();
            elements.ForEach(element => elementCollection.Add(element));
            SetFirst(elements, firstPoint);
        }

        public void RemoveElements(UIElementCollection elementCollection)
        {
            if (elements == null)
                return;
            elements.ForEach(element => elementCollection.Remove(element));
            elements = null;
        }

        public void Select(UIElementCollection elementCollection, bool isSelected = true)
        {
            if (isSelected == IsSelected)
                return;
            IsSelected = isSelected;
            if (isSelected)
                AddSelectors   (elementCollection);
            else
                RemoveSelectors(elementCollection);
        }

        void AddSelectors(UIElementCollection elementCollection)
        {
            selectors = GetSelectors().ToList();
            selectors.ForEach(element => elementCollection.Add(element));
        }

        void RemoveSelectors(UIElementCollection elementCollection)
        {
            if (selectors == null)
                return;
            selectors.ForEach(element => elementCollection.Remove(element));
            selectors = null;
        }

        public void SetNext(Point point)
        {
            SetNext(elements, point);
        }

        public bool IsNear(Point point, double nearDistance, ref double minimumDistance)
        {
            if (IsNearBounds(point, nearDistance)) {
                var distance = GetDistance(point, nearDistance);
                if (distance <= nearDistance && distance < minimumDistance) {
                    minimumDistance = distance;
                    return true;
                }
            }
            return false;
        }

        protected virtual double GetDistance(Point point, double nearDistance)
        {
            return nearDistance;
        }

        protected virtual bool IsNearBounds(Point point, double nearDistance)
        {
            var bounds = Bounds;
            if (bounds == null)
                return false;
            var inflatedBounds = Rect.Inflate(bounds.Value, nearDistance, nearDistance);
            return inflatedBounds.Contains(point);
        }

        protected abstract IEnumerable<FrameworkElement> GetInitialElements();

        protected virtual IEnumerable<FrameworkElement> ToElements()
        {
            return null;
        }

        protected virtual IEnumerable<FrameworkElement> RegetElements()
        {
            return null;
        }

        protected abstract void SetFirst(IEnumerable<FrameworkElement> elements, Point point);

        protected virtual void SetNext(IEnumerable<FrameworkElement> elements, Point point)
        {}

        void SetElements()
        {
            elements = ToElements().ToList();
            attribute.SetAttribute(elements, IsFilled);
            attribute.PropertyChanged += delegate { OnAttributeChanged(); };
        }

        void InitializeElements()
        {
            elements = GetInitialElements().ToList();
            attribute.SetAttribute(elements, IsFilled);
        }
        
        IEnumerable<Shape> GetSelectors()
        {
            var selectorPoints = SelectorPoints;
            foreach (var selectorPoint in SelectorPoints) {
                var shape = GetSelector(selectorPoint);
                SetSelectorAttribute(shape);
                yield return shape;
            }
        }

        static Shape GetSelector(Point selectorPoint)
        {
            var shape    = new Ellipse();
            shape.Margin = new Thickness { Left = selectorPoint.X - Common.SelectorRadius,
                                           Top  = selectorPoint.Y - Common.SelectorRadius };
            shape.Width  = 
            shape.Height = Common.SelectorRadius * 2.0;
            return shape;
        }

        void OnAttributeChanged()
        {
            var newElements = RegetElements();
            if (newElements != null)
                elements = newElements.ToList();
            attribute.SetAttribute(elements, IsFilled);
        }

        static void SetSelectorAttribute(Shape element)
        {
            element.Stroke          = Helper.CreateStrokeBrush(Common.SelectorStrokeColor);
            element.StrokeThickness = Common.SelectorStrokeThickness;
            element.Fill            = Helper.CreateFillBrush(Common.SelectorFillColor);
            element.Effect          = Common.DefaultShadowEffect;
        }
    }
}
