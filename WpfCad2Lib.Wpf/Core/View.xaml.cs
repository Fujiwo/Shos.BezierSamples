using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf
{
    public partial class View : UserControl
    {
        readonly MouseEventConverter mouseEventConverter = new MouseEventConverter();
        Model                        model               = null;
        Controller                   controller          = null;
        Device                       device              = new Device();

        public Model Model
        {
            set { model = value; }
        }

        public Image Image
        {
            get { return image; }
        }

        //string pdfFilePath = string.Empty;
        //public string PdfFilePath
        //{
        //    get { return pdfFilePath; }
        //    set { pdfFilePath = value; }
        //}

        public Controller Controller
        {
            set
            {
                controller                   = value;
                Debug.Assert(model != null);
                controller.Model             = model;
                controller.ElementCollection = canvas.Children;
                InitializeInputEventHandlers();
            }
        }

        Rect Area
        {
            get { return new Rect(new Size(ActualWidth, ActualHeight)); }
        }

        //Image image = new Image { Source = new BitmapImage(new Uri(@"Resoures\sample.jpg", UriKind.RelativeOrAbsolute)) };

        public View()
        {
            InitializeComponent();
            InitializeMouseEventHandlers();

            //var pdfViewer = new PdfViewer();
            //var location = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(location), @"Resoures\sample.pdf");
            //pdfViewer.LoadFile(path);
            //pdfViewer.ShowToolBar = true;

            //var pdfViewer = new PdfViewer();
            //pdfViewer.LoadFile(@"Resources\sample.pdf");

            //Image image = new Image { Source = new BitmapImage(new Uri(@"Resoures\sample.jpg", UriKind.RelativeOrAbsolute)) };
            //image.Source = new BitmapImage(new Uri(@"Resoures\sample.jpg", UriKind.RelativeOrAbsolute));
            //canvas.Children.Add(image);
        }
        
        public void Home()
        {
            device.Home();
            matrixTransform.Matrix = device.ModelToViewMatrix;
            var point = new Point() * device.ModelToViewMatrix;
        }

        void Zoom(Point zoomCenterModelPoint, double rate)
        {
            device.Zoom(zoomCenterModelPoint, rate);
            matrixTransform.Matrix = device.ModelToViewMatrix;
        }

        void Pan(Direction direction)
        {
            device.Pan(direction, Common.DefaultPanRate);
            matrixTransform.Matrix = device.ModelToViewMatrix;
        }

        // for Debug
        public void AddModelAreaRect()
        {
            //model.Area = new Rect(new Size(image.Source.Width, image.Source.Height));

            var brush = new LinearGradientBrush { StartPoint = new Point(0.0, 0.0), EndPoint = new Point(1.0, 1.0) };
            brush.GradientStops.Add(new GradientStop(Colors.SandyBrown, 0.0));
            brush.GradientStops.Add(new GradientStop(Colors.Salmon    , 0.3));
            brush.GradientStops.Add(new GradientStop(Colors.Tomato    , 0.6));
            brush.GradientStops.Add(new GradientStop(Colors.Wheat     , 1.0));
            
            var rectangle = new Rectangle {
                Margin          = new Thickness { Left = model.Area.Left, Top = model.Area.Top },
                Width           = model.Area.Width ,
                Height          = model.Area.Height,
                Stroke          = brush,
                StrokeThickness = 5.0
            };
            canvas.Children.Add(rectangle);
        }

        //Size firstActualSize;

        void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            //if (firstActualSize.Width == 0 || firstActualSize.Height == 0) {
            //    firstActualSize.Width  = ActualWidth;
            //    firstActualSize.Height = ActualHeight;
            //} else {
            //    var matrix = new Matrix();
            //    matrix.Scale(ActualWidth / firstActualSize.Width, ActualHeight / firstActualSize.Height);
            //    matrixTransform.Matrix = matrix;
            //}
            if (model == null)
                return;
            device.ModelArea       = model.Area;
            device.ViewArea        = Area;
            matrixTransform.Matrix = device.ModelToViewMatrix;
            //SetCanvasPosition(canvas, model.Area, device.ViewToModelMatrix.Transform(Area));
        }

        //void SetCanvasPosition(FrameworkElement canvas, Rect modelArea, Rect modelViewArea)
        //{
        //    canvas.Margin = new Thickness { Left = 0, Top = 0 };
        //    canvas.Width = Math.Min(modelArea.Width, modelViewArea.Width);
        //    canvas.Height = Math.Min(modelArea.Height, modelViewArea.Height);
        //}

        void InitializeMouseEventHandlers()
        {
            canvas.MouseLeftButtonDown += mouseEventConverter.OnMouseLeftButtonDown;
            canvas.MouseMove           += mouseEventConverter.OnMouseMove          ;
            canvas.MouseLeftButtonUp   += mouseEventConverter.OnMouseLeftButtonUp  ;
            canvas.MouseWheel          += OnMouseWheel;
            //MouseDoubleClick           += OnMouseDoubleClick;
        }

        //void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        //{
        //    Zoom();
        //}

        void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Zoom(e.ToPosition(sender), 1.0 - e.Delta / 120.0 * Common.MouseWheelZoomRate);
        }

        void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.Left : Pan(Direction.Left ); break;
                case Key.Right: Pan(Direction.Right); break;
                case Key.Up   : Pan(Direction.Up   ); break;
                case Key.Down : Pan(Direction.Down ); break;
            }
        }

        void InitializeInputEventHandlers()
        {
            mouseEventConverter.Click     += controller.OnClick    ;
            mouseEventConverter.DragStart += controller.OnDragStart;
            mouseEventConverter.Dragging  += controller.OnDragging ;
            mouseEventConverter.DragEnd   += controller.OnDragEnd  ;
        }
    }
}
