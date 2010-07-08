using System;
using System.IO;
using System.Net.Sockets;

namespace Squishy.Irc.Dcc
{
	public abstract class DccTransferClient : DccClient
	{
		private readonly long m_totalLength;
		protected long m_bytesTransfered;
		protected FileInfo m_file;
		protected FileStream m_fstream;
		protected long m_startPos;
		private bool m_suspended;

		public DccTransferClient(Dcc dcc, IrcUser user, Socket sock, FileInfo file, long startPos, long totalLength)
			: base(dcc, user, sock)
		{
			m_file = file;
			m_startPos = startPos;
			m_totalLength = totalLength;
			Dcc.AddTransferClient(this);
		}

		public DccTransferClient(Dcc dcc, IrcUser user, FileInfo file, long startPos, long totalLength) : base(dcc, user)
		{
			m_file = file;
			m_startPos = startPos;
			m_totalLength = totalLength;
			Dcc.AddTransferClient(this);
		}

		/// <summary>
		/// The FileInfo instance, representing the file that is being transferred.
		/// </summary>
		public FileInfo File
		{
			get { return m_file; }
		}

		/// <summary>
		/// The amount of bytes transfered during the current send/receive operation.
		/// </summary>
		public long BytesTransfered
		{
			get { return m_bytesTransfered; }
		}

		/// <summary>
		/// The Position within the file from where the transfer operation started.
		/// </summary>
		public long StartPosition
		{
			get { return m_startPos; }
		}

		/// <summary>
		/// The total size of the file.
		/// </summary>
		public long TotalLength
		{
			get { return m_totalLength; }
		}

		/// <summary>
		/// The current speed in bytes/second.
		/// </summary>
		public int BytesPerSecond
		{
			get { return Convert.ToInt32(m_bytesTransfered/(float) (DateTime.Now - StartTime).TotalSeconds); }
		}

		/// <summary>
		/// Indicates wether or not the transfering Thread is currently suspended.
		/// </summary>
		public bool Suspended
		{
			get { return m_suspended; }
		}

		/// <summary>
		/// Suspends the Transfer.
		/// </summary>
		public void Suspend()
		{
			m_suspended = true;
		}

		/// <summary>
		/// Resumes the Transfer.
		/// </summary>
		public void Resume()
		{
			m_suspended = false;
			Transfer();
		}

		/// <summary>
		/// Toggles the current suspense state.
		/// </summary>
		public void ToggleSuspend()
		{
			m_suspended = !m_suspended;
			Transfer();
		}

		protected abstract void DoTransfer();

		protected void Transfer()
		{
			if (!m_suspended && !m_closed)
			{
				DoTransfer();
			}
		}

		protected void OnBytesTransfered(int n)
		{
			m_bytesTransfered += n;
			Dcc.BytesTransferredNotify(this, n);
		}

		protected override void Cleanup()
		{
			if (m_fstream != null)
				m_fstream.Close();
			Dcc.RemoveTransferClient(this);
		}
	}
}