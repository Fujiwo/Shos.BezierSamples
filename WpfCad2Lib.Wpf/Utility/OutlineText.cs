using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf
{
	public class OutlineText : FrameworkElement
	{
		Geometry textGeometry          = null;
        Geometry textHighLightGeometry = null;

        public OutlineText()
        {
            Effect = Common.DefaultShadowEffect;
        }

		protected override Size MeasureOverride(Size a)
		{
            return textHighLightGeometry == null ? new Size(0, 0) : textHighLightGeometry.Bounds.Size;
		}

        static void OnOutlineTextInvalidated(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((OutlineText)d).CreateText();
		}

        protected override void OnRender(DrawingContext drawingContext)
        {
            // ‰æ—p‚Ìƒyƒ“‚ğì¬‚·‚é
            var geometryPen = new Pen(Stroke, StrokeThickness);

            // ‰sŠpÚ‡•”‚Ìİ’è
            // ¦ƒcƒm‚ğo‚³‚È‚¢‚½‚ß‚É‚ÍAÚ‡•”‚ğ‰~ŒÊ‚É‚·‚é‚©AMiterLimit‚ğ0‚Éİ’è‚·‚é

            // ƒvƒƒpƒeƒB‚Åİ’è‚³‚ê‚½Ú‘±•û–@‚ğ—˜—p
            geometryPen.LineJoin = OutlineJoin;

            // Miter‚ª‘I‘ğ‚³‚ê‚Ä‚¢‚é‚Æ‚«‚ÍA’·‚³§ŒÀİ’è
            if (geometryPen.LineJoin == PenLineJoin.Miter)
                geometryPen.MiterLimit = OutlineMiterLimit;

            if (EnableFill == true) //“h‚è‚Â‚Ô‚µ‚ ‚è
			{
                // ‹«ŠEü•`‰æŒã‚É“h‚è‚Â‚Ô‚µ‚ğã‘‚«‚·‚éê‡
                if (OverFill == true) {
                    // ‹«ŠEü‚¾‚¯æ‚É•`‰æ•`‰æ
                    drawingContext.DrawGeometry(null, geometryPen, textGeometry);
                    // “h‚è‚Â‚Ô‚µ‚Ì‚İ•`‰æ
                    drawingContext.DrawGeometry(Fill, null, textGeometry);
                }
                    // ‹«ŠEü‚Ì“à‘¤‚ğ“h‚è‚Â‚Ô‚·ê‡i’Êíˆ—j
                else {
                    drawingContext.DrawGeometry(Fill, geometryPen, textGeometry);
                }
            }
                //‹«ŠEü‚Ì‚İ
            else {
                // ‹«ŠEü‚Ì‚İ•`‰æ
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

			// MeasureOverride ‚ÌŠOÚ‹éŒ`ƒTƒCƒY‚Å—˜—p‚·‚é‚½‚ßAí‚ÉŠO˜g‚ÌƒWƒIƒƒgƒŠ‚ğ¶¬‚µ‚Ä‚¨‚­
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
				PenLineJoin.Round,	// ƒfƒtƒHƒ‹ƒg‚ÅŠpŠÛ
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
