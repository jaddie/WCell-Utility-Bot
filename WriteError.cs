using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
namespace Jad_Bot
{
    class WriteErrorSystem
    {
        public static bool WriteError(List<string> error)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(JadBot.GeneralFolder + "ErrorLog.txt",true))
                {
                    writer.AutoFlush = true;
                    foreach (var line in error)
                    {
                        writer.WriteLine(line);
                    }
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
