﻿using System.Net;
using System.Net.Sockets;
using log4net;

namespace ClamAVLibrary
{
    public class SysLog: Notify, INotify
    {
        #region Variables

        protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private int _port = 514;                                             // The port to use
        private string _host = "";                                           // the name to message
        Message.FacilityType _facility = Message.FacilityType.Kernel;        //
        Message.SeverityType _severity = Message.SeverityType.Emergency;     //
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

        #endregion
        #region Constructors

        public SysLog()
        {
        }

        public SysLog(string host)
        {
            this._host = host;
        }

        public SysLog(string host, int port)
        {
            this._host = host;
            this._port = port;
        }

        #endregion
        #region Properties

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
            get
            {
                return (_host);
            }
            set
            {
				if (value.Length > 0)
                {
                _host = value;
				}
            }
        }

        public int Port
        {
            get
            {
                return (_port);
            }
            set
            {
				if (_port != 0)
                {
                	_port = value;
				}
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
                _severity = value;
            }
            get
            {
                return (_severity);
            }
        }

        #endregion
        #region Methods

        public int Notify(string applicationName, string eventName, string description)
        {
            return (Notify(applicationName, eventName, description, PriorityOrder.normal));
        }	

        /// <summary>
        /// Send out the notifiction
        /// </summary>
        /// <returns></returns>
        public int Notify(string applicationName, string eventName, string description, PriorityOrder priority)
        {
            ErrorCode error = ErrorCode.None;

            // Translate priority into severyity

            Message.SeverityType severity = Message.SeverityType.Null;
            switch (priority)
            {
                case PriorityOrder.low:
                    {
                        severity = Message.SeverityType.Info;
                        break;
                    }
                case PriorityOrder.moderate:
                    {
                        severity = Message.SeverityType.Notice;
                        break;
                    }
                case PriorityOrder.normal:
                    {
                        severity = Message.SeverityType.Warning;
                        break;
                    }
                case PriorityOrder.high:
                    {
                        severity = Message.SeverityType.Alert;
                        break;
                    }
                case PriorityOrder.emergency:
                    {
                        severity = Message.SeverityType.Emergency;
                        break;
                    }
            }

            // Syslog doesnt use priority as this is a combination of sevirty and facility

            Rfc3164 message = new Rfc3164
            {
                Severity = severity,
                Facility = _facility,
                HostName = System.Environment.MachineName.ToUpper()
            };

            message.Tag = string.Format("{0}[{1}]", eventName, applicationName);
            message.Content = description;
            try
            {
                UdpClient sendSocket = new UdpClient(_host, _port);
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
					else
					{
						error = ErrorCode.None;
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

        public int Register(string applicationName, string eventName)
        {
            return (0);
        }

        public int Subscribe()
        {
            return (0);
        }

        public string ErrorDescription(int error)
        {
            return (ErrorDescription((ErrorCode)error));
        }

        private static string ErrorDescription(ErrorCode errorCode)
        {
            string errorDescripton = "";
            switch (errorCode)
            {
                case ErrorCode.None:
                    {
                        errorDescripton = "Notification submitted.";
                        break;
                    }
                default:
                    {
                        errorDescripton = "General error " + (int)errorCode;
                        break;
                    }
            }
            return (errorDescripton);
        }

        #endregion
        #region Private

        #endregion
    }
}
 