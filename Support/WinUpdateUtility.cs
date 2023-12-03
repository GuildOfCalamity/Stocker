using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WUApiLib;

namespace UpdateViewer.Support
{
    internal static class WinUpdateUtility
    {
        /*
            You can check if the TrustedInstaller is running by typing this...
            "C:\> sc query trustedinstaller"
            
            You can check if the Windows Automatic Update is running by typing this...
            "C:\> sc query wuauserv"

            You can clean up the WinSxS updates by typing this...
            "C:\> DISM.exe /Online /Cleanup-image /Restorehealth"
         */

        #region [Methods]

        // Search Routines
        public static ISearchResult SearchWindowsUpdate(UpdateSession session, TimeSpan timeout)
        {
            return SearchWindowsUpdate(session, (int)timeout.TotalMilliseconds);
        }
        public static ISearchResult SearchWindowsUpdate(UpdateSession session, int timeout)
        {
            // Search through the available updates
            Log.Write(eModule.WinUpdate, "Checking for Windows updates...");
            var searcher = session.CreateUpdateSearcher();
            var waitHandle = new ManualResetEvent(false);

            var searchCallback = new WUSearchCallback(waitHandle);
            var job = searcher.BeginSearch("IsInstalled = 0 and Type = 'Software' and IsHidden = 0", searchCallback, null);

            ISearchResult result = null;
            if (!waitHandle.WaitOne(timeout))
            {
                Log.Write(eModule.WinUpdate, $"Windows Update is taking too long to respond while it is searching for available updates.  Aborting the process.");
                job.RequestAbort();
            }
            else if (job.IsCompleted)
            {
                result = searcher.EndSearch(job);
                foreach (IUpdate update in result.Updates)
                {
                    Log.Write(eModule.WinUpdate, $"Title.......: {update.Title}");
                    Log.Write(eModule.WinUpdate, $"Description.: {update.Description}");
                    Log.Write(eModule.WinUpdate, $"IsMandatory.: {update.IsMandatory}");
                    Log.Write(eModule.WinUpdate, $"IsDownloaded: {update.IsDownloaded}");
                    Log.Write(eModule.WinUpdate, $"MsrcSeverity: {update.MsrcSeverity}");
                }
            }

            return result;
        }

        public static ISearchResult SearchWindowsInstalled(UpdateSession session, int timeout)
        {
            /*
            HRESULT IsBeta( [out, retval] VARIANT_BOOL *retval );
            HRESULT IsDownloaded( [out, retval] VARIANT_BOOL *retval );
            HRESULT IsHidden( [out, retval] VARIANT_BOOL *retval );
            HRESULT IsHidden( [in] VARIANT_BOOL value );
            HRESULT IsInstalled( [out, retval] VARIANT_BOOL *retval );
            HRESULT IsMandatory( [out, retval] VARIANT_BOOL *retval );
            HRESULT IsUninstallable( [out, retval] VARIANT_BOOL *retval );
             */

            // Search through the installed updates
            Log.Write(eModule.WinUpdate, "Checking for installed updates...");
            var searcher = session.CreateUpdateSearcher();
            var waitHandle = new ManualResetEvent(false);

            var searchCallback = new WUSearchCallback(waitHandle);
            var job = searcher.BeginSearch("IsInstalled = 1 and Type = 'Software'", searchCallback, null);

            ISearchResult result = null;
            if (!waitHandle.WaitOne(timeout))
            {
                Log.Write(eModule.WinUpdate, $"Windows Update is taking too long to respond while it is searching for available updates.  Aborting the process.");
                job.RequestAbort();
            }
            else if (job.IsCompleted)
            {
                result = searcher.EndSearch(job);
                foreach (IUpdate update in result.Updates)
                {
                    Log.Write(eModule.WinUpdate, $"Title.......: {update.Title}");
                    Log.Write(eModule.WinUpdate, $"Description.: {update.Description}");
                    Log.Write(eModule.WinUpdate, $"IsMandatory.: {update.IsMandatory}");
                    Log.Write(eModule.WinUpdate, $"IsDownloaded: {update.IsDownloaded}");
                    Log.Write(eModule.WinUpdate, $"MsrcSeverity: {update.MsrcSeverity}");
                }
            }

            return result;
        }

        // Download Routines
        public static IDownloadResult DownloadWindowsUpdate(UpdateSession session, ISearchResult search, TimeSpan timeout)
        {
            return DownloadWindowsUpdate(session, search, (int)timeout.TotalMilliseconds, false);
        }
        public static IDownloadResult DownloadWindowsUpdate(UpdateSession session, ISearchResult search, int timeout, bool skipReboots)
        {
            // Loop through the list of available updates, and determine if they require user interaction.  If they do, skip the update.
            // Otherwise, add them to be downloaded
            Log.Write(eModule.WinUpdate, $"Windows Update detected there were {search.Updates.Count} available updates:", true);
            var download = new UpdateCollection();
            foreach (IUpdate update in search.Updates)
            {
                if (skipReboots && (update.InstallationBehavior.RebootBehavior == InstallationRebootBehavior.irbAlwaysRequiresReboot)) //if ((update.InstallationBehavior.CanRequestUserInput) || (!update.EulaAccepted))
                {
                    //Log.Write(eModule.WinUpdate, $"\t{update.Title} will be skipped because it requires user input or has a license agreement that must be accepted.", true);
                    Log.Write(eModule.WinUpdate, $"\t{update.Title} will be skipped because it requires a reboot.", true);
                }
                else
                {
                    Log.Write(eModule.WinUpdate, $"\t{update.Title} added to download list", true);
                    download.Add(update);
                }
            }

            // Download the updates
            Log.Write(eModule.WinUpdate, $"{download.Count} updates are eligible for download... downloading now.", true);
            if (download.Count > 0)
            {
                try
                {
                    var downloader = session.CreateUpdateDownloader();
                    var waitHandle = new ManualResetEvent(false);

                    var downloadCallback = new WUDownloadCallback(waitHandle);
                    downloader.Updates = download;
                    var job = downloader.BeginDownload(new WUDownloadProgressChanged(), downloadCallback, new object());

                    IDownloadResult result = null;
                    if (!waitHandle.WaitOne(timeout))
                    {
                        Log.Write(eModule.WinUpdate, $"Windows Update is taking too long to respond while it is downloading the available updates. Aborting the process.", true);
                        job.RequestAbort();
                    }
                    else if (job.IsCompleted)
                    {
                        result = downloader.EndDownload(job);
                        /*
                        orcNotStarted = 0,
                        orcInProgress = 1,
                        orcSucceeded = 2,
                        orcSucceededWithErrors = 3,
                        orcFailed = 4,
                        orcAborted = 5
                        */
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    //Exception from HRESULT: 0x80240044 = WU_E_PER_MACHINE_UPDATE_ACCESS_DENIED
                    Log.Write(eModule.WinUpdate, $"DownloadWindowsUpdate(ERROR): {ex}", true);
                }
            }
            else
            {
                Log.Write(eModule.WinUpdate, $"NOTICE: Nothing to download.", true);
            }

            return null;
        }

        // Installation Routines
        public static eWUResult InstallWindowsUpdate()
        {
            try
            {
                // Create a Windows Update Session
                var session = new UpdateSession();

                // Get the search results
                var result = SearchWindowsUpdate(session, TimeSpan.FromMinutes(20));
                if (result != null)
                {
                    var download = DownloadWindowsUpdate(session, result, TimeSpan.FromMinutes(20));
                    return (download != null) ? InstallWindowsUpdate(session, result) : eWUResult.SearchTimeout;
                }

                return eWUResult.SearchTimeout;
            }
            catch (Exception ex)
            {
                Log.Write(eModule.WinUpdate, $"Unable to install windows updates: {ex.ToString()}", true);
                return eWUResult.Error;
            }
        }
        public static eWUResult InstallWindowsUpdate(UpdateSession session, ISearchResult result)
        {
            // If they were successfully downloaded, mark them to be installed
            Log.Write(eModule.WinUpdate, $"Windows Update downloaded the following updates:", true);
            var install = new UpdateCollection();
            foreach (IUpdate update in result.Updates)
            {
                if (update.IsDownloaded)
                {
                    Log.Write(eModule.WinUpdate, $"\t{update.Title} added to install list", true);
                    install.Add(update);
                }
            }

            // Install the updates
            Log.Write(eModule.WinUpdate, $"{install.Count} updates are eligible for installation... installing now.", true);
            if (install.Count > 0)
            {
                var installer = session.CreateUpdateInstaller();
                installer.Updates = install;
                var installResult = installer.Install();

                Log.Write(eModule.WinUpdate, $"Windows Update finished installation. Result: {installResult.ResultCode}, Updates installed:", true);
                for (int i = 0; i < install.Count; i++)
                    Log.Write(eModule.WinUpdate, $"\t{install[i].Title}: {installResult.GetUpdateResult(i).ResultCode}", true);
            }

            return eWUResult.OK;
        }

        #endregion

    }
}
