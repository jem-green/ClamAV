using ClamAVLibrary;
using TracerLibrary;
using Microsoft.Win32;
using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;
using System.Diagnostics;

namespace ClamAVService
{
    public partial class ClamAVService : ServiceBase
    {
        private System.Threading.Thread workerThread = null;
        private ClamAV clamAV;

        public ClamAVService()
        {
            Debug.WriteLine("In ClamAVService()");
            InitializeComponent();
            eventLog.Source = "ClamAV";
            Debug.WriteLine("Out ClamAVService()");
        }

        protected override void OnStart(string[] args)
        {
            eventLog.WriteEntry("In OnStart.");
            Debug.WriteLine("In OnStart()");

            if ((workerThread == null) ||
                ((workerThread.ThreadState &
                 (System.Threading.ThreadState.Unstarted | System.Threading.ThreadState.Stopped)) != 0))
            {
                workerThread = new Thread(new ThreadStart(ServiceWorkerMethod));
                workerThread.Start();
            }

            Debug.WriteLine("Out OnStart()");
            eventLog.WriteEntry("Out OnStart.");
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("In OnStop.");
            Debug.WriteLine("In OnStop()");
            if (workerThread != null)
            {
                // showdown any threads cleanly.
                if (clamAV != null)
                {
                    clamAV.Dispose();
                }

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
            Debug.WriteLine("Out OnStop()");
            eventLog.WriteEntry("Out OnStop.");
        }

        void ServiceWorkerMethod()
        {
            Debug.WriteLine("In ServiceWorkerMethod()");

            int pos = 0;
            Parameter<string> appPath = new Parameter<string>("");
            Parameter<string> appName = new Parameter<string>("clamav.cfg");

            appPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = appPath.Value.ToString().LastIndexOf(Path.DirectorySeparatorChar);
            if (pos > 0)
            {
                appPath.Value = appPath.Value.ToString().Substring(0, pos);
                appPath.Source = Parameter<string>.SourceType.App;
            }

            Parameter<string> logPath = new Parameter<string>("");
            Parameter<string> logName = new Parameter<string>("clamavservice");
            logPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = logPath.Value.ToString().LastIndexOf(Path.DirectorySeparatorChar);
            if (pos > 0)
            {
                logPath.Value = logPath.Value.ToString().Substring(0, pos);
                logPath.Source = Parameter<string>.SourceType.App;
            }

            Parameter<string> traceLevels = new Parameter<string>("");
            traceLevels.Value = "verbose";
            traceLevels.Source = Parameter<string>.SourceType.App;

            // Configure tracer options

            string logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            FileStreamWithRolling dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
            TextWriterTraceListenerWithTime listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            TraceFilter fileTraceFilter = new System.Diagnostics.EventTypeFilter(SourceLevels.Verbose);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);

            // 

            RegistryKey key = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            string keys = "software\\green\\clamav\\";
            foreach (string subkey in keys.Split('\\'))
            {
                key = key.OpenSubKey(subkey);
                if (key == null)
                {
                    TraceInternal.TraceError("Failed to open" + subkey);
                    break;
                }
            }
            // Get the log path

            try
            {
                if (key.GetValue("logpath", "").ToString().Length > 0)
                {
                    logPath.Value = (string)key.GetValue("logpath", logPath);
                    logPath.Source = Parameter<string>.SourceType.Registry;
                    TraceInternal.TraceVerbose("Use registry value; logPath=" + logPath);
                }
            }
            catch (NullReferenceException)
            {
                TraceInternal.TraceVerbose("Registry error use default values; logPath=" + logPath.Value);
            }
            catch (Exception e)
            {
                TraceInternal.TraceError(e.ToString());
            }

            // Get the log name

            try
            {
                if (key.GetValue("logname", "").ToString().Length > 0)
                {
                    logName.Value = (string)key.GetValue("logname", logName);
                    logName.Source = Parameter<string>.SourceType.Registry;
                    TraceInternal.TraceVerbose("Use registry value; LogName=" + logName);
                }
            }
            catch (NullReferenceException)
            {
                TraceInternal.TraceVerbose("Registry error use default values; LogName=" + logName.Value);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }

            // Get the Name
			
            try
            {
                if (key.GetValue("name", "").ToString().Length > 0)
                {
                    appName.Value = (string)key.GetValue("name", appName);
                    appName.Source = Parameter<string>.SourceType.Registry;
                    TraceInternal.TraceVerbose("Use registry value; Name=" + appName);
                }
            }
            catch (NullReferenceException)
            {
                TraceInternal.TraceVerbose("Registry error use default values; Name=" + appName.Value);
            }
            catch (Exception e)
            {
                TraceInternal.TraceError(e.ToString());
            }

            // Get the path

            try
            {
                if (key.GetValue("path", "").ToString().Length > 0)
                {
                    appPath.Value = (string)key.GetValue("path", appPath);
                    appPath.Source = Parameter<string>.SourceType.Registry;
                    TraceInternal.TraceVerbose("Use registry value; Path=" + appPath);
                }
            }
            catch (NullReferenceException)
            {
                TraceInternal.TraceVerbose("Registry error use default values; Path=" + appPath.Value);
            }
            catch (Exception e)
            {
                TraceInternal.TraceError(e.ToString());
            }

            // Get the traceLevels

            try
            {
                if (key.GetValue("debug", "").ToString().Length > 0)
                {
                    traceLevels.Value = (string)key.GetValue("debug", "verbose");
                    traceLevels.Source = Parameter<string>.SourceType.Registry;
                    TraceInternal.TraceVerbose("Use registry value; Debug=" + traceLevels.Value);
                }
            }
            catch (NullReferenceException)
            {
                TraceInternal.TraceWarning("Registry error use default values; Name=" + appName.Value + " Path=" + appPath.Value);
            }
            catch (Exception e)
            {
                TraceInternal.TraceError(e.ToString());
            }

            // Redirect the output

            listener.Flush();
            Trace.Listeners.Remove(listener);
            listener.Close();
            listener.Dispose();

            // Adjust the log location if it has been overridden in the registry

            logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(0, 1, 0, 0), FileMode.Append);
            listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            SourceLevels sourceLevels = TraceInternal.TraceLookup(traceLevels.Value.ToString());
            fileTraceFilter = new System.Diagnostics.EventTypeFilter(sourceLevels);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Add(listener);
        
            TraceInternal.TraceInformation("Use Name=" + appName.Value);
            TraceInternal.TraceInformation("Use Path=" + appPath.Value);
            TraceInternal.TraceInformation("Use Log Name=" + logName.Value);
            TraceInternal.TraceInformation("Use Log Path=" + logPath.Value);
            
            // finally use the XML file.

            Serialise serialise = new Serialise();
            if (appPath.Value.ToString().Length > 0)
            {
                serialise.Path = appPath.Value.ToString();
            }

            if (appName.Value.ToString().Length > 0)
            {
                serialise.Filename = appName.Value.ToString();
            }
            clamAV = serialise.FromXML();
            if (clamAV != null)
            {
                //Launch the clamAV thread
				
                clamAV.Location = Component.DataLocation.Program;
                clamAV.Monitor();

                // Start the clamAV thread

                clamAV.Start();
            }

            Debug.WriteLine("Out ServiceWorkerMethod()");
        }
    }
}
