using System;
using System.Collections.Generic;
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
using UpdateViewer.DataModels;
using UpdateViewer.ViewModels;

namespace UpdateViewer
{
    /// <summary>
    /// Interaction logic for UpdateView.xaml
    /// </summary>
    public partial class UpdateView : UserControl
    {
        private int previousCount = 0;
        private int previousIdx = -1;

        public UpdateView()
        {
            InitializeComponent();
            this.Loaded += MainViewModel._MainInstance.OnUpdateViewLoaded;
            listEntries.MouseDoubleClick += MainViewModel._MainInstance.OnListMouseDoubleClick;
        }


        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainViewModel._MainInstance != null && sender is ListView)
            {
                try
                {
                    ListView lv = sender as ListView;
                    MainViewModel._MainInstance.SelectedIdx = lv.SelectedIndex;
                    App.Print($"SelectedIndex={MainViewModel._MainInstance.SelectedIdx}");
                }
                catch (Exception ex)
                {
                    App.Print($"SelectionChanged(ERROR): {ex.Message}");
                    App.Print($"_MainInstance.SelectedIdx: {MainViewModel._MainInstance.SelectedIdx}");
                }
            }
        }

        private void ListViewItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListViewItem)
            {
                ListViewItem lvi = sender as ListViewItem;
                var data = (Stock)lvi.Content;
                if (data != null)
                {
                    /*
                    NOTE: We could use this to trap mouse events instead of binding.

                    System.Diagnostics.Debug.WriteLine($"> Code....: {data.Code}");
                    System.Diagnostics.Debug.WriteLine($"> Name....: {data.Name}");
                    System.Diagnostics.Debug.WriteLine($"> BuyQty..: {data.BuyQty}");
                    System.Diagnostics.Debug.WriteLine($"> BuyPrice: {data.BuyPrice}");
                    */
                }
            }
        }

        private void listEntries_LayoutUpdated(object sender, EventArgs e)
        {
            if (listEntries.Items.Count > 0) //&& (listEntries.Items.Count != previousCount)
            {
                if (MainViewModel._MainInstance != null && MainViewModel._MainInstance.externalScroll)
                {
                    MainViewModel._MainInstance.externalScroll = false;
                    ScrollToLastItem();
                }
                else if (MainViewModel._MainInstance != null && previousIdx != MainViewModel._MainInstance.SelectedIdx)
                {
                    previousIdx = MainViewModel._MainInstance.SelectedIdx;
                    ScrollMethod1(MainViewModel._MainInstance.SelectedIdx);
                }
            }
            /*
            if (listEntries.Items.Count > 0) //&& (listEntries.Items.Count != previousCount)
            {
                if (MainViewModel._MainInstance != null && MainViewModel._MainInstance.SelectedIdx > 0)
                {
                    if (previousCount != MainViewModel._MainInstance.SelectedIdx)
                    {
                        System.Diagnostics.Debug.WriteLine($"<< TotalList: {listEntries.Items.Count}>>");
                        System.Diagnostics.Debug.WriteLine($"<< SelectedIdx: {MainViewModel._MainInstance.SelectedIdx}>>");
                        previousCount = MainViewModel._MainInstance.SelectedIdx;
                        ScrollMethod1(MainViewModel._MainInstance.SelectedIdx);
                        //ScrollMethod2(listEntries);
                    }
                }
            }
            */
        }

        private void ScrollMethod1(int index)
        {
            try
            {
                if (index == -1)
                    return;

                listEntries.SelectedItem = listEntries.Items.GetItemAt(index); //listEntries.SelectedItem = listEntries.Items.GetItemAt(listEntries.Items.Count - 1);
                listEntries.ScrollIntoView(listEntries.SelectedItem);
                ListViewItem item = listEntries.ItemContainerGenerator.ContainerFromItem(listEntries.SelectedItem) as ListViewItem;
                item.Focus();
            }
            catch (Exception ex)
            {
                App.Print($"ScrollMethod1(ERROR): {ex.Message}");
                App.Print($"_index: {index}");
                App.Print($"listEntries.Items.Count: {listEntries.Items.Count}");

            }
        }

        private void ScrollToLastItem()
        {
            try
            {
                listEntries.SelectedItem = listEntries.Items.GetItemAt(listEntries.Items.Count - 1);
                listEntries.ScrollIntoView(listEntries.SelectedItem);
                ListViewItem item = listEntries.ItemContainerGenerator.ContainerFromItem(listEntries.SelectedItem) as ListViewItem;
                item.Focus();
            }
            catch (Exception ex)
            {
                App.Print($"ScrollToLastItem(ERROR): {ex.Message}");
            }

        }

        private void ScrollMethod3(DependencyObject dObj)
        {
            if (VisualTreeHelper.GetChildrenCount(dObj) > 0)
            {
                Decorator border = VisualTreeHelper.GetChild(dObj, 0) as Decorator;
                ScrollViewer sv = border.Child as ScrollViewer;
                sv.ScrollToBottom();
            }
        }
       
    }
}
