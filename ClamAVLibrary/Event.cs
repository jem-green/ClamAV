using System;
using System.Diagnostics;

namespace ClamAVLibrary
{
    public class Event
    {
        #region Fields

        string _application = "";
        string _name = "";
        EventLevel _eventLevel;
        string _eventDescription = "";
        object _eventData;

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

        public Event(string name, string application, string description, object data)
        {
            _name = name;
            _application = application;
            _eventDescription = description;
            _eventData = data;
        }

        public Event(string name, string application, string description, EventLevel level, object data)
        {
            _name = name;
            _application = application;
            _eventDescription = description;
            _eventLevel = level;
            _eventData = data;
        }

        #endregion
        #region Properties

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

        public object Data
        {
            set
            {
                _eventData = value;
            }
            get
            {
                return (_eventData);
            }
        }

        #endregion
        public EventLevel EventLookup(string eventLevelName)
        {

            EventLevel eventLevel = EventLevel.Emergency;

            if (Int32.TryParse(eventLevelName, out int severtyValue))
            {
                eventLevel = (EventLevel)severtyValue;
            }
            else
            {
                string lookup = eventLevelName;
                if (eventLevelName.Length > 1)
                {
                    lookup = eventLevelName.ToUpper();
                }

                switch (lookup)
                {
                    case "A":
                    case "ALERT":
                        {
                            eventLevel = EventLevel.Alert;
                            break;
                        }
                    case "C":
                    case "CRIT":
                    case "CRITICAL":
                        {
                            eventLevel = EventLevel.Critical;
                            break;
                        }
                    case "E":
                    case "EMERG":
                    case "EMERGENCY":
                        {
                            eventLevel = EventLevel.Emergency;
                            break;
                        }
                    case "e":
                    case "ERR":
                    case "ERROR":
                        {
                            eventLevel = EventLevel.Error;
                            break;
                        }
                    case "I":
                    case "INFO":
                    case "INFORMATIONAL":
                        {
                            eventLevel = EventLevel.Information;
                            break;
                        }
                    case "N":
                    case "NOTICE":
                        {
                            eventLevel = EventLevel.Notification;
                            break;
                        }
                    case "W":
                    case "WARN":
                    case "WARNING":
                        {
                            eventLevel = EventLevel.Warning;
                            break;
                        }
                }
            }
            return (eventLevel);

        }
        #region Methods



        #endregion
    }
}
