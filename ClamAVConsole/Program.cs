using System;
using Microsoft.Win32;
using log4net;
using System.Threading;
using ClamAVLibrary;
using System.Runtime.InteropServices;

namespace ClamAVConsole
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static bool isclosing = false;
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
            log.Debug("Enter Main");

            ctrlCHandler = new HandlerRoutine(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(ctrlCHandler, true);

            int pos = 0;
            Parameter clamAVPath = new Parameter("");
            Parameter clamAVName = new Parameter("clamav.xml");

            clamAVPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = clamAVPath.Value.LastIndexOf('\\');
            clamAVPath.Value = clamAVPath.Value.Substring(0, pos);
            clamAVPath.Source = Parameter.SourceType.App;

            try
            {
                RegistryKey key = Registry.LocalMachine;
                key = key.OpenSubKey("software\\green\\clamav\\");
                if (key.GetValue("path", "").ToString() != "")
                {
                    clamAVPath.Value = (string)key.GetValue("path", clamAVPath);
                    clamAVPath.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Name=" + clamAVPath);
                }

                if (key.GetValue("name", "").ToString() != "")
                {
                    clamAVName.Value = (string)key.GetValue("name", clamAVName);
                    clamAVName.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Path=" + clamAVName);
                }
            }
            catch (NullReferenceException)
            {
                log.Error("Registry error use default values; Name=" + clamAVName.Value + " Path=" + clamAVPath.Value);
            }
            catch (Exception e)
            {
                log.Debug(e.ToString());
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
                }
            }

            log.Info("Use Name=" + clamAVName + " Path=" + clamAVPath);

            // finally use the xml file.

            Serialise serialise = new Serialise(clamAVName.Value, clamAVPath.Value);
            clamAV = serialise.FromXML();
            if (clamAV != null)
            {
                //Launch the clamAV thread
                clamAV.Monitor();
                clamAV.Start();
            }

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
            log.Debug("Exit Main()");
        }

        private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            log.Debug("Enter ConsoleCtrlCheck()");

            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                    isclosing = true;
                    log.Info("CTRL+C received:");
                    break;

                case CtrlTypes.CTRL_BREAK_EVENT:
                    isclosing = true;
                    log.Info("CTRL+BREAK received:");
                    break;

                case CtrlTypes.CTRL_CLOSE_EVENT:
                    isclosing = true;
                    log.Info("Program being closed:");
                    break;

                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    isclosing = true;
                    log.Info("User is logging off:");
                    break;

            }
            clamAV.Dispose();
            log.Debug("Exit ConsoleCtrlCheck()");

            Environment.Exit(0);

            return (true);

        }
    }
}
