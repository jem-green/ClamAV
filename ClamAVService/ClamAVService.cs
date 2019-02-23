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
                // showdown any threads cleanly.
                //heartbeat.Stop();
                //heartbeat.Dispose();

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
            Parameter clamAVName = new Parameter("clamAV.xml");

            clamAVPath.Value = System.Reflection.Assembly.GetExecutingAssembly().Location;
            pos = clamAVPath.Value.LastIndexOf('\\');
            clamAVPath.Value = clamAVPath.Value.Substring(0, pos);

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

            log.Info("Use Name=" + clamAVName + " Path=" + clamAVPath);

            // finally use the xml file.

            // schedule the update

            //FreshClam freshClam = new FreshClam(FreshClam.Location.program);
            //freshClam.WriteConfig();
            //Schedule scanSchedule = new Schedule();
            //scanSchedule.Timeout = 1;    // 1 minute
            //scanSchedule.Units = Schedule.TimeoutUnit.week;
            //scanSchedule.Interval = 60;    // i minute checking interval
            //scanSchedule.StartDate = "10 February 2018";
            //scanSchedule.StartTime = "00:00:00";
            //freshClam.Schedule = scanSchedule;
            //freshClam.Update(new FreshClam.Option("show-progress", "no", FreshClam.Option.ConfigFormat.text));
            //freshClam.Start();

            //// Add scheduled scans

            //List<ClamScan> scans = new List<ClamScan>();

            //// Scan drive c:

            //ClamScan clamScan = new ClamScan(ClamScan.Location.program, @"c:\");
            //clamScan.WriteConfig();
            //scanSchedule = new Schedule();
            //scanSchedule.Timeout = 1;    // 1 minute
            //scanSchedule.Units = Schedule.TimeoutUnit.day;
            //scanSchedule.Interval = 60;    // i minute checking interval
            //scanSchedule.StartDate = "10 February 2018";
            //scanSchedule.StartTime = "00:05:00";
            //clamScan.Schedule = scanSchedule;
            //clamScan.Path = @"c:\";
            //clamScan.Start();
            //scans.Add(clamScan);

            //// Scan drive e:

            //clamScan = new ClamScan(ClamScan.Location.program, @"e:\");
            //clamScan.WriteConfig();
            //scanSchedule = new Schedule();
            //scanSchedule.Timeout = 1;    // 1 minute
            //scanSchedule.Units = Schedule.TimeoutUnit.week;
            //scanSchedule.Interval = 60;    // i minute checking interval
            //scanSchedule.StartDate = "10 February 2018";
            //scanSchedule.StartTime = "01:00:00";
            //clamScan.Schedule = scanSchedule;
            //clamScan.Path = @"e:\";
            //clamScan.Start();
            //scans.Add(clamScan);

            log.Debug("Out ServiceWorkerMethod");        

        }
    }
}
