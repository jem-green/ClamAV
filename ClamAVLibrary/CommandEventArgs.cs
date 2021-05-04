using System;

namespace ClamAVLibrary
{
    public class CommandEventArgs : EventArgs
    {
        #region Fields
        private Command _command;
        #endregion
        #region Constructor
        public CommandEventArgs(Command command)
        {
            this._command = command;
        }
        #endregion
        #region Properties

        public Command Command
        {
            set
            {
                _command = value;
            }
            get
            {
                return (_command);
            }
        }
        #endregion
    }
}

