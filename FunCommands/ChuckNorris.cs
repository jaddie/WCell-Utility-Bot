using System;
using System.Collections.Generic;
using System.IO;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.FunCommands
{
    class ChuckNorris
    {
        public class AddChuckNorrisFactCommand : Command
        {
            public AddChuckNorrisFactCommand()
                : base("ac", "addchuck", "addnorris")
            {
                Usage = "ac Norris fact here.";
                Description = "Add a chuck norris fact to storage";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    using (var norris = new StreamWriter("ChuckNorrisFacts.txt", true))
                    {
                        norris.WriteLine(trigger.Args.Remainder);
                        trigger.Reply("Added the new Chuck Norris fact: {0} to storage", trigger.Args.Remainder);
                    }
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
        public class RandomChuckNorrisFactCommand : Command
        {
            public RandomChuckNorrisFactCommand()
                : base("rc", "chuck", "norris")
            {
                Usage = "rc";
                Description = "Get a random fact about Chuck Norris";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var norrisLines = new List<string>();
                    var norris = new StreamReader("ChuckNorrisFacts.txt");
                    while (!norris.EndOfStream)
                    {
                        norrisLines.Add(norris.ReadLine());
                    }
                    var rand = new Random();
                    int randnum = rand.Next(0, norrisLines.Count - 1);
                    trigger.Reply(norrisLines[randnum]);
                    norris.Close();
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }
    }
}
