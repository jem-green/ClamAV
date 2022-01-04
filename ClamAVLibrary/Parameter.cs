namespace ClamAVLibrary
{
    public class Parameter
    {
        #region Fields
        object _value;
        SourceType source = SourceType.None;

        public enum SourceType
        {
            None = 0,
            Command = 1,
            Registry = 2,
            App = 3
        }

        #endregion
        #region Constructor
        public Parameter(string value)
        {
            this._value = value;
            source = SourceType.None;
        }
        public Parameter(string value, SourceType source)
        {
            this._value = value;
            this.source = source;
        }
        #endregion
        #region Parameters
        public object Value
        {
            set
            {
                this._value = value;
            }
            get
            {
                return (_value);
            }
        }

        public SourceType Source
        {
            set
            {
                source = value;
            }
            get
            {
                return (source);
            }
        }
        #endregion
        #region Methods
        public override string ToString()
        {
            return (_value.ToString());
        }
        #endregion
    }
}
