using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using log4net;

namespace ClamAVLibrary
{
    public class FreshClam
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
        public FreshClam(Location location)
        {
            string path = "";
            _startDate = new DateTime();
            _startTime = new TimeSpan(_startDate.Hour, _startDate.Minute, _startDate.Second);

            switch (location)
            {
                case Location.App:
                    {
                        path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        break;
                    }
                case Location.Program:
                    {
                        path = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        int pos = path.LastIndexOf('\\');
                        path = path.Substring(0, pos);
                        break;
                    }
                case Location.Local:
                    {
                        path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        break;
                    }
                case Location.Roaming:
                    {
                        path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                        break;
                    }
            }
            _databasePath = path + Path.DirectorySeparatorChar + "database";
            if (!Directory.Exists(_databasePath))
            {
                Directory.CreateDirectory(_databasePath);
            }
            _logPath = path + Path.DirectorySeparatorChar + "logs";
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
            _logFilenamePath = _logPath + Path.DirectorySeparatorChar + "freshclam.log";
            _configFilenamePath = path + Path.DirectorySeparatorChar + "freshclam.conf";

            _settings = new List<Setting>();
            _settings.Add(new Setting("AllowSupplementaryGroups", null));
            _settings.Add(new Setting("Bytecode", null));
            _settings.Add(new Setting("Checks", null));
            _settings.Add(new Setting("CompressLocalDatabase", null));
            _settings.Add(new Setting("ConnectTimeout", null));
            _settings.Add(new Setting("DNSDatabaseInfo", null));
            _settings.Add(new Setting("DatabaseCustomURL", null));
            _settings.Add(new Setting("DatabaseDirectory", _databasePath));
            _settings.Add(new Setting("DatabaseMirror", "database.clamav.net"));
            _settings.Add(new Setting("DatabaseOwner", null));
            _settings.Add(new Setting("Debug", null));
            _settings.Add(new Setting("DetectionStatsCountry", null));
            _settings.Add(new Setting("DetectionStatsHostID", null));
            _settings.Add(new Setting("ExtraDatabase", null));
            _settings.Add(new Setting("Foreground", null));
            _settings.Add(new Setting("HTTPProxyPassword", null));
            _settings.Add(new Setting("HTTPProxyPort", null));
            _settings.Add(new Setting("HTTPProxyServer", null));
            _settings.Add(new Setting("HTTPProxyUsername", null));
            _settings.Add(new Setting("HTTPUserAgent", null));
            _settings.Add(new Setting("LocalIPAddress", null));
            _settings.Add(new Setting("LogFacility", null));
            _settings.Add(new Setting("LogFileMaxSize", null));
            _settings.Add(new Setting("LogRotate", null));
            _settings.Add(new Setting("LogSyslog", null));
            _settings.Add(new Setting("LogTime", null));
            _settings.Add(new Setting("MaxAttempts", null));
            _settings.Add(new Setting("NotifyClamd", null));
            _settings.Add(new Setting("OnErrorExecute", null));
            _settings.Add(new Setting("OnOutdatedExecute", null));
            _settings.Add(new Setting("OnUpdateExecute", @"C:\Windows\System32\calc.exe"));
            _settings.Add(new Setting("PidFile", null));
            _settings.Add(new Setting("PrivateMirror", null));
            _settings.Add(new Setting("ReceiveTimeout", null));
            _settings.Add(new Setting("SafeBrowsing", null));
            _settings.Add(new Setting("ScriptedUpdates", null));
            _settings.Add(new Setting("SubmitDetectionStats", null));
            _settings.Add(new Setting("TestDatabases", null));
            _settings.Add(new Setting("UpdateLogFile", _logFilenamePath));
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
        #region Private

        /// <summary>
        /// 
        /// </summary>
        private void MonitorThread()
        {
            log.Debug("In MonitorThread()");

            try
            {
                FreshClamLoop();
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
        private void FreshClamLoop()
        {
            log.Debug("In FreshClamLoop()");

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
                    LaunchFreshClam();
                    _downloading = false;
                }
            }
            while (_running == true);

            log.Debug("Out FreshClamLoop()");
        }

        private void LaunchFreshClam()
        {
            log.Debug("In LaunchFreshClam()");

            _downloading = true;

            Process proc = new System.Diagnostics.Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = "freshclam.exe";   // Assume that clamAV is installed in the same location
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
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

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
                      Clam AntiVirus: Database Updater 0.100.2
           By The ClamAV Team: https://www.clamav.net/about.html#credits
           (C) 2007-2018 Cisco Systems, Inc.

    freshclam [options]

    --help               -h              Show this help
    --version            -V              Print version number and exit
    --verbose            -v              Be verbose
    --debug                              Enable debug messages
    --quiet                              Only output error messages
    --no-warnings                        Don't print and log warnings
    --stdout                             Write to stdout instead of stderr
    --show-progress                      Show download progress percentage
    --config-file=FILE                   Read configuration from FILE.
    --log=FILE           -l FILE         Log into FILE
    --no-dns                             Force old non-DNS verification method
    --checks=#n          -c #n           Number of checks per day, 1 <= n <= 50
    --datadir=DIRECTORY                  Download new databases into DIRECTORY
    --daemon-notify[=/path/clamd.conf]   Send RELOAD command to clamd
    --local-address=IP   -a IP           Bind to IP for HTTP downloads
    --on-update-execute=COMMAND          Execute COMMAND after successful update
    --on-error-execute=COMMAND           Execute COMMAND if errors occurred
    --on-outdated-execute=COMMAND        Execute COMMAND when software is outdated
    --list-mirrors                       Print mirrors from mirrors.dat
    --update-db=DBNAME                   Only update database DBNAME
    */

    /*
     * freshclam.conf
     * 
        # Path to the database directory.
        # WARNING: It must match clamd.conf's directive!
        # Default: hardcoded (depends on installation options)
        DatabaseDirectory C:\Program files\ClamAV\database

        # Path to the log file (make sure it has proper permissions)
        # Default: disabled
        UpdateLogFile  C:\program files\ClamAV\logs\freshclam.log

        # Maximum size of the log file.
        # Value of 0 disables the limit.
        # You may use 'M' or 'm' for megabytes (1M = 1m = 1048576 bytes)
        # and 'K' or 'k' for kilobytes (1K = 1k = 1024 bytes).
        # in bytes just don't use modifiers. If LogFileMaxSize is enabled,
        # log rotation (the LogRotate option) will always be enabled.
        # Default: 1M
        LogFileMaxSize 20480000

        # Log time with each message.
        # Default: no
        LogTime yes

        # Enable verbose logging.
        # Default: no
        #LogVerbose yes

        # Use system logger (can work together with UpdateLogFile).
        # Default: no
        #LogSyslog yes

        # Specify the type of syslog messages - please refer to 'man syslog'
        # for facility names.
        # Default: LOG_LOCAL6
        #LogFacility LOG_MAIL

        # Enable log rotation. Always enabled when LogFileMaxSize is enabled.
        # Default: no
        #LogRotate yes

        # This option allows you to save the process identifier of the daemon
        # Default: disabled
        #PidFile /var/run/freshclam.pid

        # By default when started freshclam drops privileges and switches to the
        # "clamav" user. This directive allows you to change the database owner.
        # Default: clamav (may depend on installation options)
        #DatabaseOwner clamav

        # Initialize supplementary group access (freshclam must be started by root).
        # Default: no
        #AllowSupplementaryGroups yes

        # Use DNS to verify virus database version. Freshclam uses DNS TXT records
        # to verify database and software versions. With this directive you can change
        # the database verification domain.
        # WARNING: Do not touch it unless you're configuring freshclam to use your
        # own database verification domain.
        # Default: current.cvd.clamav.net
        #DNSDatabaseInfo current.cvd.clamav.net

        # Uncomment the following line and replace XY with your country
        # code. See http://www.iana.org/cctld/cctld-whois.htm for the full list.
        # You can use db.XY.ipv6.clamav.net for IPv6 connections.
        #DatabaseMirror db.XY.clamav.net

        # database.clamav.net is a round-robin record which points to our most 
        # reliable mirrors. It's used as a fall back in case db.XY.clamav.net is 
        # not working. DO NOT TOUCH the following line unless you know what you
        # are doing.
        DatabaseMirror database.clamav.net

        # How many attempts to make before giving up.
        # Default: 3 (per mirror)
        MaxAttempts 3

        # With this option you can control scripted updates. It's highly recommended
        # to keep it enabled.
        # Default: yes
        #ScriptedUpdates yes

        # By default freshclam will keep the local databases (.cld) uncompressed to
        # make their handling faster. With this option you can enable the compression;
        # the change will take effect with the next database update.
        # Default: no
        #CompressLocalDatabase no

        # With this option you can provide custom sources (http:// or file://) for
        # database files. This option can be used multiple times.
        # Default: no custom URLs
        #DatabaseCustomURL http://myserver.com/mysigs.ndb
        #DatabaseCustomURL file:///mnt/nfs/local.hdb

        # This option allows you to easily point freshclam to private mirrors.
        # If PrivateMirror is set, freshclam does not attempt to use DNS
        # to determine whether its databases are out-of-date, instead it will
        # use the If-Modified-Since request or directly check the headers of the
        # remote database files. For each database, freshclam first attempts
        # to download the CLD file. If that fails, it tries to download the
        # CVD file. This option overrides DatabaseMirror, DNSDatabaseInfo
        # and ScriptedUpdates. It can be used multiple times to provide
        # fall-back mirrors.
        # Default: disabled
        #PrivateMirror mirror1.mynetwork.com
        #PrivateMirror mirror2.mynetwork.com

        # Number of database checks per day.
        # Default: 12 (every two hours)
        #Checks 24

        # Proxy settings
        # Default: disabled
        #HTTPProxyServer myproxy.com
        #HTTPProxyPort 1234
        #HTTPProxyUsername myusername
        #HTTPProxyPassword mypass

        # If your servers are behind a firewall/proxy which applies User-Agent
        # filtering you can use this option to force the use of a different
        # User-Agent header.
        # Default: clamav/version_number
        #HTTPUserAgent SomeUserAgentIdString

        # Use aaa.bbb.ccc.ddd as client address for downloading databases. Useful for
        # multi-homed systems.
        # Default: Use OS'es default outgoing IP address.
        #LocalIPAddress aaa.bbb.ccc.ddd

        # Send the RELOAD command to clamd.
        # Default: no
        NotifyClamd C:\Program files\ClamAV\clamd.conf

        # Run command after successful database update.
        # Default: disabled
        #OnUpdateExecute command

        # Run command when database update process fails.
        # Default: disabled
        #OnErrorExecute command

        # Run command when freshclam reports outdated version.
        # In the command string %v will be replaced by the new version number.
        # Default: disabled
        #OnOutdatedExecute command

        # Don't fork into background.
        # Default: no
        #Foreground yes

        # Enable debug messages in libclamav.
        # Default: no
        #Debug yes

        # Timeout in seconds when connecting to database server.
        # Default: 30
        #ConnectTimeout 60

        # Timeout in seconds when reading from database server.
        # Default: 30
        #ReceiveTimeout 60

        # With this option enabled, freshclam will attempt to load new
        # databases into memory to make sure they are properly handled
        # by libclamav before replacing the old ones.
        # Default: yes
        #TestDatabases yes

        # When enabled freshclam will submit statistics to the ClamAV Project about
        # the latest virus detections in your environment. The ClamAV maintainers
        # will then use this data to determine what types of malware are the most
        # detected in the field and in what geographic area they are.
        # Freshclam will connect to clamd in order to get recent statistics.
        # Default: no
        #SubmitDetectionStats /path/to/clamd.conf

        # Country of origin of malware/detection statistics (for statistical
        # purposes only). The statistics collector at ClamAV.net will look up
        # your IP address to determine the geographical origin of the malware
        # reported by your installation. If this installation is mainly used to
        # scan data which comes from a different location, please enable this
        # option and enter a two-letter code (see http://www.iana.org/domains/root/db/)
        # of the country of origin.
        # Default: disabled
        #DetectionStatsCountry country-code

        # This option enables support for our "Personal Statistics" service. 
        # When this option is enabled, the information on malware detected by
        # your clamd installation is made available to you through our website.
        # To get your HostID, log on http://www.stats.clamav.net and add a new
        # host to your host list. Once you have the HostID, uncomment this option
        # and paste the HostID here. As soon as your freshclam starts submitting
        # information to our stats collecting service, you will be able to view
        # the statistics of this clamd installation by logging into
        # http://www.stats.clamav.net with the same credentials you used to
        # generate the HostID. For more information refer to:
        # http://www.clamav.net/documentation.html#cctts 
        # This feature requires SubmitDetectionStats to be enabled.
        # Default: disabled
        #DetectionStatsHostID unique-id

        # This option enables support for Google Safe Browsing. When activated for
        # the first time, freshclam will download a new database file (safebrowsing.cvd)
        # which will be automatically loaded by clamd and clamscan during the next
        # reload, provided that the heuristic phishing detection is turned on. This
        # database includes information about websites that may be phishing sites or
        # possible sources of malware. When using this option, it's mandatory to run
        # freshclam at least every 30 minutes.
        # Freshclam uses the ClamAV's mirror infrastructure to distribute the
        # database and its updates but all the contents are provided under Google's
        # terms of use. See http://www.google.com/transparencyreport/safebrowsing
        # and http://www.clamav.net/documentation.html#safebrowsing 
        # for more information.
        # Default: disabled
        #SafeBrowsing yes

        # This option enables downloading of bytecode.cvd, which includes additional
        # detection mechanisms and improvements to the ClamAV engine.
        # Default: enabled
        #Bytecode yes

        # Download an additional 3rd party signature database distributed through
        # the ClamAV mirrors. 
        # This option can be used multiple times.
        #ExtraDatabase dbname1
        #ExtraDatabase dbname2
    */


}
