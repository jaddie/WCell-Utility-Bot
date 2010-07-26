using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Jad_Bot.Utilities;
using Squishy.Irc.Account;
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
					UtilityMethods.Print(e.Data + e.StackTrace, true);
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
					UtilityMethods.Print(e.Data + e.StackTrace, true);
				}
			}
		}
		public class LoginCommand : Command
		{
			public LoginCommand()
				: base("Login")
			{
				Description = "login to your account";
				Usage = "login accname pw";
			}

			public override void Process(CmdTrigger trigger)
			{
				var username = trigger.Args.NextWord();
				var password = trigger.Args.NextWord();
				if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
				{
					trigger.Reply("Error invalid input please try again!");
					return;
				}
				Login(trigger, username, password);
			}
			public static void Login(CmdTrigger trigger, string username, string password)
			{
				using (var accounts = new AccountsContainer())
				{
					var authed = false;
					foreach (var account in accounts.Accounts)
					{
						if(account.Username == username && account.Password == password)
						{
							switch (account.UserLevel)
							{
								case "guest":
									{
										authed = true;
										trigger.User.SetAccountLevel(AccountMgr.AccountLevel.Guest);
										trigger.Reply(string.Format("Logged in as {0} with level {1}", account.Username, account.UserLevel));
									}
									break;
								case "user":
									{
										authed = true;
										trigger.User.SetAccountLevel(AccountMgr.AccountLevel.User);
										trigger.Reply(string.Format("Logged in as {0} with level {1}", account.Username, account.UserLevel));
									}
									break;
								case "admin":
									{
										authed = true;
										trigger.User.SetAccountLevel(AccountMgr.AccountLevel.Admin);
										trigger.Reply(string.Format("Logged in as {0} with level {1}", account.Username, account.UserLevel));
									}
									break;
							}
						}
					}
					if (!authed)
						trigger.Reply("Account data invalid! Please try again!");
				}
			}
		}
		public class CreateAccountCommand : Command
		{
			public CreateAccountCommand()
				: base("createaccount", "ca")
			{
				Description = "Create a account";
				Usage = "createaccount accname pw role";
			}

			public override void Process(CmdTrigger trigger)
			{
				var username = trigger.Args.NextWord();
				var password = trigger.Args.NextWord();
				var userlevel = trigger.Args.NextWord();
				if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
				{
					trigger.Reply("Error invalid input please try again!");
				}
				if (string.IsNullOrEmpty(userlevel))
				{
					trigger.Reply("Role not specified, defaulting to user level.");
				}
				else
				{
					using (var accounts = new AccountsContainer())
					{
						if (Enumerable.Any(Queryable.Where(accounts.Accounts, account => account.Username == username)))
						{
							trigger.Reply("That account already exists!");
							return;
						}
					}
					AddAccount(trigger, username, password, userlevel);
					trigger.Reply("Account created");
				}
			}
			public static void AddAccount(CmdTrigger trigger, string username, string password, string userlevel)
			{
				using (var accounts = new AccountsContainer())
				{
					var account = new Account { Username = username, Password = password, UserLevel = userlevel };
					accounts.Accounts.AddObject(account);
					accounts.SaveChanges();
				}
			}
		}
		public class DeleteAccountCommand : Command
		{
			public DeleteAccountCommand()
				: base("deleteaccount", "da")
			{
				Usage = "deleteaccount username";
				Description = "Removes the account as specified";
				RequiredAccountLevel = AccountMgr.AccountLevel.Admin;
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					var username = trigger.Args.NextWord();
					if (string.IsNullOrEmpty(username))
					{
						trigger.Reply("Please specify username!");
					}
					else
					{
						using (var accounts = new AccountsContainer())
						{
							foreach (var account in Queryable.Where(accounts.Accounts,account => account.Username == username))
							{
								accounts.DeleteObject(account);
								accounts.SaveChanges();
								trigger.Reply("Account deleted!");
								return;
							}
							trigger.Reply("Account not found!");
						}
					}
				}
				catch (Exception e)
				{
					UtilityMethods.Print(e.Message + e.Data + e.StackTrace, true);
				}
			}
		}
		public class ChangeUserLevelCommand : Command
		{
			public ChangeUserLevelCommand()
				: base("ChangeUserLevel")
			{
				Description = "Changes the user level of a user";
				Usage = "ChangeUserLevel nick userlevel";
				RequiredAccountLevel = AccountMgr.AccountLevel.Admin;
			}

			public override void Process(CmdTrigger trigger)
			{
				var username = trigger.Args.NextWord();
				var userlevel = trigger.Args.NextWord().ToLower();
				if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(userlevel))
				{
					trigger.Reply("Invalid input please try again!");
				}
				else
				{
					if (!ValidateUserLevel(userlevel))
					{
						trigger.Reply("Invalid userlevel specified, options are guest,user,admin");
						return;
					}
					using (var accounts = new AccountsContainer())
					{
						foreach (var account in Queryable.Where(accounts.Accounts,account => account.Username == username))
						{
							account.UserLevel = userlevel;
							trigger.Reply("Account level changed to " + userlevel);
							accounts.SaveChanges();
							return;
						}
					}
				}
			}
		}
		public static bool ValidateUserLevel(string userlevel)
		{
			if ("admin user guest".Contains(userlevel.ToLower()))
			{
				return true;
			}
			return false;
		}
		public class SetSendQueue : Command
		{
			public SetSendQueue()
				: base("setsendqueue")
			{
				Usage = "setsendqueue number";
				Description = "Sets the throttle to number";
				RequiredAccountLevel = AccountMgr.AccountLevel.Admin;
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					var sendqueue = trigger.Args.NextInt(60);
					JadBot.SendQueue = sendqueue;
					trigger.Reply("Set sendqueue to " + sendqueue);
				}
				catch (Exception e)
				{
					UtilityMethods.Print(e.Message + e.Data + e.StackTrace, true);
				}
			}
		}
	}
}
