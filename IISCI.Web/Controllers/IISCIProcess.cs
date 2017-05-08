﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;

namespace IISCI.Web.Controllers
{
    public class IISCIProcess
    {

        private string program;
        private string arguments;

        public string Output { get; private set; }
        public string Error { get; private set; }

        public bool Success { get; private set; }

        public IISCIProcess(string prog, string args)
        {
            program = prog;
            arguments = args;

        }

        public int Run()
        {

            using (StringWriter output = new StringWriter())
            {
                using (StringWriter error = new StringWriter())
                {

                    int n = ProcessHelper.Execute(program, arguments,null,
                        o =>
                        {
                            output.WriteLine(HttpUtility.HtmlEncode(o));
                        },
                                                o =>
                        {
                            error.WriteLine(HttpUtility.HtmlEncode(o));
                        });

                    Output = "<pre>" + output.GetStringBuilder().ToString() + "</pre>";
                    string er = error.GetStringBuilder().ToString().Trim();
                    if (er.Length > 0)
                    {
                        Error = "<pre style='color:red'>" + er + "</pre>";
                        Success = false;
                    }
                    else {

                        Output = "<h3 style='color:green'>Build Successful</h3>" + Output;
                        Success = true;

                        Error = "";
                    }

                    return n;
                }
            }

        }



    }

    public class IISWebRequest {

        public static IISWebRequest Instance = new IISWebRequest();

        private static List<string> InProgress = new List<string>();

        public static IEnumerable<string> InProgressUrls {
            get {
                lock (InProgress) {
                    return InProgress.ToList();
                }
            }
        }

        public void Invoke(string url) {
            lock (InProgress)
            {
                if (InProgress.Contains(url))
                    return;
                InProgress.Add(url);
            }
            ThreadPool.QueueUserWorkItem(a => {
                while (true)
                {
                    using (WebClient client = new WebClient())
                    {
                        var response = client.DownloadString(url);
                        if (response.Contains("retry after sometime")) {
                            Thread.Sleep(1000);
                            continue;
                        }
                        break;
                    }
                }
                lock (InProgress) {
                    InProgress.Remove(url);
                }
            });
        }


        internal void Invoke(string host, string url)
        {
            if (url.StartsWith("//"))
            {
                url = "http:" + url;
            }
            else {
                if (url.StartsWith("/")) {
                    url = "http://" + host + url;
                }
            }
            Invoke(url);
        }
    }
}