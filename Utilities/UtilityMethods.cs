using System;
using System.Text;
using System.Threading;
using Squishy.Network;
using System.Collections.Generic;
namespace Jad_Bot.Utilities
{
    public class UtilityMethods
    {
        #region RandomLinkGeneration

        public static string GetLink()
        {
            try
            {
                var builder = new StringBuilder();
                builder.Append(RandomString(4, true));
                builder.Append(RandomNumber(1000, 999999));
                builder.Append(RandomString(2, true));
                return builder.ToString();
            }
            catch (Exception e)
            {
                WriteErrorSystem.WriteError(e);
            }
            return null;
        }

        public static string RandomString(int size, bool lowerCase)
        {
            try
            {
                var builder = new StringBuilder();
                var random = new Random();
                for (var i = 0; i < size; i++)
                {
                    var ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                    builder.Append(ch);
                }
                return lowerCase ? builder.ToString().ToLower() : builder.ToString();
            }
            catch (Exception e)
            {
                WriteErrorSystem.WriteError(e);
                return "";
            }
        }

        public static int RandomNumber(int min, int max)
        {
            try
            {
                var random = new Random();
                return random.Next(min, max);
            }
            catch (Exception e)
            {
                WriteErrorSystem.WriteError(e);
            }
            return 0;
        }

        #endregion

        public static void OnConsoleText(StringStream cText)
        {
            try
            {
                switch (cText.NextWord().ToLower())
                {
                    case "join":
                        {
                            if (cText.Remainder.Contains(","))
                            {
                                var chaninfo = cText.Remainder.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
                                if (chaninfo.Length > 1)
                                    JadBot.Irc.CommandHandler.Join(chaninfo[0], chaninfo[1]);
                                else
                                    JadBot.Irc.CommandHandler.Join(chaninfo[0]);
                            }
                            else
                            {
                                JadBot.Irc.CommandHandler.Join(cText.Remainder);
                            }
                        }
                        break;
                    case "say":
                        {
                            var chan = cText.NextWord();
                            var msg = cText.Remainder;
                            JadBot.Irc.CommandHandler.Msg(chan, msg);
                        }
                        break;
                    case "quit":
                        {
                            JadBot.Parser.Kill();
                            JadBot.IrcLog.WriteLine("Shutting down due to console quit command..");
                            foreach (var chan in JadBot.ChannelList)
                            {
                                JadBot.Irc.CommandHandler.Msg(chan, "Shutting down in 5 seconds due to console quit command..");
                            }
                            Thread.Sleep(5000);
                            JadBot.Irc.Client.DisconnectNow();
                            Environment.Exit(0);
                        }
                        break;
                }
            }
            catch(Exception e)
            {
                WriteErrorSystem.WriteError(e);
            }
        }
    }
}
