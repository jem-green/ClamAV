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
        Queue<Notification> _messageQueue;
        private object _threadLock = new object();
        private Thread _monitoringThread;

        private bool _disposed = false;
        private ManualResetEvent monitorEventTerminate = new ManualResetEvent(false);
        bool monitorRunning = true;	
        private int monitorInterval = 60000;
		
        private Component.DataLocation _location = Component.DataLocation.app;

        private Clamd _server;
        private FreshClam _update;
        private List<Component> _scans;
        Dictionary<string, Forwarder> _forwarders = null;
        Component.OperatingMode _mode = Component.OperatingMode.combined;

        #endregion
        #region Constructors
		
		public ClamAV()
        {
            log.Debug("in ClamAV()");
        	_update = new FreshClam();
        	_scans = new List<Component>();
			_messageQueue = new Queue<Notification>();
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

        public FreshClam Update
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
            // Server = clamd + freshclam
            // Combined = freshclam + clamscan
            // Client = clamdscan

            // Run clamd

            if (_mode == Component.OperatingMode.server)
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

            if ((_mode == Component.OperatingMode.combined) || (_mode == Component.OperatingMode.server))
            {
                _update.WriteConfig();
                _update.SocketReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                _update.Start();
            }

            // run clamdscan or clamscan depending on mode

            if ((_mode == Component.OperatingMode.combined) || (_mode == Component.OperatingMode.client))
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
                    _update.Dispose();
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
            Component.OperatingMode mode = Component.OperatingMode.combined;

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
                            mode = Component.OperatingMode.client;
                            break;
                        }
                    case "L":
                    case "COMBINED":
                        {
                            mode = Component.OperatingMode.combined;
                            break;
                        }
                    case "S":
                    case "SERVER":
                        {
                            mode = Component.OperatingMode.server;
                            break;
                        }
                }
            }
            return (mode);
        }

        public static Component.DataLocation LocationLookup(string locationName)
        {
            Component.DataLocation dataLocation = Component.DataLocation.program;
            
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
                            dataLocation = Component.DataLocation.app;
                            break;
                        }
                    case "L":
                    case "LOCAL":
                    case "LOCALAPPLICATIONDATA":
                        {
                            dataLocation = Component.DataLocation.local;
                            break;
                        }
                    case "P":
                    case "PROGRAM":
                    case "PROGRAMFOLDER":
                        {
                            dataLocation = Component.DataLocation.program;
                            break;
                        }
                    case "R":
                    case "ROAMING":
                    case "APPLICATIONDATA":
                        {
                            dataLocation = Component.DataLocation.roaming;
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
                    Notification clamEvent = _messageQueue.Peek();

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
                                log.Info("Sent to " + forwarder.Id + " " + clamEvent.Name + " " + clamEvent.Description);
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
        private void OnMessageReceived(object source, NotificationEventArgs e)
        {
            if (e.Notification != null)
            {
                if (e.Notification.Name.Length > 0)
                {
                    _messageQueue.Enqueue(e.Notification);
                    monitorSignal.Set();
                }
            }
        }
        #endregion
    }
}