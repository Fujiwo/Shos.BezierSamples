using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf
{
	public class OutlineText : FrameworkElement
	{
		Geometry textGeometry          = null;
        Geometry textHighLightGeometry = null;

        public OutlineText() => Effect = Common.DefaultShadowEffect;

		protected override Size MeasureOverride(Size a)
			=> textHighLightGeometry == null ? new Size(0, 0) : textHighLightGeometry.Bounds.Size;

        static void OnOutlineTextInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
			=> ((OutlineText)d).CreateText();

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

            if (EnableFill == true) //塗りつぶしあり
			{
                // 境界線描画後に塗りつぶしを上書きする場合
                if (OverFill == true) {
                    // 境界線だけ先に描画描画
                    drawingContext.DrawGeometry(null, geometryPen, textGeometry);
                    // 塗りつぶしのみ描画
                    drawingContext.DrawGeometry(Fill, null, textGeometry);
                }
                    // 境界線の内側を塗りつぶす場合（通常処理）
                else {
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
				CultureInfo.CurrentCulture ,
				FlowDirection.LeftToRight,
				new Typeface(
					Font,
					fontStyle,
					fontWeight,
					FontStretches.Normal),
					FontSize,
					Brushes.Black // This brush does not matter since we use the geometry of the text. 
				);

			// Build the geometry object that represents the text.
			textGeometry = formattedText.BuildGeometry(new Point(0, 0));

			// MeasureOverride の外接矩形サイズで利用するため、常に外枠のジオメトリを生成しておく
			// Build the geometry object that represents the text hightlight.
			textHighLightGeometry = formattedText.BuildHighlightGeometry(new Point(0, 0));
		}

		public bool EnableFill
		{
			get { return (bool)GetValue(EnableFillProperty); }
			set { SetValue(EnableFillProperty, value); }
		}

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

		public bool OverFill
		{
			get { return (bool)GetValue(OverFillProperty); }
			set { SetValue(OverFillProperty, value); }
		}

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
	
		public PenLineJoin OutlineJoin
		{
			get { return (PenLineJoin)GetValue(OutlineJoinProperty); }
			set { SetValue(OutlineJoinProperty, value); }
		}

		public static readonly DependencyProperty OutlineJoinProperty = 
			DependencyProperty.Register(
			"OutlineJoin",
			typeof(PenLineJoin),
			typeof(OutlineText),
			new FrameworkPropertyMetadata(
				PenLineJoin.Round,	// デフォルトで角丸
				FrameworkPropertyMetadataOptions.AffectsRender,
				new PropertyChangedCallback(OnOutlineTextInvalidated),
				null
				)
			);

		public double OutlineMiterLimit
		{
			get { return (double)GetValue(OutlineMiterLimitProperty); }
			set { SetValue(OutlineMiterLimitProperty, value); }
		}

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
	
		public bool Bold
		{
			get { return (bool)GetValue(BoldProperty); }
			set { SetValue(BoldProperty, value); }
		}

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

		public Brush Fill
		{
			get { return (Brush)GetValue(FillProperty); }
			set { SetValue(FillProperty, value); }
		}

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

		public FontFamily Font
		{
			get { return (FontFamily)GetValue(FontProperty); }
			set { SetValue(FontProperty, value); }
		}

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

		public double FontSize
		{
			get { return (double)GetValue(FontSizeProperty); }
			set { SetValue(FontSizeProperty, value); }
		}

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

		public bool Highlight
		{
			get { return (bool)GetValue(HighlightProperty); }
			set { SetValue(HighlightProperty, value); }
		}

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

		public bool Italic
		{
			get { return (bool)GetValue(ItalicProperty); }
			set { SetValue(ItalicProperty, value); }
		}

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

		public Brush Stroke
		{
			get { return (Brush)GetValue(StrokeProperty); }
			set { SetValue(StrokeProperty, value); }
		}

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

		public double StrokeThickness
		{
            get { return (double)GetValue(StrokeThicknessProperty); }
			set { SetValue(StrokeThicknessProperty, value); }
		}

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

		public string Text
		{
			get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
		}

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
}
