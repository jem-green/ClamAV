using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Text;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace ClamAVLibrary
{ 
    /// </example>
    public class SocketMonitor : IDisposable
    {
        #region Event handling

        /// <summary>
        /// Occurs when the socket receives a message.
        /// </summary>
        public event EventHandler<SocketEventArgs> SocketReceived;

        /// <summary>
        /// Handles the actual event
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnSocketReceived(SocketEventArgs e)
        {
            EventHandler<SocketEventArgs> handler = SocketReceived;
            if (handler != null)
                handler(this, e);
        }

        /// <summary>
        /// Occurs when the access to the socket fails.
        /// </summary>
        public event ErrorEventHandler Error;

        protected virtual void OnError(Exception e)
        {
            ErrorEventHandler handler = Error;
            if (handler != null)
                handler(this, new ErrorEventArgs(e));
        }

        #endregion
        #region Variables

        // Start to wait for Socket events on port 694

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private bool _disposed = false;
        private Thread _thread;
        private object _threadLock = new object();
        private ManualResetEvent _eventTerminate = new ManualResetEvent(false);
        private AutoResetEvent _eventNotify = new AutoResetEvent(false);
        private int port;
        private IPAddress _interface = IPAddress.Parse("127.0.0.1");

        //The main socket on which the server listens to the clients
        Socket serverSocket;


        byte[] byteData = new byte[1024];

        #endregion
        #region Constructors

        public SocketMonitor(int port)
        {
            this._interface = GetIPAddress();
            this.port = port;
        }

        public SocketMonitor(IPAddress @interface, int port)
        {
            this._interface = @interface;
            this.port = port;
        }

        public SocketMonitor(string @interface, int port)
        {
            this._interface = IPAddress.Parse(@interface);
            this.port = port;
        }

        #endregion
        #region Methods
        /// <summary>
        /// Disposes this object.
        /// </summary>
        public void Dispose()
        {
            Stop();
            _disposed = true;
            GC.SuppressFinalize(this);
        }

         /// <summary>
        /// <b>true</b> if this <see cref="SocketMonitor"/> object is currently monitoring;
        /// otherwise, <b>false</b>.
        /// </summary>
        public bool IsMonitoring
        {
            get { return _thread != null; }
        }

        /// <summary>
        /// Start monitoring.
        /// </summary>
        public void Start()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            lock (_threadLock)
            {
                if (!IsMonitoring)
                {
                    _eventTerminate.Reset();
                    _thread = new Thread(new ThreadStart(MonitorThread))
                    {
                        IsBackground = true
                    };
                    _thread.Start();
                }
            }
        }

        /// <summary>
        /// Stops the monitoring thread.
        /// </summary>
        public void Stop()
        {
            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            lock (_threadLock)
            {
                Thread thread = _thread;
                if (thread != null)
                {
                    _eventTerminate.Set();
                    thread.Join();
                }
            }
        }
        #endregion
        #region Private
        private void MonitorThread()
        {
            try
            {
                ThreadLoop();
            }
            catch (Exception e)
            {
                OnError(e);
            }
            _thread = null;
        }

        private void ThreadLoop()
        {
            log.Debug("In ThreadLoop()");
            //We are using UDP sockets
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            //Assign the any IP of the machine and listen on port number
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Any, port);

            //Bind this address to the server
            serverSocket.Bind(ipEndPoint);

            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            //The epSender identifies the incoming clients
            EndPoint epSender = (EndPoint)ipeSender;

            //AutoResetEvent _eventNotify = new AutoResetEvent(true);
            WaitHandle[] waitHandles = new WaitHandle[] { _eventNotify, _eventTerminate };
            while (!_eventTerminate.WaitOne(0, true))
            {
                log.Info("Wait on " + _interface + ":" + port);
                //Start receiving data
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);
                if (WaitHandle.WaitAny(waitHandles) == 0)
                {
                    log.Debug("Terminated");
                }
            }
            log.Debug("Out ThreadLoop()");
        }

        void ReceiveData(IAsyncResult iar)
        {
            IPEndPoint ipeSender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint epSender = (EndPoint)ipeSender;
            
            Beat beat = new Beat(byteData);
            serverSocket.EndReceiveFrom(iar, ref epSender);

            // Raise an event and leave the watcher to handled this
            string ip = epSender.ToString().Split(':')[0];
            SocketEventArgs args = new SocketEventArgs(ip, beat);
            OnSocketReceived(args);
            log.Debug("heartbeat received from " + ip);

            if (!_eventTerminate.WaitOne(0, true))
            {
                //Start listening to the message send by the user
                serverSocket.BeginReceiveFrom(byteData, 0, byteData.Length, SocketFlags.None, ref epSender, new AsyncCallback(ReceiveData), epSender);
            }
        }

        private IPAddress GetIPAddress()
        {
            IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
            try
            {
                IPAddress[] ipv4Addresses = Dns.GetHostEntry(string.Empty).AddressList;
                foreach (IPAddress address in ipv4Addresses)
                {
                    if (address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        //ipAddress = address;
                    }
                }
            }
            catch (Exception e)
            {
                log.Debug(e.ToString());
            }
            return (ipAddress);
        }
        #endregion
    }
}