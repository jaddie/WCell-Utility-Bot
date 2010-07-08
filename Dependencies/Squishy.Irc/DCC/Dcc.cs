using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Squishy.Irc.Commands;
using Squishy.Network;

namespace Squishy.Irc.Dcc
{
	/// <summary>
	/// Wrapper class for handling all kinds of Dcc events / actions.
	/// TODO: Maybe make it possible to have more than one transfer for the same file?
	/// TODO: Fileserver
	/// </summary>
	public class Dcc
	{
		private static bool m_acceptChat = true, m_acceptTransfer = true;
		private static TimeSpan m_defaultTimeout = TimeSpan.FromMinutes(1);
		private readonly IDictionary<IrcUser, DccChatClient> chatCons;
		private readonly IrcClient m_irc;
		private readonly IDictionary<IrcUser, List<DccTransferClient>> transferCons;

		internal Dcc(IrcClient irc)
		{
			m_irc = irc;
			chatCons = new Dictionary<IrcUser, DccChatClient>();
			transferCons = new Dictionary<IrcUser, List<DccTransferClient>>();
		}

		/// <summary>
		/// The IrcClient which this Dcc instance belongs to.
		/// </summary>
		public IrcClient IrcClient
		{
			get { return m_irc; }
		}

		/// <summary>
		/// A Collection of all active DccTransferClients.
		/// </summary>
		public ICollection<List<DccTransferClient>> TransferClients
		{
			get { return transferCons.Values; }
		}

		/// <summary>
		/// A Dictionary with all active DccChatClients indexed by the users 
		/// who the specific Chat-sessions have been established with.
		/// </summary>
		public IDictionary<IrcUser, DccChatClient> ChatClients
		{
			get { return chatCons; }
		}

		/// <summary>
		/// Determines wether or not an incoming Dcc Chat request should be accepted by default.
		/// </summary>
		public static bool AcceptChat
		{
			get { return m_acceptChat; }
			set { m_acceptChat = value; }
		}

		/// <summary>
		/// Determines wether or not an incoming Dcc Transfer request should be accepted by default.
		/// </summary>
		public static bool AcceptTransfer
		{
			get { return m_acceptTransfer; }
			set { m_acceptTransfer = value; }
		}

		/// <summary>
		/// The default timeout for establishing Dcc sessions.
		/// </summary>
		public static TimeSpan DefaultTimeout
		{
			get { return m_defaultTimeout; }
			set { m_defaultTimeout = value; }
		}

		#region Handle DCC CTCP

		internal void Handle(IrcUser user, string info)
		{
			string action = info.Split(' ')[0].ToUpper();
			string[] args = info.Substring(info.IndexOf(" ") + 1).Split(' ');
			RequestReceivedNotify(user, action, args);
			switch (action)
			{
				case "SEND":
					// SEND <filename> <longIp> <port> <size>
					HandleSend(user, args);
					break;
				case "RESUME":
					// RESUME "" <port> <position>
					HandleResume(user, args);
					break;
				case "ACCEPT":
					// ACCEPT "[<filename>]" <port> <position>
					HandleAccept(user, args);
					break;
				case "CHAT":
					// CHAT "chat" <longIp> <port>
					HandleChat(user, args);
					break;
				default:
					HandleInvalid(user, action, args);
					break;
			}
		}

		private void HandleSend(IrcUser user, string[] args)
		{
			try
			{
				string filename = args[0];
				IPAddress addr = Util.GetTcpAddress(Convert.ToInt64(args[1]));
				int port = Convert.ToInt32(args[2]);
				long size = Convert.ToInt32(args[3]);
				var endPoint = new IPEndPoint(addr, port);
				var receiveInfo = new DccReceiveArgs(user, filename, endPoint, size);
				SendRequestedNotify(receiveInfo);
				if (receiveInfo.Accept)
					StartReceive(user, new FileInfo(receiveInfo.FileName), endPoint, size, receiveInfo.Timeout);
			}
			catch (Exception)
			{
				HandleInvalid(user, "SEND", args);
			}
		}

		private void HandleAccept(IrcUser user, string[] args)
		{
			try
			{
				int port = Convert.ToInt32(args[1]);
				long pos = Convert.ToInt64(args[2]);
				foreach (var clients in transferCons.Values)
				{
					foreach (DccTransferClient client in clients)
					{
						if (client.RemoteEndPoint != null && client.RemoteEndPoint.Port == port)
						{
							((DccReceiveClient)client).Accept(pos);
						}
					}
				}
			}
			catch (Exception)
			{
				HandleInvalid(user, "ACCEPT", args);
			}
		}

		private void HandleResume(IrcUser user, string[] args)
		{
			try
			{
				var port = Convert.ToInt32(args[1]);
				var pos = Convert.ToInt64(args[2]);
				DccSendClient client = null;
				foreach (var clients in transferCons.Values)
				{
					foreach (var cli in clients)
					{
						client = cli as DccSendClient;
						if (client != null && client.Listener != null && client.Listener.LocalEndPoint.Port == port)
						{
							client.SetPos(pos);
							break;
						}
					}
				}
				if (client != null)
				{
					m_irc.CommandHandler.DccRequest(user.Nick, string.Format(
						"ACCEPT \"{0}\" {1} {2}",
						client.File.Name,
						port,
						pos));
				}
			}
			catch (Exception)
			{
				HandleInvalid(user, "RESUME", args);
			}
		}

		private void HandleChat(IrcUser user, string[] args)
		{
			try
			{
				long ip = Convert.ToInt64(args[1]);
				int port = Convert.ToInt32(args[2]);
				var endPoint = new IPEndPoint(Util.GetTcpAddress(ip), port);
				var chatArgs = new DccChatArgs(user, endPoint);
				ChatRequestedNotify(chatArgs);
				if (chatArgs.Accept)
				{
					var client = new DccChatClient(this, user, endPoint);
				}
			}
			catch (Exception)
			{
				HandleInvalid(user, "CHAT", args);
			}
		}

		private void HandleInvalid(IrcUser user, string command, string[] args)
		{
			InvalidRequestNotify(user, command, args);
		}

		#endregion

		#region Internal Management

		internal void AddTransferClient(DccTransferClient client)
		{
			List<DccTransferClient> clients = transferCons[client.User];
			if (clients == null)
			{
				clients = new List<DccTransferClient>();
				transferCons.Add(client.User, clients);
			}
			clients.Add(client);
		}

		internal void RemoveTransferClient(DccTransferClient client)
		{
			transferCons.Remove(client.User);
		}

		internal void AddChatClient(DccChatClient client)
		{
			if (chatCons.ContainsKey(client.User))
				throw new ArgumentException(
					"Chat already has been established",
					"DccChatClient with " + client.User
					);
			else
				chatCons.Add(client.User, client);
		}

		internal void RemoveChatClient(DccChatClient client)
		{
			if (chatCons.ContainsKey(client.User))
				chatCons.Remove(client.User);
		}

		#endregion

		#region Events

		#region Delegates

		public delegate void BytesTransferredHandler(DccTransferClient client, int amount);

		public delegate void ChatClosedHandler(DccChatClient client);

		public delegate void ChatEstablishedHandler(DccChatClient client);

		public delegate void ChatFailedHandler(DccChatClient client, Exception ex);

		public delegate void ChatMessageReceivedHandler(DccChatClient client, StringStream text);

		/// <param name="args">The DccChatArgs instance which provides customizability for handling this Request</param>
		public delegate void ChatRequestHandler(DccChatArgs args);

		/// <param name="user">The user who sent the request.</param>
		/// <param name="request">A string representing the Dcc request.</param>
		/// <param name="args">The following arguments seperated by space (" ").</param>
		public delegate void InvalidHandler(IrcUser user, string request, string[] args);

		public delegate void ListenerFailedHandler(DccListener serv, Exception ex);

		public delegate void ListenerTimeoutHandler(DccListener listener);

		public delegate void ReceiveTimeoutHandler(DccReceiveClient client);

		/// <summary>
		/// A delegate for handling the RequestReceived event.
		/// </summary>
		/// <param name="user">The user who sent the request.</param>
		/// <param name="request">A string representing the Dcc request.</param>
		/// <param name="args">The following arguments seperated by space (" ").</param>
		public delegate void RequestHandler(IrcUser user, string request, string[] args);

		/// <summary>
		/// A Handler for handling the SendRequested event.
		/// </summary>
		/// <param name="args">The DccReceiveArgs instance which provides customizability for handling this Request</param>
		public delegate void SendRequestHandler(DccReceiveArgs args);

		public delegate void TransferDoneHandler(DccTransferClient client);

		public delegate void TransferEstablishedHandler(DccTransferClient client);

		public delegate void TransferFailedHandler(DccTransferClient client, Exception ex);

		#endregion

		/// <summary>
		/// Fires when the Client receives any kind of Dcc request from a User.
		/// </summary>
		public event RequestHandler RequestReceived;

		internal void RequestReceivedNotify(IrcUser user, string request, string[] args)
		{
			if (RequestReceived != null)
				RequestReceived(user, request, args);
		}

		/// <summary>
		/// Fires when the Client receives a Dcc request from a User to send a file to you.
		/// </summary>
		public event SendRequestHandler SendRequested;

		internal void SendRequestedNotify(DccReceiveArgs args)
		{
			if (SendRequested != null)
				SendRequested(args);
		}

		/// <summary>
		/// Fires when the Client receives a request from a User to establish a direct chat.
		/// </summary>
		public event ChatRequestHandler ChatRequest;

		internal void ChatRequestedNotify(DccChatArgs args)
		{
			if (ChatRequest != null)
				ChatRequest(args);
		}

		/// <summary>
		/// Fires when the Client receives a Dcc request which it cannot handle.
		/// </summary>
		public event InvalidHandler InvalidRequest;

		internal void InvalidRequestNotify(IrcUser user, string request, string[] args)
		{
			if (InvalidRequest != null)
				InvalidRequest(user, request, args);
		}

		/// <summary>
		/// Fires when the Listener timeouts before a User is connected after a specific request
		/// has been sent.
		/// </summary>
		public event ListenerTimeoutHandler ListenerTimeout;

		internal void ListenerTimeoutNotify(DccListener listener)
		{
			if (ListenerTimeout != null)
				ListenerTimeout(listener);
		}

		/// <summary>
		/// Fires when the Listener raises an Exception while waiting for an incoming connection.
		/// </summary>
		public event ListenerFailedHandler ListenerFailed;

		internal void ListenerFailedNotify(DccListener serv, Exception ex)
		{
			if (ListenerFailed != null)
				ListenerFailed(serv, ex);
		}

		/// <summary>
		/// Fires when a new TransferClient has been established after a User connected to a 
		/// DccSendListener or an incoming Dcc request has been accepted and the the DccReceiveClient
		/// has successfully connected to the remote side.
		/// </summary>
		public event TransferEstablishedHandler TransferEstablished;

		internal void TransferEstablishedNotify(DccTransferClient client)
		{
			if (TransferEstablished != null)
				TransferEstablished(client);
		}

		/// <summary>
		/// Fires when a TransferClient transferred the corresbonding bytes.
		/// </summary>
		public event BytesTransferredHandler BytesTransferred;

		internal void BytesTransferredNotify(DccTransferClient client, int amount)
		{
			if (TransferEstablished != null)
				BytesTransferred(client, amount);
		}

		/// <summary>
		/// Fires when a ReceiveClient sent a Resume request and timeouts before a user sent the Accept 
		/// acknowlegement.
		/// </summary>
		public event ReceiveTimeoutHandler ReceiveTimeout;

		internal void ReceiveTimeoutNotify(DccReceiveClient client)
		{
			if (ReceiveTimeout != null)
				ReceiveTimeout(client);
		}

		/// <summary>
		/// Fires when a TransferClient is done with transfering a file.
		/// </summary>
		public event TransferDoneHandler TransferDone;

		internal void TransferDoneNotify(DccTransferClient client)
		{
			if (TransferDone != null)
				TransferDone(client);
		}

		/// <summary>
		/// Fires when the TransferClient raises an Exception during the Send or Receive operation.
		/// Does not fire when the Socket has been disposed by the Shutdown() method.
		/// (For example when the timeout has been hit.)
		/// </summary>
		public event TransferFailedHandler TransferFailed;

		internal void TransferFailedNotify(DccTransferClient client, Exception ex)
		{
			if (TransferFailed != null)
				TransferFailed(client, ex);
		}

		/// <summary>
		/// Fires when a new ChatClient has been established.
		/// </summary>
		public event ChatEstablishedHandler ChatEstablished;

		internal void ChatEstablishedNotify(DccChatClient client)
		{
			if (ChatEstablished != null)
				ChatEstablished(client);
		}

		/// <summary>
		/// Fires when a line of text has been sent to the specific DccChatClient.
		/// </summary>
		public event ChatMessageReceivedHandler ChatMessageReceived;

		internal void ChatMessageReceivedNotify(DccChatClient client, StringStream text)
		{
			if (ChatMessageReceived != null)
				ChatMessageReceived(client, text);

			if (m_irc.TriggersCommand(client.User, null, text))
			{
				m_irc.CommandHandler.ReactTo(new DccChatCmdTrigger(text, client.User));
			}
		}

		/// <summary>
		/// Fires when a Chat has been closed.
		/// </summary>
		public event ChatClosedHandler ChatClosed;

		internal void ChatClosedNotify(DccChatClient client)
		{
			if (ChatClosed != null)
				ChatClosed(client);
		}

		/// <summary>
		/// Fires when the TransferClient raises an Exception during the Send or Receive operation.
		/// Does not fire when the Socket has been disposed by the Shutdown() method.
		/// </summary>
		public event ChatFailedHandler ChatFailed;

		internal void ChatFailedNotify(DccChatClient client, Exception ex)
		{
			if (ChatFailed != null)
				ChatFailed(client, ex);
		}

		#endregion

		#region Execute DCC Actions

		/// <summary>
		/// Sends a Dcc Send request to the specified target and establishs a DccSendListener with the
		/// default timeout to listen for the incoming Chat Session.
		/// </summary>
		/// <param name="target">The nick of the user who should receive a file.</param>
		/// <param name="filename">The name of the file which should be sent.</param>
		public DccSendClient Send(string target, string filename, int port)
		{
			return Send(target, filename, m_defaultTimeout, port);
		}

		/// <summary>
		/// Sends a Dcc Send request to the specified target and establishs a DccSendListener with a custom
		/// timeout to listen for a user in order to start a file transfer.
		/// </summary>
		/// <param name="target">The nick of the user who should receive a file.</param>
		/// <param name="timeout">Specifies how long the listener should wait for an incoming receiver.</param>
		/// <param name="filename">The name of the file which should be sent.</param>
		/// <returns>A DccSendClient instance that is supposed to send the corresponding file.</returns>
		public DccSendClient Send(string target, string filename, TimeSpan timeout, int port)
		{
			var user = m_irc.GetUser(target);
			if (user == null)
			{
				user = new IrcUser(m_irc, target);
				m_irc.OnUserEncountered(user);
			}
			var file = new FileInfo(filename);
			if (!file.Exists)
			{
				throw new FileNotFoundException("The file does not exist", filename);
			}

			var client = new DccSendClient(this, user, file, timeout, port);
			m_irc.CommandHandler.DccRequest(target,
									  "SEND {0} {1} {2} {3}",
									  file.Name.Replace(" ", "_"),
									  Util.GetTcpAddress(Util.ExternalAddress),
									  client.Listener.Port,
									  file.Length);
			return client;
		}

		/// <summary>
		/// Sends a Dcc Chat request to the specified target and establishs a DccChatClient and a 
		/// corresponding Listener with the default timeout (1 Minute) to listen for the incoming 
		/// chat session.
		/// </summary>
		/// <param name="target">The nick of the user who a chat session is supposed to be established with.</param>
		public DccChatClient Chat(string target, int port)
		{
			return Chat(target, m_defaultTimeout, port);
		}

		/// <summary>
		/// Sends a Dcc Chat request to the specified target and establishs a DccChatClient and a 
		/// corresponding Listener with a custom timeout to listen for the incoming Chat Session.
		/// </summary>
		/// <param name="target">The nick of the user who should receive a file.</param>
		/// <param name="timeout">Specifies how long the listener should wait for an incoming receiver.</param>
		public DccChatClient Chat(string target, TimeSpan timeout, int port)
		{
			IrcUser user = m_irc.GetUser(target);
			if (user == null)
				user = new IrcUser(m_irc, target);
			var client = new DccChatClient(this, user, timeout, port);
			m_irc.CommandHandler.DccRequest(target,
									  string.Format("CHAT \"chat\" {0} {1}",
													Util.GetTcpAddress(Util.ExternalAddress),
													client.Listener.LocalEndPoint.Port));
			return client;
		}

		private void StartReceive(IrcUser user, FileInfo dest, IPEndPoint endPoint, long size, TimeSpan timeout)
		{
			var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			DccReceiveClient client;
			if (dest.Exists)
			{
				client = new DccReceiveClient(this, user, s, endPoint, dest, size, timeout);
				IrcClient.CommandHandler.DccRequest(user.Nick, string.Format(
															"RESUME \"\" {0} {1}",
															endPoint.Port,
															dest.Length
															));
			}
			else
			{
				client = new DccReceiveClient(this, user, s, endPoint, dest, size);
			}
		}

		#endregion

		/// <summary>
		/// Returns an array of DccTransferClients that are established with the corresponding user.
		/// </summary>
		public List<DccTransferClient> GetTransferClients(IrcUser user)
		{
			if (user != null)
			{
				List<DccTransferClient> clients;
				transferCons.TryGetValue(user, out clients);
				return clients;
			}
			return null;
		}

		/// <summary>
		/// Returns a DccTransferClient that is established with the given user and
		/// transferring the specific file or null.
		/// </summary>
		public DccTransferClient GetTransferClient(IrcUser user, string filename)
		{
			List<DccTransferClient> clients = transferCons[user];
			foreach (DccTransferClient client in clients)
			{
				bool isWin = Environment.OSVersion.Platform.Equals("Windows");
				// *nix has a case-sensitive file-system, windows doesn't
				if (client.File.Name.Equals(filename, isWin
														? StringComparison.InvariantCultureIgnoreCase
														:
															StringComparison.InvariantCulture))
				{
					return client;
				}
			}
			return null;
		}

		/// <summary>
		/// Returns a DccChatClient that is established with the user who has the corresponding nick.
		/// </summary>
		public DccChatClient GetChatClient(string nick)
		{
			return GetChatClient(m_irc.GetUser(nick));
		}

		public DccChatClient GetChatClient(IrcUser user)
		{
			if (user != null)
			{
				DccChatClient client;
				chatCons.TryGetValue(user, out client);
				return client;
			}
			return null;
		}
	}
}