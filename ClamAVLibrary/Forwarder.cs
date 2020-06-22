using log4net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ClamAVLibrary
{
    public class Forwarder : Element
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        ForwarderType _type = ForwarderType.Corral;
        string _key = "";
        string _username = "";
        string _password = "";
        int _port = 0;
        string _host = "";
        IPAddress _hostIp = IPAddress.Parse("127.0.0.1");        // default to loopback
        string _from = "";
        string _to = "";
        string _encrypt = "";
        Message.FacilityType _facility = Message.FacilityType.Kernel;
        Message.SeverityType _severity = Message.SeverityType.Alert;
        INotify _notifier = null;
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
                return (_encrypt);
            }
            set
            {
                _encrypt = value;
            }
        }

        public string From
        {
            set
            {
                _from = value;
            }
            get
            {
                return (_from);
            }
        }

        public Message.FacilityType Facility
        {
            set
            {
                _facility = value;
            }
            get
            {
                return (_facility);
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

        public string Key
        {
            set
            {
                _key = value;
            }
            get
            {
                return (_key);
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
                _notifier = value;
            }
            get
            {
                return (_notifier);
            }
        }

        public string Password
        {
            set
            {
                _password = value;
            }
            get
            {
                return (_password);
            }
        }

        public int Port
        {
            set
            {
                _port = value;
            }
            get
            {
                return (_port);
            }
        }

        public Message.SeverityType Severity
        {
            set
            {
                _severity = value;
            }
            get
            {
                return (_severity);
            }
        }

        public string To
        {
            set
            {
                _to = value;
            }
            get
            {
                return (_to);
            }
        }

        public ForwarderType Type
        {
            set
            {
                _type = value;
            }
            get
            {
                return (_type);
            }
        }

        public string Username
        {
            set
            {
                _username = value;
            }
            get
            {
                return (_username);
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

        public bool Execute(Message message)
        {
            log.Debug("In Execute");
            // need to translate beween syslog and other messaging
            // do we let the notifyer determine this.
            log.Debug("Notify " + _id);
            _notifier.Notify(message.HostName, message.Tag, message.Content);
            log.Debug("Out Execute");
            return (true);
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
