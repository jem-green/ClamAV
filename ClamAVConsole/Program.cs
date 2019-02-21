using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ClamAVLibrary;
using log4net;

namespace ClamAVConsole
{
    class Program
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static void Main(string[] args)
        {
            log.Info("Enter Main");

            FreshClam freshClam = new FreshClam(FreshClam.Location.program);
            freshClam.WriteConfig();
            freshClam.Timeout = 1;    // 1 minute
            freshClam.Units = FreshClam.TimeoutUnit.week;
            freshClam.Interval = 60;    // i minute checking interval
            freshClam.StartDate = "10 February 2018";
            freshClam.StartTime = "00:00:00";
            freshClam.Update(new FreshClam.Option("show-progress", "no", FreshClam.Option.ConfigFormat.text));
            freshClam.Start();

            List<ClamScan> scans = new List<ClamScan>();

            ClamScan clamScan = new ClamScan(ClamScan.Location.program, @"c:\");
            clamScan.WriteConfig();

            Schedule scanSchedule = new Schedule();
            scanSchedule.Timeout = 1;    // 1 minute
            scanSchedule.Units = Schedule.TimeoutUnit.day;
            scanSchedule.Interval = 60;    // i minute checking interval
            scanSchedule.StartDate = "10 February 2018";
            scanSchedule.StartTime = "00:05:00";

            clamScan.Schedule = scanSchedule;
            clamScan.Path = @"c:\";
            //clamScan.Update(new ClamScan.Option("infected","", FreshClam.Option.ConfigFormat.key));
            clamScan.Start();
            scans.Add(clamScan);

            clamScan = new ClamScan(ClamScan.Location.program, @"e:\");
            clamScan.WriteConfig();

            scanSchedule = new Schedule();
            scanSchedule.Timeout = 1;    // 1 minute
            scanSchedule.Units = Schedule.TimeoutUnit.week;
            scanSchedule.Interval = 60;    // i minute checking interval
            scanSchedule.StartDate = "10 February 2018";
            scanSchedule.StartTime = "01:00:00";

            clamScan.Schedule = scanSchedule;
            clamScan.Path = @"e:\";
            clamScan.Start();
            scans.Add(clamScan);

            //Clamd clamd = new Clamd(Clamd.Location.Program);
            //clamd.IsBackground = false;
            //clamd.WriteConfig();
            //clamd.Timeout = 1;    // 1 minute
            //clamd.Units = Clamd.UnitType.minute;
            //clamd.Interval = 60;   // i minute checking interval
            //clamd.Start();

            //ClamdScan clamdScan = new ClamdScan(ClamdScan.Location.Program, @"c:\");
            //clamdScan.WriteConfig();
            //clamdScan.Timeout = 60;    // 1 minute
            //clamdScan.Interval = 60;    // i minute
            //clamdScan.StartDate = "19 December 2018";
            //clamdScan.StartTime = "00:19:00";
            //clamdScan.Path = @"c:\";
            //clamdScan.Start();

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
            log.Info("Exit Main");
        }
    }
}
