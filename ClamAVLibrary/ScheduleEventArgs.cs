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
        #endregion
        #region Constructor
        public ScheduleEventArgs(DateTime @event)
        {
            this._event = @event;
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

        #endregion
    }
}

