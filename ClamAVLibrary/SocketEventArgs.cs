using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ClamAVLibrary
{
    public class NotificationEventArgs : EventArgs
    {
        #region Variables
        private Event _notification;
        #endregion
        #region Constructor
        public NotificationEventArgs(Event notification)
        {
            this._notification = notification;
        }
        #endregion
        #region Properties

        public Event Notification
        {
            set
            {
                _notification = value;
            }
            get
            {
                return (_notification);
            }
        }
        #endregion
    }
}

