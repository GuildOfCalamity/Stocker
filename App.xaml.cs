using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using UpdateViewer.Support;

namespace UpdateViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        internal const string MutexName = "WPF-STOCKVIEWER-11D-A54B-C36-12FB";
        public static string SystemTitle = "Stocker";
        public static bool SystemLoaded = false;
        private static readonly Mutex mutex = new Mutex(true, App.MutexName);
        public static Action<string> Print = s => Console.WriteLine($"[{DateTime.Now.ToString("hh:mm:ss.fff tt")}] {s}");

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            // Check Mutex to make sure only one instance is running.
            /*
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                mutex.ReleaseMutex();
            }
            else
            {
                Log.Write(eModule.App, $"Cannot run more than one instance of {SystemTitle}", true);
                WPFCustomMessageBox.CustomMessageBox.ShowOK($"  {SystemTitle} is already running.  ", SystemTitle, "Understood", MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
            */
        }

        /// <summary>
        /// Setup our textbox full text select event
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.TextBox), System.Windows.Controls.TextBox.GotFocusEvent, new RoutedEventHandler(TextBox_GotFocus));

            base.OnStartup(e);
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                (sender as System.Windows.Controls.TextBox).SelectAll();
                /*
                if (!string.IsNullOrEmpty((sender as System.Windows.Controls.TextBox).Name) && (sender as System.Windows.Controls.TextBox).Name.Equals("tbdatabase", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (!string.IsNullOrEmpty((sender as System.Windows.Controls.TextBox).Text))
                    {
                        (sender as System.Windows.Controls.TextBox).Text.OpenExplorerWindow();
                    }
                }
                */
            }
            catch { }
        }

        /*
        protected override void OnStartup(StartupEventArgs e)
        {
            // Select the text in a TextBox when it receives focus.
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(SelectivelyIgnoreMouseButton));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.GotKeyboardFocusEvent,
                new RoutedEventHandler(SelectAllText));
            EventManager.RegisterClassHandler(typeof(TextBox), TextBox.MouseDoubleClickEvent,
                new RoutedEventHandler(SelectAllText));
            base.OnStartup(e);
        }

        private void SelectivelyIgnoreMouseButton(object sender, MouseButtonEventArgs e)
        {
            // Find the TextBox
            DependencyObject parent = e.OriginalSource as UIElement;
            while (parent != null && !(parent is TextBox))
                parent = VisualTreeHelper.GetParent(parent);

            if (parent != null)
            {
                var textBox = (TextBox)parent;
                if (!textBox.IsKeyboardFocusWithin)
                {
                    // If the text box is not yet focused, give it the focus and
                    // stop further processing of this click event.
                    textBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void SelectAllText(object sender, RoutedEventArgs e)
        {
            var textBox = e.OriginalSource as TextBox;
            if (textBox != null)
                textBox.SelectAll();
        }
        */

        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Write(eModule.App, $"Unhandled exception thrown from Dispatcher {e.Dispatcher}: {e.Exception}", true);
                System.Diagnostics.EventLog.WriteEntry(SystemTitle, $"Unhandled exception thrown from Dispatcher {e.Dispatcher.ToString()}: {e.Exception.ToString()}");
                e.Handled = true;
            }
            catch { }

        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Log.Write(eModule.App, $"Unhandled exception thrown: {((Exception)e.ExceptionObject)}", true);
                System.Diagnostics.EventLog.WriteEntry(SystemTitle, $"Unhandled exception thrown:\r\n{((Exception)e.ExceptionObject).ToString()}");
            }
            catch { }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            try
            {
                Print($"{SystemTitle} is closing the mutex and shutting down ({e.ApplicationExitCode})");
                mutex.Close();
            }
            catch (Exception ex)
            {
                Print($"ApplicationExit(ERROR): {ex.Message}");
            }
        }


        /// <summary>
        /// Initializes the DI container.
        /// </summary>
        /// <returns>An instance implementing IServiceProvider.</returns>
        /*
        private IServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();

            var navigationService = new NavigationService();
            navigationService.Configure(nameof(MainWindow), typeof(MainWindow));
            navigationService.Configure(nameof(EditView), typeof(EditView));

            services.AddSingleton<INavigationService>(navigationService);
            services.AddSingleton<IDataService, DataService>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<ItemDetailsViewModel>();

            return services.BuildServiceProvider();
        }
        */
    }
}
