using System.Collections.Generic;
using System.IO;
using System;
namespace Jad_Bot.Utilities
{
    static class WriteErrorSystem
    {
        public static void WriteError(List<string> error = null, string errorline = null)
        {
            try
            {
                using (var writer = new StreamWriter("ErrorLog.txt", true))
                {
                    writer.AutoFlush = true;
                    if (error != null)
                    {
                        foreach (var line in error)
                        {
                            Console.WriteLine(line);
                            writer.WriteLine(line);
                        }
                    }
                    else
                    {
                        if (errorline != null)
                        {
                            writer.WriteLine(errorline);
                            Console.WriteLine(errorline);
                        }
                    }
                    return;
                }
            }
            catch
            {
                return;
            }
        }
    }
}
