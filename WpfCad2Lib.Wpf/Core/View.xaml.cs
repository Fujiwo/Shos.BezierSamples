using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// CAD の View。XAML に置いて使う UserControl。
/// </summary>
/// <remarks>
/// 構成: ScrollViewer の中に Canvas があり、Canvas の先頭に背景用の <see cref="Image"/>、その後ろに図形の要素が並ぶ。
/// Canvas の RenderTransform (matrixTransform) に <see cref="Device"/> のモデル→ビュー行列を設定するので、
/// 図形はモデル座標のまま Canvas に置き、マウス位置も Canvas 基準 (= モデル座標) で受け取れる。
/// マウス操作は MouseEventConverter でクリック/ドラッグに変換して Controller に渡し、
/// ホイールでズーム、矢印キーでパンする。
/// </remarks>
public partial class View : UserControl
{
    readonly MouseEventConverter mouseEventConverter;
    Model                        model               = null!;
    Controller                   controller          = null!;
    // ズーム・パンの状態とモデル座標 ↔ ビュー座標の行列を持つ
    readonly Device              device              = new();

    /// <summary>表示する Model。<see cref="Controller"/> より先に設定する (MVC クラスが行う)。</summary>
    public Model Model
    {
        set => model = value;
    }

    /// <summary>Canvas の背景に表示する画像。利用側が Source を設定する。</summary>
    public Image Image => image;

    //string pdfFilePath = string.Empty;
    //public string PdfFilePath
    //{
    //    get { return pdfFilePath; }
    //    set { pdfFilePath = value; }
    //}

    /// <summary>
    /// マウス操作を渡す先の Controller。設定すると、Controller に Model と描画先 (Canvas.Children) を渡し、
    /// MouseEventConverter のクリック/ドラッグ通知を Controller につなぐ。
    /// </summary>
    public Controller Controller
    {
        set
        {
            // 差し替えの場合は、古い Controller への通知を外してからつなぎ直す
            if (controller is not null)
                RemoveInputEventHandlers();
            controller                   = value;
            Debug.Assert(model is not null);
            controller.Model             = model;
            controller.ElementCollection = canvas.Children;
            InitializeInputEventHandlers();
        }
    }

    // View 自身の大きさ (ビュー座標の範囲)
    Rect Area => new(new Size(ActualWidth, ActualHeight));

    //Image image = new Image { Source = new BitmapImage(new Uri(@"Resoures\sample.jpg", UriKind.RelativeOrAbsolute)) };

    /// <summary>View を作り、マウスイベントを MouseEventConverter につなぐ。</summary>
    public View()
    {
        mouseEventConverter = new MouseEventConverter(device);
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
    
    /// <summary>ズームとパンを取り消して、Model.Area 全体が見える初期表示に戻す。</summary>
    public void Home()
    {
        device.Home();
        matrixTransform.Matrix = device.ModelToViewMatrix;
    }

    // Zoom / Pan / Home はいずれも、Device の行列を変えてから Canvas の RenderTransform に反映する
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
    /// <summary>Model.Area の範囲を示す枠を Canvas に追加する (デバッグ用)。</summary>
    public void AddModelAreaRect()
    {
        //model.Area = new Rect(new Size(image.Source.Width, image.Source.Height));

        var brush = new LinearGradientBrush { StartPoint = new Point(0.0, 0.0), EndPoint = new Point(1.0, 1.0) };
        brush.GradientStops.Add(new GradientStop(Colors.SandyBrown, 0.0));
        brush.GradientStops.Add(new GradientStop(Colors.Salmon    , 0.3));
        brush.GradientStops.Add(new GradientStop(Colors.Tomato    , 0.6));
        brush.GradientStops.Add(new GradientStop(Colors.Wheat     , 1.0));
        
        var rectangle = new Rectangle {
            Width           = model.Area.Width ,
            Height          = model.Area.Height,
            Stroke          = brush,
            StrokeThickness = 5.0
        };
        Canvas.SetLeft(rectangle, model.Area.Left);
        Canvas.SetTop (rectangle, model.Area.Top );
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
        // サイズが一時的に 0 になったら、最後の有効な位置で作図を終了し、復帰まで入力を止める
        if (model is null)
            return;
        device.ModelArea       = model.Area;
        device.ViewArea        = Area;
        if (!device.IsViewValid) {
            mouseEventConverter.OnLostMouseCapture();
            canvas.ReleaseMouseCapture();
        }
        matrixTransform.Matrix = device.ModelToViewMatrix;
        //SetCanvasPosition(canvas, model.Area, device.ViewToModelMatrix.Transform(Area));
    }

    //void SetCanvasPosition(FrameworkElement canvas, Rect modelArea, Rect modelViewArea)
    //{
    //    canvas.Margin = new Thickness { Left = 0, Top = 0 };
    //    canvas.Width = Math.Min(modelArea.Width, modelViewArea.Width);
    //    canvas.Height = Math.Min(modelArea.Height, modelViewArea.Height);
    //}

    // マウスイベントは View ではなく Canvas で受ける。
    // こうすると ToPosition が Canvas 基準 (RenderTransform を逆にたどったモデル座標) の位置を返し、マウスキャプチャーも Canvas に掛かる
    void InitializeMouseEventHandlers()
    {
        // クリックでフォーカスを得て、矢印キー (パン) を受け取れるようにする
        canvas.MouseLeftButtonDown += (_, _) => Focus();
        canvas.MouseLeftButtonDown += mouseEventConverter.OnMouseLeftButtonDown;
        canvas.MouseMove           += mouseEventConverter.OnMouseMove          ;
        canvas.MouseLeftButtonUp   += mouseEventConverter.OnMouseLeftButtonUp  ;
        canvas.LostMouseCapture    += mouseEventConverter.OnLostMouseCapture   ;
        canvas.MouseWheel          += OnMouseWheel;
        //MouseDoubleClick           += OnMouseDoubleClick;
    }

    //void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    //{
    //    Zoom();
    //}

    // ホイールを奥 (Delta > 0) に回すと拡大する
    void OnMouseWheel(object sender, MouseWheelEventArgs e)
        => Zoom(e.ToPosition(sender), Math.Pow(1.0 + Common.MouseWheelZoomRate, e.Delta / 120.0));

    // 子の ScrollViewer やキーボードナビゲーションに矢印キーを奪われないよう、Preview で受けて処理済みにする
    void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key) {
            case Key.Left : Pan(Direction.Left ); break;
            case Key.Right: Pan(Direction.Right); break;
            case Key.Up   : Pan(Direction.Up   ); break;
            case Key.Down : Pan(Direction.Down ); break;
            default       : return;
        }
        e.Handled = true;
    }

    // MouseEventConverter が変換したクリック/ドラッグを Controller につなぐ
    void InitializeInputEventHandlers()
    {
        mouseEventConverter.Click     += controller.OnClick    ;
        mouseEventConverter.DragStart += controller.OnDragStart;
        mouseEventConverter.Dragging  += controller.OnDragging ;
        mouseEventConverter.DragEnd   += controller.OnDragEnd  ;
    }

    void RemoveInputEventHandlers()
    {
        mouseEventConverter.Click     -= controller.OnClick    ;
        mouseEventConverter.DragStart -= controller.OnDragStart;
        mouseEventConverter.Dragging  -= controller.OnDragging ;
        mouseEventConverter.DragEnd   -= controller.OnDragEnd  ;
    }
}
