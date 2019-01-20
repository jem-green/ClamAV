using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using log4net;

namespace ClamAVLibrary
{
    public class ClamdScan
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        AutoResetEvent signal;
        private object _threadLock = new object();
        private Thread _thread;
        private bool _disposed = false;
        private ManualResetEvent _eventTerminate = new ManualResetEvent(false);
        private bool _running = false;
        private bool _downloading = false;

        List<Setting> _settings = null;
        string _path = "";
        string _databasePath = "";
        string _logPath = "";
        string _logFilenamePath = "";
        string _configFilenamePath = "";

        string _filePath;
        DateTime _startDate;
        TimeSpan _startTime;
        long _timeout = 86400;      // Every day 24 x 60 x 60* seconds
        int _interval = 3600;      // Every hour 60 x 60 seconds

        public enum Location
        {
            Program = 0,
            App = 1,
            Local =2,
            Roaming =3
        }

        public struct Setting
        {
            string _key;
            //object _default;
            object _value;

            //public Setting(string key, object @default, object value)
            //{
            //    _key = key;
            //    _default = @default;
            //    _value = value;
            //}

            public Setting(string key, object value)
            {
                _key = key;
                _value = value;
            }

            public string Key
            {
                get
                {
                    return (_key);
                }
                set
                {
                    _key = value;
                }
            }

            public object Value
            {
                get
                {
                    return (_value);
                }
                set
                {
                    _value = value;
                }
            }
        }

        #endregion
        #region Constructors

        public ClamdScan(Location location, string path)
        {
            _path = path;
            _startDate = new DateTime();
            _startTime = new TimeSpan(_startDate.Hour, _startDate.Minute, _startDate.Second);

            string basePath = "";
            switch (location)
            {
                case Location.App:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case Location.Program:
                    {
                        basePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        int pos = basePath.LastIndexOf('\\');
                        basePath = basePath.Substring(0, pos);
                        break;
                    }
                case Location.Local:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case Location.Roaming:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
            }
            _databasePath = basePath + System.IO.Path.DirectorySeparatorChar + "database";
            if (!Directory.Exists(_databasePath))
            {
                Directory.CreateDirectory(_databasePath);
            }
            _logPath = basePath + System.IO.Path.DirectorySeparatorChar + "logs";
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
            _logFilenamePath = _logPath + System.IO.Path.DirectorySeparatorChar + "clamdscan.log";
            _configFilenamePath = basePath + System.IO.Path.DirectorySeparatorChar + "clamdscan.conf";

            _settings = new List<Setting>();
            _settings.Add(new Setting("AlgorithmicDetection", null));
            _settings.Add(new Setting("AllowAllMatchScan", null));
            _settings.Add(new Setting("AllowSupplementaryGroups", null));
            _settings.Add(new Setting("ArchiveBlockEncrypted", null));
            _settings.Add(new Setting("Bytecode", null));
            _settings.Add(new Setting("BytecodeSecurity", null));
            _settings.Add(new Setting("BytecodeTimeout", null));
            _settings.Add(new Setting("CommandReadTimeout", null));
            _settings.Add(new Setting("CrossFilesystems", null));
            _settings.Add(new Setting("DatabaseDirectory", _databasePath));
            _settings.Add(new Setting("Debug", null));
            _settings.Add(new Setting("DetectBrokenExecutables", null));
            _settings.Add(new Setting("DetectPUA", null));
            _settings.Add(new Setting("DisableCache", null));
            _settings.Add(new Setting("DisableCertCheck", null));
            _settings.Add(new Setting("ExcludePUA", null));
            _settings.Add(new Setting("ExcludePath", null));
            _settings.Add(new Setting("ExitOnOOM", null));
            _settings.Add(new Setting("ExtendedDetectionInfo", null));
            _settings.Add(new Setting("FixStaleSocket", null));
            _settings.Add(new Setting("FollowDirectorySymlinks", null));
            _settings.Add(new Setting("FollowFileSymlinks", null));
            _settings.Add(new Setting("ForceToDisk", null));
            _settings.Add(new Setting("Foreground", null));
            _settings.Add(new Setting("HeuristicScanPrecedence", null));
            _settings.Add(new Setting("IdleTimeout", null));
            _settings.Add(new Setting("IncludePUA", null));
            _settings.Add(new Setting("LeaveTemporaryFiles", null));
            _settings.Add(new Setting("LocalSocketGroup", null));
            _settings.Add(new Setting("LocalSocketMode", null));
            _settings.Add(new Setting("LogClean", null));
            _settings.Add(new Setting("LogFacility", null));
            _settings.Add(new Setting("LogFile", null));
            _settings.Add(new Setting("LogFileMaxSize", null));
            _settings.Add(new Setting("LogFileUnlock", null));
            _settings.Add(new Setting("LogRotate", null));
            _settings.Add(new Setting("LogSyslog", null));
            _settings.Add(new Setting("LogTime", null));
            _settings.Add(new Setting("LogVerbose", null));
            _settings.Add(new Setting("MaxConnectionQueueLength", null));
            _settings.Add(new Setting("MaxDirectoryRecursion", null));
            _settings.Add(new Setting("MaxEmbeddedPE", null));
            _settings.Add(new Setting("MaxFileSize", null));
            _settings.Add(new Setting("MaxFiles", null));
            _settings.Add(new Setting("MaxHTMLNoTags", null));
            _settings.Add(new Setting("MaxHTMLNormalize", null));
            _settings.Add(new Setting("MaxIconsPE", null));
            _settings.Add(new Setting("MaxPartitions", null));
            _settings.Add(new Setting("MaxQueue", null));
            _settings.Add(new Setting("MaxRecHWP3", null));
            _settings.Add(new Setting("MaxRecursion", null));
            _settings.Add(new Setting("MaxScanSize", null));
            _settings.Add(new Setting("MaxScriptNormalize", null));
            _settings.Add(new Setting("MaxThreads", null));
            _settings.Add(new Setting("MaxZipTypeRcg", null));
            _settings.Add(new Setting("OLE2BlockMacros", null));
            _settings.Add(new Setting("OfficialDatabaseOnly", null));
            _settings.Add(new Setting("OnAccessDisableDDD", null));
            _settings.Add(new Setting("OnAccessExcludePath", null));
            _settings.Add(new Setting("OnAccessExcludeUID", null));
            _settings.Add(new Setting("OnAccessExtraScanning", null));
            _settings.Add(new Setting("OnAccessIncludePath", null));
            _settings.Add(new Setting("OnAccessMaxFileSize", null));
            _settings.Add(new Setting("OnAccessMountPath", null));
            _settings.Add(new Setting("OnAccessPrevention", null));
            _settings.Add(new Setting("PCREMatchLimit", null));
            _settings.Add(new Setting("PCREMaxFileSize", null));
            _settings.Add(new Setting("PCRERecMatchLimit", null));
            _settings.Add(new Setting("PartitionIntersection", null));
            _settings.Add(new Setting("PhishingAlwaysBlockCloak", null));
            _settings.Add(new Setting("PhishingAlwaysBlockSSLMismatch", null));
            _settings.Add(new Setting("PhishingScanURLs", null));
            _settings.Add(new Setting("PhishingSignatures", null));
            _settings.Add(new Setting("PidFile", null));
            _settings.Add(new Setting("ReadTimeout", null));
            _settings.Add(new Setting("ScanArchive", null));
            _settings.Add(new Setting("ScanELF", null));
            _settings.Add(new Setting("ScanHTML", null));
            _settings.Add(new Setting("ScanHWP3", null));
            _settings.Add(new Setting("ScanMail", null));
            _settings.Add(new Setting("ScanOLE2", null));
            _settings.Add(new Setting("ScanOnAccess", null));
            _settings.Add(new Setting("ScanPDF", null));
            _settings.Add(new Setting("ScanPE", null));
            _settings.Add(new Setting("ScanPartialMessages", null));
            _settings.Add(new Setting("ScanSWF", null));
            _settings.Add(new Setting("ScanXMLDOCS", null));
            _settings.Add(new Setting("SelfCheck", null));
            _settings.Add(new Setting("SendBufTimeout", null));
            _settings.Add(new Setting("StatsEnabled", null));
            _settings.Add(new Setting("StatsHostID", null));
            _settings.Add(new Setting("StatsPEDisabled", null));
            _settings.Add(new Setting("StatsTimeout", null));
            _settings.Add(new Setting("StreamMaxLength", null));
            _settings.Add(new Setting("StreamMaxPort", null));
            _settings.Add(new Setting("StreamMinPort", null));
            _settings.Add(new Setting("StructuredDataDetection", null));
            _settings.Add(new Setting("StructuredMinCreditCardCount", null));
            _settings.Add(new Setting("StructuredMinSSNCount", null));
            _settings.Add(new Setting("StructuredSSNFormatNormal", null));
            _settings.Add(new Setting("StructuredSSNFormatStripped", null));
            _settings.Add(new Setting("TCPAddr", null));
            _settings.Add(new Setting("TCPSocket", null));
            _settings.Add(new Setting("TemporaryDirectory", null));
            _settings.Add(new Setting("User", null));
            _settings.Add(new Setting("VirusEvent", null));
        }

        #endregion
        #region Properties

        public List<Setting> Config
        {
            get
            {
                return (_settings);
            }
            set
            {
                _settings = value;
            }
        }

        public DateTime Date
        {
            get
            {
                return (_startDate);
            }
            set
            {
                _startDate = value;
            }
        }

        public int Interval
        {
            get
            {
                return (_interval);
            }
            set
            {
                _interval = value;
            }
        }

        public string Path
        {
            get
            {
                return (_path);
            }
            set
            {
                _path = value;
            }
        }

        public string StartDate
        {
            get
            {
                return (_startDate.ToString());
            }
            set
            {
                _startDate = System.Convert.ToDateTime(value);
            }
        }

        public string StartTime
        {
            get
            {
                return (_startTime.ToString());
            }
            set
            {
                DateTime dateTime = System.Convert.ToDateTime(value);
                _startTime = new TimeSpan(dateTime.Hour, dateTime.Minute, dateTime.Second);
            }
        }

        public TimeSpan Time
        {
            get
            {
                return (_startTime);
            }
            set
            {
                _startTime = value;
            }
        }

        public long Timeout
        {
            get
            {
                return (_timeout);
            }
            set
            {
                _timeout = value;
            }
        }

        #endregion
        #region Methods

        public bool Add(string key, object value)
        {
            bool add = false;
            try
            {
                _settings.Add(new Setting(key, value));
                add = true;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            return (add);
        }

        public bool Add(Setting setting)
        {
            bool add = false;
            try
            {
                _settings.Add(setting);
                add = true;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            return (add);
        }

        public bool Remove(Setting setting)
        {
            bool remove = false;
            try
            {
                _settings.Remove(setting);
                remove = true;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            return (remove);
        }

        public bool WriteConfig()
        {
            return(WriteConfig(true));
        }
        public bool WriteConfig(bool overwrite)
        {
            bool written = false;

            StringWriter config = new StringWriter();
            foreach (Setting setting in _settings)
            {
                if (setting.Value == null)
                {
                    config.Write("# ");
                    config.WriteLine(setting.Key + " ");
                }
                else
                {
                    config.Write(setting.Key + " ");
                    if (setting.Value.GetType() == typeof(string))
                    {
                        config.WriteLine("\"" + setting.Value + "\"");
                    }
                    else
                    {
                        config.WriteLine(setting.Value);
                    }
                }
            }

            if (File.Exists(_configFilenamePath))
            {
                try
                {
                    File.Delete(_configFilenamePath);
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                }
            }

            try
            {
                FileStream fs = new FileStream(_configFilenamePath, FileMode.CreateNew, FileAccess.Write);
                byte[] byteData = null;
                byteData = Encoding.UTF8.GetBytes(config.ToString());
                fs.Write(byteData, 0, byteData.Length);
                fs.Flush();
                fs.Close();
                fs.Dispose();
                written = true;
            }
            catch (Exception ex)
            {
                log.Error(ex.ToString());
            }

            return (written);
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsWatching
        {
            get { return _thread != null; }
        }

        /// <summary>
        /// Start 
        /// </summary>
        public void Start()
        {
            log.Debug("In Start()");

            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            lock (_threadLock)
            {
                if (!IsWatching)
                {
                    _eventTerminate.Reset();
                    _thread = new Thread(new ThreadStart(MonitorThread))
                    {
                        IsBackground = true
                    };
                    _thread.Start();
                }
            }
            log.Debug("Out Start()");
        }

        /// <summary>
        /// Stops the watching thread.
        /// </summary>
        public void Stop()
        {
            log.Debug("In Stop()");

            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            signal.Set();   // force out of the waitOne
            _running = false;

            lock (_threadLock)
            {
                Thread thread = _thread;
                if (thread != null)
                {
                    _eventTerminate.Set();
                    thread.Join();
                }
            }

            log.Debug("Out Stop()");
        }

        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            log.Debug("In Dispose()");
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
            log.Debug("Out Dispose()");
        }
        #endregion
        #region Private

        /// <summary>
        /// 
        /// </summary>
        private void MonitorThread()
        {
            log.Debug("In MonitorThread()");

            try
            {
                ClamdscanLoop();
            }
            catch (Exception e)
            {
                log.Fatal(e.ToString());
            }
            _thread = null;

            log.Debug("Out MonitorThread()");
        }

        /// <summary>
        /// 
        /// </summary>
        private void ClamdscanLoop()
        {
            log.Debug("In ClamdscanLoop()");

            // process clamdscan at the defined intervals

            signal = new AutoResetEvent(false);
            _running = true;

            DateTime start = DateTime.Now;    // Set the start timer

            DateTime startDateTime = new DateTime(_startDate.Year, _startDate.Month, _startDate.Day, _startTime.Hours, _startTime.Minutes, _startTime.Seconds);
            int sleepFor = _interval * 1000; // need to convert to milliseconds
            do
            {
                DateTime now = DateTime.Now;
                TimeSpan span = now.Subtract(startDateTime);
                long elapsed = (long)span.TotalSeconds;

                // options here to either trigger at startDateTime or startDateTime + timeout

                if (elapsed < 0)
                {
                    // Schedule in the future
                    _timeout = -elapsed;
                }

                start = startDateTime.AddSeconds(_timeout * (int)(elapsed / _timeout));   // Calculate the new start

                do
                {
                    signal.WaitOne(sleepFor);    // Every Interval check
                    span = DateTime.Now.Subtract(start);
                }
                while ((((long)span.TotalSeconds < _timeout) && (_timeout > 0)) || (_timeout == 0));

                if (_downloading == false)
                {
                    LaunchClamdscan();
                    _downloading = false;
                }
            }
            while (_running == true);

            log.Debug("Out ClamdscanLoop()");
        }

        private void LaunchClamdscan()
        {
            log.Debug("In LaunchClamdscan()");

            _downloading = true;

            Process proc = new System.Diagnostics.Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "freshclam.exe";   // Assume that clamAV is installed in the same location
            startInfo.Arguments = "--config-file " + _configFilenamePath + " " + _filePath;
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            // Trap Standard output
            startInfo.RedirectStandardOutput = true;
            proc.OutputDataReceived += new DataReceivedEventHandler(OutputReceived);
            // Trap Standard error
            startInfo.RedirectStandardError = true;
            proc.ErrorDataReceived += new DataReceivedEventHandler(ErrorReceived);
            // Enable exit event to be raised

            proc.EnableRaisingEvents = true;
            proc.StartInfo = startInfo;
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            _downloading = true;
        }
        #endregion
        private void OutputReceived(object sendingProcess, DataReceivedEventArgs outputData)
        {
            log.Info("Output=" + outputData.Data);
        }

        private void ErrorReceived(object sendingProcess, DataReceivedEventArgs errorData)
        {
            if (errorData != null)
            {
                if (errorData.Data != "")
                {
                    log.Error("Error=" + errorData.Data);
                }
            }
        }
    }

    /*
                      Clam AntiVirus: Daemon Client 0.100.2
           By The ClamAV Team: https://www.clamav.net/about.html#credits
           (C) 2007-2018 Cisco Systems, Inc.

    clamdscan [options] [file/directory/-]

    --help              -h             Show this help
    --version           -V             Print version number and exit
    --verbose           -v             Be verbose
    --quiet                            Be quiet, only output error messages
    --stdout                           Write to stdout instead of stderr
                                       (this help is always written to stdout)
    --log=FILE          -l FILE        Save scan report in FILE
    --file-list=FILE    -f FILE        Scan files from FILE
    --remove                           Remove infected files. Be careful!
    --move=DIRECTORY                   Move infected files into DIRECTORY
    --copy=DIRECTORY                   Copy infected files into DIRECTORY
    --config-file=FILE                 Read configuration from FILE.
    --allmatch            -z           Continue scanning within file after finding a match.
    --multiscan           -m           Force MULTISCAN mode
    --infected            -i           Only print infected files
    --no-summary                       Disable summary at end of scanning
    --reload                           Request clamd to reload virus database
    --fdpass                           Pass filedescriptor to clamd (useful if clamd is running as a different user)
    --stream                           Force streaming files to clamd (for debugging and unit testing)
     */

    /*
     * 
     */

}
