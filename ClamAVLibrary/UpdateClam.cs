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
    public class UpdateClam : Component
    {
        #region Fields

        #endregion
        #region Constructors

        public UpdateClam() : this("", DataLocation.Program)
        {
        }

        public UpdateClam(string id) : this(id, DataLocation.Program)
        {
        }

        public UpdateClam(string id, DataLocation location)
        {
            Debug.WriteLine("In UpdateClam()");

            _id = id;
            _execute = "updateclam.exe";

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
            _logFilenamePath = _logPath + System.IO.Path.DirectorySeparatorChar + "updateclam.log";
            _configFilenamePath = basePath + System.IO.Path.DirectorySeparatorChar + "updateclam.conf";

            // Not sure about the hardcoding here

            _executePath = "c:\\program files\\clamav" + System.IO.Path.DirectorySeparatorChar + _execute;

            _settings = new List<Setting>();

            // Add command line options

            _options = new List<Option>();
            _options.Add(new Option("appdir"));
            _options.Add(new Option("tempdir"));
            _options.Add(new Option("force"));

            Debug.WriteLine("Out UpdateClam()");
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
                if (outputData.Data.Trim() != "")
                {
                    string data = outputData.Data;
                    if (data.ToUpper().LastIndexOf("PAUSE") > 0)
                    {
                        Command command = new Command("ClamAV", _id, data, Command.CommandType.Pause);
                        CommandEventArgs args = new CommandEventArgs(command);
                        OnCommandReceived(args);
                    }
                    else if (data.ToUpper().LastIndexOf("RESUME") > 0)
                    {
                        Command command = new Command("ClamAV", _id, data, Command.CommandType.Resume);
                        CommandEventArgs args = new CommandEventArgs(command);
                        OnCommandReceived(args);
                    }

                    base.OutputReceived(sendingProcess, outputData);
                }
            }
        }

        public override void ErrorReceived(object sendingProcess, DataReceivedEventArgs errorData)
        {
            if ((errorData != null) && (errorData.Data != null))
            {
                if (errorData.Data.Trim() != "")
                {
                    string data = errorData.Data;
                    if (data.Length > 9)
                    {
                        if (data.Substring(0, 9).ToUpper() == "WARNING: ")
                        {
                            Event notification = new Event("ClamAV", _id, data.Substring(9, data.Length - 9), Event.EventLevel.Warning);
                            NotificationEventArgs args = new NotificationEventArgs(notification);
                            OnEventReceived(args);
                        }
                    }
                    else if (data.Length > 7)
                    {
                        if (data.Substring(0, 7).ToUpper() == "ERROR: ")
                        {
                            Event notification = new Event("ClamAV", _id, data.Substring(7, data.Length - 7), Event.EventLevel.Error);
                            NotificationEventArgs args = new NotificationEventArgs(notification);
                            OnEventReceived(args);
                        }
                    }
                    base.ErrorReceived(sendingProcess, errorData);
                }
            }
        }

        #endregion
    }

    /*
                       Clam AntiVirus: Application Updater 0.1.0
           By The Green Team: https://www.32high.co.uk
           (C) 2020 32High

    clamupdate [options]

    --force                -f         Force the update
    --help                 -h         Show this help
    --progress             -p         Show progress bar
    --appdir=DIRECTORY                Install new application into DIRECTORY
    --tempdir=DIRECTORY               Download installer into DIRECTORY
     

     */
}