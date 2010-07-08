using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Squishy.Irc.Auth
{
	public delegate void IrcUserAuthResolvedHandler(IrcUser user);

	public interface IIrcAuthenticator : IDisposable
	{
		/// <summary>
		/// Name of the Authentication-service (eg NickServ)
		/// </summary>
		string ServiceName { get; }

		bool ResolvesInstantly { get; }

		bool IsResolving(IrcUser user);

		void Init(IrcClient client);

		/// <summary>
		/// Actual name that the user used to login with.
		/// </summary>
		/// <param name="user"></param>
		/// <param name="authResolvedHandler"></param>
		/// <returns></returns>
		void ResolveAuth(IrcUser user, IrcUserAuthResolvedHandler authResolvedHandler);
	}
}
