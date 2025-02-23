using TracerLibrary;
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
        #region Fields

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
            Debug.WriteLine("In ClamScan()");

            _id = id;
            _execute = "clamscan.exe";
            if (path.Length == 0)
            {
                path = ".";
            }

            _schedule = new Schedule();
            _schedule.Date = new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day);
            _schedule.Time = new TimeSpan(_schedule.Date.Hour, _schedule.Date.Minute, _schedule.Date.Second);
            
            string basePath = "";
            string name = "clamav";
            switch (location)
            {
                case DataLocation.App:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + System.IO.Path.DirectorySeparatorChar + name;
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
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case Component.DataLocation.Custom:
                    {
                        basePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        int pos = basePath.LastIndexOf('\\');
                        basePath = basePath.Substring(0, pos) + System.IO.Path.DirectorySeparatorChar + name;
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case DataLocation.Local:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + name;
                        if (!Directory.Exists(basePath))
                        {
                            Directory.CreateDirectory(basePath);
                        }
                        break;
                    }
                case DataLocation.Roaming:
                    {
                        basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + System.IO.Path.DirectorySeparatorChar + name;
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
            _executePath = path + System.IO.Path.DirectorySeparatorChar + _execute;

            _settings = new List<Setting>();

            // add command-line parameters

            _options = new List<Option>();
            _options.Add(new Option("help"));
            _options.Add(new Option("version"));
            _options.Add(new Option("verbose"));
            _options.Add(new Option("archive-verbose"));
            _options.Add(new Option("debug"));
            _options.Add(new Option("quiet"));
            _options.Add(new Option("stdout"));
            _options.Add(new Option("no-summary"));
            _options.Add(new Option("infected", Option.ConfigFormat.key));
            _options.Add(new Option("suppress-ok-results"));
            _options.Add(new Option("bell"));
            _options.Add(new Option("tempdir",Option.ConfigFormat.text));
            _options.Add(new Option("leave-temps"));
            _options.Add(new Option("gen-json"));
            _options.Add(new Option("database", _databasePath, Option.ConfigFormat.text));
            _options.Add(new Option("official-db-only"));
            _options.Add(new Option("fail-if-cvd-older-than"));
            _options.Add(new Option("log", _logFilenamePath, Option.ConfigFormat.text));
            _options.Add(new Option("recursive", Option.ConfigFormat.yesno));   // defaults to no
            //_options.Add(new Option("allmatch", Option.ConfigFormat.yesno));  // defaults to no


            /*
             
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
    --memory                             Scan loaded executable modules
    --kill                               Kill/Unload infected loaded modules
    --unload                             Unload infected modules from processes

    --bytecode[=yes(*)/no]               Load bytecode from the database
    --bytecode-unsigned[=yes/no(*)]      Load unsigned bytecode
                                         **Caution**: You should NEVER run bytecode signatures from untrusted sources.
                                         Doing so may result in arbitrary code execution.
    --bytecode-timeout=N                 Set bytecode timeout (in milliseconds)
    --statistics[=none(*)/bytecode/pcre] Collect and print execution statistics
    --detect-pua[=yes/no(*)]             Detect Possibly Unwanted Applications
    --exclude-pua=CAT                    Skip PUA sigs of category CAT
    --include-pua=CAT                    Load PUA sigs of category CAT
    --detect-structured[=yes/no(*)]      Detect structured data (SSN, Credit Card)
    --structured-ssn-format=X            SSN format (0=normal,1=stripped,2=both)
    --structured-ssn-count=N             Min SSN count to generate a detect
    --structured-cc-count=N              Min CC count to generate a detect
    --structured-cc-mode=X               CC mode (0=credit debit and private label, 1=credit cards only
    --scan-mail[=yes(*)/no]              Scan mail files
    --phishing-sigs[=yes(*)/no]          Enable email signature-based phishing detection
    --phishing-scan-urls[=yes(*)/no]     Enable URL signature-based phishing detection
    --heuristic-alerts[=yes(*)/no]       Heuristic alerts
    --heuristic-scan-precedence[=yes/no(*)] Stop scanning as soon as a heuristic match is found
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
    --alert-broken[=yes/no(*)]           Alert on broken executable files (PE & ELF)
    --alert-broken-media[=yes/no(*)]     Alert on broken graphics files (JPEG, TIFF, PNG, GIF)
    --alert-encrypted[=yes/no(*)]        Alert on encrypted archives and documents
    --alert-encrypted-archive[=yes/no(*)] Alert on encrypted archives
    --alert-encrypted-doc[=yes/no(*)]    Alert on encrypted documents
    --alert-macros[=yes/no(*)]           Alert on OLE2 files containing VBA macros
    --alert-exceeds-max[=yes/no(*)]      Alert on files that exceed max file size, max scan size, or max recursion limit
    --alert-phishing-ssl[=yes/no(*)]     Alert on emails containing SSL mismatches in URLs
    --alert-phishing-cloak[=yes/no(*)]   Alert on emails containing cloaked URLs
    --alert-partition-intersection[=yes/no(*)] Alert on raw DMG image files containing partition intersections
    --nocerts                            Disable authenticode certificate chain verification in PE files
    --dumpcerts                          Dump authenticode certificate chain in PE files

    --max-scantime=#n                    Scan time longer than this will be skipped and assumed clean (milliseconds)
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

             */


            Debug.WriteLine("Out ClamScan()");
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
    --memory                             Scan loaded executable modules
    --kill                               Kill/Unload infected loaded modules
    --unload                             Unload infected modules from processes
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
