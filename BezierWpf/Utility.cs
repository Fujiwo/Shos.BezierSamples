using System;
using System.Collections.Generic;

namespace BezierWpf;

/// <summary>
/// <see cref="IEnumerable{T}"/> の拡張メソッド。
/// </summary>
public static class EnumerableExtension
{
    extension<T>(IEnumerable<T> collection)
    {
        /// <summary>
        /// コレクションの各要素に <paramref name="action"/> を実行する (<see cref="List{T}.ForEach"/> を任意の列挙で使えるようにしたもの)。
        /// </summary>
        public void ForEach(Action<T> action)
        {
            foreach (var item in collection)
                action(item);
        }
    }
}
