using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace UpdateViewer.Command
{
    internal class CommandHandler : ICommand
    {
        private Action _action;

        public CommandHandler(Action action)
        {
            _action = action;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            _action();
        }

        #endregion

        public override string ToString() => $"CommandHandler bound to event {_action.Method.Name}";

    }
    internal class CommandHandler<T> : ICommand
    {
        private Action<T> _action;

        public CommandHandler(Action<T> action)
        {
            _action = action;
        }

        #region ICommand Members

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        public void Execute(object parameter)
        {
            Type type = typeof(T);
            if (type.IsValueType)
            {
                var converter = TypeDescriptor.GetConverter(type);
                T value = default(T);
                if (converter != null)
                {
                    try
                    {
                        value = (T)converter.ConvertFrom(parameter);
                    }
                    catch
                    {
                        try
                        {   // Sometimes the ConvertFrom() throws an exception
                            // for boolean bindings that are not static...
                            // CommandParameter="True" or CommandParameter="False"
                            if (typeof(T) == typeof(bool))
                            {
                                if (parameter != null)
                                    value = (T)parameter;
                                else
                                    value = default(T);
                            }
                            else
                            {
                                value = default(T);
                            }
                        }
                        catch (Exception ex)
                        {
                            App.Print($"Execute(parameter): {ex.Message}");
                        }
                    }
                }
                _action((T)(object)value);
            }
            else if (parameter is T)
                _action((T)parameter);
            else
                throw new ArgumentException();
        }

        #endregion

        public override string ToString() => $"CommandHandler<{typeof(T).Name}> bound to event {_action.Method.Name}";



        // bool b = GetDefaultValue<bool>("false");
        protected static T GetDefaultValue<T>(object propertyName)
        {
            var tc = TypeDescriptor.GetConverter(typeof(T));
            return (T)tc.ConvertFrom(propertyName);
        }
        protected static T ChangeType<T>(object value)
        {
            return (T)ChangeType(typeof(T), value);
        }
        protected static object ChangeType(Type t, object value)
        {
            TypeConverter tc = TypeDescriptor.GetConverter(t);
            return tc.ConvertFrom(value);
        }
    }
}
