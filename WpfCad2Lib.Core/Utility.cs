using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace WpfCad2Lib.Core
{
    public static class EnumerableExtension
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var item in collection)
                action(item);
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

        public static string GetMemberName<MemberType>(Expression<Func<MemberType>> expression)
            => ((MemberExpression)expression.Body).Member.Name;
    }
}
