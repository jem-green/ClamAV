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
        #region Fields

        static string _unreservedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        static string _appdir = "c:\\program files\\clamav";
        static string _tempdir = "c:\\program files\\clamav";
        static bool _readInput = false;
        static bool _update = false;
        static bool _downloaded = false;
        static Progress _progress;
        static bool _showProgress = false;
        static bool _help = false;

        #endregion
        #region Methods
        static int Main(string[] args)
        {
            // Read in specific configuration

            Debug.WriteLine("Enter Main()");
            int errorCode = -2;

            if (args.Length > 0)
            {
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
            }

            Debug.WriteLine("Exit Main()");
            return (errorCode);
        }

        #endregion
        #region Private

        static bool ValidateArguments(string[] args)
        {
            // Passed args allow for changes to web address and application location

            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "-h":
                    case "--help":
                        {
                            _help = true;
                            break;
                        }
                }
            }
            
            if (_help == false)
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
            usage =  "                       Clam AntiVirus: Application Updater 0.2.0\n";
            usage += "           By The Green Team: https://www.32high.co.uk\n";
            usage += "           (C) 2020 32High\n";
            usage += "\n";
            usage += "    clamupdate [options]\n";
            usage += "\n";
            usage += "    --force                -f         Force the update\n";
            usage += "    --help                 -h         Show this help\n";
            usage += "    --progress             -p         Show progress\n";
            usage += "    --appdir=DIRECTORY                Install new application into DIRECTORY\n";
            usage += "    --tempdir=DIRECTORY               Download installer into DIRECTORY\n";
            usage += "\n";
            usage += "\n";
            return (usage);
        }

        static void PreProcess(string[] args)
        {
            // Assume that updater is located in the same location as clamd, freshclam etc.
            _appdir = System.Reflection.Assembly.GetExecutingAssembly().Location;
            int pos = _appdir.LastIndexOf('\\');
            if (pos > 0)
            {
                _appdir = _appdir.Substring(0, pos);
            }

            // Set the tempdir to c:\users\user\local\clamav
            string name = "clamav";
            _tempdir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + System.IO.Path.DirectorySeparatorChar + name;

            // Override the DIRECTORY from the command line

            for (int item = 0; item < args.Length; item++)
            {
                string argument = args[item];
                if (argument.Length > 10)
                {
                    if (argument.Substring(0, 10) == "--tempdir=")
                    {
                        _tempdir = argument.Substring(9, argument.Length - 10);
                        _tempdir = _tempdir.TrimStart('"');
                        _tempdir = _tempdir.TrimEnd('"');
                    }
                }
                else if (argument.Length > 9)
                {
                    if (argument.Substring(0, 9) == "--appdir=")
                    {
                        _appdir = argument.Substring(9, argument.Length - 9);
                        _appdir = _appdir.TrimStart('"');
                        _appdir = _appdir.TrimEnd('"');
                    }
                    else if (argument.Substring(0, 10) == "--progress")
                    {
                        _showProgress = true;
                    }
                }
                else if (argument.Length > 6)
                {
                    if (argument.Substring(0, 7) == "--force")
                    {
                        _update = true;
                    }
                }
                else if (argument.Length == 2)
                {
                    if (argument.Substring(0, 2) == "-f")
                    {
                        _update = true;
                    }
                    else if (argument.Substring(0, 2) == "-h")
                    {
                        _help = true;
                    }
                    else if (argument.Substring(0, 2) == "-p")
                    {
                        _showProgress = true;
                    }
                }
            }
            _readInput = false; // Indicate that we don't need to do a read input
        }

        static int PostProcess()
        {
            int errorCode = -1;

            string _filename = "clamd.exe";
            string host = "https://www.clamav.net";
            string path = "/downloads/";
            //string search = "click here</a>.</em></p>\r\n      </div>\r\n          <h3><strong>";
            string search = "click here</a>.</em></p>\n      </div>\n          <h3><strong>";
            string query = "";
            string data = "";
            string uri;

            // Retrieve the page content and search for the latest version

            HttpWebRequest request;
            uri = ParseUri(host, path, query);
            request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = WebRequestMethods.Http.Get;
            request.Accept = "text/html";
            request.UserAgent = "updateclient";

            // Enable TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

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
                if (pos > 0)
                {
                    int next = data.IndexOf("</strong>", pos);
                    if (next > 0)
                    {
                        search = data.Substring(pos + search.Length, next - pos - search.Length);
                    }
                }

                if (search.Length > 0)
                { 

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
                            if (fileVersion.Length == 0)
                            {
                                fileVersion = currentVersion.ProductVersion.Trim();
                            }
                        }

                        if ((fileVersion != search) || (_update == true))
                        {
                            Console.Error.WriteLine("Clamav(" + fileVersion + ") update available " + search);
                            Console.WriteLine("Application may prevent updates so issue PAUSE");

                            // Download

                            string platform = ".win.x64";
                            string extension = ".zip";
                            string installer = "clamav-" + search + platform + extension;

                            path = "/downloads/production/" + installer;
                            uri = ParseUri(host, path, query);

                            //https://www.clamav.net/downloads/production/clamav-0.104.0.win.x64.msi

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

                                    _downloaded = false;
                                    _progress = new Progress(0, 100);
                                    try
                                    {

                                        using (WebClient client = new WebClient())
                                        {
                                            client.Headers.Add("User-Agent", "updateclient");
                                            Console.Error.WriteLine("Download " + uri);
                                            Console.Error.WriteLine("Temporary location " + fileNamePath);
                                            if (_showProgress == true)
                                            {
                                                client.DownloadFileAsync(new Uri(uri), fileNamePath);
                                                client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgressCallback);
                                            }
                                            else
                                            {
                                                client.DownloadFile(uri, fileNamePath);
                                                _downloaded = true;
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        errorCode = 9;
                                        Console.Error.WriteLine("Cannot download file");
                                    }

                                    if (_showProgress == true)
                                    {
                                        _progress.Width = 80;
                                        _progress.Change = Progress.ChangeType.position;
                                        _progress.Visible = true;
                                        Console.CursorVisible = false;
                                        Console.CursorLeft = 0;
                                    }

                                    do
                                    {
                                        Thread.Sleep(1000);
                                    }
                                    while (_downloaded == false);


                                    try
                                    {
                                        // now need to install the downloaded exe

                                        Process proc;

                                        // Enable process to be killed if still running from stop

                                        proc = new System.Diagnostics.Process();

                                        ProcessStartInfo startInfo = new ProcessStartInfo();

                                        startInfo.FileName = "unzip.exe";
                                        startInfo.Arguments = "-q -o -j \"" + fileNamePath + "\" \"*.exe\" \"*.dll\" -d \"" + _appdir + "\"";
                                        startInfo.CreateNoWindow = true;
                                        startInfo.UseShellExecute = true;

                                        // Enable exit event to be raised

                                        proc.EnableRaisingEvents = true;
                                        proc.StartInfo = startInfo;
                                        try
                                        {
                                            Console.Error.WriteLine("Unpack " + fileNamePath);
                                            proc.Start();
                                            Console.WriteLine(startInfo.FileName + " " + startInfo.Arguments);

                                            proc.WaitForExit();
                                            Console.Error.WriteLine("Finished unpacking");
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
                                catch (WebException iwe)
                                {
                                    errorCode = 4;
                                    Console.Error.WriteLine("Could not download package from " + uri);
                                    Console.Error.WriteLine("Web Exception " + iwe.ToString());
                                }
                                catch (Exception ce)
                                {
                                    errorCode = 5;
                                    Console.Error.WriteLine("Exception " + ce.Message);
                                }
                            }
                            catch (Exception)
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
                    catch (FileNotFoundException)
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
                else
                {
                    errorCode = 10;
                    Console.Error.WriteLine("Version not found");
                }
            }
            catch (WebException we)
            {
                errorCode = 1;
                Console.Error.WriteLine("Could not connect to " + uri);
                Console.Error.WriteLine("Web Exception " + we.ToString());
            }

            return (errorCode);
        }

        private static void DownloadProgressCallback(object sender, DownloadProgressChangedEventArgs e)
        {
            // In case you don't have a progressBar Log the value instead

            _progress.Current = e.ProgressPercentage;
            _progress.Update();

            if (_progress.HasChanged == true)
            {
                Console.CursorLeft = 0;
                Console.Write(_progress.Show());
            }

            if (e.ProgressPercentage == 100)
            {
                _downloaded = true;
            }
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
                    catch { };
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
        
        #endregion
    }
}
