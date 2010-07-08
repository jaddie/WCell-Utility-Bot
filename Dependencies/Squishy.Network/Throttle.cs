using System;
using System.Collections.Generic;

namespace Squishy.Network
{
	/// <summary>
	/// A Throttle limits the download and/or upload speed of all its Connections
	/// to given values.
	/// The Throttle will adjust the buffersize of the Connection'str read- and write-buffer, according 
	/// to the given speed limits. When throttling is discontinued the Throttle will reset the 
	/// buffersizes to its default values:
	/// <code>Connection.DefaultReceiveBufferSize</code> and <code>Connection.DefaultReceiveSendSize</code>
	/// </summary>
	public class Throttle
	{
		#region Fields

		private readonly Object conLock = new Object();
		private readonly List<Connection> cons;
		private bool active;
		private bool downloadSuspended;
		private int maxDownloadSpeed, maxUploadSpeed;
		private int rcvdBytes, sentBytes;
		private ThrottleType type;
		private bool uploadSuspended;

		#endregion

		public Throttle(ThrottleType type, params Connection[] connections)
		{
			this.type = type;
			cons = new List<Connection>(connections.Length);
			foreach (Connection con in connections)
			{
				Add(con);
			}
		}

		#region Misc Props

		/// <summary>
		/// Indicates wether or not this Throttle is registered to the TrafficWatch.
		/// </summary>
		public bool Active
		{
			get { return active; }
			internal set
			{
				if (active == value)
					return;
				active = value;
				if (active)
				{
					if (ThrottlesDownload)
						AdjustReceiveBuffer();
					if (ThrottlesUpload)
						AdjustSendBuffer();
				}
				else
				{
					if (ThrottlesDownload)
						ResetReceiveBuffer();
					if (ThrottlesUpload)
						ResetSendBuffer();
					Poll(); // reset
				}
			}
		}

		/// <summary>
		/// The maximum upload speed of all this Throttle'str connections together in buf per second.
		/// </summary>
		public int MaxUploadSpeed
		{
			get { return maxUploadSpeed; }
			set
			{
				maxUploadSpeed = value;
				AdjustSendBuffer();
			}
		}

		/// <summary>
		/// The maximum download speed of all this Throttle'str connections together in buf per second.
		/// </summary>
		public int MaxDownloadSpeed
		{
			get { return maxDownloadSpeed; }
			set
			{
				maxDownloadSpeed = value;
				AdjustReceiveBuffer();
			}
		}

		public ThrottleType Type
		{
			get { return type; }
			set
			{
				bool throttledUpload = ThrottlesUpload;
				bool throttledDownload = ThrottlesDownload;
				type = value;
				if (ThrottlesDownload && !throttledDownload)
				{
					AdjustReceiveBuffer();
				}
				if (ThrottlesUpload && !throttledUpload)
				{
					AdjustSendBuffer();
				}
				if (throttledDownload && !ThrottlesDownload)
				{
					ResetReceiveBuffer();
				}
				if (throttledUpload && !ThrottlesUpload)
				{
					ResetSendBuffer();
				}
			}
		}

		/// <summary>
		/// Returns true if this Throttle'str <code>ThrottleType</code> includes <code>ThrottleType.Up</code>.
		/// </summary>
		public bool ThrottlesUpload
		{
			get { return (type & ThrottleType.Up) != 0; }
			set
			{
				if (value)
					Type |= ThrottleType.Up;
				else
					Type &= ~ThrottleType.Up;
			}
		}

		/// <summary>
		/// Returns true if this Throttle'str <code>Type</code> includes <code>ThrottleType.Down</code>.
		/// </summary>
		public bool ThrottlesDownload
		{
			get { return (type & ThrottleType.Down) != 0; }
			set
			{
				if (value)
					Type |= ThrottleType.Down;
				else
					Type &= ~ThrottleType.Down;
			}
		}

		/// <summary>
		/// Indicates wether or not connections are currently suspended from uploading
		/// because they exceed their maximum download speed.
		/// </summary>
		public bool UploadSuspended
		{
			get { return uploadSuspended; }
		}

		/// <summary>
		/// Indicates wether or not connections are currently suspended from downloading
		/// because they exceed their maximum download speed.
		/// </summary>
		public bool DownloadSuspended
		{
			get { return downloadSuspended; }
		}

		#endregion

		#region Access connections

		/// <summary>
		/// Count of connections for this throttle.
		/// </summary>
		public int Count
		{
			get { return cons.Count; }
		}

		public Connection this[int index]
		{
			get { return cons[index]; }
		}

		/// <summary>
		/// Adds a new connection to this throttle.
		/// </summary>
		public void Add(Connection con)
		{
			lock (conLock)
			{
				con.SetThrottle(this);
				cons.Add(con);
			}
			if (Active)
			{
				if (ThrottlesDownload)
					AdjustReceiveBuffer();
				if (ThrottlesUpload)
					AdjustSendBuffer();
			}
		}

		/// <summary>
		/// Removes an existent Connection from this throttle or does nothing if the connection
		/// does not belong to this throttle.
		/// </summary>
		public void Remove(Connection con)
		{
			lock (conLock)
			{
				Remove(cons.IndexOf(con));
			}
		}

		public void Remove(int index)
		{
			if (index > -1)
			{
				Connection con = cons[index];
				con.SetThrottle(null);
				cons.RemoveAt(index);
				if (Active)
				{
					if (ThrottlesDownload)
						ResetReceiveBuffer(con);
					if (ThrottlesUpload)
						ResetSendBuffer(con);
				}
			}
		}

		/// <summary>
		/// Removes all connections from this throttle.
		/// </summary>
		public void RemoveAll()
		{
			if (Active)
			{
				if (ThrottlesDownload)
					ResetReceiveBuffer();
				if (ThrottlesUpload)
					ResetSendBuffer();
				foreach (Connection con in cons)
				{
				}
			}
			cons.Clear();
		}

		#endregion

		#region Suspend/Resume transfer activity

		internal void UpdateUpload(int newBytes)
		{
			if (TrafficWatch.Active && Active)
			{
				sentBytes += newBytes;
				uploadSuspended = sentBytes >= (maxUploadSpeed*TrafficWatch.PollSpan.TotalSeconds);
			}
		}

		internal void UpdateDownload(int newBytes)
		{
			if (TrafficWatch.Active && Active)
			{
				rcvdBytes += newBytes;
				downloadSuspended = rcvdBytes >= (maxDownloadSpeed*TrafficWatch.PollSpan.TotalSeconds);
			}
		}

		internal void Poll()
		{
			sentBytes = 0;
			rcvdBytes = 0;
			foreach (Connection con in cons)
			{
				con.ContinueSend();
				con.ContinueRead();
			}
			uploadSuspended = false;
			downloadSuspended = false;
		}

		#endregion

		#region Managing buffers

		internal void AdjustReceiveBuffer()
		{
			if (cons.Count == 0)
				return;
			int s = CalculateBufferSize(maxDownloadSpeed);
			foreach (Connection con in cons)
				con.SetRcvBufferSize(s, true);
		}

		internal void AdjustSendBuffer()
		{
			if (cons.Count == 0)
				return;
			int s = CalculateBufferSize(maxDownloadSpeed);
			foreach (Connection con in cons)
				con.SetSendBufferSize(s, true);
		}

		private int CalculateBufferSize(int maxSpeed)
		{
			// TODO: improve buffersize calculation
			// first calculate the byte count per second per connection
			var s = (int) Math.Round(maxSpeed*TrafficWatch.PollSpan.TotalSeconds/cons.Count);

			/*
			 * Find a good value for the buffer size that is not too small, not too big
			 * and -if possible- is a nominator of the speed.
			 */
			if (s > 100000)
				s = 8192; // not greater than 8 kb
			else if (s < 1024)
				s = 256; // not smaller than 256b
			else
				s /= 5;
			return s;
		}

		private void ResetReceiveBuffer()
		{
			if (cons.Count == 0)
				return;
			foreach (Connection con in cons)
				ResetReceiveBuffer(con);
		}

		private void ResetSendBuffer()
		{
			if (cons.Count == 0)
				return;
			foreach (Connection con in cons)
				ResetSendBuffer(con);
		}

		private void ResetReceiveBuffer(Connection con)
		{
			con.SetRcvBufferSize(Connection.DefaultReceiveBufferSize, true);
		}

		private void ResetSendBuffer(Connection con)
		{
			con.SetSendBufferSize(Connection.DefaultSendBufferSize, true);
		}

		#endregion
	}
}