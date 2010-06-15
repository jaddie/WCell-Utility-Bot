using System;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.FunCommands
{
    class FunCommands
    {
        #region StealObjectCommand

        public class StealObjectCommand : Command
        {
            public StealObjectCommand()
                : base("Steal", "StealObject")
            {
                Usage = "steal name of person to steal object from";
                Description =
                    "Steals the provided person's cookie and munches it, unless you specify a different object";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var objectowner = trigger.Args.Remainder;
                    var obj = "cookie";
                    if (trigger.Args.NextModifiers() == "object")
                    {
                        obj = trigger.Args.NextWord();
                    }
                    string target = trigger.Target.ToString();
                    if (trigger.Args.NextModifiers() == "target")
                    {
                        target = trigger.Args.NextWord();
                    }
                    if (String.IsNullOrEmpty(objectowner))
                    {
                        trigger.Reply("You didn't tell me who to steal from!");
                    }
                    if (obj.ToLower() == "cookie")
                    {
                        JadBot.Irc.CommandHandler.Describe(target,
                                                    String.Format("steals {0}'s cookie, om nom nom nom!", objectowner),
                                                    trigger.Args);
                    }
                    else
                    {
                        JadBot.Irc.CommandHandler.Describe(target,
                                                    String.Format("steals {0}'s {1}, ha! Pwned", trigger.Args.Remainder,
                                                                  obj), trigger.Args);
                    }
                }
                catch (Exception e)
                {
                    trigger.Reply("I cant steal their cookie :'(");
                    trigger.Reply(e.Message);
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region PizzaCommand

        public class PizzaCommand : Command
        {
            public PizzaCommand()
                : base("pizza")
            {
                Usage = "pizza nickname";
                Description = "Steal the person's pizza.";
            }

            public override void Process(CmdTrigger trigger)
            {
                if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
                {
                    trigger.Reply("I can't find that person to eat their pizza!");
                    return;
                }
                trigger.Irc.CommandHandler.Describe(trigger.Target,
                                                    String.Format("steals {0}'s pizza and eats it nomnomnom!",
                                                                  trigger.Args.Remainder), trigger.Args);
            }
        }

        #endregion
        #region EightBallCommand

        public class EightBallCommand : Command
        {
            public EightBallCommand()
                : base("eightball", "eight", "eb")
            {
                Usage = "eightball DecisionQuestion";
                Description = "Provide an answer to decision";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string[] eightballanswers = {
                                                    "As I see it, yes",
                                                    "Ask again later",
                                                    "Better not tell you now",
                                                    "Cannot predict now",
                                                    "Concentrate and ask again",
                                                    "Don't count on it",
                                                    "It is certain",
                                                    "It is decidedly so",
                                                    "Most likely",
                                                    "My reply is no",
                                                    "My sources say no",
                                                    "Outlook good",
                                                    "Outlook not so good",
                                                    "Reply hazy, try again",
                                                    "Signs point to yes",
                                                    "Very doubtful",
                                                    "Without a doubt",
                                                    "Yes",
                                                    "Yes - definitely",
                                                    "You may rely on it"
                                                };
                    if (!String.IsNullOrEmpty(trigger.Args.Remainder))
                    {
                        var rand = new Random();
                        var randomchoice = rand.Next(0, 19);
                        trigger.Reply(eightballanswers[randomchoice]);
                    }
                    else
                    {
                        trigger.Reply("You didnt give me a decision question!");
                    }
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region BeerCommand

        public class BeerCommand : Command
        {
            public BeerCommand()
                : base("beer")
            {
                Usage = "beer nickname";
                Description = "Steal the person's beer.";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
                    {
                        trigger.Reply("I can't find that person to drink their beer!");
                        return;
                    }
                    trigger.Irc.CommandHandler.Describe(trigger.Target,
                                                        String.Format("steals {0}'s beer and gulps it all down *slurp*!",
                                                                      trigger.Args.Remainder), trigger.Args);
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region ActionCommand

        public class ActionCommand : Command
        {
            public ActionCommand()
                : base("Action", "Me")
            {
                Usage = "action -target destination action to write";
                Description = "Writes the provided Action.";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string target = trigger.Target.ToString();
                    if (trigger.Args.NextModifiers() == "target")
                    {
                        target = trigger.Args.NextWord();
                    }
                    JadBot.Irc.CommandHandler.Describe(target, trigger.Args.Remainder, trigger.Args);
                }
                catch (Exception e)
                {
                    trigger.Reply("I cant write that action, perhaps invalid target?");
                    trigger.Reply(e.Message);
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion

        public static string ReactToAction()
        {
            try
            {
                string[] actions = {
                                       "dodges",
                                       "ducks",
                                       "evades",
                                       "parries",
                                       "blocks",
                                       "does the monkey dance"
                                   };
                var rand = new Random();
                var randomchoice = rand.Next(0, 5);
                return actions[randomchoice];
            }
            catch(Exception e)
            {
                UtilityMethods.Print(e.Data + e.StackTrace,true);
                return "";
            }
        }
    }
}
