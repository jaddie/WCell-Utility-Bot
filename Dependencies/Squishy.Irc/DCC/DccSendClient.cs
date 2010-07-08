using System;
using System.IO;
using System.Net.Sockets;

namespace Squishy.Irc.Dcc
{
	public class DccSendClient : DccTransferClient
	{
		public DccSendClient(Dcc dcc, IrcUser user, FileInfo file, TimeSpan timeout, int port) : base(dcc, user, file, 0, file.Length)
		{
			m_listener = new DccListener(this, timeout, port);
		}

		internal void SetPos(long pos)
		{
			m_startPos = pos;
		}

		internal override void Start()
		{
			try
			{
				m_fstream = m_file.OpenRead();
				m_fstream.Position = StartPosition;
			}
			catch (Exception ex)
			{
				Dcc.TransferFailedNotify(this, ex);
				Shutdown();
				return;
			}
			Dcc.TransferEstablishedNotify(this);
			Transfer();
		}

		protected override void DoTransfer()
		{
			var buf = new byte[8192];
			int n = 0;
			try
			{
				n = m_fstream.Read(buf, 0, buf.Length);
			}
			catch (Exception ex)
			{
				Dcc.TransferFailedNotify(this, ex);
				Shutdown();
				return;
			}

			if (n > 0)
				try
				{
					m_sock.BeginSend(buf, 0, n, SocketFlags.None, new AsyncCallback(OnSend), null);
				}
				catch (Exception ex)
				{
					Dcc.TransferFailedNotify(this, ex);
					Shutdown();
				}

			else
				Dcc.TransferDoneNotify(this);
		}

		private void OnSend(IAsyncResult ar)
		{
			if (m_closed)
				return;

			try
			{
				int n = m_sock.EndSend(ar);
				OnBytesTransfered(n);
			}
			catch (Exception ex)
			{
				Dcc.TransferFailedNotify(this, ex);
				Shutdown();
			}
			Transfer();
		}
	}
}