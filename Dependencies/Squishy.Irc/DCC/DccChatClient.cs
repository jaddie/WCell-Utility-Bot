using System;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Squishy.Network;

namespace Squishy.Irc.Dcc
{
	public class DccChatClient : DccClient
	{
		private IPEndPoint m_remoteEndPoint;

		internal DccChatClient(Dcc dcc, IrcUser user, TimeSpan timeout, int port) : base(dcc, user)
		{
			m_listener = new DccListener(this, timeout, port);
		}

		internal DccChatClient(Dcc dcc, IrcUser user, IPEndPoint ep) : base(dcc, user)
		{
			m_remoteEndPoint = ep;
			Start();
		}

		/// <summary>
		/// The remote IPEndPoint where this ChatClient is supposed to connect to or already 
		/// connected to or null if unknown (Listener still waiting for connection).
		/// </summary>
		public override IPEndPoint RemoteEndPoint
		{
			get { return m_remoteEndPoint; }
		}

		internal override void Start()
		{
			if (!m_sock.Connected)
			{
				try
				{
					m_sock.BeginConnect(m_remoteEndPoint, OnConnnect, null);
				}
				catch (Exception e)
				{
					Dcc.ChatFailedNotify(this, e);
					return;
				}
				Dcc.AddChatClient(this);
			}
			else
			{
				m_remoteEndPoint = (IPEndPoint) m_sock.RemoteEndPoint;
				Dcc.AddChatClient(this);
				AfterConnect();
			}
		}

		private void OnConnnect(IAsyncResult ar)
		{
			if (m_closed)
				return;
			try
			{
				m_sock.EndConnect(ar);
			}
			catch (Exception e)
			{
				Dcc.ChatFailedNotify(this, e);
				Dcc.RemoveChatClient(this);
				return;
			}
			AfterConnect();
		}

		private void AfterConnect()
		{
			Dcc.ChatEstablishedNotify(this);
			Receive();
		}

		private void Receive()
		{
			if (m_closed)
				return;
			try
			{
				var buf = new byte[1024];
				m_sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(OnReceive), buf);
			}
			catch
			{
				Shutdown();
			}
		}

		private void OnReceive(IAsyncResult ar)
		{
			if (m_closed)
				return;
			var buf = (byte[]) ar.AsyncState;
			int n = 0;
			try
			{
				n = m_sock.EndReceive(ar);
			}
			catch
			{
				n = -1;
			}

			if (n < 1)
			{
				Shutdown();
				return;
			}

			else if (n > 0)
			{
				string text = IrcClient.Encoding.GetString(buf, 0, n);
				string[] lines = Regex.Split(text, @"\n|(\r\n?)");
				for (int i = 0; i < lines.Length; i++)
				{
					string line = lines[i].Trim();
					if (line.Length <= 0)
						continue;
					Dcc.ChatMessageReceivedNotify(this, new StringStream(line));
				}
			}

			Receive();
		}

		/// <summary>
		/// Immediately sends line+"\n" to the remote side when it is connected.
		/// </summary>
		public void Send(string line)
		{
			if (!m_sock.Connected)
				return;

			try
			{
				byte[] buf = IrcClient.Encoding.GetBytes(line + "\n");
				m_sock.BeginSend(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(OnSend), null);
			}
			catch
			{
				Shutdown();
			}
		}

		private void OnSend(IAsyncResult ar)
		{
			if (m_closed)
				return;

			try
			{
				//int n = m_sock.EndSend(ar);
				m_sock.EndSend(ar);
			}
			catch
			{
				Shutdown();
			}
		}

		protected override void Cleanup()
		{
			Dcc.ChatClosedNotify(this);
			Dcc.RemoveChatClient(this);
		}
	}
}