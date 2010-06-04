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

                #endregion // TODO: Clean this ugly setting up.

                #region Parser Setup

                Parser.StartInfo.UseShellExecute = false;
                Parser.StartInfo.RedirectStandardInput = true;
                Parser.StartInfo.RedirectStandardOutput = true;
                Parser.StartInfo.FileName = string.Format("{0}WCell.Tools.exe", ToolsFolder);
                Parser.StartInfo.WorkingDirectory = ToolsFolder;
                Parser.Start();
                Parser.BeginOutputReadLine();
                ParserConsoleInput = new StreamWriter(Parser.StandardInput.BaseStream) {AutoFlush = true};
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
                    UtilityMethods.OnConsoleText(line);
                }
            }
                #region Main Exception Handling

            catch (Exception e)
            {
                UtilityMethods.Print(string.Format("Exception {0} \n {1}", e.Message, e.StackTrace), true);
                WriteErrorSystem.WriteError(new List<string> {"Exception:", e.Message, e.StackTrace});
                UtilityMethods.Print(WebLinkToGeneralFolder + "ErrorLog.txt", true);
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
        public static void Client_Connected(Connection con)
        {
            UtilityMethods.Print("Connected to IRC Server", true);
        }
        public static void Irc_Disconnected(IrcClient arg1, bool arg2)
        {
            try
            {
                UtilityMethods.Print("Disconnected from IRC server, Attempting reconnect in 5 seconds", true);
                Thread.Sleep(5000);
                Irc.BeginConnect(Irc._network[0].ToString(), Port);
            }
            catch (Exception e)
            {
                UtilityMethods.Print(e.Data + e.StackTrace,true);
            }

        }

        #region IrcSystem
        public static void OnConnecting(Connection con)
        {
            UtilityMethods.Print("Connecting to IRC server", true);
            IrcLog.WriteLine(DateTime.Now + " : Connecting to server");
        }
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
                UtilityMethods.Print(e.Data + e.StackTrace, true);
            }
        }
        protected override void OnUnknownCommandUsed(CmdTrigger trigger)
        {
            return;
        }
        protected override void OnQueryMsg(IrcUser user, StringStream text)
        {
            UtilityMethods.Print(user + text.String, true);
        }
        protected override void OnText(IrcUser user, IrcChannel chan, StringStream text)
        {
            try
            {
                CommandHandler.RemoteCommandPrefix = text.String.StartsWith("~") ? "~" : "@";
                if (text.String.Contains("ACTION") && text.String.ToLower().Contains("utilitybot"))
                {
                    if (chan != null)
                        Irc.CommandHandler.Describe(chan, FunCommands.ReactToAction(), chan.Args);
                    else
                        Irc.CommandHandler.Describe(user, FunCommands.ReactToAction(), user.Args);
                }

                #region MessagesSent

                UtilityMethods.Print(string.Format("User {0} on channel {1} Sent {2}", user, chan, text), true);

                #endregion
            }
            catch (Exception e)
            {
                CommandHandler.Msg("#woc", e.Message);
                UtilityMethods.Print(e.StackTrace + e.Message, true);
            }
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
                UtilityMethods.Print(e.Data + e.StackTrace,true);
                return false;
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
                var writer = new StreamWriter(GeneralFolder + "toolsoutput.txt", true) {AutoFlush = true};
                writer.WriteLine(e.Data);
                UtilityMethods.Print(e.Data,true);
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
                UtilityMethods.Print(ex.Data + ex.StackTrace,true);
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
                UtilityMethods.Print(WebLinkToGeneralFolder + "ErrorLog.txt");
            }
            catch(Exception ex)
            {
                UtilityMethods.Print(ex.Data + ex.StackTrace);
            }
        }

        #endregion

        #endregion
    }
}