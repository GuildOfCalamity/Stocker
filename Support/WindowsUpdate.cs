using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;

namespace UpdateViewer.Support
{
    internal class WUSearchCallback : ISearchCompletedCallback
    {
        private EventWaitHandle _handle;

        public WUSearchCallback(EventWaitHandle handle)
        {
            _handle = handle;
        }

        public void Invoke(ISearchJob searchJob, ISearchCompletedCallbackArgs callbackArgs)
        {
            _handle.Set();
        }
    }
    internal class WUDownloadCallback : IDownloadCompletedCallback
    {
        private EventWaitHandle _handle;

        public WUDownloadCallback(EventWaitHandle handle)
        {
            _handle = handle;
        }

        public void Invoke(IDownloadJob downloadJob, IDownloadCompletedCallbackArgs callbackArgs)
        {
            _handle.Set();
        }
    }
    internal class WUDownloadProgressChanged : IDownloadProgressChangedCallback
    {
        public void Invoke(IDownloadJob job, IDownloadProgressChangedCallbackArgs args)
        {
            System.Diagnostics.Debug.WriteLine($"> PercentComplete: {job.GetProgress().PercentComplete}%");
        }
    }

    internal class WindowsUpdateSession
    {
        public WindowsUpdateSession()
        {
            Session = new UpdateSession();
            Result = null;
            DownloadResult = null;
        }

        public UpdateSession Session { get; set; }
        public ISearchResult Result { get; set; }
        public bool SearchComplete { get; set; }
        public IDownloadResult DownloadResult { get; set; }
        public bool DownloadComplete { get; set; }
    }
}
