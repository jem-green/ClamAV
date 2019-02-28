using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using log4net;

namespace ClamAVLibrary
{
    public class Component
    {
        #region Event handling

        /// <summary>
        /// Occurs when the socket receives a message.
        /// </summary>
        public event EventHandler<NotificationEventArgs> SocketReceived;

        /// <summary>
        /// Handles the actual event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSocketReceived(NotificationEventArgs e)
        {
            EventHandler<NotificationEventArgs> handler = SocketReceived;
            if (handler != null)
                handler(this, e);
        }

        #endregion

        #region Variables

        
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected string _id = "";
        protected AutoResetEvent signal;
        protected object _threadLock = new object();
        protected Thread _thread;
        protected bool _disposed = false;
        protected ManualResetEvent _eventTerminate = new ManualResetEvent(false);
        protected bool _running = false;
        protected bool _downloading = false;
        protected string _execute = "";

        protected List<Setting> _settings = null;
        protected List<Option> _options = null;

        protected string _databasePath = "";
        protected string _logPath = "";
        protected string _logFilenamePath = "";
        protected string _configFilenamePath = "";
        protected OperatingMode _mode = OperatingMode.combined;
        protected DataLocation _location = DataLocation.app;

        protected Schedule _schedule;
        protected bool _background = false;           // Run in the foreground
        protected string _path = "";
        protected int _port = 3310;

        public struct Setting
        {
            public enum ConfigFormat
            {
                none = -1,
                key = 1,
                text = 2,
                number = 3,
                value = 4,
                yesno = 5,
                truefalse = 6
            }

            string _key;
            object _value;
            ConfigFormat _format;

            public Setting(string key)
            {
                _key = key;
                _value = null;
                _format = ConfigFormat.none;
            }

            public Setting(string key, object value)
            {
                _key = key;
                _value = value;
                _format = ConfigFormat.none;
            }

            public Setting(string key, object value, ConfigFormat format)
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

            public ConfigFormat Format
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

        public struct Option
        {

            public enum ConfigFormat
            {
                none = -1,
                key = 1,
                text = 2,
                number = 3,
                value = 4,
                yesno = 5,
                truefalse = 6
            }

            string _key;
            object _value;
            ConfigFormat _format;

            public Option(string key)
            {
                _key = key;
                _value = null;
                _format = ConfigFormat.none;
            }

            public Option(string key, object value)
            {
                _key = key;
                _value = value;
                _format = ConfigFormat.none;
            }

            public Option(string key, object value, ConfigFormat format)
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

            public ConfigFormat Format
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

        public enum OperatingMode : int
        {
            none = -1,
            client = 1,
            server = 2,
            combined = 3
        }

        public enum DataLocation : int
        {
            program = 0,
            app = 1,
            local = 2,
            roaming = 3
        }

        #endregion
        #region Constructors

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

        public string Id
        {
            get
            {
                return (_id);
            }
            set
            {
                _id = value;
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

        public DataLocation Location
        {
            get
            {
                return (_location);
            }
            set
            {
                _location = value;
            }
        }

        public Schedule Schedule
        {
            get
            {
                return (_schedule);
            }
            set
            {
                _schedule = value;
            }
        }

        public OperatingMode Mode
        {
            get
            {
                return (_mode);
            }
            set
            {
                _mode = value;
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

        #endregion
        #region Methods

        public bool Update(object configuration)
        {
            log.Debug("In Update()");
            bool update = false;
            try
            {
                if (configuration.GetType() == typeof(Setting))
                {
                    Setting setting = (Setting)configuration;
                    for (int i = 0; i < _settings.Count; i++)
                    {
                        if (_settings[i].Key == setting.Key)
                        {
                            _settings[i] = setting;
                            log.Info("Update setting:" + setting.Key + "=" + setting);
                        }

                    }
                }
                else
                {
                    Option option = (Option)configuration;
                    for (int i = 0; i < _options.Count; i++)
                    {
                        if (_options[i].Key == option.Key)
                        {
                            _options[i] = option;
                            log.Info("Update option:" + option.Key + "=" + option);
                        }
                    }
                }
                update = true;
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            log.Debug("Out Update()");
            return (update);
        }

        public bool Add(object configuration)
        {
            log.Debug("In Add()");
            bool add = false;
            try
            {
                if (configuration.GetType() == typeof(Setting))
                {
                    Setting setting = (Setting)configuration;
                    _settings.Add(setting);
                    log.Info("Update setting:" + setting.Key + "=" + setting);
                    add = true;
                }
                else
                {
                    Option option = (Option)configuration;
                    _options.Add(option);
                    log.Info("Update setting:" + option.Key + "=" + option);
                    add = true;
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            log.Debug("Out Add()");
            return (add);
        }

        public bool Remove(object configuration)
        {
            log.Debug("In Remove()");
            bool remove = false;
            try
            {
                if (configuration.GetType() == typeof(Setting))
                {
                    _settings.Remove((Setting)configuration);
                    remove = true;
                }
                else
                {
                    _options.Remove((Option)configuration);
                    remove = true;
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
            log.Debug("Out Remove()");
            return (remove);
        }

        public string BuildOptions()
        {
            log.Debug("In BuildOptions()");

            StringWriter options = new StringWriter();
            foreach (Option option in _options)
            {
                if (option.Value != null)
                {
                    options.Write(" --" + option.Key);
                    if (option.Format != Option.ConfigFormat.none)
                    {
                        switch (option.Format)
                        {
                            case Option.ConfigFormat.key:
                                {
                                    break;
                                }
                            case Option.ConfigFormat.value:
                                {
                                    options.Write("=" + option.Value);
                                    break;
                                }
                            case Option.ConfigFormat.number:
                                {
                                    options.Write("=" + option.Value);
                                    break;
                                }
                            case Option.ConfigFormat.text:
                                {
                                    options.Write("=\"" + option.Value + "\"");
                                    break;
                                }
                            case Option.ConfigFormat.yesno:
                                {
                                    if (Convert.ToBoolean(option.Value) == true)
                                    {
                                        options.Write("=yes");
                                    }
                                    else
                                    {
                                        options.Write("=no");
                                    }
                                    break;
                                }
                            case Option.ConfigFormat.truefalse:
                                {
                                    if (Convert.ToBoolean(option.Value) == true)
                                    {
                                        options.Write("=true");
                                    }
                                    else
                                    {
                                        options.Write("=false");
                                    }
                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (option.Value.GetType() == typeof(string))
                        {
                            options.Write("=\"" + option.Value + "\"");
                        }
                        else if (option.Value.GetType() == typeof(bool))
                        {
                            options.Write("=" + option.Value);
                        }
                        else
                        {
                            options.Write("=" + option.Value);
                        }
                    }
                }
            }

            if (_path.Length > 0)
            {
                options.Write(" " + _path);
            }

            log.Debug("Out BuildOptions()");
            return (options.ToString());
        }

        public bool WriteConfig()
        {
            return (WriteConfig(true));
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
                    if (setting.Format != Setting.ConfigFormat.none)
                    {
                        switch (setting.Format)
                        {
                            case Setting.ConfigFormat.value:
                                {
                                    config.WriteLine(setting.Value);
                                    break;
                                }
                            case Setting.ConfigFormat.number:
                                {
                                    config.WriteLine(setting.Value);
                                    break;
                                }
                            case Setting.ConfigFormat.text:
                                {
                                    config.WriteLine("\"" + setting.Value + "\"");
                                    break;
                                }
                            case Setting.ConfigFormat.yesno:
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
            log.Info("[" + _id + "] start");

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
            log.Info("[" + _id + "] stop");

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        protected virtual void Dispose(bool disposing)
        {
            log.Debug("In Dispose()");
            if (!_disposed)
            {
                if (disposing == true)
                {
                    if (_schedule != null)
                    {
                        _schedule.Dispose();
                        Stop();
                    }
                }
                _disposed = true;
            }
            log.Debug("Out Dispose()");
        }

        #region Events

        public virtual void OutputReceived(object sendingProcess, DataReceivedEventArgs outputData)
        {
            if ((outputData != null) && (outputData.Data != null))
            {
                if (outputData.Data.Trim() != "")
                {
                    log.Debug("[" + _id + "] Output =" + outputData.Data);
                }
            }
        }

        public virtual void ErrorReceived(object sendingProcess, DataReceivedEventArgs errorData)
        {
            if ((errorData != null) && (errorData.Data != null))
            {
                if (errorData.Data.Trim() != "")
                {
                    log.Debug("[" + _id + "] Error=" + errorData.Data);
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

            log.Info("[" + Id + "] monitoring");

            try
            {
                if (_background == true)
                {
                    Launch();
                }
                else
                {
                    try
                    {
                        _schedule.ScheduleReceived += new EventHandler<ScheduleEventArgs>(OnTimeoutReceived);
                        _schedule.Start();
                    }
                    catch (Exception e)
                    {
                        log.Debug(e.ToString());
                    }

                    //Loop();
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
        /// 
        /// </summary>
        //private void Loop()
        //{
        //    log.Debug("In Loop()");

        //    //

        //    signal = new AutoResetEvent(false);
        //    _running = true;

        //    DateTime start = DateTime.Now;    // Set the start timer
        //    long timeout = 0;
        //    DateTime startDateTime = new DateTime(_schedule.Date.Year, _schedule.Date.Month, _schedule.Date.Day, _schedule.Time.Hours, _schedule.Time.Minutes, _schedule.Time.Seconds);
        //    int sleepFor = _schedule.Interval * 1000; // need to convert to milliseconds
        //    do
        //    {
        //        DateTime now = DateTime.Now;
        //        TimeSpan span = now.Subtract(startDateTime);
        //        long elapsed = (long)span.TotalSeconds;

        //        // options here to either trigger at startDateTime or startDateTime + timeout

        //        if (elapsed < 0)
        //        {
        //            // Schedule in the future
        //            timeout = -elapsed;
        //        }
        //        else
        //        {
        //            timeout = TimeConvert(_schedule.Units, _schedule.Timeout);
        //        }

        //        start = startDateTime.AddSeconds(timeout * (int)(elapsed / timeout));   // Calculate the new start

        //        do
        //        {
        //            signal.WaitOne(sleepFor);    // Every Interval check
        //            span = DateTime.Now.Subtract(start);
        //            log.Debug("Checking");
        //        }
        //        while ((((long)span.TotalSeconds < timeout) && (timeout > 0)) || (timeout == 0));

        //        if (_downloading == false)
        //        {
        //            Launch();
        //        }
        //    }
        //    while (_running == true);

        //    log.Debug("Out Loop()");
        //}

        private void Launch()
        {
            log.Debug("In Launch()");

            _downloading = true;

            Process proc = new System.Diagnostics.Process();

            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.FileName = _execute;   // Assume that clamAV is installed in the same location
            startInfo.Arguments = BuildOptions();
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
                log.Info("[" + _id + "] Start " + _execute + startInfo.Arguments);
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                log.Info("[" + _id + "] Finished ");
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            _downloading = false;

            log.Debug("Out Launch()");
        }

        // Define the event handlers.
        private void OnTimeoutReceived(object source, ScheduleEventArgs e)
        {
            Launch();
        }

        long TimeConvert(Schedule.TimeoutUnit schedule, long timeout)
        {
            log.Debug("In TimeConvert()");
            long seconds = timeout;

            // convert to seconds

            switch (schedule)
            {
                case Schedule.TimeoutUnit.minute:
                    {
                        seconds = timeout * 60;
                    }
                    break;
                case Schedule.TimeoutUnit.hour:
                    {
                        seconds = timeout * 3600;
                    }
                    break;
                case Schedule.TimeoutUnit.day:
                    {
                        seconds = timeout * 24 * 3600;
                    }
                    break;
                case Schedule.TimeoutUnit.week:
                    {
                        seconds = timeout * 7 * 24 * 3600;
                    }
                    break;
            }
            log.Debug("Out TimeConvert()");
            return (seconds);
        }

        #endregion
    }
}


