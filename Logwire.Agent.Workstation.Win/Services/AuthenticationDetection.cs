using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Logwire.Agent.Workstation.Win.Utils;

namespace Logwire.Agent.Workstation.Win.Services
{
    public partial class AuthenticationDetection : ServiceBase
    {

        System.Timers.Timer timer;

        public AuthenticationDetection()
        {
            InitializeComponent();
            CanPauseAndContinue = true;
            CanHandleSessionChangeEvent = true;

            ServiceName = "AuthenticationDetectionService";

        }

        protected override void OnStart(string[] args)
        {

            EventLog logListener = new EventLog("Security");
            logListener.EntryWritten += logListener_EntryWritten;
            logListener.EnableRaisingEvents = true;

            timer = new System.Timers.Timer();
            timer.Interval = 60000; //300000;
            timer.Elapsed += timerElaspedAsync;
            timer.AutoReset = true;
            timer.Start();
        }

        private void timerElaspedAsync(object sender, ElapsedEventArgs e)
        {

            Thread t = new Thread(async () => await FileHandler.UploadEventAsync());
            t.Start();

        }

        void logListener_EntryWritten(object sender, EntryWrittenEventArgs e)
        {

            if (e.Entry.InstanceId == 4625)
            {

                bool logonType7 = false;
                string accountName = null;
                string workstationName = null;
                string reason = null;

                using (StringReader reader = new StringReader(e.Entry.Message))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {

                        if (line.Contains("Logon Type") && line.Contains("7"))
                        {
                            logonType7 = true;
                        }

                        if (line.Contains("Account Name"))
                        {
                            accountName = line.Split(':')[1]?.Trim();
                        }

                        if (line.Contains("Workstation Name"))
                        {
                            workstationName = line.Split(':')[1]?.Trim();
                        }

                    }
                }

                if (logonType7)
                {
                    FileHandler.SaveEvent(string.Format("LOGON_FAIL,{0},{1},{2}", accountName ?? "NULL", workstationName ?? "NULL", e.Entry.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss.fff")));
                }
            }

        }

        protected override void OnStop()
        {
            timer.Stop();
        }

        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {

            string eventLine = "";

            switch (changeDescription.Reason)
            {
                case SessionChangeReason.SessionLogon:
                    eventLine = string.Format("LOGIN,{0},{1},{2},{3}", System.Environment.MachineName, AccountUtils.GetUsername(changeDescription.SessionId), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), NetworkHandler.GetLocalIPAddress());
                    FileHandler.SaveEvent(eventLine);
                    break;

                case SessionChangeReason.SessionLogoff:
                    eventLine = string.Format("LOGOFF,{0},{1},{2}", AccountUtils.GetUsername(changeDescription.SessionId), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), System.Environment.MachineName);
                    FileHandler.SaveEvent(eventLine);
                    break;

                case SessionChangeReason.SessionLock:
                    eventLine = string.Format("LOCK,{0},{1},{2}", AccountUtils.GetUsername(changeDescription.SessionId), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), System.Environment.MachineName);
                    FileHandler.SaveEvent(eventLine);
                    break;
                case SessionChangeReason.SessionUnlock:
                    eventLine = string.Format("UNLOCK,{0},{1},{2}", AccountUtils.GetUsername(changeDescription.SessionId), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), System.Environment.MachineName);
                    FileHandler.SaveEvent(eventLine);
                    break;
            }
        }
    }
}
