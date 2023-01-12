using ClamAVLibrary;
using TracerLibrary;
using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace ClamAVConsole
{
    class Program
    {
		#region Fields
	
        private static ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        public static bool isClosing = false;
        static private HandlerRoutine ctrlCHandler;
        static private ClamAV clamAV;

		#endregion
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
        #region Methods
        static void Main(string[] args)
        {
            Debug.WriteLine("In Main()");

            ctrlCHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(ctrlCHandler, true);

            int pos = 0;
            Parameter<string> appPath = new Parameter<string>("");
            Parameter<string> appName = new Parameter<string>("clamav.xml");

            appPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;

            pos = appPath.Value.ToString().LastIndexOf(Path.DirectorySeparatorChar);
            if (pos > 0)
            {
                appPath.Value = appPath.Value.ToString().Substring(0, pos);
                appPath.Source = Parameter<string>.SourceType.App;
            }

            Parameter<string> logPath = new Parameter<string>("");
            Parameter<string> logName = new Parameter<string>("clamavconsole");
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
            FileStreamWithRolling dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(0, 1, 0, 0), FileMode.Append);
            TextWriterTraceListenerWithTime listener = new TextWriterTraceListenerWithTime(dailyRolling);
            Trace.AutoFlush = true;
            TraceFilter fileTraceFilter = new System.Diagnostics.EventTypeFilter(SourceLevels.Verbose);
            listener.Filter = fileTraceFilter;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(listener);

            ConsoleTraceListener console = new ConsoleTraceListener();
            TraceFilter consoleTraceFilter = new System.Diagnostics.EventTypeFilter(SourceLevels.Information);
            console.Filter = consoleTraceFilter;
			Trace.Listeners.Add(console);

            // Check if the registry has been set and overwrite the application defaults

            RegistryKey key = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, RegistryView.Registry64);
            string keys = "software\\green\\clamav\\";
            foreach(string subkey in keys.Split('\\'))
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

            // Get the name
			
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
                Trace.TraceError(e.ToString());
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
                Trace.TraceError(e.ToString());
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
                Trace.TraceWarning("Registry error use default values; Debug=" + traceLevels.Value);
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }

            // Check if the config file has been paased in and overwrite the registry

            int items = args.Length;
            for (int item = 0; item <items; item++)
            {
                switch (args[item])
                {
                    case "/D":
                    case "--debug":
                        traceLevels.Value = args[item + 1];
                        traceLevels.Value = traceLevels.Value.ToString().TrimStart('"');
                        traceLevels.Value = traceLevels.Value.ToString().TrimEnd('"');
                        traceLevels.Source = Parameter<string>.SourceType.Command;
                        TraceInternal.TraceVerbose("Use command value Name=" + traceLevels);
                        break;
                    case "/N":
                    case "--name":
                        appName.Value = args[item + 1];
                        appName.Value = appName.Value.ToString().TrimStart('"');
                        appName.Value = appName.Value.ToString().TrimEnd('"');
                        appName.Source = Parameter<string>.SourceType.Command;
                        TraceInternal.TraceVerbose("Use command value Name=" + appName);
                        break;
                    case "/P":
                    case "--path":
                        appPath.Value = args[item + 1];
                        appPath.Value = appPath.Value.ToString().TrimStart('"');
                        appPath.Value = appPath.Value.ToString().TrimEnd('"');
                        appPath.Source = Parameter<string>.SourceType.Command;
                        TraceInternal.TraceVerbose("Use command value Path=" + appPath);
                        break;
                    case "/n":
                    case "--logname":
                        logName.Value = args[item + 1];
                        logName.Value = logName.Value.ToString().TrimStart('"');
                        logName.Value = logName.Value.ToString().TrimEnd('"');
                        logName.Source = Parameter<string>.SourceType.Command;
                        TraceInternal.TraceVerbose("Use command value logName=" + logName);
                        break;
                    case "/p":
                    case "--logpath":
                        logPath.Value = args[item + 1];
                        logPath.Value = logPath.Value.ToString().TrimStart('"');
                        logPath.Value = logPath.Value.ToString().TrimEnd('"');
                        logPath.Source = Parameter<string>.SourceType.Command;
                        TraceInternal.TraceVerbose("Use command value logPath=" + logPath);
                        break;
                }
            }

            // Redirect the output

            listener.Flush();
            Trace.Listeners.Remove(listener);
            listener.Close();
            listener.Dispose();

            // Adjust the log location if it has been overridden in the registry

            logFilenamePath = logPath.Value.ToString() + Path.DirectorySeparatorChar + logName.Value.ToString() + ".log";
            dailyRolling = new FileStreamWithRolling(logFilenamePath, new TimeSpan(1, 0, 0, 0), FileMode.Append);
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

            // finally use the xml file.

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
                clamAV.Monitor();

                // Start the clamAV thread
                clamAV.Start();
            }

            manualResetEvent.WaitOne();

            clamAV.Dispose();

            Debug.WriteLine("Out Main()");
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            Debug.WriteLine("In ConsoleCtrlCheck()");

            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    {
                        isClosing = true;
                    TraceInternal.TraceInformation("CTRL+C received");
                        break;
                    }

                case CtrlTypes.CTRL_BREAK_EVENT:
                    {
                        isClosing = true;
                    TraceInternal.TraceInformation("CTRL+BREAK received");
                        break;
                    }
                case CtrlTypes.CTRL_CLOSE_EVENT:
                    {
                        isClosing = true;
                    TraceInternal.TraceInformation("Program being closed");
                        break;
                    }
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    {
                        isClosing = true;
                    TraceInternal.TraceInformation("User is logging off");
                        break;
                    }
            }

            clamAV.Dispose();
            Debug.WriteLine("Out ConsoleCtrlCheck()");

            manualResetEvent.Set();

            return (true);

        }
        #endregion
    }
}
