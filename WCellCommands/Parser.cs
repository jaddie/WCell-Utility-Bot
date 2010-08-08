using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.WCellCommands
{
    class Parser
    {
        public class ParseCommand : Command
        {
            public ParseCommand() : base("Parse", "P")
            {
                Usage = "Parse logname.extension parsertype";
                Description =
                    "Command to parse a log file using the PacketAnalyser in WCell.Tools, parsertype can be selected if not selected defaults to KSnifferSingleLine, to view parsers use the Listparsers command";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string logFile = trigger.Args.NextWord();
                    string parser = trigger.Args.Remainder;
                    int parserChoice;
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
                    JadBot.LogFile = logFile;
                    JadBot.ParserConsoleInput.WriteLine("pa uf -a");
                    JadBot.ParserConsoleInput.WriteLine("pa sp {0}", parserChoice);
                    JadBot.ParserConsoleInput.WriteLine(string.Format("pa sf {0}{1}", JadBot.UnparsedFolder, logFile));
                    JadBot.ParserConsoleInput.WriteLine(string.Format("pa so {0}{1}", JadBot.ParsedFolder, logFile));
                    JadBot.ParserConsoleInput.WriteLine("pa af eo _MOVE,_WARDEN");
                    JadBot.ParserConsoleInput.WriteLine("pa parse");
                    JadBot.ReplyChan = trigger.Target.ToString();
                }
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(e);
                }
            }
        }
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
                JadBot.ParserConsoleInput.WriteLine(trigger.Args.Remainder);
                trigger.Reply("To see streaming output: {0}", JadBot.WebLinkToGeneralFolder + "toolsoutput.txt");
            }
        }
        public class DownloadLogRemotely : Command
        {
            public DownloadLogRemotely()
                : base("downloadlog")
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
                    client.DownloadFile(httpLink, JadBot.UnparsedFolder + filename);
                    using (var downloadedfile = new StreamReader(JadBot.UnparsedFolder + filename))
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
                    WriteErrorSystem.WriteError(e);
                    trigger.Reply("Error occured:{0}", JadBot.WebLinkToGeneralFolder + "ErrorLog.txt");                }
            }
        }
    }
}
