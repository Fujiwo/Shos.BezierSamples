using WpfCad2Lib.Core;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace WpfCad2Lib.Wpf;

/// <summary>
/// 図形の見た目の属性 (線の色・塗りつぶしの色・線の太さ・文字の大きさ)。
/// </summary>
/// <remarks>
/// プロパティの値が変わると PropertyChanged を発生させる (<see cref="Subject"/> の機能)。
/// Figure はこの通知を購読して、表示中の要素に属性を設定し直す。
/// Model も「次に作る図形の属性」として 1 つ持ち、AddCommand が図形を作るたびに <see cref="Clone"/> した複製を図形に渡す。
/// </remarks>
public class FigureAttribute : Subject, ICloneable
{
    /// <summary>線の色。</summary>
    public Color StrokeColor
    {
        get;
        set
        {
            // 値が変わったときだけ通知する (同じ値の代入で図形を無駄に再設定しないため)。他のプロパティも同様
            if (value != field) {
                field = value;
                RaisePropertyChanged(() => StrokeColor);
            }
        }
    } = Common.DefaultStrokeColor;

    /// <summary>塗りつぶしの色。塗りつぶす図形 (Figure.IsFilled が true) と文字の内側に使う。</summary>
    public Color FillColor
    {
        get;
        set {
            if (value != field) {
                field = value;
                RaisePropertyChanged(() => FillColor);
            }
        }
    } = Common.DefaultFillColor;

    /// <summary>線の太さ。文字の場合は縁取りの太さ。</summary>
    public double StrokeThickness
    {
        get;
        set
        {
            if (value != field) {
                field = value;
                RaisePropertyChanged(() => StrokeThickness);
            }
        }
    } = Common.DefaultStrokeThickness;

    /// <summary>文字の大きさ。</summary>
    public double FontSize
    {
        get;
        set
        {
            if (value != field) {
                field = value;
                RaisePropertyChanged(() => FontSize);
            }
        }
    } = Common.DefaultFontSize;

    //public void Copy(FigureAttribute figureAttribute)
    //{
    //    figureAttribute.StrokeColor     = StrokeColor    ;
    //    figureAttribute.FillColor       = FillColor      ;
    //    figureAttribute.StrokeThickness = StrokeThickness;
    //    figureAttribute.FontSize        = FontSize       ;
    //}

    /// <summary>線の色と塗りつぶしの色を同じ色にする。</summary>
    public void SetColor(Color color) => StrokeColor = FillColor = color;

    /// <summary>この属性を、図形の表示用の要素すべてに設定する。</summary>
    /// <param name="elements">図形の表示用の要素。</param>
    /// <param name="isFilled">塗りつぶすかどうか (Figure.IsFilled)。</param>
    public void SetAttribute(IEnumerable<FrameworkElement> elements, bool isFilled)
        => elements.ForEach(element => SetAttribute(element, isFilled));

    // 要素の種類によって設定するプロパティが違う。Shape (Line・Path など) と袋文字 (OutlineText) を分けて扱う
    void SetAttribute(FrameworkElement element, bool isFilled)
    {
        if (element is Shape shape)
            SetShapeAttribute(shape, StrokeThickness, StrokeColor, FillColor, isFilled);
        else if (element is OutlineText text)
            SetTextAttribute(text, StrokeThickness, FontSize, FillColor, isFilled);
    }

    static void SetShapeAttribute(Shape shape, double strokeThickness, Color strokeColor, Color fillColor, bool isFilled)
    {
        shape.Stroke          = Helper.CreateStrokeBrush(strokeColor);
        shape.StrokeThickness = strokeThickness;
        shape.Fill            = isFilled ? Helper.CreateFillBrush(fillColor) : null;
        shape.Effect          = Common.DefaultShadowEffect;
    }

    // 袋文字は内側を FillColor で塗り、縁取りは読みやすいよう FillColor の明暗と反対の色 (暗い色なら明るい色) にする
    static void SetTextAttribute(OutlineText text, double strokeThickness, double fontSize, Color fillColor, bool isFilled)
    {
        text.Stroke          = Helper.CreateStrokeBrush(fillColor.IsDark() ? Common.DefaultLightColor : Common.DefaultDarkColor);
        text.StrokeThickness = strokeThickness;
        text.Fill            = isFilled ? Helper.CreateFillBrush(fillColor) : Brushes.Transparent;
        text.FontSize        = fontSize;
        text.Effect          = Common.DefaultShadowEffect;
    }

    /// <summary>同じ値を持つ新しい属性を返す。PropertyChanged の購読者は複製しない。</summary>
    // MemberwiseClone() では PropertyChanged の購読者まで複製されるため、値だけを写した新しいインスタンスを返す
    public object Clone() => new FigureAttribute {
        StrokeColor     = StrokeColor    ,
        FillColor       = FillColor      ,
        StrokeThickness = StrokeThickness,
        FontSize        = FontSize
    };
}
