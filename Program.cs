﻿#region Used Namespaces

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using Jad_Bot.FunCommands;
using System.Text;
using System.Threading;
using System.Timers;
using Jad_Bot.Utilities;
using Jad_Bot.WCellCommands;
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
        public static List<Exception> Exceptions = new List<Exception>();
        #endregion

        public static readonly DumpReader DumpReader = new DumpReader();

        #region IRC Connection info

        private const int Port = 6667;
        public static int SendQueue
        {
            get { return ThrottledSendQueue.CharsPerSecond; }
            set { ThrottledSendQueue.CharsPerSecond = value; }
        }
        public static readonly JadBot Irc = new JadBot
                                                 {
                                                     Nicks = new[] {"WCellUtilityBot", "Jad|UtilityBot"},
                                                     UserName = "Jad_WCellParser",
                                                     Info = "WCell's AutoParser",
                                                     _network = Dns.GetHostAddresses("irc.quakenet.org")
                                                 };
        public static System.Timers.Timer SpamTimer = new System.Timers.Timer();
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
        public static string ReplyChan = "#wcell";
        public static readonly Stopwatch Runtimer = new Stopwatch();
        public static Process Utility = Process.GetCurrentProcess();
        #endregion

        #endregion Fields

        public static void Main()
        {
            try
            {
                Parser.OutputDataReceived += Parser_OutputDataReceived;
                IrcLog.AutoFlush = true;
                SpamTimer.Interval = 2000;
                SpamTimer.Elapsed += SpamTimer_Elapsed;
                Console.ForegroundColor = ConsoleColor.Yellow;
                QuitReason = "I've been told to leave, bai!!";
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
                Utility.Exited += UtilityExited;

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
                Irc.Client.InternalDisconnected += Client_InternalDisconnected;
                Irc.BeginConnect(Irc._network[0].ToString(), Port);

                #endregion

                Runtimer.Start();
                System.Windows.Forms.Application.Run();
            }
                #region Main Exception Handling

            catch (Exception e)
            {
                WriteErrorSystem.WriteError(e);
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

        static void Client_InternalDisconnected(Connection con, bool connectionLost)
        {
            Irc_Disconnected(null,false);
        }
        protected override void OnUserEncountered(IrcUser user)
        {
            if (user == null || user.Nick == null)
            {
                return;
            }
            using (var db = new UtilityBotDBContainer())
            {
                foreach (var message in db.Messages.Where(message => message.IrcNick.ToLower() == user.Nick.ToLower() || message.IrcNick.ToLower().StartsWith("domi") && user.Nick.ToLower().StartsWith("domi")))
                {
                    CommandHandler.Msg(user, "Date Left: " + message.DateLeft + "\nFrom Nick: " + message.FromIrcNick + "\nMessage Sent: " + message.MessageText);
                    db.Messages.DeleteObject(message);
                    db.SaveChanges();
                }
            }
        }
        static void SpamTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            SpamTimer.Stop();
        }

        public static void UtilityExited(object sender, EventArgs e)
        {
            Parser.Kill();
        }
        public static void Client_Connected(Connection con)
        {
            Console.WriteLine("Connected to IRC Server");
            IrcLog.WriteLine("Connected to IRC Server");
        }
        public static void Irc_Disconnected(IrcClient arg1, bool arg2)
        {
            try
            {
                var msg = "Disconnected from IRC server, Attempting reconnect in 5 seconds";
                Console.WriteLine(msg);
                IrcLog.WriteLine(msg);
                Thread.Sleep(5000);
                Irc.BeginConnect(Irc._network[0].ToString(), Port);
            }
            catch (Exception e)
            {
                WriteErrorSystem.WriteError(e);
            }

        }

        #region IrcSystem
        public static void OnConnecting(Connection con)
        {
            Console.WriteLine("\nConnecting to IRC server");
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
                WriteErrorSystem.WriteError(e);
            }
        }
        protected override void OnUnknownCommandUsed(CmdTrigger trigger)
        {
            return;
        }
        protected override void OnError(Squishy.Irc.Protocol.IrcPacket packet)
        {
            WriteErrorSystem.WriteError(new Exception(packet.Args));// shouldn't be using a hardcoded value here, but its okay because this bot is specific to wcell anyway.
            base.OnError(packet);
        }

        protected override void OnText(IrcUser user, IrcChannel chan, StringStream text)
        {
            try
            {
                CommandHandler.RemoteCommandPrefix = text.String.StartsWith("~") ? "~" : "@";
                if (text.String.ToLower().Contains("badger"))
                {
                    Random rand = new Random();
                    var randomint = rand.Next(0, 100);
                    if(randomint > 50)
                    {
                        chan.Msg("Ahhhh Snakkeeee its a snaakee!!!! http://www.youtube.com/watch?v=EIyixC9NsLI");
                    }
                    else
                    {
                        chan.Msg("Mushroom Mushroom Mushroom! http://www.youtube.com/watch?v=EIyixC9NsLI");
                    }
                }
                else
                {
                    if (text.String.ToLower().Contains("snake"))
                    {
                        chan.Msg("Badger...Badger...Badger...Badger.. http://www.youtube.com/watch?v=EIyixC9NsLI");
                    }
                    else
                    {
                        if (text.String.ToLower().Contains("mushroom"))
                        {
                            chan.Msg("Badger..Badger..Badger! http://www.youtube.com/watch?v=EIyixC9NsLI");
                        }
                    }
                }

                if (text.String.Contains("ACTION") && text.String.ToLower().Contains("utility") && text.String.ToLower().Contains("bot"))
                {
                    if (chan != null)
                        Irc.CommandHandler.Describe(chan, FunCommands.FunCommands.ReactToAction(), chan.Args);
                    else
                        Irc.CommandHandler.Describe(user, FunCommands.FunCommands.ReactToAction(), user.Args);
                }

                #region MessagesSent

                Console.WriteLine(string.Format("User {0} on channel {1} Sent {2}", user, chan, text));
                IrcLog.WriteLine(string.Format("User {0} on channel {1} Sent {2}", user, chan, text));
                #endregion
            }
            catch (Exception e)
            {
                CommandHandler.Msg("#woc", e.Message);
                WriteErrorSystem.WriteError(e);
            }
        }
        public override bool MayTriggerCommand(CmdTrigger trigger, Command cmd)
        {
            try
            {
                return true;
                /*
                if (trigger.Args.String.ToLower().StartsWith(CommandHandler.RemoteCommandPrefix + "help"))
                {
                    return true;
                }
                if (base.MayTriggerCommand(trigger, cmd))
                {
                    if (trigger.Target != null)
                    {
                        if (!SpamTimer.Enabled)
                        {
                            SpamTimer.Start();
                            return true;
                        }
                        else
                        {
                            trigger.User.Msg("Don't try to make me spam!");
                            return false;
                        }
                    }
                }
                return false;*/
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now.ToString() + ":" + e.ToString());
                return false;
            }
        }

        #endregion

        #endregion

        #region ParserOutput Handling

        public static void Parser_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (e.Data == null)
                {
                    return;
                }
                #region Done
                if (e.Data.Contains("Done."))
                {
                    try
                    {
                        using (var parsedFile = new StreamReader(ParsedFolder + LogFile))
                        {
                            if (string.IsNullOrEmpty(parsedFile.ReadToEnd().ToLower()))
                            {
                                throw new Exception("Parsed file is empty");
                            }
                            else
                            {
                                Irc.CommandHandler.Msg(ReplyChan, "Completed Parsing your file is at {0}{1}",
                                                       WebLinkToParsedFolder, LogFile);
                            }
                        }
                    }
                    catch (Exception excep)
                    {
                        Irc.CommandHandler.Msg(ReplyChan, "The Following Exception Occured {0}, check input file",
                                               excep.Message);
                    }
                }
                using(var writer = new StreamWriter(GeneralFolder + "toolsoutput.txt", true) {AutoFlush = true})
                {
                    writer.WriteLine(e.Data);
                }

                #endregion

                #region Exception

                if (e.Data.Contains("Exception") && !e.Data.ToLower().Contains("nlog"))
                {
                    var newexception = new StringStream(e.Data);
                    newexception.NextWord();
                    if (Exceptions.Where(exception => exception.Message == newexception.Remainder).Any())
                    {
                        return;
                    }
                    WriteErrorSystem.WriteError(new Exception(newexception.Remainder));
                }

                #endregion
            }
            catch(Exception ex)
            {
                WriteErrorSystem.WriteError(ex);
            }
        }
        #endregion
    }
}