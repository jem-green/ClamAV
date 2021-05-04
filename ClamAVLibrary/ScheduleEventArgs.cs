using System;

namespace ClamAVLibrary
{
    public class ScheduleEventArgs : EventArgs
    {
        #region Fields
        DateTime _timeout;
        #endregion
        #region Constructor
        public ScheduleEventArgs(DateTime timeout)
        {
            this._timeout = timeout;
        }
        #endregion
        #region Properties
        public DateTime Timeout
        {
            set
            {
                _timeout = value;
            }
            get
            {
                return (_timeout);
            }
        }
        #endregion
    }
}

