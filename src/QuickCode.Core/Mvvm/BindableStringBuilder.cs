using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace QuickCode.Core.Mvvm
{
    public class BindableStringBuilder : INotifyPropertyChanged
    {
        #region Event

        public event PropertyChangedEventHandler PropertyChanged;

        private readonly EventHandler<EventArgs> TextChanged;

        #endregion

        #region Var

        private readonly StringBuilder mBuilder = new();
       

        #endregion

        #region Fields

        public string Text
        {
            get 
            { 
                return mBuilder.ToString(); 
            }
        }

        public int Count
        {
            get { return mBuilder.Length; }
        }

        public void Append(string text)
        {            
            mBuilder.Append(text);
            TextChanged?.Invoke(this, null);

            RaisePropertyChanged(() => Text);
        }

        public void AppendLine(string text)
        {
            mBuilder.AppendLine(text);
            TextChanged?.Invoke(this, null);

            RaisePropertyChanged(() => Text);
        }

        public void Clear()
        {
            mBuilder.Clear();

            TextChanged?.Invoke(this, null);
            RaisePropertyChanged(() => Text);
        }

        #endregion

        /*public void RaisePropertyChanged(string property)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }*/

        public void RaisePropertyChanged<T>(Expression<Func<T>> propertyExpression)
        {
            if (propertyExpression == null)
            {
                return;
            }

            var handler = PropertyChanged;

            if (handler != null)
            {
                if (propertyExpression.Body is MemberExpression body)
                    handler(this, new PropertyChangedEventArgs(body.Member.Name));
            }
        }

    }
}
