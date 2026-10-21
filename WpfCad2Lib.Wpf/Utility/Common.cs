using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// ライブラリ全体で使う既定値 (線の太さ・色・影・ズーム / パンの量など) をまとめたクラス。
/// </summary>
public static class Common
{
    /// <summary>図形の線の既定の太さ。</summary>
    public const double          DefaultStrokeThickness  = 3.0;
    /// <summary>選択マーカー (選択した図形の頂点などに出す円) の線の太さ。</summary>
    public const double          SelectorStrokeThickness = 2.0;
    /// <summary>選択マーカーの円の半径。</summary>
    public const double          SelectorRadius          = 6.0;
    /// <summary>文字列の既定のフォントサイズ。</summary>
    public const double          DefaultFontSize         = 48.0;

    /// <summary>袋文字の縁取りに使う明るい色。暗い文字色のときに使う。</summary>
    public static readonly Color DefaultLightColor       = Colors.White;
    /// <summary>袋文字の縁取りに使う暗い色。明るい文字色のときに使う。</summary>
    public static readonly Color DefaultDarkColor        = Colors.Black;

    /// <summary>図形の線の既定の色。</summary>
    public static readonly Color DefaultStrokeColor      = Colors.Red;
    /// <summary>図形の塗りの既定の色。</summary>
    public static readonly Color DefaultFillColor        = Colors.Red;
    // 選択マーカーは、下の図形や背景が透けて見えるよう、アルファ 0x80 (約 50%) の半透明にする
    /// <summary>選択マーカーの線の色 (半透明)。</summary>
    public static readonly Color SelectorStrokeColor     = Color.FromArgb(0x80, Colors.MediumOrchid   .R, Colors.MediumOrchid   .G, Colors.MediumOrchid   .B);
    /// <summary>選択マーカーの塗りの色 (半透明)。</summary>
    public static readonly Color SelectorFillColor       = Color.FromArgb(0x80, Colors.MediumSlateBlue.R, Colors.MediumSlateBlue.G, Colors.MediumSlateBlue.B);

    // 縁取りは文字の塗りと反対の明るさにして、どんな背景の上でも文字が読めるようにする
    /// <summary>袋文字の縁取りの既定の色。既定の塗りの色が暗ければ明るい色、明るければ暗い色にする。</summary>
    public static readonly Color DefaultFontStrokeColor  = DefaultFillColor.IsDark() ? DefaultLightColor : DefaultDarkColor;
    /// <summary>文字列の既定のフォント名。</summary>
    public const string          DefaultFontFamilyName   = "メイリオ";

    /// <summary>影の、図形からのずれの量。</summary>
    public const double          DefaultShadowDepth      = 3.0;
    /// <summary>影を落とす向き (度)。0 が右で反時計回りに測るので、330 は右やや下になる。</summary>
    public const double          DefaultShadowDirection  = 330.0;
    /// <summary>影の色。</summary>
    public static readonly Color DefaultShadowColor      = Colors.Black;
    /// <summary>影の不透明度。</summary>
    public const double          DefaultShadowOpacity    = 0.5;
    /// <summary>影のぼかしの半径。</summary>
    public const double          DefaultShadowBlurRadius = 4.0;

    /// <summary>
    /// 図形を選択するときに「近い」とみなす距離の、モデル領域の大きさ (幅と高さの平均) に対する割合。
    /// 画像の大きさに比例させるので、背景画像の解像度が変わっても選択しやすさが変わらない。
    /// </summary>
    public const double          NearDistanceRate        = 0.04;

    /// <summary>ズーム倍率の下限。1.0 はモデル領域全体がちょうどビューに収まる倍率 (ホームの状態)。</summary>
    public const double          ZoomMinimumRate         = 1.0;
    /// <summary>ズーム倍率の上限。</summary>
    public const double          ZoomMaximumRate         = 16.0;
    /// <summary>マウスホイール 1 ノッチあたりのズームの増分。1 ノッチで (1 + 1/8) 倍になる。</summary>
    public const double          MouseWheelZoomRate      = 1.0 / 8.0;
    /// <summary>矢印キー 1 回あたりのパンの量の、ビューの対角線の長さに対する割合。</summary>
    public const double          DefaultPanRate          = 1.0 / 8.0;

    /// <summary>
    /// 図形と選択マーカーに付ける影の効果。
    /// </summary>
    // 図形ごとに作らず、Freeze した 1 つのインスタンスを共有する
    public static DropShadowEffect DefaultShadowEffect { get; } = CreateDefaultShadowEffect();

    static DropShadowEffect CreateDefaultShadowEffect()
    {
        var effect = new DropShadowEffect {
            ShadowDepth = Common.DefaultShadowDepth     ,
            Direction   = Common.DefaultShadowDirection ,
            Color       = Common.DefaultShadowColor     ,
            Opacity     = Common.DefaultShadowOpacity   ,
            BlurRadius  = Common.DefaultShadowBlurRadius
        };
        // Freeze すると変更不可になり、変更通知の監視が不要になるので、多くの要素で共有しても軽い
        effect.Freeze();
        return effect;
    }
}
