using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Jad_Bot
{
    public class DumpReader
    {
        public List<string> readresults = new List<string>();
        public List<string> selectresults = new List<string>();
        public string dumptype;
        public List<string> Read(string dump, string query)
        {
            readresults.Clear();
            dumptype = dump;
            var dumpreader = new StreamReader(dumptype);
            while (!dumpreader.EndOfStream)
            {
                var currentline = dumpreader.ReadLine().ToLower();
                if (currentline.Contains(query.ToLower()))
                {
                    readresults.Add(currentline);
                }
            }
            return readresults;
        }
        public List<string> Select(int queryid)
        {
            selectresults.Clear();
            var dumpreader = new StreamReader(dumptype);
            var currentline = dumpreader.ReadLine();
            var queryidfromlist = readresults[queryid].ToString();
            Console.WriteLine(queryidfromlist);
            while (!dumpreader.EndOfStream)
            {
                currentline = dumpreader.ReadLine();
                if(currentline.ToLower() == readresults[queryid].ToString())
                {
                    while (!currentline.Contains("#####"))
                    {
                        selectresults.Add(currentline);
                        currentline = dumpreader.ReadLine();
                    }
                }
            }
            return selectresults;
        }
    }
}
