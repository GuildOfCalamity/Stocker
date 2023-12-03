using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UpdateViewer.Support
{
    public static class Log
    {
        private static object fileLock = new object();
        private static string _filePath = null;

        /// <summary>
        /// Log file path property
        /// </summary>
        private static string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_filePath))
                {
                    DriveInfo[] info = DriveInfo.GetDrives();
                    if (info.Any(i => (i.DriveType == DriveType.Fixed) && (i.IsReady) && (i.Name == @"D:\")))
                        _filePath = @"D:\Logs\" + App.SystemTitle;
                    else
                        _filePath = @"C:\Logs\" + App.SystemTitle;
                }

                return _filePath;
            }
        }

        /// <summary>
        /// Helper method
        /// </summary>
        /// <param name="module"></param>
        /// <param name="message"></param>
        /// <param name="con"></param>
        public static void Write(eModule module, string message, bool con = false)
        {
            System.Threading.ThreadPool.QueueUserWorkItem(o => {
                try {
                    string path = $@"{FilePath}\{DateTime.Today.Year}\{DateTime.Today.Month.ToString("00")}-{DateTime.Today.ToString("MMMM")}\";
                    lock (fileLock)
                    {
                        DirectoryInfo dInfo = new DirectoryInfo(path);
                        if (!dInfo.Exists)
                            dInfo.Create();

                        path += $"{App.SystemTitle}_{DateTime.Now.ToString("dd")}.log";

                        if (con) //extra debugging option
                            $"[{DateTime.Now.ToLongTimeString()}] {message}".Print(ConsoleColor.White);

                        string formatString = "{0:[yyyy-MM-dd hh:mm:ss.fff tt]} {1}";
                        object[] objLog = { DateTime.Now, message };
                        string logString = String.Format(formatString, objLog);
                        using (StreamWriter writer = new StreamWriter(path, true))
                            writer.WriteLine(String.Format(formatString, objLog));
                    }
                }
                catch (Exception ex) {
                    $"Failed to log to file: {ex.Message}".Print(ConsoleColor.Red);
                }
            });
        }
    }
}
