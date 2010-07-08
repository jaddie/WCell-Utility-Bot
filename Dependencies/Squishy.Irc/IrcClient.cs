using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Squishy.Irc.Commands;
using Squishy.Irc.Protocol;
using Squishy.Network;
using Squishy.Irc.Auth;
using System.Text.RegularExpressions;

namespace Squishy.Irc
{
	/// <summary>
	/// Representing a fully working Irc connection class. Extend and override the sufficient OnEvent methods
	/// in order to customize it.
	/// The Dcc class offers sufficient events and commands for dealing with all Dcc matters.
	/// </summary>
	public class IrcClient
	{
		public static readonly Privilege[] Privileges = (Privilege[])Enum.GetValues(typeof(Privilege));
		public static readonly Regex WhiteSpace = new Regex("\\s");

		#region Fields

		private static Encoding encoding = Encoding.UTF8;
		private static string m_quitReason = "squi# Irc Client disconnected", m_version = "squi# Irc Client 1.0";
		private readonly Client m_client;
		private readonly Dcc.Dcc m_dcc;
		private readonly IDictionary<string, IrcUser> m_Users;
		public string Info;
		private string m_CModes;
		protected IrcCommandHandler m_CommandHandler;
		private string m_CPrefixes, m_CSymbols;
		private string m_CTypes;
		private bool m_loggedIn;
		private IrcUser m_me;
		private int m_nickIndex;
		private string[] m_nicks;
		private string m_server;
		private IDictionary<string, UnbanTimer> m_unbanTimers;
		protected IrcProtocolHandler protHandler;
		protected int m_MaxNickLen = 30;

		/// <summary>
		/// Contains information and tools to operate on this specific Network.
		/// </summary>
		public readonly IrcNetworkInfo Network = new IrcNetworkInfo();

		public string ServerPassword = "";
		private string m_UserName;

		#endregion

		public IrcClient()
			: this(Encoding.UTF8)
		{
		}

		public IrcClient(Encoding encoding)
		{
			Reset();
			m_Users = new Dictionary<string, IrcUser>(StringComparer.InvariantCultureIgnoreCase);
			m_unbanTimers = new Dictionary<string, UnbanTimer>();

			m_me = new IrcUser(this);
			m_client = new Client(this);
			m_CommandHandler = new IrcCommandHandler(this);
			m_dcc = new Dcc.Dcc(this);
			protHandler = new IrcProtocolHandler(this, encoding);
		}

		#region Public Access

		/// <summary>
		/// The Client instance which handles the socket and all incoming and outgoing Text.
		/// </summary>
		public Client Client
		{
			get { return m_client; }
		}

		public IrcProtocolHandler ProtocolHandler
		{
			get { return protHandler; }
		}

		/// <summary>
		/// The Dcc instance which is used for all Dcc processing.
		/// </summary>
		public Dcc.Dcc Dcc
		{
			get { return m_dcc; }
		}

		/// <summary>
		/// The Commands instance which handles the Aliasing engine and offers a set of default Irc Commands.
		/// </summary>
		public IrcCommandHandler CommandHandler
		{
			get { return m_CommandHandler; }
		}

		/// <summary>
		/// The username to be used. 
		/// Changing this will not have an effect before you connect to a network again.
		/// </summary>
		public string UserName
		{
			get { return m_UserName; }
			set
			{
				if (WhiteSpace.IsMatch(value))
				{
					throw new ArgumentException("Username must not have whitespaces in it: " + value);
				}
				m_UserName = value;
			}
		}

		/// <summary>
		/// The maximal length of the nick depends on the network and will usually be set during login.
		/// </summary>
		/// <exception cref="InvalidOperationException">If the length of your nick exceeds this value.</exception>
		public int MaxNickLen
		{
			get { return m_MaxNickLen; }
			internal set
			{
				m_MaxNickLen = value;
				if (Me.Nick.Length > value)
				{
					Client.Disconnect();
					throw new InvalidOperationException(string.Format("Your Nick name is too long (max length: {0}): {1}",
						value, Me.Nick));
				}
			}
		}

		/// <summary>
		/// The User which is represented by this IrcClient.
		/// </summary>
		public IrcUser Me
		{
			get { return m_me; }
		}

		/// <summary>
		/// All known Users from the Channels which this IrcClient is on, indexed case-insensitively
		/// by their nicks.
		/// </summary>
		public IDictionary<string, IrcUser> Users
		{
			get { return m_Users; }
		}

		/// <summary>
		/// All known Channels which this IrcClient is on, indexed case-insensitively
		/// by their names.
		/// </summary>
		public IDictionary<string, IrcChannel> Channels
		{
			get { return m_me.Channels; }
		}

		/// <summary>
		/// The name of the server which this IrcClient is connected to.
		/// </summary>
		public string Server
		{
			get { return m_server; }
		}

		/// <summary>
		/// The quitmessage which will be seen by other Users when this IrcClient disconnects
		/// </summary>
		public static string QuitReason
		{
			get { return m_quitReason; }
			set { m_quitReason = value; }
		}

		/// <summary>
		/// The string which will be sent as response to the Ctcp version request 
		/// if the OnCtcpRequest() method is not overridden.
		/// </summary>
		public static string Version
		{
			get { return m_version; }
			set { m_version = value; }
		}

		/// <summary>
		/// The default Encoding for all Irc and Dcc connections.
		/// (Default = UTF8)
		/// </summary>
		public static Encoding Encoding
		{
			get { return encoding; }
			set { encoding = value; }
		}

		/// <summary>
		/// Indicates wether or not the IrcClient is fully logged on the network and the End of Motd was sent (raw 376).
		/// </summary>
		public bool LoggedIn
		{
			get { return m_loggedIn; }
		}

		/// <summary>
		/// An array of possible nicks through which the client will switch until an unused one is found when logging into a server.
		/// </summary>
		public string[] Nicks
		{
			get { return m_nicks; }
			set
			{
				m_nickIndex = 0;
				m_nicks = value;
			}
		}

		/// <summary>
		/// All modes which are valid on Channels for the current Irc network (such as b,m,n etc).
		/// </summary>
		public string ChanModes
		{
			get { return m_CModes; }
		}

		/// <summary>
		/// All flags that are valid for users to have on a channel (such as o,v etc).
		/// </summary>
		public string ChanFlags
		{
			get { return m_CPrefixes; }
		}

		/// <summary>
		/// All valid Channel symbols for the current Irc network (such as @,+ etc).
		/// </summary>
		public string ChanSymbols
		{
			get { return m_CSymbols; }
		}

		public void BeginConnect(string addr)
		{
			BeginConnect(addr, 6667);
		}

		/// <summary>
		/// Saves the sufficient information and lets the Client instance start to connect to a Server.
		/// It will not connect if it is already connected or currently connecting.
		/// (Use Disconnect first in that case)
		/// When the Client connected successfully it dumps its ThrottledSendQueue.
		/// </summary>
		/// <param name="addr">The address where the Client should connect to</param>
		/// <param name="port">The port on the Server where the Client should connect to</param>
		public void BeginConnect(string addr, int port)
		{
			m_me.SetInfo(m_nicks[0], UserName, ServerPassword, Info);
			m_client.BeginConnect(addr, port);
		}

		/// <summary>
		/// Indicates wether or not the current Irc Network offers the specified Channel modes - 
		/// Independent on the sequence.
		/// </summary>
		/// <param name="modes">A sequence of chars, representing Channel modes</param>
		public bool HasChanMode(string modes)
		{
			foreach (char c in modes)
				if (m_CModes.IndexOf(c) < 0)
					return false;
			return true;
		}

		/// <summary>
		/// Indicates wether or not the current Irc 4rk supports the specified 
		/// Channel symbols (such as @,+ etc) - Independent on the sequence.
		/// </summary>
		/// <param name="symbols">A sequence of symbols, representing Channel flags (such as '@')</param>
		public bool SupportsSymbols(string symbols)
		{
			foreach (char c in symbols)
				if (m_CSymbols.IndexOf(c) < 0)
					return false;
			return true;
		}

		/// <summary>
		/// Indicates wether or not this IrcClient is on the Channel with the specified name.
		/// </summary>
		public bool IsOn(string channame)
		{
			return m_me.IsOn(channame);
		}

		/// <summary>
		/// Returns the symbol for the specified flag ("@" would usually be the symbol for "o" e.g.).
		/// </summary>
		public char GetSymbolForFlag(char flag)
		{
			int i = m_CPrefixes.IndexOf(flag);
			if (i >= 0)
			{
				return m_CSymbols[i];
			}
			return (char)0;
		}

		public Privilege GetPrivForFlag(char flag)
		{
			return (Privilege)GetSymbolForFlag(flag);
		}

		/// <param name="nick">The case-insensitive User'str nick.</param>
		/// <returns>The User with the corresponding Nick</returns>
		public IrcUser GetUser(string nick)
		{
			IrcUser user;
			m_Users.TryGetValue(nick, out user);
			return user;
		}

		public IrcUser GetOrCreateUser(string mask)
		{
			IrcUser user = null;
			var nick = "";
			if (mask.IndexOf("!") > -1)
				nick = mask.Split('!')[0];
			else
				nick = mask;

			user = GetUser(nick);

			if (user == null)
			{
				user = new IrcUser(this, mask);
				OnUserEncountered(user);
			}
			else if (!user.IsParsed)
			{
				user.Parse(mask);
			}

			return user;
		}

		/// <param name="name">The case-insensitive Channel name.</param>
		/// <returns>The Channel with the corresponding name.</returns>
		public IrcChannel GetChannel(string name)
		{
			IrcChannel chan;
			m_me.Channels.TryGetValue(name, out chan);
			return chan;
		}

		/// <summary>
		/// Gets the channel with the current name or creates a new Channel object, if it
		/// doesn't exist yet
		/// </summary>
		/// <param name="name">The case-insensitive Channel name.</param>
		/// <returns>The Channel with the corresponding name.</returns>
		public IrcChannel GetOrCreateChannel(string name)
		{
			IrcChannel chan;
			m_me.Channels.TryGetValue(name, out chan);
			if (chan == null)
			{
				chan = new IrcChannel(this, name);
				m_me.Channels.Add(name, chan);
			}
			return chan;
		}


		/// <summary>
		/// Loops through all Users to find those who match the given mask.
		/// </summary>
		/// <param name="mask">The mask of the users that are to be found.</param>
		/// <returns>An Array of Users who match the given mask.</returns>
		public IrcUser[] GetUsers(string mask)
		{
			var users = new ArrayList();
			foreach (IrcUser user in m_Users.Values)
				if (user.Matches(mask))
					users.Add(user);
			return (IrcUser[])users.ToArray(typeof(IrcUser));
		}

		/// <summary>
		/// Uses the Client.Send(string) to enqueue the corresponding text into the
		/// ThrottledSendQueue.
		/// </summary>
		public void Send(string text, params object[] args)
		{
			Client.Send(string.Format(text, args));
		}

		/// <summary>
		/// Uses the Client.SendNow(text) to send a line of text immediately 
		/// to the server.
		/// </summary>
		public void SendNow(string text, params object[] args)
		{
			Client.SendNow(string.Format(text, args));
		}

		#endregion

		#region Internal Management

		internal void CheckUserKnown(IrcUser user)
		{
			if (user.Channels.Count == 0)
			{
				OnUserDisappeared(user);
				m_Users.Remove(user.Nick);
			}
		}

		protected internal virtual void OnUserEncountered(IrcUser user)
		{
			m_Users.Remove(user.Nick);
			m_Users.Add(user.Nick, user);
			if (AuthMgr != null && AutoResolveAuth)
			{
				// resolve
				AuthMgr.ResolveAuth(user);
			}
		}

		/// <summary>
		/// Called whenever a User quits or parts all common Channels
		/// </summary>
		/// <param name="user"></param>
		protected virtual void OnUserDisappeared(IrcUser user)
		{
		}

		internal void Reset()
		{
			m_server = "";
			ServerPassword = "";
			m_CTypes = "#";
			m_CPrefixes = "aohv";
			m_CSymbols = "&@%+";
			m_CModes = "b,k,l,imnpstr";
			m_nickIndex = 0;
			m_loggedIn = false;
		}

		#endregion

		#region Connect / Login / Disconnect

		/// <summary>
		/// Is called when the Client instance connects to a new server.
		/// </summary>
		protected virtual void OnConnecting()
		{
		}

		internal void ConnectingNotify()
		{
			OnConnecting();
		}

		/// <summary>
		/// Fires when the Client instance of this IrcClient established a connection with a server.
		/// </summary>
		protected virtual void OnConnected()
		{
		}

		internal void ConnectNotify()
		{
			OnConnected();
		}

		/// <summary>
		/// Fires when the Client raises the specified Exception during the Connect process.
		/// </summary>
		protected virtual void OnConnectFail(Exception ex)
		{
		}

		internal void ConnectFailNotify(Exception ex)
		{
			OnConnectFail(ex);
		}

		/// <summary>
		/// Fires when the Client disconnected.
		/// </summary>
		protected virtual void OnDisconnected(bool conLost)
		{
		}

		public event Action<IrcClient, bool> Disconnected;

		internal void DisconnectNotify(bool conLost)
		{
			OnDisconnected(conLost);
			var evt = Disconnected;
			if (evt != null)
			{
				evt(this, conLost);
			}

			AuthMgr.Cleanup();
		}

		/// <summary>
		/// Fires when network-specific Informations are sent (raw 005).
		/// </summary>
		/// <param name="pairs">A Dictionary, containing all information, indexed case-insensitively. Value is "" if the information isnt a pair.</param>
		protected virtual void OnConnectionInfo(IrcPacket packet, Dictionary<string, string> pairs)
		{
			// TODO: Improve authenticator detection
			if ("QuakeNet".Equals(Network.Name, StringComparison.InvariantCultureIgnoreCase))
			{
				// found Quakenet
				AuthMgr.Authenticator = new QuakenetAuthenticator();
			}
			else if ("freenode".Equals(Network.Name, StringComparison.InvariantCultureIgnoreCase))
			{
				AuthMgr.Authenticator = new NickServAuthenticator("320");
			}
		}

		internal void ConnectionInfoNotify(IrcPacket packet)
		{
			var args = packet.Content.Remainder;
			var index = args.IndexOf(':');
			if (index > -1)
			{
				args = args.Substring(0, index);
			}

			var words = args.Split(' ');
			m_server = packet.Prefix;
			var pairs = new Dictionary<string, string>(words.Length, StringComparer.InvariantCultureIgnoreCase);

			for (var i = 1; i < words.Length; i++)
			{
				string key, value;
				if (words[i].IndexOf('=') < 0)
				{
					key = words[i];
					value = "";
				}
				else
				{
					var pair = words[i].Split('=');
					key = pair[0];
					value = pair[1];
				}
				pairs[key] = value;
				switch (key.ToUpper())
				{
					case "CHANTYPES":
						m_CTypes = value;
						break;
					case "MAXNICKLEN":
						if (!int.TryParse(value, out m_MaxNickLen))
						{
							m_MaxNickLen = 30;
						}
						break;
					case "PREFIX":
						var pair = value.Substring(1).Split(')');
						m_CPrefixes = pair[0];
						m_CSymbols = pair[1];
						break;
					case "CHANMODES":
						m_CModes = value;
						break;
					case "NETWORK":
						Network.Name = value;
						break;
				}
			}
			if (Network.Name != null)
			{
				OnConnectionInfo(packet, pairs);
			}
		}

		#endregion

		#region Messaging

		/// <summary>
		/// Fires when the Client receives any kind of PRIVMSG or NOTICE.
		/// </summary>
		/// <param name="user">The User who sent the text</param>
		/// <param name="text">The text which was sent</param>
		protected virtual void OnText(IrcUser user, IrcChannel chan, StringStream text)
		{
		}

		internal void TextNotify(IrcUser user, IrcChannel chan, StringStream text)
		{
			if (chan != null)
				chan.TextNotify(user, text);
			OnText(user, chan, text);

			if (TriggersCommand(user, chan, text))
			{
				m_CommandHandler.ReactTo(new MsgCmdTrigger(text, user, chan));
			}
		}

		/// <summary>
		/// Fires when the Client receives a PRIVMSG which was directed to a Channel.
		/// </summary>
		protected virtual void OnChannelMsg(IrcUser user, IrcChannel chan, StringStream text)
		{
		}

		internal void ChannelMsgNotify(IrcUser user, IrcChannel chan, StringStream text)
		{
			chan.MsgReceivedNotify(user, text);
			OnChannelMsg(user, chan, text);
		}

		/// <summary>
		/// Fires when the Client receives a PRIVMSG, directed to this Client itself.
		/// </summary>
		protected virtual void OnQueryMsg(IrcUser user, StringStream text)
		{
		}

		internal void QueryMsgNotify(IrcUser user, StringStream text)
		{
			OnQueryMsg(user, text);
		}

		/// <summary>
		/// Fires when the Client receives any kind of NOTICE.
		/// </summary>
		/// <param name="user">The User who sent the text</param>
		/// <param name="chan">The Channel where it was sent (is null if its a private notice)</param>
		/// <param name="text">The text which was sent</param>
		protected virtual void OnNotice(IrcUser user, IrcChannel chan, StringStream text)
		{
		}

		public virtual bool TriggersCommand(IrcUser user, IrcChannel chan, StringStream input)
		{
			return input.ConsumeNext(CommandHandler.RemoteCommandPrefix);
		}

		internal void NoticeNotify(IrcUser user, IrcChannel chan, StringStream text)
		{
			if (chan != null)
				chan.NoticeReceivedNotify(user, text);
			OnNotice(user, chan, text);
			if (TriggersCommand(user, chan, text))
			{
				m_CommandHandler.ReactTo(new NoticeCmdTrigger(text, user, chan));
			}
		}

		/// <summary>
		/// Fires when the Client receives any kind of CTCP request. 
		/// Automatically replies to the VERSION request with the content of the Version variable
		/// if not overridden.
		/// </summary>
		/// <param name="user">The User who sent the text</param>
		/// <param name="chan">The Channel where it was sent (can be null)</param>
		/// <param name="request">The request type (such as VERSION)</param>
		/// <param name="args">The text which was sent in addition to the request</param>
		protected virtual void OnCtcpRequest(IrcUser user, IrcChannel chan, string request, string args)
		{
			if (request.ToUpper() == "VERSION" && Version != "")
				CommandHandler.CtcpReply(user.Nick, "VERSION", Version);
		}

		internal void CtcpRequestNotify(IrcUser user, IrcChannel chan, string request, string text)
		{
			if (request.ToUpper() == "DCC" && chan == null)
			{
				Dcc.Handle(user, text);
			}
			OnCtcpRequest(user, chan, request, text);
		}


		/// <summary>
		/// Fires when the Client receives any kind of CTCP reply.
		/// </summary>
		/// <param name="user">The User who sent the text</param>
		/// <param name="channel">The Channel where it was sent (can be null)</param>
		/// <param name="reply">The reply type (such as VERSION)</param>
		/// <param name="args">The text which was sent in addition to the reply</param>
		protected virtual void OnCtcpReply(IrcUser user, IrcChannel chan, string reply, string args)
		{
		}

		internal void CtcpReplyNotify(IrcUser user, IrcChannel chan, string reply, string args)
		{
			OnCtcpReply(user, chan, reply, args);
		}

		#endregion

		#region Channel Management

		/// <summary>
		/// Fires when the specified User joins the specified Channel.
		/// </summary>
		protected virtual void OnJoin(IrcUser user, IrcChannel chan)
		{
		}

		internal void JoinNotify(IrcUser user, string name)
		{
			if (!m_loggedIn)
				m_loggedIn = true;
			IrcChannel chan;
			if (user == Me)
			{
				Me.DeleteChannel(name);
			}
			if ((chan = GetChannel(name)) == null)
			{
				Send("mode " + name);
				if (user != m_me)
				{
					foreach (IrcChannel c in m_me)
					{
						user.AddChannel(c);
						c.AddUser(user);
						c.DeleteUser(user);
					}
					m_me = user;
					m_Users.Remove(m_me.Nick);
				}
				chan = new IrcChannel(this, name);
			}
			user.AddChannel(chan);
			chan.UserJoinedNotify(user);
			OnJoin(user, chan);
		}

		/// <summary>
		/// Fires when the Client receives the Names list for the specified Channel.
		/// </summary>
		/// <param name="users">An Array of Users who are on the Channel</param>
		protected virtual void OnUsersAdded(IrcChannel chan, IrcUser[] users)
		{
		}

		internal void UsersAddedNotify(IrcChannel chan, IrcUser[] users)
		{
			chan.UsersAddedNotify(users);
			OnUsersAdded(chan, users);
		}

		internal void CannotJoinNotify(IrcChannel channel, string reason)
		{
			OnCannotJoin(channel, reason);
		}

		protected virtual void OnCannotJoin(IrcChannel chan, string reason)
		{
		}

		/// <summary>
		/// Fires when the Topic for a Channel has been sent. Either when joining a Channel or when modified by a User.
		/// </summary>
		protected virtual void OnTopic(IrcUser user, IrcChannel chan, string text, bool initial)
		{
		}

		internal void TopicNotify(IrcUser user, IrcChannel chan, string text, bool initial)
		{
			chan.TopicChangedNotify(user, text, initial);
			OnTopic(user, chan, text, initial);
		}

		/// <summary>
		/// Fires when a User adds a Channel mode.
		/// </summary>
		/// <param name="user">The User who has added the mode</param>
		/// <param name="channel">The channel on which the mode has been changed</param>
		/// <param name="mode">The mode which has been changed</param>
		/// <param name="param">"" if the mode does not have any parameter</param>
		protected virtual void OnModeAdded(IrcUser user, IrcChannel chan, string mode, string param)
		{
		}

		internal void ModeAddedNotify(IrcUser user, IrcChannel chan, string mode, string param)
		{
			chan.ModeAddedNotify(user, mode, param);
			OnModeAdded(user, chan, mode, param);
		}

		/// <summary>
		/// Fures when a User deletes a Channel mode.
		/// </summary>
		/// <param name="user">The User who has added the mode</param>
		/// <param name="channel">The channel on which the mode has been changed</param>
		/// <param name="mode">The mode which has been changed</param>
		/// <param name="param">"" if the mode does not have any parameter</param>
		protected virtual void OnModeDeleted(IrcUser user, IrcChannel chan, string mode, string param)
		{
		}

		internal void ModeDeletedNotify(IrcUser user, IrcChannel chan, string mode, string param)
		{
			chan.ModeDeletedNotify(user, mode, param);
			OnModeDeleted(user, chan, mode, param);
		}

		/// <summary>
		/// Fires when a User adds a Channel flag to another User.
		/// </summary>
		protected virtual void OnFlagAdded(IrcUser user, IrcChannel chan, Privilege priv, IrcUser target)
		{
		}

		internal void FlagAddedNotify(IrcUser user, IrcChannel chan, Privilege priv, IrcUser target)
		{
			chan.FlagAddedNotify(user, priv, target);
			OnFlagAdded(user, chan, priv, target);
		}

		/// <summary>
		/// Fires when a User deletes a Channel flag from another User.
		/// </summary>
		protected virtual void OnFlagDeleted(IrcUser user, IrcChannel chan, Privilege priv, IrcUser target)
		{
		}

		internal void FlagDeletedNotify(IrcUser user, IrcChannel chan, Privilege priv, IrcUser target)
		{
			chan.FlagDeletedNotify(user, priv, target);
			OnFlagDeleted(user, chan, priv, target);
		}

		/// <summary>
		/// Fires when a User is kicked from a Channel.
		/// </summary>
		protected virtual void OnKick(IrcUser from, IrcChannel chan, IrcUser target, string reason)
		{
		}

		internal void KickNotify(IrcUser user, IrcChannel chan, IrcUser target, string reason)
		{
			OnKick(user, chan, target, reason);
			chan.UserKickedNotify(user, target, reason);
			target.DeleteChannel(chan.Name);
			CheckUserKnown(target);
		}

		/// <summary>
		/// Fires when a User parts from a Channel.
		/// </summary>
		protected virtual void OnPart(IrcUser user, IrcChannel chan, string reason)
		{
		}

		internal void PartNotify(IrcUser user, IrcChannel chan, string reason)
		{
			OnPart(user, chan, reason);
			chan.UserPartedNotify(user, reason);
			user.DeleteChannel(chan.Name);
			CheckUserKnown(user);
		}

		/// <summary>
		/// Fires when the CreationTime of a Channel has been sent (raw 329)
		/// </summary>
		protected virtual void OnChanCreationTime(IrcChannel chan, DateTime creationTime)
		{
		}

		internal virtual void ChanCreationTimeNotify(IrcChannel chan, DateTime creationTime)
		{
			chan.ChanCreationTimeSentNotify(creationTime);
			OnChanCreationTime(chan, creationTime);
		}

		/// <summary>
		/// Fires when an already established BanEntry has been sent (raw 367).
		/// </summary>
		protected virtual void OnBanListEntry(IrcChannel chan, BanEntry entry)
		{
		}

		internal void BanListEntryNotify(IrcChannel chan, BanEntry entry)
		{
			chan.BanListEntrySentNotify(entry);
			OnBanListEntry(chan, entry);
		}

		/// <summary>
		/// Fires when the BanList for a Channel has been sent completely.
		/// </summary>
		protected virtual void OnBanListComplete(IrcChannel chan)
		{
		}

		internal void BanListCompleteNotify(IrcChannel chan)
		{
			chan.BanListCompleteNotify();
			OnBanListComplete(chan);
		}

		#endregion

		#region Misc

		/// <summary>
		/// Fires when the Client is fully logged on the network and the End of Motd is sent (raw 376).
		/// </summary>
		protected virtual void Perform()
		{
		}

		internal void PerformNotify()
		{
			m_loggedIn = true;
			Perform();
		}

		/// <summary>
		/// Fires when the specified User sends an invitation for the specified Channel.
		/// </summary>
		protected virtual void OnInvite(IrcUser user, string chan)
		{
		}

		internal void InviteNotify(IrcUser user, string chan)
		{
			OnInvite(user, chan);
		}

		/// <summary>
		/// Fires when own Usermodes have been changed.
		/// </summary>
		protected virtual void OnUserModeChanged()
		{
		}

		internal void UserModeChangedNotify()
		{
			OnUserModeChanged();
		}

		/// <summary>
		/// Fires when a User has changed the nick.
		/// </summary>
		protected virtual void OnNick(IrcUser user, string oldNick, string newNick)
		{
			if (user == Me)
			{
				user.Nick = newNick;
			}
		}

		internal void NickNotify(IrcUser user, string newNick)
		{
			string oldNick = user.Nick;
			user.ChangeNick(newNick);
			if (user == Me)
			{
				m_nicks[0] = newNick;
			}
			OnNick(user, oldNick, newNick);
		}

		/// <summary>
		/// Fires when a chosen nick is already in use.
		/// </summary>
		protected virtual void OnInvalidNick(string err, string nick, string args)
		{
		}

		internal void InvalidNickNotify(string err, string nick, string args)
		{
			if (!m_loggedIn)
			{
				// handle automatic change of nicks when logging in
				CommandHandler.Nick(m_nicks[++m_nickIndex % m_nicks.Length]);
			}
			OnInvalidNick(err, nick, args);
		}

		/// <summary>
		/// Fires when a User quits.
		/// </summary>
		protected virtual void OnQuit(IrcUser user, string reason)
		{
		}

		internal void QuitNotify(IrcUser user, string reason)
		{
			foreach (var chan in user.Channels.Values)
			{
				chan.UserLeftNotify(user, reason);
			}
			OnQuit(user, reason);
			m_Users.Remove(user.Nick);
			if (user == Me)
			{
				m_client.Disconnect();
			}
		}

		/// <summary>
		/// Fires when a user is kicked from or parts from a channel or quits.
		/// </summary>
		protected virtual void OnUserLeftChannel(IrcChannel chan, IrcUser user, string reason)
		{
		}

		internal void UserLeftChannelNotify(IrcChannel chan, IrcUser user, string reason)
		{
			OnUserLeftChannel(chan, user, reason);
		}

		/// <summary>
		/// Fires when the Client receives a Who-reply (raw 352).
		/// </summary>
		/// <param name="channame">The name of the channel which the Who-reply was for or the User did  something on last</param>
		/// <param name="username">The username of the User</param>
		/// <param name="host">The hostmask of the User</param>
		/// <param name="server">The server which the User is connected to</param>
		/// <param name="nick">The nick of the User</param>
		/// <param name="flags">The network flags of the User</param>
		/// <param name="hops">The hops of the User</param>
		/// <param name="info">Additional Info about the User</param>
		protected virtual void OnWhoReply(string channame, string username, string host, string server, string nick,
										  string flags, string hops, string info)
		{
		}

		internal void WhoReplyNotify(string channame, string username, string host, string server, string nick, string flags,
									 string hops, string info)
		{
			OnWhoReply(channame, username, host, server, nick, flags, hops, info);
		}

		/// <summary>
		/// Fires when an Error reply has been sent by the network (raw is greater than 399).
		/// </summary>
		/// <param name="from">Usually the name of the current server</param>
		/// <param name="error">The raw numeric</param>
		/// <param name="preArgs">All command arguments (before a ":")</param>
		/// <param name="postArgs">All further arguments, probably describing the problem</param>
		protected virtual void OnError(IrcPacket packet)
		{
		}

		internal void ErrorNotify(IrcPacket packet)
		{
			OnError(packet);
		}

		/// <summary>
		/// Fires when an information is sent that is not captured by the intern protocol handler.
		/// </summary>
		protected virtual void OnUnspecifiedInfo(IrcPacket packet)
		{
		}

		internal void UnspecifiedInfoNotify(IrcPacket packet)
		{
			OnUnspecifiedInfo(packet);
		}

		/// <summary>
		/// Fires when something is sent by the Client. (When Client.SendNow(string) is called.)
		/// </summary>
		/// <param name="text">The line of text which is supposed to be sent.</param>
		protected virtual void OnBeforeSend(string text)
		{
		}

		internal void BeforeSendNotify(string text)
		{
			OnBeforeSend(text);
		}

		/// <summary>
		/// Fires when an exception is raised during the protocol handling.
		/// </summary>
		/// <param name="e">The Exception thrown during the handling.</param>
		protected virtual void OnExceptionRaised(Exception e)
		{
		}

		internal void ExceptionNotify(Exception e)
		{
			OnExceptionRaised(e);
		}

		#endregion

		#region Commands

		/// <summary>
		/// Fires when the aliasing engine cannot find a command.
		/// Sends the command to the Server when not overridden.
		/// </summary>
		protected virtual void OnUnknownCommandUsed(CmdTrigger trigger)
		{
			trigger.Reply("Use " + CommandHandler.RemoteCommandPrefix + "Help for an overview over all available commands!");
			//Send(trigger.Args.String);
		}

		internal void UnkownCommandUsedNotify(CmdTrigger trigger)
		{
			OnUnknownCommandUsed(trigger);
		}

		/// <summary>
		/// Fires when a Command raises an Exception while being executed.
		/// </summary>
		protected virtual void OnCommandFail(CmdTrigger trigger, Exception ex)
		{
		}

		internal void CommandFailNotify(CmdTrigger trigger, Exception ex)
		{
			OnCommandFail(trigger, ex);
		}

		/// <summary>
		/// Return wether or not a command may be triggered. 
		/// Is called everytime, a command is triggered.
		/// </summary>
		public bool MayTriggerCommand(CmdTrigger cmdTrigger)
		{
			return MayTriggerCommand(cmdTrigger, cmdTrigger.Command);
		}

		/// <summary>
		/// Return wether or not a command may be triggered. 
		/// Is called everytime, a command is triggered.
		/// </summary>
		/// <param name="cmd">A command.</param>
		public virtual bool MayTriggerCommand(CmdTrigger trigger, Command cmd)
		{
			return trigger.User == null || trigger.User == Me || trigger.User.AccountLevel >= cmd.RequiredAccountLevel;
		}

		#endregion

		#region Authentication
		public event Action<IrcUser> AuthResolved;
		internal void AuthNotify(IrcUser user)
		{
			var evt = AuthResolved;
			if (evt != null)
			{
				evt(user);
			}
		}

		private AuthenticationMgr m_AuthMgr = new AuthenticationMgr();

		public AuthenticationMgr AuthMgr
		{
			get { return m_AuthMgr; }
			protected set { m_AuthMgr = value; }
		}

		/// <summary>
		/// TODO: Implement auto resolve when joining channel or when others join channels this client is on
		/// </summary>
		public virtual bool AutoResolveAuth
		{
			get { return false; }
		}

		public virtual bool NotifyAuthedUsers
		{
			get { return false; }
		}

		internal void OnUserParsed(IrcUser user)
		{
			AuthMgr.OnNewUser(user);
		}
		#endregion
	}
}