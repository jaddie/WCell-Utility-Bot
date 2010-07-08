using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Squishy.Irc.Auth
{
	public class AuthenticationMgr
	{
		public AuthenticationMgr()
		{
		}

		public IIrcAuthenticator Authenticator
		{
			get;
			set;
		}

		public bool CanResolve
		{
			get { return Authenticator != null; }
		}

		public bool ResolvesInstantly
		{
			get { return Authenticator != null && Authenticator.ResolvesInstantly; }
		}

		public bool IsResolving(IrcUser user)
		{
			return Authenticator != null && Authenticator.IsResolving(user);
		}

		public bool ResolveAuth(IrcUser user)
		{
			if (Authenticator == null)
			{
				// No Authenticator => Cannot resolve
				return false;
			}
			else
			{
				Authenticator.ResolveAuth(user, userArg => {
					if (user.IrcClient.NotifyAuthedUsers)
					{
						user.Msg("You have been authenticated as: " + user.AuthName);
					}
					var evt = AuthResolved;
					if (evt != null)
					{
						evt(userArg);
					}
				});
				return true;
			}
		}

		public event Action<IrcUser> AuthResolved;

		internal void OnNewUser(IrcUser user)
		{
			if (user.IrcClient.AutoResolveAuth && Authenticator != null)
			{
				ResolveAuth(user);
			}
		}

		internal void Cleanup()
		{
			if (Authenticator != null)
			{
				Authenticator.Dispose();
				Authenticator = null;
			}
		}
	}
}
