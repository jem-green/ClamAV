using log4net;
using System;
using System.Collections.Generic;
using System.Threading;

namespace ClamAVLibrary
{
    public class ClamAV : IDisposable
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        AutoResetEvent _monitorSignal = new AutoResetEvent(false);
        Queue<Event> _messageQueue;
        private object _threadLock = new object();
        private Thread _monitoringThread;

        private bool _disposed = false;
        private ManualResetEvent monitorEventTerminate = new ManualResetEvent(false);
        bool _monitorRunning = true;
        private int monitorInterval = 60000;

        private Component.DataLocation _location = Component.DataLocation.App;

        private Clamd _server;
        private FreshClam _refresh;
        private List<Component> _scans;
        private UpdateClam _update;
        Dictionary<string, Forwarder> _forwarders = null;
        Component.OperatingMode _mode = Component.OperatingMode.Combined;

        #endregion
        #region Constructors

        public ClamAV()
        {
            log.Debug("in ClamAV()");
            //_refresh = new FreshClam();
            //_update = new UpdateClam();
            _scans = new List<Component>();
            _messageQueue = new Queue<Event>();
            _forwarders = new Dictionary<string, Forwarder>();
            log.Debug("Out ClamAV()");
        }

        #endregion
        #region Properties

        public Component.DataLocation Location
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

        public Component.OperatingMode Mode
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

        public Clamd Server
        {
            get
            {
                return (_server);
            }
            set
            {
                _server = value;
            }
        }

        public List<Component> Scans
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

        public FreshClam Refresh
        {
            get
            {
                return (_refresh);
            }
            set
            {
                _refresh = value;
            }
        }

        public UpdateClam Update
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
            // Server = clamd + freshclam + updateclam
            // Combined = clamscan + freshclam + updateclam
            // Client = clamdscan + updateclam????

            // Run clamd

            if (_mode == Component.OperatingMode.Server)
            {
                if (_server != null)
                {
                    _server.IsBackground = true;
                    _server.WriteConfig();
                    _server.SocketReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    _server.Start();
                }
            }

            // Run freshclam

            if ((_mode == Component.OperatingMode.Combined) || (_mode == Component.OperatingMode.Server))
            {
                if (_refresh != null)
                {
                    _refresh.WriteConfig();
                    _refresh.SocketReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    _refresh.Start();
                }
            }

            // Run updateclam

            if ((_mode == Component.OperatingMode.Combined) || (_mode == Component.OperatingMode.Server))
            {
                if (_update != null)
                {
                    _update.WriteConfig();
                    _update.SocketReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    _update.Start();
                }
            }

            // Run clamdscan or clamscan depending on mode

            if ((_mode == Component.OperatingMode.Combined) || (_mode == Component.OperatingMode.Client))
            {
                foreach (Component scan in _scans)
                {
                    // could double check the actual mode here

                    scan.WriteConfig();
                    scan.SocketReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    scan.Start();
                }
            }

            // Add the forwarders

            foreach (KeyValuePair<string, Forwarder> entry in _forwarders)
            {
                Forwarder forwarder = entry.Value;
                switch (forwarder.Type)
                {
                    case Forwarder.ForwarderType.SYSLOG:
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
                    Event notification = new Event("ClamAV", "ClamAV", "Started", Event.EventLevel.Emergency);
                    _messageQueue.Enqueue(notification);
                    Thread.Sleep(1000);         // Wait for the monitoring loop to start
                    _monitorSignal.Set();       // force out of the waitOne
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

            Event notification = new Event("ClamAV", "ClamAV", "Stopped", Event.EventLevel.Emergency);
            _messageQueue.Enqueue(notification);
            _monitorSignal.Set();        // force out of the waitOne
            Thread.Sleep(1000);

            _monitorRunning = false;     // Exit the watch loop
            _monitorSignal.Set();        // force out of the waitOne
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
                    _server.Dispose();
                    _refresh.Dispose();
                    foreach (Component component in _scans)
                    {
                        component.Dispose();
                    }
                }
                _disposed = true;
            }
            log.Debug("Out Dispose()");
        }

        public static Component.OperatingMode ModeLookup(string modeName)
        {
            Component.OperatingMode mode = Component.OperatingMode.Combined;

            if (Int32.TryParse(modeName, out int modeValue))
            {
                mode = (Component.OperatingMode)modeValue;
            }
            else
            {
                string lookup = modeName;
                if (modeName.Length > 1)
                {
                    lookup = modeName.ToUpper();
                }

                switch (lookup)
                {
                    case "C":
                    case "CLIENT":
                        {
                            mode = Component.OperatingMode.Client;
                            break;
                        }
                    case "L":
                    case "COMBINED":
                        {
                            mode = Component.OperatingMode.Combined;
                            break;
                        }
                    case "S":
                    case "SERVER":
                        {
                            mode = Component.OperatingMode.Server;
                            break;
                        }
                }
            }
            return (mode);
        }

        public static Component.DataLocation LocationLookup(string locationName)
        {
            Component.DataLocation dataLocation = Component.DataLocation.Program;

            if (Int32.TryParse(locationName, out int locationValue))
            {
                dataLocation = (Component.DataLocation)locationValue;
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
                            dataLocation = Component.DataLocation.App;
                            break;
                        }
                    case "L":
                    case "LOCAL":
                    case "LOCALAPPLICATIONDATA":
                        {
                            dataLocation = Component.DataLocation.Local;
                            break;
                        }
                    case "P":
                    case "PROGRAM":
                    case "PROGRAMFOLDER":
                        {
                            dataLocation = Component.DataLocation.Program;
                            break;
                        }
                    case "R":
                    case "ROAMING":
                    case "APPLICATIONDATA":
                        {
                            dataLocation = Component.DataLocation.Roaming;
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

            _monitorRunning = true;
            do
            {
                _monitorSignal.WaitOne(monitorInterval);
                log.Debug("Processing queue");
                while (_messageQueue.Count > 0)
                {
                    Event clamEvent = _messageQueue.Peek();

                    if (clamEvent.Type == Event.EventType.Pause)
                    {
                        // If running in server or combined mode then need to stop clamd
                        if ((_mode == Component.OperatingMode.Server) || (_mode == Component.OperatingMode.Combined))
                        {
                            _server.Pause();
                        }
                    }
                    if (clamEvent.Type == Event.EventType.Resume)
                    {
                        // If running in server or combined mode then need to start clamd 
                        if ((_mode == Component.OperatingMode.Server) || (_mode == Component.OperatingMode.Combined))
                        {
                            _server.Resume();
                        }
                    }
                    else if (clamEvent.Type == Event.EventType.Notification)
                    {
                        foreach (KeyValuePair<string, Forwarder> entry in _forwarders)
                        {
                            Forwarder forwarder = entry.Value;
                            try
                            {
                                // Need to translate events to notifications
                                Notify.PriorityOrder priority = Notify.PriorityOrder.Normal;
                                switch (clamEvent.Level)
                                {
                                    case Event.EventLevel.Information:
                                        {
                                            priority = Notify.PriorityOrder.Low;
                                            break;
                                        }
                                    case Event.EventLevel.Notification:
                                        {
                                            priority = Notify.PriorityOrder.Moderate;
                                            break;
                                        }
                                    case Event.EventLevel.Warning:
                                        {
                                            priority = Notify.PriorityOrder.Moderate;
                                            break;
                                        }
                                    case Event.EventLevel.Error:
                                        {
                                            priority = Notify.PriorityOrder.Normal;
                                            break;
                                        }
                                    case Event.EventLevel.Critical:
                                        {
                                            priority = Notify.PriorityOrder.High;
                                            break;
                                        }
                                    case Event.EventLevel.Alert:
                                        {
                                            priority = Notify.PriorityOrder.High;
                                            break;
                                        }
                                    case Event.EventLevel.Emergency:
                                        {
                                            priority = Notify.PriorityOrder.Emergency;
                                            break;
                                        }
                                }

                                int error = forwarder.Notifier.Notify(clamEvent.Application, clamEvent.Name, clamEvent.Description, priority);
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
                    }
                    _messageQueue.Dequeue();
                }
                log.Debug("Processed queue");
            }
            while (_monitorRunning == true);

            log.Debug("Out MonitorLoop()");
        }

        // Define the event handlers.
        private void OnMessageReceived(object source, NotificationEventArgs e)
        {
            if (e.Notification != null)
            {
                if (e.Notification.Name.Length > 0)
                {
                    _messageQueue.Enqueue(e.Notification);
                    _monitorSignal.Set();
                }
            }
        }
        #endregion
    }
}