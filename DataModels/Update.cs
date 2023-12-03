using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WUApiLib;

namespace UpdateViewer.DataModels
{
    public class Update
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public OperationResultCode ResultCode { get; set; }
        /*
            orcNotStarted = 0,
            orcInProgress = 1,
            orcSucceeded = 2,
            orcSucceededWithErrors = 3,
            orcFailed = 4,
            orcAborted = 5
        */
        public DateTime Installed { get; set; }
        public SolidColorBrush ResultBrush { get; set; }
    }
}
