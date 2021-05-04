namespace ClamAVLibrary
{
    public class NameKey
    {
        #region Fields

        private string _name;
        private string _key;

        #endregion
        #region Constructors

        public NameKey()
        {
            _name = "";
            _key = "";
        }


        public NameKey(string name, string key)
        {
            _name = name;
            _key = key;
        }

        #endregion
        #region Properties

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

        public string Key
        {
            set
            {
                _key = value;
            }
            get
            {
                return (_key);
            }
        }

        #endregion
    }
}
