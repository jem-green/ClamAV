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
            Forwarder forwarder = null;

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
                                            #region Scan
                                            case "scan":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    clamAV = new ClamAV();
                                                    break;
                                                }
                                            #endregion
                                            #region Update
                                            case "update":
                                                {
                                                    stack.Push(current);
                                                    current = element;
                                                    clamAV = new ClamAV();
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
                                            case "forwarder":
                                                {
                                                    //clamAV.Add(forwarder);
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            case "scan":
                                                {
                                                    //clamAV.Add(scan);
                                                    current = stack.Pop();
                                                    break;
                                                }
                                            case "update":
                                                {
                                                    //clamAV.Update(scan);
                                                    current = stack.Pop();
                                                    break;
                                                }
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
                                            case "check":
                                                {
                                                    try
                                                    {
                                                        //watcher.Check = Convert.ToInt32(text) * 1000; // convert to milliseconds
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
                                            case "interface":
                                                {
                                                    //watcher.Interface = text;
                                                    break;
                                                }
                                            case "key":
                                                {
                                                    forwarder.Key = text;
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
                                                    else if (current == "cardiac")
                                                    {
                                                        try
                                                        {
                                                            //watcher.Port = Convert.ToInt32(text);
                                                        }
                                                        catch { }
                                                    }
                                                    break;
                                                }
                                            case "timeout":
                                                {
                                                    try
                                                    {
                                                        //watcher.Timeout = Convert.ToInt32(text) * 1000; // convert to milliseconds
                                                    }
                                                    catch{};
                                                    break;
                                                }
                                            case "protocol":
                                                {
                                                    if (text.Trim().ToLower() == "rfc5424")
                                                    {
                                                        //watcher.Protocol = Watcher.ProtocolFormat.rfc5424;
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
