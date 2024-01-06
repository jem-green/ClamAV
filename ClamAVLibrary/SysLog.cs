using TracerLibrary;
using System.Net.Sockets;
using System.Diagnostics;
using System;

namespace ClamAVLibrary
{
    public class SysLog : Notify, INotify
    {
        #region Fields

        private int _port = 514;                                             // The port to use
        private string _host = "";                                           // the name to message
        Message.FacilityType _facility = Message.FacilityType.Kernel;        //
        Message.SeverityType _severity = Message.SeverityType.Emergency;     //
        PriorityOrder _priority = PriorityOrder.Normal;                      // The notification priority     

        public enum ProtocolFormat : int
        {
            Rfc3164 = 0,
            Rfc5424 = 1
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
                return ((int)_priority);
            }
            set
            {
                _priority = PriorityLookup(value.ToString());
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
            return (Notify(applicationName, eventName, description, PriorityOrder.Normal));
        }

        /// <summary>
        /// Send out the notification
        /// </summary>
        /// <returns></returns>
        public int Notify(string applicationName, string eventName, string description, PriorityOrder priority)
        {
            Debug.WriteLine("In Notify()");

            TraceInternal.TraceInformation("Send SysLog Message");
            TraceInternal.TraceVerbose("ApplicationName=" + applicationName);
            TraceInternal.TraceVerbose("EventName=" + eventName);
            TraceInternal.TraceVerbose("Description=" + description);
            TraceInternal.TraceVerbose("Priority=" + priority);
            ErrorCode error = ErrorCode.None;

            // Translate priority into severity

            Message.SeverityType severity = Message.SeverityType.Null;
            switch (priority)
            {
                case PriorityOrder.Low:
                    {
                        severity = Message.SeverityType.Info;
                        break;
                    }
                case PriorityOrder.Moderate:
                    {
                        severity = Message.SeverityType.Notice;
                        break;
                    }
                case PriorityOrder.Normal:
                    {
                        severity = Message.SeverityType.Warning;
                        break;
                    }
                case PriorityOrder.High:
                    {
                        severity = Message.SeverityType.Alert;
                        break;
                    }
                case PriorityOrder.Emergency:
                    {
                        severity = Message.SeverityType.Emergency;
                        break;
                    }
            }

            // Syslog doesn't use priority as this is a combination of severity and facility

            Rfc3164 message = new Rfc3164
            {
                Severity = severity,
                Facility = _facility,
                HostName = System.Environment.MachineName.ToUpper()
            };

            string tag;
            try
            {
                tag = string.Format("{0}[{1}]", applicationName,eventName);

                if (tag.Length > 32)
                {
                    tag = eventName;
                    if (tag.Length > 32)
                    {
                        tag = eventName.Replace(" ", "");
                        if (tag.Length > 32)
                        {
                            tag = message.Tag.Substring(0, 32);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TraceInternal.TraceError(ex.ToString());
                tag = "";
            }
            message.Tag = tag;

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
                        if (severity == Message.SeverityType.Emergency)
                        {
                            TraceInternal.TraceWarning("Sent -> " + message);
                        }
                        else
                        {
                            TraceInternal.TraceError("Sent -> " + message);
                        }
                        error = ErrorCode.None;
                    }
                }
            }
            catch (SocketException e)
            {
                TraceInternal.TraceError(e.ToString());
                error = ErrorCode.General;
            }
            Debug.WriteLine("Out Notify()");
            return ((int)error);
        }

        /// <summary>
        /// Verify the API key
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
            string errorDescripton;
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
