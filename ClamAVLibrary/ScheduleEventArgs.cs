using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ClamAVLibrary
{
    public class ScheduleEventArgs : EventArgs
    {
        #region Variables
        DateTime _event;
        string _message;
        #endregion
        #region Constructor
        public ScheduleEventArgs(DateTime @event, string message)
        {
            this._event = @event;
            this._message = message;
        }
        #endregion
        #region Properties
        public DateTime Event
        {
            set
            {
                _event = value;
            }
            get
            {
                return (_event);
            }
        }

        public string Message
        {
            set
            {
                _message = value;
            }
            get
            {
                return (_message);
            }
        }

        #endregion
    }
}

