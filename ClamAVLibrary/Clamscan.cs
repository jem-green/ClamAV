using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace ClamAVLibrary
{
    /// <summary>
    /// Wrapper class to manage and launch clamscan
    /// </summary>
    public class ClamScan : Component
    {
        #region Variables

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #endregion
        #region Constructors

        public ClamScan(string id) : this(id, DataLocation.Program, "")
        {
        }

        public ClamScan(string id, DataLocation location) : this(id, location, "")
        {
        }

        public ClamScan(string id, DataLocation location, string path)
        {
            log.Debug("In ClamScan()");

            _id = id;
            _execute = "clamscan.exe";
            if (path.Length > 0)
            {
                _path = path;
            }
            else
            {
                _path = ".";
            }

            _schedule = new Schedule();
            _schedule.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
            _schedule.Time = new TimeSpan(_schedule.Date.Hour, _schedule.Date.Minute, _schedule.Date.Second); string basePath = "";

            switch (location)
            {
                case DataLocation.App:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case DataLocation.Program:
                    {
                        basePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        int pos = basePath.LastIndexOf('\\');
                        basePath = basePath.Substring(0, pos);
                        break;
                    }
                case DataLocation.Local:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case DataLocation.Roaming:
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
            _logFilenamePath = _logPath + System.IO.Path.DirectorySeparatorChar + "clamscan.log";
            _configFilenamePath = basePath + System.IO.Path.DirectorySeparatorChar + "clamscan.conf";
            _executePath = basePath + System.IO.Path.DirectorySeparatorChar + _execute;

            _settings = new List<Setting>();
            //_settings.Add(new Setting("AlgorithmicDetection", null));
            //_settings.Add(new Setting("AllowAllMatchScan", null));
            //_settings.Add(new Setting("AllowSupplementaryGroups", null));
            //_settings.Add(new Setting("ArchiveBlockEncrypted", null));
            //_settings.Add(new Setting("Bytecode", null));
            //_settings.Add(new Setting("BytecodeSecurity", null));
            //_settings.Add(new Setting("BytecodeTimeout", null));
            //_settings.Add(new Setting("CommandReadTimeout", null));
            //_settings.Add(new Setting("CrossFilesystems", null));
            //_settings.Add(new Setting("DatabaseDirectory", null));
            //_settings.Add(new Setting("Debug", null));
            //_settings.Add(new Setting("DetectBrokenExecutables", null));
            //_settings.Add(new Setting("DetectPUA", null));
            //_settings.Add(new Setting("DisableCache", null));
            //_settings.Add(new Setting("DisableCertCheck", null));
            //_settings.Add(new Setting("ExcludePUA", null));
            //_settings.Add(new Setting("ExcludePath", null));
            //_settings.Add(new Setting("ExitOnOOM", null));
            //_settings.Add(new Setting("ExtendedDetectionInfo", null));
            //_settings.Add(new Setting("FixStaleSocket", null));
            //_settings.Add(new Setting("FollowDirectorySymlinks", null));
            //_settings.Add(new Setting("FollowFileSymlinks", null));
            //_settings.Add(new Setting("ForceToDisk", null));
            //_settings.Add(new Setting("Foreground", null));
            //_settings.Add(new Setting("HeuristicScanPrecedence", null));
            //_settings.Add(new Setting("IdleTimeout", null));
            //_settings.Add(new Setting("IncludePUA", null));
            //_settings.Add(new Setting("LeaveTemporaryFiles", null));
            //_settings.Add(new Setting("LocalSocketGroup", null));
            //_settings.Add(new Setting("LocalSocketMode", null));
            //_settings.Add(new Setting("LogClean", null));
            //_settings.Add(new Setting("LogFacility", null));
            //_settings.Add(new Setting("LogFile", null));
            //_settings.Add(new Setting("LogFileMaxSize", null));
            //_settings.Add(new Setting("LogFileUnlock", null));
            //_settings.Add(new Setting("LogRotate", null));
            //_settings.Add(new Setting("LogSyslog", null));
            //_settings.Add(new Setting("LogTime", null));
            //_settings.Add(new Setting("LogVerbose", null));
            //_settings.Add(new Setting("MaxConnectionQueueLength", null));
            //_settings.Add(new Setting("MaxDirectoryRecursion", null));
            //_settings.Add(new Setting("MaxEmbeddedPE", null));
            //_settings.Add(new Setting("MaxFileSize", null));
            //_settings.Add(new Setting("MaxFiles", null));
            //_settings.Add(new Setting("MaxHTMLNoTags", null));
            //_settings.Add(new Setting("MaxHTMLNormalize", null));
            //_settings.Add(new Setting("MaxIconsPE", null));
            //_settings.Add(new Setting("MaxPartitions", null));
            //_settings.Add(new Setting("MaxQueue", null));
            //_settings.Add(new Setting("MaxRecHWP3", null));
            //_settings.Add(new Setting("MaxRecursion", null));
            //_settings.Add(new Setting("MaxScanSize", null));
            //_settings.Add(new Setting("MaxScriptNormalize", null));
            //_settings.Add(new Setting("MaxThreads", null));
            //_settings.Add(new Setting("MaxZipTypeRcg", null));
            //_settings.Add(new Setting("OLE2BlockMacros", null));
            //_settings.Add(new Setting("OfficialDatabaseOnly", null));
            //_settings.Add(new Setting("OnAccessDisableDDD", null));
            //_settings.Add(new Setting("OnAccessExcludePath", null));
            //_settings.Add(new Setting("OnAccessExcludeUID", null));
            //_settings.Add(new Setting("OnAccessExtraScanning", null));
            //_settings.Add(new Setting("OnAccessIncludePath", null));
            //_settings.Add(new Setting("OnAccessMaxFileSize", null));
            //_settings.Add(new Setting("OnAccessMountPath", null));
            //_settings.Add(new Setting("OnAccessPrevention", null));
            //_settings.Add(new Setting("PCREMatchLimit", null));
            //_settings.Add(new Setting("PCREMaxFileSize", null));
            //_settings.Add(new Setting("PCRERecMatchLimit", null));
            //_settings.Add(new Setting("PartitionIntersection", null));
            //_settings.Add(new Setting("PhishingAlwaysBlockCloak", null));
            //_settings.Add(new Setting("PhishingAlwaysBlockSSLMismatch", null));
            //_settings.Add(new Setting("PhishingScanURLs", null));
            //_settings.Add(new Setting("PhishingSignatures", null));
            //_settings.Add(new Setting("PidFile", null));
            //_settings.Add(new Setting("ReadTimeout", null));
            //_settings.Add(new Setting("ScanArchive", null));
            //_settings.Add(new Setting("ScanELF", null));
            //_settings.Add(new Setting("ScanHTML", null));
            //_settings.Add(new Setting("ScanHWP3", null));
            //_settings.Add(new Setting("ScanMail", null));
            //_settings.Add(new Setting("ScanOLE2", null));
            //_settings.Add(new Setting("ScanOnAccess", null));
            //_settings.Add(new Setting("ScanPDF", null));
            //_settings.Add(new Setting("ScanPE", null));
            //_settings.Add(new Setting("ScanPartialMessages", null));
            //_settings.Add(new Setting("ScanSWF", null));
            //_settings.Add(new Setting("ScanXMLDOCS", null));
            //_settings.Add(new Setting("SelfCheck", null));
            //_settings.Add(new Setting("SendBufTimeout", null));
            //_settings.Add(new Setting("StatsEnabled", null));
            //_settings.Add(new Setting("StatsHostID", null));
            //_settings.Add(new Setting("StatsPEDisabled", null));
            //_settings.Add(new Setting("StatsTimeout", null));
            //_settings.Add(new Setting("StreamMaxLength", null));
            //_settings.Add(new Setting("StreamMaxPort", null));
            //_settings.Add(new Setting("StreamMinPort", null));
            //_settings.Add(new Setting("StructuredDataDetection", null));
            //_settings.Add(new Setting("StructuredMinCreditCardCount", null));
            //_settings.Add(new Setting("StructuredMinSSNCount", null));
            //_settings.Add(new Setting("StructuredSSNFormatNormal", null));
            //_settings.Add(new Setting("StructuredSSNFormatStripped", null));
            //_settings.Add(new Setting("TCPAddr", null));
            //_settings.Add(new Setting("TCPSocket", null));
            //_settings.Add(new Setting("TemporaryDirectory", null));
            //_settings.Add(new Setting("User", null));
            //_settings.Add(new Setting("VirusEvent", null));

            // add commandline parameters

            _options = new List<Option>();
            _options.Add(new Option("help"));
            _options.Add(new Option("version"));
            _options.Add(new Option("verbose"));
            _options.Add(new Option("database", _databasePath, Option.ConfigFormat.text));
            _options.Add(new Option("log", _logFilenamePath, Option.ConfigFormat.text));
            _options.Add(new Option("recursive", "", Option.ConfigFormat.key));
            _options.Add(new Option("infected", "", Option.ConfigFormat.key));

            //  --exclude="[^\]*\.dbx$" --exclude="[^\]*\.tbb$" --exclude="[^\]*\.pst$" --exclude="[^\]*\.dat$" --exclude="[^\]*\.log$" --exclude="[^\]*\.chm$" -i C:\

            /*

_options.Add(new Option("archive-verbose"))
_options.Add(new Option("debug"
_options.Add(new Option("quiet"
_options.Add(new Option("stdout"
_options.Add(new Option("no-summary
_options.Add(new Option("infected
_options.Add(new Option("no-summary
_options.Add(new Option("suppress-ok-results
_options.Add(new Option("bell
_options.Add(new Option("tempdir
_options.Add(new Option("leave-temps
_options.Add(new Option("gen-json
_options.Add(new Option("database
_options.Add(new Option("official-db-only
_options.Add(new Option("log
_options.Add(new Option("recursive
_options.Add(new Option("allmatch
_options.Add(new Option("cross-fs
_options.Add(new Option("follow-dir-symlinks
_options.Add(new Option("follow-file-symlinks
_options.Add(new Option("file-list
_options.Add(new Option("remove
_options.Add(new Option("move
_options.Add(new Option("copy
_options.Add(new Option("exclude
_options.Add(new Option("exclude-dir
_options.Add(new Option("include
_options.Add(new Option("include-dir
_options.Add(new Option("bytecode
_options.Add(new Option("bytecode-unsigned
_options.Add(new Option("bytecode-timeout
_options.Add(new Option("statistics
_options.Add(new Option("detect-pua
_options.Add(new Option("exclude-pua
_options.Add(new Option("include-pua
_options.Add(new Option("detect-structured
_options.Add(new Option("structured-ssn-format=X            SSN format (0=normal,1=stripped,2=both)
    --structured-ssn-count=N             Min SSN count to generate a detect
    --structured-cc-count=N              Min CC count to generate a detect
    --scan-mail[=yes(*)/no]              Scan mail files
    --phishing-sigs[=yes(*)/no]          Signature-based phishing detection
    --phishing-scan-urls[=yes(*)/no]     URL-based phishing detection
    --heuristic-scan-precedence[=yes/no(*)] Stop scanning as soon as a heuristic match is found
    --phishing-ssl[=yes/no(*)]           Always block (flag) SSL mismatches in URLs (phishing module)
    --phishing-cloak[=yes/no(*)]         Always block (flag) cloaked URLs (phishing module)
    --partition-intersection[=yes/no(*)] Detect partition intersections in raw disk images using heuristics
    --algorithmic-detection[=yes(*)/no]  Algorithmic detection
    --normalize[=yes(*)/no]              Normalize html, script, and text files. Use normalize=no for yara compatibility
    --scan-pe[=yes(*)/no]                Scan PE files
    --scan-elf[=yes(*)/no]               Scan ELF files
    --scan-ole2[=yes(*)/no]              Scan OLE2 containers
    --scan-pdf[=yes(*)/no]               Scan PDF files
    --scan-swf[=yes(*)/no]               Scan SWF files
    --scan-html[=yes(*)/no]              Scan HTML files
    --scan-xmldocs[=yes(*)/no]           Scan xml-based document files
    --scan-hwp3[=yes(*)/no]              Scan HWP3 files
    --scan-archive[=yes(*)/no]           Scan archive files (supported by libclamav)
    --detect-broken[=yes/no(*)]          Try to detect broken executable files
    --block-encrypted[=yes/no(*)]        Block (flag) encrypted archives
    --block-macros[=yes/no(*)]           Block (flag) OLE2 files with VBA macros
    --block-max[=yes/no(*)]              Block (flag) files that exceed max file size, max scan size, or max recursion limit
    --nocerts                            Disable authenticode certificate chain verification in PE files
    --dumpcerts                          Dump authenticode certificate chain in PE files

    --max-filesize=#n                    Files larger than this will be skipped and assumed clean
    --max-scansize=#n                    The maximum amount of data to scan for each container file (**)
    --max-files=#n                       The maximum number of files to scan for each container file (**)
    --max-recursion=#n                   Maximum archive recursion level for container file (**)
    --max-dir-recursion=#n               Maximum directory recursion level
    --max-embeddedpe=#n                  Maximum size file to check for embedded PE
    --max-htmlnormalize=#n               Maximum size of HTML file to normalize
    --max-htmlnotags=#n                  Maximum size of normalized HTML file to scan
    --max-scriptnormalize=#n             Maximum size of script file to normalize
    --max-ziptypercg=#n                  Maximum size zip to type reanalyze
    --max-partitions=#n                  Maximum number of partitions in disk image to be scanned
    --max-iconspe=#n                     Maximum number of icons in PE file to be scanned
    --max-rechwp3=#n                     Maximum recursive calls to HWP3 parsing function
    --pcre-match-limit=#n                Maximum calls to the PCRE match function.
    --pcre-recmatch-limit=#n             Maximum recursive calls to the PCRE match function.
    --pcre-max-filesize=#n               Maximum size file to perform PCRE subsig matching.
    --disable-cache                      Disable caching and cache checks for hash sums of scanned files.

             * 
             */
            log.Debug("Out ClamScan()");
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
   
                       Clam AntiVirus: Scanner 0.100.2
           By The ClamAV Team: https://www.clamav.net/about.html#credits
           (C) 2007-2018 Cisco Systems, Inc.

    clamscan [options] [file/directory/-]

    --help                -h             Show this help
    --version             -V             Print version number
    --verbose             -v             Be verbose
    --archive-verbose     -a             Show filenames inside scanned archives
    --debug                              Enable libclamav's debug messages
    --quiet                              Only output error messages
    --stdout                             Write to stdout instead of stderr
    --no-summary                         Disable summary at end of scanning
    --infected            -i             Only print infected files
    --suppress-ok-results -o             Skip printing OK files
    --bell                               Sound bell on virus detection

    --tempdir=DIRECTORY                  Create temporary files in DIRECTORY
    --leave-temps[=yes/no(*)]            Do not remove temporary files
    --gen-json[=yes/no(*)]               Generate JSON description of scanned file(s). JSON will be printed and also-
                                         dropped to the temp directory if --leave-temps is enabled.
    --database=FILE/DIR   -d FILE/DIR    Load virus database from FILE or load all supported db files from DIR
    --official-db-only[=yes/no(*)]       Only load official signatures
    --log=FILE            -l FILE        Save scan report to FILE
    --recursive[=yes/no(*)]  -r          Scan subdirectories recursively
    --allmatch[=yes/no(*)]   -z          Continue scanning within file after finding a match
    --cross-fs[=yes(*)/no]               Scan files and directories on other filesystems
    --follow-dir-symlinks[=0/1(*)/2]     Follow directory symlinks (0 = never, 1 = direct, 2 = always)
    --follow-file-symlinks[=0/1(*)/2]    Follow file symlinks (0 = never, 1 = direct, 2 = always)
    --file-list=FILE      -f FILE        Scan files from FILE
    --remove[=yes/no(*)]                 Remove infected files. Be careful!
    --move=DIRECTORY                     Move infected files into DIRECTORY
    --copy=DIRECTORY                     Copy infected files into DIRECTORY
    --exclude=REGEX                      Don't scan file names matching REGEX
    --exclude-dir=REGEX                  Don't scan directories matching REGEX
    --include=REGEX                      Only scan file names matching REGEX
    --include-dir=REGEX                  Only scan directories matching REGEX

    --bytecode[=yes(*)/no]               Load bytecode from the database
    --bytecode-unsigned[=yes/no(*)]      Load unsigned bytecode
    --bytecode-timeout=N                 Set bytecode timeout (in milliseconds)
    --statistics[=none(*)/bytecode/pcre] Collect and print execution statistics
    --detect-pua[=yes/no(*)]             Detect Possibly Unwanted Applications
    --exclude-pua=CAT                    Skip PUA sigs of category CAT
    --include-pua=CAT                    Load PUA sigs of category CAT
    --detect-structured[=yes/no(*)]      Detect structured data (SSN, Credit Card)
    --structured-ssn-format=X            SSN format (0=normal,1=stripped,2=both)
    --structured-ssn-count=N             Min SSN count to generate a detect
    --structured-cc-count=N              Min CC count to generate a detect
    --scan-mail[=yes(*)/no]              Scan mail files
    --phishing-sigs[=yes(*)/no]          Signature-based phishing detection
    --phishing-scan-urls[=yes(*)/no]     URL-based phishing detection
    --heuristic-scan-precedence[=yes/no(*)] Stop scanning as soon as a heuristic match is found
    --phishing-ssl[=yes/no(*)]           Always block (flag) SSL mismatches in URLs (phishing module)
    --phishing-cloak[=yes/no(*)]         Always block (flag) cloaked URLs (phishing module)
    --partition-intersection[=yes/no(*)] Detect partition intersections in raw disk images using heuristics
    --algorithmic-detection[=yes(*)/no]  Algorithmic detection
    --normalize[=yes(*)/no]              Normalize html, script, and text files. Use normalize=no for yara compatibility
    --scan-pe[=yes(*)/no]                Scan PE files
    --scan-elf[=yes(*)/no]               Scan ELF files
    --scan-ole2[=yes(*)/no]              Scan OLE2 containers
    --scan-pdf[=yes(*)/no]               Scan PDF files
    --scan-swf[=yes(*)/no]               Scan SWF files
    --scan-html[=yes(*)/no]              Scan HTML files
    --scan-xmldocs[=yes(*)/no]           Scan xml-based document files
    --scan-hwp3[=yes(*)/no]              Scan HWP3 files
    --scan-archive[=yes(*)/no]           Scan archive files (supported by libclamav)
    --detect-broken[=yes/no(*)]          Try to detect broken executable files
    --block-encrypted[=yes/no(*)]        Block (flag) encrypted archives
    --block-macros[=yes/no(*)]           Block (flag) OLE2 files with VBA macros
    --block-max[=yes/no(*)]              Block (flag) files that exceed max file size, max scan size, or max recursion limit
    --nocerts                            Disable authenticode certificate chain verification in PE files
    --dumpcerts                          Dump authenticode certificate chain in PE files

    --max-filesize=#n                    Files larger than this will be skipped and assumed clean
    --max-scansize=#n                    The maximum amount of data to scan for each container file (**)
    --max-files=#n                       The maximum number of files to scan for each container file (**)
    --max-recursion=#n                   Maximum archive recursion level for container file (**)
    --max-dir-recursion=#n               Maximum directory recursion level
    --max-embeddedpe=#n                  Maximum size file to check for embedded PE
    --max-htmlnormalize=#n               Maximum size of HTML file to normalize
    --max-htmlnotags=#n                  Maximum size of normalized HTML file to scan
    --max-scriptnormalize=#n             Maximum size of script file to normalize
    --max-ziptypercg=#n                  Maximum size zip to type reanalyze
    --max-partitions=#n                  Maximum number of partitions in disk image to be scanned
    --max-iconspe=#n                     Maximum number of icons in PE file to be scanned
    --max-rechwp3=#n                     Maximum recursive calls to HWP3 parsing function
    --pcre-match-limit=#n                Maximum calls to the PCRE match function.
    --pcre-recmatch-limit=#n             Maximum recursive calls to the PCRE match function.
    --pcre-max-filesize=#n               Maximum size file to perform PCRE subsig matching.
    --disable-cache                      Disable caching and cache checks for hash sums of scanned files.

Pass in - as the filename for stdin.

(*) Default scan settings
(**) Certain files (e.g. documents, archives, etc.) may in turn contain other
   files inside. The above options ensure safe processing of this kind of data.

     */

}
