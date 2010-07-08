using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Squishy.Network
{
	/// <summary>
	/// Represents an asynchronous TCP Connection that is synchronized for all critical operations.
	/// None of its methods are blocking. Therefore most actions are not completed
	/// when returning from a method but should be handled using the given events of this class.
	/// If this Connection is throttled, its Throttle will take care of the buffer size management.
	/// In that state it is illegal to change the buffersize of the throttled buffers (send- and/or receive- buffer
	/// respectively) since the throttling depends on a correctly adjusted buffersize.
	/// </summary>
	public class Connection
	{
		#region Fields

		public static int DefaultReceiveBufferSize = 4096, DefaultSendBufferSize = 4096;

		private readonly object connectLock = new object();
		private readonly object readLock = new object();
		private readonly object sendLock = new object();
		private readonly Queue<ByteBuffer> sendQueue;
		private bool closing;
		private int currentDownSpeed;
		private int currentRcvdBytes, currentSentBytes, currentUpSpeed;
		private Encoding encoding;
		private bool isconnected;
		private bool isconnecting, isdisconnecting;
		private DateTime lastReceiveTime;
		private DateTime lastSendTime;
		private string lineTerminator;
		private IPEndPoint localEndPoint;
		private bool reading;
		private ByteBuffer receiveBuf;
		private int receiveBufSize;
		private IPEndPoint remoteEndPoint;
		private int sendBufSize;
		private Socket sock;
		private DateTime startTime;
		private Throttle throttle;
		private long totalRcvdBytes, totalSentBytes;
		private bool writing;

		#endregion

		#region Constructors

		public Connection()
		{
			receiveBufSize = DefaultReceiveBufferSize;
			sendBufSize = DefaultSendBufferSize;
			sendQueue = new Queue<ByteBuffer>();
			encoding = Encoding.UTF8;
			lineTerminator = "\n";
		}

		internal Connection(Socket s)
			: this()
		{
			sock = s;
			isconnected = true;
			localEndPoint = (IPEndPoint) s.LocalEndPoint;
			remoteEndPoint = (IPEndPoint) s.RemoteEndPoint;
			BeginReceive();
		}

		#endregion

		#region Misc Props

		/// Used for encoding written or decoding read strings.
		/// Default: UTF-8
		/// </summary>
		public Encoding Encoding
		{
			get { return encoding; }
			set { encoding = value; }
		}

		/// <summary>
		/// The line terminator for reading and receiving strings.
		/// Default: "/n"
		/// </summary>
		public string LineTerminator
		{
			get { return lineTerminator; }
			set { lineTerminator = value; }
		}

		/// <summary>
		/// The size of the read buffer (defaults to <code>DefaultReceiveBufferSize</code>).
		/// Changing the buffer'str size will automatically re-allocate the buffer itself
		/// which will be synchronized with all read operations.
		/// If this connection has a download throttle, the throttle will take care of an accurate buffersize.
		/// </summary>
		public int ReceiveBufferSize
		{
			get { return receiveBufSize; }
			set { SetRcvBufferSize(value, false); }
		}

		/// <summary>
		/// The size of the send buffer (defaults to <code>DefaultSendBufferSize</code>).
		/// Changing the buffer'str size will automatically re-allocate the buffer itself
		/// which will be synchronized with all send operations.
		/// If this connection has a download throttle, the throttle will take care of an accurate buffersize.
		/// </summary>
		public int SendBufferSize
		{
			get { return sendBufSize; }
			set { SetSendBufferSize(value, false); }
		}

		/// <summary>
		/// The local endpoint to where this connection'str socket is/was connecting/connected to.
		/// </summary>
		public IPEndPoint LocalEndPoint
		{
			get { return localEndPoint; }
		}

		/// <summary>
		/// The remote endpoint and port which this Client'str socket is/was connecting/connected to.
		/// May be null if it is not resolved yet.
		/// </summary>
		public IPEndPoint RemoteEndPoint
		{
			get { return remoteEndPoint; }
		}

		public Socket Socket
		{
			get { return sock; }
		}

		/// <summary>
		/// Indicates wether or not this Connection has unsent data pending to be sent.
		/// </summary>
		public bool Sending
		{
			get { return writing || sendQueue.Count > 0; }
		}

		/// <summary>
		/// Indicates wether this Connection is currently disconnecting or waiting to disconnect 
		/// after all remaining data has been sent.
		/// </summary>
		public bool Closing
		{
			get { return closing || isdisconnecting; }
		}

		/// <summary>
		/// Indicates wether this Connection is valid. A valid Connection is connecting or connected 
		/// and not currently closing.
		/// </summary>
		public bool Valid
		{
			get { return (isconnecting || isconnected) && !Closing; }
		}

		/// <summary>
		/// Amount of buf that have been sent during this or the last session.
		/// </summary>
		public long SentBytes
		{
			get { return totalSentBytes; }
		}

		/// <summary>
		/// Amount of buf that have been received during this or the last session.
		/// </summary>
		public long RcvdBytes
		{
			get { return totalRcvdBytes; }
		}

		/// <summary>
		/// The time when the current (or last) session was established.
		/// </summary>
		public DateTime StartTime
		{
			get { return startTime; }
		}

		/// <summary>
		/// Time between now and the last time this Connection successfully connected somewhere.
		/// </summary>
		public TimeSpan RunTime
		{
			get { return DateTime.Now - startTime; }
		}

		public long TotalRcvdBytes
		{
			get { return totalRcvdBytes; }
		}

		public long TotalSentBytes
		{
			get { return totalSentBytes; }
		}

		/// <summary>
		/// Average download speed in buf/second during <code>RunTime</code>.
		/// </summary>
		public int AverageDownBpS
		{
			get { return (int) (totalRcvdBytes/RunTime.TotalSeconds); }
		}

		/// <summary>
		/// Average upload speed in buf/second during <code>RunTime</code>.
		/// </summary>
		public int AverageUpBpS
		{
			get { return (int) (totalSentBytes/RunTime.TotalSeconds); }
		}

		/// <summary>
		/// Current download speed in buf/second between now and the when last packet has been received.
		/// </summary>
		public int CurrentDownBpS
		{
			get
			{
				if (currentDownSpeed == 0)
					return CalculateDownBps();
				return currentDownSpeed;
			}
		}

		/// <summary>
		/// Current upload speed in buf/second between now and when the last packet has been sent
		/// or when throttled between now and the last Poll.
		/// </summary>
		public int CurrentUpBpS
		{
			get
			{
				if (currentUpSpeed == 0)
					return CalculateUpBps();
				return currentUpSpeed;
			}
		}

		/// <summary>
		/// If connected, a Connection is able to send and/or receive data.
		/// </summary>
		public bool IsConnected
		{
			get { return isconnected; }
		}

		/// <summary>
		/// Indicates wether this Connection is currently connecting to <code>RemoteEndPoint</code>.
		/// </summary>
		public bool IsConnecting
		{
			get { return isconnecting; }
		}

		internal void SetRcvBufferSize(int value, bool throttleAccess)
		{
			if (UploadThrottled && !throttleAccess)
				throw new InvalidOperationException("You cannot manually adjust the buffer size of a throttled Connection.");
			lock (readLock)
			{
				receiveBufSize = value;
				if (IsConnected)
				{
					sock.ReceiveBufferSize = value;
					receiveBuf = new ByteBuffer(value);
				}
			}
		}

		internal void SetSendBufferSize(int value, bool throttleAcces)
		{
			if (DownloadThrottled && !throttleAcces)
				throw new InvalidOperationException("You cannot manually adjust the buffer size of a throttled Connection.");
			lock (sendLock)
			{
				sendBufSize = value;
				if (IsConnected)
					sock.SendBufferSize = value;
			}
		}

		private int CalculateDownBps()
		{
			return (int) (currentRcvdBytes/(DateTime.Now - lastReceiveTime).TotalSeconds);
		}

		private int CalculateUpBps()
		{
			return (int) (currentSentBytes/(DateTime.Now - lastSendTime).TotalSeconds);
		}

		#endregion

		#region Connect

		/// <summary>
		/// Starts connecting to the given address.
		/// </summary>
		public void BeginConnect(IPEndPoint addr)
		{
			if (IsConnected || isconnecting)
				return;
			lock (connectLock)
			{
				isconnecting = true;

				sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				remoteEndPoint = addr;
				ContinueConnect();
			}
		}

		/// <summary>
		/// Starts connecting to the given host and port.
		/// </summary>
		public virtual void BeginConnect(string addr, int port)
		{
			if (IsConnected || isconnecting)
				return;
			lock (connectLock)
			{
				isconnecting = true;

				sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				try
				{
					remoteEndPoint = new IPEndPoint(IPAddress.Parse(addr), port);
					ContinueConnect();
				}
				catch (FormatException)
				{
					Dns.BeginGetHostEntry(addr, OnResolved, port);
				}
			}
		}

		private void OnResolved(IAsyncResult ar)
		{
			lock (connectLock)
			{
				try
				{
					var port = (int) ar.AsyncState;
					remoteEndPoint = new IPEndPoint(Dns.EndGetHostEntry(ar).AddressList[0], port);
					ContinueConnect();
				}
				catch (Exception e)
				{
					if (DisconnectNow())
						ConnectFailNotify(e);
				}
			}
		}

		private void ContinueConnect()
		{
			try
			{
				ConnectingNotify();
				sock.BeginConnect(remoteEndPoint, OnConnected, null);
			}
			catch (ObjectDisposedException)
			{
				// socket has been closed
				return;
			}
			catch (Exception e)
			{
				if (DisconnectNow())
					ConnectFailNotify(e);
			}
		}

		private void OnConnected(IAsyncResult ar)
		{
			lock (connectLock)
			{
				try
				{
					sock.EndConnect(ar);
					sock.SendBufferSize = sendBufSize;
					sock.ReceiveBufferSize = receiveBufSize;
					localEndPoint = (IPEndPoint) sock.LocalEndPoint;
				}
				catch (ObjectDisposedException)
				{
					// socket has been closed
					return;
				}
				catch (Exception e)
				{
					if (DisconnectNow())
						ConnectFailNotify(e);
					return;
				}

				isconnecting = false;
				isconnected = true;

				BeginReceive();

				ConnectedNotify();
			}
		}

		#endregion

		#region Disconnect

		/// <summary>
		/// Marks this Connection Closing. If any data is pending to be sent it will disconnect
		/// after this data has been sent, else it will immediately call <code>DisconnectNow</code>.
		/// </summary>
		public void Disconnect()
		{
			if (writing)
			{
				closing = true;
			}
			else if (!closing)
			{
				DisconnectNow();
			}
		}

		/// <summary>
		/// Disconnects this Client and releases all Resources after current read and send 
		/// operations finished.
		/// </summary>
		/// <returns>True if this call disconnected this Connection or false if it was already disonnected.</returns>
		public bool DisconnectNow()
		{
			return DisconnectNow(false);
		}

		private bool DisconnectNow(bool connectionLost)
		{
			if (isconnected || isconnecting)
			{
				isdisconnecting = true;
				closing = false;
				lock (connectLock)
				{
					lock (sendLock)
					{
						// TODO: fix deadlocks due to insufficient synchronizing
						lock (readLock)
						{
							if (!isconnecting)
							{
								DisconnectingNotify(connectionLost);
								isconnected = false;
							}

							try
							{
								sock.Close();
							}
							catch
							{
							}
							receiveBuf = null;
							sendQueue.Clear();
							sock = null;
							reading = false;
							writing = false;
							currentDownSpeed = 0;
							currentUpSpeed = 0;
							totalRcvdBytes = 0;
							totalSentBytes = 0;

							DisconnectedNotify(connectionLost);
							isconnecting = false;
						}
					}
				}
				isdisconnecting = false;
				return true;
			}
			else
			{
				return false;
			}
		}

		#endregion

		#region Send

		/// <summary>
		/// Sends a line to the current <code>RemoteEndPoint</code>.
		/// </summary>
		public void SendLine(object format, params object[] args)
		{
			Send(format + lineTerminator, args);
		}

		/// <summary>
		/// Sends <code>string.Format(format.ToString(), args)</code> to the current <code>RemoteEndPoint</code>.
		/// </summary>
		public void Send(object format, params object[] args)
		{
			Send(encoding.GetBytes(string.Format(format.ToString(), args)));
		}

		/// <summary>
		/// Sends the whole byte array to the current <code>RemoteEndPoint</code>.
		/// </summary>
		public void Send(byte[] buf)
		{
			Send(buf, 0, buf.Length);
		}

		/// <summary>
		/// Sends the contents of the given byte array to the current <code>RemoteEndPoint</code>,
		/// using the given offset and length.
		/// </summary>
		public void Send(byte[] buf, int offset, int length)
		{
			if (!IsConnected)
				return;

			Send(new ByteBuffer(offset, length, buf));
		}

		/// <summary>
		/// Sends the information from the given ByteBuffer to the current <code>RemoteEndPoint</code>.
		/// </summary>
		public void Send(ByteBuffer buf)
		{
			lock (sendLock)
			{
				if (writing || (UploadThrottled && throttle.UploadSuspended))
				{
					sendQueue.Enqueue(buf);
				}
				else
				{
					try
					{
						writing = true;
						sock.BeginSend(buf.bytes, buf.offset, buf.length, SocketFlags.None, new AsyncCallback(OnSend), buf);
					}
					catch (ObjectDisposedException)
					{
						// socket has been closed
						writing = false;
					}
					catch
					{
						DisconnectNow(true);
					}
				}
			}
		}

		/// <summary>
		/// Sends <code>string.Format(format.ToString(), args)</code> to the current <code>RemoteEndPoint</code>.
		/// </summary>
		public void SendNow(object format, params object[] args)
		{
			SendNow(string.Format(format.ToString(), args));
		}

		/// <summary>
		/// Sends <code>string.Format(format.ToString(), args)</code> to the current <code>RemoteEndPoint</code>.
		/// </summary>
		public virtual void SendNow(string text)
		{
			byte[] bytes = encoding.GetBytes(text);
			SendNow(new ByteBuffer(0, bytes.Length, bytes));
		}

		public void SendNow(ByteBuffer buf)
		{
			try
			{
				writing = true;
				sock.Send(buf.bytes, buf.offset, buf.length, SocketFlags.None);
			}
			catch (ObjectDisposedException)
			{
				// socket has been closed
				writing = false;
			}
			catch
			{
				writing = false;
				DisconnectNow(true);
			}
		}

		internal void ContinueSend()
		{
			currentUpSpeed = CalculateUpBps();
			currentSentBytes = 0;
			lastSendTime = DateTime.Now;
			if (sendQueue.Count > 0)
				lock (sendLock)
				{
					Send(sendQueue.Dequeue());
				}
		}

		private void OnSend(IAsyncResult ar)
		{
			lock (sendLock)
			{
				if (isdisconnecting)
					return;

				int n;
				try
				{
					n = sock.EndSend(ar);
					totalSentBytes += n;
					if (UploadThrottled)
					{
						currentSentBytes += n;
						throttle.UpdateUpload(n);
					}
					else
					{
						currentUpSpeed = CalculateUpBps();
						currentSentBytes = n;
						lastSendTime = DateTime.Now;
					}

					var buf = (ByteBuffer) ar.AsyncState;
					int r = buf.length - n;
					if (r > 0)
					{
						buf.offset = buf.bytes.Length - r;
						SendNow(buf);
					}
					else if (sendQueue.Count > 0)
					{
						SendNow(sendQueue.Dequeue());
					}
					else
					{
						writing = false;
						if (closing)
						{
							DisconnectNow(false);
						}
					}
				}
				catch (ObjectDisposedException)
				{
					// socket has been closed
					return;
				}
				catch
				{
					DisconnectNow(true);
					return;
				}

				SentNotify(n, writing);
			}
		}

		#endregion

		#region Recieve

		private void BeginReceive()
		{
			startTime = DateTime.Now;
			receiveBuf = new ByteBuffer(receiveBufSize);
			reading = true;
			lastReceiveTime = DateTime.Now;
			ContinueReadUnlocked();
		}

		private void OnRecieved(IAsyncResult ar)
		{
			lock (readLock)
			{
				if (isdisconnecting)
					return;

				var buf = (ByteBuffer) ar.AsyncState;
				int n = 0;
				try
				{
					n = sock.EndReceive(ar);
					totalRcvdBytes += n;
				}
				catch (ObjectDisposedException)
				{
					// socket has been closed
					return;
				}
				catch
				{
					DisconnectNow(true);
					return;
				}

				if (n > 0)
				{
					if (DownloadThrottled)
					{
						currentRcvdBytes += n;
						throttle.UpdateDownload(n);
					}
					else
					{
						// TODO: find a way for consistent speed calculation for non-polled connections
						currentRcvdBytes = n;
						lastReceiveTime = DateTime.Now;
					}

					buf.offset = 0;
					buf.length = n;
					ReceivedNotify(buf);

					if (throttle == null || !throttle.DownloadSuspended)
						ContinueReadUnlocked();
					else
					{
						reading = false;
					}
				}
				else
				{
					//throw new Exception("FATAL: Received non-positive amount of buf: " + n);
					DisconnectNow(true);
				}
			}
		}

		private void ContinueReadUnlocked()
		{
			try
			{
				sock.BeginReceive(receiveBuf.Data, 0, receiveBuf.Data.Length, SocketFlags.None, new AsyncCallback(OnRecieved),
				                  receiveBuf);
			}
			catch (ObjectDisposedException)
			{
			} // socket has been closed
			catch
			{
				DisconnectNow(true);
			}
		}

		internal void ContinueRead()
		{
			if (!reading)
			{
				currentDownSpeed = CalculateDownBps();
				currentRcvdBytes = 0;
				lastReceiveTime = DateTime.Now;
				reading = true;
				lock (readLock)
				{
					ContinueReadUnlocked();
				}
			}
		}

		#endregion

		#region Events

		#region Connectivity

		#region Delegates

		public delegate void ConnectedHandler(Connection con);

		public delegate void ConnectFailHandler(Connection con, Exception e);

		public delegate void ConnectingHandler(Connection con);

		public delegate void DisconnectedHandler(Connection con, bool connectionLost);

		public delegate void DisconnectingHandler(Connection con, bool connectionLost);

		#endregion

		/// <summary>
		/// Is raised when this Connection starts to build up a new connection.
		/// </summary>
		public event ConnectingHandler Connecting;

		private void ConnectingNotify()
		{
			if (Connecting != null)
				Connecting(this);
		}

		/// <summary>
		/// Is raised when connecting to a given address failed.
		/// </summary>
		public event ConnectFailHandler ConnectFailed;

		private void ConnectFailNotify(Exception e)
		{
			if (ConnectFailed != null)
				ConnectFailed(this, e);
			isconnecting = false;
		}

		/// <summary>
		/// Is raised when this connection successfully connected to its <code>RemoteEndPoint</code>.
		/// </summary>
		public event ConnectedHandler Connected;

		internal event ConnectedHandler InternalConnected;

		private void ConnectedNotify()
		{
			if (InternalConnected != null)
				InternalConnected(this);
			if (Connected != null)
				Connected(this);
		}

		/// <summary>
		/// Is called right when this Connection is connected and is about to disconnect.
		/// </summary>
		public event DisconnectingHandler Disconnecting;

		private void DisconnectingNotify(bool connectionLost)
		{
			if (Disconnecting != null)
				Disconnecting(this, connectionLost);
		}

		/// <summary>
		/// Is called right when this Connection was connected and finished disconnecting.
		/// </summary>
		public event DisconnectedHandler Disconnected;

		public event DisconnectedHandler InternalDisconnected;

		private void DisconnectedNotify(bool connectionLost)
		{
			if (InternalDisconnected != null)
				InternalDisconnected(this, connectionLost);
			if (Disconnected != null)
				Disconnected(this, connectionLost);
		}

		#endregion

		#region Receive

		#region Delegates

		public delegate void BytesReceivedHandler(Connection con, ByteBuffer buf);

		public delegate void LineReceivedHandler(Connection con, StringStream line);

		public delegate void RawTextReceivedHandler(Connection con, StringStream text);

		#endregion

		private string lastLine = "";

		/// <summary>
		/// Is raised when buf have been received which have been encoded
		/// with the given <code>Encoding</code> and wrapped into a <code>StringStream</code>.
		/// </summary>
		public event RawTextReceivedHandler RawTextReceived;


		/// <summary>
		/// Is raised when a string has been received that terminates with the given
		/// <code>LineTerminator</code>.
		/// </summary>
		public event LineReceivedHandler LineReceived;

		/// <summary>
		/// Is raised when buf have been received. When delegates are registered to this event,
		/// text events will be ignored.
		/// </summary>
		public event BytesReceivedHandler BytesReceived;

		internal event BytesReceivedHandler InternalBytesReceived;

		private void ReceivedNotify(ByteBuffer buf)
		{
			if (InternalBytesReceived != null)
			{
				InternalBytesReceived(this, buf);
				buf.offset = 0;
			}
			if (BytesReceived != null)
			{
				BytesReceived(this, buf);
			}
			else if (RawTextReceived != null || LineReceived != null)
			{
				string text = lastLine + encoding.GetString(buf.Data, 0, buf.length);
				if (RawTextReceived != null)
					RawTextReceived(this, new StringStream(text));
				if (LineReceived != null)
				{
					var ss = new StringStream(text);
					while (ss.HasNext)
					{
						if (BytesReceived != null)
						{
							// Handler might change
							int count;
							if (encoding.IsSingleByte)
								count = ss.Position;
							else
								count = encoding.GetByteCount(ss.String.Substring(0, ss.Position));
							buf.offset = count;
							buf.length -= count;
							BytesReceived(this, buf);
							break;
						}
						if (LineReceived != null)
						{
							string line = ss.NextWord(lineTerminator);
							if (!ss.HasNext && !text.EndsWith(lineTerminator))
							{
								lastLine += line;
								return;
							}
							LineReceived(this, new StringStream(line));
						}
					}
					lastLine = "";
				}
			}
		}

		#endregion

		#region Send

		#region Delegates

		public delegate void SentHandler(Connection con, int amount, bool hasRemaining);

		#endregion

		/// <summary>
		/// Is called when buf have been sent.
		/// </summary>
		public event SentHandler Sent;

		internal event SentHandler InternalSent;

		private void SentNotify(int amount, bool hasRemaining)
		{
			if (InternalSent != null)
				InternalSent(this, amount, hasRemaining);
			if (Sent != null)
				Sent(this, amount, hasRemaining);
		}

		#endregion

		#endregion

		#region Throttling

		/// <summary>
		/// Gets or sets the current throttle of this connection.
		/// </summary>
		public Throttle Throttle
		{
			get { return throttle; }
			set
			{
				if (throttle != value)
				{
					if (value != null)
						value.Add(this);
					else
						throttle.Remove(this);
				}
			}
		}

		/// <summary>
		/// Returns wether or not this Connection'str upload is throttled.
		/// </summary>
		public bool UploadThrottled
		{
			get { return throttle != null && throttle.ThrottlesUpload; }
		}

		/// <summary>
		/// Returns wether or not this Connection'str download is throttled.
		/// </summary>
		public bool DownloadThrottled
		{
			get { return throttle != null && throttle.ThrottlesDownload; }
		}

		internal void SetThrottle(Throttle newThrottle)
		{
			if (newThrottle != null)
			{
				if (throttle != null)
					throw new InvalidOperationException("Connection can only be throttled by one Throttle at a time.");
				throttle = newThrottle;
			}
			else
				throttle = null;
		}

		#endregion

		public override string ToString()
		{
			if (remoteEndPoint != null)
				return remoteEndPoint.ToString();
			else
				return base.ToString();
		}
	}
}