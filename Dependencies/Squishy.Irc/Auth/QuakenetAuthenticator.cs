using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squishy.Irc.Protocol;

namespace Squishy.Irc.Auth
{
	public class QuakenetAuthenticator : AsyncIrcAuthenticator
	{
		/// <summary>
		/// Server 330 Me UserNick UserAuth :is authed as
		/// </summary>
		public override string AuthOpcode
		{
			get { return "330"; }
		}

		public override string ServiceName
		{
			get { return "Q"; }
		}

		public override void ResolveAuth(IrcUser user, IrcUserAuthResolvedHandler authResolvedHandler)
		{
			user.IrcClient.CommandHandler.Whois(user.Nick);
			base.ResolveAuth(user, authResolvedHandler);
		}

		protected override string ResolveAuth(IrcUser user, IrcPacket packet)
		{
			packet.Content.SkipWord();

			var nick = packet.Content.NextWord();
			if (nick == user.Nick)
			{
				var userName = packet.Content.NextWord();
				return userName;
			}
			return null;
		}
	}
}
