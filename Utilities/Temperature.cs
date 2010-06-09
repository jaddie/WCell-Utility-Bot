using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot.Utilities
{
    class Celsius : Command
    {
                public Celsius()
            : base("celsius")
        {
            Usage = "celsius degree";
            Description = "returns celsius converted to other measurements";
        }

        public override void Process(CmdTrigger trigger)
        {
            try
            {
                int celsius = Convert.ToInt32(trigger.Args.Remainder.Trim());
                int fahrenheit = (celsius * 9 / 5) + 32;
                int kelvin = celsius + 273;

                trigger.Reply(trigger.Args.Remainder + " Celsius is , " + fahrenheit.ToString() + "F , " + kelvin.ToString() + " Kelvin");
            }
            catch(Exception e)
            {
            }
        }
    }

    class Fahrenheit : Command
    {
        public Fahrenheit()
            : base("fahrenheit")
        {
            Usage = "celsius degree";
            Description = "returns fahrenheit converted to other measurements";
        }

        public override void Process(CmdTrigger trigger)
        {
            try
            {
                int fahrenheit = Convert.ToInt32(trigger.Args.Remainder.Trim());
                int celsius = (fahrenheit - 32) * 5 / 9;
                int kelvin = (fahrenheit + 459) * 5 / 9;

                trigger.Reply(trigger.Args.Remainder + " Fahrenheit is , " + celsius.ToString() + "C , " + kelvin.ToString() + " Kelvin");
            }
            catch (Exception e)
            {
            }
        }
    }
}
