using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace ClamAVLibrary
{
    public class ClamAV : IDisposable
    {
        #region Variables
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        AutoResetEvent monitorSignal;
        Queue _messageQueue;
        private object _threadLock = new object();
        private Thread _monitoringThread;

        private bool _disposed = false;
        private ManualResetEvent monitorEventTerminate = new ManualResetEvent(false);
        bool monitorRunning = true;	
        private int monitorInterval = 60000;
		
        private OperatingMode _mode = OperatingMode.combined;
        private DataLocation _logLocation = DataLocation.app;
        private DataLocation _databaseLocation = DataLocation.app;
        private DataLocation _serverLocation = DataLocation.app;

        private Schedule _update;
        private List<Schedule> _scans;
        Dictionary<string, Forwarder> _forwarders = null;

        public enum DataLocation : int
        {
            program = 0,
            app = 1,
            local = 2,
            roaming = 3
        }

        public enum OperatingMode : int
        {
            none = -1,
            client = 1,
            server = 2,
            combined = 3
        }

        #endregion

        #region Constructors
		
		public ClamAV()
        {
            log.Debug("in ClamAV()");
        	_update = new Schedule();
        	_scans = new List<Schedule>();
			_messageQueue = new Queue();
            _forwarders = new Dictionary<string, Forwarder>();
            log.Debug("Out ClamAV()");
        }	
		
        #endregion

        #region Properties

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

        public DataLocation Log
        {
            get
            {
                return (_logLocation);
            }
            set
            {
                _logLocation = value;
            }
        }

        public DataLocation Database
        {
            get
            {
                return (_databaseLocation);
            }
            set
            {
                _databaseLocation = value;
            }
        }

        public Schedule Update
        {
            get
            {
                return (_update);
            }
            set
            {
                _update = value;
            }
        }

        public List<Schedule> Scans
        {
            get
            {
                return (_scans);
            }
            set
            {
                _scans = value;
            }
        }
		
        #endregion
        #region Methods

        public void Add(Forwarder forwarder)
        {
            try
            {
                _forwarders.Add(forwarder.Id, forwarder);
            }
            catch (ArgumentException)
            {
                log.Debug("Duplicate entry " + forwarder.Id);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }
        }

        public void Remove(Forwarder forwarder)
        {
            _forwarders.Remove(forwarder.Id);
        }

        public void Monitor()
        {
            log.Debug("In Monitor()");

            // Launch the scanners and updators
            // Server = clamd + freshclam
            // Combined = freshclam + clamscan
            // Client = clamdscan

            if (_mode == OperatingMode.server)
            {
                Clamd server = new Clamd(_databaseLocation);
                server.IsBackground = true;
                server.Start();
            }

            if ((_mode == OperatingMode.combined) || (_mode == OperatingMode.server))
            {
                FreshClam freshClam = new FreshClam(_databaseLocation);
                freshClam.Schedule = _update;
                freshClam.Start();
                freshClam.Schedule.Start();
            }

            if (_mode == OperatingMode.combined)
            {
                foreach (Schedule scan in _scans)
                {
                    ClamScan clamScan = new ClamScan(_databaseLocation);
                    clamScan.Schedule = scan;
                    clamScan.Start();
                    clamScan.Schedule.Start();
                }
            }

            if ((_mode == OperatingMode.combined) || (_mode == OperatingMode.client))
            {
                foreach (Schedule scan in _scans)
                {
                    ClamdScan clamdScan = new ClamdScan(_databaseLocation);
                    clamdScan.Schedule = scan;
                    clamdScan.Start();
                    clamdScan.Schedule.Start();
                }
            }

            // Add the forwarders

            foreach (KeyValuePair<string, Forwarder> entry in _forwarders)
            {
                Forwarder forwarder = entry.Value;
                switch (forwarder.Type)
                {
                    case Forwarder.ForwaderType.SYSLOG:
                        {
                            SysLog syslog = new SysLog(forwarder.Host, forwarder.Port)
                            {
                                Facility = forwarder.Facility,
                                Severity = forwarder.Severity
                            };
                            forwarder.Notifier = syslog;
                            break;
                        }
                }
            }

            log.Debug("Out Monitor()");
        }
        /// <summary>
        /// 
        /// </summary>
        public bool IsMonitoring
        {
            get { return _monitoringThread != null; }
        }
        /// <summary>
        /// Start monitoring.
        /// </summary>
        public void Start()
        {
            log.Debug("In Start()");

            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            lock (_threadLock)
            {
                if (!IsMonitoring)
                {
                    monitorEventTerminate.Reset();
                    _monitoringThread = new Thread(new ThreadStart(MonitoringThread))
                    {
                        IsBackground = true
                    };
                    _monitoringThread.Start();
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

            monitorRunning = false;     // Exit the watch loop
            monitorSignal.Set();        // force out of the waitOne
            lock (_threadLock)
            {
                Thread thread = _monitoringThread;
                if (thread != null)
                {
                    monitorEventTerminate.Set();
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
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        public static OperatingMode OperatingLookup(string operatingName)
        {
            OperatingMode operatingMode = OperatingMode.combined;

            if (Int32.TryParse(operatingName, out int operatingValue))
            {
                operatingMode = (OperatingMode)operatingValue;
            }
            else
            {
                string lookup = operatingName;
                if (operatingName.Length > 1)
                {
                    lookup = operatingName.ToUpper();
                }

                switch (lookup)
                {
                    case "C":
                    case "CLIENT":
                        {
                            operatingMode = OperatingMode.client;
                            break;
                        }
                    case "c":
                    case "COMBINED":
                    case "BOTH":
                        {
                            operatingMode = OperatingMode.combined;
                            break;
                        }
                    case "S":
                    case "SERVER":
                        {
                            operatingMode = OperatingMode.server;
                            break;
                        }
                }
            }
            return (operatingMode);
        }

        public static DataLocation LocationLookup(string locationName)
        {
            DataLocation dataLocation = DataLocation.program;
            
            if (Int32.TryParse(locationName, out int locationValue))
            {
                dataLocation = (DataLocation)locationValue;
            }
            else
            {
                string lookup = locationName;
                if (locationName.Length > 1)
                {
                    lookup = locationName.ToUpper();
                }

                switch (lookup)
                {
                    case "A":
                    case "APP":
                    case "APPLICATION":
                    case "PROGRAMEDATA":
                        {
                            dataLocation = DataLocation.app;
                            break;
                        }
                    case "L":
                    case "LOCAL":
                    case "LOCALAPPLICATIONDATA":
                        {
                            dataLocation = DataLocation.local;
                            break;
                        }
                    case "P":
                    case "PROGRAM":
                    case "PROGRAMFOLDER":
                        {
                            dataLocation = DataLocation.program;
                            break;
                        }
                    case "R":
                    case "ROAMING":
                    case "APPLICATIONDATA":
                        {
                            dataLocation = DataLocation.roaming;
                            break;
                        }

                }
            }
            return (dataLocation);
        }

        #endregion
        #region Private

        private void MonitoringThread()
        {
            log.Debug("In MonitoringThread()");

            try
            {
                MonitorLoop();
            }
            catch (Exception e)
            {
                log.Fatal(e.ToString());
            }
            _monitoringThread = null;

            log.Debug("Out MonitoringThread()");
        }

        private void MonitorLoop()
        {
            log.Debug("In MonitorLoop()");

            // Monitor messages received from the scanner or updater

            monitorSignal = new AutoResetEvent(false);
            monitorRunning = true;
            do
            {
                monitorSignal.WaitOne(monitorInterval);
                log.Debug("Processing queue");
                while (_messageQueue.Count > 0)
                {
                    Event clamEvent = (Event)_messageQueue.Peek();

                    foreach (KeyValuePair<string, Forwarder> entry in _forwarders)
                    {
                        Forwarder forwarder = entry.Value;
                        try
                        {
                            int error = forwarder.Notifier.Notify(clamEvent.Application, clamEvent.Name, clamEvent.Description);
                            if (error > 0)
                            {
                                log.Error("Could not send to " + forwarder.Id + " " + forwarder.Notifier.ErrorDescription(error));
                            }
                            else
                            {
                                log.Debug("Sent to " + forwarder.Id + " " + clamEvent.Name + " " + clamEvent.Description);
                            }
                        }
                        catch (Exception e)
                        {
                            log.Debug(e.ToString());
                        }
                    }
                        
                    _messageQueue.Dequeue();
                }
                log.Debug("Processed queue");
            }
            while (monitorRunning == true);

            log.Debug("Out MonitorLoop()");
        }

        // Define the event handlers.
        private void OnMessageReceived(object source, ScheduleEventArgs e)
        {
            if (e.Message.Length > 0)
            {
                _messageQueue.Enqueue(e);
                monitorSignal.Set();
            }
        }
        #endregion

    }
}