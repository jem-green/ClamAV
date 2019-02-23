﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ClamAVLibrary
{
    public class ScheduleEventArgs : EventArgs
    {
        #region Variables
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

