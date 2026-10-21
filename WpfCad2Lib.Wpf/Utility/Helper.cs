using System.Windows;
using System.Windows.Media;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 図形の線と塗りに使うグラデーションブラシを作るヘルパー。
/// </summary>
public static class Helper
{
    /// <summary>
    /// 線用のブラシを作る。左上から右下へ少しずつ暗くなるグラデーションで、光が左上から当たったような立体感を出す。
    /// 不透明度は元の色のまま変えない。
    /// </summary>
    public static Brush CreateStrokeBrush(Color strokeColor)
    {
        // StartPoint / EndPoint は、塗る対象の外接矩形を (0, 0)〜(1, 1) とする相対座標 (既定の MappingMode = RelativeToBoundingBox)。
        // (0, 0) → (1, 1) なので、図形の大きさによらず左上から右下への対角線方向のグラデーションになる
        var brush = new LinearGradientBrush { StartPoint = new Point(0.0, 0.0), EndPoint = new Point(1.0, 1.0) };
        // 左上 0.95 倍 → 中央の 0.3〜0.7 は 0.80 倍で一定 → 右下 0.65 倍、と明るさを変える
        brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.95f), 0.0));
        brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.80f), 0.3));
        brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.80f), 0.7));
        brush.GradientStops.Add(new GradientStop(strokeColor.MultiplyRGB(0.65f), 1.0));
        // 変更しないブラシは Freeze しておくと、変更通知の監視が不要になって描画が軽くなる
        brush.Freeze();
        return brush;
    }

    /// <summary>
    /// 塗り用のブラシを作る。線用と同じ左上から右下へのグラデーションだが、<see cref="Color.Multiply"/> でアルファも掛けるので、
    /// 右下ほど半透明になり、背景の画像が少し透けて見える。
    /// </summary>
    public static Brush CreateFillBrush(Color fillColor)
    {
        var brush = new LinearGradientBrush { StartPoint = new Point(0.0, 0.0), EndPoint = new Point(1.0, 1.0) };
        brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.95f), 0.0));
        brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.80f), 0.3));
        brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.80f), 0.7));
        brush.GradientStops.Add(new GradientStop(fillColor.Multiply(0.65f), 1.0));
        brush.Freeze();
        return brush;
    }
}

/// <summary>
/// <see cref="Color"/> の拡張メソッド。
/// </summary>
public static class HelperExtensions
{
    extension(Color color)
    {
        /// <summary>
        /// 暗い色かどうかを返す。R・G・B の平均が中間 (0.5) より小さければ暗いとみなす。
        /// </summary>
        /// <remarks>
        /// ScR / ScG / ScB は 0.0〜1.0 の scRGB (リニア) の値なので、3 成分の和を 3 / 2 と比べている。
        /// 人の目の感度 (緑に敏感) は考慮しない簡易な判定である。
        /// </remarks>
        public bool IsDark()
            => color.ScR + color.ScG + color.ScB < 3.0f / 2.0f;

        /// <summary>
        /// アルファを含む 4 成分すべてに <paramref name="coefficient"/> を掛けた色を返す。
        /// 1 より小さい係数では、暗くなると同時に半透明になる。
        /// </summary>
        public Color Multiply(float coefficient)
            => Color.Multiply(color, coefficient);

        /// <summary>
        /// R・G・B だけに <paramref name="coefficient"/> を掛け、アルファはそのままにした色を返す (不透明度を保ったまま暗くする)。
        /// </summary>
        public Color MultiplyRGB(float coefficient)
            => new Color { ScR = color.ScR * coefficient, ScG = color.ScG * coefficient, ScB = color.ScB * coefficient, ScA = color.ScA };
    }
}
