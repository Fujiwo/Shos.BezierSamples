using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using WpfCad2Lib.Wpf;
using Xunit;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace BezierSamples.Tests;

public class InputLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WpfSameCommandInstance_ReassignmentDoesNotRestartGesture(bool switchAway)
        => StaThread.Run(() => {
            var canvas = new Canvas();
            var command = new BezierWpf.LineCommand();
            var controller = new BezierWpf.Controller { Model = canvas.Children, Command = command };
            controller.OnDragStart(new Point(100, 100));
            controller.OnDragging(new Point(120, 110));
            if (switchAway)
                controller.Command = new BezierWpf.RectangleCommand();
            controller.Command = command;
            controller.OnDragging(new Point(140, 130));
            controller.OnDragEnd(new Point(150, 140));
            var line = Assert.IsType<Line>(Assert.Single(canvas.Children.Cast<object>()));
            Assert.Equal(100, line.X1);
            Assert.Equal(150, line.X2);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CadSameCommandInstance_ReassignmentDoesNotRestartGesture(bool switchAway)
        => StaThread.Run(() => {
            var canvas = new Canvas();
            var model = new Model();
            var command = new LineCommand();
            var controller = new Controller { Model = model, ElementCollection = canvas.Children };
            controller.Command = command;
            controller.OnDragStart(new Point(100, 100));
            controller.OnDragging(new Point(120, 110));
            if (switchAway)
                controller.Command = new RectangleCommand();
            controller.Command = command;
            controller.OnDragging(new Point(140, 130));
            controller.OnDragEnd(new Point(150, 140));
            var line = Assert.IsType<Line>(Assert.Single(canvas.Children.Cast<object>()));
            Assert.Equal(100, line.X1);
            Assert.Equal(150, line.X2);
            model.Select(canvas.Children, new Point(130, 124));
            Assert.True(canvas.Children.Count > 1);
        });

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ReleaseCrossingThreshold_CreatesOneLineAtModelEndpoint(bool cad, bool queuedMove)
        => StaThread.Run(() => {
            var canvas = new Canvas();
            var probe = new ConverterProbe(cad);
            if (cad) {
                var controller = new Controller { Model = new Model(), ElementCollection = canvas.Children };
                controller.Command = new LineCommand();
                probe.Subscribe("DragStart", controller.OnDragStart);
                probe.Subscribe("Dragging", controller.OnDragging);
                probe.Subscribe("DragEnd", controller.OnDragEnd);
            } else {
                var controller = new BezierWpf.Controller { Model = canvas.Children };
                controller.Command = new BezierWpf.LineCommand();
                probe.Subscribe("DragStart", controller.OnDragStart);
                probe.Subscribe("Dragging", controller.OnDragging);
                probe.Subscribe("DragEnd", controller.OnDragEnd);
            }
            probe.Call("OnMouseLeftButtonDown", new Point(100, 100));
            if (queuedMove)
                probe.Call("OnMouseMove", new Point(109, 100));
            probe.Call("OnMouseLeftButtonUp", new Point(130, 100));
            probe.Call("OnLostMouseCapture");
            var line = Assert.IsType<Line>(Assert.Single(canvas.Children.Cast<object>()));
            Assert.Equal(100, line.X1);
            Assert.Equal(130, line.X2);
        });

    [Fact]
    public void CadLayoutZeroSize_FinishesActiveFigureAndRestoresInput()
        => StaThread.Run(() => {
            var view = new WpfCad2Lib.Wpf.View();
            var model = new Model();
            view.Model = model;
            view.Controller = new Controller();
            var controller = (Controller)GetField(view, "controller");
            controller.Command = new LineCommand();
            void Layout(double width)
            {
                view.Measure(new Size(width, 1000));
                view.Arrange(new Rect(0, 0, width, 1000));
                view.UpdateLayout();
            }
            Layout(1000);
            var converter = GetField(view, "mouseEventConverter");
            void Input(string method, Point point)
                => converter.GetType().GetMethod(method, [typeof(Point)])!.Invoke(converter, [point]);
            Input("OnMouseLeftButtonDown", new Point(100, 100));
            Input("OnMouseMove", new Point(130, 100));
            Layout(0);
            var device = (Device)GetField(view, "device");
            Assert.False(device.IsViewValid);
            Input("OnMouseMove", new Point(180, 100));
            Input("OnMouseLeftButtonUp", new Point(180, 100));
            var canvas = (Canvas)GetField(view, "canvas");
            var line = Assert.Single(canvas.Children.OfType<Line>());
            Assert.Equal(130, line.X2);
            model.Select(canvas.Children, new Point(120, 100));
            Assert.True(canvas.Children.Count > 2);
            Layout(1000);
            Assert.True(device.IsViewValid);
            Input("OnMouseLeftButtonDown", new Point(200, 200));
            Input("OnMouseLeftButtonUp", new Point(230, 200));
            Assert.Equal(2, canvas.Children.OfType<Line>().Count());
        });

    [Theory]
    [InlineData(false, false, 10)]
    [InlineData(false, true, 30)]
    [InlineData(true, false, 10)]
    [InlineData(true, true, 30)]
    public void ReleaseCrossingThreshold_EmitsCompleteOrderedDrag(bool cad, bool move, double end)
        => StaThread.Run(() => {
            var probe = new ConverterProbe(cad);
            probe.Call("OnMouseLeftButtonDown", new Point());
            if (move)
                probe.Call("OnMouseMove", new Point(9, 0));
            probe.Call("OnMouseLeftButtonUp", new Point(end, 0));
            probe.Call("OnLostMouseCapture");
            probe.Call("OnMouseLeftButtonUp", new Point(end, 0));
            var expected = new List<(string, Point)> { ("DragStart", new()) };
            if (move)
                expected.Add(("Dragging", new Point(9, 0)));
            expected.Add(("Dragging", new Point(end, 0)));
            expected.Add(("DragEnd", new Point(end, 0)));
            Assert.Equal(expected, probe.Events);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReleaseBelowThreshold_RemainsClickAndClearsQueuedPoints(bool cad)
        => StaThread.Run(() => {
            var probe = new ConverterProbe(cad);
            probe.Call("OnMouseLeftButtonDown", new Point());
            probe.Call("OnMouseMove", new Point(5, 0));
            probe.Call("OnMouseLeftButtonUp", new Point(9, 0));
            probe.Call("OnLostMouseCapture");
            probe.Call("OnMouseLeftButtonDown", new Point(100, 0));
            probe.Call("OnMouseMove", new Point(120, 0));
            probe.Call("OnLostMouseCapture");
            Assert.Equal(new[] {
                ("Click", new Point(9, 0)), ("DragStart", new Point(100, 0)),
                ("Dragging", new Point(120, 0)), ("DragEnd", new Point(120, 0))
            }, probe.Events);
        });

    [Theory]
    [InlineData(0.5, 9, false)]
    [InlineData(0.5, 10, true)]
    [InlineData(8, 9, false)]
    [InlineData(8, 10, true)]
    [InlineData(16, 20, true)]
    public void CadThreshold_IsMeasuredInViewDips(double scale, double screenDistance, bool drag)
        => StaThread.Run(() => {
            var device = new Device {
                ModelArea = new Rect(0, 0, 1000, 1000),
                ViewArea = new Rect(0, 0, 1000 * scale, 1000 * scale)
            };
            device.Pan(Direction.Left, 0.1);
            var probe = new ConverterProbe(true, device);
            var start = new Point(100, 200);
            var end = start + new Vector(screenDistance / scale, 0);
            probe.Call("OnMouseLeftButtonDown", start);
            probe.Call("OnMouseLeftButtonUp", end);
            Assert.Equal(drag ? "DragStart" : "Click", probe.Events[0].Name);
            Assert.Equal(end, probe.Events[^1].Position);
        });

    [Fact]
    public void CadZeroViewport_DisablesDrawingAndRecovers()
        => StaThread.Run(() => {
            var device = new Device { ViewArea = new Rect(0, 0, 0, 100) };
            var probe = new ConverterProbe(true, device);
            probe.Call("OnMouseLeftButtonDown", new Point());
            probe.Call("OnMouseMove", new Point(30, 0));
            probe.Call("OnMouseLeftButtonUp", new Point(40, 0));
            Assert.Empty(probe.Events);
            device.ViewArea = new Rect(0, 0, 1, 1);
            probe.Call("OnMouseLeftButtonDown", new Point());
            probe.Call("OnMouseLeftButtonUp", new Point(30, 0));
            Assert.Equal("DragEnd", probe.Events[^1].Name);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WpfCommandChange_FinishesOriginalAndUsesNewCommandNext(bool sameMode)
        => StaThread.Run(() => {
            var canvas = new Canvas();
            var controller = new BezierWpf.Controller { Model = canvas.Children };
            controller.Command = new BezierWpf.LineCommand();
            controller.OnDragStart(new Point(100, 100));
            controller.OnDragging(new Point(120, 110));
            controller.Command = sameMode ? new BezierWpf.LineCommand() : new BezierWpf.RectangleCommand();
            controller.OnDragging(new Point(140, 130));
            controller.OnDragEnd(new Point(150, 140));
            controller.OnDragging(new Point(200, 200));
            controller.OnDragEnd(new Point(200, 200));
            var line = Assert.IsType<Line>(Assert.Single(canvas.Children.Cast<object>()));
            Assert.Equal(100, line.X1);
            Assert.Equal(150, line.X2);
            controller.OnDragStart(new Point(200, 200));
            controller.OnDragging(new Point(220, 230));
            controller.OnDragEnd(new Point(250, 260));
            Assert.Equal(2, canvas.Children.Count);
            if (sameMode)
                Assert.IsType<Line>(canvas.Children[1]);
            else
                Assert.IsType<Rectangle>(canvas.Children[1]);
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CadCommandChange_RegistersOriginalFigureAndUsesNewCommandNext(bool sameMode)
        => StaThread.Run(() => {
            var canvas = new Canvas();
            var model = new Model();
            var controller = new Controller { Model = model, ElementCollection = canvas.Children };
            controller.Command = new LineCommand();
            controller.OnDragStart(new Point(100, 100));
            controller.OnDragging(new Point(120, 110));
            controller.Command = sameMode ? new LineCommand() : new RectangleCommand();
            controller.OnDragging(new Point(140, 130));
            controller.OnDragEnd(new Point(150, 140));
            controller.OnDragging(new Point(200, 200));
            controller.OnDragEnd(new Point(200, 200));
            var line = Assert.IsType<Line>(Assert.Single(canvas.Children.Cast<object>()));
            Assert.Equal(150, line.X2);
            model.Select(canvas.Children, new Point(130, 124));
            Assert.True(canvas.Children.Count > 1);
            controller.OnDragStart(new Point(200, 200));
            controller.OnDragging(new Point(220, 230));
            controller.OnDragEnd(new Point(250, 260));
            Assert.Equal(2, canvas.Children.OfType<Shape>().Count(s => s is Line || s is Rectangle));
        });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WinFormsCaptureLoss_CommitsOnceAtLastPositionAndAllowsNextDrag(bool normalUpFirst)
        => StaThread.Run(() => {
            using var form = new BezierWinForm.BezierWinForm();
            var view = (Forms.Control)GetField(form, "view");
            var model = (BezierWinForm.Model)GetField(form, "model");
            void Mouse(string method, Forms.MouseButtons button, int x)
                => typeof(Forms.Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(view, [new Forms.MouseEventArgs(button, 1, x, 0, 0)]);
            Mouse("OnMouseDown", Forms.MouseButtons.Left, 0);
            Mouse("OnMouseMove", Forms.MouseButtons.Left, 20);
            Mouse("OnMouseMove", Forms.MouseButtons.Left, 25);
            if (normalUpFirst)
                Mouse("OnMouseUp", Forms.MouseButtons.Left, 25);
            typeof(Forms.Control).GetMethod("OnMouseCaptureChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(view, [EventArgs.Empty]);
            Mouse("OnMouseMove", Forms.MouseButtons.None, 80);
            var figure = Assert.IsType<BezierWinForm.BezierLine>(Assert.Single(model));
            var points = (List<Drawing.PointF>)GetField(figure, "position");
            Assert.Equal(new Drawing.PointF(25, 0), points[^1]);
            Mouse("OnMouseUp", Forms.MouseButtons.Left, 80);
            Assert.Single(model);
            Mouse("OnMouseDown", Forms.MouseButtons.Left, 100);
            Mouse("OnMouseMove", Forms.MouseButtons.Left, 120);
            Mouse("OnMouseUp", Forms.MouseButtons.Left, 130);
            Assert.Equal(2, model.Count());
        });

    static object GetField(object instance, string name)
        => instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    sealed class ConverterProbe
    {
        readonly object converter;
        public List<(string Name, Point Position)> Events { get; } = [];

        public ConverterProbe(bool cad, Device? device = null)
        {
            var type = (cad ? typeof(Controller) : typeof(BezierWpf.Controller)).Assembly
                .GetType(cad ? "WpfCad2Lib.Wpf.MouseEventConverter" : "BezierWpf.MouseEventConverter", true)!;
            converter = cad
                ? type.GetConstructor([typeof(Device)])!.Invoke([device ?? new Device()])
                : Activator.CreateInstance(type, nonPublic: true)!;
            foreach (var name in new[] { "Click", "DragStart", "Dragging", "DragEnd" })
                Subscribe(name, point => Events.Add((name, point)));
        }

        public void Subscribe(string name, Action<Point> handler)
            => converter.GetType().GetEvent(name)!.AddEventHandler(converter, handler);

        public void Call(string name, params Point[] points)
            => converter.GetType().GetMethod(name, points.Select(_ => typeof(Point)).ToArray())!
                .Invoke(converter, points.Cast<object>().ToArray());
    }
}
