using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WpfCad2Lib.Wpf
{
    public static class Common
    {
        public const double          DefaultStrokeThickness  = 3.0;
        public const double          SelectorStrokeThickness = 2.0;
        public const double          SelectorRadius          = 6.0;
        public const double          DefaultFontSize         = 48.0;

        public readonly static Color DefaultLightColor       = Colors.White;
        public readonly static Color DefaultDarkColor        = Colors.Black;

        public readonly static Color DefaultStrokeColor      = Colors.Red;
        public readonly static Color DefaultFillColor        = Colors.Red;
        public readonly static Color SelectorStrokeColor     = Color.FromArgb(0x80, Colors.MediumOrchid   .R, Colors.MediumOrchid   .G, Colors.MediumOrchid   .B);
        public readonly static Color SelectorFillColor       = Color.FromArgb(0x80, Colors.MediumSlateBlue.R, Colors.MediumSlateBlue.G, Colors.MediumSlateBlue.B);

        public readonly static Color DefaultFontStrokeColor  = DefaultFillColor.IsDark() ? DefaultLightColor : DefaultDarkColor;
        public const string          DefaultFontFamilyName   = "メイリオ";

        public const double          DefaultShadowDepth      = 3.0;
        public const double          DefaultShadowDirection  = 330.0;
        public readonly static Color DefaultShadowColor      = Colors.Black;
        public const double          DefaultShadowOpacity    = 0.5;
        public const double          DefaultShadowBlurRadius = 4.0;

        public const double          NearDistanceRate        = 0.04;

        public const double          ZoomMinimumRate         = 1.0;
        public const double          ZoomMaximumRate         = 16.0;
        public const double          MouseWheelZoomRate      = 1.0 / 8.0;
        public const double          DefaultPanRate          = 1.0 / 8.0;

        public static DropShadowEffect DefaultShadowEffect
        {
            get {
                return new DropShadowEffect {
                    ShadowDepth = Common.DefaultShadowDepth     ,
                    Direction   = Common.DefaultShadowDirection ,
                    Color       = Common.DefaultShadowColor     ,
                    Opacity     = Common.DefaultShadowOpacity   ,
                    BlurRadius  = Common.DefaultShadowBlurRadius
                };
            }
        }
    }
}
