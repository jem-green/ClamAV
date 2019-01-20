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

            //FreshClam freshClam = new FreshClam(FreshClam.Location.Program);
            //freshClam.WriteConfig();
            //freshClam.Timeout = 60;    // 1 minute
            //freshClam.Interval = 60;    // i minute
            //freshClam.StartDate = "19 December 2018";
            //freshClam.StartTime = "00:19:00";
            //freshClam.Start();

            ClamdScan clamdScan = new ClamdScan(ClamdScan.Location.Program, @"c:\");
            clamdScan.WriteConfig();
            clamdScan.Timeout = 60;    // 1 minute
            clamdScan.Interval = 60;    // i minute
            clamdScan.StartDate = "19 December 2018";
            clamdScan.StartTime = "00:19:00";
            clamdScan.Path = @"c:\";
            clamdScan.Start();

            ManualResetEvent manualResetEvent = new ManualResetEvent(false);
            manualResetEvent.WaitOne();
            log.Info("Exit Main");
        }
    }
}
