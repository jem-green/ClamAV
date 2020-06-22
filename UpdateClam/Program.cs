using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UpdateClam
{
    class Program
    {
        public event System.Net.DownloadProgressChangedEventHandler DownloadProgressChanged;
        static string _unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        static string _appdir = "c:\\program files\\clamav";
        static string _tempdir = "c:\\program files\\clamav";
        static bool _readInput = false;
        static bool _update = false;

        static int Main(string[] args)
        {
            int errorCode = 0;

            if (ValidateArguments(args))
            {
                PreProcess(args);
                if (_readInput)
                {
                    string currentLine = Console.In.ReadLine();
                    while (currentLine != null)
                    {
                        //ProcessLine(currentLine);
                        currentLine = Console.In.ReadLine();
                    }
                }
                errorCode = PostProcess();
            }
            else
            {
                Console.Error.Write(Usage());
                errorCode = -1;
            }
            return (errorCode);
        }

        static bool ValidateArguments(string[] args)
        {
            // Passed args allow for changes to web address and application location

            bool help = false;

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-h":
                    case "--help":
                        {
                            help = true;
                            break;
                        }
                }
            }
            
            if (help == false)
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }

        static string Usage()
        {
            string usage = "";
            usage =  "                       Clam AntiVirus: Application Updater 0.1.0\n";
            usage += "           By The Green Team: https://www.32high.co.uk\n";
            usage += "           (C) 2020 32High\n";
            usage += "\n";
            usage += "    clamupdate [options]\n";
            usage += "\n";
            usage += "    --force                -f         Force the update\n";
            usage += "    --help                 -h         Show this help\n";
            usage += "    --appdir=DIRECTORY                Install new application into DIRECTORY";
            usage += "    --tempdir=DIRECTORY               Download installer into DIRECTORY";
            usage += "\n";
            usage += "\n";
            return (usage);
        }

        static void PreProcess(string[] args)
        {
            // Assume that updater is located in the same location as clamd, freshclam etc.
            _appdir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            int pos = _appdir.LastIndexOf('\\');
            _appdir = _appdir.Substring(0, pos);
            _tempdir = _appdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + "ClamAV";

            for (int item = 0; item < args.Length; item++)
            {
                string argument = args[item];
                if (argument.Length > 9)
                {
                    if (argument.Substring(0, 9) == "--appdir=")
                    {
                        _appdir = argument.Substring(9, argument.Length - 9);
                        _appdir = _appdir.TrimStart('"');
                        _appdir = _appdir.TrimEnd('"');
                    }
                }
                else if (argument.Length > 10)
                {
                    if (argument.Substring(0, 10) == "--tempdir=")
                    {
                        _tempdir = argument.Substring(9, argument.Length - 10);
                        _tempdir = _tempdir.TrimStart('"');
                        _tempdir = _tempdir.TrimEnd('"');
                    }
                }
                else if (argument.Length > 6)
                {
                    if (argument.Substring(0, 7) == "--force")
                    {
                        _update = true;
                    }
                }
            }
            _readInput = false; // Indcate that we dont need to do a read input
        }

        static int PostProcess()
        {
            int errorCode = -1;

            string _filename = "clamd.exe";
            string host = "https://www.clamav.net";
            string path = "/downloads";
            string search = "The latest stable release is\n            <strong>";
            string query = "";
            string data = "";
            string uri;

            // Retrieve the page content and search for the latest version

            HttpWebRequest request;
            uri = ParseUri(host, path, query);
            request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Get;

            try
            {
                // Read the response

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                // Decode the response

                Stream responseStream = response.GetResponseStream();
                StreamReader streamReader = new StreamReader(responseStream);
                char[] responseContent = new char[2048];
                int size = 0;
                while (streamReader.EndOfStream == false)
                {
                    size = streamReader.Read(responseContent, 0, responseContent.Length);
                    data += new string(responseContent).Substring(0, size);
                }
                data = data.TrimEnd('\0');

                // Search for the version

                int pos = data.IndexOf(search);
                int next = data.IndexOf(" </strong>", pos);
                search = data.Substring(pos + search.Length, next - pos - search.Length);

                // Need to decide if an update is needed so need to have the current build
                // so check the version of _filename (clamd)

                string fileNamePath = Path.Combine(_appdir, _filename);
                string fileVersion = "";
                try
                {
                    if (File.Exists(fileNamePath) == true)
                    {
                        FileVersionInfo currentVersion = FileVersionInfo.GetVersionInfo(fileNamePath);
                        fileVersion = currentVersion.FileVersion.Trim();
                    }

                    if ((fileVersion != search) || (_update == true))
                    {
                        Console.Error.WriteLine("Clamav(" + fileVersion + ") update available " + search);
                        Console.WriteLine("Application may prevent updates so issue PAUSE");

                        // Download

                        string installer = "ClamAV-" + search + ".exe";
                        path = "/downloads/production/" + installer;
                        uri = ParseUri(host, path, query);

                        try
                        {
                            if (!Directory.Exists(_tempdir))
                            {
                                Directory.CreateDirectory(_tempdir);
                            }

                            fileNamePath = Path.Combine(_tempdir, installer);
                            if (File.Exists(fileNamePath) == true)
                            {
                                File.Delete(fileNamePath);
                            }

                            try
                            {

                                // Download the file - see if we can show progresss.

                                using (WebClient client = new WebClient())
                                {
                                    Console.Error.WriteLine("Download " + uri);
                                    Console.Error.WriteLine("Temporary location " + fileNamePath);
                                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                                    client.DownloadFile(uri, fileNamePath);
                                }

                                try
                                {
                                    // now need to install the downloaded exe

                                    Process proc;

                                    // Enable process to be killed if still running from stop

                                    proc = new System.Diagnostics.Process();

                                    ProcessStartInfo startInfo = new ProcessStartInfo();

                                    startInfo.FileName = fileNamePath;
                                    startInfo.Arguments = "/SUPRESSMESSAGEBOX /VERYSILENT";
                                    startInfo.CreateNoWindow = true;
                                    startInfo.UseShellExecute = true;

                                    // Enable exit event to be raised

                                    proc.EnableRaisingEvents = true;
                                    proc.StartInfo = startInfo;
                                    try
                                    {
                                        Console.Error.WriteLine("Install " + fileNamePath);
                                        proc.Start();
                                        Console.WriteLine(fileNamePath + " " + startInfo.Arguments);

                                        proc.WaitForExit();
                                        Console.Error.WriteLine("Finished installing");
                                        Console.WriteLine("Update complete so issue RESUME");
                                        errorCode = 0;

                                        if (File.Exists(fileNamePath) == true)
                                        {
                                            try
                                            {
                                                Console.Error.WriteLine("Cleanup and delete " + fileNamePath);
                                                File.Delete(fileNamePath);
                                            }
                                            catch (Exception)
                                            {
                                                Console.Error.WriteLine("Could not delete " + fileNamePath);
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        errorCode = 7;
                                        Console.Error.WriteLine("Exception " + e.Message);
                                    }

                                    proc.Dispose();
                                    proc = null;
                                }
                                catch (Exception pe)
                                {
                                    errorCode = 6;
                                    Console.Error.WriteLine("Exception " + pe.Message);
                                }

                            }
                            catch (WebException we)
                            {
                                errorCode = 4;
                                Console.Error.WriteLine("Could not download package from " + uri);
                            }
                            catch (Exception ce)
                            {
                                errorCode = 5;
                                Console.Error.WriteLine("Exception " + ce.Message);
                            }
                        }
                        catch (Exception fe)
                        {
                            errorCode = 6;
                            Console.Error.WriteLine("Could not delete " + fileNamePath);
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Clamav(" + fileVersion + ") is current");
                        errorCode = 0;
                    }
                }
                catch (FileNotFoundException e)
                {
                    errorCode = 2;
                    Console.Error.WriteLine("File not found " + fileNamePath);
                }
                catch (Exception ex)
                {
                    errorCode = 3;
                    Console.Error.WriteLine("Exception " + ex.Message);
                }
            }
            catch (WebException we)
            {
                errorCode = 1;
                Console.Error.WriteLine("Could not connect to " + uri);
            }

            return (errorCode);
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            // In case you don't have a progressBar Log the value instead 
            Console.WriteLine(e.ProgressPercentage);
        }

        static string ParseUri(string host, string path, string query)
        {
            string uri = "";

            if (path.Length == 0)
            {
                uri = Uri.EscapeUriString(host);
            }
            else
            {
                uri = Uri.EscapeUriString(host + path);
            }

            if (query.Length > 0)
            {
                uri += "?";
                uri += ParseQuery(query);
            }
            return (uri);
        }

        static  string ParseQuery(string query)
        {
            // ideal key=value&key=value
            // problem key=part & part&key=value  -- dosent split well
            // ideally need to have an escape sequence

            string parsed = "";
            string key = "";
            string value = "";

            string[] q = query.Split('&');
            for (int i = 0; i < q.Length; i++)
            {
                if (q[i].IndexOf('=') > 0)
                {
                    key = q[i].Split('=')[0];
                    value = q[i].Split('=')[1];
                    try
                    {
                        if (i < q.Length - 1)
                        {
                            if (q[i + 1].IndexOf('=') < 0)
                            {
                                value = value + "&" + q[i + 1];
                                i += 1;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        //
                    }
                }

                parsed = parsed + key + "=" + UrlEncode(value) + "&";
            }
            parsed = parsed.TrimEnd('&');
            return (parsed);
        }

        /// <summary>
        /// This is a different Url Encode implementation since the default .NET one outputs the percent encoding in lower case.
        /// While this is not a problem with the percent encoding spec, it is used in upper case throughout OAuth
        /// </summary>
        /// <param name="value">The value to Url encode</param>
        /// <returns>Returns a Url encoded string</returns>
        static string UrlEncode(string value)
        {
            StringBuilder result = new StringBuilder();

            foreach (char symbol in value)
            {
                if (_unreservedChars.IndexOf(symbol) != -1)
                {
                    result.Append(symbol);
                }
                else
                {
                    result.Append('%' + String.Format("{0:X2}", (int)symbol));
                }
            }

            return result.ToString();
        }
    }
}
