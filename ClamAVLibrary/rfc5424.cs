using System;
using System.Text.RegularExpressions;

namespace ClamAVLibrary
{
    public class Rfc5424 : Message
    {
        // http://www.ietf.org/rfc/rfc5424.txt

        #region variables

        private static string _headerPattern = @"\<(?<PRIVAL>\d{1,3})\>(?<VERSION>[1-9]{0,2}) (?<TIMESTAMP>(\S|\w)+) (?<HOSTNAME>-|(\S|\w){1,255}) (?<APPNAME>-|(\S|\w){1,48}) (?<PROCID>-|(\S|\w){1,128}) (?<MSGID>-|(\S|\w){1,32})";
        private static string _structuredDataPattern = @"(?<STRUCTUREDDATA>-|\[[^\[\=\x22\]\x20]{1,32}( ([^\[\=\x22\]\x20]{1,32}=\x22.+\x22))?\])";
        private static string _messagePattern = @"( (?<MSG>.+))?";
        private static Regex _Expression = new Regex($@"^{_headerPattern} {_structuredDataPattern}{_messagePattern}$", RegexOptions.None);

        int _version;
        string _procId;
        string _messageId;
        string _structuredData;

        #endregion
        #region Constructor

        public Rfc5424()
        {

        }

        public Rfc5424(string message) : this(null, message)
        {

        }

        public Rfc5424(string host, string message)
        {
            Parse(host, message);

        }

        #endregion
        #region Properties
        #endregion
        #region Methods

        public override bool Parse(string host, string message)
        {
            bool parsed = false;

            // Decode message
            // <165>1 2020-04-12T10:54:36Z GENESIS rtl_433 - - - {"time":"2020-04-12 11:54:35","model":"Oil Watchman","id":138043494,"flags":192,"maybetemp":8,"temperature_C":35.0,"binding_countdown":0,"depth":55}

            Match match = _Expression.Match(message);

            if (match.Success == true)
            {
                parsed = true;

                // Decode the header

                string prival = match.Groups["PRIVAL"].Value;

                //Decode Prival
                try
                {
                    int priority = Convert.ToInt32(prival);
                    if (priority < 0)
                    {
                        _facility = FacilityType.Internally;
                        _severity = SeverityType.Critical;
                    }
                    else
                    {
                        int facility = priority >> 3;  // divide by 8 by shifting left
                        int severity = priority & 0x7; // and with 7 to mask out
                        _facility = (FacilityType)Enum.Parse(typeof(FacilityType), facility.ToString());
                        _severity = (SeverityType)Enum.Parse(typeof(SeverityType), severity.ToString());
                    }
                }
                catch (Exception e)
                {
                    log.Error(e.ToString());
                    _facility = FacilityType.Internally;
                    _severity = SeverityType.Critical;
                }

                _version = Convert.ToInt32(match.Groups["VERSION"].Value);
                _timeStamp = Convert.ToDateTime(match.Groups["TIMESTAMP"].Value);
                _hostName = match.Groups["HOSTNAME"].Value;
                _tag = match.Groups["APPNAME"].Value;
                _procId = match.Groups["PROCID"].Value;
                _messageId = match.Groups["MSGID"].Value;

                // decode the structured data

                _structuredData = match.Groups["STRUCTUREDDATA"].Value;

                //Decode message

                _content = match.Groups["MSG"].Value;

                log.Debug("prival='" + prival.ToString() + "'");
                log.Debug("-> facility='" + _facility.ToString() + "'");
                log.Debug("-> severity='" + _severity.ToString() + "'");
                log.Debug("verson=" + _version);
                log.Debug("timeStamp=" + _timeStamp);
                log.Debug("hostName='" + _hostName + "'");
                log.Debug("tag='" + _tag + "'");
                log.Debug("procId='" + _procId + "'");
                log.Debug("messageId='" + _messageId + "'");
                log.Debug("structuredData='" + _structuredData + "'");
                log.Debug("content='" + _content + "'");
            }
            return (parsed);
        }

        public override string ToString()
        {
            return (string.Format("<{0}>{1} {2} {3}: {4}", ((int)_facility * 8) + (int)_severity, _timeStamp.ToString("MMM dd HH:mm:ss"), _hostName, _tag, base._content));
        }

        #endregion
        #region Private

        private static DateTime ToDateTime(string dateString)
        {
            /* 
             * Jan 01 12:00:00
             * 012345678901234
             */
            DateTime dateTime = DateTime.Now;
            try
            {
                string monthName = dateString.Substring(0, 3);
                string months = "janfebmaraprmayjunjulaugsepoctnovdec";
                int month = 1 + months.IndexOf(monthName.ToLower()) / 3;
                int day = Convert.ToInt32(dateString.Substring(3, 3));
                int hour = Convert.ToInt32(dateString.Substring(7, 2));
                int minute = Convert.ToInt32(dateString.Substring(10, 2));
                int second = Convert.ToInt32(dateString.Substring(13, 2));
                dateTime = new DateTime(DateTime.Now.Year, month, day, hour, minute, second);
            }
            catch { }
            return (dateTime);
        }

        #endregion
    }
}
