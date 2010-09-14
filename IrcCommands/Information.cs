using System;
using Jad_Bot.Utilities;
using Squishy.Irc.Commands;

namespace Jad_Bot.IrcCommands
{
	class Information
	{
		public class WCellLogsCommand : Command
		{
			public WCellLogsCommand()
				: base("WCellLogs", "logs")
			{
				Usage = "wcelllogs";
				Description = "Show the link to the logs from the AutoDeployServer";
			}

			public override void Process(CmdTrigger trigger)
			{
				trigger.Reply("The WCell Test Server Logs are at: " + JadBot.WebLinkToLogsFolder);
			}
		}
		public class UnparsedLogsCommand : Command
		{
			public UnparsedLogsCommand()
				: base("unparsed")
			{
				Usage = "unparsed";
				Description = "Replies with the link to the folder with the unparsed logs in.";
			}

			public override void Process(CmdTrigger trigger)
			{
				trigger.Reply(JadBot.WebLinkToUnparsedFolder);
			}
		}
		public class UploadCommand : Command
		{
			public UploadCommand()
				: base("Upload")
			{
				Usage = "Upload";
				Description =
					"Replies with the address to use for uploading unparsed logs, and downloading parsed logs.";
			}

			public override void Process(CmdTrigger trigger)
			{
				trigger.Reply("Please use {0} for uploads such as unparsed logs you wish to parse with this bot",
							  JadBot.UploadSite);
			}
		}
		public class ParsedLogsCommand : Command
		{
			public ParsedLogsCommand()
				: base("parsed")
			{
				Usage = "parsed";
				Description = "Replies with the link to the folder with parsed logs in.";
			}

			public override void Process(CmdTrigger trigger)
			{
				trigger.Reply(JadBot.WebLinkToParsedFolder);
			}
		}
		public class GeneralFilesCommand : Command
		{
			public GeneralFilesCommand()
				: base("general")
			{
				Usage = "general";
				Description = "Replies with link to the folder with general files in.";
			}

			public override void Process(CmdTrigger trigger)
			{
				trigger.Reply(JadBot.WebLinkToGeneralFolder);
			}
		}
		public class AttackCommand : Command
		{
			public AttackCommand()
				: base("attack")
			{
				Usage = "attack nick";
				Description = "Attack the given user";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					string[] attacks = {
										   "slaps %s",
										   "kicks %s",
										   "punches %s in the face",
										   "stings %s with a needle",
										   "shoots %s with a phaser"
									   };
					if (!string.IsNullOrEmpty(trigger.Args.Remainder))
					{
						var rand = new Random();
						int randomchoice = rand.Next(0, 4);
						if (trigger.Channel != null && !trigger.Channel.HasUser(trigger.Args.Remainder))
						{
							trigger.Reply("I can't find that person to attack them!");
							return;
						}
						string attack = attacks[randomchoice].Replace("%s", trigger.Args.Remainder);
						JadBot.Irc.CommandHandler.Describe(trigger.Target, attack, trigger.Args);
					}
					else
					{
						trigger.Reply("You didnt give me a nick to attack!");
					}
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class GitRepoCommand : Command
		{
			public GitRepoCommand() : base("git","repo")
			{
				Usage = "git";
				Description = "Display git repo info";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("You can find WCell's GIT repository at: http://github.com/WCell/WCell");  //TODO: Add link to wiki
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class WCellTerrainRepoCommand : Command
		{
			public WCellTerrainRepoCommand()
				: base("wcellterrain")
			{
				Usage = "wcellterrain";
				Description = "Replies with the link to the wcell terrain repo";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("WCell Terrain Git Repo: http://github.com/WCell/WCell-Terrain");
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class DBCommand : Command
		{
			public DBCommand()
				: base("db")
			{
				Usage = "db";
				Description = "Shows the wiki link for supported databases";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("WCell supports the following database projects: http://www.wcell.org/Wiki/index.php?title=Supported_Databases");
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class WCellDumpsCommand : Command
		{
			public WCellDumpsCommand()
				: base("dumps", "wcelldumps")
			{
				Usage = "wcelldumps";
				Description = "Provides link to dumps folder";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("WCell Test Server Dumps Are At: http://wcell.org/Dumps");
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class WikiCommand : Command
		{
			public WikiCommand()
				: base("wiki")
			{
				Usage = "wiki";
				Description = "Displays the link for the wcell wiki page";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("The WCell Wiki is located at: http://wiki.wcell.org");
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class Express2010AdvancedCommand : Command
		{
			public Express2010AdvancedCommand() : base("express")
			{
				Usage = "express";
				Description = "Shows the info to make 2010 express into a usable IDE";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("Click tools -> settings -> Expert Settings -- This should provide you with most options of the bigger versions of VS so you can actually get something done, including the break button!");
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class Pastebin : Command
		{
			public Pastebin()
				: base("paste", "pastebin")
			{
				Usage = "pastebin";
				Description = "returns link to pastebin";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					trigger.Reply("If you have more than 2 lines to show us, please paste them at http://wcell.pastebin.com/ and show us the link.");
				}
				catch (Exception e)
				{
					WriteErrorSystem.WriteError(e);
				}
			}
		}
		public class MSDNSearch : Command
		{
			public MSDNSearch() : base("msdn")
			{
				Usage = "msdn search query";
				Description = "Provide a link to an msdn search for your query";
			}

			public override void Process(CmdTrigger trigger)
			{
				try
				{
					var query = trigger.Args.Remainder.Replace(" ", "%20");
					trigger.Reply("Link to MSDN Search: http://social.msdn.microsoft.com/Search/en-gb?query=" + query);
				}
				catch (Exception)
				{
					trigger.Reply("I failed :P");
				}
			}
		}
        public class TrackerCommand : Command
        {
            public TrackerCommand() : base("trac","tracker","track","bugs","issues","report","tickets")
            {
                Usage = "tracker";
                Description = "Shows the link to teh tracker for git browsing, and bug reports etc.";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("http://tracker.wcell.org/wcellmaster");
				}
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(e);
                }
            }
        }
        public class APICommand : Command
        {
            public APICommand() : base("api")
            {
                Usage = "api";
                Description = "Shows link to wiki link for api";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("http://wiki.wcell.org/index.php/WCell_API");
                }
                catch (Exception)
                {
                    //None
                }
            }
        }
        public class WCellBuildServerCommand : Command
        {
            public WCellBuildServerCommand() : base("buildserver","bs","buildsystem","buildsite")
            {
                Usage = "buildserver";
                Description = "Shows the link to the WCell Build Server";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    trigger.Reply("http://server.wcell.org");
				}
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(new Exception("Failed to send build server link!" + e.Data + e.Message));
                }
            }
        }
        
        public class ListExceptionsCommand : Command
        {
            public ListExceptionsCommand()
                : base("listexceptions","le")
            {
                Usage = "listexceptions";
                Description = "Lists all stored (in memory) exceptions";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    if (JadBot.Exceptions.Count > 0)
                    {
                        var id = 0;
                        foreach (var exception in JadBot.Exceptions)
                        {
                            trigger.Reply(id + ": " + exception.Message);
                            id++;
                        }
                        trigger.Reply("End of list");
                    }
                    else
                    {
                        trigger.Reply("No available Exceptions!");
                    }
                }
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(e);
                }
            }
        }
        
        public class ShowExceptionCommand : Command
        {
            public ShowExceptionCommand()
                : base("showexception","se")
            {
                Usage = "excep s id";
                Description = "Display exception stored at id";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    if (JadBot.Exceptions.Count > 0)
                    {
                        var exception = JadBot.Exceptions[trigger.Args.NextInt(0)];
                        trigger.Reply(exception.Message + exception.InnerException + exception.Source +
                                      exception.StackTrace + exception.TargetSite);
                        trigger.Reply("Finished Output");
                    }
                    else
                    {
                        trigger.Reply("No available Exceptions!");
                    }
                }
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(e);
                }
            }
        }
        
        public class ClearExceptionsCommand : Command
        {
            public ClearExceptionsCommand()
                : base("clearexceptions","ce")
            {
                Usage = "clearexceptions";
                Description = "Wipes the list of exceptions";
            }

            public override void Process(CmdTrigger trigger)
            {
                try
                {
                    JadBot.Exceptions.Clear();
                    JadBot.Exceptions.TrimExcess();
                    trigger.Reply("Cleared all stored exceptions.");
                }
                catch (Exception e)
                {
                    WriteErrorSystem.WriteError(e);
                }
            }
        }
			
						
	}
}
