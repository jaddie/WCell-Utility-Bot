using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Squishy.Network;
using Squishy.Irc.Protocol;

namespace Squishy.Irc
{
	/// <summary>
	/// A Client instance represents the connection to a server.
	/// It is working fully asynchronously and has a built-in excess flood protection.
	/// For configuring that protection, look into the ThrottledSendQueue class.
	/// </summary>
	/// TODO: Use a stream for sending.
	public class Client : Connection
	{
		#region Fields

		//Socket m_sock;
		private readonly IrcClient m_irc;
		private readonly ThrottledSendQueue m_queue;
		private string m_addr;
		private int m_port;

		#endregion

		internal Client(IrcClient irc)
		{
			m_irc = irc;
			m_queue = new ThrottledSendQueue();
			m_queue.Dequeued += SendLineNow;
			m_addr = "";

			Connecting += OnConnecting;
			Connected += OnConnect;
			ConnectFailed += OnConnectFail;
			Disconnecting += OnDisconnecting;
		}

		//public void BeginCheckStatus(string addr, int port) {
		//    // create connection
		//    Connection checkCon = new Connection();

		//    // set events
		//    checkCon.Connected += delegate(Connection con) {
		//        // execute code when successfully connected to the server

		//        con.Disconnect();			// disconnect afterwards because we don't want to do anything else
		//    };

		//    checkCon.ConnectFailed += delegate(Connection con, Exception ex) {
		//        // execute code when server cannot be connected to

		//    };

		//    // connect
		//    checkCon.BeginConnect(addr, port);
		//}

		/// <summary>
		/// Returns the ThrottledSendQueue which is used by Send(string).
		/// </summary>
		public ThrottledSendQueue SendQueue
		{
			get { return m_queue; }
		}

		/// <summary>
		/// The remote (server'str) address which this Client'str socket is going to or already 
		/// connected to.
		/// </summary>
		public string RemoteAddress
		{
			get { return m_addr; }
		}

		/// <summary>
		/// The remote (server'str) port which this Client'str socket is going to or already 
		/// connected to.
		/// </summary>
		public int RemotePort
		{
			get { return m_port; }
		}

		#region Connect

		public override void BeginConnect(string addr, int port)
		{
			m_addr = addr;
			m_port = port;
			base.BeginConnect(addr, port);
		}

		private void OnConnecting(Connection con)
		{
			if (Util.ExternalAddress != null)
			{
				try
				{
					// try to bind to given external address
					con.Socket.Bind(new IPEndPoint(Util.ExternalAddress, 0));
				}
				catch
				{
				}
			}
			m_irc.ConnectingNotify();
		}

		private void OnConnect(Connection con)
		{
			m_irc.ConnectNotify();

			if (m_irc.Me.Pw.Length > 0)
				SendLineNow("PASS :" + m_irc.ServerPassword);

			SendLineNow(string.Format("NICK {0}\r\n", m_irc.Me.Nick));
			SendLineNow(string.Format("USER {0} \"\" \"{1}\" :{2}", m_irc.Me.UserName, m_addr, m_irc.Me.Info));
		}

		private void OnConnectFail(Connection con, Exception e)
		{
			m_irc.ConnectFailNotify(e);
		}

		#endregion

		public void OnDisconnecting(Connection con, bool conLost)
		{
			m_queue.Clear();
			if (IsConnected)
			{
				SendNow("QUIT :" + IrcClient.QuitReason + "\n");
				Thread.Sleep(100);
			}
			m_irc.Reset();
			m_irc.DisconnectNotify(conLost);
		}

		#region Send

		/// <summary>
		/// If connected, enqueues the given text to the SendQueue.
		/// </summary>
		public void Send(string text)
		{
			if (!IsConnected)
				return;

			m_queue.Enqueue(text);
		}

		/// <summary>
		/// If connected, splits the text by line terminators, triggers the IrcClient.OnBeforeSend method
		/// and sends each line + "\r\n" immediately to the server.
		/// Throws an exception if one of the lines exceed the IRC-conform max of 512 bytes (510 + "\r\n").
		/// </summary>
		/// <param name="text">A string that will be immediately sent.</param>
		public override void SendNow(string text)
		{
			if (!IsConnected)
				return;

			if (text == null)
				throw new ArgumentNullException("Text to be sent may not be null.");

			string[] lines = Regex.Split(text, "[\r\n]+");
			for (int i = 0; i < lines.Length; i++)
			{
				string line = lines[i].Trim();
				if (line.Length > 0)
					SendLineNow(line);
			}
		}

		internal void SendLineNow(string line)
		{
			if (!IsConnected)
				return;

			while (line.Length > IrcProtocol.MaxLineLength)
			{
				SendLineNow(line.Substring(0, IrcProtocol.MaxLineLength));
				line = line.Substring(IrcProtocol.MaxLineLength);
			}

			m_irc.BeforeSendNotify(line);
			base.SendNow(line + "\r\n");
		}

		#endregion
	}
}