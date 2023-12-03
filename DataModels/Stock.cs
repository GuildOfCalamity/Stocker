using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace UpdateViewer.DataModels
{
    [DataContract]
    public class Stock
    {

        [DataMemberAttribute()]
        public int? ID { get; set; }
        
        [DataMemberAttribute()]
        public string Code { get; set; }
        
        [DataMemberAttribute()]
        public string Name { get; set; }
        
        [DataMemberAttribute()]
        public double? BuyQty { get; set; }
        
        [DataMemberAttribute()]
        public double? BuyPrice { get; set; }
        
        [DataMemberAttribute()]
        public DateTime? BuyDate { get; set; }
        
        [DataMemberAttribute()]
        public double? SellQty { get; set; }
        
        [DataMemberAttribute()]
        public double? SellPrice { get; set; }
        
        [DataMemberAttribute()]
        public DateTime? SellDate { get; set; }
        
        [DataMemberAttribute()]
        public double? Profit { get; set; }

        [DataMemberAttribute()]
        public double? DivAmt { get; set; }

        [DataMemberAttribute()]
        public double? DivPct { get; set; }

        [DataMemberAttribute()]
        public double? Resv1 { get; set; }

        [DataMemberAttribute()]
        public double? Resv2 { get; set; }

        [DataMemberAttribute()]
        public string Notes { get; set; }
        
        [DataMemberAttribute()]
        public DateTime? Reminder { get; set; }

        //Unfortunately I had to change this to a string so that the contract serializer would not shit itself
        [DataMemberAttribute()]
        public string StockBrush { get; set; }
        //public SolidColorBrush StockBrush { get; set; }
        /*
        [DataMemberAttribute()]
        public string StoredBrush
        {
            get => StockBrush.ToString();
            protected set
            {
            }
        }
        */

        /*
        public Stock(Stock newStock)
        {
            ID = newStock.ID;
            Code = newStock.Code;
            Name = newStock.Name;
            BuyQty = newStock.BuyQty;
            BuyPrice = newStock.BuyPrice;
            BuyDate = newStock.BuyDate;
            SellQty = newStock.SellQty;
            SellPrice = newStock.SellPrice;
            SellDate = newStock.SellDate;
            Profit = newStock.Profit;
            Notes = newStock.Notes;
            Reminder = newStock.Reminder;
        }
        */
    }
}
