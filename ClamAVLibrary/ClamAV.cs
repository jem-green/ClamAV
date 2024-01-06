using TracerLibrary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Diagnostics;
using static ClamAVLibrary.Component;

namespace ClamAVLibrary
{
    public class ClamAV : IDisposable
    {
        #region Fields

        private readonly AutoResetEvent _notificationSignal = new AutoResetEvent(false);
        private readonly AutoResetEvent _commandSignal = new AutoResetEvent(false);
        private readonly Queue<Event> _messageQueue;
        private readonly Queue<Command> _commandQueue;
        private readonly object _threadLock = new object();
        private Thread _monitoringThread;
        private Thread commandThread;
        private bool _disposed = false;
        private readonly ManualResetEvent monitorEventTerminate = new ManualResetEvent(false);
        private ManualResetEvent CommandEventTerminate = new ManualResetEvent(false);

        bool _notificationRunning = true;
        bool _commandRunning = true;
        private readonly int monitorInterval = 60000;

        private Component.DataLocation _location = Component.DataLocation.App;

        private Clamd _server;
        private FreshClam _refresh;
        private List<Component> _scans;
        private UpdateClam _update;
        readonly Dictionary<string, Forwarder> _forwarders = null;
        Component.OperatingMode _mode = Component.OperatingMode.Combined;

        #endregion
        #region Constructors

        public ClamAV()
        {
            Debug.WriteLine("in ClamAV()");
            //_refresh = new FreshClam();
            //_update = new UpdateClam();
            _scans = new List<Component>();
            _messageQueue = new Queue<Event>();
            _commandQueue = new Queue<Command>();
            _forwarders = new Dictionary<string, Forwarder>();
            Debug.WriteLine("Out ClamAV()");
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
                TraceInternal.TraceWarning("Duplicate entry " + forwarder.Id);
            }
            catch (Exception e)
            {
                TraceInternal.TraceError(e.ToString());
            }
        }

        public void Remove(Forwarder forwarder)
        {
            _forwarders.Remove(forwarder.Id);
        }

        public void Monitor()
        {
            Debug.WriteLine("In Monitor()");

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
                    _server.EventReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    _server.Start();
                }
            }

            // Run freshclam

            if ((_mode == Component.OperatingMode.Combined) || (_mode == Component.OperatingMode.Server))
            {
                if (_refresh != null)
                {
                    _refresh.WriteConfig();
                    _refresh.EventReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    _refresh.Start();
                }
            }

            // Run updateclam

            if ((_mode == Component.OperatingMode.Combined) || (_mode == Component.OperatingMode.Server))
            {
                if (_update != null)
                {
                    _update.WriteConfig();
                    _update.EventReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
                    _update.CommandReceived += new EventHandler<CommandEventArgs>(OnCommandReceived);
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
                    scan.EventReceived += new EventHandler<NotificationEventArgs>(OnMessageReceived);
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
                            TraceInternal.TraceVerbose("Add SYSLOG");
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

            Debug.WriteLine("Out Monitor()");
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
            Debug.WriteLine("In Start()");

            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            lock (_threadLock)
            {
                if (!IsMonitoring)
                {
                    _monitoringThread = new Thread(new ThreadStart(MonitoringThread))
                    {
                        IsBackground = true
                    };
                    _monitoringThread.Start();

                    Event notification = new Event("ClamAV", "ClamAV", "Started", Event.EventLevel.Emergency);
                    _messageQueue.Enqueue(notification);

                    // Start the command thread

                    commandThread = new Thread(new ThreadStart(CommandThread))
                    {
                        IsBackground = true
                    };
                    commandThread.Start();
					
                    Thread.Sleep(1000);         // Wait for the monitoring loop to start
                    _notificationSignal.Set();       // force out of the waitOne
                }
            }
            Debug.WriteLine("Out Start()");
        }
        /// <summary>
        /// Stops the watching thread.
        /// </summary>
        public void Stop()
        {
            Debug.WriteLine("In Stop()");

            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            Event notification = new Event("ClamAV", "ClamAV", "Stopped", Event.EventLevel.Emergency);
            _messageQueue.Enqueue(notification);
            _notificationSignal.Set();        // force out of the waitOne

            _commandRunning = false;       // Exit the check loop
            _commandSignal.Set();       // force out of the waitOne

            _notificationRunning = false;     // Exit the watch loop
            _notificationSignal.Set();        // force out of the waitOne

            lock (_threadLock)
            {
                Thread thread = _monitoringThread;
                if (thread != null)
                {
                    monitorEventTerminate.Set();
                    thread.Join();
                }
            }
			
			lock (_threadLock)
            {
                Thread thread = commandThread;
                if (thread != null)
                {
                    CommandEventTerminate.Set();
                    thread.Join();
                }
            }
			
            Debug.WriteLine("Out Stop()");
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
            Debug.WriteLine("In Dispose()");
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
            Debug.WriteLine("Out Dispose()");
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
                    case "C":
                    case "CUSTOM":
                        {
                            dataLocation = DataLocation.Custom;
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
            Debug.WriteLine("In MonitoringThread()");

            try
            {
                MonitorNotification();
            }
            catch (Exception e)
            {
                TraceInternal.TraceCritical(e.ToString());
            }
            _monitoringThread = null;
            
            Debug.WriteLine("Out MonitoringThread()");
        }

        private void CommandThread()
        {
            Debug.WriteLine("In CommandThread()");

            try
            {
                MonitorCommand();
            }
            catch (Exception e)
            {
                TraceInternal.TraceCritical(e.ToString());
            }
            _monitoringThread = null;

            Debug.WriteLine("Out CommandThread()");
        }

        private void MonitorNotification()
        {
            Debug.WriteLine("In MonitorNitification()");

            // Monitor messages received from the scanner or updater

            _notificationRunning = true;
            do
            {
                _notificationSignal.WaitOne(monitorInterval);
                TraceInternal.TraceVerbose("Processing notification");
                while (_messageQueue.Count > 0)
                {
                    Event clamEvent = _messageQueue.Peek();

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
                                TraceInternal.TraceError("Could not send to " + forwarder.Id + " " + forwarder.Notifier.ErrorDescription(error));
                            }
                            else
                            {
                                TraceInternal.TraceVerbose("Sent to " + forwarder.Id + " " + clamEvent.Name + " " + clamEvent.Description);
                            }
                        }
                        catch (Exception e)
                        {
                            TraceInternal.TraceError(e.ToString());
                        }
                    }
                    
                    _messageQueue.Dequeue();
                }
                TraceInternal.TraceVerbose("Processed notification");
            }
            while (_notificationRunning == true);

            Debug.WriteLine("Out MonitorNitification()");
        }

        private void MonitorCommand()
        {
            Debug.WriteLine("In MonitorCommand()");

            // Monitor command received from the scanner or updater

            _commandRunning = true;
            do
            {
                _commandSignal.WaitOne(monitorInterval);
                TraceInternal.TraceVerbose("Processing command");
                while (_commandQueue.Count > 0)
                {
                    Command command = _commandQueue.Peek();

                    if (command.Type == Command.CommandType.Pause)
                    {
                        // If running in server or combined mode then need to stop clamd
                        if ((_mode == Component.OperatingMode.Server) || (_mode == Component.OperatingMode.Combined))
                        {
                            _server.Pause();
                        }
                    }
                    if (command.Type == Command.CommandType.Resume)
                    {
                        // If running in server or combined mode then need to start clamd 
                        if ((_mode == Component.OperatingMode.Server) || (_mode == Component.OperatingMode.Combined))
                        {
                            _server.Resume();
                        }
                    }
                    _commandQueue.Dequeue();
                }
                TraceInternal.TraceVerbose("Processed command");
            }
            while (_commandRunning == true);

            Debug.WriteLine("Out MonitorCommand()");
        }

        // Define the event handlers.
        private void OnMessageReceived(object source, NotificationEventArgs e)
        {
            if (e.Notification != null)
            {
                if (e.Notification.Name.Length > 0)
                {
                    _messageQueue.Enqueue(e.Notification);
                    _notificationSignal.Set();
                }
            }
        }

        private void OnCommandReceived(object source, CommandEventArgs e)
        {
            if (e.Command != null)
            {
                if (e.Command.Name.Length > 0)
                {
                    _commandQueue.Enqueue(e.Command);
                    _commandSignal.Set();
                }
            }
        }

        #endregion
    }
}