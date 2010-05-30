using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class SourceCode
    {
        #region Nested type: ReadSourceFile

        public class ReadSourceFile : Command
        {
            private readonly List<FileInfo> _files = new List<FileInfo>();

            public ReadSourceFile()
                : base("find", "RS", "grep")
            {
                Usage =
                    "rs -i iftherearemorethan1filesbeforefileidhere -l lowerline-upperline -includepath spaceseperated search terms notcase sensitive";
                Description =
                    "Allows you to search through the source code of WCell and show the specified lines from found files.";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("Scanning source for matches, please allow some time...");
                    var sourceDir = new DirectoryInfo(@"c:\wcellsource");
                    int linenumber = 0;
                    int upperlinenumber = 0;
                    int fileid = 0;
                    bool includefullpath = false;
                    bool fileidgiven = false;
                    if (trigger.Args.String.Contains("-i"))
                    {
                        if (trigger.Args.NextModifiers() == "i")
                        {
                            fileidgiven = true;
                            fileid = trigger.Args.NextInt(0);
                        }
                    }
                    if (trigger.Args.String.Contains("-l"))
                    {
                        if (trigger.Args.NextModifiers() == "l")
                        {
                            linenumber = trigger.Args.NextInt(0, "-");
                            upperlinenumber = trigger.Args.NextInt(0);
                        }
                    }
                    if (trigger.Args.String.Contains("-includepath"))
                    {
                        if (trigger.Args.NextModifiers() == "includepath")
                        {
                            includefullpath = true;
                        }
                    }
                    var searchterms = new List<string>();
                    while (trigger.Args.HasNext)
                    {
                        string searchterm = trigger.Args.NextWord().Trim();
                        searchterms.Add(searchterm);
                    }
                    GetFilesNormalName(sourceDir, _files);
                    foreach (var file in _files)
                    {
                        int runs = 0;
                        foreach (var searchterm in searchterms)
                        {
                            string nameTerm = file.Name;
                            if (includefullpath)
                            {
                                nameTerm = file.FullName;
                            }
                            if (nameTerm.ToLower().Contains(searchterm.ToLower()))
                            {
                                runs = runs + 1;
                                if (runs == searchterms.Count)
                                {
                                    JadBot.Matches.Add(file.FullName);
                                }
                            }
                        }
                    }
                    if (JadBot.Matches.Count > 1 && !fileidgiven)
                    {
                        var readWriter = new StreamWriter(JadBot.GeneralFolder + @"\SourceOptions.txt", false) { AutoFlush = false };
                        int i = 0;
                        foreach (var match in JadBot.Matches)
                        {
                            readWriter.WriteLine(i + ": " + match);
                            i = i + 1;
                        }
                        trigger.Reply("There were more than 1 found files, please choose!");
                        string line = "";
                        int id = 0;
                        foreach (var match in JadBot.Matches)
                        {
                            if (id > 5)
                                continue;
                            line = line + "\n" + id + ": " + match;
                            id = id + 1;
                        }
                        trigger.Reply(line);
                        if (JadBot.Matches.Count > 5)
                        {
                            trigger.Reply(
                                "\n There are even more results, check the link or be more specific use same command again but with -i file id at the start.");
                            trigger.Reply(JadBot.WebLinkToGeneralFolder + "SourceOptions.txt");
                        }
                        readWriter.Close();
                    }
                    else
                    {
                        if (JadBot.Matches.Count == 1)
                        {
                            string path = JadBot.Matches[fileid];
                            path = path.Replace("c:\\wcellsource", "Master");
                            path = path.Replace('\\', '-');
                            var selectionWriter = new StreamWriter(JadBot.GeneralFolder + string.Format("\\{0}.html", path)) { AutoFlush = false };
                            string lines = ReadFileLines(JadBot.Matches[0], linenumber, upperlinenumber, fileid);
                            selectionWriter.WriteLine("<html>\n<body>\n<pre>");
                            selectionWriter.WriteLine("Filename: {0}", path);
                            selectionWriter.Write(lines);
                            selectionWriter.WriteLine("</pre>\n</body>\n</html>");
                            trigger.Reply(JadBot.WebLinkToGeneralFolder + "{0}.html", path);
                            selectionWriter.Close();
                        }
                        else
                        {
                            if (!fileidgiven)
                                trigger.Reply("No Results Apparently");
                        }
                    }
                    if (fileidgiven)
                    {
                        int matchesnum = JadBot.Matches.Count - 1;
                        if (fileid > matchesnum)
                        {
                            trigger.Reply(
                                "Invalid Fileid Selection, are you sure there are more than 1 files found? run the query without -i number to check results first.");
                            return;
                        }
                        string path = JadBot.Matches[fileid];
                        path = path.Replace("c:\\wcellsource", "Master");
                        path = path.Replace('\\', '-');
                        using (var selectionWriter = new StreamWriter(JadBot.GeneralFolder + string.Format("\\{0}.html", path)))
                        {
                            string lines = ReadFileLines(JadBot.Matches[fileid], linenumber, upperlinenumber, fileid);
                            selectionWriter.WriteLine("<html>\n<body>\n");
                            selectionWriter.WriteLine("Filename: {0}", path);
                            selectionWriter.Write(lines);
                            selectionWriter.WriteLine("\n</body>\n</html>");
                            trigger.Reply(JadBot.WebLinkToGeneralFolder + "{0}.html", path);
                        }
                    }
                    JadBot.Matches.Clear();
                    _files.Clear();
                    JadBot.FileLines.Clear();
                }
                catch (Exception e)
                {
                    trigger.Reply("Please check your input, error occured: {0}", e.Message);
                    JadBot.Print(e.Data + e.StackTrace, true);
                }
            }

            public static void GetFilesNormalName(DirectoryInfo sourceDir, List<FileInfo> files)
            {
                try
                {
                    foreach (var dir in sourceDir.GetDirectories())
                    {
                        if (dir.Name.Contains(".svn") | dir.Name.Contains(".git") | dir.Name.Contains("obj"))
                        {
                            continue;
                        }
                        files.AddRange(dir.GetFiles().Where(file => file.Extension != ".dll"));
                        GetFilesNormalName(dir, files);
                    }
                }
                catch (Exception e)
                {
                    JadBot.Print(e.Data + e.StackTrace, true);
                }
            }

            public static string HighlightText(string text)
            {
                text = "<span class=\"highlighttext\">" + text + "</span>";
                return text;
            }

            public static string ReadFileLines(string readFile, int readLineLower, int readLineUpper, int fileId)
            {
                try
                {
                    var syn = new SyntaxHighlighter { AddStyleDefinition = true };
                    using (var file = new StreamReader(readFile))
                    {
                        int currentlinenumber = 1;
                        if (readLineUpper == 0 && readLineUpper < readLineLower)
                        {
                            readLineUpper = readLineLower;
                        }
                        string returnlines =
                            "<table border=\"0\"> <style> .highlighttext { BACKGROUND-COLOR:#F4FA58 } </style>";
                        var filelinesids = new List<string>();
                        var filelines = new List<string>();
                        string path = JadBot.Matches[fileId];
                        path = path.Replace("c:\\wcellsource", "Master");
                        path = path.Replace('\\', '-');
                        while (!file.EndOfStream)
                        {
                            string line = file.ReadLine();
                            var fileinfo = new FileInfo(readFile);
                            if (fileinfo.Extension == ".cs")
                                line = syn.Highlight(line);
                            syn.AddStyleDefinition = false;
                            if (currentlinenumber >= readLineLower && currentlinenumber <= readLineUpper &&
                                readLineUpper != 0)
                            {
                                filelinesids.Add(
                                    string.Format("<a name=\"{0}\" href=\"{1}/{2}.html#{0}\">", currentlinenumber,
                                                  JadBot.WebLinkToGeneralFolder, path) + currentlinenumber + "</a>:");
                                filelines.Add(HighlightText(line));
                            }
                            else
                            {
                                filelinesids.Add(
                                    string.Format("<a name=\"{0}\" href=\"{1}/{2}.html#{0}\">", currentlinenumber,
                                                  JadBot.WebLinkToGeneralFolder, path) + currentlinenumber + "</a>:");
                                filelines.Add(line);
                            }
                            currentlinenumber = currentlinenumber + 1;
                        }
                        string[] fileids = { "" };
                        string[] fileLines = { "" };
                        filelinesids.ForEach(filelineid => fileids[0] = fileids[0] + "\n" + filelineid);
                        filelines.ForEach(fileline => fileLines[0] = fileLines[0] + "\n" + fileline);
                        returnlines = returnlines + "\n <tr><td><pre>" + fileids[0] + "</pre></td> <td><pre>" +
                                      fileLines[0] +
                                      "</pre></td></tr>";
                        returnlines = returnlines + "</table>";
                        return returnlines;
                    }
                }
                catch (Exception e)
                {
                    JadBot.Print(e.Data + e.StackTrace, true);
                }
                return null;
            }
        }

        #endregion

    }
}
