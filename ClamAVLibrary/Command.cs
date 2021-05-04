namespace ClamAVLibrary
{
    public class Command
    {
        #region Fields

        string _application = "";
        string _name = "";
        string _commandDescription = "";
        CommandType _type = CommandType.Null;

        //Command types

        public enum CommandType
        {
            Null = -1,
            Pause = 0,
            Resume = 1,
            Stop = 2,
            Start = 3
        }

        #endregion
        #region Constructor

        public Command()
        {
        }

        public Command(string name, string application, string description)
        {
            _name = name;
            _application = application;
            _commandDescription = description;
            _type = CommandType.Null;
        }

        public Command(string name, string application, string description, CommandType type)
        {
            _name = name;
            _application = application;
            _commandDescription = description;
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
                _commandDescription = value;
            }
            get
            {
                return (_commandDescription);
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

        public CommandType Type
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
