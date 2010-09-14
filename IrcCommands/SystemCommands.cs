using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;
using Enumerable = System.Linq.Enumerable;
using Queryable = System.Linq.Queryable;

namespace Jad_Bot.IrcCommands
{
	class SystemCommands
	{
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
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class RestartCommand : Command
		{
			public RestartCommand()
				: base("Restart")
			{
				Usage = "Restart Safe or Restart Utility";
				Description =
					"Command which will shutdown the WCell server or Utility Bot depending on what you set, use Kill to instant kill the server, use Safe to close with saving data and safely etc";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					string nextWord = trigger.Args.NextWord().ToLower();
					if (nextWord == "safe")
					{
						trigger.Reply("Safely shutting down WCell saving data and Starting it up again");
						var prog = new Process
						{
							StartInfo = { FileName = @"c:\wcellupdater.exe", Arguments = @"c:\wcellsource\run\ c:\run\ debug\" }
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
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class SetSendQueue : Command
		{
			public SetSendQueue()
				: base("setsendqueue")
			{
				Usage = "setsendqueue number";
				Description = "Sets the throttle to number";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					var sendqueue = trigger.Args.NextInt(60);
					if (sendqueue > 75)
					{
						trigger.Reply("Error invalid value!");
							return;
					}
					JadBot.SendQueue = sendqueue;
					trigger.Reply("Set sendqueue to " + sendqueue);
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
	}
}
