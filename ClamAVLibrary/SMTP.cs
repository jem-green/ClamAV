using System;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.IO;
using log4net;

namespace GrowlLibrary
{
    public class SMTP : INotify
    {
        #region Variables

        protected static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        string from = "";                   // "administrator@solar.local";
        string to = "";                     // "jeremy@solar.local";
        string host = "MERCURY";            // "MERCURY"
        string username = "";               // 
        string password = "";               //
        int port = 25;                      //
        MailPriority priority = MailPriority.Normal;  // The notification priority
        EncryptType encrypt = EncryptType.None;     // 

        public enum EncryptType : int
        {
            None = 0,
            SSL = 1,
            TLS = 2
        }

        public enum ErrorCode : int
        {
            None = 0,
            General = 1,
        }
        #endregion
        #region Constructors
        public SMTP()
        {
        }

        #endregion

        #region Properties

        public string Encrypt
        {
            get
            {
                return (encrypt.ToString());
            }
            set
            {
                encrypt = EncryptLookup(value);
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

        public string Host
        {
            get
            {
                return (host);
            }
            set
            {
                if (value != "")
                {
                    host = value;
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
                if (port != 0)
                {
                    port = value;
                }
            }
            get
            {
                return (port);
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

        public int Notify(string applicationName, string eventName, string description)
        {
            return (Notify(applicationName, eventName, description, 0));
        }

        public int Notify(string applicationName, string eventName, string description, int priority)
        {
            ErrorCode error = ErrorCode.General;

            MailMessage message = new MailMessage(from, to)
            {
                Subject = applicationName + " - " + eventName,
                Body = description,
                Priority = PriorityLookup(priority.ToString())
            };

            //Send the message.
            SmtpClient client = new SmtpClient(host);
            // Add credentials if the SMTP server requires them.

            if (encrypt != EncryptType.None)
            {
                client.EnableSsl = true;
            }

            if ((username != "") && (password != ""))
            {
                NetworkCredential credential = new NetworkCredential(username, password);
                client.Credentials = credential;
            }
            else
            {
                client.Credentials = System.Net.CredentialCache.DefaultNetworkCredentials;
            }

            //client.DeliveryMethod = SmtpDeliveryMethod.PickupDirectoryFromIis;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;

            try
            {
                client.Send(message);
                error = ErrorCode.None;
            }
            catch (Exception ex)
            {
                log.Error(ex);
                error = ErrorCode.General;
            }

            return ((int)error);
        }

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

        /// <summary>
        /// Lookup the output format
        /// </summary>
        /// <param name="encryptName"></param>
        /// <returns></returns>
        private EncryptType EncryptLookup(string encryptName)
        {
            EncryptType encrypt = EncryptType.None;

            string lookup = encryptName;
            if (encryptName.Length > 1)
            {
                lookup = encryptName.ToUpper();
            }

            switch (lookup)
            {
                case "N":
                case "NONE":
                    {
                        encrypt = EncryptType.None;
                        break;
                    }
                case "S":
                case "SSL":
                    {
                        encrypt = EncryptType.SSL;
                        break;
                    }
                case "T":
                case "TLS":
                    {
                        encrypt = EncryptType.TLS;
                        break;
                    }
            }
            return (encrypt);
        }

        /// <summary>
        /// Lookup a priority type from the name
        /// </summary>
        /// <param name="priorityName"></param>
        /// <returns></returns>
        public MailPriority PriorityLookup(string priorityName)
        {
            MailPriority priority = MailPriority.Normal;

            string lookup = priorityName;
            if (priorityName.Length > 2)
            {
                lookup = priorityName.ToUpper();
            }

            switch (lookup)
            {
                case "-2":
                case "LOWEST":
                case "l":
                    priority = MailPriority.Low;
                    break;
                case "-1":
                case "LOW":
                case "L":
                    priority = MailPriority.Low;
                    break;
                case "0":
                case "NORMAL":
                case "N":
                    priority = MailPriority.Normal;
                    break;
                case "1":
                case "HIGH":
                case "H":
                    priority = MailPriority.High;
                    break;
                case "2":
                case "HIGHEST":
                case "h":
                    priority = MailPriority.High;
                    break;
            }
            return (priority);
        }
    }
}
