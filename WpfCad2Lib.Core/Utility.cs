using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WpfCad2Lib.Core;

/// <summary>
/// <see cref="IEnumerable{T}"/> の拡張メソッド。
/// </summary>
public static class EnumerableExtension
{
    extension<T>(IEnumerable<T> collection)
    {
        /// <summary>
        /// すべての要素に対して <paramref name="action"/> を実行する (<see cref="List{T}.ForEach"/> を任意の列挙に広げたもの)。
        /// </summary>
        public void ForEach(Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }
    }

    //public static int IndexOf<T>(this IEnumerable<T> collection, Func<T, bool> isMatch)
    //{
    //    int index = 0;
    //    foreach (var item in collection) {
    //        if (isMatch(item))
    //            return index;
    //        index++;
    //    }
    //    return -1;
    //}

    //public static int IndexOf<T>(this IEnumerable<T> collection, T target)
    //{
    //    return collection.IndexOf(item => item.Equals(target));
    //}
}

/// <summary>
/// 式木 (<see cref="Expression"/>) からメンバー名を取り出すなど、オブジェクトに関する補助メソッド。
/// </summary>
public static class ObjectExtensions
{
    //public static object Eval(this object item, string propertyName)
    //{
    //    var propertyInfo = item.GetType().GetProperty(propertyName);
    //    return propertyInfo == null ? null : propertyInfo.GetValue(item, null);
    //}

    //public static string GetMemberName<ObjectType, MemberType>(this ObjectType @this, Expression<Func<ObjectType, MemberType>> expression)
    //{
    //    return ((MemberExpression)expression.Body).Member.Name;
    //}

    /// <summary>
    /// <c>() =&gt; Property</c> の形のラムダ式から、メンバーの名前 ("Property") を返す。
    /// </summary>
    /// <remarks>
    /// ラムダ式を <see cref="Expression{TDelegate}"/> として受け取ると、実行されずに式木のまま渡される。
    /// その本体 (Body) はメンバーへのアクセスを表す <see cref="MemberExpression"/> なので、Member.Name で名前が分かる。
    /// メンバーアクセス以外の式を渡すとキャストで例外になる。
    /// </remarks>
    public static string GetMemberName<MemberType>(Expression<Func<MemberType>> expression)
        => ((MemberExpression)expression.Body).Member.Name;
}
