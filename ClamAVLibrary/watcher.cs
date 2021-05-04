using System;
using System.Diagnostics;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using log4net;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace ClamAVLibrary
{
    public class Watcher //: IDisposable
    {
        //        #region Fields

        //        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //        AutoResetEvent monitorSignal;
        //        AutoResetEvent checkSignal;
        //        Queue messageQueue;
        //        SocketMonitor socketMonitor;

        //        private object _threadLock = new object();
        //        private Thread monitoringThread;
        //        private Thread checkingThread;
        //        private bool _disposed = false;
        //        private ManualResetEvent monitorEventTerminate = new ManualResetEvent(false);
        //        private ManualResetEvent checkEventTerminate = new ManualResetEvent(false);

        //        private int port = 694;
        //        IPAddress @interface = IPAddress.Parse("127.0.0.1");
        //        private int timeout = 60000;
        //        private int checkInterval = 60000;
        //        private int monitorInterval = 60000;

        //        Dictionary<string, Forwarder> forwarders = null;
        //        SortedList<string, Node> nodes = null;

        //        bool monitorRunning = true;
        //        bool checkRunning = true;

        //        //Storage storage;

        //        #endregion

        //        #region Constructor

        //        public Watcher()
        //        {
        //            log.Debug("In Watcher()");
        //            forwarders = new Dictionary<string, Forwarder>();
        //            nodes = new SortedList<string, Node>();
        //            messageQueue = new Queue();
        //            log.Debug("Out Watcher()");
        //        }

        //        #endregion

        //        #region Properties

        //        //public IPAddress Interface
        //        //{
        //        //    get
        //        //    {
        //        //        return (@interface);
        //        //    }
        //        //    set
        //        //    {
        //        //        @interface = value;
        //        //    }
        //        //}


        //        public int Check
        //        {
        //            get
        //            {
        //                return (checkInterval);
        //            }
        //            set
        //            {
        //                checkInterval = value;
        //            }
        //        }

        //        public string Interface
        //        {
        //            get
        //            {
        //                return (@interface.ToString());
        //            }
        //            set
        //            {
        //                @interface = GetInterfaceAddress(value);
        //            }
        //        }

        //        public int Monitor
        //        {
        //            get
        //            {
        //                return (monitorInterval);
        //            }
        //            set
        //            {
        //                monitorInterval = value;
        //            }
        //        }

        //        public int Port
        //        {
        //            get
        //            {
        //                return (port);
        //            }
        //            set
        //            {
        //                port = value;
        //            }
        //        }

        //        public int Timeout
        //        {
        //            get
        //            {
        //                return (timeout);
        //            }
        //            set
        //            {
        //                timeout = value;
        //            }
        //        }

        //        #endregion

        //        #region Methods

        //        public void Add(Forwarder forwarder)
        //        {
        //            try
        //            {
        //                forwarders.Add(forwarder.Id, forwarder);
        //            }
        //            catch (ArgumentException)
        //            {
        //                log.Debug("Duplicate entry " + forwarder.Id);
        //            }
        //            catch (Exception e)
        //            {
        //                log.Error(e.ToString());
        //            }
        //        }

        //        public void Remove(Forwarder forwarder)
        //        {
        //            forwarders.Remove(forwarder.Id);
        //        }

        //        public void Watch()
        //        {
        //            log.Debug("In Watch()");

        //            //if (storage.Create() == true)
        //            //{

        //            //    if (storage.Open() == true)
        //            //    {
        //            //        // Load any existing nodes

        //            //        nodes = storage.GetNodes();

        //            //        // Restore exiting nodes data

        //            //        storage.RestoreNodes(nodes);

        //            //    }
        //            //}

        //            // Add the forwarders

        //            foreach (KeyValuePair<string, Forwarder> entry in forwarders)
        //            {
        //                Forwarder forwarder = entry.Value;
        //                switch (forwarder.Type)
        //                {
        //                    case Forwarder.ForwaderType.SYSLOG:
        //                        {
        //                            SysLog syslog = new SysLog(forwarder.Host, forwarder.Port)
        //                            {
        //                                Facility = forwarder.Facility,
        //                                Severity = forwarder.Severity
        //                            };
        //                            forwarder.Notifier = syslog;
        //                            break;
        //                        }
        //                }
        //            }

        //            // Add socket

        //            try
        //            {
        //                socketMonitor = new SocketMonitor(@interface, port);
        //                socketMonitor.SocketReceived += new EventHandler<SocketEventArgs>(OnMessageReceived);
        //                socketMonitor.Start();
        //            }
        //            catch (Exception e)
        //            {
        //                log.Debug(e.ToString());
        //            }

        //            log.Debug("Out Watch()");
        //        }

        //        /// <summary>
        //        /// 
        //        /// </summary>
        //        public bool IsWatching
        //        {
        //            get { return monitoringThread != null; }
        //        }

        //        /// <summary>
        //        /// Start watching.
        //        /// </summary>
        //        public void Start()
        //        {
        //            log.Debug("In Start()");

        //            if (_disposed)
        //                throw new ObjectDisposedException(null, "This instance is already disposed");

        //            lock (_threadLock)
        //            {
        //                if (!IsWatching)
        //                {
        //                    monitorEventTerminate.Reset();
        //                    monitoringThread = new Thread(new ThreadStart(MonitoringThread))
        //                    {
        //                        IsBackground = true
        //                    };
        //                    monitoringThread.Start();

        //                    // Start the checking thread

        //                    checkingThread = new Thread(new ThreadStart(CheckingThread))
        //                    {
        //                        IsBackground = true
        //                    };
        //                    checkingThread.Start();
        //                }
        //            }
        //            log.Debug("Out Start()");
        //        }

        //        /// <summary>
        //        /// Stops the watching thread.
        //        /// </summary>
        //        public void Stop()
        //        {
        //            log.Debug("In Stop()");

        //            if (_disposed)
        //                throw new ObjectDisposedException(null, "This instance is already disposed");

        //            monitorRunning = false;     // Exit the watch loop
        //            monitorSignal.Set();        // force out of the waitOne
        //            socketMonitor.Dispose();    // Stop the socket monitor

        //            checkRunning = false;       // Exit the check loop
        //            checkSignal.Set();          // force out of the waitOne


        //            lock (_threadLock)
        //            {
        //                Thread thread = monitoringThread;
        //                if (thread != null)
        //                {
        //                    monitorEventTerminate.Set();
        //                    thread.Join();
        //                }
        //            }

        //            lock (_threadLock)
        //            {
        //                Thread thread = checkingThread;
        //                if (thread != null)
        //                {
        //                    checkEventTerminate.Set();
        //                    thread.Join();
        //                }
        //            }

        //            log.Debug("Out Stop()");
        //        }

        //        /// <summary>
        //        /// Disposes this object.
        //        /// </summary>
        //        public void Dispose()
        //        {
        //            Stop();
        //            _disposed = true;
        //            GC.SuppressFinalize(this);
        //        }

        //        private void MonitoringThread()
        //        {
        //            log.Debug("In MonitoringThread()");

        //            try
        //            {
        //                MonitorLoop();
        //            }
        //            catch (Exception e)
        //            {
        //                log.Fatal(e.ToString());
        //            }
        //            monitoringThread = null;

        //            log.Debug("Out MonitoringThread()");
        //        }

        //        private void CheckingThread()
        //        {
        //            log.Debug("In CheckingThread()");

        //            try
        //            {
        //                CheckLoop();
        //            }
        //            catch (Exception e)
        //            {
        //                log.Fatal(e.ToString());
        //            }
        //            checkingThread = null;

        //            log.Debug("Out CheckingThread()");
        //        }

        //        public void CheckLoop()
        //        {
        //            log.Debug("In CheckLoop()");

        //            // Check for late heartbeats

        //            checkSignal = new AutoResetEvent(false);
        //            checkRunning = true;
        //            do
        //            {
        //                checkSignal.WaitOne(checkInterval);
        //                log.Debug("Processing Check");
        //                foreach (KeyValuePair<string, Node> item in nodes)
        //                {
        //                    Node node = item.Value;

        //                    log.Debug("Checking node " + node.NodeId);

        //                    TimeSpan timeSpan = -node.LastUpdated.Subtract(DateTime.Now);
        //                    if (timeSpan.TotalMilliseconds > timeout * 3)
        //                    {
        //                        if (node.Status != Node.StatusType.Down)
        //                        {
        //                            log.Info("No heartbeat from " + node.NodeId + " for " + Convert.ToInt32(timeSpan.TotalSeconds) + " seconds");

        //                            // Ping back to check
        //                            if (Ping(item.Key) != 0)
        //                            {
        //                                node.Status = Node.StatusType.Down;
        //                                storage.UpdateClamAV(node);

        //                                foreach (KeyValuePair<string, Forwarder> entry in forwarders)
        //                                {
        //                                    Forwarder forwarder = entry.Value;
        //                                    try
        //                                    {
        //                                        string eventName = "ClamAV";
        //                                        string description = "Stopped";
        //                                        int error = forwarder.Notifier.Notify(item.Key.ToString(), eventName, description);
        //                                        if (error > 0)
        //                                        {
        //                                            log.Error("Could not send to " + forwarder.Id + " " + forwarder.Notifier.ErrorDescription(error));
        //                                        }
        //                                        else
        //                                        {
        //                                            log.Debug("Sent to " + forwarder.Id + " " + eventName + " " + description);
        //                                        }
        //                                    }
        //                                    catch (Exception e)
        //                                    {
        //                                        log.Debug(e.ToString());
        //                                    }
        //                                }
        //                            }
        //                            else
        //                            {
        //                                // Set the status update to responding if the node could be pinged

        //                                if (node.Status != Node.StatusType.Responding)
        //                                {
        //                                    node.Status = Node.StatusType.Responding;
        //                                    storage.UpdateClamAV(node);
        //                                }

        //                                foreach (KeyValuePair<string, Forwarder> entry in forwarders)
        //                                {
        //                                    Forwarder forwarder = entry.Value;
        //                                    try
        //                                    {
        //                                        string eventName = "ClamAV";
        //                                        string description = "Responding";
        //                                        int error = forwarder.Notifier.Notify(item.Key.ToString(), eventName, description);
        //                                        if (error > 0)
        //                                        {
        //                                            log.Error("Could not send to " + forwarder.Id + " " + forwarder.Notifier.ErrorDescription(error));
        //                                        }
        //                                        else
        //                                        {
        //                                            log.Debug("Sent to " + forwarder.Id + " " + eventName + " " + description);
        //                                        }
        //                                    }
        //                                    catch (Exception e)
        //                                    {
        //                                        log.Debug(e.ToString());
        //                                    }
        //                                }
        //                            }
        //                        }
        //                    }
        //                    else if (timeSpan.TotalMilliseconds >= timeout)
        //                    {
        //                        log.Debug("Delayed heartbeat from " + item.Key + " of " + Convert.ToInt32(timeSpan.TotalSeconds) + " seconds");
        //                        if (node.Status != Node.StatusType.Delayed)
        //                        {
        //                            node.Status = Node.StatusType.Delayed;
        //                            storage.UpdateClamAV(node);
        //                        }
        //                    }
        //                }
        //                log.Debug("Processed Check");
        //            }
        //            while (checkRunning == true);

        //            log.Debug("Out CheckLoop()");
        //        }

        //        public void MonitorLoop()
        //        {
        //            log.Debug("In MonitorLoop()");

        //            // Build a list of heartbeats received in memory

        //            monitorSignal = new AutoResetEvent(false);
        //            monitorRunning = true;
        //            do
        //            {
        //                monitorSignal.WaitOne(monitorInterval);
        //                log.Debug("Processing queue");
        //                while (messageQueue.Count > 0)
        //                {
        //                    SocketEventArgs SocketEvent = (SocketEventArgs)messageQueue.Peek();

        //                    // update or add the node based on the recevied heartbeat

        //                    try
        //                    {
        //                        Node node = nodes[SocketEvent.Beat.NodeId];
        //                        node.LastUpdated = SocketEvent.Beat.TimeStamp;
        //                        if (SocketEvent.Beat.IpAddress != node.IpAddress)
        //                        {
        //                            node.IpAddress = SocketEvent.Beat.IpAddress;
        //                            log.Info("Node " + node.NodeId + " IP address changed to " +  node.IpAddress);
        //                            storage.UpdateNode(node);
        //                        }

        //                        // Check if the node have recently started heartbeating send started message

        //                        if ((node.Status != Node.StatusType.Up))
        //                        {
        //                            log.Info("ClamAV recevied from " + node.NodeId);
        //                            node.Status = Node.StatusType.Up;
        //                            storage.UpdateClamAV(node);
        //                        }

        //                        if ((node.Status == Node.StatusType.Down) || (node.Status == Node.StatusType.Responding))
        //                        {
        //                            foreach (KeyValuePair<string, Forwarder> entry in forwarders)
        //                            {
        //                                Forwarder forwarder = entry.Value;
        //                                try
        //                                {
        //                                    string eventName = "ClamAV";
        //                                    string description = "Started";
        //                                    int error = forwarder.Notifier.Notify(node.IpAddress, eventName, description);
        //                                    if (error > 0)
        //                                    {
        //                                        log.Error("Could not send to " + forwarder.Id + " " + forwarder.Notifier.ErrorDescription(error));
        //                                    }
        //                                    else
        //                                    {
        //                                        log.Debug("Sent to " + forwarder.Id + " " + eventName + " " + description);
        //                                    }
        //                                }
        //                                catch (Exception e)
        //                                {
        //                                    log.Debug(e.ToString());
        //                                }
        //                            }
        //                        }


        //                    }
        //                    catch (KeyNotFoundException)
        //                    {
        //                        // Need to check if the node has changed ip address
        //                        // Or base the lookup on the nodeId

        //                        Node node = new Node()
        //                        {
        //                            NodeId = SocketEvent.Beat.NodeId,
        //                            IpAddress = SocketEvent.Beat.IpAddress,
        //                            InstanceId = SocketEvent.Beat.Instance,
        //                            Status = Node.StatusType.Up,
        //                            LastUpdated = SocketEvent.Beat.TimeStamp
        //                        };
        //                        storage.AddNode(node);
        //                        storage.AddClamAV(node);
        //                        nodes.Add(node.NodeId, node);
        //                    }
        //                    catch (Exception e)
        //                    {
        //                        log.Error(e);
        //                    }

        //                    messageQueue.Dequeue();
        //                }
        //                log.Debug("Processed queue");
        //            }
        //            while (monitorRunning == true);

        //            log.Debug("Out MonitorLoop()");
        //        }

        //        #endregion

        //        // Define the event handlers.
        //        private void OnMessageReceived(object source, SocketEventArgs e)
        //        {
        //            if (e.IP != "")
        //            {
        //                messageQueue.Enqueue(e);
        //                monitorSignal.Set();
        //            }
        //        }

        //        private int Ping(string ip)
        //        {
        //            int check = 0;
        //            int error = 1;
        //            int number = 3;
        //            int threshold = number;

        //            log.Debug("Pinging " + ip);

        //            PingOptions options = new PingOptions(128, true);   //set options ttl=128 and no fragmentation
        //            Ping ping = new Ping();                             //create a Ping object
        //            byte[] data = new byte[32];                         //32 empty bytes buffer

        //            //List<long> responseTimes = new List<long>();

        //            for (int i = 0; i < number; i++)
        //            {
        //                try
        //                {
        //                    PingReply reply = ping.Send(ip, 1000, data, options);

        //                    if (reply != null)
        //                    {
        //                        switch (reply.Status)
        //                        {
        //                            case IPStatus.Success:
        //                                {
        //                                    check = check + 1;
        //                                }
        //                                break;
        //                            case IPStatus.TimedOut:
        //                                error = 3;
        //                                break;
        //                            default:
        //                                error = 4;
        //                                break;
        //                        }
        //                    }
        //                    else
        //                    {
        //                        error = 5;
        //                    }
        //                }
        //                catch
        //                {
        //                    error = 5;
        //                }
        //            }

        //            if (check >= threshold)
        //            {
        //                log.Debug("Ping response received from " + ip);
        //                error = 0;
        //            }

        //            return (error);
        //        }


        //        private IPAddress GetInterfaceAddress(string cidr)
        //        {
        //            log.Debug("In GetInterfaceAddress");
        //            IPAddress ip = IPAddress.Parse("127.0.0.1");
        //            long subnet = 0;
        //            long mask = 0;
        //            if (cidr.Contains("/"))
        //            {
        //                mask = Convert.ToInt32(cidr.Substring(cidr.IndexOf('/') + 1));
        //                subnet = BitConverter.ToInt32(IPAddress.Parse(cidr.Substring(0, cidr.IndexOf('/'))).GetAddressBytes(), 0);
        //            }
        //            else
        //            {
        //                if (mask == 0)
        //                {
        //                    mask = 24;
        //                }
        //                subnet = BitConverter.ToInt32(IPAddress.Parse(cidr).GetAddressBytes(), 0);
        //            }

        //            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
        //            {
        //                if ((networkInterface.OperationalStatus == OperationalStatus.Up) && (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
        //                {
        //                    foreach (UnicastIPAddressInformation addressInformation in networkInterface.GetIPProperties().UnicastAddresses)
        //                    {
        //                        if (addressInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        //                        {
        //                            long ipAddress = BitConverter.ToInt32(addressInformation.Address.GetAddressBytes(), 0);
        //                            if ((subnet & mask) == (ipAddress & mask))
        //                            {
        //                                ip = addressInformation.Address;
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            log.Debug("Out GetInterfaceAddress");
        //            return (ip);
        //        }
        //    }

        //    class Oldest : IComparer<Node>
        //    { 
        //        public int Compare(Node x, Node y)
        //        {
        //            if (x.LastUpdated == y.LastUpdated)
        //            {
        //                return (0);
        //            }

        //            if (x.LastUpdated < y.LastUpdated)
        //            {
        //                return (-1);
        //            }

        //            return (1);
        //        }
    }
}
