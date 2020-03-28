using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Logwire.Agent.Workstation.Win.Models;
using Newtonsoft.Json;
using static System.Environment;

namespace Logwire.Agent.Workstation.Win.Utils
{

    class FileHandler
    {
        private static FileHandler _instance;

        private Semaphore semaphoreObject = new Semaphore(initialCount: 1, maximumCount: 1, name: "FileHandler");

        public static FileHandler Instance
        {
            get
            {

                if (_instance == null)
                    _instance = new FileHandler();

                return _instance;

            }
        }


        private static string FileLocation = Path.Combine(GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Logging Agent\\Events.txt");

        internal static void SaveEvent(string line)
        {

            Directory.CreateDirectory(Path.GetDirectoryName(FileLocation));

            Thread t = new Thread(() =>
            {

                Instance.semaphoreObject.WaitOne();

                StreamWriter file = new System.IO.StreamWriter(FileLocation, true);

                try
                {
                    file.WriteLine(line);
                    file.Flush();
                }
                finally
                {
                    file.Close();
                    file.Dispose();
                }

                Instance.semaphoreObject.Release();

            });

            t.Start();

        }

        internal static async System.Threading.Tasks.Task UploadEventAsync()
        {
            bool connected = true;

            using (HttpClient client = new HttpClient())
            {

                try
                {

                    System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);

                    var response = await client.PostAsync("https://logwire/api/agent/workstation/user/event/authentication/", new StringContent("", Encoding.UTF8, "application/json"));

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        connected = false;
                    }

                }
                catch (Exception e)
                {
                    connected = false;

                    var appLog = new EventLog("Application");
                    appLog.Source = "LoggingAgent";
                    appLog.WriteEntry(e.Message + "\n" + e.StackTrace);
                }

            }

            if (connected)
            {

                List<string> FailedLines = new List<string>();
                string[] lines = null;

                Instance.semaphoreObject.WaitOne();

                if (File.Exists(FileLocation))
                {
                    lines = File.ReadAllLines(FileLocation);

                    ClearFile();
                }

                Instance.semaphoreObject.Release();

                if (lines != null)
                {
                    using (HttpClient client = new HttpClient())
                    {

                        System.Net.ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true);

                        foreach (string line in lines)
                        {
                            try
                            {

                                string text = GetJSONString(line);
                                var response = await client.PostAsync("https://logstash:31311/", new StringContent(text, Encoding.UTF8, "application/json"));

                                if (response.StatusCode != HttpStatusCode.OK)
                                {
                                    FailedLines.Add(line);
                                }

                            }
                            catch (Exception e)
                            {
                                FailedLines.Add(line);

                                var appLog = new EventLog("Application");
                                appLog.Source = "LoggingAgent";
                                appLog.WriteEntry(e.Message + "\n" + e.StackTrace);
                            }
                        }
                    }
                }

                foreach (string line in FailedLines)
                {
                    SaveEvent(line);
                }

            }

        }

        private static string GetJSONString(string line)
        {

            string[] split = line.Split(',');

            switch (split[0])
            {
                case "LOGIN": return JsonConvert.SerializeObject(new LoginModel { machine_name = split[1], username = split[2], time = split[3], ip = split[4] });
                case "LOGON_FAIL": return JsonConvert.SerializeObject(new LoginFailModel { username = split[1], machine_name = split[2], time = split[3] });
                case "LOGOFF": return JsonConvert.SerializeObject(new LogoffModel { username = split[1], time = split[2], machine_name = split[3] });
                case "LOCK": return JsonConvert.SerializeObject(new LockModel { username = split[1], time = split[2], machine_name = split[3] });
                case "UNLOCK": return JsonConvert.SerializeObject(new UnlockModel { username = split[1], time = split[2], machine_name = split[3] });
            }

            return "";

        }

        private static void ClearFile()
        {
            File.Delete(FileLocation);
        }

    }
}
