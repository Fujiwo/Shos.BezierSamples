namespace WpfCad2Lib.Core;

/// <summary>
/// 数値計算の補助メソッド。
/// </summary>
/// <remarks><see cref="System.Math"/> と名前がぶつからないよう、MathHelper という名前にしている。</remarks>
public static class MathHelper
{
    /// <summary>
    /// 2 つの値の平均を返す。
    /// </summary>
    public static double Average(double value1, double value2)
        => (value1 + value2) / 2.0;
}
