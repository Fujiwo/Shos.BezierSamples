using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf
{
    public static class Helper
    {
        public static Brush CreateStrokeBrush(Color strokeColor)
        {
            var brush = new LinearGradientBrush { StartPoint = new Point(0.0, 0.0), EndPoint = new Point(1.0, 1.0) };
            brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.95f), 0.0));
            brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.80f), 0.3));
            brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.80f), 0.7));
            brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.65f), 1.0));
            return brush;
        }

        public static Brush CreateFillBrush(Color fillColor)
        {
            var brush = new LinearGradientBrush { StartPoint = new Point(0.0, 0.0), EndPoint = new Point(1.0, 1.0) };
            brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.95f), 0.0));
            brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.80f), 0.3));
            brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.80f), 0.7));
            brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.65f), 1.0));
            return brush;
        }
    }

    public static class HelperExtensions
    {
        public static bool IsDark(this Color color)
        {
            return color.ScR + color.ScG + color.ScB < 3.0f / 2.0f;
        }

        public static Color Multiply(this Color color, float coefficient)
        {
            return Color.Multiply(color, coefficient);
        }

        public static Color MultiplyRGB(this Color color, float coefficient)
        {
            return new Color { ScR = color.ScR * coefficient, ScG = color.ScG * coefficient, ScB = color.ScB * coefficient, ScA = color.ScA };
        }
    }
}
