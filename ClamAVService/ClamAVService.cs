using System;
using System.ServiceProcess;
using log4net;
using ClamAVLibrary;
using System.Threading;
using Microsoft.Win32;

namespace ClamAVService
{
    public partial class ClamAVService : ServiceBase
    {

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private System.Threading.Thread workerThread = null;
        private ClamAV heartbeat;

        public ClamAVService()
        {
            InitializeComponent();
            eventLog.Source = "ClamAV";
            log.Debug("In ClamAVService.");
        }

        protected override void OnStart(string[] args)
        {

            eventLog.WriteEntry("In OnStart.");
            log.Debug("In OnStart.");

            if ((workerThread == null) ||
                ((workerThread.ThreadState &
                 (System.Threading.ThreadState.Unstarted | System.Threading.ThreadState.Stopped)) != 0))
            {
                workerThread = new Thread(new ThreadStart(ServiceWorkerMethod));
                workerThread.Start();
            }

            log.Debug("Out OnStart");
            eventLog.WriteEntry("Out OnStart.");
        }

        protected override void OnStop()
        {
            eventLog.WriteEntry("In OnStop.");
            log.Debug("In OnStop.");
            if (workerThread != null)
            {
                // showdown any threads cleanly.
                heartbeat.Stop();
                heartbeat.Dispose();

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
            log.Debug("Out OnStop.");
            eventLog.WriteEntry("Out OnStop.");
        }

        void ServiceWorkerMethod()
        {
            log.Debug("In ServiceWorkerMethod.");

            int pos = 0;
            Parameter heartbeatPath = new Parameter("");
            Parameter heartbeatName = new Parameter("heartbeat.xml");

            heartbeatPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = heartbeatPath.Value.LastIndexOf('\\');
            heartbeatPath.Value = heartbeatPath.Value.Substring(0, pos);

            try
            {
                RegistryKey key = Registry.LocalMachine;
                key = key.OpenSubKey("software\\green\\heartbeat\\");
                if (key.GetValue("path", "").ToString() != "")
                {
                    heartbeatPath.Value = (string)key.GetValue("path", heartbeatPath);
                    heartbeatPath.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Name=" + heartbeatPath);
                }

                if (key.GetValue("name", "").ToString() != "")
                {
                    heartbeatName.Value = (string)key.GetValue("name", heartbeatName);
                    heartbeatName.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Path=" + heartbeatName);
                }
            }
            catch (NullReferenceException)
            {
                log.Error("Registry error use default values; Name=" + heartbeatName.Value + " Path=" + heartbeatPath.Value);
            }
            catch (Exception e)
            {
                log.Debug(e.ToString());
            }

            log.Info("Use Name=" + heartbeatName + " Path=" + heartbeatPath);

            // finally use the xml file.

            heartbeat = new ClamAV();
            heartbeat.FromXML(heartbeatName.Value, heartbeatPath.Value);

            // Launch the watcher thread

            heartbeat.Start();

            log.Debug("Out ServiceWorkerMethod");        

        }
    }
}
