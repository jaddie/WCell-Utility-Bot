using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class Information
    {
        #region WCellLogsCommand

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
                trigger.Reply("The WCell Test Server Logs are at: " + JadBot.WebLinkToLogsFolder);
            }
        }

        #endregion
        #region UnparsedLogsCommand

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
                trigger.Reply(JadBot.WebLinkToUnparsedFolder);
            }
        }

        #endregion
        #region UploadCommand

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
                              JadBot.UploadSite);
            }
        }

        #endregion
        #region ParsedLogsCommand

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
                trigger.Reply(JadBot.WebLinkToParsedFolder);
            }
        }

        #endregion
        #region GeneralFilesCommand

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
                trigger.Reply(JadBot.WebLinkToGeneralFolder);
            }
        }

        #endregion
        #region AttackCommand

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
                try
                {
                    string[] attacks = {
                                           "slaps %s",
                                           "kicks %s",
                                           "punches %s in the face",
                                           "stings %s with a needle",
                                           "shoots %s with a phaser"
                                       };
                    if (!string.IsNullOrEmpty(trigger.Args.Remainder))
                    {
                        var rand = new Random();
                        int randomchoice = rand.Next(0, 4);
                        if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
                        {
                            trigger.Reply("I can't find that person to attack them!");
                            return;
                        }
                        string attack = attacks[randomchoice].Replace("%s", trigger.Args.Remainder);
                        JadBot.Irc.CommandHandler.Describe(trigger.Target, attack, trigger.Args);
                    }
                    else
                    {
                        trigger.Reply("You didnt give me a nick to attack!");
                    }
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region GitRepoCommand

        public class GitRepoCommand : Command
        {
            public GitRepoCommand() : base("git","repo")
            {
                Usage = "git";
                Description = "Display git repo info";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("You can find WCell's GIT repository at: http://github.com/WCell/WCell");  //TODO: Add link to wiki
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region WCellTerrainRepoCommand

        public class WCellTerrainRepoCommand : Command
        {
            public WCellTerrainRepoCommand()
                : base("wcellterrain")
            {
                Usage = "wcellterrain";
                Description = "Replies with the link to the wcell terrain repo";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("WCell Terrain Git Repo: http://github.com/WCell/WCell-Terrain");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region DBCommand

        public class DBCommand : Command
        {
            public DBCommand()
                : base("db")
            {
                Usage = "db";
                Description = "Shows the wiki link for supported databases";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("WCell supports the following database projects: http://www.wcell.org/Wiki/index.php?title=Supported_Databases");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region WCellDumpsCommand

        public class WCellDumpsCommand : Command
        {
            public WCellDumpsCommand()
                : base("dumps", "wcelldumps")
            {
                Usage = "wcelldumps";
                Description = "Provides link to dumps folder";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("WCell Test Server Dumps Are At: http://wcell.org/Dumps");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
    }
}
