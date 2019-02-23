using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ClamAVLibrary
{
    public class NotificationEventArgs : EventArgs
    {
        #region Variables
        private Notification _notification;
        #endregion
        #region Constructor
        public NotificationEventArgs(Notification notification)
        {
            this._notification = notification;
        }
        #endregion
        #region Properties

        public Notification Notification
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

