using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using log4net;

namespace ClamAVLibrary
{
    public class Forwarder
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private string id = "";
        ForwarderType type = ForwarderType.SYSLOG;
        string key = "";
        string username = "";
        string password = "";
        int port = 0;
        string _host = "";
        IPAddress _hostIp = IPAddress.Parse("127.0.0.1");        // default to loopback
        string from = "";
        string to = "";
        string encrypt = "";
        Message.FacilityType facility = Message.FacilityType.Kernel;
        Message.SeverityType severity = Message.SeverityType.Alert;
        INotify notifier = null;
        List<NameKey> _keys = null;
        
        public enum ForwarderType
        {
            Corral = 0,
            Growl = 1,
            None = 2,
            NotifyMyAndroid = 3,
            NotifyMyDevice = 4,
            Prowl = 5,
            SMTP = 6,
            SYSLOG = 7,
        }

        #endregion
        #region Constructor

        public Forwarder()
        {
            log.Debug("In Forwarder()");
            _keys = new List<NameKey>();
            log.Debug("Out Forwarder()");
        }

        #endregion
        #region Properties

        public string Encrypt
        {
            get
            {
                return (encrypt);
            }
            set
            {
                encrypt = value;
            }
        }

        public string From
        {
            set
            {
                from = value;
            }
            get
            {
                return (from);
            }
        }

        public Message.FacilityType Facility
        {
            set
            {
                facility = value;
            }
            get
            {
                return (facility);
            }
        }

        public string Host
        {
            set
            {
                _host = value;
                _hostIp = GetIPAddress(_host);
            }
            get
            {
                return (_host);
            }
        }

        public string Id
        {
            set
            {
                id = value;
            }
            get
            {
                return (id);
            }
        }

        public string Key
        {
            set
            {
                key = value;
            }
            get
            {
                return (key);
            }
        }
        public List<NameKey> Keys
        {
            set
            {
                _keys = value;
            }
            get
            {
                return (_keys);
            }
        }

        public INotify Notifier
        {
            set
            {
                notifier = value;
            }
            get
            {
                return (notifier);
            }
        }

        public string Password
        {
            set
            {
                password = value;
            }
            get
            {
                return (password);
            }
        }

        public int Port
        {
            set
            {
                port = value;
            }
            get
            {
                return (port);
            }
        }

        public Message.SeverityType Severity
        {
            set
            {
                severity = value;
            }
            get
            {
                return (severity);
            }
        }

        public string To
        {
            set
            {
                to = value;
            }
            get
            {
                return (to);
            }
        }

        public ForwarderType Type
        {
            set
            {
                type = value;
            }
            get
            {
                return (type);
            }
        }

        public string Username
        {
            set
            {
                username = value;
            }
            get
            {
                return (username);
            }
        }

        #endregion
        #region Methods
        public static ForwarderType ForwarderLookup(string forwarderName)
        {
            ForwarderType forwarderType = ForwarderType.Corral;

            if (Int32.TryParse(forwarderName, out int forwarderValue))
            {
                forwarderType = (ForwarderType)forwarderValue;
            }
            else
            {
                string lookup = forwarderName;
                if (forwarderName.Length > 1)
                {
                    lookup = forwarderName.ToUpper();
                }

                switch (lookup)
                {
                    case "C":
                    case "CORRAL":
                        {
                            forwarderType = Forwarder.ForwarderType.Corral;
                            break;
                        }
                    case "G":
                    case "GROWL":
                        {
                            forwarderType = Forwarder.ForwarderType.Growl;
                            break;
                        }
                    case "N":
                    case "NMA":
                    case "NOTIFYMYANDROID":
                    case "NOTIFY MY ANDROID":
                        {
                            forwarderType = Forwarder.ForwarderType.NotifyMyAndroid;
                            break;
                        }
                    case "n":
                    case "NMD":
                    case "NOTIFYMYDEVICE":
                    case "NOTIFY MY DEVICE":
                        {
                            forwarderType = Forwarder.ForwarderType.NotifyMyDevice;
                            break;
                        }
                    case "P":
                    case "PROWL":
                        {
                            forwarderType = Forwarder.ForwarderType.Prowl;
                            break;
                        }
                    case "s":
                    case "SMTP":
                        {
                            forwarderType = Forwarder.ForwarderType.SMTP;
                            break;
                        }
                    case "S":
                    case "SYSLOG":
                        {
                            forwarderType = Forwarder.ForwarderType.SYSLOG;
                            break;
                        }
                }
            }
            return (forwarderType);
        }

        public void Execute(Message message)
        {
            log.Debug("In Execute");
            // need to translate beween syslog and other messaging
            // do we let the notifyer determine this.
            log.Info("Notify " + id);
            notifier.Notify(message.HostName, message.Tag, message.Content);
            log.Debug("Out Execute");
        }
        #endregion
        #region Private
        private static IPAddress GetIPAddress(string host)
        {
            log.Debug("In GetIPAddress");
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
            log.Debug("Out GetIPAddress");
            return (ip);
        }
        #endregion
    }
}
