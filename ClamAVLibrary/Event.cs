using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace ClamAVLibrary
{
    public class Event
    {
        #region Variables

        string _application = "";
        string _name = "";
        EventLevel _eventLevel;
        string _eventDescription = "";

        public enum EventLevel
        {
            Critical = 1,
            Error = 2,
            Warning = 3,
            Information = 4,
            Emergency =5,
            Alert = 6,
            Notification = 7
        }

        #endregion
        #region Constructor

        Event()
        {
        }

        #endregion
        #region Proprties

        public string Application
        {
            set
            {
                _application = value;
            }
            get
            {
                return (_application);
            }
        }

        public string Description
        {
            set
            {
                _eventDescription = value;
            }
            get
            {
                return (_eventDescription);
            }
        }

        public EventLevel Level
        {
            set
            {
                _eventLevel = value;
            }
            get
            {
                return (_eventLevel);
            }
        }

        public string Name
        {
            set
            {
                _name = value;
            }
            get
            {
                return (_name);
            }
        }

        #endregion
        #region Methods
        #endregion
    }
}
