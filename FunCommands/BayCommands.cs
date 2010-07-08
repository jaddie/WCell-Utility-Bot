using System;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.FunCommands
{
    class BayCommands
    {
        public class LegalBay : Command
        {
            public LegalBay() : base("pb", "piratebay")
            {
                Usage = "piratebay searchterm";
                Description = "search for torrents at piratebay";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string searchTerm = trigger.Args.Remainder.Replace(" ", "%20");
                    trigger.Reply("http://thepiratebay.org/search/" + searchTerm + "/0/7/0");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
        public class PasteBay : Command
        {
            public PasteBay() : base("paste", "pastebay")
            {
                Usage = "pastebay";
                Description = "returns link to pastebay";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("If you have more than 2 lines to show us, please paste them at http://wcell.pastebay.com/ and show us the link.");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
        public class BayImg : Command
        {
            public BayImg() : base("BayImg")
            {
                Usage = "bayimg";
                Description = "returns link to bayimg";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("If you have a picture you like show us or perhaps a file you want to send us .rar it and upload to http://bayimg.com/ and show us the link.");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
    }
}
