using log4net;
using System;

namespace ClamAVLibrary
{
    /// <summary>
    /// Base class for id and enabled
    /// </summary>
    public class Element
    {
        #region Fields

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected string _id = "";
        protected bool _enabled = true;

        #endregion
        #region Constructors

        public Element()
        {
        }
        public Element(String id)
        {
            _id = id;
        }
        public Element(String id, bool enabled)
        {
            _id = id;
            _enabled = enabled;
        }

        #endregion
        #region Properties

        public string Id
        {
            get
            {
                return (_id);
            }
            set
            {
                _id = value;
            }
        }

        public bool Enabled
        {
            get
            {
                return (_enabled);
            }
            set
            {
                _enabled = value;
            }
        }

        #endregion Properties
        #region Methods
        #endregion Methods
    }
}
