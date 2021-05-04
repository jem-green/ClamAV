namespace ClamAVLibrary
{
    public class Parameter
    {
        #region Fields

        string value;
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
            this.value = value;
            source = SourceType.None;
        }
        public Parameter(string value, SourceType source)
        {
            this.value = value;
            this.source = source;
        }
        #endregion
        #region Parameters
        public string Value
        {
            set
            {
                this.value = value;
            }
            get
            {
                return (value);
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
            return (value);
        }
        #endregion
    }
}
