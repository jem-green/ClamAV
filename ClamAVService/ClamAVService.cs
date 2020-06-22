using ClamAVLibrary;
using log4net;
using Microsoft.Win32;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace ClamAVService
{
    public partial class ClamAVService : ServiceBase
    {

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private System.Threading.Thread workerThread = null;
        private ClamAV clamAV;

        public ClamAVService()
        {
            log.Debug("In ClamAVService()");
            InitializeComponent();
            eventLog.Source = "ClamAV";
            log.Debug("Out ClamAVService()");
        }

        protected override void OnStart(string[] args)
        {

            eventLog.WriteEntry("In OnStart.");
            log.Debug("In OnStart()");

            if ((workerThread == null) ||
                ((workerThread.ThreadState &
                 (System.Threading.ThreadState.Unstarted | System.Threading.ThreadState.Stopped)) != 0))
            {
                workerThread = new Thread(new ThreadStart(ServiceWorkerMethod));
                workerThread.Start();
            }

            log.Debug("Out OnStart()");
            eventLog.WriteEntry("Out OnStart.");
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("In OnStop.");
            log.Debug("In OnStop()");
            if (workerThread != null)
            {
                try
                {
                    workerThread.Abort();
                }
                catch { }
                try
                {
                    workerThread.Join();
                }
                catch { }
            }
            log.Debug("Out OnStop()");
            eventLog.WriteEntry("Out OnStop.");
        }

        void ServiceWorkerMethod()
        {
            log.Debug("In ServiceWorkerMethod()");

            int pos = 0;
            Parameter clamAVPath = new Parameter("");
            Parameter clamAVName = new Parameter("clamav.xml");

            clamAVPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = clamAVPath.Value.LastIndexOf('\\');
            clamAVPath.Value = clamAVPath.Value.Substring(0, pos);
            clamAVPath.Source = Parameter.SourceType.App;

            Parameter logPath = new Parameter("");
            Parameter logName = new Parameter("clamavservice.log");
            logPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = logPath.Value.LastIndexOf('\\');
            logPath.Value = logPath.Value.Substring(0, pos);
            logPath.Source = Parameter.SourceType.App;

            RegistryKey key = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
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

            // finally use the xml file.

            Serialise serialise = new Serialise(clamAVName.Value, clamAVPath.Value);
            clamAV = serialise.FromXML();
            if (clamAV != null)
            {
                //Launch the clamAV thread
                clamAV.Location = Component.DataLocation.Program;
                clamAV.Monitor();
                clamAV.Start();
            }

            log.Debug("Out ServiceWorkerMethod()");
        }
    }
}
