using System;
using Squishy.Network;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// CmdTriggers trigger Commands. There are different kinds of triggers which are handled differently, 
	/// according to where they came from.
	/// 
	/// </summary>
	public abstract class CmdTrigger
	{
		private readonly StringStream args;
		private readonly IrcChannel chan;
		private readonly IrcUser user;

		internal string alias;
		internal Command cmd;
		internal bool expectsServResponse;
		public IrcClient irc;

		public CmdTrigger()
		{
		}

		public CmdTrigger(StringStream args)
			: this(args, null, null)
		{
		}

		public CmdTrigger(StringStream args, IrcUser user, IrcChannel chan)
		{
			this.args = args;
			this.user = user;
			this.chan = chan;
		}

		/// <summary>
		/// That command that has been triggered or null if the command for this <code>Alias</code> could
		/// not be found.
		/// </summary>
		public Command Command
		{
			get { return cmd; }
		}

		/// <summary>
		/// The alias that has been used to trigger this command.
		/// </summary>
		public string Alias
		{
			get { return alias; }
		}

		/// <summary>
		/// The IrcClient from where the trigger came.
		/// </summary>
		public IrcClient Irc
		{
			get { return irc; }
			set { irc = value; }
		}

		/// <summary>
		/// The user who triggered the command.
		/// </summary>
		public IrcUser User
		{
			get { return user; }
		}

		/// <summary>
		/// The channel in which it happened (or null).
		/// </summary>
		public IrcChannel Channel
		{
			get { return chan; }
		}

		/// <summary>
		/// The ChatTarget which initialized this trigger (<code>Channel</code> or <code>User</code>).
		/// </summary>
		public ChatTarget Target
		{
			get { return (chan != null ? (ChatTarget) chan : user); }
		}

		/// <summary>
		/// The arguments of this CmdTrigger as text.
		/// </summary>
		public string Text
		{
			get { return args.String; }
		}

		/// <summary>
		/// A <code>StringStream</code> which contains the supplied arguments.
		/// </summary>
		public StringStream Args
		{
			get { return args; }
		}

		/// <summary>
		/// Wether or not the command triggered, through this CmdTrigger expects 
		/// an answer from the server.
		/// TODO: !
		/// </summary>
		public bool ExpectsServResponse
		{
			get { return expectsServResponse; }
		}

		/// <returns>Wether this is allowed to trigger the current command or not.</returns>
		public bool MayTrigger()
		{
			return Irc.MayTriggerCommand(this);
		}

		/// <returns>Wether this is allowed to trigger the given command or not.</returns>
		public bool MayTrigger(Command cmd)
		{
			return Irc.MayTriggerCommand(this, cmd);
		}

		/// <summary>
		/// Replies accordingly with the given text.
		/// </summary>
		public abstract void Reply(string text);

		public void Reply(Object format, params Object[] args)
		{
			Reply(string.Format(format.ToString(), args));
		}


		/// <summary>
		/// Is called on a response from the server
		/// </summary>
		/// 
		public virtual bool OnServerResponse(string sender, string action, StringStream remainder)
		{
			return false;
		}

		internal virtual bool NotifyServResponse(string sender, string action, string remainder)
		{
			return expectsServResponse = OnServerResponse(sender, action, new StringStream(remainder));
		}
	}
}