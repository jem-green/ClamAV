using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml;

namespace ClamAVLibrary
{
    public class ClamAV
    {
        #region Variables
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        AutoResetEvent signal;
        private object _threadLock = new object();
        private Thread _thread;
        private bool _disposed = false;
        private ManualResetEvent _eventTerminate = new ManualResetEvent(false);

        string _database = "";
        string _configuration = "";
        Clamd _clamd;
        FreshClam _freshClam;
        Clamscan _clamScan;

        bool running = true;

        #endregion
        #region Constructors

        public ClamAV()
        {
            log.Debug("InClamAV()");
            log.Debug("Out ClamAV()");
        }

        #endregion
        #region Properties

        //public string Host
        //{
        //    set
        //    {
        //        host = value;
        //        hostIp = GetIPAddress(host);
        //    }
        //    get
        //    {
        //        return (host);
        //    }
        //}

        //public int Interval
        //{
        //    set
        //    {
        //        interval = value;
        //    }
        //    get
        //    {
        //        return (Interval);
        //    }
        //}

        //public int Port
        //{
        //    set
        //    {
        //        port = value;
        //    }
        //    get
        //    {
        //        return (port);
        //    }
        //}

        #endregion
        #region Methods

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
            log.Debug("InStart()");

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
            log.Debug("InStop()");

            if (_disposed)
                throw new ObjectDisposedException(null, "This instance is already disposed");

            signal.Set();   // force out of the waitOne
            running = false;

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

        /// <summary>
        /// 
        /// </summary>
        private void MonitorThread()
        {
            log.Debug("InMonitorThread()");

            try
            {
                ClamAVLoop();
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
        public void ClamAVLoop()
        {
            log.Debug("InClamAVLoop()");

            // process heartbeats at the defined intervals

            signal = new AutoResetEvent(false);
            running = true;

            // Run the clamscan

            // send heartbeat
            SendClamAV();
            do
            {
                //signal.WaitOne(interval);
                // send heartbeat
                SendClamAV();
            }
            while (running == true);

            log.Debug("Out ClamAVLoop()");
        }

        private void LaunchClamd()
        {
            log.Debug("In LaunchClamd()");

            //Process clamd = new Process();

            log.Debug("Out LaunchClamd()");
        }


        private void SendClamAV()
        {
            log.Debug("In SendClamAV()");
            //System.Net.Sockets.Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            //try
            //{
            //    IPEndPoint endPoint = new IPEndPoint(hostIp, port);
            //    beat.UpdateCounter();
            //    socket.SendTo(beat.EncodeBeat(), endPoint);
            //    log.Info("ClamAV sent to " + hostIp + ":" + port);
            //}
            //catch(SocketException se)
            //{
            //    log.Debug("Socket errored " + se.Message);
            //}
            //catch (Exception e)
            //{
            //    log.Error(e.ToString());
            //}
            log.Debug("Out SendClamAV()");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="path"></param>
        public void FromXML(string filename, string path)
        {
            log.Debug("In FromXML");
            try
            {
                // Point to the file

                string fileLocation = System.IO.Path.Combine(path, filename);
                try
                {
                    FileStream fs = new FileStream(fileLocation, FileMode.Open);

                    // Pass the parameters in

                    XmlReaderSettings xmlSettings = new XmlReaderSettings
                    {

                        // Enable <!ENTITY to be expanded
                        // <!ENTITY chap1 SYSTEM "chap1.xml">
                        // &chap1;

                        ProhibitDtd = false
                    };

                    // Open the file and pass in the settings

                    try
                    {
                        Stack<string> stack = new Stack<string>();
                        string element = "";
                        string text = "";
                        string current = "";    // Used to flag what level we are at
                        int level = 1;          // Indentation level

                        XmlReader xmlReader = XmlReader.Create(fs, xmlSettings);
                        while (xmlReader.Read())
                        {
                            switch (xmlReader.NodeType)
                            {
                                #region Element
                                case XmlNodeType.Element:
                                    {
                                        element = xmlReader.LocalName.ToLower();

                                        if (!xmlReader.IsEmptyElement)
                                        {
                                            log.Info(Level(level) + "<" + element + ">");
                                            level = level + 1;
                                        }
                                        else
                                        {
                                            log.Info(Level(level) + "<" + element + "/>");
                                        }

                                        stack.Push(current);
                                        current = element;
                                        break;

                                    }
                                #endregion
                                #region EndElement
                                case XmlNodeType.EndElement:
                                    {
                                        element = xmlReader.LocalName;
                                        level = level - 1;
                                        log.Info(Level(level) + "</" + element + ">");
                                        current = stack.Pop();
                                        break;
                                    }
                                #endregion
                                #region Text
                                case XmlNodeType.Text:
                                    {
                                        text = xmlReader.Value;
                                        text = text.Replace("\t", "");
                                        text = text.Replace("\n", "");
                                        text = text.Trim();
                                        log.Info(Level(level) + "  " + text);

                                        switch (current)
                                        {
                                            case "host":
                                                {
                                                    //host = text;
                                                    //hostIp = GetIPAddress(host);
                                                    break;
                                                }
                                            case "instance":
                                                {
                                                    try
                                                    {
                                                        //beat.Instance = Convert.ToByte(text);
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "interval":
                                                {
                                                    try
                                                    {
                                                        //Interval = Convert.ToInt32(text) * 1000; // convert to milliseconds
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "interface":
                                                {
                                                    try
                                                    {
                                                        //beat.Interface = text;
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "node":
                                                {
                                                    try
                                                    {
                                                        //beat.NodeId = text;
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "port":
                                                {
                                                    try
                                                    {
                                                        //port = Convert.ToInt32(text);
                                                    }
                                                    catch { };
                                                    break;
                                                }                 
                                        }
                                        break;
                                    }
                                #endregion
                                #region Entity
                                case XmlNodeType.Entity:
                                    break;
                                #endregion
                                case XmlNodeType.EndEntity:
                                    break;
                                case XmlNodeType.Whitespace:
                                    break;
                                case XmlNodeType.Comment:
                                    break;
                                case XmlNodeType.Attribute:
                                    break;
                                default:
                                    log.Info(xmlReader.NodeType);
                                    break;
                            }
                        }

                        xmlReader.Close();  // Force the close
                        xmlReader = null;
                    }
                    catch (Exception ex)
                    {
                        log.Warn("XML Error " + ex.Message);
                    }
                    fs.Close();
                    fs.Dispose();   // Force the dispose as it was getting left open
                }
                catch (FileNotFoundException ex)
                {
                    log.Warn("File Error " + ex.Message);
                }
                catch (Exception ex)
                {
                    log.Warn("File Error " + ex.Message);
                }

            }
            catch (Exception e)
            {
                log.Error("Other Error " + e.Message);
            }
            log.Debug("Out FromXML");
        }
        #endregion
        #region Private

        private IPAddress GetIPAddress(string host)
        {
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            try
            {
                IPHostEntry hostEntry = Dns.GetHostEntry(host);
                if (hostEntry.AddressList.Length > 0)
                {
                    if (hostEntry.AddressList[0].AddressFamily == AddressFamily.InterNetwork)
                    {
                        ip = hostEntry.AddressList[0];
                    }
                }
            }
            catch
            {
                try
                {
                    ip = IPAddress.Parse(host);
                }
                catch { };
            }
            return (ip);
        }

        private string Level(int level)
        {
            string text = "";
            for (int i = 1; i < level; i++)
            {
                text = text + "  ";
            }
            return (text);
        }
        #endregion
    }
}
