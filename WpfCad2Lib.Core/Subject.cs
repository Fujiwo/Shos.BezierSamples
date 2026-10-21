using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WpfCad2Lib.Core;

/// <summary>
/// プロパティの変更を通知するクラスの基底 (Observer パターンの Subject)。
/// 派生クラスはプロパティの setter で <see cref="RaisePropertyChanged{PropertyType}"/> を呼ぶ。
/// </summary>
public class Subject : INotifyPropertyChanged
{
    /// <summary>
    /// プロパティの値が変わったときに発生する。
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// <see cref="PropertyChanged"/> イベントを発生させる。
    /// プロパティ名は <c>RaisePropertyChanged(() =&gt; StrokeColor)</c> のようにラムダ式で渡す。
    /// </summary>
    /// <remarks>
    /// 名前を文字列で書くとプロパティ名を変えたときに直し忘れるが、式で渡せばコンパイラーが検査し、名前の変更にも追従する。
    /// (C# 5 以降なら [CallerMemberName] や nameof でも同じことができる。)
    /// </remarks>
    protected void RaisePropertyChanged<PropertyType>(Expression<Func<PropertyType>> propertyExpression)
        => RaisePropertyChanged(ObjectExtensions.GetMemberName(propertyExpression));

    void RaisePropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
