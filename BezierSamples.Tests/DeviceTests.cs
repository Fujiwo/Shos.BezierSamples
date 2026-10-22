using System.Windows;
using WpfCad2Lib.Wpf;
using Xunit;

namespace BezierSamples.Tests;

public class DeviceTests
{
    static Device CreateDevice() => new() {
        ModelArea = new Rect(0, 0, 1000, 1000),
        ViewArea = new Rect(0, 0, 1000, 1000)
    };

    [Theory]
    [InlineData(0, 100)]
    [InlineData(100, 0)]
    [InlineData(0, 0)]
    public void ZeroSize_PreservesLastTransformAndRecovers(double width, double height)
    {
        var device = CreateDevice();
        device.Pan(Direction.Left, 0.1);
        device.Zoom(new Point(250, 300), 2);
        var original = device.ModelToViewMatrix;
        var center = device.ViewToModelMatrix.Transform(new Point(500, 500));

        device.ViewArea = new Rect(0, 0, width, height);
        Assert.Equal(original, device.ModelToViewMatrix);
        device.ViewArea = new Rect(0, 0, 0, 0);
        device.ViewArea = new Rect(0, 0, 1200, 800);

        BezierAssert.Equal(center, device.ViewToModelMatrix.Transform(new Point(600, 400)));
        AssertRoundTrip(device);
    }

    [Theory]
    [InlineData(false, 1200, 1000)]
    [InlineData(false, 800, 600)]
    [InlineData(true, 1200, 1000)]
    [InlineData(true, 800, 600)]
    public void Resize_PreservesNavigatedCenter(bool zoom, double width, double height)
    {
        var device = CreateDevice();
        device.Pan(Direction.Left, 0.1);
        if (zoom)
            device.Zoom(new Point(250, 300), 2);
        var center = device.ViewToModelMatrix.Transform(new Point(500, 500));
        device.ViewArea = new Rect(0, 0, width, height);
        BezierAssert.Equal(center, device.ViewToModelMatrix.Transform(new Point(width / 2, height / 2)));
        AssertRoundTrip(device);
    }

    [Fact]
    public void Home_AfterNavigationAndResize_RestoresInitialFit()
    {
        var device = CreateDevice();
        device.Pan(Direction.Down, 0.2);
        device.Zoom(new Point(250, 300), 3);
        device.ViewArea = new Rect(0, 0, 800, 600);
        device.Home();
        BezierAssert.Equal(new Point(), device.ModelToViewMatrix.Transform(new Point()));
        BezierAssert.Equal(new Point(600, 600), device.ModelToViewMatrix.Transform(new Point(1000, 1000)));
        device.ViewArea = new Rect(0, 0, 1200, 1000);
        BezierAssert.Equal(new Point(), device.ModelToViewMatrix.Transform(new Point()));
        AssertRoundTrip(device);
    }

    [Fact]
    public void HomeDuringZeroSize_IsAppliedWhenLayoutRecovers()
    {
        var device = CreateDevice();
        device.Pan(Direction.Down, 0.2);
        device.ViewArea = new Rect(0, 0, 0, 100);
        device.Home();
        device.ViewArea = new Rect(0, 0, 800, 600);
        BezierAssert.Equal(new Point(), device.ModelToViewMatrix.Transform(new Point()));
        AssertRoundTrip(device);
    }

    [Fact]
    public void Zoom_PreservesRequestedScreenPositionAfterPanAndResize()
    {
        var device = CreateDevice();
        device.Pan(Direction.Right, 0.1);
        device.ViewArea = new Rect(0, 0, 800, 600);
        var point = new Point(170, 260);
        var screen = device.ModelToViewMatrix.Transform(point);
        device.Zoom(point, 2);
        BezierAssert.Equal(screen, device.ModelToViewMatrix.Transform(point));
        device.Zoom(point, 1000);
        BezierAssert.Equal(screen, device.ModelToViewMatrix.Transform(point));
        AssertRoundTrip(device);
    }

    [Fact]
    public void NavigationDuringZeroSize_DoesNotChangeSavedView()
    {
        var device = CreateDevice();
        device.Pan(Direction.Left, 0.1);
        var original = device.ModelToViewMatrix;
        device.ViewArea = new Rect(0, 0, 0, 0);
        device.Pan(Direction.Up, 0.5);
        device.Zoom(new Point(100, 200), 2);
        Assert.Equal(original, device.ModelToViewMatrix);
        device.ViewArea = new Rect(0, 0, 1000, 1000);
        Assert.Equal(original, device.ModelToViewMatrix);
    }

    static void AssertRoundTrip(Device device)
    {
        Assert.True(device.ModelToViewMatrix.HasInverse);
        var point = new Point(123.5, -276.25);
        BezierAssert.Equal(point, device.ViewToModelMatrix.Transform(device.ModelToViewMatrix.Transform(point)));
    }
}
