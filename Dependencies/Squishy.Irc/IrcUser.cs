using System;
using System.Collections;
using System.Collections.Generic;
using Squishy.Irc.Account;

namespace Squishy.Irc
{
	public interface IIrcUserArgs
	{
	}

	public class IrcUser : IComparable, ChatTarget
	{
		private readonly IDictionary<string, IrcChannel> m_comChans;
		private readonly IrcClient m_irc;
		private bool m_hasIdentd;
		private string m_host;
		private string m_info;
		private bool m_isParsed;
		private string m_modes;
		private string m_UserName, m_authName;
		private string m_nick;
		private string m_pw;
        private bool m_LoggedIn;
        private AccountMgr.AccountLevel m_AccountLevel;

		public IrcUser(IrcClient irc, string nick, IrcChannel chan)
			: this(irc)
		{
			m_nick = nick;
			m_comChans.Add(chan.Name, chan);
			irc.OnUserEncountered(this);
		}

		public IrcUser(IrcClient irc, string mask)
			: this(irc)
		{
			Parse(mask);
			irc.OnUserEncountered(this);
		}

		#region Public Accessors

		/// <summary>
		/// The IrcClient instance which this User belongs to.
		/// </summary>
		public IrcClient IrcClient
		{
			get { return m_irc; }
		}

		/// <summary>
		/// Shows wether or not this User'str full mask could have parsed yet.
		/// </summary>
		public bool IsParsed
		{
			get { return m_isParsed; }
		}

		/// <summary>
		/// The nickname of this User.
		/// </summary>
		public string Nick
		{
			get { return m_nick; }
			internal set { m_nick = value; }
		}

		/// <summary>
		/// The username of this User.
		/// </summary>
		public string UserName
		{
			get { return m_UserName; }
		}

		/// <summary>
		/// The name that this user used to identify him/her-self
		/// with the network. On some networks this is different from the actual UserName.
		/// </summary>
		public string AuthName
		{
			get { return m_authName; }
			set {
				if (m_authName != value)
				{
					m_authName = value;
					if (!string.IsNullOrEmpty(value))
					{
						IrcClient.AuthNotify(this);
					}
				}
			}
		}

		/// <summary>
		/// Authenticated Users have AuthName set.
		/// </summary>
		public bool IsAuthenticated
		{
			get { return !string.IsNullOrEmpty(m_authName); }
		}

		/// <summary>
		/// The hostmask that this User connected from (might be scrambled or overridden with a fake one)
		/// </summary>
		public string Host
		{
			get { return m_host; }
		}

		/// <summary>
		/// The full mask of this User (NICK!NAME@HOST). If the User'str mask is not completely parsed yet,
		/// the unknown variables
		/// are replaced with "*".
		/// </summary>
		public string Mask
		{
			get { return m_nick + "!" + m_UserName + "@" + m_host; }
		}

		/// <summary>
		/// This User'str pw. Only used for the User which is represented by the current IrcClient in whom'str case
		/// it will be used as the pw of the network. Unused for other users.
		/// </summary>
		public string Pw
		{
			get { return m_pw; }
			set { m_pw = value; }
		}

		/// <summary>
		/// Additional Info string which will be shown in case of the Who reply by the Irc server for this User.
		/// </summary>
		public string Info
		{
			get { return m_info; }
		}

		/// <summary>
		/// The known Usermodes of this User.
		/// </summary>
		public string Modes
		{
			get { return m_modes; }
		}

		/// <summary>
		/// CommandsByAlias of Channels which this User is on, indexed case-insensitively by their names.
		/// </summary>
		public IDictionary<string, IrcChannel> Channels
		{
			get { return m_comChans; }
		}

		/// <summary>
		/// Indicates wether or not this User has got Identd enabled (~ user prefix)
		/// </summary>
		public bool HasIdentd
		{
			get { return m_hasIdentd; }
		}

		/// <summary>
		/// Returns the Channel with the specific name or null if the user is not on
		/// the Channel.
		/// </summary>
		public IrcChannel this[string name]
		{
			get { return m_comChans[name]; }
		}

		public virtual void Msg(object format, params object[] args)
		{
			m_irc.CommandHandler.Msg(this, format.ToString(), args);
		}

		public virtual void Notice(string line)
		{
			m_irc.CommandHandler.Notice(this, line);
		}

		public string Identifier
		{
			get { return Nick; }
		}

		/// <summary>
		/// Compares this user'str nick with the specified string case-insensitively.
		/// </summary>
		public bool Is(string nick)
		{
			return nick.Equals(m_nick, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Indicates wether or not this User is on the channel with the specified name
		/// </summary>
		public bool IsOn(string channame)
		{
			return m_comChans.ContainsKey(channame);
		}

		/// <summary>
		/// Indicates wether or not this User is on the specified channel.
		/// </summary>
		public bool IsOn(IrcChannel chan)
		{
			return IsOn(chan.Name);
		}

		/// <param name="mask">The mask that is to be matched with this User'str mask.</param>
		/// <returns>Util.IsWildmatch(Mask, mask)</returns>
		public bool Matches(string mask)
		{
			return Util.IsWildmatch(Mask, mask);
		}

        public bool IsLoggedIn
        {
            get
            {
                return m_LoggedIn;
            }
        }

        public AccountMgr.AccountLevel AccountLevel
        {
            get { return m_AccountLevel; }
        }

		#endregion

		/// <summary>
		/// Custom data associated with this IrcUser.
		/// </summary>
		public IIrcUserArgs Args
		{
			get;
			set;
		}

		#region Internal

		internal IrcUser(IrcClient irc)
		{
			m_isParsed = false;
            m_LoggedIn = false;
            m_AccountLevel = AccountMgr.AccountLevel.Guest;
			m_comChans = new Dictionary<string, IrcChannel>(StringComparer.InvariantCultureIgnoreCase);
			m_irc = irc;
			m_nick = "*";
			m_UserName = "*";
			m_host = "*";
		}

		internal void SetInfo(string username, string host, string info)
		{
			m_UserName = username;
			m_host = host;
			m_info = info;
		}

		internal void SetInfo(string nick, string username, string pw, string info)
		{
			m_UserName = username;
			m_pw = pw;
			m_info = info;
			ChangeNick(nick);
		}

        public void SetAccountLevel(AccountMgr.AccountLevel level)
        {
            m_AccountLevel = level;
        }

		internal void AddChannel(IrcChannel chan)
		{
			if (!m_comChans.ContainsKey(chan.Name))
				m_comChans.Add(chan.Name, chan);
		}

		internal void DeleteChannel(string chan)
		{
			m_comChans.Remove(chan);
		}

		internal void AddMode(string mode)
		{
			m_modes += mode;
		}

		internal void DeleteMode(string mode)
		{
			int i;
			if ((i = m_modes.IndexOf(mode)) > -1)
				m_modes.Remove(i, 1);
		}

		internal void ChangeNick(string newNick)
		{
			var oldNick = m_nick;
			if (oldNick == newNick)
			{
				return;
			}
			m_nick = newNick;
			foreach (var chan in m_comChans.Values)
			{
				chan.OnNickChange(this, oldNick);
			}
			//m_irc.Users.Remove(oldNick);
			m_irc.OnUserEncountered(this);
		}

		internal void Parse(string mask)
		{
			if (mask.IndexOf("!") == -1)
				m_nick = mask;
			else
			{
				m_nick = mask.Split('!')[0];

				try
				{
					m_UserName = mask.Split('!')[1].Split('@')[0];
					if (!m_UserName.StartsWith("~"))
						m_hasIdentd = true;
					else
					{
						m_hasIdentd = false;
						m_UserName = m_UserName.Substring(1);
					}
				}
				catch (Exception)
				{
					return;
				}

				try
				{
					m_host = mask.Split('@')[1];
				}
				catch (Exception)
				{
					return;
				}
			}

			m_isParsed = true;
			m_irc.OnUserParsed(this);
		}

		#endregion

		#region Misc methods for IComparable / ToString()

		public int CompareTo(Object o)
		{
			var u = (IrcUser)o;
			return Mask.CompareTo(u.Mask);
		}

		public IEnumerator GetEnumerator()
		{
			return m_comChans.Values.GetEnumerator();
		}

		/// <returns>The nick of this user.</returns>
		public override string ToString()
		{
			return m_nick;
		}

		#endregion
	}
}