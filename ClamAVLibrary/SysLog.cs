using System.Net.Sockets;
using log4net;

namespace ClamAVLibrary
{
    public class SysLog: INotify
    {
        #region Variables

        protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int port = 514;                                             // The port to use
        private string host = "";                                           // the name to ping
        Message.FacilityType facility = Message.FacilityType.Kernel;        //
        Message.SeverityType severity = Message.SeverityType.Emergency;     //
        PriorityOrder priority = PriorityOrder.normal;                      // The notification priority     

        public enum ProtocolFormat : int
        {
            rfc3164 = 0,
            rfc5424 = 1
        }

        public enum ErrorCode : int
        {
            None = 0,
            General = 1,
            Socket = 2
        }

        public enum PriorityOrder
        {
            low = -2,
            moderate = -1,
            normal = 0,
            high = 1,
            emergency = 2
        }

        #endregion
        #region Constructors

        public SysLog()
        {
        }

        public SysLog(string host)
        {
            this.host = host;
        }

        public SysLog(string host, int port)
        {
            this.host = host;
            this.port = port;
        }

        #endregion
        #region Properties

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
            get
            {
                return (host);
            }
            set
            {
                host = value;
            }
        }

        public int Port
        {
            get
            {
                return (port);
            }
            set
            {
                port = value;
            }
        }

        public int Priority
        {
            get
            {
                return ((int)priority);
            }
            set
            {
                priority = PriorityLookup(value.ToString());
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

        #endregion
        #region Methods

        public int Notify(string applicationName, string eventName, string description)
        {
            return (Notify(applicationName, eventName, description, 0));
        }	

        /// <summary>
        /// Send out the notifiction
        /// </summary>
        /// <returns></returns>
        public int Notify(string applicationName, string eventName, string description, int priority)
        {
            ErrorCode error = ErrorCode.None;

            Rfc3164 message = new Rfc3164
            {
                Severity = this.severity,
                Facility = this.facility,
                Host = host
            };
            message.Tag = string.Format("{0}[{1}]", eventName, applicationName);
            message.Content = description;
            try
            {
                UdpClient sendSocket = new UdpClient(host, port);
                string payload = message.ToString();
                byte[] output = System.Text.ASCIIEncoding.ASCII.GetBytes(payload);

                // Make sure the final buffer is less then 4096 bytes and if so then send the data
                if (output.Length < 4096)
                {

                    int sent = sendSocket.Send(output, output.Length);
                    sendSocket.Close();
                    if (sent <= 0)
                    {
                        error = ErrorCode.General;
                    }
                }
            }
            catch (SocketException e)
            {
                log.Error(e.ToString());
                error = ErrorCode.General;
            }
            return ((int)error);
        }

        /// <summary>
        /// Verifiy the API key
        /// </summary>
        /// <returns></returns>
        public int Verify()
        {
            return (0);
        }
		
	    public string ErrorDescription(int error)
        {
            string errorDescripton = "";
            ErrorCode errorCode = (ErrorCode)error;
            switch (errorCode)
            {
                case ErrorCode.None:
                    {
                        errorDescripton = "Notification submitted.";
                        break;
                    }
                default:
                    {
                        errorDescripton = "General error " + error;
                        break;
                    }
            }
            return (errorDescripton);
        }

        public PriorityOrder PriorityLookup(string priorityName)
        {
            PriorityOrder priority = 0;

            string lookup = priorityName;
            if (priorityName.Length > 2)
            {
                lookup = priorityName.ToUpper();
            }

            switch (lookup)
            {
                case "-2":
                case "LOW":
                case "L":
                    priority = PriorityOrder.low;
                    break;
                case "-1":
                case "MODERATE":
                case "M":
                    priority = PriorityOrder.moderate;
                    break;
                case "0":
                case "NORMAL":
                case "N":
                    priority = PriorityOrder.normal;
                    break;
                case "1":
                case "HIGH":
                case "H":
                    priority = PriorityOrder.high;
                    break;
                case "2":
                case "EMERGENCY":
                case "E":
                    priority = PriorityOrder.emergency;
                    break;
            }
            return (priority);
        }
        #endregion
    }
}