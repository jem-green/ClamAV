using System;
using System.ServiceProcess;
using log4net;
using ClamAVLibrary;
using System.Threading;
using Microsoft.Win32;
using System.Collections.Generic;

namespace ClamAVService
{
    public partial class ClamAVService : ServiceBase
    {

        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private System.Threading.Thread workerThread = null;
        private ClamAV clamAV;

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
                if (key.GetValue("path", "").ToString().Length > 0)
                {
                    clamAVPath.Value = (string)key.GetValue("path", clamAVPath);
                    clamAVPath.Source = Parameter.SourceType.Registry;
                    log.Debug("Use registry value Name=" + clamAVPath);
                }

                if (key.GetValue("name", "").ToString().Length > 0)
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


            log.Debug("Out ServiceWorkerMethod");  
        }
    }
}
