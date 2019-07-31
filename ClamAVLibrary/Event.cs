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

        //Log levels 
     
        public enum EventLevel
        {
            Null = -1,
            Emergency = 0,
            Alert = 1,
            Critical = 2,
            Error = 3,
            Warning = 4,
            Notification = 5,
            Information = 6     
        }

        #endregion
        #region Constructor

        public Event()
        {
        }

        public Event(string name, string application, string description, EventLevel level)
        {
            _name = name;
            _application = application;
            _eventDescription = description;
            _eventLevel = level;      
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
