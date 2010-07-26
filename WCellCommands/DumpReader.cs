using System;
using System.Collections.Generic;
using System.IO;

namespace Jad_Bot.WCellCommands
{
    public class DumpReader
    {
        public List<string> Readresults = new List<string>();
        public readonly List<string> Selectresults = new List<string>();
        public string Dumptype;
        public IEnumerable<string> Read(string dump, string query,string filterterms)
        {
            Readresults.Clear();
            Dumptype = dump;
            string []terms = { "" };
            if (filterterms.Contains(","))
            {
                terms = filterterms.Split(',');
            }
            using (var dumpreader = new StreamReader(Dumptype))
            {
                while (!dumpreader.EndOfStream)
                {
                    var currentline = dumpreader.ReadLine().ToLower();
                        if (currentline.Contains(query.ToLower()))
                        {
                            foreach(var term in terms)
                            {
                                if (!currentline.Contains(term))
                                    break;
                            }
                                Readresults.Add(currentline);
                    }
                }
            }
            return Readresults;
        }
        public List<string> Select(int queryid)
        {
            Selectresults.Clear();
            using (var dumpreader = new StreamReader(Dumptype))
            {
                string currentline;
                var queryidfromlist = Readresults[queryid];
                Console.WriteLine(queryidfromlist);
                while (!dumpreader.EndOfStream)
                {
                    currentline = dumpreader.ReadLine();
                    if(currentline.ToLower() == Readresults[queryid])
                    {
                        while (!currentline.Contains("#####"))
                        {
                            Selectresults.Add(currentline);
                            currentline = dumpreader.ReadLine();
                        }
                    }
                }
            }
            return Selectresults;
        }
    }
}
