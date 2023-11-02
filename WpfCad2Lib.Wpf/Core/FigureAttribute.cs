using WpfCad2Lib.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf
{
    public class FigureAttribute : Subject, ICloneable
    {
        Color  strokeColor     = Common.DefaultStrokeColor    ;
        Color  fillColor       = Common.DefaultFillColor      ;
        double strokeThickness = Common.DefaultStrokeThickness;
        double fontSize        = Common.DefaultFontSize       ;

        public Color StrokeColor
        {
            get { return strokeColor; }
            set
            {
                if (value != strokeColor) {
                    strokeColor = value;
                    RaisePropertyChanged(() => StrokeColor);
                }
            }
        }

        public Color FillColor
        {
            get { return fillColor; }
            set {
                if (value != fillColor) {
                    fillColor = value;
                    RaisePropertyChanged(() => FillColor);
                }
            }
        }

        public double StrokeThickness
        {
            get { return strokeThickness; }
            set
            {
                if (value != strokeThickness) {
                    strokeThickness = value;
                    RaisePropertyChanged(() => StrokeThickness);
                }
            }
        }

        public double FontSize
        {
            get { return fontSize; }
            set
            {
                if (value != fontSize) {
                    fontSize = value;
                    RaisePropertyChanged(() => FontSize);
                }
            }
        }

        //public void Copy(FigureAttribute figureAttribute)
        //{
        //    figureAttribute.StrokeColor     = StrokeColor    ;
        //    figureAttribute.FillColor       = FillColor      ;
        //    figureAttribute.StrokeThickness = StrokeThickness;
        //    figureAttribute.FontSize        = FontSize       ;
        //}

        public void SetColor(Color color)
        {
            StrokeColor = FillColor = color;
        }

        public void SetAttribute(IEnumerable<FrameworkElement> elements, bool isFilled)
        {
            elements.ForEach(element => SetAttribute(element, isFilled));
        }

        void SetAttribute(FrameworkElement element, bool isFilled)
        {
            var shape = element as Shape;
            if (shape != null) {
                SetShapeAttribute(shape, StrokeThickness, StrokeColor, FillColor, isFilled);
            } else {
                var text = element as OutlineText;
                if (text != null)
                    SetTextAttribute(text, StrokeThickness, FontSize, FillColor, isFilled);
            }
        }

        static void SetShapeAttribute(Shape shape, double strokeThickness, Color strokeColor, Color fillColor, bool isFilled)
        {
            shape.Stroke          = Helper.CreateStrokeBrush(strokeColor);
            shape.StrokeThickness = strokeThickness;
            shape.Fill            = isFilled ? Helper.CreateFillBrush(fillColor) : null;
            shape.Effect          = Common.DefaultShadowEffect;
        }

        static void SetTextAttribute(OutlineText text, double strokeThickness, double fontSize, Color fillColor, bool isFilled)
        {
            text.Stroke          = Helper.CreateStrokeBrush(fillColor.IsDark() ? Common.DefaultLightColor : Common.DefaultDarkColor);
            text.StrokeThickness = strokeThickness;
            text.Fill            = isFilled ? Helper.CreateFillBrush(fillColor) : null;
            text.FontSize        = fontSize;
            text.Effect          = Common.DefaultShadowEffect;
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
