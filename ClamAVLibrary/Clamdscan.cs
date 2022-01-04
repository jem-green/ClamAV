using TracerLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClamAVLibrary
{
    /// <summary>
    /// Wrapper class to manage and launch clamdscan
    /// </summary>
    public class ClamdScan : Component
    {
        #region Fields

        #endregion
        #region Constructor

        public ClamdScan(string id) : this(id, DataLocation.Program, "", 0)
        {
        }

        public ClamdScan(string id, DataLocation location) : this(id, location, "", 0)
        {
        }

        public ClamdScan(string id, string path) : this(id, DataLocation.Program, path, 0)
        {
        }

        public ClamdScan(string id, DataLocation location, string path, int port)
        {
            Debug.WriteLine("In ClamdScan()");

            _id = id;
            _execute = "clamdscan.exe";
            if (port != 0)
            {
                this._port = port;
            }
            _path = path;

            _schedule = new Schedule();
            _schedule.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
            _schedule.Time = new TimeSpan(_schedule.Date.Hour, _schedule.Date.Minute, _schedule.Date.Second);

            string basePath = "";
            switch (location)
            {
                case Component.DataLocation.App:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case Component.DataLocation.Program:
                    {
                        basePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        int pos = basePath.LastIndexOf('\\');
                        basePath = basePath.Substring(0, pos);
                        break;
                    }
                case Component.DataLocation.Local:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case Component.DataLocation.Roaming:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
            }
            _databasePath = basePath + System.IO.Path.DirectorySeparatorChar + "database";
            if (!Directory.Exists(_databasePath))
            {
                Directory.CreateDirectory(_databasePath);
            }
            _logPath = basePath + System.IO.Path.DirectorySeparatorChar + "logs";
            if (!Directory.Exists(_logPath))
            {
                Directory.CreateDirectory(_logPath);
            }
            _logFilenamePath = _logPath + System.IO.Path.DirectorySeparatorChar + "clamdscan.log";
            _configFilenamePath = basePath + System.IO.Path.DirectorySeparatorChar + "clamdscan.conf";

            // Not sure about the hardcoding here

            _executePath = "c:\\program files\\clamav" + System.IO.Path.DirectorySeparatorChar + _execute;

            _settings = new List<Setting>();
            _settings.Add(new Setting("AlgorithmicDetection", null));
            _settings.Add(new Setting("AllowAllMatchScan", null));
            _settings.Add(new Setting("AllowSupplementaryGroups", null));
            _settings.Add(new Setting("ArchiveBlockEncrypted", null));
            _settings.Add(new Setting("Bytecode", null));
            _settings.Add(new Setting("BytecodeSecurity", null));
            _settings.Add(new Setting("BytecodeTimeout", null));
            _settings.Add(new Setting("CommandReadTimeout", null));
            _settings.Add(new Setting("CrossFilesystems", null));
            _settings.Add(new Setting("DatabaseDirectory", _databasePath));
            _settings.Add(new Setting("Debug", null));
            _settings.Add(new Setting("DetectBrokenExecutables", null));
            _settings.Add(new Setting("DetectPUA", null));
            _settings.Add(new Setting("DisableCache", null));
            _settings.Add(new Setting("DisableCertCheck", null));
            _settings.Add(new Setting("ExcludePUA", null));
            _settings.Add(new Setting("ExcludePath", null));
            _settings.Add(new Setting("ExitOnOOM", null));
            _settings.Add(new Setting("ExtendedDetectionInfo", null));
            _settings.Add(new Setting("FixStaleSocket", null));
            _settings.Add(new Setting("FollowDirectorySymlinks", null));
            _settings.Add(new Setting("FollowFileSymlinks", null));
            _settings.Add(new Setting("ForceToDisk", null));
            _settings.Add(new Setting("Foreground", null));
            _settings.Add(new Setting("HeuristicScanPrecedence", null));
            _settings.Add(new Setting("IdleTimeout", null));
            _settings.Add(new Setting("IncludePUA", null));
            _settings.Add(new Setting("LeaveTemporaryFiles", null));
            _settings.Add(new Setting("LocalSocketGroup", null));
            _settings.Add(new Setting("LocalSocketMode", null));
            _settings.Add(new Setting("LogClean", null));
            _settings.Add(new Setting("LogFacility", null));
            _settings.Add(new Setting("LogFile", null));
            _settings.Add(new Setting("LogFileMaxSize", null));
            _settings.Add(new Setting("LogFileUnlock", null));
            _settings.Add(new Setting("LogRotate", null));
            _settings.Add(new Setting("LogSyslog", null));
            _settings.Add(new Setting("LogTime", null));
            _settings.Add(new Setting("LogVerbose", null));
            _settings.Add(new Setting("MaxConnectionQueueLength", null));
            _settings.Add(new Setting("MaxDirectoryRecursion", null));
            _settings.Add(new Setting("MaxEmbeddedPE", null));
            _settings.Add(new Setting("MaxFileSize", null));
            _settings.Add(new Setting("MaxFiles", null));
            _settings.Add(new Setting("MaxHTMLNoTags", null));
            _settings.Add(new Setting("MaxHTMLNormalize", null));
            _settings.Add(new Setting("MaxIconsPE", null));
            _settings.Add(new Setting("MaxPartitions", null));
            _settings.Add(new Setting("MaxQueue", null));
            _settings.Add(new Setting("MaxRecHWP3", null));
            _settings.Add(new Setting("MaxRecursion", null));
            _settings.Add(new Setting("MaxScanSize", null));
            _settings.Add(new Setting("MaxScriptNormalize", null));
            _settings.Add(new Setting("MaxThreads", null));
            _settings.Add(new Setting("MaxZipTypeRcg", null));
            _settings.Add(new Setting("OLE2BlockMacros", null));
            _settings.Add(new Setting("OfficialDatabaseOnly", null));
            _settings.Add(new Setting("OnAccessDisableDDD", null));
            _settings.Add(new Setting("OnAccessExcludePath", null));
            _settings.Add(new Setting("OnAccessExcludeUID", null));
            _settings.Add(new Setting("OnAccessExtraScanning", null));
            _settings.Add(new Setting("OnAccessIncludePath", null));
            _settings.Add(new Setting("OnAccessMaxFileSize", null));
            _settings.Add(new Setting("OnAccessMountPath", null));
            _settings.Add(new Setting("OnAccessPrevention", null));
            _settings.Add(new Setting("PCREMatchLimit", null));
            _settings.Add(new Setting("PCREMaxFileSize", null));
            _settings.Add(new Setting("PCRERecMatchLimit", null));
            _settings.Add(new Setting("PartitionIntersection", null));
            _settings.Add(new Setting("PhishingAlwaysBlockCloak", null));
            _settings.Add(new Setting("PhishingAlwaysBlockSSLMismatch", null));
            _settings.Add(new Setting("PhishingScanURLs", null));
            _settings.Add(new Setting("PhishingSignatures", null));
            _settings.Add(new Setting("PidFile", null));
            _settings.Add(new Setting("ReadTimeout", null));
            _settings.Add(new Setting("ScanArchive", null));
            _settings.Add(new Setting("ScanELF", null));
            _settings.Add(new Setting("ScanHTML", null));
            _settings.Add(new Setting("ScanHWP3", null));
            _settings.Add(new Setting("ScanMail", null));
            _settings.Add(new Setting("ScanOLE2", null));
            _settings.Add(new Setting("ScanOnAccess", null));
            _settings.Add(new Setting("ScanPDF", null));
            _settings.Add(new Setting("ScanPE", null));
            _settings.Add(new Setting("ScanPartialMessages", null));
            _settings.Add(new Setting("ScanSWF", null));
            _settings.Add(new Setting("ScanXMLDOCS", null));
            _settings.Add(new Setting("SelfCheck", null));
            _settings.Add(new Setting("SendBufTimeout", null));
            _settings.Add(new Setting("StatsEnabled", null));
            _settings.Add(new Setting("StatsHostID", null));
            _settings.Add(new Setting("StatsPEDisabled", null));
            _settings.Add(new Setting("StatsTimeout", null));
            _settings.Add(new Setting("StreamMaxLength", null));
            _settings.Add(new Setting("StreamMaxPort", null));
            _settings.Add(new Setting("StreamMinPort", null));
            _settings.Add(new Setting("StructuredDataDetection", null));
            _settings.Add(new Setting("StructuredMinCreditCardCount", null));
            _settings.Add(new Setting("StructuredMinSSNCount", null));
            _settings.Add(new Setting("StructuredSSNFormatNormal", null));
            _settings.Add(new Setting("StructuredSSNFormatStripped", null));
            _settings.Add(new Setting("TCPAddr", _host, Setting.ConfigFormat.text));
            _settings.Add(new Setting("TCPSocket", _port, Setting.ConfigFormat.value));
            _settings.Add(new Setting("TemporaryDirectory", null));
            _settings.Add(new Setting("User", null));
            _settings.Add(new Setting("VirusEvent", null));

            // add commandline parameters

            _options = new List<Option>();
            _options.Add(new Option("help"));
            _options.Add(new Option("version"));
            _options.Add(new Option("debug"));
            _options.Add(new Option("quiet"));
            _options.Add(new Option("log"));
            _options.Add(new Option("file-list"));
            _options.Add(new Option("remove"));
            _options.Add(new Option("move"));
            _options.Add(new Option("copy"));
            _options.Add(new Option("config-file", _configFilenamePath, Option.ConfigFormat.text));
            _options.Add(new Option("allmatch"));
            _options.Add(new Option("multiscan"));
            _options.Add(new Option("infected"));
            _options.Add(new Option("no_summary"));
            _options.Add(new Option("reload"));
            _options.Add(new Option("fdpass"));
            _options.Add(new Option("stream"));

            Debug.WriteLine("Out ClamdScan()");
        }

        #endregion
        #region Properties



        #endregion
        #region Methods

        #endregion
        #region Events

        #endregion
        #region Private

        public override void OutputReceived(object sendingProcess, DataReceivedEventArgs outputData)
        {
            if ((outputData != null) && (outputData.Data != null))
            {
                if (outputData.Data.Trim().Length > 0)
                {
                    string data = outputData.Data;
                    if (data.ToUpper().LastIndexOf("FOUND") > 0)
                    {
                        Event notification = new Event("ClamAV", _id, data, Event.EventLevel.Critical);
                        NotificationEventArgs args = new NotificationEventArgs(notification);
                        OnEventReceived(args);
                    }
                    else if (data.ToUpper().LastIndexOf("ERROR") > 0)
                    {
                        Event notification = new Event("ClamAV", _id, data, Event.EventLevel.Error);
                        NotificationEventArgs args = new NotificationEventArgs(notification);
                        OnEventReceived(args);
                    }
                    base.OutputReceived(sendingProcess, outputData);
                }
            }
        }

        public override void ErrorReceived(object sendingProcess, DataReceivedEventArgs errorData)
        {
            if ((errorData != null) && (errorData.Data != null))
            {
                if (errorData.Data.Trim().Length > 0)
                {
                    string data = errorData.Data;
                    if (data.Substring(0, 9).ToUpper() == "WARNING: ")
                    {
                        Event notification = new Event("ClamAV", _id, data.Substring(9, data.Length - 9), Event.EventLevel.Warning);
                        NotificationEventArgs args = new NotificationEventArgs(notification);
                        OnEventReceived(args);
                    }
                    else if (data.Substring(0, 7).ToUpper() == "ERROR: ")
                    {
                        Event notification = new Event("ClamAV", _id, data.Substring(7, data.Length - 7), Event.EventLevel.Error);
                        NotificationEventArgs args = new NotificationEventArgs(notification);
                        OnEventReceived(args);
                    }
                    base.ErrorReceived(sendingProcess, errorData);
                }
            }
        }

        #endregion
    }
    /*
                      Clam AntiVirus: Daemon Client 0.100.2
           By The ClamAV Team: https://www.clamav.net/about.html#credits
           (C) 2007-2018 Cisco Systems, Inc.

    clamdscan [options] [file/directory/-]

    --help              -h             Show this help
    --version           -V             Print version number and exit
    --verbose           -v             Be verbose
    --quiet                            Be quiet, only output error messages
    --stdout                           Write to stdout instead of stderr
                                       (this help is always written to stdout)
    --log=FILE          -l FILE        Save scan report in FILE
    --file-list=FILE    -f FILE        Scan files from FILE
    --remove                           Remove infected files. Be careful!
    --move=DIRECTORY                   Move infected files into DIRECTORY
    --copy=DIRECTORY                   Copy infected files into DIRECTORY
    --config-file=FILE                 Read configuration from FILE.
    --allmatch            -z           Continue scanning within file after finding a match.
    --multiscan           -m           Force MULTISCAN mode
    --infected            -i           Only print infected files
    --no-summary                       Disable summary at end of scanning
    --reload                           Request clamd to reload virus database
    --fdpass                           Pass filedescriptor to clamd (useful if clamd is running as a different user)
    --stream                           Force streaming files to clamd (for debugging and unit testing)
     */
}
