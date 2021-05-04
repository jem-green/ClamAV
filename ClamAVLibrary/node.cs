using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace ClamAVLibrary
{
    public class Node : IComparable
    {
        #region Fields
        private string nodeId;
        private int instanceId = 0;
        private string ip = "127.0.0.1";
        private DateTime timeStamp = DateTime.Now;
        private long counter = 0;
        private string description = "";
        private StatusType status = StatusType.Unknown;

        public enum StatusType : int
        {
            Unknown = -1,
            Down = 0,
            Up = 1,
            Delayed = 2,
            Responding = 3
        }
        #endregion
        #region Constructors
        public Node()
        { }
        public Node(string IpAddress)
        {
            this.ip = IpAddress;
        }
        public Node(string nodeId, byte instanceId)
        {
            this.nodeId = nodeId;
            this.instanceId = instanceId;
        }
        public Node(string nodeId, byte instanceId, string ip)
        {
            this.nodeId = nodeId;
            this.instanceId = instanceId;
            this.ip = ip;
        }
        #endregion
        #region Properties
        public long Counter
        {
            set
            {
                counter = value;
            }
            get
            {
                return (counter);
            }
        }
        public string Description
        {
            set
            {
                description = value;
            }
            get
            {
                return (description);
            }
        }
        public int InstanceId
        {
            set
            {
                instanceId = value;
            }
            get
            {
                return (instanceId);
            }
        }
        public string IpAddress
        {
            set
            {
                ip = value;
            }
            get
            {
                return (ip);
            }
        }
        public DateTime LastUpdated
        {
            set
            {
                timeStamp = value;
            }
            get
            {
                return (timeStamp);
            }
        }
        public string NodeId
        {
            set
            {
                nodeId = value;
            }
            get
            {
                return (nodeId);
            }
        }
        public StatusType Status
        {
            set
            {
                status = value;
            }
            get
            {
                return (status);
            }
        }
        #endregion
        #region Methods
        //public int Compare(Node a, Node b)
        //{
        //    return (1);
        //}
        public int CompareTo(object obj)
            {
            if (obj == null) return 1;

            Node otherNode = obj as Node;
            if (otherNode != null)
                return this.ip.CompareTo(otherNode.ip);
            else
                throw new ArgumentException("Object is not a Node");
        }

        public static string StatusName(StatusType statusType)
        {
            int status = (int)statusType;
            return (Enum.GetName(typeof(Node.StatusType), status));
        }

        public static StatusType StatusLookup(string StatusName)
        {
            StatusType status = StatusType.Unknown;

            string lookup = StatusName;
            if (StatusName.Length > 1)
            {
                lookup = StatusName.ToUpper();
            }

            //foreach (int item in Enum.GetValues(typeof(Node.StatusType)))
            //{
            //    string name = Enum.GetName(typeof(Node.StatusType), item);
            //    if (name == lookup)
            //    {
            //        status = (StatusType)item;
            //    }
            //    else if (name.Substring(0,1) == lookup.Substring(0,1))
            //    {
            //        status = (StatusType)item;
            //    }
            //    else if (item == Convert.ToInt32(lookup))
            //    {
            //        status = (StatusType)item;
            //    }
            //}

            switch (lookup)
            {
                case "-1":
                case "UNKNOWN":
                case "u":
                    status = StatusType.Unknown;
                    break;
                case "0":
                case "DOWN":
                case "D":
                    status = StatusType.Down;
                    break;
                case "1":
                case "UP":
                case "U":
                    status = StatusType.Up;
                    break;
                case "2":
                case "DELAYED":
                case "d":
                    status = StatusType.Delayed;
                    break;
                case "3":
                case "RESPONDING":
                case "R":
                    status = StatusType.Responding;
                    break;
            }
            return (status);
        }

        #endregion
    }
}
