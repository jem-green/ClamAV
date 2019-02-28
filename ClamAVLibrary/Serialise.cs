using System;
using System.Collections.Generic;
using System.Text;
using log4net;
using System.IO;
using System.Xml;

namespace ClamAVLibrary
{
    public class Serialise
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        string _filename = "";
        string _path = "";

        #endregion
        #region Constructor

        public Serialise()
        { }

        public Serialise(string filename, string path)
        {
            this._filename = filename;
            this._path = path;
        }

        #endregion
        #region Properties
        public string Filename
        {
            get
            {
                return (_filename);
            }
            set
            {
                _filename = value;
            }
        }

        public string Path
        {
            get
            {
                return (_path);
            }
            set
            {
                _path = value;
            }
        }
        #endregion
        #region Methods

        public ClamAV FromXML()
        {
            return(FromXML(_filename, _path));
        }

        public ClamAV FromXML(string filename, string path)
        {
            ClamAV clamAV = null;
            Schedule schedule = null;
            Forwarder forwarder = null;
            FreshClam freshClam = null;
            Component scan = null;
            Clamd clamd = null;
            string key = "";
            string value = "";
            string id = "";
            Component.OperatingMode mode = Component.OperatingMode.combined;
            Component.DataLocation location = Component.DataLocation.program;

            try
            {
                // Point to the file

                string fileLocation = System.IO.Path.Combine(path, filename);
                try
                {
                    FileStream fs = new FileStream(fileLocation, FileMode.Open);

                    // Pass the parameters in

                    XmlReaderSettings xmlSettings = new XmlReaderSettings
                    {

                        // Enable <!ENTITY to be expanded
                        // <!ENTITY chap1 SYSTEM "chap1.xml">
                        // &chap1;

                        ProhibitDtd = false
                    };

                    // Open the file and pass in the settings

                    try
                    {
                        Stack<string> stack = new Stack<string>();
                        string element = "";
                        string text = "";
                        string current = "";    // Used to flag what level we are at
                        int level = 1;          // Indentation level

                        XmlReader xmlReader = XmlReader.Create(fs, xmlSettings);
                        while (xmlReader.Read())
                        {
                            switch (xmlReader.NodeType)
                            {
                                #region Element
                                case XmlNodeType.Element:
                                    {
                                        element = xmlReader.LocalName.ToLower();

                                        if (!xmlReader.IsEmptyElement)
                                        {
                                            log.Info(Level(level) + "<" + element + ">");
                                            level = level + 1;
                                        }
                                        else
                                        {
                                            log.Info(Level(level) + "<" + element + "/>");
                                        }

                                        switch (element)
                                        {
                                            #region ClamAV
                                            case "clamav":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    clamAV = new ClamAV();
                                                    break;
                                                }
                                            #endregion
                                            #region Configuration
                                            case "configuration":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    break;
                                                }
                                            #endregion
                                                    #region Forwarder
                                            case "forwarder":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    forwarder = new Forwarder();

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        forwarder.Id = xmlReader.Value.ToLower();
                                                                    }
                                                                    break;
                                                                case "type":
                                                                    {
                                                                        switch (xmlReader.Value.ToLower())
                                                                        {

                                                                            case "nma":
                                                                                {
                                                                                    forwarder.Type = Forwarder.ForwaderType.NMA;
                                                                                    break;
                                                                                }
                                                                            case "prowl":
                                                                                {
                                                                                    forwarder.Type = Forwarder.ForwaderType.Prowl;
                                                                                    break;
                                                                                }
                                                                            case "smtp":
                                                                                {
                                                                                    forwarder.Type = Forwarder.ForwaderType.SMTP;
                                                                                    break;
                                                                                }
                                                                            case "growl":
                                                                            default:
                                                                                {
                                                                                    forwarder.Type = Forwarder.ForwaderType.Growl;
                                                                                    break;
                                                                                }
                                                                        }
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                    }
                                                    break;
                                                }
                                            #endregion
                                            #region Scan
                                            case "scan":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    id = "";
                                                    mode = Component.OperatingMode.combined;
                                                    location = Component.DataLocation.program;

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = xmlReader.Value.ToLower();
                                                                        break;
                                                                    }
                                                                case "mode":
                                                                    {
                                                                        mode = ClamAV.ModeLookup(text);
                                                                        break;
                                                                    }
                                                                case "location":
                                                                    {
                                                                        location = ClamAV.LocationLookup(text);
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                    }

                                                    if (mode == Component.OperatingMode.combined)
                                                    {
                                                        scan = new ClamScan(id,location);
                                                        scan.Mode = mode;
                                                    }
                                                    else if (mode == Component.OperatingMode.client)
                                                    {
                                                        scan = new ClamdScan(id,location);
                                                        scan.Mode = mode;
                                                    }

                                                    break;
                                                }
                                            #endregion
                                            #region StartDate
                                            case "startdate":
                                                {
                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "format":
                                                                    {
                                                                        schedule.DateFormat = xmlReader.Value.ToLower();
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    break;
                                                }
                                            #endregion
                                            #region StartTime
                                            case "starttime":
                                                {

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "format":
                                                                    {
                                                                        schedule.TimeFormat = xmlReader.Value.ToLower();
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }

                                                    break;
                                                }
                                            #endregion
                                            #region Server
                                            case "server":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    id = "";
                                                    mode = Component.OperatingMode.server;
                                                    location = Component.DataLocation.program;
                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = xmlReader.Value.ToLower();
                                                                        break;
                                                                    }
                                                                case "mode":
                                                                    {
                                                                        mode = ClamAV.ModeLookup(text);
                                                                        break;
                                                                    }
                                                                case "location":
                                                                    {
                                                                        location = ClamAV.LocationLookup(text);
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                    }
                                                    clamd = new Clamd(location);
                                                    clamd.Mode = mode;
                                                    clamd.Id = id;
                                                    break;
                                                }
                                            #endregion
                                            #region Update
                                            case "update":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    id = "";
                                                    mode = Component.OperatingMode.combined;
                                                    location = Component.DataLocation.program;

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = xmlReader.Value.ToLower();
                                                                        break;
                                                                    }
                                                                case "mode":
                                                                    {
                                                                        mode = ClamAV.ModeLookup(text);
                                                                        break;
                                                                    }
                                                                case "location":
                                                                    {
                                                                        location = ClamAV.LocationLookup(text);
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                    }

                                                    freshClam = new FreshClam(id);
                                                    freshClam.Mode = mode;
                                                    freshClam.Id = id;

                                                    break;
                                                }
                                            #endregion
                                            #region Schedule
                                            case "schedule":
                                                {
                                                    stack.Push(current);
                                                    current = element;

                                                    schedule = new Schedule();

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = xmlReader.Value.ToLower();
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    schedule.Id = id;
                                                    break;
                                                }
                                            #endregion
                                            #region Setting
                                            case "setting":
                                                {
                                                    key = "";
                                                    value = "";
                                                    break;
                                                }
                                            #endregion
                                            #region Option
                                            case "option":
                                                {
                                                    key = "";
                                                    value = "";
                                                    break;
                                                }
                                            #endregion          
                                        }
                                        break;

                                    }
                                #endregion
                                #region EndElement
                                case XmlNodeType.EndElement:
                                    {
                                        element = xmlReader.LocalName;
                                        level = level - 1;
                                        log.Info(Level(level) + "</" + element + ">");
                                        switch (element)
                                        {
                                            #region Configuration
                                            case "configuration":
                                                {
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                                    #region Setting
                                            case "setting":
                                                {
                                                    Component.Setting setting = new Component.Setting(key, value);
                                                    if (current == "server")
                                                    {
                                                        clamd.Update(setting);
                                                    }
                                                    else if (current == "update")
                                                    {
                                                        freshClam.Update(setting);
                                                    }
                                                    else if (current == "scan")
                                                    {
                                                        scan.Update(setting);
                                                    }
                                                    break;
                                                }
                                            #endregion
                                            #region Option
                                            case "option":
                                                {
                                                    Component.Option option = new Component.Option(key, value);
                                                    if (stack.Peek() == "server")
                                                    {
                                                        clamd.Update(option);
                                                    }
                                                    else if (stack.Peek() == "update")
                                                    {
                                                        freshClam.Update(option);
                                                    }
                                                    else if (stack.Peek() == "scan")
                                                    {
                                                        scan.Update(option);
                                                    }
                                                    break;
                                                }
                                            #endregion
                                            #region Forwarder
                                            case "forwarder":
                                                {
                                                    forwarder.Key = key;
                                                    clamAV.Add(forwarder);
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Scan
                                            case "scan":
                                                {
                                                    // Not sure how to identify the scan
                                                    clamAV.Scans.Add(scan);
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Update
                                            case "update":
                                                {
                                                    clamAV.Update = freshClam;
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Server
                                            case "server":
                                                {
                                                    clamAV.Server = clamd;
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Schedule
                                            case "schedule":
                                                {
                                                    if (stack.Peek() == "update")
                                                    {
                                                        freshClam.Schedule = schedule;
                                                    }
                                                    else if (stack.Peek() == "scan")
                                                    {
                                                        // Problem here with type of scan
                                                        // May have to test if server, client or combined
                                                        scan.Schedule = schedule;
                                                    }
                                                    current = stack.Pop();
                                                    break;
                                                }
                                                #endregion
                                        }
                                        break;
                                    }
                                #endregion
                                #region Text

                                case XmlNodeType.Text:
                                    {
                                        text = xmlReader.Value;
                                        text = text.Replace("\t", "");
                                        text = text.Replace("\n", "");
                                        log.Info(Level(level) + text);

                                        switch (element)
                                        {
                                            case "key":
                                                {
                                                    key = text;
                                                    break;
                                                }
                                            case "value":
                                                {
                                                    value = text;
                                                    break;
                                                }
                                            case "location":
                                                {
                                                    if (current == "configuration")
                                                    {
                                                        try
                                                        {
                                                            clamAV.Location = ClamAV.LocationLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    else if (current == "update")
                                                    {
                                                        try
                                                        {
                                                            freshClam.Location = ClamAV.LocationLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    else if (current == "scan")
                                                    {
                                                        try
                                                        {
                                                            scan.Location = ClamAV.LocationLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    break;
                                                }
                                            case "mode":
                                                {
                                                    if (current == "configuration")
                                                    {
                                                        try
                                                        {
                                                            clamAV.Mode = ClamAV.ModeLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    else if (current == "update")
                                                    {
                                                        try
                                                        {
                                                            freshClam.Mode = ClamAV.ModeLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    else if (current == "scan")
                                                    {
                                                        try
                                                        {
                                                            scan.Mode = ClamAV.ModeLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    break;
                                                }
                                            case "startdate":
                                                {
                                                    try
                                                    {
                                                        schedule.StartDate = text;
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "starttime":
                                                {
                                                    try
                                                    {
                                                        schedule.StartTime = text;
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "units":
                                                {
                                                    try
                                                    {
                                                        schedule.Units = Schedule.UnitLookup(text);
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "timeout":
                                                {
                                                    try
                                                    {
                                                        schedule.Timeout = Convert.ToInt32(text);
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "encrypt":
                                                {
                                                    forwarder.Encrypt = text;
                                                    break;
                                                }
                                            case "facility":
                                                {
                                                    forwarder.Facility = Message.FacilityLookup(text);
                                                    break;
                                                }
                                            case "from":
                                                {
                                                    forwarder.From = text;
                                                    break;
                                                }
                                            case "host":
                                                {
                                                    forwarder.Host = text;
                                                    break;
                                                }
                                            case "monitor":
                                                {
                                                    try
                                                    {
                                                        //watcher.Monitor = Convert.ToInt32(text) * 1000; // convert to milliseconds
                                                    }
                                                    catch { };
                                                    break;
                                                }
                                            case "password":
                                                {
                                                    forwarder.Password = text;
                                                    break;
                                                }
                                            case "path":
                                                {
                                                    scan.Path = text;
                                                    break;
                                                }
                                            case "port":
                                                {
                                                    if (current == "forwarder")
                                                    {
                                                        try
                                                        {
                                                            forwarder.Port = Convert.ToInt32(text);
                                                        }
                                                        catch { }
                                                    }
                                                    else if (current == "clamd")
                                                    {
                                                        try
                                                        {
                                                            clamd.Port = Convert.ToInt32(text);
                                                        }
                                                        catch { }
                                                    }
                                                    break;
                                                }
                                            case "protocol":
                                                {
                                                    if (text.Trim().ToLower() == "rfc5424")
                                                    {
                                                        //clamAV.Protocol = Watcher.ProtocolFormat.rfc5424;
                                                    }
                                                    break;
                                                }
                                            case "severity":
                                                {
                                                    forwarder.Severity = Message.SeverityLookup(text);
                                                    break;
                                                }

                                            case "to":
                                                {
                                                    forwarder.To = text;
                                                    break;
                                                }
                                            case "type":
                                                {
                                                    switch (text.ToLower())
                                                    {
                                                        case "nma":
                                                            {
                                                                forwarder.Type = Forwarder.ForwaderType.NMA;
                                                                break;
                                                            }
                                                        case "prowl":
                                                            {
                                                                forwarder.Type = Forwarder.ForwaderType.Prowl;
                                                                break;
                                                            }
                                                        case "smtp":
                                                            {
                                                                forwarder.Type = Forwarder.ForwaderType.SMTP;
                                                                break;
                                                            }
                                                        case "syslog":
                                                            {
                                                                forwarder.Type = Forwarder.ForwaderType.SYSLOG;
                                                                break;
                                                            }
                                                        case "growl":
                                                        default:
                                                            {
                                                                forwarder.Type = Forwarder.ForwaderType.Growl;
                                                                break;
                                                            }
                                                    }
                                                    break;
                                                }
                                            case "username":
                                                {
                                                    forwarder.Username = text;
                                                    break;
                                                }
                                        }
                                        break;
                                    }
                                #endregion
                                #region Entity
                                case XmlNodeType.Entity:
                                    break;
                                #endregion
                                case XmlNodeType.EndEntity:
                                    break;
                                case XmlNodeType.Whitespace:
                                    break;
                                case XmlNodeType.Comment:
                                    break;
                                case XmlNodeType.Attribute:
                                    break;
                                default:
                                    log.Info(xmlReader.NodeType);
                                    break;

                            }
                        }

                        xmlReader.Close();  // Force the close
                        xmlReader = null;
                    }
                    catch (Exception ex)
                    {
                        log.Warn("XML Error " + ex.Message);
                    }
                    fs.Close();
                    fs.Dispose();   // Force the dispose as it was getting left open
                }
                catch (FileNotFoundException ex)
                {
                    log.Warn("File Error " + ex.Message);
                }
                catch (Exception ex)
                {
                    log.Warn("File Error " + ex.Message);
                }

            }
            catch (Exception e)
            {
                log.Error("Other Error " + e.Message);
            }

            return (clamAV);
        }
        #endregion
        #region Private
        private string Level(int level)
        {
            string text = "";
            for (int i = 1; i < level; i++)
            {
                //text = text + "\t";
                text = text + "  ";
            }
            return (text);
        }
        #endregion
    }
}
