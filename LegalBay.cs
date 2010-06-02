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
                trigger.Reply("http://thepiratebay.org/search/" + trigger.Args.Remainder + "/0/7/0");
            }
            catch (Exception e)
            {
                UtilityMethods.Print(e.Data + e.StackTrace, true);
            }
        }
    }
}
