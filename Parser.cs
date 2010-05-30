using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class Parser
    {
        #region ParseCommand

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
                    JadBot.ReplyChan = trigger.Channel.ToString();
                }
                catch (Exception e)
                {
                    JadBot.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion

    }
}
