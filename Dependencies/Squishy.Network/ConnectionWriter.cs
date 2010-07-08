using System;
using System.IO;

namespace Squishy.Network
{
	/// <summary>
	/// The ConnectionWriter provides several methods to send continuous data from a Stream.
	/// When either the Connection disconnects, reading fails or no more data can be read, 
	/// the stream will be closed after the corresponding event has been raised if 
	/// <code>CloseStreamAfterTranscaction</code> is set to true - otherwise left open.
	/// </summary>
	public class ConnectionWriter
	{
		#region Fields

		private readonly AsyncCallback readStreamCallback;
		private readonly object streamLock = new object();

		/// <summary>
		/// Indicates wether to close the Stream when failed or finished reading.
		/// Default: true
		/// </summary>
		public bool CloseStreamAfterTransaction = true;

		private Connection con;

		private FileInfo currentFile;
		private Stream currentStream;
		private Connection.DisconnectedHandler disconnectCallback;
		private Connection.SentHandler sendCallback;
		private ByteBuffer streamBuffer;
		private bool writing;

		#endregion

		#region Setup

		public ConnectionWriter(Connection con)
			: this()
		{
			Connection = con;
		}

		public ConnectionWriter()
		{
			readStreamCallback = OnStreamRead;
		}

		#endregion

		#region Misc Props

		/// <summary>
		/// Gets or sets the underlying Connection. Throws an InvalidOperationException if trying to
		/// set the connection while writing.
		/// </summary>
		public Connection Connection
		{
			get { return con; }
			set
			{
				if (con != value)
				{
					if (writing)
						throw new InvalidOperationException(
							"You must not change the Connection while the writer is in use - Call ConnectionWriter.Stop first.");
					if (con != null)
					{
						con.InternalSent -= sendCallback;
						con.InternalDisconnected -= disconnectCallback;
					}
					disconnectCallback = OnInternalDisconnected;
					sendCallback = OnInternalSent;
					con = value;
					con.InternalSent += sendCallback;
					con.InternalDisconnected += disconnectCallback;
				}
			}
		}

		/// <summary>
		/// Indicates wether or not this ConnectionWriter is currently used to write data from a stream.
		/// </summary>
		public bool Writing
		{
			get { return writing; }
		}

		/// <summary>
		/// The file that is currently being read or null.
		/// </summary>
		public FileInfo CurrentFile
		{
			get { return currentFile; }
		}

		/// <summary>
		/// The stream that is currently read from or null if there is no writing in progress.
		/// </summary>
		public Stream CurrentStream
		{
			get { return currentStream; }
		}

		/// <summary>
		/// Indicates wether the underlying Connection is not null and Valid.
		/// </summary>
		public bool Ready
		{
			get { return con != null && con.Valid; }
		}

		#endregion

		#region Write implementation

		/// <summary>
		/// If not writing yet, opens a readonly-stream to the given file
		/// and will asynchronously send it to the remote connection.
		/// </summary>
		public void WriteFile(FileInfo file)
		{
			if (writing)
				return;
			writing = true;
			currentFile = file;
			InitWrite(file.OpenRead());
		}

		/// <summary>
		/// If not writing yet, asynchronously sends everything that can be read from this stream.
		/// </summary>
		public void WriteAll(Stream stream)
		{
			if (writing)
				return;
			writing = true;
			InitWrite(stream);
		}

		/// <summary>
		/// Stops reading and -if CloseStreamAfterTransaction is true- also closes the underlying stream.
		/// </summary>
		public void Stop()
		{
			lock (streamLock)
			{
				ResetAfterTransfer();
			}
		}

		private void InitWrite(Stream stream)
		{
			lock (streamLock)
			{
				streamBuffer = new ByteBuffer(con.SendBufferSize);
				currentStream = stream;
				ContinueReadStreamData();
			}
		}

		private void ContinueReadStreamData()
		{
			try
			{
				currentStream.BeginRead(streamBuffer.bytes, 0, streamBuffer.bytes.Length, readStreamCallback, streamBuffer.bytes);
			}
			catch (Exception e)
			{
				DataReadFailedNotify(e);
			}
		}

		private void OnStreamRead(IAsyncResult ar)
		{
			if (!Ready)
				return;

			lock (streamLock)
			{
				if (!writing)
					return;

				int n;
				try
				{
					n = currentStream.EndRead(ar);
				}
				catch (Exception e)
				{
					DataReadFailedNotify(e);
					return;
				}

				if (n > 0)
				{
					con.Send((byte[]) ar.AsyncState, 0, n);
				}
				else
				{
					// finished - no more data can be read
					DataSentNotify();
				}
			}
		}

		private void OnInternalSent(Connection con, int amount, bool hasReamining)
		{
			if (writing && !hasReamining)
				ContinueReadStreamData();
		}

		#endregion

		#region Cleanup

		private bool resetting;

		private void ResetAfterTransfer()
		{
			if (!resetting)
			{
				resetting = true;
				if (CloseStreamAfterTransaction && currentStream != null)
				{
					try
					{
						currentStream.Close();
					}
					catch
					{
					}
				}
				currentStream = null;
				currentFile = null;
				streamBuffer = null;
				writing = false;
				resetting = false;
			}
		}

		private void OnInternalDisconnected(Connection con, bool conLost)
		{
			if (writing)
				ResetAfterTransfer();
		}

		#endregion

		#region Events

		#region Delegates

		public delegate void DataReadFailedHandler(ConnectionWriter con, Exception e);

		public delegate void DataSentHandler(ConnectionWriter con);

		#endregion

		/// <summary>
		/// Will be raised when an I/O operation upon the given stream failed.
		/// </summary>
		public event DataReadFailedHandler DataReadFailed;

		private void DataReadFailedNotify(Exception e)
		{
			if (DataReadFailed != null)
				DataReadFailed(this, e);
			ResetAfterTransfer();
		}

		/// <summary>
		/// Will be raised when no more data can be read from a stream.
		/// </summary>
		public event DataSentHandler DataSent;

		private void DataSentNotify()
		{
			if (DataSent != null)
				DataSent(this);
			ResetAfterTransfer();
		}

		#endregion
	}
}