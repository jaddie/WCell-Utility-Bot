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
        public List<string> Read(string dump, string query,string filterterms)
        {
            Readresults.Clear();
            Dumptype = dump;
            string []terms = { "" };
            if (filterterms.Contains(","))
            {
                terms = filterterms.Split(',');
            }
            else
            {
                terms = new string[] { filterterms } ;
            }
            using (var dumpreader = new StreamReader(Dumptype))
            {
                while (!dumpreader.EndOfStream)
                {
                    var currentline = dumpreader.ReadLine();
                        if (currentline.ToLower().Contains(query.ToLower()))
                        {
                            int runs = 0;
                            foreach(var term in terms)
                            {
                                runs++;
                                if (!currentline.ToLower().Contains(term.ToLower()))
                                    break;
                                else
                                {
                                    if(runs == terms.Length)
                                    Readresults.Add(currentline);
                                }
                            }
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
                    if(currentline == Readresults[queryid])
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
