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
        ForwaderType type = ForwaderType.Growl;
        string key = "";
        string username = "";
        string password = "";
        int port = 0;
        string host = "";
        IPAddress hostIp = IPAddress.Parse("127.0.0.1");        // default to loopback
        string from = "";
        string to = "";
        string encrypt = "";
        Message.FacilityType facility = Message.FacilityType.Kernel;
        Message.SeverityType severity = Message.SeverityType.Alert;
        INotify notifier = null;
        
        public enum ForwaderType
        {
            Growl = 0,
            None = 1,
            NMA = 2,
            Prowl = 3,
            SMTP = 4,
            SYSLOG = 5
        }

        #endregion
        #region Constructor
        public Forwarder()
        {
            log.Debug("In Forwarder()");
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
                host = value;
                hostIp = GetIPAddress(host);
            }
            get
            {
                return (host);
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

        public ForwaderType Type
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
        #endregion
    }
}
