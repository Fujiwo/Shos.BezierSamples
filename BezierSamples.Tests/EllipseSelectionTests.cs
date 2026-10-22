using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using WpfCad2Lib.Wpf;
using Xunit;

namespace BezierSamples.Tests;

public class EllipseSelectionTests
{
    [Theory]
    [InlineData(600, 600, 3, 0.0)]
    [InlineData(600, 600, 3, 0.125)]
    [InlineData(600, 400, 40, 0.2)]
    [InlineData(40, 600, 20, 0.3)]
    [InlineData(20, 600, 40, 0.25)]
    [InlineData(600, 20, 40, 0.0)]
    [InlineData(20, 600, 100, 0.25)]
    public void Selection_AgreesWithArrangedRenderedCenterline(double width, double height, double stroke, double fraction)
        => StaThread.Run(() => {
            var canvas = new Canvas();
            var figure = new EllipseFigure();
            figure.Attribute.StrokeThickness = stroke;
            figure.AddElements(canvas.Children, new Point(10, 20));
            figure.SetLast(new Point(10 + width, 20 + height));
            var ellipse = Assert.IsType<Ellipse>(Assert.Single(canvas.Children.Cast<object>()));
            ellipse.Measure(new Size(width, height));
            ellipse.Arrange(new Rect(0, 0, width, height));
            var geometry = PathGeometry.CreateFromGeometry(ellipse.RenderedGeometry);
            geometry.GetPointAtFractionLength(fraction, out var local, out _);
            var point = local + new Vector(10, 20);
            var distance = double.MaxValue;
            Assert.True(figure.IsNear(point, 0.01, ref distance),
                $"Rendered bounds: {geometry.Bounds}; point: {point}; stroke: {stroke}");
            Assert.InRange(distance, 0, 0.01);
        });

    [Fact]
    public void AttributeStrokeChange_UpdatesOutlineSelection()
        => StaThread.Run(() => {
            var figure = Create(600, 600);
            figure.Attribute.StrokeThickness = 3;
            var distance = double.MaxValue;
            Assert.True(figure.IsNear(new Point(608.5, 320), 0.01, ref distance));
            figure.Attribute.StrokeThickness = 40;
            distance = double.MaxValue;
            Assert.False(figure.IsNear(new Point(608.5, 320), 0.01, ref distance));
            Assert.True(figure.IsNear(new Point(590, 320), 0.01, ref distance));
        });

    [Theory]
    [InlineData(600, 600, 0)]
    [InlineData(600, 600, 45)]
    [InlineData(600, 600, 135)]
    [InlineData(600, 600, 225)]
    [InlineData(600, 600, 315)]
    [InlineData(1000, 40, 45)]
    [InlineData(40, 1000, 225)]
    public void Circumference_IsSelectable(double width, double height, double degrees)
        => StaThread.Run(() => {
            var figure = Create(width, height);
            var angle = degrees * Math.PI / 180;
            var point = new Point(10 + width / 2 * (1 + Math.Cos(angle)),
                                  20 + height / 2 * (1 + Math.Sin(angle)));
            var distance = double.MaxValue;
            Assert.True(figure.IsNear(point, 0.01, ref distance));
            Assert.InRange(distance, 0, 0.001);
        });

    [Theory]
    [InlineData(600, 600, 610, 20)]
    [InlineData(600, 600, 10, 620)]
    [InlineData(600, 600, 310, 320)]
    [InlineData(1000, 400, 1010, 20)]
    public void PointsFarFromOutline_AreNotSelectable(double width, double height, double x, double y)
        => StaThread.Run(() => {
            var figure = Create(width, height);
            var distance = double.MaxValue;
            Assert.False(figure.IsNear(new Point(x, y), 40, ref distance));
            Assert.Equal(double.MaxValue, distance);
        });

    [Theory]
    [InlineData(609, 320, 1, true)]
    [InlineData(611, 320, 1, true)]
    [InlineData(608, 320, 1, false)]
    [InlineData(612, 320, 1, false)]
    public void Tolerance_AppliesOnBothSidesOfOutline(double x, double y, double tolerance, bool expected)
        => StaThread.Run(() => {
            var figure = Create(600, 600);
            var distance = double.MaxValue;
            Assert.Equal(expected, figure.IsNear(new Point(x, y), tolerance, ref distance));
        });

    [Fact]
    public void NearestCandidate_UsesOutlineDistance()
        => StaThread.Run(() => {
            var data = new CadData();
            var ellipse = Create(600, 600);
            var rectangle = new RectangleFigure();
            rectangle.AddElements(new Canvas().Children, new Point(500, 400));
            rectangle.SetLast(new Point(550, 550));
            data.Add(rectangle);
            data.Add(ellipse);
            var p = 300 / Math.Sqrt(2);
            Assert.Same(ellipse, data.Find(new Point(310 + p, 320 + p), 40));
        });

    static EllipseFigure Create(double width, double height)
    {
        var figure = new EllipseFigure();
        figure.Attribute.StrokeThickness = 0;
        figure.AddElements(new Canvas().Children, new Point(10, 20));
        figure.SetLast(new Point(10 + width, 20 + height));
        return figure;
    }
}
