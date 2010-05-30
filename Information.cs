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
                trigger.Reply("The WCell AutoDeploy Logs are at: " + JadBot.WebLinkToLogsFolder);
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

    }
}
