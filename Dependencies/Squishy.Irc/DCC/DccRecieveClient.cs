using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Timers;

namespace Squishy.Irc.Dcc
{
	public class DccReceiveClient : DccTransferClient
	{
		private readonly IPEndPoint m_remoteAddr;
		private readonly Timer m_timeoutTimer;
		private bool m_accepted;


		public DccReceiveClient(Dcc dcc, IrcUser user, Socket sock, IPEndPoint addr, FileInfo file, long totalSize) :
			base(dcc, user, sock, file, 0, totalSize)
		{
			m_remoteAddr = addr;
			Accept(0);
		}

		public DccReceiveClient(Dcc dcc, IrcUser user, Socket sock, IPEndPoint addr, FileInfo file, long totalSize,
								TimeSpan timeout)
			: base(dcc, user, sock, file, 0, totalSize)
		{
			m_remoteAddr = addr;
			// m_sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 10000)
			m_timeoutTimer = new Timer((int)timeout.TotalMilliseconds);
			m_timeoutTimer.Elapsed += OnTimeout;
			m_timeoutTimer.AutoReset = false;
			m_timeoutTimer.Start();
		}

		/// <summary>
		/// The remote IPEndPoint where this ReceiveClient is supposed to connect to or already 
		/// connected to.
		/// </summary>
		public override IPEndPoint RemoteEndPoint
		{
			get { return m_remoteAddr; }
		}

		internal void Accept(long pos)
		{
			if (m_closed)
				return;

			m_accepted = true;
			m_startPos = pos;
			if (!m_sock.Connected)
				try
				{
					m_sock.BeginConnect(m_remoteAddr, OnConnect, null);
					return;
				}
				catch (Exception e)
				{
					Dcc.TransferFailedNotify(this, e);
					Shutdown();
					return;
				}
			AfterConnect();
		}

		private void OnConnect(IAsyncResult ar)
		{
			if (m_closed)
				return;

			try
			{
				m_sock.EndConnect(ar);
			}
			catch (Exception e)
			{
				Dcc.TransferFailedNotify(this, e);
				Shutdown();
				return;
			}
			AfterConnect();
		}

		private void AfterConnect()
		{
			if (m_closed)
				return;
			try
			{
				m_fstream = new FileStream(m_file.FullName, FileMode.Append, FileAccess.Write, FileShare.Read);
				m_fstream.Position = StartPosition;
			}
			catch (Exception e)
			{
				Dcc.TransferFailedNotify(this, e);
				Shutdown();
				return;
			}
			Dcc.TransferEstablishedNotify(this);
			Transfer();
		}

		protected override void DoTransfer()
		{
			try
			{
				var buf = new byte[4096];
				if ((m_bytesTransfered + StartPosition) < TotalLength && m_sock.Connected)
				{
					m_sock.BeginReceive(buf, 0, buf.Length, SocketFlags.None, new AsyncCallback(OnReceive), buf);
					return;
				}
			}
			catch (Exception e)
			{
				Dcc.TransferFailedNotify(this, e);
				Shutdown();
				return;
			}
			Dcc.TransferDoneNotify(this);
		}

		private void OnReceive(IAsyncResult ar)
		{
			if (m_closed)
				return;

			try
			{
				var buf = (byte[])ar.AsyncState;
				int n = m_sock.EndReceive(ar);
				if (n < 1)
					throw new Exception("Connection has been closed remotewise.");
				m_fstream.Write(buf, 0, n);
				OnBytesTransfered(n);
			}
			catch (Exception e)
			{
				Dcc.TransferFailedNotify(this, e);
				Shutdown();
				return;
			}
			Transfer();
		}

		private void OnTimeout(object sender, ElapsedEventArgs info)
		{
			if (!m_accepted)
			{
				Dcc.ReceiveTimeoutNotify(this);
				Shutdown();
			}
			else
				m_timeoutTimer.Close();
		}

		protected override void Cleanup()
		{
			base.Cleanup();
			if (m_timeoutTimer != null && m_timeoutTimer.Enabled)
				m_timeoutTimer.Close();
		}
	}
}