using System;
using System.Net;
using System.Net.Sockets;

namespace Squishy.Irc.Dcc
{
	public abstract class DccClient
	{
		private readonly Dcc m_dcc;
		private readonly DateTime m_startTime;
		protected bool m_closed;
		protected DccListener m_listener;
		protected internal Socket m_sock;
		protected IrcUser m_user;

		public DccClient(Dcc dcc, IrcUser user, Socket sock) : this(dcc, user)
		{
			m_sock = sock;
		}

		public DccClient(Dcc dcc, IrcUser user)
		{
			m_dcc = dcc;
			m_user = user;
			m_startTime = DateTime.Now;
			m_closed = false;
		}

		/// <summary>
		/// The User with whom this transfer has been established.
		/// </summary>
		public IrcUser User
		{
			get { return m_user; }
		}

		/// <summary>
		/// The time when this session started.
		/// </summary>
		public DateTime StartTime
		{
			get { return m_startTime; }
		}

		/// <summary>
		/// The Dcc instance to which this DccClient belongs.
		/// </summary>
		public Dcc Dcc
		{
			get { return m_dcc; }
		}

		/// <summary>
		/// Indicates wether or not this Client is connected.
		/// </summary>
		public virtual bool Connected
		{
			get { return m_sock != null && m_sock.Connected; }
		}

		/// <summary>
		/// Indicates wether this DccClient is closed or still active. (true if Shutdown() has been called)
		/// </summary>
		public bool Closed
		{
			get { return m_closed; }
		}

		/// <summary>
		/// A DccListener which is waiting for an incoming connection or null.
		/// </summary>
		public DccListener Listener
		{
			get { return m_listener; }
		}

		/// <summary>
		/// The LocalEndPoint of the Transfer connection.
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get { return (IPEndPoint) m_sock.LocalEndPoint; }
		}

		/// <summary>
		/// The RemoteEndPoint of the Transfer connection.
		/// </summary>
		public virtual IPEndPoint RemoteEndPoint
		{
			get { return m_sock != null ? (IPEndPoint) m_sock.RemoteEndPoint : null; }
		}

		internal virtual void Start()
		{
		}

		/// <summary>
		/// Calls the Cleanup() method and closes the socket.
		/// </summary>
		public void Shutdown()
		{
			if (m_sock == null || m_closed)
				return;

			m_closed = true;
			Cleanup();
			m_sock.Close();
		}

		/// <summary>
		/// Can be overridden for customized cleanup (called by Shutdown()).
		/// </summary>
		protected virtual void Cleanup()
		{
		}
	}
}