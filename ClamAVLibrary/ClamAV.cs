using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using log4net;

namespace ClamAVLibrary
{
    public class ClamAV
    {
        #region Variables

        private OperatingMode _mode = OperatingMode.combined;
        private DataLocation _logs = DataLocation.Local;
        private DataLocation _database = DataLocation.Local;
        private Schedule _update;
        private List<Schedule> _scans;

        public enum DataLocation : int
        {
            Program = 0,
            App = 1,
            Local = 2,
            Roaming = 3
        }

        public enum OperatingMode : int
        {
            none = -1,
            client = 1,
            server = 2,
            combined = 3
        }

        #endregion

        #region Constructors
        #endregion

        #region Properties

        OperatingMode Mode
        {
            get
            {
                return (_mode);
            }
            set
            {
                _mode = value;
            }
        }

        DataLocation Logs
        {
            get
            {
                return (_logs);
            }
            set
            {
                _logs = value;
            }
        }

        DataLocation Database
        {
            get
            {
                return (_database);
            }
            set
            {
                _database = value;
            }
        }

        Schedule Update
        {
            get
            {
                return (_update);
            }
            set
            {
                _update = value;
            }
        }

        List<Schedule> Scans
        {
            get
            {
                return (_scans);
            }
            set
            {
                _scans = value;
            }
        }

        #endregion

        #region Methods
        #endregion

        #region Private
        #endregion

    }
}