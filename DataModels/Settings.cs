using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using UpdateViewer.Support;

namespace UpdateViewer.DataModels
{
    //[DataContractAttribute(Name = "StockerObject", Namespace = "UpdateViewer")]
    //[System.SerializableAttribute()]
    [DataContract]
    public class Settings
    {
        private List<object> AllSettings = new List<object>();

        //[System.Runtime.Serialization.DataMemberAttribute()]
        private string _Title = "Stocker";
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private DateTime _LastUsed = DateTime.Now;
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private bool _AutomaticCheck = false;
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private bool _ReminderCheck = false;
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private bool _EnableTips = true;
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private int _BackupDays = 1;
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private eSort _SortType = eSort.BuyDate;
        //[System.Runtime.Serialization.DataMemberAttribute()]
        private string _StockYear = "All Years";

        /// <summary>
        /// Constructor
        /// </summary>
        public Settings(string title, DateTime lastUsed, bool automaticCheck, bool reminderCheck, bool enableTips, eSort sortMethod, string stockYear, int backupDays)
        {
            _Title = title; AllSettings.Add(_Title);
            _LastUsed = lastUsed; AllSettings.Add(_LastUsed);
            _AutomaticCheck = automaticCheck; AllSettings.Add(_AutomaticCheck);
            _ReminderCheck = reminderCheck; AllSettings.Add(_ReminderCheck);
            _EnableTips = enableTips; AllSettings.Add(_EnableTips);
            _SortType = sortMethod; AllSettings.Add(_SortType);
            _StockYear = stockYear; AllSettings.Add(_StockYear);
            _BackupDays = backupDays; AllSettings.Add(_BackupDays);
        }

        [DataMember]
        public string Title
        {
            get { return _Title; }
            set { _Title = value; }
        }

        [DataMember]
        public DateTime LastUsed
        {
            get { return _LastUsed; }
            set { _LastUsed = value; }
        }

        [DataMember]
        public bool AutomaticCheck
        {
            get { return _AutomaticCheck; }
            set { _AutomaticCheck = value; }
        }

        [DataMember]
        public bool ReminderCheck
        {
            get { return _ReminderCheck; }
            set { _ReminderCheck = value; }
        }

        [DataMember]
        public bool EnableTips
        {
            get { return _EnableTips; }
            set { _EnableTips = value; }
        }

        [DataMember]
        public eSort SortType
        {
            get { return _SortType; }
            set { _SortType = value; }
        }

        [DataMember]
        public string StockYear
        {
            get { return _StockYear; }
            set { _StockYear = value; }
        }

        [DataMember]
        public int BackupDays
        {
            get { return _BackupDays; }
            set { _BackupDays = value; }
        }

        public IEnumerable<string> ListSettings()
        {
            yield return _Title;
            yield return _LastUsed.ToString();
            yield return _AutomaticCheck.ToString();
            yield return _ReminderCheck.ToString();
            yield return _EnableTips.ToString();
            yield return _SortType.ToString();
            yield return _StockYear;
        }

        public IEnumerable<object> ListSettingsGeneric()
        {
            foreach (var thing in AllSettings)
            {
                yield return thing;
            }
        }

    }

    /// <summary>
    /// Helper class
    /// </summary>
    public static class TConverter
    {
        public static T ChangeType<T>(object value)
        {
            return (T)ChangeType(typeof(T), value);
        }
        public static object ChangeType(Type t, object value)
        {
            System.ComponentModel.TypeConverter tc = System.ComponentModel.TypeDescriptor.GetConverter(t);
            return tc.ConvertFrom(value);
        }
        public static void RegisterTypeConverter<T, TC>() where TC : System.ComponentModel.TypeConverter
        {

            System.ComponentModel.TypeDescriptor.AddAttributes(typeof(T), new System.ComponentModel.TypeConverterAttribute(typeof(TC)));
        }
    }
}
