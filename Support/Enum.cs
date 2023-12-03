using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateViewer.Support
{
    public enum eYear
    {
        AllStocks,
        BoughtThisYear,
        SoldThisYear,
        BoughtLastYear,
        SoldLastYear

    }

    public enum eSort
    {
        Name,
        Code,
        BuyDate,
        SellDate,
        BuyPrice,
        SellPrice,
        Profit,
        DivAmt,
        DivPct
    }

    public enum eModule
    {
        App,
        MainModel,
        UpdateModel,
        ReportModel,
        ConfigModel,
        MiscModel,
        Threaded
    }

    public enum eWUResult
    {
        NotRun,
        OK,
        Error,
        SearchTimeout,
        Unnecessary
    }

    public enum eRegistrySetting
    {
        ServerPort,
        RunWindowsUpdate
    }

    public enum eSQLReplicationType
    {
        NotImplemented,
        Publisher,
        Subscriber
    }

    public enum eTaskResult
    {
        [DefaultValue(0x000)]
        [Description("Initial state (no state)")]
        None,

        [DefaultValue(0x001)]
        [Description("Task has been setup")]
        Created,

        [DefaultValue(0x002)]
        [Description("Task is beginning")]
        Initialize,

        [DefaultValue(0x003)]
        [Description("Task is currently executing")]
        Running,

        [DefaultValue(0x004)]
        [Description("Task completed normally")]
        Success,

        [DefaultValue(0x005)]
        [Description("Task completed with issues")]
        Warning,

        [DefaultValue(0x006)]
        [Description("The task failed")]
        Failure,

        [DefaultValue(0x007)]
        [Description("Something is wrong with the task setup")]
        Invalid,

        [DefaultValue(0x008)]
        [Description("Task is in the process of being canceled")]
        Canceling,  

        [DefaultValue(0x009)]
        [Description("Task has been canceled")]
        Canceled
    }
}
