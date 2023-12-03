using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UpdateViewer.Command;
using UpdateViewer.DataModels;

namespace UpdateViewer.ViewModels
{
    public class EditViewModel : ObservableObject
    {
        private bool _allowSave = true;
        public bool AllowSave
        {
            get { return _allowSave; }
            set
            {
                if (_allowSave != value)
                {
                    _allowSave = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _editTitle = "You are currently editing a stock.";
        public string EditTitle
        {
            get { return _editTitle; }
            set
            {
                if (_editTitle != value)
                {
                    _editTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        private static Stock _workStock = null;
        public static Stock WorkStock
        {
            get { return _workStock; }
            set
            {
                if (_workStock != value)
                {
                    _workStock = value;
                    //OnPropertyChanged();
                }
            }
        }

        public ICommand SaveEdit
        {
            get
            {
                //Con.WriteLine(">> Inside ToggleLog <<");
                return new CommandHandler<bool>((save) => {
                    if (save)
                    {
                        MainViewModel.stockChanged = true;
                    }
                    else
                    {
                        MainViewModel.stockChanged = false; 
                    }
                });
            }
        }

        public EditViewModel()
        {
            App.Print("In EditViewModel Constructor");
        }
        public EditViewModel(Stock stock)
        {
            App.Print($"Got single stock object");
            App.Print($"> {stock?.ID}");
            App.Print($"> {stock?.Name}");
            WorkStock = stock;
        }

        public EditViewModel(ObservableCollection<Stock> stocks)
        {
            App.Print($"Got observable stocks");
        }


    }
}
