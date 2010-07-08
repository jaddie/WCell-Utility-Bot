using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Squishy.Irc.Protocol;

namespace Squishy.Irc.Auth
{
	/// <summary>
	/// Represents the information required to asynchronously resolve a User
	/// </summary>
	public class IrcUserAuthQuery
	{
		public readonly IrcUser User;

		public readonly DateTime AuthStart;

		internal readonly IrcProtocol.PacketHandler Handler;

		public IrcUserAuthQuery(IrcUser user, IrcProtocol.PacketHandler handler)
		{
			User = user;
			AuthStart = DateTime.Now;
			Handler = handler;
		}
	}

	public abstract class AsyncIrcAuthenticator : IIrcAuthenticator
	{
		public static TimeSpan DefaultTimeout = TimeSpan.FromMinutes(1);

		protected readonly Dictionary<string, IrcUserAuthQuery> pendingQueries = new Dictionary<string, IrcUserAuthQuery>();

		public AsyncIrcAuthenticator()
		{
		}

		public virtual void Init(IrcClient client)
		{
		}

		public bool ResolvesInstantly
		{
			get { return false; }
		}

		public bool IsResolving(IrcUser user)
		{
			IrcUserAuthQuery query;
			if (pendingQueries.TryGetValue(user.UserName, out query))
			{
				if ((DateTime.Now - query.AuthStart) < DefaultTimeout)
				{
					return true;
				}
				else
				{
					RemoveQuery(query);
				}
			}
			return false;
		}

		public abstract string ServiceName { get; }

		public abstract string AuthOpcode { get; }

		public virtual void ResolveAuth(IrcUser user, IrcUserAuthResolvedHandler authResolvedHandler)
		{
			if (pendingQueries.ContainsKey(user.UserName))
			{
				return;
			}

			IrcUserAuthQuery query = null;
			IrcProtocol.PacketHandler handler = packet => {
				var authName = ResolveAuth(user, packet);
				if (authName != null)
				{
					user.AuthName = authName;
					RemoveQuery(query);
					authResolvedHandler(user);
				}
			};
			
			IrcProtocol.Instance.AddPacketHandler(AuthOpcode, handler);

			pendingQueries[user.UserName] = query = new IrcUserAuthQuery(user, handler);

			user.IrcClient.CommandHandler.Whois(user.Nick);
		}

		/// <summary>
		/// Returns null if not the right one
		/// </summary>
		/// <param name="packet"></param>
		/// <returns></returns>
		protected abstract string ResolveAuth(IrcUser user, IrcPacket packet);

		public void RemoveQuery(string username)
		{
			IrcUserAuthQuery query;
			if (pendingQueries.TryGetValue(username, out query))
			{
				RemoveQuery(query);
			}
		}

		/// <summary>
		/// Removes (and therefor cancels) the given query
		/// </summary>
		/// <param name="query"></param>
		public void RemoveQuery(IrcUserAuthQuery query)
		{
			IrcProtocol.Instance.RemovePacketHandler(AuthOpcode, query.Handler);
			pendingQueries.Remove(query.User.UserName);
		}

		public virtual void Dispose()
		{
			foreach (var info in pendingQueries.Values)
			{
				IrcProtocol.Instance.RemovePacketHandler(AuthOpcode, info.Handler);
			}
			pendingQueries.Clear();
		}
	}
}
