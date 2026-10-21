using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 袋文字 (縁取りした文字) を描く要素。
/// </summary>
/// <remarks>
/// <para>
/// TextBlock には文字の縁を描く機能が無い。そこで <see cref="FormattedText.BuildGeometry"/> で文字の字形を
/// <see cref="Geometry"/> (輪郭の図形) に変換し、<see cref="OnRender"/> でペン (縁取り) とブラシ (塗り) を指定して描く。
/// </para>
/// <para>
/// ペンの線は輪郭の中心線から内外に半分ずつはみ出す。既定 (<see cref="OverFill"/> = true) では縁取りを先に描いて
/// その上に塗りを重ねるので、内側にはみ出した半分が隠れ、文字の形を細らせずに外側だけを縁取れる。
/// </para>
/// <para>
/// どのプロパティを変えても <c>OnOutlineTextInvalidated</c> でジオメトリを作り直し、
/// <see cref="FrameworkPropertyMetadataOptions.AffectsRender"/> により再描画される。
/// </para>
/// </remarks>
public class OutlineText : FrameworkElement
{
    // 文字の輪郭のジオメトリ。縁取りと塗りに使う
    Geometry? textGeometry;
    // 文字列全体を囲む枠 (ハイライト) のジオメトリ。要素の大きさの計算と、Highlight 表示に使う
    Geometry? textHighLightGeometry;

    /// <summary>
    /// 影を付けた袋文字を作る。
    /// </summary>
    public OutlineText() => Effect = Common.DefaultShadowEffect;

    /// <summary>
    /// 要素の大きさを、文字列を囲む枠の大きさにする。ジオメトリがまだ無いときは 0 にする。
    /// </summary>
    protected override Size MeasureOverride(Size a)
        => textHighLightGeometry is null ? new Size(0, 0) : textHighLightGeometry.Bounds.Size;

    // すべての依存関係プロパティの変更時に呼ばれ、新しい値でジオメトリを作り直す
    static void OnOutlineTextInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((OutlineText)d).CreateText();

    /// <summary>
    /// 文字の輪郭のジオメトリを、縁取りのペンと塗りのブラシで描く。
    /// </summary>
    protected override void OnRender(DrawingContext drawingContext)
    {
        // 縁取用のペンを作成する
        var geometryPen = new Pen(Stroke, StrokeThickness);

        // 鋭角接合部の設定
        // ※ツノを出さないためには、接合部を円弧にするか、MiterLimitを0に設定する

        // プロパティで設定された接続方法を利用
        geometryPen.LineJoin = OutlineJoin;

        // Miterが選択されているときは、長さ制限設定
        if (geometryPen.LineJoin == PenLineJoin.Miter)
            geometryPen.MiterLimit = OutlineMiterLimit;

        if (EnableFill == true) { //塗りつぶしあり
            // 境界線描画後に塗りつぶしを上書きする場合
            // (線の内側半分が塗りで隠れ、外側半分だけが縁として見える。文字の太さが元の字形のまま保たれる)
            if (OverFill == true) {
                // 境界線だけ先に描画描画
                drawingContext.DrawGeometry(null, geometryPen, textGeometry);
                // 塗りつぶしのみ描画
                drawingContext.DrawGeometry(Fill, null, textGeometry);
            }
            // 境界線の内側を塗りつぶす場合（通常処理）
            else {
                // 1 回の DrawGeometry では塗り → 線の順に描かれるので、線が内側にもかかり、文字が細く見える
                drawingContext.DrawGeometry(Fill, geometryPen, textGeometry);
            }
        }
        //境界線のみ
        else {
            // 境界線のみ描画
            drawingContext.DrawGeometry(null, geometryPen, textGeometry);
        }

        // Draw the text highlight based on the properties that are set.
        if (Highlight)
            drawingContext.DrawGeometry(null, new Pen(Stroke, StrokeThickness), textHighLightGeometry);
    }


    /// <summary>
    /// Create the outline geometry based on the formatted text.
    /// </summary>
    public void CreateText()
    {
        var fontStyle  = Italic ? FontStyles.Italic : FontStyles.Normal ;
        var fontWeight = Bold   ? FontWeights.Bold  : FontWeights.Medium;

        // Create the formatted text based on the properties set.
        var formattedText = new FormattedText(
            Text,
            CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight,
            new Typeface(
                Font,
                fontStyle,
                fontWeight,
                FontStretches.Normal),
            FontSize,
            // 文字色はここでは使わない (ジオメトリだけを取り出し、色は OnRender の Fill / Stroke で決める)
            Brushes.Black,
            // 表示する画面の DPI に合わせて字形を作る
            VisualTreeHelper.GetDpi(this).PixelsPerDip
            );

        // Build the geometry object that represents the text.
        textGeometry = formattedText.BuildGeometry(new Point(0, 0));

        // MeasureOverride の外接矩形サイズで利用するため、常に外枠のジオメトリを生成しておく
        // Build the geometry object that represents the text hightlight.
        textHighLightGeometry = formattedText.BuildHighlightGeometry(new Point(0, 0));
    }

    /// <summary>
    /// 文字の内側を塗りつぶすかどうか。false のときは縁取りだけを描く (白抜き文字)。既定は true。
    /// </summary>
    public bool EnableFill
    {
        get => (bool)GetValue(EnableFillProperty);
        set => SetValue(EnableFillProperty, value);
    }

    /// <summary>
    /// <see cref="EnableFill"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty EnableFillProperty = DependencyProperty.Register(
        "EnableFill",
        typeof(bool),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 縁取りを先に描き、その上に塗りを重ねるかどうか。true だと縁取りが文字の外側だけに見える。既定は true。
    /// </summary>
    public bool OverFill
    {
        get => (bool)GetValue(OverFillProperty);
        set => SetValue(OverFillProperty, value);
    }

    /// <summary>
    /// <see cref="OverFill"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty OverFillProperty = DependencyProperty.Register(
        "OverFill",
        typeof(bool),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            true,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 縁取りの線の角 (接合部) の形。既定の Round では、鋭い角でも縁取りがツノのように尖らない。
    /// </summary>
    public PenLineJoin OutlineJoin
    {
        get => (PenLineJoin)GetValue(OutlineJoinProperty);
        set => SetValue(OutlineJoinProperty, value);
    }

    /// <summary>
    /// <see cref="OutlineJoin"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty OutlineJoinProperty = DependencyProperty.Register(
        "OutlineJoin",
        typeof(PenLineJoin),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            PenLineJoin.Round,  // デフォルトで角丸
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// <see cref="OutlineJoin"/> が <see cref="PenLineJoin.Miter"/> のときの、角の尖りの長さの上限 (線の太さの半分に対する比)。
    /// </summary>
    public double OutlineMiterLimit
    {
        get => (double)GetValue(OutlineMiterLimitProperty);
        set => SetValue(OutlineMiterLimitProperty, value);
    }

    /// <summary>
    /// <see cref="OutlineMiterLimit"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty OutlineMiterLimitProperty = DependencyProperty.Register(
        "OutlineMiterLimit",
        typeof(double),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            0.0,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 太字にするかどうか。false のときは Medium の太さになる。
    /// </summary>
    public bool Bold
    {
        get => (bool)GetValue(BoldProperty);
        set => SetValue(BoldProperty, value);
    }

    /// <summary>
    /// <see cref="Bold"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty BoldProperty = DependencyProperty.Register(
        "Bold",
        typeof(bool),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 文字の塗りのブラシ。
    /// </summary>
    public Brush Fill
    {
        get => (Brush)GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// <see cref="Fill"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty FillProperty = DependencyProperty.Register(
        "Fill",
        typeof(Brush),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            new SolidColorBrush(Common.DefaultFillColor),
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// フォントファミリー。
    /// </summary>
    public FontFamily Font
    {
        get => (FontFamily)GetValue(FontProperty);
        set => SetValue(FontProperty, value);
    }

    /// <summary>
    /// <see cref="Font"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty FontProperty = DependencyProperty.Register(
        "Font",
        typeof(FontFamily),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            new FontFamily(Common.DefaultFontFamilyName),
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// フォントサイズ。
    /// </summary>
    public double FontSize
    {
        get => (double)GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    /// <summary>
    /// <see cref="FontSize"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(
        "FontSize",
        typeof(double),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            (double)Common.DefaultFontSize,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 文字列を囲む枠 (ハイライト) も縁取りのペンで描くかどうか。
    /// </summary>
    public bool Highlight
    {
        get => (bool)GetValue(HighlightProperty);
        set => SetValue(HighlightProperty, value);
    }

    /// <summary>
    /// <see cref="Highlight"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register(
        "Highlight",
        typeof(bool),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 斜体にするかどうか。
    /// </summary>
    public bool Italic
    {
        get => (bool)GetValue(ItalicProperty);
        set => SetValue(ItalicProperty, value);
    }

    /// <summary>
    /// <see cref="Italic"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty ItalicProperty = DependencyProperty.Register(
        "Italic",
        typeof(bool),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            false,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 縁取りの線のブラシ。
    /// </summary>
    public Brush Stroke
    {
        get => (Brush)GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// <see cref="Stroke"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
        "Stroke",
        typeof(Brush),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            new SolidColorBrush(Common.DefaultFontStrokeColor),
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 縁取りの線の太さ。<see cref="OverFill"/> が true のときは、外側に見える縁の幅はこの半分になる。
    /// </summary>
    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// <see cref="StrokeThickness"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        "StrokeThickness",
        typeof(double),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            Common.DefaultStrokeThickness,
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );

    /// <summary>
    /// 表示する文字列。
    /// </summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// <see cref="Text"/> の依存関係プロパティ。
    /// </summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        "Text",
        typeof(string),
        typeof(OutlineText),
        new FrameworkPropertyMetadata(
            "",
            FrameworkPropertyMetadataOptions.AffectsRender,
            new PropertyChangedCallback(OnOutlineTextInvalidated),
            null
            )
        );
}
