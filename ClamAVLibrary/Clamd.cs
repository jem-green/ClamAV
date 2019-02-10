﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using log4net;

namespace ClamAVLibrary
{
    /// <summary>
    /// Wrapper class to manage and launch clamd
    /// </summary>
    public class Clamd
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
        long _timeout = 86400;      // Every day 24 x 60 x 60* seconds
        int _interval = 86400;      // Every day 24*60*60* seconds
        UnitType _units = UnitType.day;    // 
        bool _background = true;    // Run in the background
        int _port = 3310;           // Default clamAV port

        public enum Location : int
        {
            Program = 0,
            App = 1,
            Local =2,
            Roaming =3
        }

        public enum UnitType : int
        {
            second = 0,
            minute = 1,
            hour = 2,
            day = 3,
            week = 4,
            month = 5,
            year = 6
        }

        public struct Setting
        {
            public enum Type
            {
                None = -1,
                String = 0,
                Number = 1,
                Value = 2,
                YesNo = 3
            }

            string _key;
            //object _default;
            object _value;
            Type _format;

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
                _format = Type.None;
            }

            public Setting(string key, object value, Type format)
            {
                _key = key;
                _value = value;
                _format = format;
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

            public Type Format
            {
                get
                {
                    return (_format);
                }
                set
                {
                    _format = value;
                }
            }
        }

        #endregion
        #region Constructors

        public Clamd(Location location) : this(location, 0)
        {
        }

        public Clamd(Location location, int port)
        {
			log.Debug("In Clamd()");
            if (port != 0)
            {
                this._port = port;
            }
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
            _logFilenamePath = _logPath + System.IO.Path.DirectorySeparatorChar + "clamd.log";
            _configFilenamePath = basePath + System.IO.Path.DirectorySeparatorChar + "clamd.conf";

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
            _settings.Add(new Setting("Foreground", !_background, Setting.Type.YesNo));
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
            _settings.Add(new Setting("TCPSocket", 3310,Setting.Type.Value));
            _settings.Add(new Setting("TemporaryDirectory", null));
            _settings.Add(new Setting("User", null));
            _settings.Add(new Setting("VirusEvent", null));
            log.Debug("Out Clamd()");
        }

        #endregion
        #region Properties

        public int Port
        {
            get
            {
                return (_port);
            }
            set
            {
                _port = value;
            }
        }

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

        public UnitType Units
        {
            get
            {
                return (_units);
            }
            set
            {
                _units = value;
            }
        }
        public bool IsBackground
		{
            get
            {
                return (_background);
            }
            set
            {
                _background = value;
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
            log.Debug("In WriteConfig()");
            bool written = false;

            StringWriter config = new StringWriter();
            foreach (Setting setting in _settings)
            {
                if (setting.Value == null)
                {
                    config.Write("# ");
                    config.WriteLine(setting.Key);
                }
                else
                {
                    config.Write(setting.Key + " ");
                    if (setting.Format != Setting.Type.None)
                    {
                        switch (setting.Format)
                        {
                            case Setting.Type.Value:
                                {
                                    config.WriteLine(setting.Value);
                                    break;
                                }
                            case Setting.Type.Number:
                                {
                                    config.WriteLine(setting.Value);
                                    break;
                                }
                            case Setting.Type.String:
                                {
                                    config.WriteLine("\"" + setting.Value + "\"");
                                    break;
                                }
                            case Setting.Type.YesNo:
                                {
                                    if (Convert.ToBoolean(setting.Value) == true)
                                    {
                                        config.WriteLine("yes");
                                    }
                                    else
                                    {
                                        config.WriteLine("no");
                                    }
                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (setting.Value.GetType() == typeof(string))
                        {
                            config.WriteLine("\"" + setting.Value + "\"");
                        }
                        else if (setting.Value.GetType() == typeof(bool))
                        {
                            config.WriteLine(setting.Value);
                        }
                        else
                        {
                            config.WriteLine(setting.Value);
                        }
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
            log.Debug("Out WriteConfig()");
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
        /// Start watching.
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

        #region Events

        private void OutputReceived(object sendingProcess, DataReceivedEventArgs outputData)
        {
            if ((outputData != null) && (outputData.Data != null))
            {
                if (outputData.Data.Trim() != "")
                {
                    log.Info("Output=" + outputData.Data);
                }
            }
        }

        private void ErrorReceived(object sendingProcess, DataReceivedEventArgs errorData)
        {
            if ((errorData != null) && (errorData.Data != null))
            {
                if (errorData.Data.Trim() != "")
                {
                    log.Error("Error=" + errorData.Data);
                }
            }
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
                if (_background == true)
                {
                    LaunchClamd();
                }
                else
                {
                    ClamdLoop();
                }
            }
            catch (Exception e)
            {
                log.Fatal(e.ToString());
            }
            _thread = null;

            log.Debug("Out MonitorThread()");
        }
        /// <summary>
        /// Wait for a specific interval before launching clamd
        /// </summary>
        private void ClamdLoop()
        {
            log.Debug("In ClamdLoop()");

            // process heartbeats at the defined intervals

            signal = new AutoResetEvent(false);
            _running = true;

            int sleepFor = _interval * 1000; // need to convert to milliseconds
            do
            {
                signal.WaitOne(sleepFor);
                // run freshclam
                if (_downloading == false)
                {
                    LaunchClamd();
                    _downloading = false;
                }
            }
            while (_running == true);

            log.Debug("Out ClamdLoop()");
        }

        /// <summary>
        /// Launch the clamd within a process
        /// </summary>
        private void LaunchClamd()
        {
            log.Debug("In LaunchClamd()");

            _downloading = true;

            Process proc = new System.Diagnostics.Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "clamd.exe";   // Assume that clamAV is installed in the same location
            startInfo.Arguments = "--config-file " + _configFilenamePath;
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
            try
            {
            	proc.Start();
                log.Info(startInfo.FileName + " " + startInfo.Arguments);
            	proc.BeginOutputReadLine();
            	proc.BeginErrorReadLine();
            }
            catch(Exception e)
            {
                log.Error(e.ToString());
            }
            _downloading = true;
            log.Debug("Out LaunchClamd()");
        }

        #endregion
    }

    /*
                          Clam AntiVirus: Daemon 0.100.2
               By The ClamAV Team: https://www.clamav.net/about.html#credits
               (C) 2007-2018 Cisco Systems, Inc.

        clamd [options]

        --help                   -h             Show this help
        --version                -V             Show version number
        --debug                                 Enable debug mode
        --config-file=FILE       -c FILE        Read configuration from FILE

        Pass in - as the filename for stdin.
    */

    /*
     * clamd.conf
     * 
        # Uncomment this option to enable logging.
        # LogFile must be writable for the user running daemon.
        # A full path is required.
        # Default: disabled
        LogFile C:\Program Files\ClamAV\logs\clamd.log

        # By default the log file is locked for writing - the lock protects against
        # running clamd multiple times (if want to run another clamd, please
        # copy the configuration file, change the LogFile variable, and run
        # the daemon with --config-file option).
        # This option disables log file locking.
        # Default: no
        #LogFileUnlock yes

        # Maximum size of the log file.
        # Value of 0 disables the limit.
        # You may use 'M' or 'm' for megabytes (1M = 1m = 1048576 bytes)
        # and 'K' or 'k' for kilobytes (1K = 1k = 1024 bytes). To specify the size
        # in bytes just don't use modifiers. If LogFileMaxSize is enabled, log
        # rotation (the LogRotate option) will always be enabled.
        # Default: 1M
        LogFileMaxSize 2M

        # Log time with each message.
        # Default: no
        LogTime yes

        # Also log clean files. Useful in debugging but drastically increases the
        # log size.
        # Default: no
        #LogClean yes

        # Use system logger (can work together with LogFile).
        # Default: no
        #LogSyslog yes

        # Specify the type of syslog messages - please refer to 'man syslog'
        # for facility names.
        # Default: LOG_LOCAL6
        #LogFacility LOG_MAIL

        # Enable verbose logging.
        # Default: no
        #LogVerbose yes

        # Enable log rotation. Always enabled when LogFileMaxSize is enabled.
        # Default: no
        #LogRotate yes

        # Log additional information about the infected file, such as its
        # size and hash, together with the virus name.
        #ExtendedDetectionInfo yes

        # This option allows you to save a process identifier of the listening
        # daemon (main thread).
        # Default: disabled
        #PidFile /var/run/clamd.pid

        # Optional path to the global temporary directory.
        # Default: system specific (usually /tmp or /var/tmp).
        #TemporaryDirectory /var/tmp

        # Path to the database directory.
        # Default: hardcoded (depends on installation options)
        #DatabaseDirectory /var/lib/clamav

        # Only load the official signatures published by the ClamAV project.
        # Default: no
        #OfficialDatabaseOnly no

        # The daemon can work in local mode, network mode or both. 
        # Due to security reasons we recommend the local mode.

        # Path to a local socket file the daemon will listen on.
        # Default: disabled (must be specified by a user)
        # LocalSocket /tmp/clamd.socket

        # Sets the group ownership on the unix socket.
        # Default: disabled (the primary group of the user running clamd)
        #LocalSocketGroup virusgroup

        # Sets the permissions on the unix socket to the specified mode.
        # Default: disabled (socket is world accessible)
        #LocalSocketMode 660

        # Remove stale socket after unclean shutdown.
        # Default: yes
        #FixStaleSocket yes

        # TCP port address.
        # Default: no
        TCPSocket 3310

        # TCP address.
        # By default we bind to INADDR_ANY, probably not wise.
        # Enable the following to provide some degree of protection
        # from the outside world. This option can be specified multiple
        # times if you want to listen on multiple IPs. IPv6 is now supported.
        # Default: no
        TCPAddr 127.0.0.1
        TCPAddr 192.168.1.138

        # Maximum length the queue of pending connections may grow to.
        # Default: 200
        #MaxConnectionQueueLength 30

        # Clamd uses FTP-like protocol to receive data from remote clients.
        # If you are using clamav-milter to balance load between remote clamd daemons
        # on firewall servers you may need to tune the options below.

        # Close the connection when the data size limit is exceeded.
        # The value should match your MTA's limit for a maximum attachment size.
        # Default: 25M
        #StreamMaxLength 10M

        # Limit port range.
        # Default: 1024
        #StreamMinPort 30000
        # Default: 2048
        #StreamMaxPort 32000

        # Maximum number of threads running at the same time.
        # Default: 10
        #MaxThreads 20

        # Waiting for data from a client socket will timeout after this time (seconds).
        # Default: 120
        #ReadTimeout 300

        # This option specifies the time (in seconds) after which clamd should
        # timeout if a client doesn't provide any initial command after connecting.
        # Default: 5
        #CommandReadTimeout 5

        # This option specifies how long to wait (in miliseconds) if the send buffer is full.
        # Keep this value low to prevent clamd hanging
        #
        # Default: 500
        #SendBufTimeout 200

        # Maximum number of queued items (including those being processed by MaxThreads threads)
        # It is recommended to have this value at least twice MaxThreads if possible.
        # WARNING: you shouldn't increase this too much to avoid running out  of file descriptors,
        # the following condition should hold:
        # MaxThreads*MaxRecursion + (MaxQueue - MaxThreads) + 6< RLIMIT_NOFILE (usual max is 1024)
        #
        # Default: 100
        #MaxQueue 200

        # Waiting for a new job will timeout after this time (seconds).
        # Default: 30
        #IdleTimeout 60

        # Don't scan files and directories matching regex
        # This directive can be used multiple times
        # Default: scan all
        #ExcludePath ^/proc/
        #ExcludePath ^/sys/

        # Maximum depth directories are scanned at.
        # Default: 15
        #MaxDirectoryRecursion 20

        # Follow directory symlinks.
        # Default: no
        #FollowDirectorySymlinks yes

        # Follow regular file symlinks.
        # Default: no
        #FollowFileSymlinks yes

        # Scan files and directories on other filesystems.
        # Default: yes
        #CrossFilesystems yes

        # Perform a database check.
        # Default: 600 (10 min)
        #SelfCheck 600

        # Execute a command when virus is found. In the command string %v will
        # be replaced with the virus name.
        # Default: no
        #VirusEvent /usr/local/bin/send_sms 123456789 "VIRUS ALERT: %v"

        # Run as another user (clamd must be started by root for this option to work)
        # Default: don't drop privileges
        #User clamav

        # Initialize supplementary group access (clamd must be started by root).
        # Default: no
        #AllowSupplementaryGroups no

        # Stop daemon when libclamav reports out of memory condition.
        #ExitOnOOM yes

        # Don't fork into background.
        # Default: no
        #Foreground yes

        # Enable debug messages in libclamav.
        # Default: no
        #Debug yes

        # Do not remove temporary files (for debug purposes).
        # Default: no
        #LeaveTemporaryFiles yes

        # Permit use of the ALLMATCHSCAN command. If set to no, clamd will reject
        # any ALLMATCHSCAN command as invalid.
        # Default: yes
        #AllowAllMatchScan no

        # Detect Possibly Unwanted Applications.
        # Default: no
        #DetectPUA yes

        # Exclude a specific PUA category. This directive can be used multiple times.
        # See https://github.com/vrtadmin/clamav-faq/blob/master/faq/faq-pua.md for 
        # the complete list of PUA categories.
        # Default: Load all categories (if DetectPUA is activated)
        #ExcludePUA NetTool
        #ExcludePUA PWTool

        # Only include a specific PUA category. This directive can be used multiple
        # times.
        # Default: Load all categories (if DetectPUA is activated)
        #IncludePUA Spy
        #IncludePUA Scanner
        #IncludePUA RAT

        # In some cases (eg. complex malware, exploits in graphic files, and others),
        # ClamAV uses special algorithms to provide accurate detection. This option
        # controls the algorithmic detection.
        # Default: yes
        #AlgorithmicDetection yes

        # This option causes memory or nested map scans to dump the content to disk.
        # If you turn on this option, more data is written to disk and is available
        # when the LeaveTemporaryFiles option is enabled.
        #ForceToDisk yes

        # This option allows you to disable the caching feature of the engine. By
        # default, the engine will store an MD5 in a cache of any files that are
        # not flagged as virus or that hit limits checks. Disabling the cache will
        # have a negative performance impact on large scans.
        # Default: no
        #DisableCache yes

        ##
        ## Executable files
        ##

        # PE stands for Portable Executable - it's an executable file format used
        # in all 32 and 64-bit versions of Windows operating systems. This option allows
        # ClamAV to perform a deeper analysis of executable files and it's also
        # required for decompression of popular executable packers such as UPX, FSG,
        # and Petite. If you turn off this option, the original files will still be
        # scanned, but without additional processing.
        # Default: yes
        #ScanPE yes

        # Certain PE files contain an authenticode signature. By default, we check
        # the signature chain in the PE file against a database of trusted and
        # revoked certificates if the file being scanned is marked as a virus.
        # If any certificate in the chain validates against any trusted root, but
        # does not match any revoked certificate, the file is marked as whitelisted.
        # If the file does match a revoked certificate, the file is marked as virus.
        # The following setting completely turns off authenticode verification.
        # Default: no
        #DisableCertCheck yes

        # Executable and Linking Format is a standard format for UN*X executables.
        # This option allows you to control the scanning of ELF files.
        # If you turn off this option, the original files will still be scanned, but
        # without additional processing.
        # Default: yes
        #ScanELF yes

        # With this option clamav will try to detect broken executables (both PE and
        # ELF) and mark them as Broken.Executable.
        # Default: no
        #DetectBrokenExecutables yes


        ##
        ## Documents
        ##

        # This option enables scanning of OLE2 files, such as Microsoft Office
        # documents and .msi files.
        # If you turn off this option, the original files will still be scanned, but
        # without additional processing.
        # Default: yes
        #ScanOLE2 yes

        # With this option enabled OLE2 files with VBA macros, which were not
        # detected by signatures will be marked as "Heuristics.OLE2.ContainsMacros".
        # Default: no
        #OLE2BlockMacros no

        # This option enables scanning within PDF files.
        # If you turn off this option, the original files will still be scanned, but
        # without decoding and additional processing.
        # Default: yes
        #ScanPDF yes

        # This option enables scanning within SWF files.
        # If you turn off this option, the original files will still be scanned, but
        # without decoding and additional processing.
        # Default: yes
        #ScanSWF yes

        # This option enables scanning xml-based document files supported by libclamav.
        # If you turn off this option, the original files will still be scanned, but
        # without additional processing.
        # Default: yes
        #ScanXMLDOCS yes

        # This option enables scanning of HWP3 files.
        # If you turn off this option, the original files will still be scanned, but
        # without additional processing.
        # Default: yes
        #ScanHWP3 yes


        ##
        ## Mail files
        ##

        # Enable internal e-mail scanner.
        # If you turn off this option, the original files will still be scanned, but
        # without parsing individual messages/attachments.
        # Default: yes
        #ScanMail yes

        # Scan RFC1341 messages split over many emails.
        # You will need to periodically clean up $TemporaryDirectory/clamav-partial directory.
        # WARNING: This option may open your system to a DoS attack.
        #	   Never use it on loaded servers.
        # Default: no
        #ScanPartialMessages yes

        # With this option enabled ClamAV will try to detect phishing attempts by using
        # signatures.
        # Default: yes
        #PhishingSignatures yes

        # Scan URLs found in mails for phishing attempts using heuristics.
        # Default: yes
        #PhishingScanURLs yes

        # Always block SSL mismatches in URLs, even if the URL isn't in the database.
        # This can lead to false positives.
        #
        # Default: no
        #PhishingAlwaysBlockSSLMismatch no

        # Always block cloaked URLs, even if URL isn't in database.
        # This can lead to false positives.
        #
        # Default: no
        #PhishingAlwaysBlockCloak no

        # Detect partition intersections in raw disk images using heuristics.
        # Default: no
        #PartitionIntersection no

        # Allow heuristic match to take precedence.
        # When enabled, if a heuristic scan (such as phishingScan) detects
        # a possible virus/phish it will stop scan immediately. Recommended, saves CPU
        # scan-time.
        # When disabled, virus/phish detected by heuristic scans will be reported only at
        # the end of a scan. If an archive contains both a heuristically detected
        # virus/phish, and a real malware, the real malware will be reported
        #
        # Keep this disabled if you intend to handle "*.Heuristics.*" viruses 
        # differently from "real" malware.
        # If a non-heuristically-detected virus (signature-based) is found first, 
        # the scan is interrupted immediately, regardless of this config option.
        #
        # Default: no
        #HeuristicScanPrecedence yes


        ##
        ## Data Loss Prevention (DLP)
        ##

        # Enable the DLP module
        # Default: No
        #StructuredDataDetection yes

        # This option sets the lowest number of Credit Card numbers found in a file
        # to generate a detect.
        # Default: 3
        #StructuredMinCreditCardCount 5

        # This option sets the lowest number of Social Security Numbers found
        # in a file to generate a detect.
        # Default: 3
        #StructuredMinSSNCount 5

        # With this option enabled the DLP module will search for valid
        # SSNs formatted as xxx-yy-zzzz
        # Default: yes
        #StructuredSSNFormatNormal yes

        # With this option enabled the DLP module will search for valid
        # SSNs formatted as xxxyyzzzz
        # Default: no
        #StructuredSSNFormatStripped yes


        ##
        ## HTML
        ##

        # Perform HTML normalisation and decryption of MS Script Encoder code.
        # Default: yes
        # If you turn off this option, the original files will still be scanned, but
        # without additional processing.
        #ScanHTML yes


        ##
        ## Archives
        ##

        # ClamAV can scan within archives and compressed files.
        # If you turn off this option, the original files will still be scanned, but
        # without unpacking and additional processing.
        # Default: yes
        #ScanArchive yes

        # Mark encrypted archives as viruses (Encrypted.Zip, Encrypted.RAR).
        # Default: no
        #ArchiveBlockEncrypted no


        ##
        ## Limits
        ##

        # The options below protect your system against Denial of Service attacks
        # using archive bombs.

        # This option sets the maximum amount of data to be scanned for each input file.
        # Archives and other containers are recursively extracted and scanned up to this
        # value.
        # Value of 0 disables the limit
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 100M
        #MaxScanSize 150M

        # Files larger than this limit won't be scanned. Affects the input file itself
        # as well as files contained inside it (when the input file is an archive, a
        # document or some other kind of container).
        # Value of 0 disables the limit.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 25M
        #MaxFileSize 30M

        # Nested archives are scanned recursively, e.g. if a Zip archive contains a RAR
        # file, all files within it will also be scanned. This options specifies how
        # deeply the process should be continued.
        # Note: setting this limit too high may result in severe damage to the system.
        # Default: 16
        #MaxRecursion 10

        # Number of files to be scanned within an archive, a document, or any other
        # container file.
        # Value of 0 disables the limit.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 10000
        #MaxFiles 15000

        # Maximum size of a file to check for embedded PE. Files larger than this value
        # will skip the additional analysis step.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 10M
        #MaxEmbeddedPE 10M

        # Maximum size of a HTML file to normalize. HTML files larger than this value
        # will not be normalized or scanned.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 10M
        #MaxHTMLNormalize 10M

        # Maximum size of a normalized HTML file to scan. HTML files larger than this
        # value after normalization will not be scanned.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 2M
        #MaxHTMLNoTags 2M

        # Maximum size of a script file to normalize. Script content larger than this
        # value will not be normalized or scanned.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 5M
        #MaxScriptNormalize 5M

        # Maximum size of a ZIP file to reanalyze type recognition. ZIP files larger
        # than this value will skip the step to potentially reanalyze as PE.
        # Note: disabling this limit or setting it too high may result in severe damage
        # to the system.
        # Default: 1M
        #MaxZipTypeRcg 1M

        # This option sets the maximum number of partitions of a raw disk image to be scanned.
        # Raw disk images with more partitions than this value will have up to the value number
        # partitions scanned. Negative values are not allowed.
        # Note: setting this limit too high may result in severe damage or impact performance.
        # Default: 50
        #MaxPartitions 128

        # This option sets the maximum number of icons within a PE to be scanned.
        # PE files with more icons than this value will have up to the value number icons scanned.
        # Negative values are not allowed.
        # WARNING: setting this limit too high may result in severe damage or impact performance.
        # Default: 100
        #MaxIconsPE 200

        # This option sets the maximum recursive calls for HWP3 parsing during scanning.
        # HWP3 files using more than this limit will be terminated and alert the user.
        # Scans will be unable to scan any HWP3 attachments if the recursive limit is reached.
        # Negative values are not allowed.
        # WARNING: setting this limit too high may result in severe damage or impact performance.
        # Default: 16
        #MaxRecHWP3 16

        # This option sets the maximum calls to the PCRE match function during an instance of regex matching.
        # Instances using more than this limit will be terminated and alert the user but the scan will continue.
        # For more information on match_limit, see the PCRE documentation.
        # Negative values are not allowed.
        # WARNING: setting this limit too high may severely impact performance.
        # Default: 10000
        #PCREMatchLimit 20000

        # This option sets the maximum recursive calls to the PCRE match function during an instance of regex matching.
        # Instances using more than this limit will be terminated and alert the user but the scan will continue.
        # For more information on match_limit_recursion, see the PCRE documentation.
        # Negative values are not allowed and values > PCREMatchLimit are superfluous.
        # WARNING: setting this limit too high may severely impact performance.
        # Default: 5000
        #PCRERecMatchLimit 10000

        # This option sets the maximum filesize for which PCRE subsigs will be executed.
        # Files exceeding this limit will not have PCRE subsigs executed unless a subsig is encompassed to a smaller buffer.
        # Negative values are not allowed.
        # Setting this value to zero disables the limit.
        # WARNING: setting this limit too high or disabling it may severely impact performance.
        # Default: 25M
        #PCREMaxFileSize 100M


        ##
        ## On-access Scan Settings
        ##

        # Enable on-access scanning. Currently, this is supported via fanotify.
        # Clamuko/Dazuko support has been deprecated.
        # Default: no
        #ScanOnAccess yes

        # Set the  mount point to be scanned. The mount point specified, or the mount point 
        # containing the specified directory will be watched. If any directories are specified, 
        # this option will preempt the DDD system. This will notify only. It can be used multiple times.
        # (On-access scan only)
        # Default: disabled
        #OnAccessMountPath /
        #OnAccessMountPath /home/user

        # Don't scan files larger than OnAccessMaxFileSize
        # Value of 0 disables the limit.
        # Default: 5M
        #OnAccessMaxFileSize 10M

        # Set the include paths (all files inside them will be scanned). You can have
        # multiple OnAccessIncludePath directives but each directory must be added
        # in a separate line. (On-access scan only)
        # Default: disabled
        #OnAccessIncludePath /home
        #OnAccessIncludePath /students

        # Set the exclude paths. All subdirectories are also excluded.
        # (On-access scan only)
        # Default: disabled
        #OnAccessExcludePath /home/bofh

        # With this option you can whitelist specific UIDs. Processes with these UIDs
        # will be able to access all files.
        # This option can be used multiple times (one per line).
        # Default: disabled
        #OnAccessExcludeUID 0

        # Toggles dynamic directory determination. Allows for recursively watching include paths.
        # (On-access scan only)
        # Default: no
        #OnAccessDisableDDD yes

        # Modifies fanotify blocking behaviour when handling permission events.
        # If off, fanotify will only notify if the file scanned is a virus,
        # and not perform any blocking.
        # (On-access scan only)
        # Default: no
        #OnAccessPrevention yes

        # Toggles extra scanning and notifications when a file or directory is created or moved.
        # Requires the  DDD system to kick-off extra scans.
        # (On-access scan only)
        # Default: no
        #OnAccessExtraScanning yes

        ##
        ## Bytecode
        ##

        # With this option enabled ClamAV will load bytecode from the database. 
        # It is highly recommended you keep this option on, otherwise you'll miss detections for many new viruses.
        # Default: yes
        #Bytecode yes

        # Set bytecode security level.
        # Possible values:
        #       None - no security at all, meant for debugging. DO NOT USE THIS ON PRODUCTION SYSTEMS
        #         This value is only available if clamav was built with --enable-debug!
        #       TrustSigned - trust bytecode loaded from signed .c[lv]d files,
        #                insert runtime safety checks for bytecode loaded from other sources
        #       Paranoid - don't trust any bytecode, insert runtime checks for all
        # Recommended: TrustSigned, because bytecode in .cvd files already has these checks
        # Note that by default only signed bytecode is loaded, currently you can only
        # load unsigned bytecode in --enable-debug mode.
        #
        # Default: TrustSigned
        #BytecodeSecurity TrustSigned

        # Set bytecode timeout in miliseconds.
        # 
        # Default: 5000
        # BytecodeTimeout 1000

        ##
        ## Statistics gathering and submitting
        ##

        # Enable statistical reporting.
        # Default: no
        #StatsEnabled yes

        # Disable submission of individual PE sections for files flagged as malware.
        # Default: no
        #StatsPEDisabled yes

        # HostID in the form of an UUID to use when submitting statistical information.
        # Default: auto
        #StatsHostID auto

        # Time in seconds to wait for the stats server to come back with a response
        # Default: 10
        #StatsTimeout 10
    */
}