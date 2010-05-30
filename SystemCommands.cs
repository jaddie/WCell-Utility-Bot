using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using Squishy.Irc.Commands;

namespace Jad_Bot
{
    class SystemCommands
    {
        #region Nested type: RuntimeCommand

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
        #region Nested type: RestartWcellCommand

        public class RestartWcellCommand : Command
        {
            public RestartWcellCommand()
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
                                WorkingDirectory = @"c:\realmserver\"
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
                                    @"c:\run\authserver\wcell.authserverconsole.exe",
                                WorkingDirectory = @"c:\authserver",
                                UseShellExecute = true
                            }
                        };
                        wcellRealmserver.StartInfo.UseShellExecute = true;
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
                        Environment.Exit(0);
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
                        authServer = System.Diagnostics.Process.GetProcessesByName("WCell.AuthServerConsole");
                        if (authServer.Length > 0)
                            trigger.Reply("AuthServer Seems to be Online again!");
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
                }
                catch (Exception e)
                {
                    JadBot.Print(e.Data + e.StackTrace, true);
                }
            }
        }

        #endregion

    }
}
