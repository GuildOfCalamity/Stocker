using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UpdateViewer.ViewModels;

namespace UpdateViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Threading.DispatcherTimer _popTimer = null;
        private const double popupStay = 1.25;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainViewModel._MainInstance.OnWindowLoaded;
            this.Closing += MainViewModel._MainInstance.OnWindowClosing;

            //UpdateView uv = new UpdateView();
            //<DockPanel x:Name="ContentArea" Grid.Row="1" />
            //this.ContentArea.Children.Add(uv); 
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Cursor = Cursors.Hand;
                DragMove();
            }
            Cursor = Cursors.Arrow;
        }

        private void mainPopup_Opened(object sender, EventArgs e)
        {
            //*** Start timer to close ***
            //Create the timer for our clock text
            if (_popTimer == null)
            {
                _popTimer = new System.Windows.Threading.DispatcherTimer();
                _popTimer.Interval = TimeSpan.FromSeconds(popupStay);
                _popTimer.Tick += popTimer_Tick;
                _popTimer.Start();
            }
            else
            {
                if (!_popTimer.IsEnabled)
                    _popTimer.Start();
            }

        }

        void popTimer_Tick(object sender, EventArgs e)
        {
            App.Print($"popTimer fired at {DateTime.Now.ToLongTimeString()}");
            mainPopup.IsOpen = false;
            _popTimer.Stop();
        }

    }
}
