using System.IO;
using System;

namespace Jad_Bot.Utilities
{
    static class WriteErrorSystem
    {
        public static System.Timers.Timer ErrorLinkTimer = new System.Timers.Timer(10000);
        public static void WriteError(Exception exception)
        {
            try
            {
                if (exception == null)
                    return;
                JadBot.Exceptions.Add(exception);
                if (!ErrorLinkTimer.Enabled)
                {
                    ErrorLinkTimer.Start();
                    ErrorLinkTimer.Enabled = true;
                    Console.WriteLine(DateTime.Now + "An exception occured");
                    JadBot.Irc.CommandHandler.Msg("#wcell", "An Exception occured, use @le to list available exceptions and @se to select.");
                }
                using (var writer = new StreamWriter("ErrorLog.txt", true))
                {
                    writer.AutoFlush = true;
                    var e = DateTime.Now + exception.Message + "\n" + exception.Data + "\n" + exception.InnerException + "\n" + exception.Source + "\n" + exception.StackTrace + "\n";
                    writer.WriteLine(e);
                }
            }
            catch
            {
                return;
            }
        }
    }
}
