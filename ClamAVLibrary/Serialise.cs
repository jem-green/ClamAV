﻿using TracerLibrary;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using static ClamAVLibrary.Component;

namespace ClamAVLibrary
{
    /// <summary>
    /// Serialise and deserialise configuration data
    /// </summary>
    public class Serialise
    {
        #region Fields

        string _filename = "";
        string _path = "";

        #endregion
        #region Constructors

        public Serialise()
        { }

        public Serialise(string filename, string path)
        {
            _filename = filename;
            _path = path;
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
            return (FromXML(_filename, _path));
        }

        public ClamAV FromXML(string filename, string path)
        {
            ClamAV clamAV = null;
            Schedule schedule = null;
            Forwarder forwarder = null;
            FreshClam freshClam = null;
            UpdateClam updateClam = null;
            Component scan = null;
            Clamd clamd = null;
            string key = "";
            string value = "";
            string id = "";
            bool enabled = true;
            string appPath = "";
            Forwarder.ForwarderType forwarderType = Forwarder.ForwarderType.None;
            Component.OperatingMode mode = Component.OperatingMode.Standalone;
            Component.DataLocation location = Component.DataLocation.Program;

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

                        DtdProcessing = DtdProcessing.Ignore
                        //ProhibitDtd = true
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
                                            TraceInternal.TraceVerbose(Level(level) + "<" + element + ">");
                                            level = level + 1;
                                        }
                                        else
                                        {
                                            TraceInternal.TraceVerbose(Level(level) + "<" + element + "/>");
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
                                                    id = "";
                                                    enabled = true;
                                                    forwarderType = Forwarder.ForwarderType.None;

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = text;
                                                                    }
                                                                    break;
                                                                case "enabled":
                                                                    {
                                                                        enabled = BooleanLookup(text);
                                                                    }
                                                                    break;
                                                                case "type":
                                                                    {
                                                                        switch (text)
                                                                        {

                                                                            case "nma":
                                                                                {
                                                                                    forwarderType = Forwarder.ForwarderType.NotifyMyAndroid;
                                                                                    break;
                                                                                }
                                                                            case "prowl":
                                                                                {
                                                                                    forwarderType = Forwarder.ForwarderType.Prowl;
                                                                                    break;
                                                                                }
                                                                            case "smtp":
                                                                                {
                                                                                    forwarderType = Forwarder.ForwarderType.SMTP;
                                                                                    break;
                                                                                }
                                                                            case "growl":
                                                                            default:
                                                                                {
                                                                                    forwarderType = Forwarder.ForwarderType.Growl;
                                                                                    break;
                                                                                }
                                                                        }
                                                                        break;
                                                                    }
                                                            }
                                                        }
                                                    }
                                                    forwarder = new Forwarder();
                                                    forwarder.Id = id;
                                                    forwarder.Enabled = enabled;
                                                    forwarder.Type = forwarderType;
                                                    break;
                                                }
                                            #endregion
                                            #region Option
                                            case "option":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    key = "";
                                                    value = "";
                                                    break;
                                                }
                                            #endregion
                                            #region Refresh
                                            case "refresh":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    id = "";
                                                    enabled = true;
                                                    mode = Component.OperatingMode.Standalone;
                                                    location = clamAV.Location;

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = text;
                                                                        break;
                                                                    }
                                                                case "enabled":
                                                                    {
                                                                        enabled = BooleanLookup(text);
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

                                                    freshClam = new FreshClam(id, location, clamAV.Path);
                                                    freshClam.Mode = mode;
                                                    freshClam.Id = id;
                                                    freshClam.Enabled = enabled;
                                                    break;
                                                }
                                            #endregion
                                            #region Scan
                                            case "scan":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    id = "";
                                                    enabled = true;
                                                    mode = clamAV.Mode; // Apply global which defaults to standalone
                                                    location = clamAV.Location;

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = text;
                                                                        break;
                                                                    }
                                                                case "enabled":
                                                                    {
                                                                        enabled = BooleanLookup(text);
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

                                                    // defaults to the global setting which is standalone
                                                    if (mode == Component.OperatingMode.Standalone)
                                                    {
                                                        // If scan running on a single server and doesn't need clamd
                                                        // Launch ClamScan
                                                        scan = new ClamScan(id, location, clamAV.Path);
                                                        scan.Mode = mode;
                                                        scan.Enabled = enabled;
                                                    }
                                                    else if ((mode == Component.OperatingMode.Client) || (mode == Component.OperatingMode.Combined))
                                                    {
                                                        // If scan is running remotely use then need clamd
                                                        // Launch ClamdScan

                                                        scan = new ClamdScan(id, location, clamAV.Path, clamd.Port);
                                                        scan.Host = clamd.Host;
                                                        scan.Mode = mode;
                                                        scan.Enabled = enabled;
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
                                                    enabled = true;
                                                    mode = Component.OperatingMode.Server;
                                                    location = clamAV.Location;
                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = text;
                                                                        break;
                                                                    }
                                                                case "enabled":
                                                                    {
                                                                        enabled = BooleanLookup(text);
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
                                                    clamd = new Clamd(location, clamAV.Path);
                                                    clamd.Mode = mode;
                                                    clamd.Id = id;
                                                    clamd.Enabled = enabled;
                                                    break;
                                                }
                                            #endregion
                                            #region Setting
                                            case "setting":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    key = "";
                                                    value = "";
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
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "format":
                                                                    {
                                                                        schedule.DateFormat = text;
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
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "format":
                                                                    {
                                                                        schedule.TimeFormat = text;
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }

                                                    break;
                                                }
                                            #endregion
                                            #region Schedule
                                            case "schedule":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    enabled = true;
                                                    schedule = new Schedule();

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = text;
                                                                    }
                                                                    break;
                                                            }
                                                        }
                                                    }
                                                    schedule.Id = id;
                                                    break;
                                                }
                                            #endregion
                                            #region Update
                                            case "update":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    id = "";
                                                    enabled = true;
                                                    mode = Component.OperatingMode.Standalone;
                                                    location = clamAV.Location;

                                                    if (xmlReader.HasAttributes == true)
                                                    {
                                                        while (xmlReader.MoveToNextAttribute())
                                                        {
                                                            text = xmlReader.Value.ToLower();
                                                            switch (xmlReader.Name.ToLower())
                                                            {
                                                                case "id":
                                                                    {
                                                                        id = text;
                                                                        break;
                                                                    }
                                                                case "enabled":
                                                                    {
                                                                        enabled = BooleanLookup(text);
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

                                                    updateClam = new UpdateClam(id, location, clamAV.Path);
                                                    updateClam.Mode = mode;
                                                    updateClam.Id = id;
                                                    updateClam.Enabled = enabled;

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
                                        TraceInternal.TraceVerbose(Level(level) + "</" + element + ">");
                                        switch (element)
                                        {
                                            #region ClamAV
                                            #endregion
                                            #region Configuration
                                            case "configuration":
                                                {
                                                    current = stack.Pop();
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
                                            #region Option
                                            case "option":
                                                {
                                                    if (key.Length > 0)
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
                                                    }
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Refresh
                                            case "update":
                                                {
                                                    clamAV.Update = updateClam;
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
                                            #region Server
                                            case "server":
                                                {
                                                    clamAV.Server = clamd;
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Setting
                                            case "setting":
                                                {
                                                    if (key.Length > 0)
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
                                                    }
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region StartDate
                                            #endregion
                                            #region StartTime
                                            #endregion
                                            #region Schedule
                                            case "schedule":
                                                {
                                                    if (stack.Peek() == "refresh")
                                                    {
                                                        freshClam.Schedule = schedule;
                                                    }
                                                    else if (stack.Peek() == "update")
                                                    {
                                                        updateClam.Schedule = schedule;
                                                    }
                                                    else if (stack.Peek() == "scan")
                                                    {
                                                        // Problem here with type of scan
                                                        // May have to test if server, standalone, client or combined
                                                        scan.Schedule = schedule;
                                                    }
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            #endregion
                                            #region Update
                                            case "refresh":
                                                {
                                                    clamAV.Refresh = freshClam;
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
                                        TraceInternal.TraceVerbose(Level(level) + text);

                                        switch (element)
                                        {
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
                                                    if (current == "forwarder")
                                                    {
                                                        forwarder.Host = text;
                                                    }
                                                    else if (current == "scan")
                                                    {
                                                        scan.Host = text;
                                                    }
                                                    break;
                                                }
                                            case "interface":
                                                {
                                                    clamd.Interface = text;
                                                    break;
                                                }
                                            case "key":
                                                {
                                                    key = text;
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
                                                    else if (current == "server")
                                                    {
                                                        try
                                                        {
                                                            clamd.Location = ClamAV.LocationLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    else if (current == "refresh")
                                                    {
                                                        try
                                                        {
                                                            freshClam.Location = ClamAV.LocationLookup(text);
                                                        }
                                                        catch { };
                                                    }
                                                    else if (current == "update")
                                                    {
                                                        try
                                                        {
                                                            updateClam.Location = ClamAV.LocationLookup(text);
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
                                            #region Mode
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
                                                    else if (current == "refresh")
                                                    {
                                                        try
                                                        {
                                                            freshClam.Mode = ClamAV.ModeLookup(text);
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
                                            #endregion
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
                                                    if (current == "configuration")
                                                    {
                                                        clamAV.Path = text;
                                                    }
                                                    else if (current == "schedule")
                                                    {
                                                        scan.Path = text;
                                                    }
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
                                                    else if (current == "server")
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
                                                    else if (text.Trim().ToLower() == "rf3164")
                                                    {
                                                        //clamAV.Protocol = Watcher.ProtocolFormat.rfc3164;
                                                    }
                                                    break;
                                                }
                                            case "severity":
                                                {
                                                    forwarder.Severity = Message.SeverityLookup(text);
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

                                            case "timeout":
                                                {
                                                    try
                                                    {
                                                        schedule.Timeout = Convert.ToInt32(text);
                                                    }
                                                    catch { };
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
                                                                forwarder.Type = Forwarder.ForwarderType.NotifyMyAndroid;
                                                                break;
                                                            }
                                                        case "prowl":
                                                            {
                                                                forwarder.Type = Forwarder.ForwarderType.Prowl;
                                                                break;
                                                            }
                                                        case "smtp":
                                                            {
                                                                forwarder.Type = Forwarder.ForwarderType.SMTP;
                                                                break;
                                                            }
                                                        case "syslog":
                                                            {
                                                                forwarder.Type = Forwarder.ForwarderType.SYSLOG;
                                                                break;
                                                            }
                                                        case "growl":
                                                        default:
                                                            {
                                                                forwarder.Type = Forwarder.ForwarderType.Growl;
                                                                break;
                                                            }
                                                    }
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
                                            case "username":
                                                {
                                                    forwarder.Username = text;
                                                    break;
                                                }
                                            case "value":
                                                {
                                                    value = text;
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
                                    TraceInternal.TraceVerbose(xmlReader.NodeType.ToString());
                                    break;

                            }
                        }

                        xmlReader.Close();  // Force the close
                        xmlReader = null;
                    }
                    catch (Exception ex)
                    {
                        TraceInternal.TraceWarning("XML Error " + ex.Message);
                    }
                    fs.Close();
                    fs.Dispose();   // Force the dispose as it was getting left open
                }
                catch (FileNotFoundException ex)
                {
                    TraceInternal.TraceWarning("File Error " + ex.Message);
                }
                catch (Exception ex)
                {
                    TraceInternal.TraceError("File Error " + ex.Message);
                }

            }
            catch (Exception e)
            {
                TraceInternal.TraceError("Other Error " + e.Message);
            }

            return (clamAV);
        }
        #endregion
        #region Private

        private static string Level(int level)
        {
            string text = "";
            for (int i = 1; i < level; i++)
            {
                text = text + "  ";
            }
            return (text);
        }

        public static bool BooleanLookup(string locationName)
        {
            bool boolean = true;

            if (bool.TryParse(locationName, out bool booleanValue))
            {
                boolean = booleanValue;
            }
            else
            {
                string lookup = locationName;
                if (locationName.Length > 1)
                {
                    lookup = locationName.ToUpper();
                }

                switch (lookup)
                {
                    case "Y":
                    case "YES":
                    case "TRUE":
                        {
                            boolean = true;
                            break;
                        }
                    case "N":
                    case "NO":
                    case "FALSE":
                        {
                            boolean =false;
                            break;
                        }
                }
            }
            return (boolean);
        }

        #endregion
    }
}
