using ClamAVLibrary;
using log4net;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace ClamAVConsole
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        public static bool isClosing = false;
        static private HandlerRoutine ctrlCHandler;
        static private ClamAV clamAV;

        #region unmanaged

        // Declare the SetConsoleCtrlHandler function
        // as external and receiving a delegate.

        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

        // A delegate type to be used as the handler routine
        // for SetConsoleCtrlHandler.
        public delegate bool HandlerRoutine(CtrlTypes CtrlType);

        // An enumerated type for the control messages
        // sent to the handler routine.
        public enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

        #endregion

        static void Main(string[] args)
        {
            log.Debug("In main()");

            ctrlCHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(ctrlCHandler, true);

            int pos = 0;
            Parameter clamAVPath = new Parameter("");
            Parameter clamAVName = new Parameter("clamav.xml");

            clamAVPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = clamAVPath.Value.LastIndexOf('\\');
            clamAVPath.Value = clamAVPath.Value.Substring(0, pos);
            clamAVPath.Source = Parameter.SourceType.App;

            Parameter logPath = new Parameter("");
            Parameter logName = new Parameter("clamavconsole.log");
            logPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = logPath.Value.LastIndexOf('\\');
            logPath.Value = logPath.Value.Substring(0, pos);
            logPath.Source = Parameter.SourceType.App;

            //RegistryKey key = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            RegistryKey key = Registry.LocalMachine;
            string keys = "software\\green\\clamav\\";
            foreach (string subkey in keys.Split('\\'))
            {
                key = key.OpenSubKey(subkey);
                if (key == null)
                {
                    log.Debug("Failed to open" + subkey);
                    break;
                }
            }

            // Get the log path

            try
            {
                if (key.GetValue("logpath", "").ToString().Length > 0)
                {
                    logPath.Value = (string)key.GetValue("logpath", logPath);
                    logPath.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value logPath=" + logPath);
                }
            }
            catch (NullReferenceException)
            {
                log.Debug("Registry error use default values; logPath=" + logPath.Value);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            // Get the log name

            try
            {
                if (key.GetValue("logname", "").ToString().Length > 0)
                {
                    logName.Value = (string)key.GetValue("logname", logName);
                    logName.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value logName=" + logName);
                }
            }
            catch (NullReferenceException)
            {
                log.Debug("Registry error use default values; logName=" + logName.Value);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            // Get the path

            try
            {
                if (key.GetValue("path", "").ToString().Length > 0)
                {
                    clamAVPath.Value = (string)key.GetValue("path", clamAVPath);
                    clamAVPath.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Name=" + clamAVPath);
                }
            }
            catch (NullReferenceException)
            {
                log.Debug("Registry error use default values; Name=" + clamAVPath.Value);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            // Get the name

            try
            {
                if (key.GetValue("name", "").ToString().Length > 0)
                {
                    clamAVName.Value = (string)key.GetValue("name", clamAVName);
                    clamAVName.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Path=" + clamAVName);
                }
            }
            catch (NullReferenceException)
            {
                log.Warn("Registry error use default values; Name=" + clamAVName.Value + " Path=" + clamAVPath.Value);
            }
            catch (Exception e)
            {
                log.Error(e.ToString());
            }

            // Check if the config file has been paased in and overwrite the registry

            for (int item = 0; item < args.Length; item++)
            {
                switch (args[item])
                {
                    case "/N":
                    case "--name":
                        clamAVName.Value = args[item + 1];
                        clamAVName.Value = clamAVName.Value.TrimStart('"');
                        clamAVName.Value = clamAVName.Value.TrimEnd('"');
                        clamAVName.Source = Parameter.SourceType.Command;
                        log.Debug("Use command value Name=" + clamAVName);
                        break;
                    case "/P":
                    case "--path":
                        clamAVPath.Value = args[item + 1];
                        clamAVPath.Value = clamAVPath.Value.TrimStart('"');
                        clamAVPath.Value = clamAVPath.Value.TrimEnd('"');
                        clamAVPath.Source = Parameter.SourceType.Command;
                        log.Debug("Use command value Path=" + clamAVPath);
                        break;
                    case "/n":
                    case "--logname":
                        logName.Value = args[item + 1];
                        logName.Value = logName.Value.TrimStart('"');
                        logName.Value = logName.Value.TrimEnd('"');
                        logName.Source = Parameter.SourceType.Command;
                        log.Debug("Use command value logName=" + logName);
                        break;
                    case "/p":
                    case "--logpath":
                        logPath.Value = args[item + 1];
                        logPath.Value = logPath.Value.TrimStart('"');
                        logPath.Value = logPath.Value.TrimEnd('"');
                        logPath.Source = Parameter.SourceType.Command;
                        log.Debug("Use command value logPath=" + logPath);
                        break;
                }
            }

            // Adjust the log location if it has been overridden in the registry
            // This is an interim measure until can add in the naming mask

            try
            {

                log4net.Repository.Hierarchy.Hierarchy hierarchy = (log4net.Repository.Hierarchy.Hierarchy)LogManager.GetRepository();
                log.Debug("Check appenders " + hierarchy.Root.Appenders.Count);
                foreach (log4net.Appender.IAppender appender in hierarchy.Root.Appenders)
                {
                    log.Debug("Check log file appenders " + appender.Name + " " + appender.GetType().Name);
                    // only set the file appenders
                    if (appender.GetType() == typeof(log4net.Appender.RollingFileAppender))
                    {
                        if (logPath.Value.Length > 0)
                        {
                            log4net.Appender.RollingFileAppender rollingFileAppender = (log4net.Appender.RollingFileAppender)appender;
                            // Programmatically set this to the desired location here
                            string logFileLocation = logPath.Value + Path.DirectorySeparatorChar;
                            log.Debug("Set RollingFileAppender logFileLocation=" + logFileLocation);
                            if (!Directory.Exists(logPath.Value))
                            {
                                Directory.CreateDirectory(logPath.Value);
                            }
                            rollingFileAppender.File = logFileLocation;
                            rollingFileAppender.ActivateOptions();
                            log.Debug("Set logPath=" + logFileLocation);
                        }
                    }
                    else if (appender.GetType() == typeof(log4net.Appender.FileAppender))
                    {
                        if ((logPath.Value.Length > 0) && (logName.Value.Length > 0))
                        {
                            log4net.Appender.FileAppender fileAppender = (log4net.Appender.FileAppender)appender;
                            // Programmatically set this to the desired location here
                            string logFileLocation = Path.Combine(logPath.Value, logName.Value);
                            log.Debug("Set FileAppender logFileLocation=" + logFileLocation);
                            if (!Directory.Exists(logPath.Value))
                            {
                                Directory.CreateDirectory(logPath.Value);
                            }
                            fileAppender.File = logFileLocation;
                            fileAppender.ActivateOptions();
                            log.Debug("Set logPath=" + logPath + " logName=" + logName);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.Error("Cannot set log value " + e.ToString());
            }

            log.Info("Use Name=" + clamAVName + " Path=" + clamAVPath);
            log.Info("Use Log Name=" + logName + " Log Path=" + logPath);

            // finally use the xml config file

            Serialise serialise = new Serialise(clamAVName.Value, clamAVPath.Value);
            clamAV = serialise.FromXML();
            if (clamAV != null)
            {
                //Launch the clamAV thread
                clamAV.Monitor();
                clamAV.Start();
            }

            manualResetEvent.WaitOne();

            clamAV.Dispose();

            log.Debug("Out Main()");
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            log.Debug("In ConsoleCtrlCheck:");

            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    {
                        isClosing = true;
                        log.Info("CTRL+C received:");
                        break;
                    }

                case CtrlTypes.CTRL_BREAK_EVENT:
                    {
                        isClosing = true;
                        log.Info("CTRL+BREAK received:");
                        break;
                    }
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    {
                        isClosing = true;
                        log.Info("Program being closed:");
                        break;
                    }
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    {
                        isClosing = true;
                        log.Info("User is logging off:");
                        break;
                    }
            }

            clamAV.Dispose();
            log.Debug("Out ConsoleCtrlCheck:");
            manualResetEvent.Set();
            //Environment.Exit(0);

            return (true);

        }
    }
}
