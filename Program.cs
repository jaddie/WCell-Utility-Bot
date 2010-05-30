#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using Squishy.Irc;
using Squishy.Irc.Commands;
using Squishy.Network;
using Timer = System.Timers.Timer;

#endregion

namespace Jad_Bot
{
    public class JadBot : IrcClient
    {
        #region MainExecution

        #region Fields

        #region Lists

        public static readonly List<string> ChannelList = new List<string> {"#WOC", "#wcell.dev,wcellrulz", "#wcell"};
        public static readonly List<string> Matches = new List<string>();
        public static readonly List<string> FileLines = new List<string>();

        #endregion

        public static readonly DumpReader DumpReader = new DumpReader();

        #region IRC Connection info

        private const int Port = 6667;

        public static readonly JadBot Irc = new JadBot
                                                 {
                                                     Nicks = new[] {"WCellUtilityBot", "Jad|UtilityBot"},
                                                     UserName = "Jad_WCellParser",
                                                     Info = "WCell's AutoParser",
                                                     _network = Dns.GetHostAddresses("irc.quakenet.org")
                                                 };

        private IPAddress[] _network;

        #endregion

        #region Streams

        public static StreamReader Config;
        public static StreamWriter ParserConsoleInput;
        public static readonly StreamWriter IrcLog = new StreamWriter("IrcLog.log", true);
        #endregion

        #region Folder strings

        public static string UnparsedFolder;
        public static string ParsedFolder;
        public static string GeneralFolder;
        public static string ToolsFolder;
        public static string UploadSite;
        public static string WebLinkToParsedFolder;
        public static string WebLinkToUnparsedFolder;
        public static string WebLinkToGeneralFolder;
        public static string WebLinkToLogsFolder;

        #endregion

        #region Other

        public static readonly Process Parser = new Process();
        public static string LogFile;
        public static string ReplyChan = "#woc";
        public static readonly Stopwatch Runtimer = new Stopwatch();

        #endregion

        #region ErrorHandling

        public static readonly Timer ErrorTimer = new Timer();
        public static string Error = "Error: ";

        #endregion

        #endregion Fields

        public static void Main()
        {
            try
            {
                Parser.OutputDataReceived += Parser_OutputDataReceived;
                IrcLog.AutoFlush = false;

                Console.ForegroundColor = ConsoleColor.Yellow;

                #region Config File Setup

                if (!File.Exists("config.txt"))
                {
                    var configfile = File.CreateText("Config.txt");
                    configfile.WriteLine("ToolsFolder:");
                    configfile.Close();
                    Config = new StreamReader("Config.txt");
                }
                else
                {
                    Config = new StreamReader("config.txt");
                }

                #endregion

                #region Setup Variables from config

                string readLine;
                while (!Config.EndOfStream)
                {
                    try
                    {
                        readLine = Config.ReadLine();
                        if (readLine.StartsWith("ToolsFolder:", true, null))
                            ToolsFolder = readLine.Remove(0, 12);
                        if (readLine.StartsWith("UnparsedFolder:", true, null))
                            UnparsedFolder = readLine.Remove(0, 15);
                        if (readLine.StartsWith("ParsedFolder:", true, null))
                            ParsedFolder = readLine.Remove(0, 13);
                        if (readLine.StartsWith("WebLinkToParsedFolder:", true, null))
                            WebLinkToParsedFolder = readLine.Remove(0, 22);
                        if (readLine.StartsWith("WebLinkToUnparsedFolder:", true, null))
                            WebLinkToUnparsedFolder = readLine.Remove(0, 24);
                        if (readLine.StartsWith("WebLinkToGeneralFolder:", true, null))
                            WebLinkToGeneralFolder = readLine.Remove(0, 23);
                        if (readLine.StartsWith("LocalPathToGeneralFolder:", true, null))
                            GeneralFolder = readLine.Remove(0, 25);
                        if (readLine.StartsWith("WebLinkToLogsFolder:", true, null))
                            WebLinkToLogsFolder = readLine.Remove(0, 20);
                        if (readLine.StartsWith("UploadSite:", true, null))
                            UploadSite = readLine.Remove(0, 11);
                        if (readLine.StartsWith("Network:", true, null))
                            Irc._network = Dns.GetHostAddresses(readLine.Remove(0, 8));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Please check your config, one of your variables is not correctly set.");
                    }
                }
                Config.Close();

                #region ExceptionsForNullConfigSettings

                if (string.IsNullOrEmpty(ToolsFolder)) throw new Exception("Toolsfolder is not set in config!");
                if (string.IsNullOrEmpty(UnparsedFolder))
                    throw new Exception("Unparsed logs folder not set in config!");
                if (string.IsNullOrEmpty(ParsedFolder)) throw new Exception("Parsed logs folder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToUnparsedFolder))
                    throw new Exception("WebLinkToUnparsedFolder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToParsedFolder))
                    throw new Exception("WebLinkToParsedFolder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToGeneralFolder))
                    throw new Exception("WebLinkToGeneralFolder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToLogsFolder))
                    throw new Exception("WebLinkToLogsFolder not set in config!");
                if (string.IsNullOrEmpty(UploadSite)) throw new Exception("UploadSite not set in config!");

                #endregion

                #endregion

                #region Parser Setup

                Parser.StartInfo.UseShellExecute = false;
                Parser.StartInfo.RedirectStandardInput = true;
                Parser.StartInfo.RedirectStandardOutput = true;
                Parser.StartInfo.FileName = string.Format("{0}WCell.Tools.exe", ToolsFolder);
                Parser.StartInfo.WorkingDirectory = ToolsFolder;
                Parser.Start();
                Parser.BeginOutputReadLine();
                ParserConsoleInput = new StreamWriter(Parser.StandardInput.BaseStream) {AutoFlush = false};
                // Input into the console
                Irc.Disconnected += Irc_Disconnected;
                Process utility = Process.GetCurrentProcess();
                utility.Exited += UtilityExited;

                #endregion

                #region FoldersOutput

                Console.WriteLine(UnparsedFolder);
                Console.WriteLine(ParsedFolder);
                Console.WriteLine(GeneralFolder);
                Console.WriteLine(ToolsFolder);
                Console.WriteLine(WebLinkToParsedFolder);
                Console.WriteLine(WebLinkToGeneralFolder);
                Console.Write(UploadSite);

                #endregion

                #region IRC Connecting

                Irc.Client.Connecting += OnConnecting;
                Irc.Client.Connected += Client_Connected;
                Irc.BeginConnect(Irc._network[0].ToString(), Port);

                #endregion

                Runtimer.Start();
                while (true) // Prevent WCell.Tools from crashing - due to console methods inside the program.
                {
                    var line = new StringStream(Console.ReadLine());
                    OnConsoleText(line);
                }
            }
                #region Main Exception Handling

            catch (Exception e)
            {
                Print(string.Format("Exception {0} \n {1}", e.Message, e.StackTrace), true);
                WriteErrorSystem.WriteError(new List<string> {"Exception:", e.Message, e.StackTrace});
                Print(WebLinkToGeneralFolder + "ErrorLog.txt", true);
                foreach (var chan in ChannelList)
                {
                    Irc.CommandHandler.Msg(chan, "The error is at the following address: {0}",
                                           WebLinkToGeneralFolder + "ErrorLog.txt");
                }
                Console.WriteLine("Closing in 5 seconds");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }

            #endregion
        }

        public static void UtilityExited(object sender, EventArgs e)
        {
            Parser.Kill();
        }

        public static void Print(string text, bool irclog = false)
        {
            try
            {
                Console.WriteLine(DateTime.Now + text);
                if (irclog)
                    IrcLog.WriteLine(DateTime.Now + text);
            }
            catch(Exception e)
            {
                Console.WriteLine("Write Failure" + e.Data + e.StackTrace);
            }

        }

        public static void Client_Connected(Connection con)
        {
            Print("Connected to IRC Server", true);
        }

        public static void Irc_Disconnected(IrcClient arg1, bool arg2)
        {
            try
            {
                Print("Disconnected from IRC server, Attempting reconnect in 5 seconds", true);
                Thread.Sleep(5000);
                Irc.BeginConnect(Irc._network[0].ToString(), Port);
            }
            catch (Exception e)
            {
                Print(e.Data + e.StackTrace,true);
            }

        }

        public static void OnConsoleText(StringStream cText)
        {
            try
            {
                switch (cText.NextWord().ToLower())
                {
                    case "join":
                        {
                            if (cText.Remainder.Contains(","))
                            {
                                var chaninfo = cText.Remainder.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                                if (chaninfo.Length > 1)
                                    Irc.CommandHandler.Join(chaninfo[0], chaninfo[1]);
                                else
                                    Irc.CommandHandler.Join(chaninfo[0]);
                            }
                            else
                            {
                                Irc.CommandHandler.Join(cText.Remainder);
                            }
                        }
                        break;
                    case "say":
                        {
                            var chan = cText.NextWord();
                            var msg = cText.Remainder;
                            Irc.CommandHandler.Msg(chan, msg);
                        }
                        break;
                    case "quit":
                        {
                            Parser.Kill();
                            Print("Shutting down due to console quit command..", true);
                            foreach (var chan in ChannelList)
                            {
                                Irc.CommandHandler.Msg(chan, "Shutting down in 5 seconds due to console quit command..");
                            }
                            Thread.Sleep(5000);
                            Irc.Client.DisconnectNow();
                            Environment.Exit(0);
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                Print(e.Data + e.StackTrace, true);
            }
        }

        #region IrcSystem

        protected override void Perform()
        {
            try
            {
                IrcCommandHandler.Initialize();
                CommandHandler.RemoteCommandPrefix = "~";
                foreach (var chan in ChannelList)
                {
                    if (chan.Contains(","))
                    {
                        var chaninfo = chan.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (chaninfo.Length > 1)
                            CommandHandler.Join(chaninfo[0], chaninfo[1]);
                        else
                            CommandHandler.Join(chaninfo[0]);
                    }
                    else
                    {
                        CommandHandler.Join(chan);
                    }
                }
            }
            catch (Exception e)
            {
                Print(e.Data + e.StackTrace, true);
            }
        }

        protected override void OnUnknownCommandUsed(CmdTrigger trigger)
        {
            return;
        }

        public override bool MayTriggerCommand(CmdTrigger trigger, Command cmd)
        {
            try
            {
                if (File.Exists("auth.txt") && cmd.Name.ToLower() != "restartwcellcommand" &&
                    cmd.Name.ToLower() != "addauth")
                {
                    using (var reader = new StreamReader("auth.txt"))
                    {
                        var usernames = new List<string>();
                        while (!reader.EndOfStream)
                        {
                            usernames.Add(reader.ReadLine());
                        }
                        if (usernames.Any(username => username == trigger.User.AuthName))
                        {
                            return true;
                        }
                    }
                }
                return trigger.User.IsOn("#wcell.dev") || trigger.User.IsOn("#woc") || trigger.User.IsOn("#wcell");
            }
            catch(Exception e)
            {
                Print(e.Data + e.StackTrace,true);
                return false;
            }
        }

        public static void OnConnecting(Connection con)
        {
            Print("Connecting to IRC server", true);
            IrcLog.WriteLine(DateTime.Now + " : Connecting to server");
        }

        protected override void OnQueryMsg(IrcUser user, StringStream text)
        {
            Print(user + text.String, true);
        }

        public static string ReactToAction()
        {
            try
            {
                string[] actions = {
                                       "dodges",
                                       "ducks",
                                       "evades",
                                       "parries",
                                       "blocks",
                                       "does the monkey dance"
                                   };
                var rand = new Random();
                var randomchoice = rand.Next(0, 5);
                return actions[randomchoice];
            }
            catch(Exception e)
            {
                Print(e.Data + e.StackTrace,true);
                return "";
            }
        }

        protected override void OnText(IrcUser user, IrcChannel chan, StringStream text)
        {
            try
            {
                CommandHandler.RemoteCommandPrefix = text.String.StartsWith("~") ? "~" : "@";
                if (text.String.Contains("ACTION") && text.String.ToLower().Contains("utilitybot"))
                {
                    if (chan != null)
                        Irc.CommandHandler.Describe(chan, ReactToAction(), chan.Args);
                    else
                        Irc.CommandHandler.Describe(user, ReactToAction(), user.Args);
                }

                #region MessagesSent

                Print(string.Format("User {0} on channel {1} Sent {2}", user, chan, text), true);

                #endregion
            }
            catch (Exception e)
            {
                CommandHandler.Msg("#woc", e.Message);
                Print(e.StackTrace + e.Message, true);
            }
        }

        #endregion

        #endregion

        #region ParserOutput Handling

        public static void Parser_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            #region Done
            try
            {
                if (e.Data.Contains("Done."))
                {
                    try
                    {
                        using (var parsedFile = new StreamReader(ParsedFolder + LogFile))
                        {
                            if (parsedFile.BaseStream.Length < 1)
                                throw new Exception("Parsed file is empty");

                            Irc.CommandHandler.Msg(ReplyChan, "Completed Parsing your file is at {0}{1}",
                                                   WebLinkToParsedFolder, LogFile);
                        }
                    }
                    catch (Exception excep)
                    {
                        Irc.CommandHandler.Msg(ReplyChan, "The Following Exception Occured {0}, check input file",
                                               excep.Message);
                    }
                }
                var writer = new StreamWriter(GeneralFolder + "toolsoutput.txt", true) {AutoFlush = false};
                writer.WriteLine(e.Data);
                writer.Close();

                #endregion

                #region Exception

                if (e.Data.Contains("Exception"))
                {
                    ErrorTimer.Interval = 5000;
                    ErrorTimer.Start();
                    ErrorTimer.Elapsed += ErrorTimerElapsed;
                    while (ErrorTimer.Enabled)
                    {
                        Error = Error + e.Data + "\n";
                    }
                }

                #endregion
            }
            catch(Exception ex)
            {
                Print(ex.Data + ex.StackTrace,true);
            }
        }

        #region Parser Error Handling

        public static void ErrorTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                ErrorTimer.Stop();
                WriteErrorSystem.WriteError(new List<string> {"Exception:", Error});
                Irc.CommandHandler.Msg(ReplyChan, WebLinkToGeneralFolder + "ErrorLog.txt");
                Print(WebLinkToGeneralFolder + "ErrorLog.txt");
            }
            catch(Exception ex)
            {
                Print(ex.Data + ex.StackTrace);
            }
        }

        #endregion

        #endregion

        #region RandomLinkGeneration

        public static string GetLink()
        {
            try
            {
                var builder = new StringBuilder();
                builder.Append(RandomString(4, true));
                builder.Append(RandomNumber(1000, 999999));
                builder.Append(RandomString(2, true));
                return builder.ToString();
            }
            catch(Exception e)
            {
                Print(e.Data + e.StackTrace,true);
            }
            return null;
        }

        public static string RandomString(int size, bool lowerCase)
        {
            try
            {
                var builder = new StringBuilder();
                var random = new Random();
                for (var i = 0; i < size; i++)
                {
                    var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26*random.NextDouble() + 65)));
                    builder.Append(ch);
                }
                return lowerCase ? builder.ToString().ToLower() : builder.ToString();
            }
            catch(Exception e)
            {
                Print(e.Data + e.StackTrace, true);
                return "";
            }
        }

        public static int RandomNumber(int min, int max)
        {
            try
            {
                var random = new Random();
                return random.Next(min, max);
            }
            catch(Exception e)
            {
                Print(e.Data + e.StackTrace,true);
            }
            return 0;
        }

        #endregion

        #region Custom Commands






        #region Nested type: ClearQueueCommand

        public class ClearQueueCommand : Command
        {
            public ClearQueueCommand() : base("ClearQueue", "CQ")
            {
                Usage = "ClearSendQueue";
                Description = "Command to clear the send queue. Useful if you want the bot to stop spamming";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var lines = trigger.Irc.Client.SendQueue.Length;
                    trigger.Irc.Client.SendQueue.Clear();
                    trigger.Reply("Cleared SendQueue of {0} lines", lines);
                }
                catch(Exception e)
                {
                    Print(e.Data + e.StackTrace,true);
                }
            }
        }

        #endregion

        #region Nested type: DownloadLogRemotely

        public class DownloadLogRemotely : Command
        {
            public DownloadLogRemotely() : base("downloadlog")
            {
                Usage = "downloadlog savefilename httplink";
                Description =
                    "Downloads a log from the specified link, savefilename is the name to use for the downloaded file, httplink is the direct link to the file on the site / server";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string filename = trigger.Args.NextWord();
                    string httpLink = trigger.Args.Remainder;
                    trigger.Reply("Attempting to download log from {0} and save in unparsed folder as {1}", httpLink,
                                  filename);
                    var client = new WebClient();
                    client.DownloadFile(httpLink, UnparsedFolder + filename);
                    using (var downloadedfile = new StreamReader(UnparsedFolder + filename))
                    {
                        if (downloadedfile.BaseStream.Length < 1)
                        {
                            trigger.Reply("The downloaded file looks empty, are you sure your link is valid?");
                        }
                        else
                        {
                            trigger.Reply(
                                "Download complete file is saved in the unparsed logs folder as {0} you can now use the parse command on it.",
                                filename);
                        }
                    }
                }
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(new List<string>
                                                    {e.Message + e.StackTrace + e.InnerException + e.Source});
                    trigger.Reply("Error occured:{0}", WebLinkToGeneralFolder + "ErrorLog.txt");
                    Print(string.Format("Error Occured in download file command: {0}",e.Message + e.StackTrace + e.InnerException + e.Source),true);
                }
            }
        }

        #endregion



        #region Nested type: FindMethod

        public class FindMethod : Command
        {
            public FindMethod()
                : base("method", "findmethod", "print")
            {
                Usage = "method World.Resync";
                Description = "Find and display a method from the code";
            }
            
            //~find World.ReSync
            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("Hai, I don't work yet! :O");

                }
                catch (Exception e)
                {
                    Print(e.Data + e.StackTrace,true);
                }            }
        }

        #endregion

        #region oldidea

        /*var filename = trigger.Args.NextWord(".");
                var method = trigger.Args.NextWord();
                DirectoryInfo wcellsource = new DirectoryInfo(@"c:\wcellsource");
                FileInfo[] wcellsourcefiles = wcellsource.GetFiles(string.Format("{0}.cs",filename), SearchOption.AllDirectories);
                int leftbracecount = 0;
                int rightbracecount = 0;
                foreach(FileInfo fileinfo in wcellsourcefiles)
                {
                    StreamReader file = new StreamReader(fileinfo.FullName);
                    while(!file.EndOfStream)
                    {
                        string line = file.ReadLine();
                        if(line.ToLower().Contains(method))
                        {
                            while (rightbracecount <= leftbracecount)
                            {
                                line = file.ReadLine();

                            }
                        }
                    }
                }
                readresults.Clear();
                dumptype = dump;
                var dumpreader = new StreamReader(dumptype);
                while (!dumpreader.EndOfStream)
                {
                    var currentline = dumpreader.ReadLine().ToLower();
                    if (currentline.Contains(query.ToLower()))
                    {
                        readresults.Add(currentline);
                    }
                }
                return readresults;*/

        #endregion









        #region Nested type: ReadSourceFile

        public class ReadSourceFile : Command
        {
            private readonly List<FileInfo> _files = new List<FileInfo>();

            public ReadSourceFile()
                : base("find", "RS", "grep")
            {
                Usage =
                    "rs -i iftherearemorethan1filesbeforefileidhere -l lowerline-upperline -includepath spaceseperated search terms notcase sensitive";
                Description =
                    "Allows you to search through the source code of WCell and show the specified lines from found files.";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("Scanning source for matches, please allow some time...");
                    var sourceDir = new DirectoryInfo(@"c:\wcellsource");
                    int linenumber = 0;
                    int upperlinenumber = 0;
                    int fileid = 0;
                    bool includefullpath = false;
                    bool fileidgiven = false;
                    if (trigger.Args.String.Contains("-i"))
                    {
                        if (trigger.Args.NextModifiers() == "i")
                        {
                            fileidgiven = true;
                            fileid = trigger.Args.NextInt(0);
                        }
                    }
                    if (trigger.Args.String.Contains("-l"))
                    {
                        if (trigger.Args.NextModifiers() == "l")
                        {
                            linenumber = trigger.Args.NextInt(0, "-");
                            upperlinenumber = trigger.Args.NextInt(0);
                        }
                    }
                    if (trigger.Args.String.Contains("-includepath"))
                    {
                        if (trigger.Args.NextModifiers() == "includepath")
                        {
                            includefullpath = true;
                        }
                    }
                    var searchterms = new List<string>();
                    while (trigger.Args.HasNext)
                    {
                        string searchterm = trigger.Args.NextWord().Trim();
                        searchterms.Add(searchterm);
                    }
                    GetFilesNormalName(sourceDir, _files);
                    foreach (var file in _files)
                    {
                        int runs = 0;
                        foreach (var searchterm in searchterms)
                        {
                            string nameTerm = file.Name;
                            if (includefullpath)
                            {
                                nameTerm = file.FullName;
                            }
                            if (nameTerm.ToLower().Contains(searchterm.ToLower()))
                            {
                                runs = runs + 1;
                                if (runs == searchterms.Count)
                                {
                                    Matches.Add(file.FullName);
                                }
                            }
                        }
                    }
                    if (Matches.Count > 1 && !fileidgiven)
                    {
                        var readWriter = new StreamWriter(GeneralFolder + @"\SourceOptions.txt", false)
                                             {AutoFlush = false};
                        int i = 0;
                        foreach (var match in Matches)
                        {
                            readWriter.WriteLine(i + ": " + match);
                            i = i + 1;
                        }
                        trigger.Reply("There were more than 1 found files, please choose!");
                        string line = "";
                        int id = 0;
                        foreach (var match in Matches)
                        {
                            if (id > 5)
                                continue;
                            line = line + "\n" + id + ": " + match;
                            id = id + 1;
                        }
                        trigger.Reply(line);
                        if (Matches.Count > 5)
                        {
                            trigger.Reply(
                                "\n There are even more results, check the link or be more specific use same command again but with -i file id at the start.");
                            trigger.Reply(WebLinkToGeneralFolder + "SourceOptions.txt");
                        }
                        readWriter.Close();
                    }
                    else
                    {
                        if (Matches.Count == 1)
                        {
                            string path = Matches[fileid];
                            path = path.Replace("c:\\wcellsource", "Master");
                            path = path.Replace('\\', '-');
                            var selectionWriter = new StreamWriter(GeneralFolder + string.Format("\\{0}.html", path))
                                                      {AutoFlush = false};
                            string lines = ReadFileLines(Matches[0], linenumber, upperlinenumber, fileid);
                            selectionWriter.WriteLine("<html>\n<body>\n<pre>");
                            selectionWriter.WriteLine("Filename: {0}", path);
                            selectionWriter.Write(lines);
                            selectionWriter.WriteLine("</pre>\n</body>\n</html>");
                            trigger.Reply(WebLinkToGeneralFolder + "{0}.html", path);
                            selectionWriter.Close();
                        }
                        else
                        {
                            if (!fileidgiven)
                                trigger.Reply("No Results Apparently");
                        }
                    }
                    if (fileidgiven)
                    {
                        int matchesnum = Matches.Count - 1;
                        if (fileid > matchesnum)
                        {
                            trigger.Reply(
                                "Invalid Fileid Selection, are you sure there are more than 1 files found? run the query without -i number to check results first.");
                            return;
                        }
                        string path = Matches[fileid];
                        path = path.Replace("c:\\wcellsource", "Master");
                        path = path.Replace('\\', '-');
                        using (var selectionWriter = new StreamWriter(GeneralFolder + string.Format("\\{0}.html", path)))
                        {
                            string lines = ReadFileLines(Matches[fileid], linenumber, upperlinenumber, fileid);
                            selectionWriter.WriteLine("<html>\n<body>\n");
                            selectionWriter.WriteLine("Filename: {0}", path);
                            selectionWriter.Write(lines);
                            selectionWriter.WriteLine("\n</body>\n</html>");
                            trigger.Reply(WebLinkToGeneralFolder + "{0}.html", path);
                        }
                    }
                    Matches.Clear();
                    _files.Clear();
                    FileLines.Clear();
                }
                catch (Exception e)
                {
                    trigger.Reply("Please check your input, error occured: {0}", e.Message);
                    Print(e.Data + e.StackTrace,true);
                }
            }

            public static void GetFilesNormalName(DirectoryInfo sourceDir, List<FileInfo> files)
            {
                try
                {
                    foreach (var dir in sourceDir.GetDirectories())
                    {
                        if (dir.Name.Contains(".svn") | dir.Name.Contains(".git") | dir.Name.Contains("obj"))
                        {
                            continue;
                        }
                        files.AddRange(dir.GetFiles().Where(file => file.Extension != ".dll"));
                        GetFilesNormalName(dir, files);
                    }
                }
                catch(Exception e)
                {
                    Print(e.Data + e.StackTrace,true);
                }
            }

            public static string HighlightText(string text)
            {
                text = "<span class=\"highlighttext\">" + text + "</span>";
                return text;
            }

            public static string ReadFileLines(string readFile, int readLineLower, int readLineUpper, int fileId)
            {
                try
                {
                    var syn = new SyntaxHighlighter {AddStyleDefinition = true};
                    using(var file = new StreamReader(readFile))
                    {
                        int currentlinenumber = 1;
                        if (readLineUpper == 0 && readLineUpper < readLineLower)
                        {
                            readLineUpper = readLineLower;
                        }
                        string returnlines =
                            "<table border=\"0\"> <style> .highlighttext { BACKGROUND-COLOR:#F4FA58 } </style>";
                        var filelinesids = new List<string>();
                        var filelines = new List<string>();
                        string path = Matches[fileId];
                        path = path.Replace("c:\\wcellsource", "Master");
                        path = path.Replace('\\', '-');
                        while (!file.EndOfStream)
                        {
                            string line = file.ReadLine();
                            var fileinfo = new FileInfo(readFile);
                            if (fileinfo.Extension == ".cs")
                                line = syn.Highlight(line);
                            syn.AddStyleDefinition = false;
                            if (currentlinenumber >= readLineLower && currentlinenumber <= readLineUpper &&
                                readLineUpper != 0)
                            {
                                filelinesids.Add(
                                    string.Format("<a name=\"{0}\" href=\"{1}/{2}.html#{0}\">", currentlinenumber,
                                                  WebLinkToGeneralFolder, path) + currentlinenumber + "</a>:");
                                filelines.Add(HighlightText(line));
                            }
                            else
                            {
                                filelinesids.Add(
                                    string.Format("<a name=\"{0}\" href=\"{1}/{2}.html#{0}\">", currentlinenumber,
                                                  WebLinkToGeneralFolder, path) + currentlinenumber + "</a>:");
                                filelines.Add(line);
                            }
                            currentlinenumber = currentlinenumber + 1;
                        }
                        string[] fileids = {""};
                        string[] fileLines = {""};
                        filelinesids.ForEach(filelineid => fileids[0] = fileids[0] + "\n" + filelineid);
                        filelines.ForEach(fileline => fileLines[0] = fileLines[0] + "\n" + fileline);
                        returnlines = returnlines + "\n <tr><td><pre>" + fileids[0] + "</pre></td> <td><pre>" +
                                      fileLines[0] +
                                      "</pre></td></tr>";
                        returnlines = returnlines + "</table>";
                        return returnlines;
                    }
                }
                catch(Exception e)
                {
                    Print(e.Data + e.StackTrace,true);
                }
                return null;
            }
        }

        #endregion









        #endregion
    }
}