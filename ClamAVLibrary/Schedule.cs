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
        protected virtual void OnSocketReceived(ScheduleEventArgs e)
        {
            ScheduleReceived?.Invoke(this, e);
        }

        #endregion


        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /*
            <startdate format="dd-MMM-yyyy">10-feb-2019</startdate>
            <starttime format="HH:mm:ss">03:00:00</starttime>
            <schedule>day</schedule>
            <timeout>1</timeout>
         */

        #region Variables

        private DateTime _startDate = DateTime.Now;       //
        private TimeSpan _startTime = new TimeSpan();     //
        private long _timeout = 1;                        // Every day
        private TimeoutUnit _units = TimeoutUnit.day;     // Daily
        private int _checkInterval = 60;                  // Every minute 
        private string _dateFormat = "";
        private string _timeFormat = "";

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

        protected AutoResetEvent signal;
        protected object _threadLock = new object();
        protected Thread _thread;
        protected bool _disposed = false;
        protected ManualResetEvent _eventTerminate = new ManualResetEvent(false);
        protected bool _running = false;

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

        public string StartDate
        {
            get
            {
                return (_startDate.ToString());
            }
            set
            {
                if (_dateFormat.Length >0)
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
            Dispose(true);
            GC.SuppressFinalize(this);
            log.Debug("Out Dispose()");
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        private void Loop()
        {
            log.Debug("In Loop()");

            // process clamdscan at the defined intervals

            signal = new AutoResetEvent(false);
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
                    signal.WaitOne(sleepFor);    // Every Interval check
                    span = DateTime.Now.Subtract(start);
                }
                while ((((long)span.TotalSeconds < timeout) && (timeout > 0)) || (timeout == 0));

                // need to raise the event
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
