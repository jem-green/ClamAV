using System;
using System.Text.RegularExpressions;
using TracerLibrary;

namespace ClamAVLibrary
{
    public class Rfc3164 : Message
    {
        // http://www.ietf.org/rfc/rfc3164.txt

        #region Fields

        //<(?<PRI>([0-9]{1,3}))>(?<HEADER>(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+\d{1,2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\s[\-\[\]a-zA-Z0-9:._]+\s)(?<MSG>.*)
        private static string _pattern = @"<(?<PRI>([0-9]{1,3}))>(?<HEADER>(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec|jan|feb|mar|apr|may|jun|jul|aug|sep|oct|nov|dec)\s+\d{1,2}\s[0-9]{2}:[0-9]{2}:[0-9]{2}\s[\-\[\]a-zA-Z0-9:._]+\s)(?<MSG>.*)";
        // (?<TAG>[a-zA-Z0-0]*)(?<CONTENT>[^a-zA-Z0-9].*)
        private static string _msgPattern = @"(?<TAG>[a-zA-Z0-0]*)(?<CONTENT>[^a-zA-Z0-9].*)";
        private static Regex _expression;

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
            Parse(host, message);
        }

        #endregion
        #region Properties
        #endregion
        #region Methods

        public override bool Parse(string host, string message)
        {
            bool parsed = false;

            // Decode messgage

            _expression = new Regex(_pattern, RegexOptions.Compiled);
            Match match = _expression.Match(message);

            if (match.Success == true)
            {
                parsed = true;
                string pri = match.Groups["PRI"].Value;
                string header = match.Groups["HEADER"].Value.TrimEnd();
                string msg = match.Groups["MSG"].Value.TrimEnd();


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
                    TraceInternal.TraceError(e.ToString());
                    _facility = FacilityType.Internally;
                    _severity = SeverityType.Critical;
                }

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
                    TraceInternal.TraceError(e.ToString());
                    _timeStamp = DateTime.Now;
                    _hostName = Environment.MachineName;
                }
	            TraceInternal.TraceVerbose("timestamp='" + _timeStamp + "'");
	            TraceInternal.TraceVerbose("hostname='" + _hostName + "'");

                //Decode msg

                _expression = new Regex(_msgPattern, RegexOptions.Compiled);
                match = _expression.Match(msg);
                _tag = match.Groups["TAG"].Value;
                _content = match.Groups["CONTENT"].Value.TrimEnd();
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
                    }
                    if (_content.Length == 0)
                    {
                        _content = msg;
                    }
                }
                catch (Exception e)
                {
                    TraceInternal.TraceVerbose(e.ToString());
                    _tag = "";
                    _content = msg;
                }

                TraceInternal.TraceVerbose("pri='" + pri + "'");
                TraceInternal.TraceVerbose("-> facility='" + _facility.ToString() + "'");
                TraceInternal.TraceVerbose("-> severity='" + _severity.ToString() + "'");
                TraceInternal.TraceVerbose("header='" + header + "'");
                TraceInternal.TraceVerbose("->timestamp='" + _timeStamp + "'");
                TraceInternal.TraceVerbose("->hostname='" + _hostName + "'");
                TraceInternal.TraceVerbose("msg='" + msg + "'");
                TraceInternal.TraceVerbose("-> tag='" + _tag + "'");
                TraceInternal.TraceVerbose("-> content='" + _content + "'");
            }
            return (parsed);
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
