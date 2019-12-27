using System;
using System.Text;
using System.Net;
using log4net;
using System.IO;
using System.Net.NetworkInformation;

namespace ClamAVLibrary
{
    public class Beat
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        byte[] nodeId = new byte[2] { 32, 32 };                 // No default
        private byte instanceId = 0;                            // default to 0 as primary
        IPAddress @interface = IPAddress.Parse("127.0.0.1");    // default to loopback
        
        private long counter = 0;                               // Initialise counter at zero
        private DateTime timeStamp = DateTime.Now;              // Defaults to now
        private IPAddress ip = IPAddress.Parse("127.0.0.1");    // default to loopback

        #endregion
        #region Constructors

        public Beat()
        {
            log.Debug("In Beat()");
            log.Debug("Out Beat()");
        }

        public Beat(byte[] payload)
        {
            log.Debug("In Beat()");
            DecodeBeat(payload);
            log.Debug("Out Beat()");
        }

        #endregion
        #region Properties

        public long Counter
        {
            set
            {
                Counter = value;
            }
            get
            {
                return (counter);
            }
        }

        public byte Instance
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

        public string Interface
        {
            get
            {
                return (@interface.ToString());
            }
            set
            {
                @interface =  GetInterfaceAddress(value);
            }
        }

        public string IpAddress
        {
            get
            {
                return (ip.ToString());
            }
            set
            {
                ip = IPAddress.Parse(value);
            }
        }

        public string NodeId
        {
            set
            {
                string nodeId = value + "  ";
                nodeId = nodeId.Substring(0, 2);
                this.nodeId = Encoding.ASCII.GetBytes(nodeId);
            }
            get
            {
                return (System.Text.Encoding.Default.GetString(nodeId));
            }
        }

        public DateTime TimeStamp
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
        #endregion
        #region Methods

        public byte[] EncodeBeat()
        {
            return (EncodeBeat(DateTime.Now, this.counter));
        }

        public byte[] EncodeBeat(DateTime timeStamp)
        {
            return (EncodeBeat(timeStamp, this.counter));
        }

        public byte[] EncodeBeat(DateTime timeStamp, long counter)
        {
            log.Debug("In EncodeBeat");

            // Encode heartbeat message
            // Type  = n - byte
            // node = nn - byte, byte
            // instance = n - byte
            // ipv4 = nnnn - byte,byte,byte,byte 
            // Epoch = nnnnnnnn - int64
            // Sequence = nnnnnnnn - int64 
            //
            // 0  - type
            // 1  - node[0]
            // 2  - node[1]
            // 3  - instance
            // 4  - ip(0)
            // 5  - ip(1)
            // 6  - ip(2)
            // 7  - ip(3)
            // 8  - epoch(0)
            // 9  - epoch(1)
            // 10 - epoch(2)
            // 11 - epoch(3)
            // 12 - epoch(4)
            // 13 - epoch(5)
            // 14 - epoch(6)
            // 15 - epoch(7)
            // 16 - sequence(0)
            // 17 - sequence(1)
            // 18 - sequence(2)
            // 19 - sequence(3)
            // 20 - sequence(4)
            // 21 - sequence(5)
            // 22 - sequence(6)
            // 23 - sequence(7)

            byte[] node = new byte[2] { nodeId[0], nodeId[1] };
            byte[] instance = BitConverter.GetBytes(instanceId);
            byte[] ip = @interface.GetAddressBytes();
            byte[] epoch = BitConverter.GetBytes(timeStamp.Ticks);
            byte[] sequence = BitConverter.GetBytes(counter);

            byte[] heartbeat = new byte[24];
            heartbeat[0] = 0;
            heartbeat[1] = nodeId[0];
            heartbeat[2] = nodeId[1];
            heartbeat[3] = instanceId;

            ip.CopyTo(heartbeat, 4);
            epoch.CopyTo(heartbeat, 8);
            sequence.CopyTo(heartbeat, 16);

            if (log.IsDebugEnabled == true)
            {
                log.Debug("nodeId=" + (char)nodeId[0] + (char)nodeId[1]);
                log.Debug("instanceId=" + instanceId);
                log.Debug("ip=" + @interface.ToString());
                log.Debug("timeStamp=" + timeStamp);
                log.Debug("counter=" + counter);
            }

            log.Debug("Out EncodeBeat");
            return (heartbeat);
        }

        public void DecodeBeat(byte[] payload)
        {
            log.Debug("In DecodeBeat");

            // Decode heartbeat message
            // Type  = n - byte
            // node = nn - byte, byte
            // instance = n - byte
            // ipv4 = nnnn - byte,byte,byte,byte 
            // Epoch = nnnnnnnn - int64
            // Sequence = nnnnnnnn - int64 

            try
            {
                if (payload[1] == 0)
                {
                    nodeId[0] = 32;
                }
                else
                {
                    nodeId[0] = payload[1];
                }
                if (payload[2] == 0)
                {
                    nodeId[1] = 32;
                }
                else
                {
                    nodeId[1] = payload[2];
                }

                instanceId = payload[3];
                ip = new IPAddress(new byte[] { payload[4], payload[5], payload[6], payload[7] });
                try
                {
                    long epoch = BitConverter.ToInt64(payload, 8);
                    timeStamp = new DateTime(epoch);
                }
                catch
                {
                    timeStamp = DateTime.Now;
                }
                try
                {
                    counter = BitConverter.ToInt64(payload, 16);
                }
                catch
                {
                    counter = 0;
                }

                if (log.IsDebugEnabled == true)
                {
                    log.Debug("nodeId=" + (char)nodeId[0] + (char)nodeId[1]);
                    log.Debug("instanceId=" + instanceId);
                    log.Debug("ip=" + ip.ToString());
                    log.Debug("timeStamp=" + timeStamp);
                    log.Debug("counter=" + counter);
                }
            }
            catch(Exception e)
            {
                log.Error("Payload format error");
                log.Error(e);
            }

            log.Debug("Out DecodeBeat");
        }

        public void UpdateCounter()
        {
            log.Debug("In UpdateCounter");
            try
            {
                string filePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                int pos = filePath.LastIndexOf('\\');
                filePath = filePath.Substring(0, pos);
                filePath = filePath + Path.DirectorySeparatorChar + "counter.bin";

                using (FileStream fileStream = File.Open(filePath, FileMode.OpenOrCreate))
                {
                    byte[] value = new byte[8];
                    fileStream.Read(value, 0, 8);
                    counter = BitConverter.ToInt64(value, 0);
                }
                counter = counter + 1;
                using (FileStream fileStream = File.Open(filePath, FileMode.Truncate))
                {
                    byte[] value = BitConverter.GetBytes(counter);
                    fileStream.Write(value, 0, 8);
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                counter = 0;
            }
            log.Debug("Out UpdateCounter");
        }

        #endregion
        #region Private

        private static IPAddress GetInterfaceAddress(string cidr)
        {
            log.Debug("In GetInterfaceAddress");
            IPAddress ip = IPAddress.Parse("127.0.0.1");
            int mask = 32;
            if (cidr.Contains("/"))
            {
                mask = Convert.ToInt32(cidr.Substring(cidr.IndexOf('/') + 1));
            }
            long subnet = BitConverter.ToInt32(IPAddress.Parse(cidr.Substring(0, cidr.IndexOf('/'))).GetAddressBytes(), 0);
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if ((networkInterface.OperationalStatus == OperationalStatus.Up) && (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet || networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    foreach (UnicastIPAddressInformation addressInformation in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        if (addressInformation.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            long ipAddress = BitConverter.ToInt32(addressInformation.Address.GetAddressBytes(), 0);
                            if ((subnet & mask) == (ipAddress & mask))
                            {
                                ip = addressInformation.Address;
                            }
                        }
                    }
                }
            }
            log.Debug("Out GetInterfaceAddress");
            return (ip);
        }
        #endregion
    }
}
