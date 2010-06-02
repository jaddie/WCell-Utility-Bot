using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class LegalBay : Command
    {
        public LegalBay()
            : base("pb", "piratebay")
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
}
