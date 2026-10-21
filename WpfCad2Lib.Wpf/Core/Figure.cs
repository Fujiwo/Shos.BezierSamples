using WpfCad2Lib.Core;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf
{
    public abstract class Figure
    {
        FigureAttribute          attribute = new FigureAttribute();
        List<FrameworkElement>?   elements;
        List<Shape>?              selectors;
        int                       selected  = -1;

        public FigureAttribute Attribute
        {
            get { return attribute; }
            set
            {
                if (value == attribute)
                    return;
                attribute.PropertyChanged -= OnAttributeChanged;
                attribute                  = value;
                attribute.PropertyChanged += OnAttributeChanged;
                ApplyAttribute();
            }
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

        protected Figure()
        {
            attribute.PropertyChanged += OnAttributeChanged;
        }

        public void AddElements(UIElementCollection elementCollection, Point firstPoint)
        {
            InitializeElements();
            if (elements is null)
                return;
            elements.ForEach(element => elementCollection.Add(element));
            SetFirst(elements, firstPoint);
        }

        public void RemoveElements(UIElementCollection elementCollection)
        {
            if (elements is null)
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
            if (selectors is null)
                return;
            selectors.ForEach(element => elementCollection.Remove(element));
            selectors = null;
        }

        public void SetNext(Point point)
        {
            if (elements is null)
                return;
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
            if (bounds is null)
                return false;
            var inflatedBounds = Rect.Inflate(bounds.Value, nearDistance, nearDistance);
            return inflatedBounds.Contains(point);
        }

        protected abstract IEnumerable<FrameworkElement> GetInitialElements();

        protected abstract void SetFirst(IEnumerable<FrameworkElement> elements, Point point);

        protected virtual void SetNext(IEnumerable<FrameworkElement> elements, Point point)
        {}

        void InitializeElements()
        {
            elements = GetInitialElements().ToList();
            ApplyAttribute();
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

        void OnAttributeChanged(object? sender, PropertyChangedEventArgs e)
        {
            ApplyAttribute();
        }

        // 画面上の要素はそのままに、属性 (色・太さなど) だけを設定し直す
        void ApplyAttribute()
        {
            if (elements is not null)
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
