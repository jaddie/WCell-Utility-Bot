using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class LinusTorvaldus
    {
        #region AddLinusTorvaldsFactCommand

        public class AddLinusTorvaldsFactCommand : Command
        {
            public AddLinusTorvaldsFactCommand()
                : base("al", "addlinus", "addtorvalds")
            {
                Usage = "ac Linus fact here.";
                Description = "Add a Linus Torvalds fact to storage";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var norris = new StreamWriter("LinusFacts.txt", true) { AutoFlush = false };
                    norris.WriteLine(trigger.Args.Remainder);
                    trigger.Reply("Added the new Linus Torvalds fact: {0} to storage", trigger.Args.Remainder);
                    norris.Close();
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region RandomLinusTorvaldsFactCommand

        public class RandomLinusTorvaldsFactCommand : Command
        {
            public RandomLinusTorvaldsFactCommand()
                : base("rl", "linus", "torvalds")
            {
                Usage = "rl";
                Description = "Get a random fact about Linus Torvalds";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var linusLines = new List<string>();
                    var linus = new StreamReader("LinusFacts.txt");
                    while (!linus.EndOfStream)
                    {
                        linusLines.Add(linus.ReadLine());
                    }
                    var rand = new Random();
                    int randnum = rand.Next(0, linusLines.Count - 1);
                    trigger.Reply(linusLines[randnum]);
                    linus.Close();
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
