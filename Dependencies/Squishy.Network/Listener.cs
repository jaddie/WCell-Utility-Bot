using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace Squishy.Network
{
	public class Listener
	{
		/// <summary>
		/// Local address for the server to bind to. Usually there is no need to change this.
		/// </summary>
		public static IPAddress BindAddress = new IPAddress(new byte[] {0, 0, 0, 0});

		private readonly object acceptLock = new object();
		private bool closing;

		private Socket listener;
		private int port = -1;

		/// <summary>
		/// The that this listener is (or was - in case that the Listener is closed) listening to.
		/// </summary>
		public int Port
		{
			get { return port; }
		}

		/// <summary>
		/// Wether this Listener is listening or not.
		/// </summary>
		public bool Listening
		{
			get { return listener != null && listener.IsBound; }
		}

		/// <summary>
		/// Starts listening on the given port.
		/// </summary>
		public void BeginListen(int port)
		{
			lock (acceptLock)
			{
				// someone might close the connection while setting up this Listener
				this.port = port;
				listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				try
				{
					listener.Bind(new IPEndPoint(BindAddress, port));
					listener.Listen(10);
					listener.BeginAccept(new AsyncCallback(OnAccept), listener);
				}
				catch (Exception e)
				{
					ListenFailedNotify(e);
				}
			}
		}

		/// <summary>
		/// Shuts this Listener down - if still listening.
		/// </summary>
		public void Close()
		{
			lock (acceptLock)
			{
				if (!closing && Listening)
				{
					closing = true;
					try
					{
						listener.Close();
					}
					catch (IOException)
					{
					}
					CloseNotify();
					closing = false;
				}
			}
		}

		/// <summary>
		/// Is called, once a client connected to this listener.
		/// </summary>
		private void OnAccept(IAsyncResult ar)
		{
			lock (acceptLock)
			{
				if (listener != null && listener.IsBound)
				{
					Socket s = null;
					try
					{
						s = listener.EndAccept(ar);
					}
					catch (ObjectDisposedException)
					{
						// server has been closed
						return;
					}
					catch (Exception e)
					{
						ListenFailedNotify(e);
					}

					if (s != null)
					{
						AcceptNotify(new Connection(s));
					}

					try
					{
						listener.BeginAccept(new AsyncCallback(OnAccept), listener);
					}
					catch (ObjectDisposedException)
					{
					} // might have been closed during notification
				}
			}
		}

		#region Events

		#region Delegates

		public delegate void AcceptHandler(Listener server, Connection client);

		/// <summary>
		/// Is raised when the server has been shutdown.
		/// </summary>
		public delegate void ClosedHandler(Listener server);

		public delegate void ListenFailedHandler(Listener server, Exception e);

		#endregion

		/// <summary>
		/// Is raised when the server cannot start listening (the port is probably occupied).
		/// </summary>
		public event ListenFailedHandler ListenFailed;

		private void ListenFailedNotify(Exception e)
		{
			Close();
			if (ListenFailed != null)
			{
				ListenFailed(this, e);
			}
		}

		/// <summary>
		/// Is raised when a new Connection has been accepted.
		/// </summary>
		public event AcceptHandler Accept;

		private void AcceptNotify(Connection client)
		{
			if (Accept != null)
			{
				Accept(this, client);
			}
		}

		public event ClosedHandler Closed;

		private void CloseNotify()
		{
			if (Closed != null)
			{
				Closed(this);
			}
		}

		#endregion
	}
}