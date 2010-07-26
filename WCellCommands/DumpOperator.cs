using System;
using System.Collections.Generic;
using System.IO;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.WCellCommands
{
    class DumpOperator
    {
        public class QueryDumpCommand : Command
        {
            public QueryDumpCommand() : base("Query")
            {
                Usage = "Query -s -d dumptype partialspelloreffectname";
                Description = "Command to read the spelldump from WCell.Tools and return a list of different matches for the query, dump defaults to spell if not recognised - use dumptypes command to see list, -s means search for only lines including spell: -d dumptype is used to specify the dumptype e.g. GOs";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    if (trigger.Args.Remainder.Length > 0)
                    {
                        using (var readWriter = new StreamWriter(JadBot.GeneralFolder + "Options.txt") { AutoFlush = true })
                        {
                            var dumptype = "";
                            string filterterms = "";
                            var pos = trigger.Args.Position;
                            var next = trigger.Args.NextModifiers().ToLower();
                            if (next == "dumptype" || next == "dt" || next == "d")
                            {
                                dumptype = trigger.Args.NextWord().ToLower();
                            }
                            else
                                trigger.Args.Position = pos;
                            next = trigger.Args.NextModifiers().ToLower();
                            if (next == "filter" || next == "f" || next == "ft")
                            {
                                filterterms = trigger.Args.NextWord();
                            }
                            switch (dumptype)
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
                            List<string> readOutput = JadBot.DumpReader.Read(dumptype, trigger.Args.Remainder,filterterms);
                            if (readOutput.Capacity < 1)
                            {
                                trigger.Reply("No Results!");
                                return;
                            }
                            int id = -1;
                            foreach (var line in readOutput)
                            {
                                id++;
                                readWriter.WriteLine(id + ": " + line);
                            }
                        }
                        trigger.Reply(JadBot.WebLinkToGeneralFolder + "Options.txt");
                    }
                    else
                    {
                        trigger.Reply("It looks to me like you didnt put a query! :O");
                    }
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
        public class SelectDumpCommand : Command
        {
            public SelectDumpCommand() : base("Select")
            {
                Usage = "Select id";
                Description =
                    "Select from the generated list from the Query command, where id is the number in the list to the left";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string randfilename = UtilityMethods.GetLink();
                    using (var selectionWriter = new StreamWriter(JadBot.GeneralFolder + string.Format("Selection{0}.txt", randfilename)))
                    {
                        List<string> selectOutput = JadBot.DumpReader.Select(trigger.Args.NextInt());
                        if (selectOutput.Count > 0)
                        {
                            foreach (var line in selectOutput)
                            {
                                selectionWriter.WriteLine(line);
                            }
                        }
                        else
                        {
                            trigger.Reply("The output from the selector was null :O");
                        }
                    }
                    trigger.Reply(JadBot.WebLinkToGeneralFolder + string.Format("Selection{0}.txt", randfilename));
                }
                catch (Exception excep)
                {
                    trigger.Reply("The Following Exception Occured {0}, check input", excep.Message);
                    UtilityMethods.Print(excep.Data + excep.StackTrace, true);
                }
            }
        }
        public class DumpTypesCommand : Command
        {
            public DumpTypesCommand() : base("DumpTypes")
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
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
    }
}
