using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ClamAVLibrary
{
    public class SocketEventArgs : EventArgs
    {
        #region Variables
        private string ip = "127.0.0.1";
        private Beat beat;
        #endregion
        #region Constructor
        public SocketEventArgs(string ip, Beat beat)
        {
            this.ip = ip;
            this.beat = beat;
        }
        #endregion
        #region Properties
        public string IP
        {
            set
            {
                ip = value;
            }
            get
            {
                return (ip);
            }
        }

        public Beat Beat
        {
            set
            {
                beat = value;
            }
            get
            {
                return (beat);
            }
        }
        #endregion
    }
}

