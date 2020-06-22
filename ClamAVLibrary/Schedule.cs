using log4net;
using System;
using System.Globalization;
using System.Threading;

namespace ClamAVLibrary
{
    public class Schedule : IDisposable
    {
        #region Event handling

        /// <summary>
        /// Occurs when the socket receives a message.
        /// </summary>
        public event EventHandler<ScheduleEventArgs> ScheduleReceived;

        /// <summary>
        /// Handles the actual event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnScheduleTimeout(ScheduleEventArgs e)
        {
            ScheduleReceived?.Invoke(this, e);
        }

        #endregion
        #region Variables
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /*
            <startdate format="dd-MMM-yyyy">10-feb-2019</startdate>
            <starttime format="HH:mm:ss">03:00:00</starttime>
            <schedule>day</schedule>
            <timeout>1</timeout>
         */

        private string _id = "";                            //
        private DateTime _startDate;                        //
        private TimeSpan _startTime;                        //
        private long _timeout = 1;                          // Every day
        private TimeoutUnit _units = TimeoutUnit.day;       // Daily
        private int _checkInterval = 60;                    // Every minute 
        private string _dateFormat = "";                    //
        private string _timeFormat = "";                    //

        public enum TimeoutUnit : int
        {
            second = 0,
            minute = 1,
            hour = 2,
            day = 3,
            week = 4,
            month = 5,
            year = 6
        }

        protected AutoResetEvent _signal;
        protected object _threadLock = new object();
        protected Thread _thread;
        protected bool _disposed = false;
        protected ManualResetEvent _eventTerminate = new ManualResetEvent(false);
        protected bool _running = false;

        #endregion
        #region Constructors

        public Schedule()
        {
            log.Debug("In Schedule()");
            _startDate = new DateTime();       //
            _startTime = new TimeSpan();       //
            log.Debug("Out Schedule()");
        }

        #endregion
        #region Properties

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

        public string DateFormat
        {
            get
            {
                return (_dateFormat);
            }
            set
            {
                _dateFormat = value;
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

        public int Interval
        {
            get
            {
                return (_checkInterval);
            }
            set
            {
                _checkInterval = value;
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
                if (_dateFormat.Length > 0)
                {
                    CultureInfo provider = CultureInfo.InvariantCulture;
                    if (!DateTime.TryParseExact(value, _dateFormat, provider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out _startDate))
                    {
                        throw new FormatException("Invalid date format");
                    }
                }
                else
                {
                    CultureInfo provider = CultureInfo.InvariantCulture;
                    if (!DateTime.TryParse(value, provider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out _startDate))
                    {
                        throw new FormatException("Invalid date format");
                    }
                }
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
                CultureInfo provider = CultureInfo.InvariantCulture;
                DateTime dateTime;
                if (_timeFormat.Length > 0)
                {
                    if (!DateTime.TryParseExact(value, _timeFormat, provider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dateTime))
                    {
                        throw new FormatException("Invalid time format");
                    }
                }
                else
                {
                    if (!DateTime.TryParse(value, provider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out dateTime))
                    {
                        throw new FormatException("Invalid time format");
                    }
                }
                _startTime = dateTime.TimeOfDay;
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

        public string TimeFormat
        {
            get
            {
                return (_dateFormat);
            }
            set
            {
                _dateFormat = value;
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

        //public string TimeoutUnits
        //{
        //    get
        //    {
        //        return (_units.ToString());
        //    }
        //    set
        //    {
        //        _units = UnitLookup( value);
        //    }
        //}

        public TimeoutUnit Units
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

        #endregion
        #region Methods

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
                if (!_running)
                {
                    _eventTerminate.Reset();
                    _thread = new Thread(new ThreadStart(Loop))
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
            
            if (_signal != null)
            {
                _signal.Set();   // force out of the waitOne
            }
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

        protected virtual void Dispose(bool disposing)
        {
            log.Debug("In Dispose()");
            if (!_disposed)
            {
                if (disposing == true)
                {
                    Stop();
                }
                _disposed = true;
            }
            log.Debug("Out Dispose()");
        }

        public static TimeoutUnit UnitLookup(string unitName)
        {
            TimeoutUnit timeoutUnit = TimeoutUnit.day;

            if (Int32.TryParse(unitName, out int unitValue))
            {
                timeoutUnit = (TimeoutUnit)unitValue;
            }
            else
            {
                string lookup = unitName;
                if (unitName.Length > 1)
                {
                    lookup = unitName.ToUpper();
                }

                switch (lookup)
                {
                    case "M":
                    case "MINUTE":
                        {
                            timeoutUnit = TimeoutUnit.minute;
                            break;
                        }
                    case "H":
                    case "HOUR":
                        {
                            timeoutUnit = TimeoutUnit.hour;
                            break;
                        }
                    case "D":
                    case "DAY":
                        {
                            timeoutUnit = TimeoutUnit.day;
                            break;
                        }
                    case "W":
                    case "WEEK":
                        {
                            timeoutUnit = TimeoutUnit.week;
                            break;
                        }
                    case "m":
                    case "MONTH":
                        {
                            timeoutUnit = TimeoutUnit.month;
                            break;
                        }
                    case "Y":
                    case "YEAR":
                        {
                            timeoutUnit = TimeoutUnit.year;
                            break;
                        }
                }
            }
            return (timeoutUnit);
        }

        #endregion
        #region Private

        /// <summary>
        /// 
        /// </summary>
        private void Loop()
        {
            log.Debug("In Loop()");

            log.Info("[" + _id + "] schedule at " + _timeout + " " + _units.ToString() + " interval starting on " + _startDate.ToString("dd/MM/yyyy") + " " + _startTime.ToString());

            // process clamdscan at the defined intervals

            _signal = new AutoResetEvent(false);
            _running = true;

            DateTime start = DateTime.Now;    // Set the start timer
            long timeout = 0;
            DateTime startDateTime = new DateTime(_startDate.Year, _startDate.Month, _startDate.Day, _startTime.Hours, _startTime.Minutes, _startTime.Seconds);
            int sleepFor = _checkInterval * 1000; // need to convert to milliseconds
            do
            {
                DateTime now = DateTime.Now;
                TimeSpan span = now.Subtract(startDateTime);
                long elapsed = (long)span.TotalSeconds;

                // options here to either trigger at startDateTime or startDateTime + timeout

                if (elapsed < 0)
                {
                    // Schedule in the future
                    timeout = -elapsed;
                }
                else
                {
                    timeout = TimeConvert(_units, _timeout);
                }

                start = startDateTime.AddSeconds(timeout * (int)(elapsed / timeout));   // Calculate the new start

                do
                {
                    _signal.WaitOne(sleepFor);    // Every Interval check
                    span = DateTime.Now.Subtract(start);
                }
                while (((((long)span.TotalSeconds < timeout) && (timeout > 0)) || (timeout == 0)) && (_running == true));

                // need to raise the event

                if (_running == true)
                {
                    log.Info("[" + _id + "] timeout");
                    ScheduleEventArgs args = new ScheduleEventArgs(DateTime.Now);
                    OnScheduleTimeout(args);
                }
            }
            while (_running == true);

            log.Debug("Out Loop()");
        }

        private static long TimeConvert(TimeoutUnit schedule, long timeout)
        {
            log.Debug("In TimeConvert()");
            long seconds = timeout;

            // convert to seconds

            switch (schedule)
            {
                case TimeoutUnit.minute:
                    {
                        seconds = timeout * 60;
                    }
                    break;
                case TimeoutUnit.hour:
                    {
                        seconds = timeout * 3600;
                    }
                    break;
                case TimeoutUnit.day:
                    {
                        seconds = timeout * 24 * 3600;
                    }
                    break;
                case TimeoutUnit.week:
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
