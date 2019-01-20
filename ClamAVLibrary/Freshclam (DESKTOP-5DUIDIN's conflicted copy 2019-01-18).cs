﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Diagnostics;

namespace ClamAVLibrary
{
    class Freshclam
    {
        #region Variables
        string _database = "";
        string _configuration = "";
        Process proces;

        #endregion
        #region Constructors
        #endregion
        Freshclam()
        {

        }
        Freshclam(string database, string configuration)
        {
            _database = database;
            _configuration = configuration;
        }
        #region Properties

        string Database
        {
            set
            {
                _database = value;
            }
            get
            {
                return (_database);
            }
        }

        string Configuration
        {
            set
            {
                _configuration = value;
            }
            get
            {
                return (_configuration);
            }
        }


        #endregion
        #region Methods

        /// <summary>
        /// Start a new freshclam process
        /// </summary>
        void Start()
        {

        }
        #endregion
        #region Private
        #endregion
    }

    /* 
                      Clam AntiVirus: Database Updater 0.100.2
           By The ClamAV Team: https://www.clamav.net/about.html#credits
           (C) 2007-2018 Cisco Systems, Inc.

    freshclam [options]

    --help               -h              Show this help
    --version            -V              Print version number and exit
    --verbose            -v              Be verbose
    --debug                              Enable debug messages
    --quiet                              Only output error messages
    --no-warnings                        Don't print and log warnings
    --stdout                             Write to stdout instead of stderr
    --show-progress                      Show download progress percentage
    --config-file=FILE                   Read configuration from FILE.
    --log=FILE           -l FILE         Log into FILE
    --no-dns                             Force old non-DNS verification method
    --checks=#n          -c #n           Number of checks per day, 1 <= n <= 50
    --datadir=DIRECTORY                  Download new databases into DIRECTORY
    --daemon-notify[=/path/clamd.conf]   Send RELOAD command to clamd
    --local-address=IP   -a IP           Bind to IP for HTTP downloads
    --on-update-execute=COMMAND          Execute COMMAND after successful update
    --on-error-execute=COMMAND           Execute COMMAND if errors occurred
    --on-outdated-execute=COMMAND        Execute COMMAND when software is outdated
    --list-mirrors                       Print mirrors from mirrors.dat
    --update-db=DBNAME                   Only update database DBNAME
    */

    /*
        # Path to the database directory.
        # WARNING: It must match clamd.conf's directive!
        # Default: hardcoded (depends on installation options)
        DatabaseDirectory C:\Program files\ClamAV\database

        # Path to the log file (make sure it has proper permissions)
        # Default: disabled
        UpdateLogFile  C:\program files\ClamAV\logs\freshclam.log

        # Maximum size of the log file.
        # Value of 0 disables the limit.
        # You may use 'M' or 'm' for megabytes (1M = 1m = 1048576 bytes)
        # and 'K' or 'k' for kilobytes (1K = 1k = 1024 bytes).
        # in bytes just don't use modifiers. If LogFileMaxSize is enabled,
        # log rotation (the LogRotate option) will always be enabled.
        # Default: 1M
        LogFileMaxSize 20480000

        # Log time with each message.
        # Default: no
        LogTime yes

        # Enable verbose logging.
        # Default: no
        #LogVerbose yes

        # Use system logger (can work together with UpdateLogFile).
        # Default: no
        #LogSyslog yes

        # Specify the type of syslog messages - please refer to 'man syslog'
        # for facility names.
        # Default: LOG_LOCAL6
        #LogFacility LOG_MAIL

        # Enable log rotation. Always enabled when LogFileMaxSize is enabled.
        # Default: no
        #LogRotate yes

        # This option allows you to save the process identifier of the daemon
        # Default: disabled
        #PidFile /var/run/freshclam.pid

        # By default when started freshclam drops privileges and switches to the
        # "clamav" user. This directive allows you to change the database owner.
        # Default: clamav (may depend on installation options)
        #DatabaseOwner clamav

        # Initialize supplementary group access (freshclam must be started by root).
        # Default: no
        #AllowSupplementaryGroups yes

        # Use DNS to verify virus database version. Freshclam uses DNS TXT records
        # to verify database and software versions. With this directive you can change
        # the database verification domain.
        # WARNING: Do not touch it unless you're configuring freshclam to use your
        # own database verification domain.
        # Default: current.cvd.clamav.net
        #DNSDatabaseInfo current.cvd.clamav.net

        # Uncomment the following line and replace XY with your country
        # code. See http://www.iana.org/cctld/cctld-whois.htm for the full list.
        # You can use db.XY.ipv6.clamav.net for IPv6 connections.
        #DatabaseMirror db.XY.clamav.net

        # database.clamav.net is a round-robin record which points to our most 
        # reliable mirrors. It's used as a fall back in case db.XY.clamav.net is 
        # not working. DO NOT TOUCH the following line unless you know what you
        # are doing.
        DatabaseMirror database.clamav.net

        # How many attempts to make before giving up.
        # Default: 3 (per mirror)
        MaxAttempts 3

        # With this option you can control scripted updates. It's highly recommended
        # to keep it enabled.
        # Default: yes
        #ScriptedUpdates yes

        # By default freshclam will keep the local databases (.cld) uncompressed to
        # make their handling faster. With this option you can enable the compression;
        # the change will take effect with the next database update.
        # Default: no
        #CompressLocalDatabase no

        # With this option you can provide custom sources (http:// or file://) for
        # database files. This option can be used multiple times.
        # Default: no custom URLs
        #DatabaseCustomURL http://myserver.com/mysigs.ndb
        #DatabaseCustomURL file:///mnt/nfs/local.hdb

        # This option allows you to easily point freshclam to private mirrors.
        # If PrivateMirror is set, freshclam does not attempt to use DNS
        # to determine whether its databases are out-of-date, instead it will
        # use the If-Modified-Since request or directly check the headers of the
        # remote database files. For each database, freshclam first attempts
        # to download the CLD file. If that fails, it tries to download the
        # CVD file. This option overrides DatabaseMirror, DNSDatabaseInfo
        # and ScriptedUpdates. It can be used multiple times to provide
        # fall-back mirrors.
        # Default: disabled
        #PrivateMirror mirror1.mynetwork.com
        #PrivateMirror mirror2.mynetwork.com

        # Number of database checks per day.
        # Default: 12 (every two hours)
        #Checks 24

        # Proxy settings
        # Default: disabled
        #HTTPProxyServer myproxy.com
        #HTTPProxyPort 1234
        #HTTPProxyUsername myusername
        #HTTPProxyPassword mypass

        # If your servers are behind a firewall/proxy which applies User-Agent
        # filtering you can use this option to force the use of a different
        # User-Agent header.
        # Default: clamav/version_number
        #HTTPUserAgent SomeUserAgentIdString

        # Use aaa.bbb.ccc.ddd as client address for downloading databases. Useful for
        # multi-homed systems.
        # Default: Use OS'es default outgoing IP address.
        #LocalIPAddress aaa.bbb.ccc.ddd

        # Send the RELOAD command to clamd.
        # Default: no
        NotifyClamd C:\Program files\ClamAV\clamd.conf

        # Run command after successful database update.
        # Default: disabled
        #OnUpdateExecute command

        # Run command when database update process fails.
        # Default: disabled
        #OnErrorExecute command

        # Run command when freshclam reports outdated version.
        # In the command string %v will be replaced by the new version number.
        # Default: disabled
        #OnOutdatedExecute command

        # Don't fork into background.
        # Default: no
        #Foreground yes

        # Enable debug messages in libclamav.
        # Default: no
        #Debug yes

        # Timeout in seconds when connecting to database server.
        # Default: 30
        #ConnectTimeout 60

        # Timeout in seconds when reading from database server.
        # Default: 30
        #ReceiveTimeout 60

        # With this option enabled, freshclam will attempt to load new
        # databases into memory to make sure they are properly handled
        # by libclamav before replacing the old ones.
        # Default: yes
        #TestDatabases yes

        # When enabled freshclam will submit statistics to the ClamAV Project about
        # the latest virus detections in your environment. The ClamAV maintainers
        # will then use this data to determine what types of malware are the most
        # detected in the field and in what geographic area they are.
        # Freshclam will connect to clamd in order to get recent statistics.
        # Default: no
        #SubmitDetectionStats /path/to/clamd.conf

        # Country of origin of malware/detection statistics (for statistical
        # purposes only). The statistics collector at ClamAV.net will look up
        # your IP address to determine the geographical origin of the malware
        # reported by your installation. If this installation is mainly used to
        # scan data which comes from a different location, please enable this
        # option and enter a two-letter code (see http://www.iana.org/domains/root/db/)
        # of the country of origin.
        # Default: disabled
        #DetectionStatsCountry country-code

        # This option enables support for our "Personal Statistics" service. 
        # When this option is enabled, the information on malware detected by
        # your clamd installation is made available to you through our website.
        # To get your HostID, log on http://www.stats.clamav.net and add a new
        # host to your host list. Once you have the HostID, uncomment this option
        # and paste the HostID here. As soon as your freshclam starts submitting
        # information to our stats collecting service, you will be able to view
        # the statistics of this clamd installation by logging into
        # http://www.stats.clamav.net with the same credentials you used to
        # generate the HostID. For more information refer to:
        # http://www.clamav.net/documentation.html#cctts 
        # This feature requires SubmitDetectionStats to be enabled.
        # Default: disabled
        #DetectionStatsHostID unique-id

        # This option enables support for Google Safe Browsing. When activated for
        # the first time, freshclam will download a new database file (safebrowsing.cvd)
        # which will be automatically loaded by clamd and clamscan during the next
        # reload, provided that the heuristic phishing detection is turned on. This
        # database includes information about websites that may be phishing sites or
        # possible sources of malware. When using this option, it's mandatory to run
        # freshclam at least every 30 minutes.
        # Freshclam uses the ClamAV's mirror infrastructure to distribute the
        # database and its updates but all the contents are provided under Google's
        # terms of use. See http://www.google.com/transparencyreport/safebrowsing
        # and http://www.clamav.net/documentation.html#safebrowsing 
        # for more information.
        # Default: disabled
        #SafeBrowsing yes

        # This option enables downloading of bytecode.cvd, which includes additional
        # detection mechanisms and improvements to the ClamAV engine.
        # Default: enabled
        #Bytecode yes

        # Download an additional 3rd party signature database distributed through
        # the ClamAV mirrors. 
        # This option can be used multiple times.
        #ExtraDatabase dbname1
        #ExtraDatabase dbname2
    */


}
