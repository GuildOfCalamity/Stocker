/***************************************************************************
 * COPYRIGHT @ Chameware, LLC.
 * ALL RIGHTS RESERVED
 *
 * Developed by Chamware, LLC. (hotsauceboy@gmail.com)
 *
 * Copyright in the whole and every part of this software program belongs to
 * Chamware, LLC.  It may not be used, sold, licensed, transferred, copied 
 * or reproduced in whole or in part in any manner or form other than in 
 * accordance with and subject to the terms of a written license from 
 * Chamware or with the prior written consent of Lure or as permitted 
 * by applicable law.
 *
 * This software program contains confidential and proprietary information and
 * must not be disclosed, in whole or in part, to any person or organization
 * without the prior written consent of Lure.  If you are neither the
 * intended recipient, nor an agent, employee, nor independent contractor
 * responsible for delivering this message to the intended recipient, you are
 * prohibited from copying, disclosing, distributing, disseminating, and/or
 * using the information in this email in any manner. If you have received
 * this message in error, please advise us immediately at
 * hotsauceboy@gmail.com by return email and then delete the message from 
 * your computer and all other records (whether electronic, hard copy, or
 * otherwise).
 *
 * Any copies or reproductions of this software program (in whole or in part)
 * made by any method must also include a copy of this legend.
 * 
 ****************************************************************************/

/*** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***

    Purpose: This is a basic book keeping program that will help organize 
             your stock porfolio.
    Author.: Steve Chamlee (hotsauceboy@gmail.com)
    Date...: Sept 2021
    TODO...: Add web scrapping to update current stock values daily.

*** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** *** ***/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using UpdateViewer.Command;
using UpdateViewer.DataModels;
using UpdateViewer.Support;
using WPFCustomMessageBox;


namespace UpdateViewer.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        #region [Global Variables]
        /*
         *   NOTE: We can use this "instance" singleton teqnique to setup signalling between the models.
         *         You must set "_MainInstance = this" in the local constructor.
         */
        public static MainViewModel _MainInstance = null;
        private Settings coreSettings = null;
        public static List<Stock> sortList = new List<Stock>();
        public static List<Stock> MainStocks = new List<Stock>();
        public static bool stockChanged = false;
        public static Stock editedStock;
        private const int vMain = 0;
        private const int vEdit = 1;
        private const int vReport = 2;
        private const int vConfig = 3;
        public bool externalScroll = false;
        public static string dataSource = Directory.GetCurrentDirectory() + $"\\Data";
        // Task Variables
        private static CancellationTokenSource _cts = null;
        private static TimeSpan _maxToken = TimeSpan.FromMinutes(5); // only let task run for 5 minutes max
        System.Windows.Threading.DispatcherTimer _timer = null;
        // Threaded Process Variables
        public static bool processRunning = false;
        public static Thread ProcessThread = null;
        public static EventWaitHandle ProcessStarted = new EventWaitHandle(false, EventResetMode.AutoReset);
        public static EventWaitHandle ProcessReady = new EventWaitHandle(false, EventResetMode.AutoReset);
        public static EventWaitHandle ProcessStopped = new EventWaitHandle(false, EventResetMode.AutoReset);
        public static Queue<string> MessageQueue = new Queue<string>();

        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Properties]

        // We'll use this to change the view state from the main model
        private int _switchViewTemplate = vMain;
        public int SwitchViewTemplate
        {
            get => _switchViewTemplate;
            set
            {
                if (_switchViewTemplate != value)
                {
                    _switchViewTemplate = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableObject _currentView; //we'll use this to load in our UserControls
        public ObservableObject CurrentView
        {
            get { return _currentView; }
            set
            {
                if (_currentView != value)
                {
                    _currentView = value;
                    OnPropertyChanged();
                }
            }
        }

        private Sorts _selectedSort = null;
        public Sorts SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (_selectedSort != value)
                {
                    _selectedSort = value;
                    OnPropertyChanged();
                    App.Print($"Sort selected to {SelectedSort.Name} <<");
                    // Update our core settings
                    if (coreSettings != null)
                        coreSettings.SortType = SelectedSort.Type;
                    SortStocks(Stocks, SelectedSort.Type);
                }
            }
        }

        private Stock _selectedStock = null;
        public Stock SelectedStock
        {
            get => _selectedStock;
            set
            {
                if (_selectedStock != value)
                {
                    _selectedStock = value;
                    OnPropertyChanged();
                    if (_selectedStock != null)
                    {
                        if (YearMethods.Count > 0)
                        {
                            if (SelectedYear == YearMethods[0])
                            {
                                EnableEdit = false;
                                EnableDelete = false;
                                EnableAdd = false;
                                EnableLookup = true;
                            }
                            else
                            {
                                EnableEdit = true;
                                EnableDelete = true;
                                EnableAdd = true;
                                EnableLookup = true;
                            }
                        }

                        // Setup fields in case user changes to edit view
                        EditTitle = $"{_selectedStock.Name}";
                        try
                        {
                            double formatPL = (double)_selectedStock.Profit;
                            EditBrush = CalculateColor(_selectedStock.Profit);
                            EditProfitLoss = "$" + formatPL.ToString("#,###,0.00");
                            double formatBuy = (double)_selectedStock?.BuyQty * (double)_selectedStock?.BuyPrice;
                            EditBuyValue = "$" + formatBuy.ToString("#,###,0.00");
                            double formatSell = (double)_selectedStock?.SellQty * (double)_selectedStock?.SellPrice;
                            EditSellValue = "$" + formatSell.ToString("#,###,0.00");
                        }
                        catch (Exception ex)
                        {
                            Log.Write(eModule.MainModel, $"_selectedStock formatting error: {ex.Message}");
                        }
                    }
                    else
                    {
                        EnableEdit = false;
                        EnableDelete = false;
                        EnableLookup = false;
                        EditTitle = $"You are editing a stock."; //default msg

                        if (SelectedYear == YearMethods[0])
                            EnableAdd = false;
                        else
                            EnableAdd = true;
                    }
                }
            }
        }

        private int _selectedIdx = -1;
        public int SelectedIdx
        {
            get => _selectedIdx;
            set
            {
                if (_selectedIdx != value)
                {
                    _selectedIdx = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<Sorts> _sortMethod = new ObservableCollection<Sorts>();
        public ObservableCollection<Sorts> SortMethods
        {
            get => _sortMethod;
            set
            {
                _sortMethod = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<string> _yearMethod = new ObservableCollection<string>();
        public ObservableCollection<string> YearMethods
        {
            get => _yearMethod;
            set
            {
                _yearMethod = value;
                OnPropertyChanged();
            }
        }

        private string _selectedYear = "All Years";
        public string SelectedYear
        {
            get => _selectedYear;
            set
            {
                if (_selectedYear != value)
                {
                    _selectedYear = value;
                    OnPropertyChanged();

                    App.Print($"Year selected to {_selectedYear} <<");
                    // Update our core settings
                    if (coreSettings != null)
                        coreSettings.StockYear = _selectedYear;

                    int numYear = 0;
                    if (Int32.TryParse(_selectedYear, out numYear))
                    {
                        Stocks = LoadStocks(numYear, false);
                        EnableAdd = true;
                    }
                    else
                    {
                        Stocks = LoadStocks(DateTime.Now.Year, true);
                        EnableAdd = false;
                    }

                    CalculateTotalValue();

                    if (SelectedSort != null)
                        SortStocks(Stocks, SelectedSort.Type);
                    else
                        SortStocks(Stocks, eSort.Name);
                }
            }
        }

        private ObservableCollection<Stock> _stocks = new ObservableCollection<Stock>();
        public ObservableCollection<Stock> Stocks //this is our main observable
        {
            get => _stocks;
            set
            {
                _stocks = value;
                OnPropertyChanged();
            }
        }

        public string DataLocation
        {
            get => dataSource;
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

        private string _editBrush = "#999999";
        public string EditBrush
        {
            get { return _editBrush; }
            set
            {
                if (_editBrush != value)
                {
                    _editBrush = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _editProfitLoss = "0";
        public string EditProfitLoss
        {
            get { return _editProfitLoss; }
            set
            {
                if (_editProfitLoss != value)
                {
                    _editProfitLoss = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _editBuyValue = "0";
        public string EditBuyValue
        {
            get { return _editBuyValue; }
            set
            {
                if (_editBuyValue != value)
                {
                    _editBuyValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _editSellValue = "0";
        public string EditSellValue
        {
            get { return _editSellValue; }
            set
            {
                if (_editSellValue != value)
                {
                    _editSellValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _reportTitle = "You are currently viewing a stock report";
        public string ReportTitle
        {
            get { return _reportTitle; }
            set
            {
                if (_reportTitle != value)
                {
                    _reportTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _printerData = "This is sample data for the printer";
        public string PrinterData
        {
            get { return _printerData; }
            set
            {
                if (_printerData != value)
                {
                    _printerData = value;
                    OnPropertyChanged();
                }
            }
        }

        private static eTaskResult _taskStatus = eTaskResult.None;
        public static eTaskResult CurrentTaskStatus
        {
            get => _taskStatus;
            set
            {
                _taskStatus = value;
            }
        }

        private bool _saveNeeded = false;
        public bool SaveNeeded
        {
            get { return _saveNeeded; }
            set
            {
                if (_saveNeeded != value)
                {
                    _saveNeeded = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _editOK = true;
        public bool EditOK
        {
            get { return _editOK; }
            set
            {
                if (_editOK != value)
                {
                    _editOK = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _configOK = true;
        public bool ConfigOK
        {
            get { return _configOK; }
            set
            {
                if (_configOK != value)
                {
                    _configOK = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _allowClose = true;
        public bool AllowClose
        {
            get { return _allowClose; }
            set
            {
                if (_allowClose != value)
                {
                    _allowClose = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _allowHide = true;
        public bool AllowHide
        {
            get { return _allowHide; }
            set
            {
                if (_allowHide != value)
                {
                    _allowHide = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _updateTitle = "Below is the list of your stocks";
        public string UpdateTitle
        {
            get { return _updateTitle; }
            set 
            {
                if (_updateTitle != value)
                {
                    _updateTitle = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableLookup = false;
        public bool EnableLookup
        {
            get => _enableLookup;
            set
            {
                if (_enableLookup != value)
                {
                    _enableLookup = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableEdit = false;
        public bool EnableEdit
        {
            get => _enableEdit;
            set
            {
                if (_enableEdit != value)
                {
                    _enableEdit = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableAdd = true;
        public bool EnableAdd
        {
            get => _enableAdd;
            set
            {
                if (_enableAdd != value)
                {
                    _enableAdd = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableDelete = false;
        public bool EnableDelete
        {
            get => _enableDelete;
            set
            {
                if (_enableDelete != value)
                {
                    _enableDelete = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _cbAuto = true;
        public bool cbAuto
        {
            get => _cbAuto;
            set
            {
                if (_cbAuto != value)
                {
                    _cbAuto = value;
                    // Update our core settings
                    if (coreSettings != null)
                        coreSettings.AutomaticCheck = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _cbRemind = true;
        public bool cbRemind
        {
            get => _cbRemind;
            set
            {
                if (_cbRemind != value)
                {
                    _cbRemind = value;
                    // Update our core settings
                    if (coreSettings != null)
                        coreSettings.ReminderCheck = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _cbTips = true;
        public bool cbTips
        {
            get => _cbTips;
            set
            {
                if (_cbTips != value)
                {
                    _cbTips = value;
                    // Update our core settings
                    if (coreSettings != null)
                        coreSettings.EnableTips = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _enableCheck = true;
        public bool EnableCheck
        {
            get => _enableCheck;
            set
            {
                if (_enableCheck != value)
                {
                    _enableCheck = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showPopup = false;
        public bool ShowPopup
        {
            get => _showPopup;
            set
            {
                if (_showPopup != value)
                {
                    _showPopup = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _maxPopupWidth = 610;
        public int MaxPopupWidth
        {
            get => _maxPopupWidth;
            set
            {
                if (_maxPopupWidth != value)
                {
                    _maxPopupWidth = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _maxPopupHeight = 100;
        public int MaxPopupHeight
        {
            get => _maxPopupHeight;
            set
            {
                if (_maxPopupHeight != value)
                {
                    _maxPopupHeight = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _backupDays = 2;
        public int BackupDays
        {
            get => _backupDays;
            set
            {
                if (_backupDays != value)
                {
                    _backupDays = value;
                    // Update our core settings
                    if (coreSettings != null)
                        coreSettings.BackupDays = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _totalBuyValue = "0";
        public string TotalBuyValue
        {
            get => _totalBuyValue;
            set
            {
                if (_totalBuyValue != value)
                {
                    _totalBuyValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _totalBuyQty = "0";
        public string TotalBuyQty
        {
            get => _totalBuyQty;
            set
            {
                if (_totalBuyQty != value)
                {
                    _totalBuyQty = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _totalSellValue = "0";
        public string TotalSellValue
        {
            get => _totalSellValue;
            set
            {
                if (_totalSellValue != value)
                {
                    _totalSellValue = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _totalSellQty = "0";
        public string TotalSellQty
        {
            get => _totalSellQty;
            set
            {
                if (_totalSellQty != value)
                {
                    _totalSellQty = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _totalProfit = "0";
        public string TotalProfit
        {
            get => _totalProfit;
            set
            {
                if (_totalProfit != value)
                {
                    _totalProfit = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _popupMessage;
        public string PopupMessage
        {
            get => _popupMessage;
            set
            {
                _popupMessage = value;
                OnPropertyChanged(); 
            }
        }

        private string _checkContext = "Check for updates";
        public string CheckContext
        {
            get { return _checkContext; }
            set
            {
                if (_checkContext != value)
                {
                    _checkContext = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _editContext = "Edit Stock";
        public string EditContext
        {
            get { return _editContext; }
            set
            {
                if (_editContext != value)
                {
                    _editContext = value;
                    OnPropertyChanged();
                }
            }
        }

        private object currentViewNew;

        public object CurrentViewNew
        {
            get { return currentViewNew; }
            set
            {
                currentViewNew = value;
                OnPropertyChanged();
            }
        }

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


        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Commands]
        public ICommand CloseApp
        {
            get
            {
                return new CommandHandler<bool>((closing) => 
                {
                    if (closing)
                    {
                        Log.Write(eModule.MainModel, $"Closing application...", true);
                        if (Stocks != null && SaveNeeded)
                        {
                            int numYear = 0;
                            if (Int32.TryParse(SelectedYear, out numYear))
                            {
                                if (SaveStocks(Stocks, numYear))
                                    Log.Write(eModule.MainModel, $"Save was successful", true);
                                else
                                    Log.Write(eModule.MainModel, $"Save failed!", true);
                            }
                            else
                            {
                                DisplayPopupMessage($"You must select a year to edit/save stocks.");
                                return;
                            }
                        }
                        UpdateTitle = "Closing application...";
                        Thread.Sleep(250);
                        App.Current.Shutdown();
                    }
                });
            }
        }

        public ICommand HideApp
        {
            get
            {
                return new CommandHandler<bool>((hide) => 
                {
                    if (hide)
                    {
                        Application.Current.MainWindow.WindowState = WindowState.Minimized;
                    }
                });
            }
        }

        public ICommand SwitchView
        {
            get
            {
                //Con.WriteLine(">> Inside SwitchView <<");
                return new CommandHandler<Type>((type) => {
                    //var _isAuth = CurrentView.IsAuthorizedUser;
                    CurrentView = Activator.CreateInstance(type) as ObservableObject;
                    //CurrentView.IsAuthorizedUser = _isAuth;
                    DisplayPopupMessage($"Switching to {type}");
                });
            }
        }

        public ICommand SwitchViewEdit
        {
            get
            {
                return new CommandHandler<Type>((type) => 
                {
                    //CurrentView = new EditViewModel(SelectedStock);
                    SwitchViewTemplate = vEdit;
                });
            }
        }

        public ICommand SwitchViewConfig
        {
            get
            {
                return new CommandHandler<Type>((type) =>
                {
                    //CurrentView = new EditViewModel(SelectedStock);
                    SwitchViewTemplate = vConfig;
                });
            }
        }

        public ICommand SwitchViewAdd
        {
            get
            {
                return new CommandHandler<Type>((type) => 
                {
                    //CurrentView = new EditViewModel(SelectedStock);
                    try
                    {
                        if (Stocks != null)
                        {
                            Stock newStock = new Stock() { ID = Stocks.Count + 1, Code = "ABC", Name = "Name", BuyQty = 0, BuyPrice = 0.0, BuyDate = null, SellQty = 0, SellPrice = 0.0, SellDate = null, Profit = 0.0, DivAmt = 0, DivPct = 0, Resv1 = 0, Resv2 = 0, Reminder = null, Notes = $"{SelectedYear} database", StockBrush = "#FFEEEEEE" };
                            SelectedStock = newStock;
                            Stocks.Add(newStock);
                            SwitchViewTemplate = vEdit;
                            SelectedIdx = Stocks.Count - 1;
                            externalScroll = true;
                        }
                        else
                        {
                            Stocks = new ObservableCollection<Stock>();
                            Stock newStock = new Stock() { ID = Stocks.Count + 1, Code = "ABC", Name = "Name", BuyQty = 0, BuyPrice = 0.0, BuyDate = null, SellQty = 0, SellPrice = 0.0, SellDate = null, Profit = 0.0, DivAmt = 0, DivPct = 0, Resv1 = 0, Resv2 = 0, Reminder = null, Notes = $"{SelectedYear} database", StockBrush = "#FFEEEEEE" };
                            SelectedStock = newStock;
                            Stocks.Add(newStock);
                            SwitchViewTemplate = vEdit;
                            SelectedIdx = Stocks.Count - 1;
                            externalScroll = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        DisplayPopupMessage("Error occurred while trying to add a new stock.");
                        Log.Write(eModule.MainModel, $"Error adding new stock: {ex.Message}");
                    }
                });
            }
        }
        public ICommand SwitchViewReport
        {
            get
            {
                return new CommandHandler<Type>((type) => {
                    //CurrentView = new EditViewModel(SelectedStock);
                    if (FormatPrinterData())
                    {
                        CalculateTotalValue();
                        //ReportTitle = $"Report generated on {DateTime.Now.ToLongDateString()}";
                        ReportTitle = $"Report showing stocks from {SelectedYear}";
                        SwitchViewTemplate = vReport;
                    }
                });
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
                        UpdateStock(SelectedStock);
                        SwitchViewTemplate = vMain;
                    }
                });
            }
        }

        public ICommand DeleteStock
        {
            get
            {
                //Con.WriteLine(">> Inside ToggleLog <<");
                return new CommandHandler<bool>((delete) => {
                    if (delete)
                    {
                        if (SelectedStock != null)
                        {
                            Stocks.RemoveAt((int)SelectedStock?.ID);
                        }
                    }
                });
            }
        }

        public ICommand GoBackEdit
        {
            get
            {
                return new CommandHandler<bool>((edit) => 
                {
                    if (edit)
                    {
                        if (ValidateStock(SelectedStock))
                            SwitchViewTemplate = vMain;
                        else
                            DisplayPopupMessage("Please fix the data fields.");
                    }
                });
            }
        }

        public ICommand GoBackConfig
        {
            get
            {
                return new CommandHandler<bool>((config) =>
                {
                    if (config)
                    {
                         SwitchViewTemplate = vMain;
                    }
                });
            }
        }

        public ICommand GoBackReport
        {
            get
            {
                return new CommandHandler<bool>((save) =>
                {
                    if (save)
                    {
                        SwitchViewTemplate = vMain;
                    }
                });
            }
        }


        public ICommand PrintReport
        {
            get
            {
                //Con.WriteLine(">> Inside ToggleLog <<");
                return new CommandHandler<bool>((save) => {
                    if (save)
                    {
                        if (!string.IsNullOrEmpty(PrinterData))
                        {
                            try
                            {
                                PrintDialog printDialog = new PrintDialog();
                                printDialog.SelectedPagesEnabled = true;
                                printDialog.UserPageRangeEnabled = true;
                                if (printDialog?.ShowDialog() == true)
                                {
                                    FlowDocument flowDocument = new FlowDocument();
                                    flowDocument.PagePadding = new Thickness(30);
                                    // 8.5" x 11" = 563 pixels x 750 pixels
                                    flowDocument.PageWidth = 660;
                                    flowDocument.ColumnWidth = 700;
                                    flowDocument.TextAlignment = TextAlignment.Justify;
                                    flowDocument.IsOptimalParagraphEnabled = true;
                                    flowDocument.IsColumnWidthFlexible = true;
                                    flowDocument.MaxPageWidth = 700;
                                    flowDocument.PageHeight = 950;
                                    flowDocument.FontFamily = new FontFamily("Consolas");
                                    flowDocument.FontSize = 14;
                                    flowDocument.Blocks.Add(new Paragraph(new Run(PrinterData)));

                                    // Print the textbox stock data
                                    printDialog.PrintDocument((((IDocumentPaginatorSource)flowDocument).DocumentPaginator), "Stock Report");
                                    DisplayPopupMessage($"Job sent to printer.");
                                    Log.Write(eModule.MainModel, $"Job sent to printer ({printDialog.PrintableAreaWidth}x{printDialog.PrintableAreaHeight})");
                                }
                                else
                                {
                                    DisplayPopupMessage($"Print job was canceled.");
                                }
                            }
                            catch (Exception ex)
                            {
                                DisplayPopupMessage("Error occurred during print!");
                                Log.Write(eModule.MainModel, $"PrintFailed: {ex.Message}");
                            }
                        }
                        else
                        {
                            DisplayPopupMessage("There is currently no printer data.");
                        }
                    }
                });
            }
        }
        public ICommand CheckCommand { get; set; }
        public ICommand cbCommand { get; set; }
        
        public ICommand DeleteCommand { get; set; }
        public ICommand LookupCommand { get; set; }

        public RelayCommand OldViewSwitchCommand { get; set; }

        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Main Constructor/Entrypoint]
        /// <summary>
        /// Default constructor
        /// </summary>
        public MainViewModel()
        {
            //Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose; //we're setting this in App.xaml
            /*
             *   NOTE: We can use this "instance" singleton teqnique to setup signalling between the models
             */
            if (_MainInstance == null)
                _MainInstance = this;
            //else
            //    return; // If other windows try to call our contructor then leave

            //Default to our main view
            SwitchViewTemplate = vMain;

            Log.Write(eModule.MainModel, $"{App.SystemTitle} v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}", true);

            // Setup out command tasks
            //CheckCommand = new AsyncCommand(CheckUpdatesMethod);
            DeleteCommand = new AsyncCommand(DeleteStockMethod);
            LookupCommand = new AsyncCommand(LookupStockMethod);

            // Setup our current view
            //CurrentView = this;

            //Create the timer for our process start
            if (_timer == null)
            {
                _timer = new System.Windows.Threading.DispatcherTimer();
                _timer.Interval = TimeSpan.FromSeconds(2);
                _timer.Tick += timer_Tick;
                _timer.Start();
            }

            // Test our checkbox command
            cbCommand = new AsyncCommand(CheckboxMethod);

            // Setup core system settings
            ConfigureSystemSettings();

            // Load in our stock data
            //LoadUpdatedStocksTask(true);

            /*
            Stocks = LoadStocks();
            if (Stocks.Count == 0)
            {
                DisplayPopupMessage("Could not load stock data, generating new file...");
                BuildStockList();
            }

                Stock newStock = new Stock() { ID = Stocks.Count + 1, Code = "ABC", Name = "Name", BuyQty = 0, BuyPrice = 0.0, BuyDate = null, SellQty = 0, SellPrice = 0.0, SellDate = null, Profit = 0.0, DivAmt = 0.0, DivPct = 0.0, Resv1 = 0.0, Resv2 = 0.0, Reminder = null, Notes = $"2021 database", StockBrush = "#FFEEEEEE" };
                Stocks.Add(newStock);
                SaveStocks(Stocks, 2021);
            */

        }
        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Event Methods]

        /// <summary>
        /// To lauch edit view from event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnListMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnableEdit)
                SwitchViewTemplate = vEdit;
        }

        /// <summary>
        /// UpdateView Event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnUpdateViewLoaded(object sender, RoutedEventArgs e)
        {
            //App.Print("MainViewModel: UpdateView Loaded");
            //LoadStockData();
            if (!App.SystemLoaded)
            {
                LoadUpdatedStocksTask(true);
                App.SystemLoaded = true;
            }
            else if (YearMethods.Count > 0)
            {
                if (SelectedYear == YearMethods[0])
                {
                    EnableEdit = false;
                    EnableDelete = false;
                }
            }
        }

        /// <summary>
        /// EditView event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnEditControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (stockChanged)
            {
                editedStock = EditViewModel.WorkStock;

                App.Print($"Edited Stock {editedStock?.Code} at {DateTime.Now.ToLongTimeString()}");
                
                //UpdateStocks(editedStock); //not needed anymore since we are working directly with the observable collection
            }
            else
            {
                App.Print($"Stock was unchanged");
            }


            if (SelectedIdx > -1)
                RecalcStock(SelectedStock, SelectedIdx);

            int numYear = 0;
            if (Int32.TryParse(SelectedYear, out numYear))
            {
                // Save any changes that may have happened
                //SaveUpdatedStocksTask(numYear);

                SaveStocksThread(Stocks, numYear);
            }
            else
            {
                DisplayPopupMessage($"You must select a year to edit/save stocks.");
            }
        }

        /// <summary>
        /// Test event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Edit_ButtonClick(object sender, RoutedEventArgs e)
        {
            /*
            Button b = sender as Button;
            ProductCategory productCategory = b.CommandParameter as ProductCategory;
            MessageBox.Show(productCategory.Id);
            */
        }

        /// <summary>
        /// Event hook from MainWindow.xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnWindowLoaded(object sender, System.Windows.RoutedEventArgs e)
        {
            App.Print($"MainWindow Loaded");
        }

        /// <summary>
        /// Event hook from MainWindow.xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (coreSettings != null)
            {
                UpdateTitle = $"Saving system settings...";
                SaveSettings(coreSettings, Directory.GetCurrentDirectory() + "\\Settings.cfg");
            }

        }

        /// <summary>
        /// Timer event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void timer_Tick(object sender, EventArgs e)
        {
            _timer.Stop();

            if (coreSettings != null)
            {
                // Automatically check stocks
                if (coreSettings.AutomaticCheck)
                    CheckCommand.Execute(true);
            }
        }

        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Saving & Loading Methods]

        /// <summary>
        /// Save our stock data to disk
        /// </summary>
        /// <param name="sysSettings"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool SaveStocks(ObservableCollection<Stock> stocks)
        {
            try
            {
                DisplayPopupMessage("Saving stocks...");

                if (stocks != null && stocks.Count > 0)
                {
                    if (!SelectedYear.Equals(YearMethods[0], StringComparison.CurrentCultureIgnoreCase))
                    {
                        DisplayPopupMessage($"Change view to {YearMethods[0]} and try again.");
                        return false;
                    }
                    else
                    {
                        File.WriteAllBytes(Directory.GetCurrentDirectory() + "\\Stocks.dat", Encoding.UTF8.GetBytes(stocks.ToJson()));
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error saving stocks!");
                Log.Write(eModule.MainModel, $"SaveStocks(ERROR): {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// Save our stock data to disk
        /// </summary>
        /// <param name="sysSettings"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool SaveStocks(ObservableCollection<Stock> stocks, int year)
        {
            if (string.IsNullOrEmpty(dataSource))
                dataSource = Directory.GetCurrentDirectory() + $"\\Data";

            try
            {
                if (stocks != null && stocks.Count > 0)
                {
                    if (SelectedYear == YearMethods[0])
                    {
                        DisplayPopupMessage($"You must select a year to edit/save stocks.");
                        return false;
                    }
                    else
                    {
                        DisplayPopupMessage($"Saving {year} stocks...");
                        lock (stocks)
                        {
                            File.WriteAllBytes(dataSource + $"\\{year}.dat", Encoding.UTF8.GetBytes(stocks.ToJson()));
                        }
                        Log.Write(eModule.MainModel, $"Stock data was saved {dataSource}\\{year}.dat", true);
                        return true;
                    }
                }
                else
                {
                    DisplayPopupMessage("No stock data to save.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error saving stocks!");
                Log.Write(eModule.MainModel, $"SaveStocks(ERROR): {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// Save our stock data to disk
        /// </summary>
        /// <param name="sysSettings"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public void SaveStocksThread(ObservableCollection<Stock> stocks, int year)
        {
            //string dataSource = Directory.GetCurrentDirectory() + $"\\Data";

            ThreadPool.QueueUserWorkItem(o =>
            {
                try
                {
                    if (stocks != null && stocks.Count > 0)
                    {
                        if (SelectedYear == YearMethods[0])
                        {
                            DisplayPopupMessage($"You must select a year to edit/save stocks.");
                        }
                        else
                        {
                            DisplayPopupMessage($"Saving {year} stocks...");
                            lock (stocks)
                            {
                                File.WriteAllBytes(dataSource + $"\\{year}.dat", Encoding.UTF8.GetBytes(stocks.ToJson()));
                            }
                            Log.Write(eModule.MainModel, $"Stock data was saved {dataSource}\\{year}.dat", true);
                        }
                    }
                    else
                    {
                        DisplayPopupMessage("No stock data to save.");
                    }
                }
                catch (Exception ex)
                {
                    DisplayPopupMessage("Error saving stocks!");
                    Log.Write(eModule.MainModel, $"SaveStocks(ERROR): {ex.Message}", true);
                }
            });
        }


        /// <summary>
        /// Load our stock data from disk
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Stock> LoadStocks()
        {
            ObservableCollection<Stock> tmp = null;
            //string dataSource = Directory.GetCurrentDirectory();
            try
            {
                DisplayPopupMessage("Loading stocks...");

                if (File.Exists(dataSource + "\\Stocks.dat"))
                {
                    // Make a backup
                    if (File.Exists(dataSource + "\\Stocks.bak"))
                    {
                        FileInfo fi = new FileInfo(dataSource + "\\Stocks.bak");
                        DateTime lastBackup = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).Subtract(new TimeSpan(BackupDays, 0, 0, 0));
                        if (fi.LastWriteTime < lastBackup) //only backup if last backup is older than 1 day
                            File.Copy(dataSource + "\\Stocks.dat", dataSource + "\\Stocks.bak", true);
                    }
                    else
                    {
                        File.Copy(dataSource + "\\Stocks.dat", dataSource + "\\Stocks.bak", true);
                    }

                    // Load the working data
                    string imported = Encoding.UTF8.GetString(File.ReadAllBytes(dataSource + "\\Stocks.dat"));
                    App.Print($"imported: {dataSource}\\Stocks.dat");
                    tmp = imported.FromJsonTo<ObservableCollection<Stock>>();
                    return tmp;
                }
                else
                {
                    App.Print($"WARNING: Stock data does not exist!");
                    return tmp;
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage($"Error loading stocks!");
                Log.Write(eModule.MainModel, $"LoadStocks(ERROR): {ex.Message}");
                return tmp;
            }
        }

        /// <summary>
        /// Load our stock data from disk
        /// </summary>
        /// <returns></returns>
        public ObservableCollection<Stock> LoadStocks(int year, bool allStocks = false)
        {
            ObservableCollection<Stock> tmp = null;
            //string dataSource = Directory.GetCurrentDirectory() + $"\\Data";
            try
            {

                if (allStocks)
                {
                    DisplayPopupMessage($"Loading all stocks...");
                    ObservableCollection<Stock> allData = new ObservableCollection<Stock>();
                    foreach (string fi in Directory.GetFiles(dataSource))
                    {
                        if (Path.GetExtension(fi.ToLower()).Equals(".dat", StringComparison.OrdinalIgnoreCase))
                        {
                            string imported = Encoding.UTF8.GetString(File.ReadAllBytes(fi));
                            App.Print($"imported: {Path.GetFileName(fi)}");
                            tmp = imported.FromJsonTo<ObservableCollection<Stock>>();
                            foreach (var item in tmp)
                            {
                                allData.Add(item);
                            }
                        }
                    }

                    if (allData.Count == 0)
                        DisplayPopupMessage($"No stock data was found in {dataSource}");

                    return allData;
                }
                else
                {
                    DisplayPopupMessage($"Loading {year} stocks...");

                    if (File.Exists(dataSource + $"\\{year}.dat"))
                    {
                        // Make a backup
                        if (File.Exists(dataSource + $"\\{year}.bak"))
                        {
                            FileInfo fi = new FileInfo(dataSource + $"\\{year}.bak");
                            DateTime lastBackup = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).Subtract(new TimeSpan(BackupDays, 0, 0, 0));
                            if (fi.LastWriteTime < lastBackup) //only backup if last backup is older than 1 day
                                File.Copy(dataSource + $"\\{year}.dat", dataSource + $"\\{year}.bak", true);
                        }
                        else
                        {
                            File.Copy(dataSource + $"\\{year}.dat", dataSource + $"\\{year}.bak", true);
                        }

                        // Load the working data
                        string imported = Encoding.UTF8.GetString(File.ReadAllBytes(dataSource + $"\\{year}.dat"));
                        App.Print($"imported: {year}.dat");
                        tmp = imported.FromJsonTo<ObservableCollection<Stock>>();
                        return tmp;
                    }
                    else
                    {
                        Log.Write(eModule.MainModel, $"WARNING: Stock data does not exist! ({dataSource}\\{year})");
                        DisplayPopupMessage($"No stock data exists for year {year}");
                        // Create a new database collection
                        tmp = new ObservableCollection<Stock>();
                        EnableAdd = true;
                        return tmp;
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage($"Error loading stocks!");
                Log.Write(eModule.MainModel, $"LoadStocks(ERROR): {ex.Message}");
                return tmp;
            }
        }

        /// <summary>
        /// Task-based load method
        /// </summary>
        /// <param name="allStocks"></param>
        /// <returns></returns>
        private async Task LoadUpdatedStocksTask(bool allStocks = true)
        {
            _ = Task.Run(() =>
            {
                if (_cts == null) // We want to start the task
                {
                    //DisplayPopupMessage("Loading stocks...");
                    CurrentTaskStatus = eTaskResult.Initialize;

                    _cts = new CancellationTokenSource(_maxToken);
                    CancellationToken tok = _cts.Token;
                    List<Task> _taskQueue = new List<Task>();
                    // Setup the task queue
                    _taskQueue.Add(Task.Run(() =>
                    {
                        int numYear = 0;
                        if (Int32.TryParse(SelectedYear, out numYear))
                            Stocks = LoadStocks(numYear, false);
                        else
                            Stocks = LoadStocks(DateTime.Now.Year, allStocks); //on first load pull in all stocks

                        CalculateTotalValue();

                        if (coreSettings != null)
                            SortStocks(Stocks, coreSettings.SortType);
                        else
                            SortStocks(Stocks, eSort.Name);

                    }, tok));
                    CurrentTaskStatus = eTaskResult.Created;

                    // Create the tasks queue
                    Task allWork = Task.WhenAll(_taskQueue); //You can use WhenAny() to signal if any of the tasks have completed
                    allWork.ConfigureAwait(false);
                    try
                    {
                        if (!allWork.IsCompleted)
                        {
                            CurrentTaskStatus = eTaskResult.Running;

                            // Wait for the tasks to complete
                            allWork.Wait(tok);

                            App.Print($"allWork: {allWork.Status}");
                            if (allWork.Status == TaskStatus.RanToCompletion)
                                CurrentTaskStatus = eTaskResult.Success;
                            else if (allWork.Status == TaskStatus.Faulted)
                                CurrentTaskStatus = eTaskResult.Failure;
                            else if (allWork.Status == TaskStatus.WaitingForActivation)
                                CurrentTaskStatus = eTaskResult.Warning;
                        }
                        else
                        {
                            CurrentTaskStatus = eTaskResult.Invalid;
                        }

                    }
                    catch (OperationCanceledException ocex)
                    {
                        App.Print($"OperationCanceledException: {ocex.Message}");
                        CurrentTaskStatus = eTaskResult.Canceled;
                    }
                    catch (ObjectDisposedException odex)
                    {
                        App.Print($"ObjectDisposedException: {odex.Message}");
                        CurrentTaskStatus = eTaskResult.Failure;
                    }
                    catch (AggregateException aex)
                    {
                        App.Print($"AggregateException: {aex.Message}");
                        CurrentTaskStatus = eTaskResult.Failure;
                    }
                    finally
                    {
                        _cts = null;
                    }

                }
                else // We want to cancel the task
                {
                    CurrentTaskStatus = eTaskResult.Canceling;
                    _cts.Cancel();
                    _cts = null;
                }
            });
        }

        /// <summary>
        /// Task-based save method
        /// </summary>
        /// <param name="year"></param>
        /// <returns></returns>
        private async Task SaveUpdatedStocksTask(int year)
        {
            await Task.Run(() =>
            {
                if (_cts == null) // We want to start the task
                {
                    CurrentTaskStatus = eTaskResult.Initialize;

                    _cts = new CancellationTokenSource(_maxToken);
                    CancellationToken tok = _cts.Token;

                    List<Task> _taskQueue = new List<Task>();
                    // Setup the task queue
                    _taskQueue.Add(Task.Run(() =>
                    {
                        SaveStocks(Stocks, year);
                    }, tok));
                    CurrentTaskStatus = eTaskResult.Created;

                    // Create the tasks queue
                    Task allWork = Task.WhenAll(_taskQueue); //You can use WhenAny() to signal if any of the tasks have completed
                    allWork.ConfigureAwait(false);
                    try
                    {
                        if (!allWork.IsCompleted)
                        {
                            CurrentTaskStatus = eTaskResult.Running;

                            // Wait for the tasks to complete
                            allWork.Wait(tok);

                            App.Print($"allWork: {allWork.Status}");
                            if (allWork.Status == TaskStatus.RanToCompletion)
                                CurrentTaskStatus = eTaskResult.Success;
                            else if (allWork.Status == TaskStatus.Faulted)
                                CurrentTaskStatus = eTaskResult.Failure;
                            else if (allWork.Status == TaskStatus.WaitingForActivation)
                                CurrentTaskStatus = eTaskResult.Warning;
                        }
                        else
                        {
                            CurrentTaskStatus = eTaskResult.Invalid;
                        }

                    }
                    catch (OperationCanceledException ocex)
                    {
                        App.Print($"OperationCanceledException: {ocex.Message}");
                        CurrentTaskStatus = eTaskResult.Canceled;
                    }
                    catch (ObjectDisposedException odex)
                    {
                        App.Print($"ObjectDisposedException: {odex.Message}");
                        CurrentTaskStatus = eTaskResult.Failure;
                    }
                    catch (AggregateException aex)
                    {
                        App.Print($"AggregateException: {aex.Message}");
                        CurrentTaskStatus = eTaskResult.Failure;
                    }
                    finally
                    {
                        _cts = null;
                    }

                }
                else // We want to cancel the task
                {
                    CurrentTaskStatus = eTaskResult.Canceling;
                    _cts.Cancel();
                    _cts = null;
                }
            });
        }
        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Helper Methods]
        /// <summary>
        /// Configure basic system variables
        /// </summary>
        private void ConfigureSystemSettings()
        {
            int yearCount = DateTime.Now.Year;
            YearMethods.Clear();
            YearMethods.Add("All Years");
            for (int i = 0; i < 21; i++)
            {
                YearMethods.Add(yearCount.ToString());
                yearCount--;
            }
            SelectedYear = YearMethods[0];

            SortMethods.Clear();
            SortMethods.Add(new Sorts() { Type = eSort.Name, Name = "Sort by name" });
            SortMethods.Add(new Sorts() { Type = eSort.Code, Name = "Sort by code" });
            SortMethods.Add(new Sorts() { Type = eSort.BuyDate, Name = "Sort by buy date" });
            SortMethods.Add(new Sorts() { Type = eSort.BuyPrice, Name = "Sort by buy price" });
            SortMethods.Add(new Sorts() { Type = eSort.SellDate, Name = "Sort by sell date" });
            SortMethods.Add(new Sorts() { Type = eSort.SellPrice, Name = "Sort by sell price" });
            SortMethods.Add(new Sorts() { Type = eSort.Profit, Name = "Sort by profit" });
            SortMethods.Add(new Sorts() { Type = eSort.DivAmt, Name = "Sort by div amt" });
            SortMethods.Add(new Sorts() { Type = eSort.DivPct, Name = "Sort by div pct" });
            coreSettings = LoadSettings(Directory.GetCurrentDirectory() + "\\Settings.cfg");
            if (coreSettings != null)
            {
                foreach (var setting in coreSettings.ListSettings())
                {
                    App.Print($"Setting: {setting}");
                }

                cbAuto = coreSettings.AutomaticCheck;
                cbRemind = coreSettings.ReminderCheck;
                cbTips = coreSettings.EnableTips;

                if (coreSettings.SortType == eSort.Name)
                    SelectedSort = SortMethods[0];
                else if (coreSettings.SortType == eSort.Code)
                    SelectedSort = SortMethods[1];
                else if (coreSettings.SortType == eSort.BuyDate)
                    SelectedSort = SortMethods[2];
                else if (coreSettings.SortType == eSort.BuyPrice)
                    SelectedSort = SortMethods[3];
                else if (coreSettings.SortType == eSort.SellDate)
                    SelectedSort = SortMethods[4];
                else if (coreSettings.SortType == eSort.SellPrice)
                    SelectedSort = SortMethods[5];
                else if (coreSettings.SortType == eSort.Profit)
                    SelectedSort = SortMethods[6];
                else if (coreSettings.SortType == eSort.DivAmt)
                    SelectedSort = SortMethods[7];
                else if (coreSettings.SortType == eSort.DivPct)
                    SelectedSort = SortMethods[8];
                else
                    SelectedSort = SortMethods[0];

                SelectedYear = coreSettings.StockYear;

                BackupDays = coreSettings.BackupDays;
            }
            else //default if no settings
            {
                SelectedSort = SortMethods[0];
            }
        }

        /// <summary>
        /// Testing method
        /// </summary>
        /// <param name="workStock"></param>
        /// <returns></returns>
        private bool UpdateStock(Stock workStock)
        {
            if (Stocks != null && Stocks.Count > 0 && (bool)workStock?.ID.HasValue)
            {
                int idx = (int)workStock?.ID;
                Stocks[idx] = workStock;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Testing method
        /// </summary>
        /// <param name="workStock"></param>
        /// <returns></returns>
        private bool RecalcStock(Stock workStock, int index)
        {
            try
            {
                if ((bool)workStock?.ID.HasValue)
                {
                    double profit = CalculateProfit(workStock);
                    string brush = CalculateColor(profit);
                    Stock tmpStock = new Stock() { ID = workStock.ID, Code = workStock.Code, Name = workStock.Name, BuyQty = workStock.BuyQty, BuyPrice = workStock.BuyPrice, BuyDate = workStock.BuyDate, SellQty = workStock.SellQty, SellPrice = workStock.SellPrice, SellDate = workStock.SellDate, Profit = profit, DivAmt = workStock.DivAmt, DivPct = workStock.DivPct, Resv1 = workStock.Resv1, Resv2 = workStock.Resv2, Reminder = workStock.Reminder, Notes = workStock.Notes, StockBrush = brush };
                    Stocks.RemoveAt(index);
                    Stocks.Insert(index, tmpStock);
                    SelectedStock = tmpStock;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error recalculating stock!");
                Log.Write(eModule.MainModel, $"RecalcStock(ERROR): {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ValidateStock(Stock item)
        {
            bool retVal = false;
            double test;
            try
            {
                // We should add some logic check to see if SellDate has value if SellQty is > 0
                // Also, check BuyDate has value if BuyQty is > 0
                retVal = (bool)item?.BuyPrice.HasValue && Double.TryParse(item?.BuyPrice.ToString(), out test) &&
                         (bool)item?.BuyQty.HasValue && Double.TryParse(item?.BuyQty.ToString(), out test) &&
                         (bool)item?.SellPrice.HasValue && Double.TryParse(item?.SellPrice.ToString(), out test) &&
                         (bool)item?.SellQty.HasValue && Double.TryParse(item?.SellQty.ToString(), out test) &&
                         (bool)item?.Profit.HasValue && Double.TryParse(item?.Profit.ToString(), out test);

                if ((bool)item?.SellQty.HasValue && (bool)item?.BuyQty.HasValue)
                {
                    if (item?.SellQty > item?.BuyQty)
                        item.SellQty = item.BuyQty;
                }

                return retVal;
            }
            catch
            {
                return retVal;
            }
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <returns></returns>
        private bool FormatPrinterData()
        {
            int delimLen = 77;
            int totItems = 0;
            double totProfit = 0.0;
            double totSellQty = 0.0;
            double totSellVal = 0.0;
            double totBuyQty = 0.0;
            double totBuyVal = 0.0;
            double avgDivAmt = 0.0;
            double avgDivPct = 0.0;

            try
            {
                if (Stocks != null && Stocks.Count > 0)
                {
                    List<string> buffer = new List<string>();
                    foreach (var item in Stocks)
                    {
                        totItems++;
                        buffer.Add(FormatStockData(item));
                        totProfit += (double)item?.Profit;
                        totBuyQty += (double)item?.BuyQty;
                        totBuyVal += (double)item?.BuyPrice;
                        totSellQty += (double)item?.SellQty;
                        totSellVal += (double)item?.SellPrice;
                        avgDivAmt += (double)item?.DivAmt;
                        avgDivPct += (double)item?.DivPct;
                    }

                    PrinterData = $"  ◄►  ◄►  ◄►  Stock report for {DateTime.Now.ToLongDateString()}  ◄►  ◄►  ◄► " + Environment.NewLine;
                    PrinterData = PrinterData + new string('─', delimLen) + Environment.NewLine;
                    // Setup our header row for the printer report
                    string formatReport = "{0,-7}{1,-18}{2,-7}{3,-9}{4,-7}{5,-9}{6,-7}{7,-7}{8,-9}"; //negative left-justifies, while positive right-justifies
                    object[] objReport = { "CODE", "NAME", "BYQTY", "BYVAL", "SLQTY", "SLVAL", "DIVAMT", "DIVPCT", "PROFIT" };
                    PrinterData = PrinterData + String.Format(formatReport, objReport) + Environment.NewLine;
                    PrinterData = PrinterData + new string('─', delimLen) + Environment.NewLine;
                    foreach (var tmp in buffer)
                    {
                        PrinterData = PrinterData + tmp + Environment.NewLine;
                    }
                    PrinterData = PrinterData + new string('═', delimLen) + Environment.NewLine;
                    double totAvgDivAmt = 0;
                    try { totAvgDivAmt = avgDivAmt / totItems; } catch { }
                    double totAvgDivPct = 0;
                    try { totAvgDivPct = avgDivPct / totItems; } catch { }
                    string formatTally = "{0,-25}{1,-7}{2,-9}{3,-7}{4,-9}{5,-7}{6,-7}{7,-9}"; 
                    object[] objTally = { $"Totals: ({totItems} Items)", totBuyQty, totBuyVal, totSellQty, totSellVal, totAvgDivAmt.ToString("0.0"), totAvgDivPct.ToString("0.0"), $"${totProfit}" };
                    PrinterData = PrinterData + String.Format(formatTally, objTally);
                    return true;
                }
                else
                {
                    DisplayPopupMessage("No stock data available.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage($"Could not format printer data.");
                Log.Write(eModule.MainModel, $"FormatPrinterData(ERROR): {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Helper method for printer
        /// </summary>
        /// <param name="stock"></param>
        /// <returns></returns>
        private string FormatStockData(Stock stock)
        {
            try
            {
                //string formatString = "{0,-7}{1,-25}{2,-6} {3,-11:M/d/yyyy}{4,-6} {5,-11:M/d/yyyy} ${6,-6}"; //negative left-justifies, while positive right-justifies
                string formatString = "{0,-7}{1,-18}{2,-7}{3,-9}{4,-7}{5,-9}{6,-7}%{7,-6}${8,-9}"; //negative left-justifies, while positive right-justifies

                string tmpName = "";
                if (stock.Name.Length > 18)
                    tmpName = stock.Name.Substring(0, 18);
                else
                    tmpName = stock.Name;

                object[] objLog = { stock.Code, tmpName, stock?.BuyQty, stock?.BuyPrice, stock?.SellQty, stock?.SellPrice, stock?.DivAmt, stock?.DivPct, stock?.Profit };
                return String.Format(formatString, objLog);
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <returns></returns>
        private bool DetermineIfSetHasChanged(HashSet<Stock> mySet1, HashSet<Stock> mySet2)
        {
            HashSet<Stock> excepts = new HashSet<Stock>(mySet1);
            excepts.ExceptWith(mySet2);
            App.Print("> Unique elements of set...");
            // Unique elements of both HashSets
            foreach (Stock stk in excepts)
            {
                Debug.WriteLine(stk);
            }
            return true;
        }
    
        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="message"></param>
        public void DisplayPopupMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return;

            PopupMessage = $"{message}"; //$"{App.Current.ToString().Replace(".App", "")}: {message}";
            ShowPopup = true;
        }

        /// <summary>
        /// Load system settings from disk
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public Settings LoadSettings(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    string imported = Encoding.UTF8.GetString(File.ReadAllBytes(Directory.GetCurrentDirectory() + "\\Settings.cfg"));
                    App.Print($"imported: {imported}");
                    return imported.FromJsonTo<Settings>();
                }
                else
                {
                    App.Print($"WARNING: Settings file does not exist, creating default config file...");
                    SaveSettings(new Settings($"{App.SystemTitle}", DateTime.Now, false, true, true, eSort.BuyDate, "All Years", BackupDays), Directory.GetCurrentDirectory() + "\\Settings.cfg");
                    return null;
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error loading settings!");
                Log.Write(eModule.MainModel, $"LoadSettings(ERROR): {ex.Message}", true);
                return null;
            }
        }

        /// <summary>
        /// Save system settings to disk
        /// </summary>
        /// <param name="sysSettings"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool SaveSettings(Settings sysSettings, string filePath)
        {
            try
            {
                File.WriteAllBytes(filePath, Encoding.UTF8.GetBytes(sysSettings.ToJson()));
                return true;
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error saving settings!");
                Log.Write(eModule.MainModel, $"SaveSettings(ERROR): {ex.Message}", true);
                return false;
            }
        }

        /// <summary>
        /// Helper method
        /// </summary>
        private void CalculateTotalValue()
        {
            double valBCost = 0;
            double valBQty = 0;
            double valSCost = 0;
            double valSQty = 0;
            double valProfit = 0;

            try
            {
                if (Stocks != null && Stocks.Count > 0)
                {
                    foreach (var item in Stocks)
                    {
                        if ((bool)item?.BuyPrice.HasValue)
                            valBCost += (double)item?.BuyPrice;
                        if ((bool)item?.BuyQty.HasValue)
                            valBQty += (double)item?.BuyQty;
                        if ((bool)item?.SellPrice.HasValue)
                            valSCost += (double)item?.SellPrice;
                        if ((bool)item?.SellQty.HasValue)
                            valSQty += (double)item?.SellQty;
                        if ((bool)item?.Profit.HasValue)
                            valProfit += (double)item?.Profit;
                    }
                    TotalBuyValue = "Total Buy Value... $" + valBCost.ToString("#,###,##0.00");
                    TotalBuyQty = "Total Buy Qty..... " + valBQty.ToString("#,###,##0");
                    TotalSellValue = "Total Sell Value.. $" + valSCost.ToString("#,###,##0.00");
                    TotalSellQty = "Total Sell Qty.... " + valSQty.ToString("#,###,##0");
                    TotalProfit = "Total Profit $" + valProfit.ToString("#,###,##0.00");
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error calculating total!");
                Log.Write(eModule.MainModel, $"CalculateTotalValue(ERROR): {ex.Message}", true);
            }
        }

        /// <summary>
        /// Main sorting method
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sortType"></param>
        private void SortStocks(ObservableCollection<Stock> data, eSort sortType)
        {
            // We may get called on startup before structures are initialized
            if (data == null || data.Count == 0)
                return;

            DateTime dtStartCurrent = new DateTime(DateTime.Now.Year, 1, 1);       //first day of current year
            DateTime dtStopCurrent = new DateTime(DateTime.Now.Year, 12, 31);      //last day of current year
            DateTime dtStartPrevious = new DateTime(DateTime.Now.Year - 1, 1, 1);  //first day of previous year
            DateTime dtStopPrevious = new DateTime(DateTime.Now.Year - 1, 12, 31); //last day of previous year
            DateTime buyDate = DateTime.MinValue;
            DateTime sellDate = DateTime.MinValue;
            //Stock debugStock = new Stock() { BuyDate = null, BuyPrice = null, BuyQty = null, Code = "DBG", ID = null, Name = "DEBUG", Notes = "SortStocks()", Profit = null, Reminder = null };

            try
            {
                // Make a copy of original stock listing
                /*
                if (MainStocks.Count == 0 && SelectedYear != YearMethods[0])
                {
                    foreach (Stock item in data)
                    {
                        MainStocks.Add(item);
                    }
                }
                else
                {
                    App.Print("[+] ADDING MAINSTOCKS BACK IN [+]");
                    data.Clear();
                    foreach (Stock item in MainStocks)
                    {
                        data.Add(item);
                    }

                    if (SelectedYear != YearMethods[0])
                        MainStocks.Clear();
                }
                */

                // Build our sortable list
                sortList.Clear();
                foreach (var item in data)
                {
                    if ((bool)item?.BuyDate.HasValue)
                        buyDate = (DateTime)item?.BuyDate;
                    else
                        buyDate = DateTime.MinValue;

                    if ((bool)item?.SellDate.HasValue)
                        sellDate = (DateTime)item?.SellDate;
                    else
                        sellDate = DateTime.MinValue;

                    //Put data into observable
                    if (_selectedYear == YearMethods[0])
                    {
                        sortList.Add(item);
                    }
                    else //catch-all
                    {
                        sortList.Add(item);
                    }

                    //Need to fix this
                    /*
                    else if (_selectedYear == eYear.BoughtThisYear)
                    {
                        if (buyDate.Between(dtStartCurrent, dtStopCurrent))
                            sortList.Add(item);
                    }
                    else if (_selectedYear == eYear.SoldThisYear)
                    {
                        if (sellDate.Between(dtStartCurrent, dtStopCurrent))
                            sortList.Add(item);
                    }
                    else if (_selectedYear == eYear.BoughtLastYear)
                    {
                        if (buyDate.Between(dtStartPrevious, dtStopPrevious))
                            sortList.Add(item);
                    }
                    else if (_selectedYear == eYear.SoldLastYear)
                    {
                        if (sellDate.Between(dtStartPrevious, dtStopPrevious))
                            sortList.Add(item);
                    }
                    else //catch-all
                    {
                        sortList.Add(item);
                    }
                    */

                }

                //o---------------------------o
                //|    Sorting methods here   |
                //o---------------------------o

                // Using List's built-in sort method...
                //uList.Sort((x, y) => DateTime.Compare(x.Installed, y.Installed));
                //Also... uList.Sort((x, y) => x.Installed.CompareTo(y.Installed));

                if (sortType == eSort.BuyDate)
                    sortList = sortList.OrderByDescending(x => x?.BuyDate).ToList();
                else if (sortType == eSort.BuyPrice)
                    sortList = sortList.OrderByDescending(x => x?.BuyPrice).ToList();
                else if (sortType == eSort.SellDate)
                    sortList = sortList.OrderByDescending(x => x?.SellDate).ToList();
                else if (sortType == eSort.SellPrice)
                    sortList = sortList.OrderByDescending(x => x?.SellPrice).ToList();
                else if (sortType == eSort.Profit)
                    sortList = sortList.OrderByDescending(x => x?.Profit).ToList();
                else if (sortType == eSort.Code)
                    sortList = sortList.OrderBy(x => x.Code).ToList();
                else if (sortType == eSort.Name)
                    sortList = sortList.OrderBy(x => x.Name).ToList();
                else
                    sortList = sortList.OrderBy(x => x?.ID).ToList();


                // Re-populate our observable collection
                Stocks.Clear();
                foreach (var item in sortList)
                {
                    //debugStock = new Stock() { BuyDate = item?.BuyDate, BuyPrice = item?.BuyPrice, BuyQty = item?.BuyQty, Code = item?.Code, ID = item?.ID, Name = item?.Name, Notes = item?.Notes, Profit = item?.Profit, Reminder = item?.Reminder };
                    // Can't sell more than you have
                    if ((bool)item?.SellQty.HasValue && (bool)item?.BuyQty.HasValue)
                    {
                        if (item?.SellQty > item?.BuyQty)
                            item.SellQty = item.BuyQty;
                    }

                    // Calculate profit for stock item
                    item.Profit = CalculateProfit(item);

                    // Calculate color for profit value
                    item.StockBrush = CalculateColor(item?.Profit);

                    // Add to observable collection
                    Stocks.Add(item);
                }

                CalculateTotalValue();
                UpdateTitle = $"Currently viewing {Stocks.Count} stocks from {SelectedYear}";
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error sorting stocks!");
                Log.Write(eModule.MainModel, $"SortStocks(ERROR): {ex.Message}", true);
            }
        }

        /// <summary>
        /// Calculate color for profit value
        /// </summary>
        /// <param name="profit"></param>
        /// <returns></returns>
        private string CalculateColor(double? profit)
        {
            if (profit.HasValue)
            {
                if (profit > 0.00)
                    return Colors.LimeGreen.ToString();
                else if (profit < 0.00)
                    return "#FF2626"; // Colors.Red.ToString()
                else
                    return Colors.LightGray.ToString();
            }
            else
                return Colors.Purple.ToString();
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="stock"></param>
        /// <returns></returns>
        private double CalculateProfit(Stock stock)
        {
            double? retVal = 0.0;

            // Only show profit is something was sold
            if (stock?.SellQty > 0)
            {

                // Calculate change direction
                if (stock?.BuyPrice < stock?.SellPrice)
                    retVal = (stock?.SellPrice - stock?.BuyPrice) * stock?.SellQty;
                else if (stock?.BuyPrice > stock?.SellPrice)
                    retVal = (stock?.BuyPrice - stock?.SellPrice) * stock?.SellQty * -1;
                else
                    retVal = 0.0; //no change

                retVal = Math.Round((double)retVal, 3, MidpointRounding.AwayFromZero);
            }


            return (double)retVal;
        }

        /// <summary>
        /// Testing method
        /// </summary>
        /// <returns></returns>
        private async Task CheckboxMethod()
        {
            await Task.Run(() =>
            {
                // Some demonstration logic for acting on checkbox changed events
                if (cbAuto && cbRemind && cbTips)
                    App.Print($"All of the checkboxes are checked.");
                else if (cbAuto || cbRemind || cbTips)
                    App.Print($"One of the checkboxes is checked.");
                else
                    App.Print($"None of the checkboxes are checked.");

            });
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        internal static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;
            try
            {
                stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread 
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }
            }
            //file is not locked   
            return false;
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <returns></returns>
        private async Task DeleteStockMethod()
        {
            try
            {
                if (EnableDelete)
                {
                    if ((bool)SelectedStock?.ID.HasValue)
                    {
                        MessageBoxResult result = CustomMessageBox.ShowYesNo(
                            $"  Are you sure you want to remove: {SelectedStock?.Name} ?  ",
                            $"{App.SystemTitle}",
                            "Yes",
                            "No",
                            MessageBoxImage.Warning);

                        if (result == MessageBoxResult.Yes)
                        {
                            int tmp = (int)SelectedStock?.ID;
                            lock (Stocks)
                            {
                                Stocks.Remove(SelectedStock);

                                // We're going to save after each change since the user
                                // can switch years now.
                                //SaveNeeded = true;
                            }
                            DisplayPopupMessage("Operation successful.");

                            int numYear = 0;
                            if (Int32.TryParse(SelectedYear, out numYear))
                            {
                                SaveStocksThread(Stocks, numYear);
                            }
                            else
                            {
                                DisplayPopupMessage($"You must select a year to edit/save stocks.");
                            }
                        }
                        else
                        {
                            DisplayPopupMessage("Operation canceled.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DisplayPopupMessage("Error deleting stock!");
                Log.Write(eModule.MainModel, $"DeleteStock(ERROR): {ex.Message}", true);
            }
        }

        /// <summary>
        /// Heler method
        /// </summary>
        /// <returns></returns>
        private async Task LookupStockMethod()
        {
            if (SelectedStock == null || string.IsNullOrEmpty(SelectedStock.Code))
                return;

            // === === ===  IMPORTANT NOTE  === === ===
            // If you want the button to be disabled while the task
            // runs then add "await" in front of the "Task.Run(()".
            // If you want to be able to click the button again
            // to cancel the task then remove the "await".
            // We want the CancellationTokenSource to be global
            // in our MainViewModel so that we can cancel this
            // task from anywhere in the MainViewModel system.
            await Task.Run(() =>
            {
                if (_cts == null) // We want to start the task
                {
                    if (AllowClose)
                    {
                        CheckContext = "Checking...";
                        AllowClose = false;
                        CurrentTaskStatus = eTaskResult.Initialize;

                        _cts = new CancellationTokenSource(_maxToken);
                        CancellationToken tok = _cts.Token;

                        List<Task> _taskQueue = new List<Task>();
                        // Setup the task queue
                        _taskQueue.Add(Task.Run(() =>
                        {
                            UpdateTitle = $"Looking up stock code using browser...";
                            // Yahoo finance...
                            // For exact quote
                            //https://finance.yahoo.com/quote/GOOG?p=GOOG
                            // For lookup search
                            //https://finance.yahoo.com/lookup?s=GOOG
                            System.Diagnostics.Process.Start("https://finance.yahoo.com/quote/" + SelectedStock.Code + "?p=" + SelectedStock.Code);
                            UpdateTitle = $"Browser stock lookup complete";
                        }, tok));
                        CurrentTaskStatus = eTaskResult.Created;

                        // Create the tasks queue
                        Task allWork = Task.WhenAll(_taskQueue); //You can use WhenAny() to signal if any of the tasks have completed
                        allWork.ConfigureAwait(false);
                        try
                        {
                            if (!allWork.IsCompleted)
                            {
                                CurrentTaskStatus = eTaskResult.Running;

                                // Wait for the tasks to complete
                                allWork.Wait(tok);

                                App.Print($"allWork: {allWork.Status}");
                                if (allWork.Status == TaskStatus.RanToCompletion)
                                    CurrentTaskStatus = eTaskResult.Success;
                                else if (allWork.Status == TaskStatus.Faulted)
                                    CurrentTaskStatus = eTaskResult.Failure;
                                else if (allWork.Status == TaskStatus.WaitingForActivation)
                                    CurrentTaskStatus = eTaskResult.Warning;
                            }
                            else
                            {
                                CurrentTaskStatus = eTaskResult.Invalid;
                            }

                        }
                        catch (OperationCanceledException ocex)
                        {
                            App.Print($"OperationCanceledException: {ocex.Message}");
                            CurrentTaskStatus = eTaskResult.Canceled;
                        }
                        catch (ObjectDisposedException odex)
                        {
                            App.Print($"ObjectDisposedException: {odex.Message}");
                            CurrentTaskStatus = eTaskResult.Failure;
                        }
                        catch (AggregateException aex)
                        {
                            App.Print($"AggregateException: {aex.Message}");
                            CurrentTaskStatus = eTaskResult.Failure;
                        }
                        finally
                        {
                            AllowClose = true;
                            CheckContext = "Check for updates";
                            _cts = null;
                        }
                    }
                    else
                    {
                        CurrentTaskStatus = eTaskResult.Invalid;
                    }

                }
                else // We want to cancel the task
                {
                    CurrentTaskStatus = eTaskResult.Canceling;
                    _cts.Cancel();
                    _cts = null;
                }
            });
        }
        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Misc/Test Methods]
        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="pSeed"></param>
        /// <param name="pLength"></param>
        /// <returns></returns>
        public static string NameGen(int pSeed, int pLength)
        {
            const string pwChars =
                "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                "abcdefghijklmnopqrstuvwxyz" +
                "0123456789";

            if (pLength <= 3)
                pLength = 3; //minimum of 4 characters (due to complexity requirements)

            char[] charArray = pwChars.Distinct().ToArray();

            //int length = 12;
            var result = new char[pLength];
            var rng = new Random((int)pSeed);

            for (int x = 0; x < pLength; x++)
            {
                result[x] = pwChars[rng.Next() % pwChars.Length];
            }
            return (new string(result));
        }

        /// <summary>
        /// Helper method
        /// </summary>
        public void GenerateStockData()
        {
            if (Stocks.Count == 0)
            {
                UpdateTitle = $"Loading your stock data...";
                for (int i = 1; i < 61; i++)
                    Stocks.Add(new Stock() { ID = i, Code = "ABC", Name = "Acme Inc.", BuyPrice = 5.101, BuyDate = DateTime.Now, BuyQty = 100, SellPrice = 0.0, SellQty = 0, SellDate = null, Notes = "This is an example note.", Reminder = null, Profit = 2.34, StockBrush = "FFFFFF" });
                UpdateTitle = $"Stock loading is complete";
            }
            else
            {
                //UpdateTitle = $"Stock count: {Stocks.Count}";
                UpdateTitle = $"Re-loading stock data...";
                Stocks.Clear();
                for (int i = 1; i < 61; i++)
                    Stocks.Add(new Stock() { ID = i, Code = "ABC", Name = "Acme Inc.", BuyPrice = 5.101, BuyDate = DateTime.Now, BuyQty = 100, SellPrice = 0.0, SellQty = 0, SellDate = null, Notes = "This is an example note.", Reminder = null, Profit = 2.34, StockBrush = "FFFFFF" });
                UpdateTitle = $"Stock loading is complete";
            }
        }
        /// <summary>
        /// Test method
        /// </summary>
        /// <returns></returns>
        public int BuildStockList()
        {
            try
            {
                int sampleCount = 999;

                Random rnd = new Random();
                int returnValue = 0;
                int seedValue = 0;
                string pswdSeed = DateTime.Now.ToString("MMddyyyy");
                char[] seedArray = pswdSeed.ToCharArray();
                foreach (char ch in seedArray)
                {
                    if (int.TryParse(ch.ToString(), out returnValue)) //just use numbers
                        seedValue += returnValue;
                }

                UpdateTitle = $"Building stock data...";

                //o-------------------------------o
                //|    Load user file data here   |
                //o-------------------------------o
                double profit = 0.0;
                double buyprice = 0.0;
                //SolidColorBrush sbc;
                string sbc = "";
                List<Stock> fileData = new List<Stock>();
                for (int i = 0; i < sampleCount; i++)
                {
                    profit = Math.Round(rnd.NextDouble() * 100, 3, MidpointRounding.AwayFromZero);
                    buyprice = Math.Round(rnd.NextDouble() * 100, 3, MidpointRounding.AwayFromZero);

                    if (i % 2 == 0)
                        profit *= -1.0;

                    if (profit >= 80.00)
                        sbc = Colors.LimeGreen.ToString(); // new SolidColorBrush(Colors.LimeGreen);
                    else if (profit >= 40.00)
                        sbc = Colors.Green.ToString(); // new SolidColorBrush(Colors.Green);
                    else if (profit >= 0.01)
                        sbc = Colors.Tan.ToString(); // new SolidColorBrush(Colors.Tan);
                    else if (profit >= -20.00)
                        sbc = Colors.Orange.ToString(); // new SolidColorBrush(Colors.Orange);
                    else if (profit >= -40.00)
                        sbc = Colors.OrangeRed.ToString(); // new SolidColorBrush(Colors.OrangeRed);
                    else if (profit >= -80.00)
                        sbc = Colors.Red.ToString(); // new SolidColorBrush(Colors.Red);
                    else if (profit >= -101.00)
                        sbc = Colors.DarkRed.ToString(); // new SolidColorBrush(Colors.DarkRed);
                    else
                        sbc = Colors.Purple.ToString(); // new SolidColorBrush(Colors.Purple);

                    if (stockChanged && (i == editedStock?.ID))
                        fileData.Add(editedStock);
                    else
                        fileData.Add(new Stock() { ID = i, Code = NameGen(rnd.Next(1, 101), 3), Name = NameGen(rnd.Next(1, 101), 12), BuyPrice = buyprice, BuyDate = DateTime.Now, BuyQty = rnd.Next(1, 1000), SellPrice = 0.0, SellQty = 0, SellDate = null, Notes = "This is an example note.", Reminder = null, Profit = profit, StockBrush = sbc });
                }

                sortList.Clear();
                for (int i = 0; i < fileData.Count; ++i)
                {
                    sortList.Add(fileData[i]);
                }

                //o-------------------------------o
                //|    Sorting our results here   |
                //o-------------------------------o
                // Using List's built-in sort method...
                //uList.Sort((x, y) => DateTime.Compare(x.Installed, y.Installed));
                //Also... uList.Sort((x, y) => x.Installed.CompareTo(y.Installed));

                // Using LINQ...
                sortList = sortList.OrderBy(x => x.ID).ToList();


                // After sort setup our observable collection
                int counter = 0;
                foreach (var item in sortList)
                {
                    counter++;
                    Log.Write(eModule.MainModel, $"ID.....: {item.ID}");
                    Log.Write(eModule.MainModel, $"Code...: {item.Code}");
                    Log.Write(eModule.MainModel, $"Name...: {item.Name}");

                    //Put data into observable
                    Stocks.Add(item);
                }

                UpdateTitle = $"Stock loading is complete";
                SaveStocks(Stocks);

                return counter;
            }
            catch (Exception ex)
            {
                $"ERROR: {ex.Message}".Print(ConsoleColor.Red);
                return -1;
            }
        }
        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Threaded Processing]
        /*** This example uses EventWaitHandles to send signals during thread execution. ***/

        /// <summary>
        /// Start the threaded process
        /// </summary>
        static void StartSystem(bool forceJoin = false)
        {
            if (ProcessThread == null && !processRunning)
            {
                processRunning = true;
                ProcessThread = new Thread(new ThreadStart(ProcessLoop));
                ProcessThread.Priority = ThreadPriority.Normal;
                if (!ProcessThread.IsAlive)
                {
                    ProcessThread.Start();
                    ProcessStarted.Set();
                }
            }
            else if (ProcessThread != null && forceJoin)
            {
                if (ProcessThread.ThreadState == System.Threading.ThreadState.Running)
                {
                    try
                    {
                        ProcessThread.Join(3000);
                    }
                    catch (ThreadStateException tex)
                    {
                        Log.Write(eModule.Threaded, $"Process already running: {ProcessThread.ThreadState}", true);
                    }
                }
                else
                {
                    Log.Write(eModule.Threaded, $"Thread state: {ProcessThread.ThreadState}", true);
                }
            }
            else
            {
                if (ProcessThread != null)
                {
                    Log.Write(eModule.Threaded, $"Process already running: {ProcessThread.ThreadState}", true);
                }
                else
                {
                    Task.Run(() => DownloadPageAsync("http://www.albahari.com/nutshell/E8-CH20.aspx"));
                }
            }
        }

        /// <summary>
        /// Processing loop for thread
        /// </summary>
        static void ProcessLoop()
        {
            string msg = "";
            int waitIdx = -999;
            EventWaitHandle[] handles = new EventWaitHandle[] { ProcessStarted, ProcessReady, ProcessStopped };
            /*
            while (EventWaitHandle.WaitAll(handles) != true)
            {
                Debug.WriteLine("Waiting on all events to complete...");
            }
            */
            while (processRunning)
            {
                waitIdx = EventWaitHandle.WaitAny(handles);
                Debug.WriteLine($"waitIdx: {waitIdx}");
                if (waitIdx == 0) //ProcessStarted
                {
                    Log.Write(eModule.Threaded, $"[ProcessStarted]", true);
                }
                else if (waitIdx == 1) //ProcessReady
                {
                    Debug.WriteLine($"Total: {MessageQueue.Count}");
                    while (MessageQueue.Count > 0)
                    {
                        lock (MessageQueue)
                        {
                            msg = MessageQueue.Dequeue();
                            Debug.WriteLine($"Queue: {msg}");
                        }
                    }
                    StopSystem();
                }
                else if (waitIdx == 2) //ProcessStopped
                {
                    processRunning = false; //we now know that our process is finished, so exit
                }
            }
            handles = null;
            Log.Write(eModule.Threaded, $"[Exiting ProcessLoop()]", true);
        }

        /// <summary>
        /// Halt the thread process
        /// </summary>
        static void StopSystem()
        {
            ProcessStopped.Set();
            Log.Write(eModule.Threaded, $"[ProcessStopped]", true);
        }

        /// <summary>
        /// Add data into the msg queue for the thread process
        /// </summary>
        static void AddDataAndNotifyThread()
        {
            for (int i = 50; i > 0; i--)
            {
                MessageQueue.Enqueue(i.ToString("000"));
                Thread.Sleep(100);
            }
            ProcessReady.Set();
            Log.Write(eModule.Threaded, $"[ProcessReady]", true);
        }
        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [TaskScheduler]
        //This is a support class for setting the priority on a Task Factory object.
        /**** [EXAMPLE] ****************************************************************

            Task.Factory.StartNew(() => 
            {
                //Everything here will be executed in a thread whose priority is BelowNormal

            }, null, TaskCreationOptions.None, PriorityScheduler.BelowNormal);

         *******************************************************************************/
        public class PriorityScheduler : TaskScheduler
        {
            public static PriorityScheduler AboveNormal = new PriorityScheduler(ThreadPriority.AboveNormal);
            public static PriorityScheduler BelowNormal = new PriorityScheduler(ThreadPriority.BelowNormal);
            public static PriorityScheduler Lowest = new PriorityScheduler(ThreadPriority.Lowest);

            private System.Collections.Concurrent.BlockingCollection<Task> _tasks = new System.Collections.Concurrent.BlockingCollection<Task>();
            private Thread[] _threads;
            private ThreadPriority _priority;
            private readonly int _maximumConcurrencyLevel = Math.Max(1, Environment.ProcessorCount);

            public PriorityScheduler(ThreadPriority priority)
            {
                _priority = priority;
            }

            public override int MaximumConcurrencyLevel
            {
                get { return _maximumConcurrencyLevel; }
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return _tasks;
            }

            protected override void QueueTask(Task task)
            {
                _tasks.Add(task);

                if (_threads == null)
                {
                    _threads = new Thread[_maximumConcurrencyLevel];
                    for (int i = 0; i < _threads.Length; i++)
                    {
                        int local = i;
                        _threads[i] = new Thread(() =>
                        {
                            foreach (Task t in _tasks.GetConsumingEnumerable())
                                base.TryExecuteTask(t);
                        });
                        _threads[i].Name = string.Format("PriorityScheduler: ", i);
                        _threads[i].Priority = _priority;
                        _threads[i].IsBackground = true;
                        _threads[i].Start();
                    }
                }
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false; // we might not want to execute task that should schedule as high or low priority inline
            }
        }
        #endregion

        //=== === === === === === === === === === === === === === === === === === === === === === === === 
        //=== === === === === === === === === === === === === === === === === === === === === === === === 

        #region [Misc Extras]
        /*
        Task Scheduler API --> https://www.nuget.org/packages/ASquare.WindowsTaskScheduler/
        //This will create Daily trigger to run every 10 minutes for a duration of 18 hours
        SchedulerResponse response = WindowTaskScheduler
            .Configure()
            .CreateTask("TaskName", "C:\\Test.bat")
            .RunDaily()
            .RunEveryXMinutes(10)
            .RunDurationFor(new TimeSpan(18, 0, 0))
            .SetStartDate(new DateTime(2015, 8, 8))
            .SetStartTime(new TimeSpan(8, 0, 0))
            .Execute();
         */

        static void DownloadPage()
        {
            System.Net.WebRequest req = System.Net.WebRequest.Create("http://www.albahari.com/nutshell/code.html");
            req.Proxy = null;
            using (System.Net.WebResponse res = req.GetResponse())
            {
                using (Stream rs = res.GetResponseStream())
                {
                    using (FileStream fs = File.Create("code_sync.html"))
                    {
                        rs.CopyTo(fs);
                    }
                }
            }
        }


        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="strURL"></param>
        /// <returns></returns>
        static async Task DownloadPageAsync(string strURL)
        {
            string outName = strURL.Split('/').Last();
            outName = outName.Replace(".aspx", ".htm");
            outName = outName.Replace(".asp", ".htm");
            Log.Write(eModule.MiscModel, $"Downloading {outName} ...", true);

            System.Net.WebRequest req = System.Net.WebRequest.Create(strURL);
            req.Proxy = null;
            using (System.Net.WebResponse res = await req.GetResponseAsync())
            {
                using (Stream rs = res.GetResponseStream())
                {
                    using (FileStream fs = File.Create(outName))
                    {
                        await rs.CopyToAsync(fs);
                    }
                }
            }
        }

        /*
        var progress = new Progress<double>();
        progress.ProgressChanged += (sender, value) => System.Console.Write("\r%{0:N0}", value);
        var cancellationToken = new CancellationTokenSource();
        var destination = File.OpenWrite("LINQPad6Setup.exe");
        Task.Run(() => DownloadFileAsync("https://www.linqpad.net/GetFile.aspx?LINQPad6Setup.exe", destination, progress, default));
        */
        static async Task DownloadFileAsync(string url, Stream destination, IProgress<double> progress, CancellationToken token)
        {
            System.Net.Http.HttpClient hClient = new System.Net.Http.HttpClient();

            var response = await hClient.GetAsync(url, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode)
                throw new Exception(string.Format("The request returned with HTTP status code {0}", response.StatusCode));

            var total = response.Content.Headers.ContentLength.HasValue ? response.Content.Headers.ContentLength.Value : -1L;

            var source = await response.Content.ReadAsStreamAsync();

            await CopyStreamWithProgressAsync(source, destination, total, progress, token);
        }

        static async Task CopyStreamWithProgressAsync(Stream input, Stream output, long total, IProgress<double> progress, CancellationToken token)
        {
            const int IO_BUFFER_SIZE = 8 * 1024; // Optimal size depends on your scenario

            // Expected size of input stream may be known from an HTTP header when reading from HTTP.
            // Other streams may have their own protocol for pre-reporting expected size.

            var canReportProgress = total != -1 && progress != null;
            var totalRead = 0L;
            byte[] buffer = new byte[IO_BUFFER_SIZE];
            int read;
            while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                token.ThrowIfCancellationRequested();
                await output.WriteAsync(buffer, 0, read);
                totalRead += read;
                if (canReportProgress)
                    progress.Report((totalRead * 1d) / (total * 1d) * 100);
            }
        }

        static void OpenHtml(string location)
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                Process.Start(new ProcessStartInfo("cmd", $"/c start {location}"));
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                Process.Start("xdg-open", location); // Desktop Linux
            else throw new Exception("Platform-specific code needed to open URL.");
        }

        public static bool CompressFile(string uncompressedFile, string compressedFile)
        {
            try
            {
                // Open the source (uncompressed) file, using a 4K input buffer.
                using (FileStream inStream = new FileStream(uncompressedFile, FileMode.Open, FileAccess.Read, FileShare.None, 4096))
                {
                    // Open the destination (compressed) file with a FileStream object.
                    using (FileStream outStream = new FileStream(compressedFile, FileMode.Create))
                    {
                        // Wrap a DeflateStream object around the output stream.
                        using (DeflateStream zipStream = new DeflateStream(outStream, CompressionMode.Compress))
                        {
                            // Prepare a 4K read buffer .
                            byte[] buffer = new byte[4096];
                            while (true)
                            {
                                // Read up to 4K bytes from the input file, exit the loop if no more bytes.
                                int readBytes = inStream.Read(buffer, 0, buffer.Length);
                                if (readBytes == 0)
                                {
                                    break;
                                }

                                // Write the contents of the buffer to the compressed stream.
                                zipStream.Write(buffer, 0, readBytes);
                            }
                            // Flush and close all streams.
                            zipStream.Flush();
                        }  // Close the DeflateStream object.
                    }     // Close the output FileStream object.
                }        // Close the input FileStream object.
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool UncompressFile(string compressedFile, string uncompressedFile)
        {
            try
            {
                // Open the output (uncompressed) file, use a 4K output buffer.
                using (FileStream outStream = new FileStream(uncompressedFile, FileMode.Create, FileAccess.Write, FileShare.None, 4096))
                {
                    // Open the source (compressed) file.
                    using (FileStream inStream = new FileStream(compressedFile, FileMode.Open))
                    {
                        // Wrap the DeflateStream object around the input stream.
                        using (DeflateStream zipStream = new DeflateStream(inStream, CompressionMode.Decompress))
                        {
                            // Prepare a 4K buffer.
                            byte[] buffer = new byte[4096];
                            while (true)
                            {
                                // Read enough compressed bytes to fill the 4K buffer.
                                int bytesRead = zipStream.Read(buffer, 0, 4096);
                                // Exit the loop if no more bytes were read.
                                if (bytesRead == 0)
                                {
                                    break;
                                }
                                // Else, write these bytes to the uncompressed file, and loop.
                                outStream.Write(buffer, 0, bytesRead);
                            }
                            // Ensure that cached bytes are written correctly and close all streams.
                            outStream.Flush();
                        }  // Close the DeflateStream object.
                    }     // Close the input FileStream object.
                }        // Close the output FileStream object.
                return true;
            }
            catch
            {
                return false;
            }
        }

        /*
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_Processor");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_LogicalDisk");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_Volume");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_NetworkAdapterConfiguration");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_SerialPort");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_PhysicalMedia");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_OnBoardDevice");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_DiskDrive");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_Environment");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_VideoController");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_BIOS");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_LocalTime");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_DesktopMonitor");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_OperatingSystem");
        //PrintPropertiesOfWmiClass("root\\cimv2", "Win32_Account");
        private static void PrintPropertiesOfWmiClass(string namespaceName, string wmiClassName)
        {
            try
            {
                ManagementPath managementPath = new ManagementPath();
                managementPath.Path = namespaceName;
                ManagementScope managementScope = new ManagementScope(managementPath);
                ObjectQuery objectQuery = new ObjectQuery("SELECT * FROM " + wmiClassName);
                ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(managementScope, objectQuery);
                ManagementObjectCollection objectCollection = objectSearcher.Get();
                foreach (ManagementObject managementObject in objectCollection)
                {
                    PropertyDataCollection props = managementObject.Properties;
                    foreach (PropertyData prop in props)
                    {
                        Console.WriteLine("----------------------------------------------------------------");
                        Console.WriteLine("Property name...: {0}", prop.Name);
                        Console.WriteLine("Property type...: {0}", prop.Type);
                        Console.WriteLine("Property value..: {0}", prop.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(MethodBase.GetCurrentMethod().Name + "(ERROR): " + ex.Message);
            }
        }

        //USAGE: List<String> namespaces = GetWmiNamespaces("root");
        private static List<String> GetWmiNamespaces(string root)
        {
            List<String> namespaces = new List<string>();
            try
            {
                ManagementClass nsClass = new ManagementClass(new ManagementScope(root), new ManagementPath("__namespace"), null);
                foreach (ManagementObject ns in nsClass.GetInstances())
                {
                    string namespaceName = root + "\\" + ns["Name"].ToString();
                    namespaces.Add(namespaceName);
                    namespaces.AddRange(GetWmiNamespaces(namespaceName));
                }
            }
            catch (ManagementException me)
            {
                Console.WriteLine(me.Message);
            }

            return namespaces.OrderBy(s => s).ToList();
        }
        */
        #endregion

    }

    #region [Possible View Switching Class]
    /*
    public ViewState eViewState { get; set; }

    public enum ViewState
    {
        MainView,
        EditView
    }
    */

    /*
    public enum ItemViewType { View1, View2 };
    class ItemViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate View1Template { get; set; }
        public DataTemplate View2Template { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var vm = item as MainViewModel;
            if (vm == null)
                return null;

            switch (vm.ViewType)
            {
                case ItemViewType.View1:
                    return View1Template;
                case ItemViewType.View2:
                    return View2Template;
            }

            return null;
        }
    }
    */
    #endregion
}
