using System;
using System.Collections.Generic;
using System.Reflection;
using Squishy.Network;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// Wrapper class for some default non-Dcc-related commands and command handling 
	/// by providing the ReactTo(string) method.
	/// (Look into the Dcc class for Dcc-related commands).
	/// Also has a Hashtable, containing all Command objects that are currently being used.
	/// </summary>
	public class IrcCommandHandler
	{
		private static readonly List<Command> commands = new List<Command>();
		/// <summary>
		/// The Table of all Commands which exists for the use of the ReactTo() method
		/// (Filled by the Initialize() method).
		/// The keys are all possible aliases of all commands and the values are ArrayLists of Commands 
		/// which are associated with the specific alias.
		/// The aliases are stored case-insensitively. 
		/// Use the Remove(Command) and Add(Command) methods to manipulate this CommandsByAlias.
		/// </summary>
		public static readonly IDictionary<string, Command> CommandsByAlias = new Dictionary<string, Command>(StringComparer.InvariantCultureIgnoreCase);
		private readonly IDictionary<string, Queue<CmdTrigger>> awaitedResponses;
		private readonly IrcClient ircClient;

		/// <summary>
		/// Sets the default command-prefix to trigger this client'str commands.
		/// Set this to 0 if this client is not supposed to act like a bot.
		/// </summary>
		public string RemoteCommandPrefix = "!";

		public IrcCommandHandler(IrcClient connection)
		{
			ircClient = connection;
			awaitedResponses = new Dictionary<string, Queue<CmdTrigger>>();
		}

		#region React to custom commands

		public Command this[string alias]
		{
			get { return Get(alias); }
		}

		/// <summary>
		/// Calls <code>return ReactTo(new CmdTrigger(text));</code>.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		public bool ReactTo(StringStream text)
		{
			return ReactTo(new ExecuteCmdTrigger(text));
		}

		/// <summary>
		/// Executes a specific Command with parameters.
		/// 
		/// Interprets the first word as alias, takes all enabled Commands with the specific alias out of the 
		/// CommandsByAlias-map and triggers the specific Process() method on all of them.
		/// If the processing of the command raises an Exception, the fail events are triggered.
		/// </summary>
		/// <returns>True if at least one Command was triggered, otherwise false.</returns>
		public bool ReactTo(CmdTrigger trigger)
		{
			var cmdstring = trigger.Args.NextWord();

			trigger.alias = cmdstring;
			trigger.irc = ircClient;

			// execute the command
			var triggered = false;
			var cmd = this[cmdstring];
			if (cmd != null)
			{
				trigger.cmd = cmd;
				if (cmd.Enabled && ircClient.MayTriggerCommand(trigger))
				{
					try
					{
						cmd.Process(trigger);

						// command callbacks
						cmd.ExecutedNotify(trigger);

						// TODO: Create a tree of expected responses?
						//string[] expectedReplies = cmd.ExpectedServResponses;
						//if (expectedReplies != null) {
						//    trigger.expectsServResponse = true;

						//    foreach (string reply in expectedReplies) {
						//        AddWaitingTrigger(reply, trigger);
						//    }
						//}
					}
					catch (Exception e)
					{
						cmd.FailNotify(trigger, e);
					}
					triggered = true;
				}
			}
			else
			{
				ircClient.UnkownCommandUsedNotify(trigger);
			}
			return triggered;
		}

		/// <summary>
		/// Add a trigger that awaits a server-response;
		/// </summary>
		internal void AddWaitingTrigger(string reply, CmdTrigger trigger)
		{
			Queue<CmdTrigger> triggers;
			if (awaitedResponses.TryGetValue(reply, out triggers))
			{
				triggers = new Queue<CmdTrigger>();
				awaitedResponses.Add(reply, triggers);
			}
			triggers.Enqueue(trigger);
		}


		internal void NotifyServResponse(string sender, string action, string remainder)
		{
			// get the next trigger from the queue that awaits a certain response to the command being used.
			// TODO: implenet protocol engine that defines requests and response-maps between
			//			protocol actions and client-states
			Queue<CmdTrigger> triggers;
			awaitedResponses.TryGetValue(action, out triggers);
			if (triggers != null)
			{
				var trigger = triggers.Dequeue();
				trigger.NotifyServResponse(sender, action, remainder);
			}
		}

		#endregion

		#region Default IRC-Actions

		public void Join(string target)
		{
			ircClient.Send("JOIN " + target);
		}

		public void Join(string target, string key)
		{
			ircClient.Send("JOIN {0} :{1}", target, key);
		}

		public void Nick(string newNick)
		{
			ircClient.Send("NICK " + newNick);
		}

		public void Whois(string nick)
		{
			ircClient.Send("WHOIS " + nick + " " + nick);
		}

		public void WhoisSimple(string nick)
		{
			ircClient.Send("WHOIS " + nick);
		}

		public void Part(string chan, string reason, params object[] args)
		{
			ircClient.Send("PART " + chan + " :" + String.Format(reason, args));
		}

		public void Part(IrcChannel chan, string reason, params object[] args)
		{
			ircClient.Send("PART " + chan.Name + " :" + String.Format(reason, args));
		}

		public void Msg(ChatTarget Target, object format, params object[] args)
		{
			Msg(Target.Identifier, format, args);
		}

		public void Msg(string Target, object format, params object[] args)
		{
			string[] lines = String.Format(format.ToString(), args).Replace("\r\n", "\n").Split('\r', '\n');
			foreach (string line in lines)
				ircClient.Send("PRIVMSG {0} :{1}", Target, line);
		}

		public void Notice(ChatTarget Target, string format, params object[] args)
		{
			Notice(Target.Identifier, format, args);
		}

		public void Notice(string Target, string format, params object[] args)
		{
			string[] lines = String.Format(format, args).Replace("\r\n", "\n").Split('\r', '\n');
			foreach (string line in lines)
				ircClient.Send("NOTICE {0} :{1}", Target, line);
		}

		public void Describe(ChatTarget Target, string format, params object[] args)
		{
			Describe(Target.Identifier, format, args);
		}

		public void SetTopic(string chan, string topic)
		{
			ircClient.Send("TOPIC " + chan + " :" + topic);
		}

		public void Describe(string Target, string format, params object[] args)
		{
			string[] lines = String.Format(format, args).Replace("\r\n", "\n").Split('\r', '\n');
			foreach (string line in lines)
				CtcpRequest(Target, "ACTION", format, line);
		}

		public void CtcpRequest(string Target, string Request, string argFormat, params object[] args)
		{
			Msg(Target, "{0} {1}", Request.ToUpper(), string.Format(argFormat, args));
		}

		public void CtcpReply(string Target, string Request, string argFormat, params object[] args)
		{
			Notice(Target, "{0} {1}", Request.ToUpper(), string.Format(argFormat, args));
		}

		public void DccRequest(string Target, string requestFormat, params object[] args)
		{
			CtcpRequest(Target, "DCC", requestFormat, args);
		}

		public void Mode(string flags)
		{
			ircClient.Send("MODE " + flags);
		}

		public void Mode(string flags, string Targets)
		{
			ircClient.Send("MODE " + flags + " " + Targets);
		}

		public void Mode(string Channel, string flags, string Targets)
		{
			ircClient.Send("MODE " + Channel + " " + flags + " " + Targets);
		}

		public void Mode(IrcChannel Channel, string flags, string Targets)
		{
			ircClient.Send("MODE " + Channel.Name + " " + flags + " " + Targets);
		}

		public void Mode(string flags, params object[] Targets)
		{
			ircClient.Send("MODE " + flags + " " + Util.GetWords(Targets, 0));
		}

		public void Mode(string Channel, string flags, params object[] Targets)
		{
			ircClient.Send("MODE " + Channel + " " + flags + " " + Util.GetWords(Targets, 0));
		}

		public void Mode(IrcChannel Channel, string flags, params object[] Targets)
		{
			ircClient.Send("MODE " + Channel.Name + " " + flags + " " + Util.GetWords(Targets, 0));
		}

		public void Kick(IrcChannel channel, IrcUser user)
		{
			Kick(channel.Name, user.Nick);
		}

		public void Kick(string channel, string user)
		{
			ircClient.Send("KICK " + channel + " " + user);
		}

		public void Kick(IrcChannel Channel, IrcUser User, string reasonFormat, params object[] args)
		{
			Kick(Channel.Name, User.Nick, reasonFormat, args);
		}

		public void Kick(string Channel, string User, string reasonFormat, params object[] args)
		{
			ircClient.Send("KICK " + Channel + " " + User + " :" + string.Format(reasonFormat, args));
		}

		public void Ban(IrcChannel Channel, params object[] Masks)
		{
			Ban(Channel.Name, Masks);
		}

		public void Ban(string Channel, params object[] Masks)
		{
			if (Masks.Length == 0)
				return;
			string flag = "+";
			for (int i = 0; i < Masks.Length; i++)
				flag += "b";
			ircClient.Send("MODE {0} {1} {2}", Channel, flag, Util.GetWords(Masks, 0));
		}

		public void Ban(string Channel, TimeSpan Time, params object[] Masks)
		{
			Ban(ircClient.GetChannel(Channel), Time, Masks);
		}

		public void Ban(IrcChannel Channel, TimeSpan Time, params object[] Masks)
		{
			if (Masks.Length == 0)
				return;
			string flag = "+";
			foreach (string mask in Masks)
			{
				new UnbanTimer(Channel, mask, Time);
				flag += "b";
			}
			ircClient.Send("MODE {0} {1} {2}", Channel, flag, Util.GetWords(Masks, 0));
		}

		public void KickBan(string channel, string reason, params object[] masks)
		{
			KickBan(ircClient.GetChannel(channel), reason, masks);
		}

		public void KickBan(IrcChannel channel, string reason, params object[] masks)
		{
			Ban(channel, masks);
			foreach (string mask in masks)
			{
				foreach (IrcUser u in channel)
					if (u.Matches(mask))
						Kick(channel, u, reason);
			}
		}

		public void KickBan(string channel, params object[] masks)
		{
			KickBan(ircClient.GetChannel(channel), masks);
		}

		public void KickBan(IrcChannel channel, params object[] masks)
		{
			Ban(channel, masks);
			foreach (string mask in masks)
			{
				foreach (IrcUser u in channel)
					if (u.Matches(mask))
						Kick(channel, u);
			}
		}

		public void KickBan(string channel, TimeSpan time, string reason, params object[] masks)
		{
			KickBan(ircClient.GetChannel(channel), time, reason, masks);
		}

		public void KickBan(IrcChannel channel, TimeSpan time, string reason, params object[] masks)
		{
			Ban(channel, time, masks);
			foreach (string mask in masks)
			{
				foreach (IrcUser u in channel)
					if (u.Matches(mask))
						Kick(channel, u, reason);
			}
		}

		public void KickBan(string channel, TimeSpan time, params object[] masks)
		{
			KickBan(ircClient.GetChannel(channel), time, masks);
		}

		public void KickBan(IrcChannel channel, TimeSpan time, params object[] masks)
		{
			Ban(channel, time, masks);
			foreach (string mask in masks)
			{
				foreach (IrcUser u in channel)
					if (u.Matches(mask))
						Kick(channel, u);
			}
		}

		public void Unban(IrcChannel Channel, string Masks)
		{
			Unban(Channel, Masks.Split(' '));
		}

		public void Unban(IrcChannel Channel, params string[] Masks)
		{
			Unban(Channel.Name, Masks);
		}

		public void Unban(string Channel, string Masks)
		{
			Unban(Channel, Masks.Split(' '));
		}

		public void Unban(string Channel, params string[] Masks)
		{
			if (Masks.Length == 0)
				return;
			string flag = "-";
			for (int i = 0; i < Masks.Length; i++)
				flag += "b";
			ircClient.Send("MODE {0} {1} {2}", Channel, flag, Util.GetWords(Masks, 0));
		}

		public void RetrieveBanList(string Channel)
		{
			ircClient.Send("MODE " + Channel + " +b");
		}

		public void Invite(string Nick, string Channel)
		{
			ircClient.Send("INVITE " + Nick + " " + Channel);
		}

		public void Invite(string Nick, IrcChannel Channel)
		{
			ircClient.Send("INVITE " + Nick + " " + Channel);
		}

		public void Invite(IrcUser User, string Channel)
		{
			ircClient.Send("INVITE " + User + " " + Channel);
		}

		public void Invite(IrcUser User, IrcChannel Channel)
		{
			ircClient.Send("INVITE " + User + " " + Channel);
		}

		#endregion

		//static IDictionary<Type, Command> commandMapByType;


		public static IList<Command> List
		{
			get { return commands; }
		}

		//public static IDictionary<Type, Command> CommandsByType {
		//    get { return commandMapByType; }
		//}

		/// <summary>
		/// Adds a Command to the CommandsByAlias.
		/// </summary>
		public static void Add(Command cmd)
		{
			//Type type = cmd.GetType();
			//if (commandMapByType.ContainsKey(type)) {
			//    throw new Exception("Trying to create a second instance of a Singleton Command-object");
			//}

			//// map by type
			//commandMapByType[type] = cmd;
			commands.Add(cmd);

			// Add to table, mapped by aliases
			foreach (var alias in cmd.Aliases)
			{
				//Command exCommand; Unused variable
				//if (!commandsByAlias.TryGetValue(alias, out exCommand))
				{
					CommandsByAlias[alias] = cmd;
				}
				//else
				{
					//throw new IrcException("Found two Commands with Alias \"{0}\": {1} and {2}", alias, exCommand, cmd);
				}
			}
		}

		/// <summary>
		/// Adds a command of the specific type to the CommandsByAlias.
		/// </summary>
		public static void Add(Type cmdType)
		{
			if (cmdType.IsSubclassOf(typeof (Command)))
			{
				var cmd = (Command) Activator.CreateInstance(cmdType);
				Add(cmd);
			}
		}

		/// <summary>
		/// Removes a Command.
		/// </summary>
		public static void Remove(Command cmd)
		{
			//commandMapByType.Remove(cmd.GetType());
			commands.Remove(cmd);

			foreach (var alias in cmd.Aliases)
			{
				CommandsByAlias.Remove(alias);
			}
		}

		public static Command Get(string alias)
		{
			Command command;
			CommandsByAlias.TryGetValue(alias, out command);
			return command;
		}

		///// <summary>
		///// Removes all Commands of the specific Type from the CommandsByAlias.
		///// </summary>
		///// <returns>True if any commands have been removed, otherwise false.</returns>
		//public static bool Remove(Type cmdType) {
		//    Command cmd = Get(cmdType);
		//    if (cmd != null) {
		//        Remove(cmd);
		//        return true;
		//    }
		//    return false;
		//}
		/// <summary>
		/// Clears the CommandsByAlias, invokes an instance of every Class that is inherited from Command and adds it
		/// to the CommandsByAlias and the List.
		/// Is automatically called when an instance of IrcClient is created in order to find all Commands.
		/// </summary>
		public static void Initialize()
		{
			//commandMapByType = new Dictionary<Type, Command>();

			var cmdType = typeof (Command);

			var thisTypes = cmdType.Assembly.GetTypes();
			Type[] types;

			var callAsm = Assembly.GetCallingAssembly();
			var totalLength = thisTypes.Length;

			if (callAsm != null)
			{
				var callingTypes = callAsm.GetTypes();
				totalLength += callingTypes.Length;
				types = new Type[totalLength];
				Array.Copy(thisTypes, types, thisTypes.Length);
				Array.Copy(callingTypes, 0, types, thisTypes.Length, callingTypes.Length);
			}
			else
			{
				types = thisTypes;
			}

			foreach (var type in types)
			{
				Add(type);
			}
		}
	}
}