using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace WpfCad2Lib.Core
{
    public class Subject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void RaisePropertyChanged<PropertyType>(Expression<Func<PropertyType>> propertyExpression)
            => RaisePropertyChanged(ObjectExtensions.GetMemberName(propertyExpression));

        void RaisePropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
