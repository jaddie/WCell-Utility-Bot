using System.Net;

namespace Squishy.Irc.Dcc
{
	public class DccChatArgs
	{
		/// <summary>
		/// The EndPoint where the ChatClient should connect to.
		/// </summary>
		public readonly IPEndPoint RemoteEndPoint;

		/// <summary>
		/// The User who requests the chat session.
		/// </summary>
		public readonly IrcUser User;

		/// <summary>
		/// Determines wether or not a ChatClient shall be established.
		/// </summary>
		public bool Accept;


		internal DccChatArgs(IrcUser user, IPEndPoint endPoint)
		{
			User = user;
			RemoteEndPoint = endPoint;
			Accept = Dcc.AcceptChat;
		}
	}
}