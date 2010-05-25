using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using Squishy.Irc;
using Squishy.Irc.Commands;
using Squishy.Network;
using Timer = System.Timers.Timer;

namespace Jad_Bot
{
    public class JadBot : IrcClient
    {
        #region MainExecution

        #region Fields

        #region Lists

        private static readonly List<string> ChannelList = new List<string> {"#WOC", "#wcell.dev,wcellrulz", "#wcell"};
        private static List<string> ConsoleOutput = new List<string>();
        private static List<string> FileLineOptions = new List<string>();
        private static List<string> matches = new List<string>();
        private static List<string> FileLines = new List<string>();
        #endregion
        public static bool Grabinput = true;
        public static string ToolsOutput = "";
        public static DumpReader SDR = new DumpReader();
        #region IRC Connection info

        private const int Port = 6667;

        private static readonly JadBot Irc = new JadBot
                                                 {
                                                     Nicks = new[] {"WCellUtilityBot", "Jad|UtilityBot"},
                                                     UserName = "Jad_WCellParser",
                                                     Info = "WCell's AutoParser",
                                                     Network = Dns.GetHostAddresses("irc.quakenet.org")
                                                 };

        private new IPAddress[] Network;

        #endregion

        #region Streams

        private static StreamReader Config;
        private static StreamWriter ParserConsoleInput;
        private static readonly StreamWriter IrcLog = new StreamWriter("IrcLog.log", true);
        private static StreamWriter readWriter;
        private static StreamWriter selectionWriter;

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

        private static readonly Process Parser = new Process();
        private static string LogFile;
        private static string ReplyChan = "#woc";
        private static Stopwatch Runtimer = new Stopwatch();
        #endregion

        #region ErrorHandling

        private static readonly Timer ErrorTimer = new Timer();
        private static string _error = "Error: ";

        #endregion

        #endregion Fields

        public static void Main()
        {
            Parser.OutputDataReceived += Parser_OutputDataReceived;
            IrcLog.AutoFlush = true;
            try
            {
                Console.ForegroundColor = ConsoleColor.Yellow;

                #region Config File Setup

                if (!File.Exists("config.txt"))
                {
                    StreamWriter configfile = File.CreateText("Config.txt");
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
                            Irc.Network = Dns.GetHostAddresses(readLine.Remove(0,8));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Please check your config, one of your variables is not correctly set.");
                    }
                }
                Config.Close();

                #region ExceptionsForNullConfigSettings

                if (string.IsNullOrEmpty(ToolsFolder)) throw new Exception("Toolsfolder is not set in config!");
                if (string.IsNullOrEmpty(UnparsedFolder))throw new Exception("Unparsed logs folder not set in config!");
                if (string.IsNullOrEmpty(ParsedFolder)) throw new Exception("Parsed logs folder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToUnparsedFolder))throw new Exception("WebLinkToUnparsedFolder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToParsedFolder))throw new Exception("WebLinkToParsedFolder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToGeneralFolder))throw new Exception("WebLinkToGeneralFolder not set in config!");
                if (string.IsNullOrEmpty(WebLinkToLogsFolder))throw new Exception("WebLinkToLogsFolder not set in config!");
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
                ParserConsoleInput = new StreamWriter(Parser.StandardInput.BaseStream) {AutoFlush = true}; // Input into the console
                Irc.Disconnected += Irc_Disconnected;
                System.Diagnostics.Process utility = System.Diagnostics.Process.GetCurrentProcess();
                utility.Exited += new EventHandler(UtilityExited);
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
                Irc.Client.Connected += new Connection.ConnectedHandler(Client_Connected);
                Irc.BeginConnect(Irc.Network[0].ToString(), Port);

                #endregion
                Runtimer.Start();
                while (true) // Prevent WCell.Tools from crashing - due to console methods inside the program.
                {
                    Thread.Sleep(1000);
                }
            }
                #region Main Exception Handling

            catch (Exception e)
            {
                Print(string.Format("Exception {0} \n {1}",e.Message,e.StackTrace), true);
                WriteErrorSystem.WriteError(new List<string> { "Exception:", e.Message, e.StackTrace });
                Print(WebLinkToGeneralFolder + "ErrorLog.txt",true);
                foreach (var chan in ChannelList)
                {
                    Irc.CommandHandler.Msg(chan, "The error is at the following address: {0}",WebLinkToGeneralFolder + "ErrorLog.txt");
                }
                Console.WriteLine("Closing in 5 seconds");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }

            #endregion
        }

        static void UtilityExited(object sender, EventArgs e)
        {
            Parser.Kill();
        }

        static void Print(string text,bool irclog = false)
        {
            Console.WriteLine(DateTime.Now + text);
            if(irclog)
            IrcLog.WriteLine(DateTime.Now + text);
        }

        static void Client_Connected(Connection con)
        {
            Print("Connected to IRC Server",true);
        }

        static void Irc_Disconnected(IrcClient arg1, bool arg2)
        {
            Print("Disconnected from IRC server, Attempting reconnect in 5 seconds",true);
            Thread.Sleep(5000);
            Irc.BeginConnect(Irc.Network[0].ToString(),Port);
        }

        static void JadBot_Closing(object arg1, object arg2)
        {
            var tools = Process.GetProcessesByName("WCell.tools");
            foreach (var process in tools)
            {
                process.Kill();
            }
        }

        private static void OnConsoleText(StringStream cText)
        {
            switch (cText.NextWord().ToLower())
            {
                case "join":
                    {
                        if (cText.Remainder.Contains(","))
                        {
                            var chaninfo = cText.Remainder.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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
                        Print("Shutting down due to console quit command..",true);
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

        #region IrcSystem

        protected override void Perform()
        {
            IrcCommandHandler.Initialize();
            CommandHandler.RemoteCommandPrefix = "~";
            foreach (var chan in ChannelList)
            {
                if (chan.Contains(","))
                {
                    string[] chaninfo = chan.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
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

        protected override void OnUnknownCommandUsed(CmdTrigger trigger)
        {
            return;
        }

        public override bool MayTriggerCommand(CmdTrigger trigger, Command cmd)
        {
            if (File.Exists("auth.txt") && cmd.Name.ToLower() != "restartwcellcommand" && cmd.Name.ToLower() != "addauth")
            {
                using (StreamReader reader = new StreamReader("auth.txt"))
                {
                    List<string> usernames = new List<string>();
                    while (!reader.EndOfStream)
                    {
                        usernames.Add(reader.ReadLine());
                    }
                    foreach (var username in usernames)
                    {
                        if (username == trigger.User.AuthName)
                        {
                            return true;
                        }
                    }
                }
            }
            if (trigger.User.IsOn("#wcell.dev") || trigger.User.IsOn("#woc") || trigger.User.IsOn("#wcell"))
                return true;
            else
                return false;
        }

        private static void OnConnecting(Connection con)
        {
            Print("Connecting to IRC server",true);
            IrcLog.WriteLine(DateTime.Now + " : Connecting to server");
        }

        protected override void OnQueryMsg(IrcUser user, StringStream text)
        {
            Print(user + text.String,true);
        }

        private static string ReactToAction()
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

        protected override void OnText(IrcUser user, IrcChannel chan, StringStream text)
        {
            try
            {
                CommandHandler.RemoteCommandPrefix = text.String.StartsWith("~") ? "~" : "@";
                if (text.String.Contains("ACTION") && text.String.ToLower().Contains("utilitybot"))
                {
                    if(chan != null)
                    Irc.CommandHandler.Describe(chan, ReactToAction(), chan.Args);
                    else
                    Irc.CommandHandler.Describe(user, ReactToAction(), user.Args);
                }
                #region MessagesSent

                Print(string.Format("User {0} on channel {1} Sent {2}", user, chan, text),true);
                #endregion
            }
            catch (Exception e)
            {
                CommandHandler.Msg("#woc", e.Message);
                Print(e.StackTrace + e.Message,true);
            }
        }

        #endregion

        #endregion

        #region ParserOutput Handling

        private static void Parser_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            #region Done

            if (e.Data.Contains("Done."))
            {
                try
                {
                    using (var ParsedFile = new StreamReader(ParsedFolder + LogFile))
                    {
                        if (ParsedFile.BaseStream.Length < 1)
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
            StreamWriter writer = new StreamWriter(GeneralFolder + "toolsoutput.txt",true);
            writer.AutoFlush = true;
            writer.WriteLine(e.Data);
            writer.Close();
            #endregion

            #region Exception

            if (e.Data.Contains("Exception"))
            {
                ErrorTimer.Interval = 5000;
                ErrorTimer.Start();
                ErrorTimer.Elapsed += errorTimer_Elapsed;
                while (ErrorTimer.Enabled)
                {
                    _error = _error + e.Data + "\n";
                }
            }

            #endregion
        }

        #region Parser Error Handling

        private static void errorTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            ErrorTimer.Stop();
            WriteErrorSystem.WriteError(new List<string> { "Exception:", _error });
            Irc.CommandHandler.Msg(ReplyChan, WebLinkToGeneralFolder + "ErrorLog.txt");
            Print(WebLinkToGeneralFolder + "ErrorLog.txt");
        }

        #endregion

        #endregion

        #region RandomLinkGeneration

        public static string GetLink()
        {
            var builder = new StringBuilder();
            builder.Append(RandomString(4, true));
            builder.Append(RandomNumber(1000, 999999));
            builder.Append(RandomString(2, true));
            return builder.ToString();
        }

        private static string RandomString(int size, bool lowerCase)
        {
            var builder = new StringBuilder();
            var random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26*random.NextDouble() + 65)));
                builder.Append(ch);
            }
            if (lowerCase)
                return builder.ToString().ToLower();
            return builder.ToString();
        }

        private static int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }

        #endregion

        #region Custom Commands

        #region Nested type: StealObjectCommand

        public class StealObjectCommand : Command
        {
            public StealObjectCommand()
                : base("Steal", "StealObject")
            {
                Usage = "steal name of person to steal object from";
                Description = "Steals the provided person's cookie and munches it, unless you specify a different object";
            }
            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var objectowner = trigger.Args.Remainder;
                    var Object = "cookie";
                    if (trigger.Args.NextModifiers() == "object")
                    {
                        Object = trigger.Args.NextWord();
                    }
                    var target = trigger.Target.ToString();
                    if (trigger.Args.NextModifiers() == "target")
                    {
                        target = trigger.Args.NextWord();
                    }
                    if (string.IsNullOrEmpty(objectowner))
                    {
                        trigger.Reply("You didn't tell me who to steal from!");
                    }
                    if (Object.ToLower() == "cookie")
                    {
                        Irc.CommandHandler.Describe(target, string.Format("steals {0}'s cookie, om nom nom nom!", objectowner), trigger.Args);
                    }
                    else
                    {
                        Irc.CommandHandler.Describe(target, string.Format("steals {0}'s {1}, ha! Pwned",trigger.Args.Remainder,Object),trigger.Args);
                    }
                }
                catch (Exception e)
                {
                    trigger.Reply("I cant steal their cookie :'(");
                    trigger.Reply(e.Message);
                }
            }
        }

        #endregion

        #region Nested type: ActionCommand

        public class ActionCommand : Command
        {
            public ActionCommand()
                : base("Action", "Me")
            {
                Usage = "action -target destination action to write";
                Description = "Writes the provided Action.";
            }
            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var target = trigger.Target.ToString();
                    if (trigger.Args.NextModifiers() == "target")
                    {
                        target = trigger.Args.NextWord();
                    }
                    Irc.CommandHandler.Describe(target,trigger.Args.Remainder, trigger.Args);
                }
                catch (Exception e)
                {
                    trigger.Reply("I cant write that action, perhaps invalid target?");
                    trigger.Reply(e.Message);
                }
            }
        }

        #endregion

        #region Nested type: AddAuthCommand

        public class AddAuthCommand : Command
        {
            public AddAuthCommand()
                : base("AddAuth", "AuthAdd")
            {
                Usage = "Auth qauthusername";
                Description = "Adds the username to Auth list";
            }
            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    using (StreamWriter streamWriter = new StreamWriter("auth.txt", true))
                    {
                        streamWriter.WriteLine(trigger.Args.Remainder);
                        trigger.Reply("Added Q Auth {0}",trigger.Args.Remainder);
                    }
                }
                catch (Exception e)
                {
                    trigger.Reply("I cant write to the auth file!");
                    trigger.Reply(e.Message);
                }
            }
        }

        #endregion

        #region Nested type: ReadSourceFileCommand

        public class ReadSourceFile : Command
        {
            readonly List<FileInfo> _files = new List<FileInfo>();
            public ReadSourceFile()
                : base("find", "RS","grep")
            {
                Usage = "rs -i iftherearemorethan1filesbeforefileidhere -l lowerline-upperline -includepath spaceseperated search terms notcase sensitive";
                Description = "Allows you to search through the source code of WCell and show the specified lines from found files.";
            }
            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("Scanning source for matches, please allow some time...");
                    var sourceDir = new DirectoryInfo(@"c:\wcellsource");
                    var linenumber = 0;
                    var upperlinenumber = 0;
                    var fileid = 0;
                    var includefullpath = false;
                    var fileidgiven = false;
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
                    if(trigger.Args.String.Contains("-includepath"))
                    {
                        if(trigger.Args.NextModifiers() == "includepath")
                        {
                            includefullpath = true;
                        }
                    }
                    var searchterms = new List<string>();
                    while (trigger.Args.HasNext)
                    {
                        var searchterm = trigger.Args.NextWord().Trim();
                        searchterms.Add(searchterm);
                    }
                    GetFilesNormalName(sourceDir, _files);
                    foreach (var file in _files)
                    {
                        var runs = 0;
                        foreach (string searchterm in searchterms)
                        {
                            var nameTerm = file.Name;
                            if (includefullpath)
                            {
                                nameTerm = file.FullName;
                            }
                            if (nameTerm.ToLower().Contains(searchterm.ToLower()))
                            {
                                runs = runs + 1;
                                if (runs == searchterms.Count)
                                {
                                    matches.Add(file.FullName);
                                }
                            }
                        }
                    }
                    if (matches.Count > 1 && !fileidgiven)
                    {
                        var readWriter = new StreamWriter(GeneralFolder + @"\SourceOptions.txt", false)
                                             {AutoFlush = true};
                        int i = 0;
                        foreach (var match in matches)
                        {
                            readWriter.WriteLine(i + ": " + match);
                            i = i + 1;
                        }
                        trigger.Reply("There were more than 1 found files, please choose!");
                        string line = "";
                        int id = 0;
                        foreach (var match in matches)
                        {
                            if (id > 5)
                                continue;
                            line = line + "\n" + id + ": " + match;
                            id = id + 1;
                        }
                        trigger.Reply(line);
                        if (matches.Count > 5)
                        {
                             trigger.Reply("\n There are even more results, check the link or be more specific use same command again but with -i file id at the start.");
                             trigger.Reply(WebLinkToGeneralFolder + "SourceOptions.txt");
                        }
                        readWriter.Close();
                    }
                    else
                    {
                        if (matches.Count == 1)
                        {
                            var path = matches[fileid];
                            path = path.Replace("c:\\wcellsource", "Master");
                            path = path.Replace('\\', '-');
                            var selectionWriter = new StreamWriter(GeneralFolder + string.Format("\\{0}.html", path))
                                                      {AutoFlush = true};
                            string lines = ReadFileLines(matches[0], linenumber, upperlinenumber, fileid);
                            selectionWriter.WriteLine("<html>\n<body>\n<pre>");
                            selectionWriter.WriteLine("Filename: {0}", path);
                            selectionWriter.Write(lines);
                            selectionWriter.WriteLine("</pre>\n</body>\n</html>");
                            trigger.Reply(WebLinkToGeneralFolder + "{0}.html", path);
                            selectionWriter.Close();
                        }
                        else
                        {
                            if(!fileidgiven)
                            trigger.Reply("No Results Apparently");
                        }
                    }
                    if (fileidgiven)
                    {
                        int matchesnum = matches.Count - 1;
                        if(fileid > matchesnum)
                        {
                            trigger.Reply("Invalid Fileid Selection, are you sure there are more than 1 files found? run the query without -i number to check results first.");
                            return;
                        }
                        var path = matches[fileid];
                        path = path.Replace("c:\\wcellsource", "Master");
                        path = path.Replace('\\', '-');
                        var selectionWriter = new StreamWriter(GeneralFolder + string.Format("\\{0}.html", path))
                                                  {AutoFlush = true};
                        var lines = ReadFileLines(matches[fileid], linenumber, upperlinenumber, fileid);
                        selectionWriter.WriteLine("<html>\n<body>\n");
                        selectionWriter.WriteLine("Filename: {0}", path);
                        selectionWriter.Write(lines);
                        selectionWriter.WriteLine("\n</body>\n</html>");
                        trigger.Reply(WebLinkToGeneralFolder + "{0}.html",path);
                        selectionWriter.Close();
                    }
                    matches.Clear();
                    _files.Clear();
                    FileLines.Clear();
                }
                catch(Exception e)
                {
                    trigger.Reply("Please check your input, error occured: {0}",e.Message);
                }
            }
            static void GetFilesNormalName(DirectoryInfo sourceDir, List<FileInfo> files)
            {
                foreach (var dir in sourceDir.GetDirectories())
                {
                    if (dir.Name.Contains(".svn") | dir.Name.Contains(".git") | dir.Name.Contains("obj"))
                    {
                        continue;
                    }
                    foreach (var file in dir.GetFiles())
                    {
                        if (file.Extension == ".dll")
                            continue;
                        files.Add(file);
                    }
                    GetFilesNormalName(dir,files);
                }
            }
            static string HighlightText(string text)
            {
                text = "<span class=\"highlighttext\">" + text + "</span>";
                return text;
            }
            public static string ReadFileLines(string readFile, int readLineLower, int readLineUpper, int fileId)
            {
                var syn = new SyntaxHighlighter {AddStyleDefinition = true};
                var file = new StreamReader(readFile);
                var currentlinenumber = 1;
                if(readLineUpper == 0 && readLineUpper < readLineLower)
                {
                    readLineUpper = readLineLower;
                }
                var returnlines = "<table border=\"0\"> <style> .highlighttext { BACKGROUND-COLOR:#F4FA58 } </style>";
                List<string> filelinesids = new List<string>();
                List<string> filelines = new List<string>();
                var path = matches[fileId];
                path = path.Replace("c:\\wcellsource", "Master");
                path = path.Replace('\\', '-');
                while (!file.EndOfStream)
                {
                    var line = file.ReadLine();
                    var fileinfo = new FileInfo(readFile);
                    if(fileinfo.Extension == ".cs")
                    line = syn.Highlight(line);
                    syn.AddStyleDefinition = false;
                    if(currentlinenumber >= readLineLower && currentlinenumber <= readLineUpper && readLineUpper != 0)
                    {
                        filelinesids.Add(string.Format("<a name=\"{0}\" href=\"{1}/{2}.html#{0}\">",currentlinenumber,WebLinkToGeneralFolder,path) + currentlinenumber + "</a>:");
                        filelines.Add(HighlightText(line));
                    }
                    else
                    {
                        filelinesids.Add(string.Format("<a name=\"{0}\" href=\"{1}/{2}.html#{0}\">", currentlinenumber, WebLinkToGeneralFolder, path) + currentlinenumber + "</a>:");
                        filelines.Add(line);
                    }
                    currentlinenumber = currentlinenumber + 1;
                }
                var fileids = "";
                var fileLines = "";
                foreach (var fileLineid in filelinesids)
                {
                    fileids = fileids + "\n" + fileLineid;
                }
                foreach (var fileline in filelines)
                {
                    fileLines = fileLines + "\n" + fileline;
                }
                returnlines = returnlines + "\n <tr><td><pre>" + fileids + "</pre></td> <td><pre>" + fileLines + "</pre></td></tr>";
                file.Close();
                returnlines = returnlines + "</table>";
                return returnlines;
            }
        }
        #endregion

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
                int lines = trigger.Irc.Client.SendQueue.Length;
                trigger.Irc.Client.SendQueue.Clear();
                trigger.Reply("Cleared SendQueue of {0} lines", lines);
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
                    WriteErrorSystem.WriteError(new List<string> { e.Message + e.StackTrace + e.InnerException + e.Source });
                    trigger.Reply("Error occured:{0}", WebLinkToGeneralFolder + "ErrorLog.txt");
                    Console.WriteLine("Error Occured in download file command: {0} {1}",e.Message + e.StackTrace + e.InnerException + e.Source);
                }
            }
        }

        #endregion

        #region Nested type: DumpTypesCommand

        public class DumpTypesCommand : Command
        {
            public DumpTypesCommand()
                : base("DumpTypes")
            {
                Usage = "Dumptypes";
                Description = "Prints out the list of different dump types";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply(
                        "AreaTriggers \n GOs \n Items \n NPCs \n Quests \n SpellsAndEffects \n Vehicles \n use these for the dumptype on the query command!");
                }
                catch
                {
                }
            }
        }

        #endregion

        #region Nested type: GeneralFilesCommand

        public class GeneralFilesCommand : Command
        {
            public GeneralFilesCommand()
                : base("general")
            {
                Usage = "general";
                Description = "Replies with link to the folder with general files in.";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply(WebLinkToGeneralFolder);
            }
        }

        #endregion

        #region Nested type: ListParsers

        public class ListParsers : Command
        {
            public ListParsers()
                : base("listparsers", "parsers")
            {
                Usage = "listparsers";
                Description = "Lists the various available parsers for use with the parsing system";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply("Available Parsers are as follows: \n 0:KSniffer \n 1: KSnifferSingleLine \n 2:Sniffitzt");
            }
        }

        #endregion

        #region Nested type: ParseCommand

        public class ParseCommand : Command
        {
            public ParseCommand()
                : base("Parse", "P")
            {
                Usage = "Parse logname.extension parsertype";
                Description =
                    "Command to parse a log file using the PacketAnalyser in WCell.Tools, parsertype can be selected if not selected defaults to KSnifferSingleLine, to view parsers use the Listparsers command";
            }

            public override void Process(CmdTrigger trigger)
            {
                string logFile = trigger.Args.NextWord();
                string parser = trigger.Args.Remainder;
                int parserChoice = 1;
                bool temp = int.TryParse(parser, out parserChoice);
                if (!temp)
                {
                    parserChoice = 1;
                }
                if (parser != null)
                {
                    switch (parser.ToLower())
                    {
                        case "ksniffer":
                            {
                                parserChoice = 0;
                            }
                            break;
                        case "ksniffersingleline":
                            {
                                parserChoice = 1;
                            }
                            break;
                        case "sniffitzt":
                            {
                                parserChoice = 2;
                            }
                            break;
                    }
                }
                trigger.Reply("Command recieved attempting to parse file: {0} with parser {1}", logFile, parser);
                LogFile = logFile;
                ParserConsoleInput.WriteLine("pa uf -a");
                ParserConsoleInput.WriteLine("pa sp {0}", parserChoice);
                ParserConsoleInput.WriteLine(string.Format("pa sf {0}{1}", UnparsedFolder, logFile));
                ParserConsoleInput.WriteLine(string.Format("pa so {0}{1}", ParsedFolder, logFile));
                ParserConsoleInput.WriteLine("pa af eo _MOVE,_WARDEN");
                ParserConsoleInput.WriteLine("pa parse");
                ReplyChan = trigger.Channel.ToString();
            }
        }

        #endregion

        #region Nested type: ParsedLogsCommand

        public class ParsedLogsCommand : Command
        {
            public ParsedLogsCommand()
                : base("parsed")
            {
                Usage = "parsed";
                Description = "Replies with the link to the folder with parsed logs in.";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply(WebLinkToParsedFolder);
            }
        }

        #endregion

        #region Nested type: QueryDumpCommand

        public class QueryDumpCommand : Command
        {
            public QueryDumpCommand()
                : base("Query")
            {
                Usage = "Query dumptype partialspelloreffectname";
                Description =
                    "Command to read the spelldump from WCell.Tools and return a list of different matches for the query, dump defaults to spell if not recognised - use dumptypes command to see list.";
            }

            public override void Process(CmdTrigger trigger)
            {
                if (trigger.Args.Remainder.Length > 0)
                {
                    readWriter = new StreamWriter(GeneralFolder + "Options.txt");
                    readWriter.AutoFlush = true;
                    string dumptype;
                    switch (trigger.Args.NextWord().ToLower())
                    {
                        case "areatriggers":
                            {
                                dumptype = "areatriggers.txt";
                            }
                            break;
                        case "gos":
                            {
                                dumptype = "gos.txt";
                            }
                            break;
                        case "items":
                            {
                                dumptype = "items.txt";
                            }
                            break;
                        case "npcs":
                            {
                                dumptype = "npcs.txt";
                            }
                            break;
                        case "quests":
                            {
                                dumptype = "quests.txt";
                            }
                            break;
                        default:
                        case "spellsandeffects":
                            {
                                dumptype = "spellsandeffects.txt";
                            }
                            break;
                        case "vehicles":
                            {
                                dumptype = "vehicles.txt";
                            }
                            break;
                    }
                    List<string> readOutput = SDR.Read(dumptype, trigger.Args.Remainder);
                    int id = -1;
                    foreach (string line in readOutput)
                    {
                        id++;
                        readWriter.WriteLine(id + ": " + line);
                    }
                    readWriter.Close();
                    trigger.Reply(WebLinkToGeneralFolder + "Options.txt");
                }
                else
                {
                    trigger.Reply("It looks to me like you didnt put a query! :O");
                }
            }
        }

        #endregion

        #region Nested type: RestartWcellCommand

        public class RestartWcellCommand : Command
        {
            public RestartWcellCommand()
                : base("Restart")
            {
                Usage = "Restart Kill or Restart Safe or Restart Utility";
                Description =
                    "Command which will shutdown the WCell server or Utility Bot depending on what you set, use Kill to instant kill the server, use Safe to close with saving data and safely etc";
            }

            public override void Process(CmdTrigger trigger)
            {
                string nextWord = trigger.Args.NextWord().ToLower();
                if (nextWord == "kill")
                {
                    trigger.Reply("Killing WCell and restarting it");
                    var KillWCell = System.Diagnostics.Process.GetProcessesByName("wcell.realmserverconsole");
                    foreach (var p in KillWCell)
                    {
                        p.Kill();
                    }
                    System.Diagnostics.Process WcellRealmserver = new Process();
                    WcellRealmserver.StartInfo.FileName = @"c:\run\realmserver\debug\wcell.realmserverconsole.exe";
                    WcellRealmserver.StartInfo.WorkingDirectory = @"c:\run\realmserver\";
                    WcellRealmserver.Start();
                    var killauth = System.Diagnostics.Process.GetProcessesByName("wcell.authserverconsole");
                    foreach (var p in killauth)
                    {
                        p.Kill();
                    }
                    System.Diagnostics.Process WCellAuthserver = new Process();
                    WCellAuthserver.StartInfo.FileName = @"c:\run\authserver\debug\wcell.authserverconsole.exe";
                    WCellAuthserver.StartInfo.WorkingDirectory = @"c:\run\authserver";
                    WCellAuthserver.StartInfo.UseShellExecute = true;
                    WcellRealmserver.StartInfo.UseShellExecute = true;
                    WCellAuthserver.Start();
                    WcellRealmserver.Start();
                }
                if (nextWord == "safe")
                {
                    trigger.Reply("Safely shutting down WCell saving data and Starting it up again");
                    System.Diagnostics.Process prog = new Process();
                    prog.StartInfo.FileName = @"c:\program_launcher.exe";
                    prog.StartInfo.Arguments = @"c:\config.txt";
                    prog.Start();
                }
                if (nextWord == "utility")
                {
                    trigger.Reply("Restarting Utility - If there is no auto restarter the utility will not return");
                    Parser.Kill();
                    Environment.Exit(0);
                }
                if (nextWord == "authserver")
                {
                    Process[] AuthServer = System.Diagnostics.Process.GetProcessesByName("WCell.AuthServerConsole");
                    foreach (Process process in AuthServer)
                    {
                        trigger.Reply("Attempting to restart AuthServer");
                        process.Kill();
                        var WCellStarter = new Process();
                        WCellStarter.StartInfo.FileName = @"c:\run\authserver\debug\wcell.authserverconsole.exe";
                        WCellStarter.StartInfo.UseShellExecute = true;
                        WCellStarter.Start();
                    }
                    Thread.Sleep(3000);
                    AuthServer = System.Diagnostics.Process.GetProcessesByName("WCell.AuthServerConsole");
                    if (AuthServer.Length > 0)
                        trigger.Reply("AuthServer Seems to be Online again!");
                }
                if (nextWord == "realmserver")
                {
                    Process[] RealmServer = System.Diagnostics.Process.GetProcessesByName("WCell.RealmServerConsole");
                    foreach (Process process in RealmServer)
                    {
                        trigger.Reply("Attempting to restart RealmServer");
                        process.Kill();
                        System.Diagnostics.Process WcellRealmserver = new Process();
                        WcellRealmserver.StartInfo.FileName = @"c:\run\realmserver\debug\wcell.realmserverconsole.exe";
                        WcellRealmserver.StartInfo.WorkingDirectory = @"c:\run\realmserver\debug\";
                        WcellRealmserver.StartInfo.UseShellExecute = true;
                        WcellRealmserver.Start();
                    }
                    Thread.Sleep(3000);
                    RealmServer = System.Diagnostics.Process.GetProcessesByName("WCell.RealmServerConsole");
                    if (RealmServer.Length > 0)
                        trigger.Reply("RealmServer Seems to be Online again!");
                }
            }
        }

        #endregion

        #region Nested type: SelectDumpCommand

        public class SelectDumpCommand : Command
        {
            public SelectDumpCommand()
                : base("Select")
            {
                Usage = "Select id";
                Description =
                    "Select from the generated list from the Query command, where id is the number in the list to the left";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string randfilename = GetLink();
                    selectionWriter = new StreamWriter(GeneralFolder + string.Format("Selection{0}.txt", randfilename));
                    selectionWriter.AutoFlush = true;
                    List<string> selectOutput = SDR.Select(trigger.Args.NextInt());
                    if (selectOutput.Count > 0 && selectOutput != null)
                    {
                        foreach (string line in selectOutput)
                        {
                            selectionWriter.WriteLine(line);
                        }
                    }
                    else
                    {
                        trigger.Reply("The output from the selector was null :O");
                    }
                    selectionWriter.Close();
                    trigger.Reply(WebLinkToGeneralFolder + string.Format("Selection{0}.txt", randfilename));
                }
                catch (Exception excep)
                {
                    trigger.Reply("The Following Exception Occured {0}, check input", excep.Message);
                }
            }
        }

        #endregion

        #region Nested type: UnparsedLogsCommand

        public class UnparsedLogsCommand : Command
        {
            public UnparsedLogsCommand()
                : base("unparsed")
            {
                Usage = "unparsed";
                Description = "Replies with the link to the folder with the unparsed logs in.";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply(WebLinkToUnparsedFolder);
            }
        }

        #endregion

        #region Nested type: UploadCommand

        public class UploadCommand : Command
        {
            public UploadCommand()
                : base("Upload")
            {
                Usage = "Upload";
                Description =
                    "Replies with the address to use for uploading unparsed logs, and downloading parsed logs.";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply("Please use {0} for uploads such as unparsed logs you wish to parse with this bot",
                              UploadSite);
            }
        }

        #endregion

        #region Nested type: WCellLogsCommand

        public class WCellLogsCommand : Command
        {
            public WCellLogsCommand()
                : base("WCellLogs", "logs")
            {
                Usage = "wcelllogs";
                Description = "Show the link to the logs from the AutoDeployServer";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply("The WCell AutoDeploy Logs are at: " + WebLinkToLogsFolder);
            }
        }

        #endregion

        #region Nested type: RandomChuckNorrisFactCommand

        public class RandomChuckNorrisFactCommand : Command
        {
            public RandomChuckNorrisFactCommand()
                : base("rc", "chuck","norris")
            {
                Usage = "rc";
                Description = "Get a random fact about Chuck Norris";
            }

            public override void Process(CmdTrigger trigger)
            {
                var norrisLines = new List<string>();
                var norris = new StreamReader("ChuckNorrisFacts.txt");
                while (!norris.EndOfStream)
                {
                    norrisLines.Add(norris.ReadLine());
                }
                var rand = new Random();
                var randnum = rand.Next(0, norrisLines.Count - 1);
                trigger.Reply(norrisLines[randnum]);
                norris.Close();
            }
        }

        #endregion

        #region Nested type: AddChuckNorrisFactCommand

        public class AddChuckNorrisFactCommand : Command
        {
            public AddChuckNorrisFactCommand()
                : base("ac", "addchuck", "addnorris")
            {
                Usage = "ac Norris fact here.";
                Description = "Add a chuck norris fact to storage";
            }

            public override void Process(CmdTrigger trigger)
            {
                var norris = new StreamWriter("ChuckNorrisFacts.txt",true) {AutoFlush = true};
                norris.WriteLine(trigger.Args.Remainder);
                trigger.Reply("Added the new Chuck Norris fact: {0} to storage", trigger.Args.Remainder);
                norris.Close();
            }
        }

        #endregion

        #region Nested type: RandomLinusTorvaldsFactCommand

        public class RandomLinusTorvaldsFactCommand : Command
        {
            public RandomLinusTorvaldsFactCommand()
                : base("rl", "linus", "torvalds")
            {
                Usage = "rl";
                Description = "Get a random fact about Linus Torvalds";
            }

            public override void Process(CmdTrigger trigger)
            {
                var linusLines = new List<string>();
                var linus = new StreamReader("LinusFacts.txt");
                while (!linus.EndOfStream)
                {
                    linusLines.Add(linus.ReadLine());
                }
                var rand = new Random();
                var randnum = rand.Next(0, linusLines.Count - 1);
                trigger.Reply(linusLines[randnum]);
                linus.Close();
            }
        }

        #endregion

        #region Nested type: AddLinusTorvaldsFactCommand

        public class AddLinusTorvaldsFactCommand : Command
        {
            public AddLinusTorvaldsFactCommand()
                : base("al", "addlinus", "addtorvalds")
            {
                Usage = "ac Linus fact here.";
                Description = "Add a Linus Torvalds fact to storage";
            }

            public override void Process(CmdTrigger trigger)
            {
                var norris = new StreamWriter("LinusFacts.txt", true) {AutoFlush = true};
                norris.WriteLine(trigger.Args.Remainder);
                trigger.Reply("Added the new Linus Torvalds fact: {0} to storage", trigger.Args.Remainder);
                norris.Close();
            }
        }

        #endregion

        #region Nested type: EightBallCommand

        public class EightBallCommand : Command
        {
            public EightBallCommand()
                : base("eightball", "eight", "eb")
            {
                Usage = "eightball DecisionQuestion";
                Description = "Provide an answer to decision";
            }
            public override void Process(CmdTrigger trigger)
            {
                string[] eightballanswers = {
                                                	"As I see it, yes",
	                                                "Ask again later",
	                                                "Better not tell you now",
	                                                "Cannot predict now",
	                                                "Concentrate and ask again",
	                                                "Don't count on it",
	                                                "It is certain",
	                                                "It is decidedly so",
	                                                "Most likely",
	                                                "My reply is no",
	                                                "My sources say no",
	                                                "Outlook good",
	                                                "Outlook not so good",
	                                                "Reply hazy, try again",
	                                                "Signs point to yes",
	                                                "Very doubtful",
	                                                "Without a doubt",
	                                                "Yes",
	                                                "Yes - definitely",
	                                                "You may rely on it"
                                            };
                if(!string.IsNullOrEmpty(trigger.Args.Remainder))
                {
                    var rand = new Random();
                    var randomchoice = rand.Next(0, 19);
                    trigger.Reply(eightballanswers[randomchoice]);
                }
                else
                {
                    trigger.Reply("You didnt give me a decision question!");
                }
            }
        }
        #endregion

        #region Nested type: AttackCommand

        public class AttackCommand : Command
        {
            public AttackCommand()
                : base("attack")
            {
                Usage = "attack nick";
                Description = "Attack the given user";
            }
            public override void Process(CmdTrigger trigger)
            {
                string[] attacks = {
                                        "slaps %s",
                                        "kicks %s",
                                        "punches %s in the face",
                                        "stings %s with a needle",
                                        "shoots %s with a phaser"

                                   };
                if(!string.IsNullOrEmpty(trigger.Args.Remainder))
                {
                    var rand = new Random();
                    var randomchoice = rand.Next(0, 4);
                    if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
                    {
                        trigger.Reply("I can't find that person to attack them!");
                        return;
                    }
                    var attack = attacks[randomchoice].Replace("%s", trigger.Args.Remainder);
                    Irc.CommandHandler.Describe(trigger.Target,attack,trigger.Args);
                }
                else
                {
                    trigger.Reply("You didnt give me a nick to attack!");
                }
            }
        }
        #endregion

        #region Nested type: SendToolsCommand

        public class SendToolsCommand : Command
        {
            public SendToolsCommand()
                : base("st", "sendtools", "tools")
            {
                Usage = "tools command to send here";
                Description = "Sends a command directly through the tools project";
            }

            public override void Process(CmdTrigger trigger)
            {
                ParserConsoleInput.WriteLine(trigger.Args.Remainder);
                trigger.Reply("To see streaming output: {0}", WebLinkToGeneralFolder + "toolsoutput.txt");
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
                trigger.Reply("Hai, I don't work yet! :O");
            }
        }
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
        #endregion

        #region Nested type: RuntimeCommand

        public class RuntimeCommand : Command
        {
            public RuntimeCommand()
                : base("Runtime")
            {
                Usage = "runtime";
                Description = "Show how long the program has been running.";
            }
            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply("This UtilityBot has been running for: " + (int)Runtimer.Elapsed.TotalMinutes + " minutes, oh yeah I'm a good bot!");
            }
        }
        #endregion

        #region Nested type: PizzaCommand

        public class PizzaCommand : Command
        {
            public PizzaCommand()
                : base("pizza")
            {
                Usage = "pizza nickname";
                Description = "Steal the person's pizza.";
            }
            public override void Process(CmdTrigger trigger)
            {
                if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
                {
                    trigger.Reply("I can't find that person to eat their pizza!");
                    return;
                }
                trigger.Irc.CommandHandler.Describe(trigger.Target,string.Format("steals {0}'s pizza and eats it nomnomnom!",trigger.Args.Remainder),trigger.Args);
            }
        }
        #endregion

        #region Nested type: BeerCommand

        public class BeerCommand : Command
        {
            public BeerCommand()
                : base("beer")
            {
                Usage = "beer nickname";
                Description = "Steal the person's beer.";
            }
            public override void Process(CmdTrigger trigger)
            {
                if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
                {
                    trigger.Reply("I can't find that person to drink their beer!");
                    return;
                }
                trigger.Irc.CommandHandler.Describe(trigger.Target, string.Format("steals {0}'s beer and gulps it all down *slurp*!", trigger.Args.Remainder), trigger.Args);
            }
        }
        #endregion
        #endregion
    }
}