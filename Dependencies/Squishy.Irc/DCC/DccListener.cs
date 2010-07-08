using System;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace Squishy.Irc.Dcc
{
	public class DccListener
	{
		#region Variables

		private readonly TimeSpan m_timeout;
		private readonly int m_Port;
		private readonly Timer m_timeoutTimer;
		protected DccClient m_client;
		private bool m_closed;
		protected Socket m_sock;

		#endregion

		internal DccListener(DccClient client, TimeSpan timeout, int port)
		{
			m_client = client;
			m_timeout = timeout;
			m_Port = port;
			m_closed = false;
			m_sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				try
				{
					m_sock.Bind(new IPEndPoint(Util.ExternalAddress, m_Port));
				}
				catch
				{
					m_sock.Bind(new IPEndPoint(Util.LocalHostAddress, m_Port));
				}
				m_sock.Listen(1);
				m_sock.BeginAccept(new AsyncCallback(OnAccept), null);
			}
			catch (Exception ex)
			{
				m_client.Dcc.ListenerFailedNotify(this, ex);
			}
			m_timeoutTimer = new Timer(m_timeout.TotalMilliseconds);
			m_timeoutTimer.Elapsed += OnTimeout;
			m_timeoutTimer.Start();
		}

		/// <summary>
		/// The LocalEndPoint of the listen socket.
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get { return (IPEndPoint)m_sock.LocalEndPoint; }
		}

		public int Port
		{
			get { return m_Port; }
		}

		public DccClient Client
		{
			get { return m_client; }
		}

		/// <summary>
		/// Indicates wether this DccListener is closed or still active. (true if Shutdown() has been called)
		/// </summary>
		public bool Closed
		{
			get { return m_closed; }
		}

		private void OnAccept(IAsyncResult ar)
		{
			if (m_closed)
				return;

			Socket sock = null;
			try
			{
				sock = m_sock.EndAccept(ar);
			}
			catch (Exception ex)
			{
				m_client.Dcc.ListenerFailedNotify(this, ex);
				Shutdown();
				return;
			}
			m_client.m_sock = sock;
			m_client.Start();
			Shutdown();
		}

		private void OnTimeout(object sender, ElapsedEventArgs info)
		{
			if (!m_closed)
			{
				m_client.Dcc.ListenerTimeoutNotify(this);
				if (m_client is DccTransferClient)
					m_client.Dcc.RemoveTransferClient(m_client as DccTransferClient);
				if (m_client is DccChatClient)
					m_client.Dcc.RemoveChatClient(m_client as DccChatClient);
				Shutdown();
			}
			else
				m_timeoutTimer.Close();
		}

		/// <summary>
		/// Closes this Listener and removes the corresponding DccClient.
		/// </summary>
		public void Shutdown()
		{
			if (m_closed)
				return;

			if (m_timeoutTimer.Enabled)
				m_timeoutTimer.Close();
			m_closed = true;
			m_sock.Close();
		}
	}
}