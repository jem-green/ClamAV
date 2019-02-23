using System;
using System.Text.RegularExpressions;
using System.Net;

namespace ClamAVLibrary
{
    public class Rfc3164 : Message
    {
        // http://www.ietf.org/rfc/rfc3164.txt

        #region variables

        private struct PriStruct
        {
            public FacilityType Facility { get; set; }
            public SeverityType Severity { get; set; }

            public PriStruct(string facility, string severity) : this()
            {
                this.Facility = (FacilityType)Enum.Parse(typeof(FacilityType), facility);
                this.Severity = (SeverityType)Enum.Parse(typeof(SeverityType), severity);
            }

            public PriStruct(string priorityNumber) : this()
            {
                int priority = Convert.ToInt32(priorityNumber);
                int intFacility = priority >> 3;  // divide by 8 by shifting left
                int intSeverity = priority & 0x7; // and with 7 to mask out
                this.Facility = (FacilityType)Enum.Parse(typeof(FacilityType), intFacility.ToString());
                this.Severity = (SeverityType)Enum.Parse(typeof(SeverityType), intSeverity.ToString());
            }
            public PriStruct(Int32 priority) : this()
            {
                int facility = priority >> 3;  // divide by 8 by shifting left
                int severity = priority & 0x7; // and with 7 to mask out
                this.Facility = (FacilityType)Enum.Parse(typeof(FacilityType), facility.ToString());
                this.Severity = (SeverityType)Enum.Parse(typeof(SeverityType), severity.ToString());
            }
            public override string ToString()
            {
                //export values to a valid pri structure
                return string.Format("{0}.{1}", this.Facility, this.Severity);
            }
        }

        private struct HeaderStruct
        {
            public DateTime TimeStamp { get; set; }
            public string HostName { get; set; }

            public HeaderStruct(DateTime timeStamp, string hostName) : this()
            {
                this.TimeStamp = timeStamp;
                this.HostName = hostName;
            }

            public HeaderStruct(DateTime timeStamp) : this()
            {
                this.TimeStamp = timeStamp;
                this.HostName = null;
            }
        }

        private struct MsgStruct
        {
            public string Tag { get; set; }
            public string Content { get; set; }

            public MsgStruct(string tag, string content) : this()
            {
                this.Tag = tag;
                this.Content = content;
            }

            public MsgStruct(string content) : this()
            {
                this.Tag = System.Environment.MachineName;
                this.Content = content;
            }
        }

        private struct MessageStruct
        {
            public PriStruct Pri { get; set; }

            public HeaderStruct Header { get; set; }

            public MsgStruct Msg { get; set; }

            public MessageStruct(PriStruct PRI, HeaderStruct HEADER, MsgStruct MSG) : this()
            {
                this.Pri = PRI;
                this.Header = HEADER;
                this.Msg = MSG;
            }

            public MessageStruct(string Message) : this()
            {
                Regex mRegex = new Regex("<(?<PRI>([0-9]{1,3}))>(?<MSG>.*)", RegexOptions.Compiled);
                Match tmpMatch = mRegex.Match(Message);
                this.Pri = new PriStruct(tmpMatch.Groups["PRI"].Value);
                this.Header = new HeaderStruct(DateTime.Now, null);
                this.Msg = new MsgStruct(tmpMatch.Groups["MSG"].Value);
            }

            public MessageStruct(string message, string hostName) : this()
            {
                Regex mRegex = new Regex("<(?<PRI>([0-9]{1,3}))>(?<MSG>.*)", RegexOptions.Compiled);
                Match tmpMatch = mRegex.Match(message);
                this.Pri = new PriStruct(tmpMatch.Groups["PRI"].Value);
                this.Header = new HeaderStruct(DateTime.Now, hostName);
                this.Msg = new MsgStruct(tmpMatch.Groups["MSG"].Value);
            }

            public MessageStruct(string tag, string message, string hostName) : this()
            {
                Regex mRegex = new Regex("<(?<PRI>([0-9]{1,3}))>(?<MSG>.*)", RegexOptions.Compiled);
                Match tmpMatch = mRegex.Match(message);
                this.Pri = new PriStruct(tmpMatch.Groups["PRI"].Value);
                this.Header = new HeaderStruct(DateTime.Now, hostName);
                this.Msg = new MsgStruct("", tmpMatch.Groups["MSG"].Value);
            }

            public override string ToString()
            {
                return (string.Format("<{0}>{1} {2} {3}: {4}", ((int)Pri.Facility * 8) + (int)Pri.Severity, Header.TimeStamp.ToString("MMM dd HH:mm:ss"), Header.HostName, Msg.Tag, Msg.Content));
            }
        }

        #endregion
        #region Constructors

        public Rfc3164()
        {
            severity = SeverityType.Emergency;     // The message severity
            facility = FacilityType.Kernel;        // The type of message
            DateTime timeStamp = DateTime.Now;
        }

        public Rfc3164(string message)
        {
            severity = SeverityType.Emergency;     // The message severity
            facility = FacilityType.Kernel;        // The type of message
            DateTime timeStamp = DateTime.Now;
            content = message;
        }

        public Rfc3164(string message, string severity, string facility)
        {
            SeverityType severityType = SeverityLookup(severity);   // The message severity
            FacilityType facilityType = FacilityLookup(facility);   // The type of message
            DateTime timeStamp = DateTime.Now;
            content = message;
        }

        public Rfc3164(string message, DateTime timeStamp, string severity, string facility)
        {
            SeverityType severityType = SeverityLookup(severity);   // The message severity
            FacilityType facilityType = FacilityLookup(facility);   // The type of message
            content = message;
        }

        public Rfc3164(string message, SeverityType severityType, FacilityType facilityType)
        {
            DateTime timeStamp = DateTime.Now;
            content = message;
        }

        #endregion
        #region Properties

        public override string Payload
        {
            get
            {
                MessageStruct messsageStructure;

                if ((facility != FacilityType.Null) && (severity != SeverityType.Null))
                {
                    PriStruct PRI = new PriStruct
                    {
                        Facility = facility,
                        Severity = severity
                    };

                    HeaderStruct HEADER = new HeaderStruct
                    {
                        TimeStamp = timeStamp,
                        HostName = host
                    };

                    MsgStruct MSG = new MsgStruct(tag, content);
                    messsageStructure = new MessageStruct(PRI, HEADER, MSG);
                }
                else
                {
                    messsageStructure = new MessageStruct(content);
                }
                return (messsageStructure.ToString());
            }
        }

        #endregion
    }
}
