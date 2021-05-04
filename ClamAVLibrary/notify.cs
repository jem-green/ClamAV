namespace ClamAVLibrary
{
    // Forwarder base class
    public class Notify
    {
        #region Fields
        // Generally there are 5 levels supported by most notfication solutions
        public enum PriorityOrder
        {
            Low = -2,
            Moderate = -1,
            Normal = 0,
            High = 1,
            Emergency = 2
        }
        #endregion
        #region Methods
        /// <summary>
        /// Gets the priority name from the priority
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public virtual string PriorityName(PriorityOrder priority)
        {
            return (priority.ToString());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="priorityName"></param>
        /// <returns></returns>
        public virtual PriorityOrder PriorityLookup(string priorityName)
        {
            PriorityOrder priority = 0;

            string lookup = priorityName;
            if (priorityName.Length > 2)
            {
                lookup = priorityName.ToUpper();
            }

            switch (lookup)
            {
                case "-2":
                case "LOW":
                case "L":
                    priority = PriorityOrder.Low;
                    break;
                case "-1":
                case "MODERATE":
                case "M":
                    priority = PriorityOrder.Moderate;
                    break;
                case "0":
                case "NORMAL":
                case "N":
                    priority = PriorityOrder.Normal;
                    break;
                case "1":
                case "HIGH":
                case "H":
                    priority = PriorityOrder.High;
                    break;
                case "2":
                case "EMERGENCY":
                case "E":
                    priority = PriorityOrder.Emergency;
                    break;
            }
            return (priority);
        }
        #endregion
    }
}
