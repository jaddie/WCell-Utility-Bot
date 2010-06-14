using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class SystemCommands
    {
        #region RuntimeCommand

        public class RuntimeCommand : Command
        {
            public RuntimeCommand()
                : base("Runtime")
            {
                Usage = "runtime";
                Description = "Show how long the program has been running.";
            }

            public override void Process(CmdTrigger trigger)
            {
                trigger.Reply("This UtilityBot has been running for: " + (int)JadBot.Runtimer.Elapsed.TotalMinutes +
                              " minutes, oh yeah I'm a good bot!");
            }
        }

        #endregion
        #region ClearQueueCommand

        public class ClearQueueCommand : Command
        {
            public ClearQueueCommand()
                : base("ClearQueue", "CQ")
            {
                Usage = "ClearSendQueue";
                Description = "Command to clear the send queue. Useful if you want the bot to stop spamming";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    var lines = trigger.Irc.Client.SendQueue.Length;
                    trigger.Irc.Client.SendQueue.Clear();
                    trigger.Reply("Cleared SendQueue of {0} lines", lines);
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion
        #region RestartCommand

        public class RestartCommand : Command
        {
            public RestartCommand()
                : base("Restart")
            {
                Usage = "Restart Kill or Restart Safe or Restart Utility";
                Description =
                    "Command which will shutdown the WCell server or Utility Bot depending on what you set, use Kill to instant kill the server, use Safe to close with saving data and safely etc";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    string nextWord = trigger.Args.NextWord().ToLower();
                    if (nextWord == "kill")
                    {
                        trigger.Reply("Killing WCell and restarting it");
                        Process[] killWCell = System.Diagnostics.Process.GetProcessesByName("wcell.realmserverconsole");
                        foreach (var p in killWCell)
                        {
                            p.Kill();
                        }
                        var wcellRealmserver = new Process
                                                   {
                                                       StartInfo =
                                                           {
                                                               FileName = @"c:\run\debug\wcell.realmserverconsole.exe",
                                                               WorkingDirectory = @"c:\realmserver\",
                                                               UseShellExecute = true
                                                           }
                                                   };
                        wcellRealmserver.Start();
                        Process[] killauth = System.Diagnostics.Process.GetProcessesByName("wcell.authserverconsole");
                        foreach (var p in killauth)
                        {
                            p.Kill();
                        }
                        var wCellAuthserver = new Process
                        {
                            StartInfo =
                            {
                                FileName =
                                    @"c:\run\debug\authserver\wcell.authserverconsole.exe",
                                WorkingDirectory = @"c:\run\debug\authserver",
                                UseShellExecute = true
                            }
                        };
                        wCellAuthserver.Start();
                        wcellRealmserver.Start();
                    }
                    if (nextWord == "safe")
                    {
                        trigger.Reply("Safely shutting down WCell saving data and Starting it up again");
                        var prog = new Process
                        {
                            StartInfo = { FileName = @"c:\program_launcher.exe", Arguments = @"c:\config.txt" }
                        };
                        prog.Start();
                    }
                    if (nextWord == "utility")
                    {
                        trigger.Reply("Restarting Utility - If there is no auto restarter the utility will not return");
                        JadBot.Parser.Kill();
                        var restartbat = new StreamWriter("Restartbat.bat") {AutoFlush = true};
                        restartbat.WriteLine("taskkill /F /IM jad_bot.exe");
                        restartbat.WriteLine("start " + JadBot.Utility.StartInfo.FileName);
                        var cmd = new Process {StartInfo = {FileName = "cmd.exe", Arguments = "start RestartBat.bat"}};
                        cmd.Start();
                    }
                    if (nextWord == "authserver")
                    {
                        Process[] authServer = System.Diagnostics.Process.GetProcessesByName("WCell.AuthServerConsole");
                        foreach (var process in authServer)
                        {
                            trigger.Reply("Attempting to restart AuthServer");
                            process.Kill();
                            var wCellStarter = new Process
                            {
                                StartInfo =
                                {
                                    FileName = @"c:\run\debug\wcell.authserverconsole.exe",
                                    UseShellExecute = true
                                }
                            };
                            wCellStarter.Start();
                        }
                        Thread.Sleep(3000);
                    }
                    if (nextWord != "realmserver") return;
                    Process[] realmServer = System.Diagnostics.Process.GetProcessesByName("WCell.RealmServerConsole");
                    foreach (var process in realmServer)
                    {
                        trigger.Reply("Attempting to restart RealmServer");
                        process.Kill();
                        var wcellRealmserver = new Process
                        {
                            StartInfo =
                            {
                                FileName = @"c:\run\debug\wcell.realmserverconsole.exe",
                                WorkingDirectory = @"c:\run\debug\",
                                UseShellExecute = true
                            }
                        };
                        wcellRealmserver.Start();
                    }
                    Thread.Sleep(3000);
                    realmServer = System.Diagnostics.Process.GetProcessesByName("WCell.RealmServerConsole");
                    if (realmServer.Length > 0)
                        trigger.Reply("RealmServer Seems to be Online again!");
                    var authConsoleServer = System.Diagnostics.Process.GetProcessesByName("WCell.AuthServerConsole");
                    if (authConsoleServer.Length > 0)
                        trigger.Reply("AuthServer Seems to be Online again!");
                }
                catch (Exception e)
                {
                    UtilityMethods.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion

    }
}
