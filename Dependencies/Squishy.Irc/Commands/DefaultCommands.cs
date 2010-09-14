using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using Squishy.Irc.Dcc;
using Squishy.Irc.Protocol;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// TODO: Use localized strings
	/// The help command is special since it generates output.
	/// This output needs to be shown in the GUI if used from commandline and 
	/// sent to the requester if executed remotely.
	/// </summary>
	public class HelpCommand : Command
	{
		public static int MaxUncompressedCommands = 3;

		public HelpCommand()
			: base("Help", "?")
		{
			Usage = "Help|? [<match>]";
			Description = "Shows an overview over all Commands or -if you specify a <match>- the help for any matching commands.";
		}

		public override void Process(CmdTrigger trigger)
		{
			var match = trigger.Args.NextWord();
			IList<Command> cmds;
			if (match.Length > 0)
			{
				cmds = new List<Command>();
				foreach (var cmd in IrcCommandHandler.List)
				{
					if (cmd.Enabled &&
						trigger.MayTrigger(cmd) &&
						cmd.Aliases.FirstOrDefault(ali => ali.IndexOf(match, StringComparison.InvariantCultureIgnoreCase) != -1) != null)
					{
						cmds.Add(cmd);
					}
				}
				if (cmds.Count == 0)
				{
					trigger.Reply("Could not find a command matching '{0}'", match);
				}
				else
				{
					trigger.Reply("Found {0} matching commands: ", cmds.Count);
				}
			}
			else
			{
				trigger.Reply("Use \"Help <searchterm>\" to receive help on a certain command. - All current commands:");
				cmds = IrcCommandHandler.List.Where(cmd => cmd.Enabled && trigger.MayTrigger(cmd)).ToList();
			}

			var line = "";
			foreach (var cmd in cmds)
			{
				if (cmds.Count <= MaxUncompressedCommands)
				{
					var desc = string.Format("{0} ({1})", cmd.Usage, cmd.Description);
					trigger.Reply(desc);
				}
				else
				{
					var info = cmd.Name;
					info += " (" + cmd.Aliases.ToString(", ") + ")  ";

					if (line.Length + info.Length >= IrcProtocol.MaxLineLength)
					{
						trigger.Reply(line);
						line = "";
					}

					line += info;
				}
			}

			if (line.Length > 0)
			{
				trigger.Reply(line);
			}
		}
	}

	public class VersionCommand : Command
	{
		public VersionCommand()
			: base("Version")
		{
			Usage = "Version";
			Description = "Shows the version of this client.";
		}

		public override void Process(CmdTrigger trigger)
		{
			//trigger.Reply(IrcClient.Version);
			AssemblyName asmName = Assembly.GetAssembly(GetType()).GetName();
			trigger.Reply(asmName.Name + ", v" + asmName.Version);
		}
	}

	public class JoinCommand : Command
	{
		public JoinCommand()
			: base("Join", "J")
		{
			Usage = "Join|J <Channel>";
			Description = "Joins a channel";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Join(trigger.Args.NextWord());
		}

		//public override string[] ExpectedServResponses {
		//    get {
		//        return new string[] {
		//            "JOIN",
		//            "405",				// Too many channels
		//            "471",				// Cannot join channel (+l)
		//            "473",				// Cannot join channel (+i)
		//            "474",				// Cannot join channel (+b)
		//            "475",				// Cannot join channel (+k)
		//            "477",				// need to register
		//            "485"				// Cannot join channel
		//        };
		//    }
		//}
	}

	public class AuthCommand : Command
	{
		public AuthCommand()
			: base("Auth")
		{
			Usage = "Auth";
			Description = "Will query the Authentication data from the server if not already present.";
		}

		public override void Process(CmdTrigger trigger)
		{
			var user = trigger.User;
			if (user == null)
			{
				trigger.Reply("AuthCommand requires User-argument.");
			}
			else
			{
				if (user.IsAuthenticated)
				{
					trigger.Reply("User {0} is authed as: {1}", user.Nick, user.AuthName);
				}
				else
				{
					var authMgr = trigger.irc.AuthMgr;
					if (authMgr.IsResolving(user))
					{
						trigger.Reply("User {0} is being resolved - Please wait...", user.Nick, user.AuthName);
					}
					else if (!authMgr.CanResolve)
					{
						trigger.Reply("Authentication is not supported on this Network.");
					}
					else
					{
						trigger.Reply("Resolving User...".Colorize(IrcColorCode.Red));
						authMgr.ResolveAuth(user);
					}
				}
			}
		}

		//public override string[] ExpectedServResponses {
		//    get {
		//        return new string[] {
		//            "JOIN",
		//            "405",				// Too many channels
		//            "471",				// Cannot join channel (+l)
		//            "473",				// Cannot join channel (+i)
		//            "474",				// Cannot join channel (+b)
		//            "475",				// Cannot join channel (+k)
		//            "477",				// need to register
		//            "485"				// Cannot join channel
		//        };
		//    }
		//}
	}

	public class NickCommand : Command
	{
		public NickCommand()
			: base("Nick")
		{
			Usage = "Nick <NewNnick>";
			Description = "Changes your current nickname.";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Nick(trigger.Args.NextWord());
		}
	}

	public class TopicCommand : Command
	{
		public TopicCommand()
			: base("Topic")
		{
			Usage = "Topic [<Channel>] <Topic>";
			Description = "Changes the Topic in the given Channel (if possible). The channel parameter will only be accepted if not used in a Channel.";
		}

		public override void Process(CmdTrigger trigger)
		{
			var chan = trigger.Channel;
			if (chan == null)
			{
				chan = trigger.irc.GetChannel(trigger.Args.NextWord());
				if (chan == null)
				{
					trigger.Reply("Invalid Channel.");
					return;
				}
			}
			chan.Topic = trigger.Args.Remainder;
		}
	}

	public class PartCommand : Command
	{
		public PartCommand()
			: base("Part")
		{
			Usage = "Part [<Channel> [<Reason>]]";
			Description = "Parts a given channel (or the channel of origin if no argument given) with an optional reason";
		}

		public override void Process(CmdTrigger trigger)
		{
			var target = trigger.Args.NextWord();
			if (target.Length == 0)
			{
				target = trigger.Channel.Name;
			}
			trigger.Irc.CommandHandler.Part(target,
									  trigger.Args.Remainder.Trim());
		}
	}

	public class PartThisCommand : Command
	{
		public PartThisCommand()
			: base("PartThis")
		{
			Usage = "PartThis [<Reason>]";
			Description = "Parts the channel from where the trigger originated";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Part(trigger.Channel,
									  trigger.Args.Remainder.Trim());
		}
	}

	public class MsgCommand : Command
	{
		public MsgCommand()
			: base("Msg", "Message", "Privmsg")
		{
			Usage = "Msg|Message|Privmsg <Target> <Text>";
			Description = "Sends a privmsg to the specified target";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Msg(trigger.Args.NextWord(), trigger.Args.Remainder.Trim());
		}
	}

	public class NoticeCommand : Command
	{
		public NoticeCommand()
			: base("Notice")
		{
			Usage = "Notice <Target> <Text>";
			Description = "Sends a notice to the specified target";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Notice(trigger.Args.NextWord(), trigger.Args.Remainder.Trim());
		}
	}

	public class CtcpRequestCommand : Command
	{
		public CtcpRequestCommand()
			: base("Ctcp")
		{
			Usage = "Ctcp <Target> <Request> [<arguments>]";
			Description = "Sends a ctcp - request to a target";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.CtcpRequest(trigger.Args.NextWord(), trigger.Args.NextWord(),
											 trigger.Args.Remainder.Trim());
		}
	}

	public class KickCommand : Command
	{
		public KickCommand()
			: base("Kick")
		{
			Usage = "Kick <Channel> <User> [<Reason>]";
			Description = "Kicks a user from a channel with an optional reason";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Kick(trigger.Args.NextWord(), trigger.Args.NextWord(),
									  trigger.Args.Remainder.Trim());
		}
	}


	public class KickMaskCommand : Command
	{
		public KickMaskCommand()
			: base("KickMask", "KickM")
		{
			Usage = "Kickmask|Kickm <Channel> <Mask> [<Reason>]";
			Description = "Kicks all users with a specified mask for an optional reason";
		}

		public override void Process(CmdTrigger trigger)
		{
			string chanName = trigger.Args.NextWord();
			IrcChannel chan = trigger.Irc.GetChannel(chanName);

			if (chan != null)
			{
				string mask = trigger.Args.NextWord();

				foreach (IrcUser user in chan)
				{
					if (user.Matches(mask))
						trigger.Irc.CommandHandler.Kick(chanName, user.Nick, trigger.Args.Remainder.Trim());
				}
			}
		}
	}

	public class ModeCommand : Command
	{
		public ModeCommand()
			: base("Mode")
		{
			Usage = "Mode <flags> <targets>";
			Description = "Sets the specified mode";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Mode(trigger.Args.Remainder.Trim());
		}
	}

	public class BanCommand : Command
	{
		public BanCommand()
			: base("Ban")
		{
			Usage = "Ban [-u <seconds>] <Channel> <Mask1> <Mask2> ...";
			Description =
				"Bans masks from a channel. If the -u switch is specified, the following argument must be the number of seconds before the masks are automatically unbanned again.";
		}

		public override void Process(CmdTrigger trigger)
		{
			string word = trigger.Args.NextWord();
			string target = trigger.Args.NextWord();
			if (word.Equals("-u"))
			{
				TimeSpan time = TimeSpan.FromSeconds(trigger.Args.NextInt());
				trigger.Irc.CommandHandler.Ban(target, time, trigger.Args.RemainingWords());
			}
			else
			{
				trigger.Irc.CommandHandler.Ban(target, trigger.Args.RemainingWords());
			}
		}
	}

	public class UnbanCommand : Command
	{
		public UnbanCommand()
			: base("Unban")
		{
			Usage = "Unban <Channel> <Mask1> <Mask2> ...";
			Description = "Unbans given masks from a channel";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Unban(trigger.Args.NextWord(), trigger.Args.RemainingWords());
		}
	}

	public class InviteCommand : Command
	{
		public InviteCommand()
			: base("Invite")
		{
			Usage = "Invite <Nick> [<Channel>]";
			Description = "Invites a person into a channel. Invites into the channel of origin if channel is left out.";
		}

		public override void Process(CmdTrigger trigger)
		{
			string nick = trigger.Args.NextWord();
			string target = trigger.Args.NextWord();
			if (target.Length == 0)
			{
				target = trigger.Channel.Name;
			}
			trigger.Irc.CommandHandler.Invite(nick, target);
		}
	}

	public class InviteMeCommand : Command
	{
		public InviteMeCommand()
			: base("InviteMe")
		{
			Usage = "InviteMe <Channel>";
			Description = "Invites the triggering user into a channel.";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.Invite(trigger.User, trigger.Args.NextWord());
		}
	}

	public class BanListCommand : Command
	{
		public BanListCommand()
			: base("BanList", "ListBans")
		{
			Usage = "BanList|ListBans <channel>";
			Description = "Retrieves the active banmasks from a channel";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.CommandHandler.RetrieveBanList(trigger.Args.NextWord());
		}
	}

	public class SetInfoCommand : Command
	{
		public SetInfoCommand()
			: base("SetInfo", "ChangeInfo")
		{
			Usage = "SetInfo|ChangeInfo <userinfo>";
			Description = "Changes your user-info (will have effect after reconnect)";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.Info = trigger.Args.NextWord();
		}
	}

	public class SetPwCommand : Command
	{
		public SetPwCommand()
			: base("SetPw", "SetPass", "ChangePass")
		{
			Usage = "SetPW|SetPass|ChangePass <newPassword>";
			Description = "Changes your password (will have effect after reconnect)";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.ServerPassword = trigger.Args.NextWord();
		}
	}

	public class SetUsernameCommand : Command
	{
		public SetUsernameCommand()
			: base("SetUser", "SetUsername")
		{
			Usage = "SetUser|SetUserName <username>";
			Description = "Changes your username (will have effect after reconnect)";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.UserName = trigger.Args.NextWord();
		}
	}

	public class SetNicksCommand : Command
	{
		public SetNicksCommand()
			: base("SetNicks")
		{
			Usage = "SetNicks <nick>[ <nick2> [<nick3>...]]";
			Description = "Changes your default nicknames (seperated by space).";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.Nicks = trigger.Args.RemainingWords();
		}
	}

	public class ConnectCommand : Command
	{
		public ConnectCommand()
			: base("Connect", "Con")
		{
			Usage = "Connect|Con [<address> [<port>]]";
			Description = "(Re)connects to the given server.";
		}

		public override void Process(CmdTrigger trigger)
		{
			string addr = trigger.Args.NextWord();
			int port = trigger.Irc.Client.RemotePort;
			if (addr.Length == 0)
			{
				addr = trigger.Irc.Client.RemoteAddress;
			}
			else
			{
				port = trigger.Args.NextInt(port);
			}
			trigger.Irc.Client.Disconnect();

			trigger.Irc.BeginConnect(addr, port);
		}
	}

	public class DisconnectCommand : Command
	{
		public DisconnectCommand()
			: base("Disconnect", "Discon")
		{
			Usage = "Disconnect|Discon";
			Description = "Disconnects the current connection";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.Client.DisconnectNow();
		}
	}

	public class SetExternalIPCommand : Command
	{
		public SetExternalIPCommand()
			: base("SetExternalIP")
		{
			Usage = "SetExternalIP <Ip>";
			Description = "Changes your Util.ExternalAddres. This is used for DCC sessions and to bind local sockets to, if possible.";
		}

		public override void Process(CmdTrigger trigger)
		{
			Util.ExternalAddress = IPAddress.Parse(trigger.Args.NextWord());
		}
	}

	#region DCC
	public class DccSendCommand : Command
	{
		public DccSendCommand()
			: base("DccSend")
		{
			Usage = "DccSend <filename> [<port> [<target>]]";
			Description = "Tries to send a file to a user.";
		}

		public override void Process(CmdTrigger trigger)
		{
			string filename = trigger.Args.NextWord();
			var port = trigger.Args.NextInt(0);
			string target = trigger.Args.NextWord();
			if (target.Length == 0)
			{
				target = trigger.User.Nick;
			}
			trigger.Irc.Dcc.Send(target, filename, port);
		}
	}

	public class DccChatCommand : Command
	{
		public DccChatCommand()
			: base("DccChat")
		{
			Usage = "DccChat <target> [<port> [<text>]]";
			Description =
				"Tries to establish a direct Chat session with the specified target or sends the given text if the connection is already established.";
		}

		public override void Process(CmdTrigger trigger)
		{
			var nick = trigger.Args.NextWord();
			var port = trigger.Args.NextInt(0);
			var client = trigger.Irc.Dcc.GetChatClient(nick);
			if (client == null)
			{
				trigger.Irc.Dcc.Chat(nick, port);
			}
			else
			{
				client.Send(trigger.Args.Remainder.Trim());
			}
		}
	}

	//public class StatsCommand : Command {
	//    public StatsCommand()
	//        : base("Stats") {
	//        Usage = "Stats <options>";
	//        Description = "Queries server stats.";
	//    }

	//    public override void Process(CmdTrigger trigger) {
	//        trigger.Irc.Send("STATS " + trigger.Args.Remainder.Trim());
	//    }
	//}
	#endregion

	public class SendCommand : Command
	{
		public SendCommand()
			: base("Send")
		{
			Usage = "Send <args>";
			Description = "Sends the given args as-is to the server (raw).";
		}

		public override void Process(CmdTrigger trigger)
		{
			trigger.Irc.Send(trigger.Args.Remainder.Trim());
		}
	}


	/*
	public class EchoCommand : Command {
		public EchoCommand() : base("echo","e","ech") {
			Usage = "Echo <text>";
			Description = "Echo'str some evluated text in the active window";
		}

		public override void Process(CmdTrigger trigger) {
			Match match = (new Regex(@"\$([^ ]+)")).Match(args);
			Type type = typeof(IrcConnection);
			while (match.Success) {
				string var = match.Groups[1].Value;
				PropertyInfo prop = type.GetProperty(var, BindingFlags.IgnoreCase);
				if (prop != null) {
					args = args.Replace(var, prop.GetValue(trigger.Irc, new object[0]).ToString());
				}
				match = match.NextMatch();
			}
			Window.Active.WriteLine(args.Replace("$active", Window.Active.Text));
		}
	}*/
}