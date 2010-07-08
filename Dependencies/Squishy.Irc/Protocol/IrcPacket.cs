using Squishy.Network;
using Squishy.Network.Prot;

namespace Squishy.Irc.Protocol
{
	public class IrcPacket : StringPacket
	{
		private readonly IrcClient irc;

		private readonly string origString;
		private readonly string prefix;
		private string args;
		internal IrcChannel channel;
		internal IrcProtocolHandler protHandler;

		private IrcUser user;

		public IrcPacket(IrcClient irc, string prefix, string action, StringStream content, string origString)
			: base(action, content)
		{
			this.irc = irc;
			this.prefix = prefix;
			this.origString = origString;
		}

		public IrcClient IrcClient
		{
			get { return irc; }
		}

		/// <summary>
		/// The Sender of this packet (might be not an actual user if
		/// this packet does not expect a Sender)
		/// </summary>
		public IrcUser User
		{
			get
			{
				if (user == null)
				{
					user = irc.GetOrCreateUser(prefix);
				}
				return user;
			}
			internal set { user = value; }
		}

		public string Prefix
		{
			get { return prefix; }
		}

		public IrcChannel Channel
		{
			get { return channel; }
		}

		public IrcProtocolHandler ProtHandler
		{
			get { return protHandler; }
		}

		/// <summary>
		/// Args in the IRC protocol is everything behind the first colon ':'
		/// </summary>
		public string ArgsOrFirstWord
		{
			get
			{
				var args = Args;
				if (args.Length == 0)
				{
					args = Content.NextWord();
				}
				return args;
			}
		}

		/// <summary>
		/// Args in the IRC protocol is everything behind the first colon ':'
		/// </summary>
		public string Args
		{
			get
			{
				if (args == null)
				{
					int i = content.String.IndexOf(":");
					if (i > -1)
					{
						args = content.String.Substring(i + 1);
					}
					else
					{
						args = "";
					}
				}
				return args;
			}
		}


		public override string ToString()
		{
			return origString;
		}
	}
}