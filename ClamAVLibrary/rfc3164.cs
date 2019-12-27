using System;
using System.Text.RegularExpressions;
using System.Net;

namespace ClamAVLibrary
{
    public class Rfc3164 : Message
    {
        // http://www.ietf.org/rfc/rfc3164.txt

        #region variables
        #endregion
        #region Constructor

        public Rfc3164()
        {

        }

        public Rfc3164(string message) : this(null, message)
        {

        }

        public Rfc3164(string host, string message)
        {
            // Decode messgae

            //<(?<PRI>([0-9]{1,3}))>(?<HEADER>(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+\d{1,2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\s[\-\[\]a-zA-Z0-9:._]+\s)(?<MSG>.*)
            string test = @"<(?<PRI>([0-9]{1,3}))>(?<HEADER>(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+\d{1,2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\s[\-\[\]a-zA-Z0-9:._]+\s)(?<MSG>.*)";
            Regex mRegex = new Regex(test, RegexOptions.Compiled);
            Match tmpMatch = mRegex.Match(message);
            string pri = tmpMatch.Groups["PRI"].Value;
            string header = tmpMatch.Groups["HEADER"].Value.TrimEnd();
            string msg = tmpMatch.Groups["MSG"].Value.TrimEnd();
            log.Debug("pri='" + pri + "'");
            log.Debug("header='" + header + "'");
            log.Debug("msg='" + msg + "'");

            //Decode Pri
            try
            {
                int priority = Convert.ToInt32(pri);
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
            log.Debug("facility='" + _facility.ToString() + "'");
            log.Debug("severity='" + _severity.ToString() + "'");

            //Decode header

            try
            {
                int pos = header.LastIndexOf(' ');
                if (pos > 0)
                {
                    _timeStamp = ToDateTime(header.Substring(0, pos));
                    _hostName = header.Substring(pos + 1, header.Length - pos - 1);
                }
                else
                {
                    _timeStamp = ToDateTime(header);
                    _hostName = "";
                }
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
                _timeStamp = DateTime.Now;
                _hostName = Environment.MachineName;
            }
            log.Debug("timestamp='" + _timeStamp + "'");
            log.Debug("hostname='" + _hostName + "'");

            //Decode msg

            // (?<TAG>[a-zA-Z0-0]*)(?<CONTENT>[^a-zA-Z0-9].*)
            test = @"(?<TAG>[a-zA-Z0-0]*)(?<CONTENT>[^a-zA-Z0-9].*)";
            mRegex = new Regex(test, RegexOptions.Compiled);
            tmpMatch = mRegex.Match(msg);
            _tag = tmpMatch.Groups["TAG"].Value;
            _content = tmpMatch.Groups["CONTENT"].Value.TrimEnd();
            try
            {
                if (_tag.Length == 0)
                {
                    // workaround for missing hostname, as tag is empty
                    // Better solution is have a rule that uses a specfic pfsense version.
                    if (host != null)
                    {
                        _tag = _hostName.Substring(0, _hostName.Length - 1);
                        _hostName = host;
                    }
                    else
                    {
                        _tag = "Corral";
                    }
                }
                if (_content.Length == 0)
                {
                    _content = msg;
                }
            }
            catch (Exception e)
            {
                log.Debug(e.ToString());
                _tag = "Corral";
                _content = msg;
            }
            log.Debug("tag='" + _tag + "'");
            log.Debug("content='" + _content + "'");
        }

        #endregion
        #region Properties
        #endregion
        #region Methods

        public override string ToString()
        {
            return (string.Format("<{0}>{1} {2} {3}: {4}", ((int)_facility * 8) + (int)_severity, _timeStamp.ToString("MMM dd HH:mm:ss"), _hostName, _tag, _content));
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
