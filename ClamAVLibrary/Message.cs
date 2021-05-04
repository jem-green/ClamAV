using log4net;
using System;

namespace ClamAVLibrary
{
    /// <summary>
    /// Syslog base message class
    /// </summary>
    public abstract class Message : ICloneable
    {
        #region Fields

        protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public enum FacilityType : int
        {
            Null = -1,      // Null: not set
            Kernel = 0,     // kernel messages
            User = 1,       // user-level messages
            Mail = 2,       // mail system
            System = 3,     // system daemons
            Security = 4,   // security/authorization messages (note 1)
            Internally = 5, // messages generated internally by syslogd
            Printer = 6,    // line printer subsystem
            News = 7,       // network news subsystem
            UUCP = 8,       // UUCP subsystem
            Cron = 9,       // clock daemon (note 2) changed to cron
            Security2 = 10, // security/authorization messages (note 1)
            Ftp = 11,       // FTP daemon
            Ntp = 12,       // NTP subsystem
            Audit = 13,     // log audit (note 1)
            Alert = 14,     // log alert (note 1)
            Clock2 = 15,    // clock daemon (note 2)
            Local0 = 16,    // local use 0  (local0)
            Local1 = 17,    // local use 1  (local1)
            Local2 = 18,    // local use 2  (local2)
            Local3 = 19,    // local use 3  (local3)
            Local4 = 20,    // local use 4  (local4)
            Local5 = 21,    // local use 5  (local5)
            Local6 = 22,    // local use 6  (local6)
            Local7 = 23,    // local use 7  (local7)
        }

        public enum SeverityType : int
        {
            Null = -1,      // Null: not set
            Emergency = 0,  // Emergency: system is unusable
            Alert = 1,      // Alert: action must be taken immediately
            Critical = 2,   // Critical: critical conditions
            Error = 3,      // Error: error conditions
            Warning = 4,    // Warning: warning conditions
            Notice = 5,     // Notice: normal but significant condition
            Info = 6,       // Informational: informational messages
            Debug = 7,      // Debug: debug-level messages
        }

        public enum ProtocolFormat
        {
            None = -1,
            Rfc3164 = 0,
            Rfc5424 = 1
        }

        protected FacilityType _facility = FacilityType.Null;
        protected SeverityType _severity = SeverityType.Null;
        protected string _content = "";
        protected DateTime _timeStamp = DateTime.Now;
        protected string _hostName = "";
        protected string _tag = "";
        protected ProtocolFormat _protocol = ProtocolFormat.None;

        #endregion
        #region Properties

        public FacilityType Facility
        {
            set
            {
                if (value != FacilityType.Null) // Stop overring with null data.
                {
                    _facility = value;
                }
            }
            get
            {
                return (_facility);
            }
        }

        public SeverityType Severity
        {
            set
            {
                if (value != SeverityType.Null)
                {
                    _severity = value;
                }
            }
            get
            {
                return (_severity);
            }
        }

        public string Content
        {
            set
            {
                _content = value;
            }
            get
            {
                return (_content);
            }
        }

        public DateTime TimeStamp
        {
            set
            {
                _timeStamp = value;
            }
            get
            {
                return (_timeStamp);
            }
        }

        public string HostName
        {
            set
            {
                if (value.Length > 0)
                {
                    _hostName = value;
                }
            }
            get
            {
                return (_hostName);
            }
        }

        public string Tag
        {
            set
            {
                if (value.Length > 0)
                {
                    _tag = value;
                    if (_tag.Length > 32)
                    {
                        throw new Exception("TAG name too long > 32 characters");
                    }
                }
            }
            get
            {
                return (_tag);
            }
        }

        #endregion
        #region Methods

        /// <summary>
        /// Lookup the log severity
        /// </summary>
        /// <param name="severityityName"></param>
        /// <returns></returns>
        public static SeverityType SeverityLookup(string severityityName)
        {
            SeverityType severityType = SeverityType.Emergency;

            if (Int32.TryParse(severityityName, out int severtyValue))
            {
                severityType = (SeverityType)severtyValue;
            }
            else
            {
                string lookup = severityityName;
                if (severityityName.Length > 1)
                {
                    lookup = severityityName.ToUpper();
                }

                switch (lookup)
                {
                    case "A":
                    case "ALERT":
                        {
                            severityType = SeverityType.Alert;
                            break;
                        }
                    case "C":
                    case "CRIT":
                    case "CRITICAL":
                        {
                            severityType = SeverityType.Critical;
                            break;
                        }
                    case "D":
                    case "DEBUG":
                        {
                            severityType = SeverityType.Debug;
                            break;
                        }
                    case "E":
                    case "EMERG":
                    case "EMERGENCY":
                        {
                            severityType = SeverityType.Emergency;
                            break;
                        }
                    case "e":
                    case "ERR":
                    case "ERROR":
                        {
                            severityType = SeverityType.Error;
                            break;
                        }
                    case "I":
                    case "INFO":
                    case "INFORMATIONAL":
                        {
                            severityType = SeverityType.Info;
                            break;
                        }
                    case "N":
                    case "NOTICE":
                        {
                            severityType = SeverityType.Notice;
                            break;
                        }
                    case "W":
                    case "WARN":
                    case "WARNING":
                        {
                            severityType = SeverityType.Warning;
                            break;
                        }
                }
            }
            return (severityType);
        }

        /// <summary>
        /// Lookup the facilty
        /// </summary>
        /// <param name="facilityName"></param>
        /// <returns></returns>
        public static FacilityType FacilityLookup(string facilityName)
        {
            FacilityType facilityType = FacilityType.Kernel;

            if (Int32.TryParse(facilityName, out int facilityValue))
            {
                facilityType = (FacilityType)facilityValue;
            }
            else
            {
                string lookup = facilityName;
                if (facilityName.Length > 1)
                {
                    lookup = facilityName.ToUpper();
                }

                switch (lookup)
                {
                    case "A":
                    case "ALERT":
                        facilityType = FacilityType.Alert;
                        break;
                    case "a":
                    case "AUDIT":
                        facilityType = FacilityType.Audit;
                        break;
                    case "C":
                    case "CLOCK2":
                        facilityType = FacilityType.Clock2;
                        break;
                    case "c":
                    case "CRON":
                        facilityType = FacilityType.Cron;
                        break;
                    case "F":
                    case "FTP":
                        facilityType = FacilityType.Ftp;
                        break;
                    case "I":
                    case "INTERNALLY":
                        facilityType = FacilityType.Internally;
                        break;
                    case "K":
                    case "KERNEL":
                        facilityType = FacilityType.Kernel;
                        break;
                    case "L":
                    case "L0":
                    case "LOCAL0":
                        facilityType = FacilityType.Local0;
                        break;
                    case "L1":
                    case "LOCAL1":
                        facilityType = FacilityType.Local1;
                        break;
                    case "L2":
                    case "LOCAL2":
                        facilityType = FacilityType.Local2;
                        break;
                    case "L3":
                    case "LOCAL3":
                        facilityType = FacilityType.Local3;
                        break;
                    case "L4":
                    case "LOCAL4":
                        facilityType = FacilityType.Local4;
                        break;
                    case "L5":
                    case "LOCAL5":
                        facilityType = FacilityType.Local5;
                        break;
                    case "L6":
                    case "LOCAL6":
                        facilityType = FacilityType.Local6;
                        break;
                    case "L7":
                    case "LOCAL7":
                        facilityType = FacilityType.Local7;
                        break;
                    case "M":
                    case "MAIL":
                        facilityType = FacilityType.Mail;
                        break;
                    case "N":
                    case "NEWS":
                        facilityType = FacilityType.News;
                        break;
                    case "n":
                    case "NTP":
                        facilityType = FacilityType.Ntp;
                        break;
                    case "P":
                    case "PRINTER":
                        facilityType = FacilityType.Printer;
                        break;
                    case "S":
                    case "S1":
                    case "SECURITY":
                        facilityType = FacilityType.Security;
                        break;
                    case "S2":
                    case "SECURITY2":
                        facilityType = FacilityType.Security2;
                        break;
                    case "s":
                    case "SYSTEM":
                        facilityType = FacilityType.System;
                        break;
                    case "U":
                    case "USER":
                        facilityType = FacilityType.User;
                        break;
                    case "u":
                    case "UUCP":
                        facilityType = FacilityType.UUCP;
                        break;
                }
            }
            return (facilityType);
        }

        public static ProtocolFormat ProtocolLookup(string ProtocolName)
        {
            ProtocolFormat protocol = ProtocolFormat.None;

            if (Int32.TryParse(ProtocolName, out int facilityValue))
            {
                protocol = (ProtocolFormat)facilityValue;
            }
            else
            {
                string lookup = ProtocolName;
                if (ProtocolName.Length > 1)
                {
                    lookup = ProtocolName.ToUpper();
                }

                switch (lookup)
                {
                    case "RFC3164":
                    case "3164":
                    case "BSD":
                        protocol = ProtocolFormat.Rfc3164;
                        break;
                    case "RFC5424":
                    case "5424":
                    case "AUDIT":
                        protocol = ProtocolFormat.Rfc5424;
                        break;
                }
            }
            return (protocol);
        }

        public string Format(string format)
        {
            string content = format;
            content = content.Replace("[severity]", _severity.ToString());
            content = content.Replace("[facility]", _facility.ToString());
            content = content.Replace("[host]", _hostName);
            content = content.Replace("[timestamp]", _timeStamp.ToString());
            content = content.Replace("[date]", _timeStamp.ToString("dd/MM/yyyy"));
            content = content.Replace("[time]", _timeStamp.ToString("H:mm"));
            content = content.Replace("[tag]", _tag);
            return (content);
        }

        public abstract bool Parse(string host, string message);

        public object Clone()
        {
            return (this.MemberwiseClone());
        }

        public abstract override string ToString();

        #endregion
    }
}
