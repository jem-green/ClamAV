namespace ClamAVLibrary
{
    public class Event
    {
        #region Variables

        string _application = "";
        string _name = "";
        EventLevel _eventLevel;
        string _eventDescription = "";
        EventType _type = EventType.Null;

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

        //Event types

        public enum EventType
        {
            Null = -1,
            Notification = 0,
            Pause = 1,
            Resume = 2,
            Stop = 3,
            Start = 4
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
            _type = EventType.Notification;
        }

        public Event(string name, string application, string description, EventLevel level, EventType type)
        {
            _name = name;
            _application = application;
            _eventDescription = description;
            _eventLevel = level;
            _type = type;
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

        public EventType Type
        {
            set
            {
                _type = value;
            }
            get
            {
                return (_type);
            }
        }

        #endregion
        #region Methods
        #endregion
    }
}
