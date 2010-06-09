using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot.Utilities
{
    class Celcius : Command
    {
                public Celcius()
            : base("celcius")
        {
            Usage = "celcius degree";
            Description = "returns celsius converted to other measurements";
        }

        public override void Process(CmdTrigger trigger)
        {
            try
            {
                int celcius = Convert.ToInt32(trigger.Args.Remainder.Trim());
                int fahrenheit = (celcius * 9 / 5) + 32;
                int kelvin = celcius + 273;

                trigger.Reply(trigger.Args.Remainder + " Celcius is , " + fahrenheit.ToString() + "F , " + kelvin.ToString() + " Kelvin");
            }
            catch(Exception e)
            {
                trigger.Reply(e.ToString());
            }
        }
    }

    class Fahrenheit : Command
    {
        public Fahrenheit()
            : base("fahrenheit")
        {
            Usage = "fahrenheit degree";
            Description = "returns fahrenheit converted to other measurements";
        }

        public override void Process(CmdTrigger trigger)
        {
            try
            {
                int fahrenheit = Convert.ToInt32(trigger.Args.Remainder.Trim());
                int celcius = (fahrenheit - 32) * 5 / 9;
                int kelvin = (fahrenheit + 459) * 5 / 9;

                trigger.Reply(trigger.Args.Remainder + " Fahrenheit is , " + celcius + "C , " + kelvin + " Kelvin");
            }
            catch (Exception e)
            {
                trigger.Reply(e.ToString());
            }
        }
    }
}
