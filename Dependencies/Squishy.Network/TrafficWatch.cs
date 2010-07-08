using System;
using System.Collections.Generic;
using System.Threading;

namespace Squishy.Network
{
	/// <summary>
	/// This class is required for Connection throtteling. When active, a thread will poll all given throttles
	/// every PollSpan. That means calculating an exact current speed of all their Connections and continue
	/// transfer of all throttled connections.
	/// </summary>
	public static class TrafficWatch
	{
		#region Fields

		private static readonly Object pollLock = new Object();
		private static readonly List<Throttle> throttles = new List<Throttle>();
		private static bool active;
		private static DateTime lastPollTime;
		private static TimeSpan pollSpan = TimeSpan.FromSeconds(1);
		private static Thread thread;

		#endregion

		#region Create new Throttles

		/// <summary>
		/// Adds and returns a new SetThrottle which will restrict the total upload speed of all its connections
		/// to the given value.
		/// <param name="cons">Initial connections of the new throttle.</param>
		/// </summary>
		public static Throttle AddUploadThrottle(int maxUpBps, params Connection[] cons)
		{
			var throttle = new Throttle(ThrottleType.Up, cons);
			throttle.MaxUploadSpeed = maxUpBps;
			Add(throttle);
			return throttle;
		}

		/// <summary>
		/// Adds and returns a new SetThrottle which will restrict the total download speed of all its connections
		/// to the given value.
		/// <param name="cons">Initial connections of the new throttle.</param>
		/// </summary>
		public static Throttle AddDownloadThrottle(int maxDownBps, params Connection[] cons)
		{
			var throttle = new Throttle(ThrottleType.Down, cons);
			throttle.MaxDownloadSpeed = maxDownBps;
			Add(throttle);
			return throttle;
		}

		/// <summary>
		/// Adds and returns a new SetThrottle which will restrict the total download speed and
		/// upload speeds of all its connections to the given values.
		/// <param name="cons">Initial connections of the new throttle.</param>
		/// </summary>
		public static Throttle AddThrottle(int maxUpBps, int maxDownBps, params Connection[] cons)
		{
			var throttle = new Throttle(ThrottleType.Both, cons);
			throttle.MaxUploadSpeed = maxUpBps;
			throttle.MaxDownloadSpeed = maxDownBps;
			Add(throttle);
			return throttle;
		}

		#endregion

		#region Access Throttles

		/// <summary>
		/// Total count of Throttles.
		/// </summary>
		public static int Count
		{
			get { return throttles.Count; }
		}

		public static Throttle GetThrottle(int index)
		{
			return throttles[index];
		}

		/// <summary>
		/// Adds a new Throttle.
		/// </summary>
		public static void Add(Throttle throttle)
		{
			lock (pollLock)
			{
				throttle.Active = true;
				throttles.Add(throttle);
			}
		}

		/// <summary>
		/// Removes the given Throttle or does nothing if its not Active.
		/// </summary>
		public static void Remove(Throttle throttle)
		{
			Remove(throttles.IndexOf(throttle));
		}

		public static void Remove(int index)
		{
			if (index > -1)
			{
				lock (pollLock)
				{
					Throttle throttle = throttles[index];
					throttle.Active = false;
					throttles.RemoveAt(index);
				}
			}
		}

		/// <summary>
		/// Removes all Throttles.
		/// </summary>
		public static void RemoveAll()
		{
			lock (pollLock)
			{
				foreach (Throttle t in throttles)
					t.Active = false;
				throttles.Clear();
			}
		}

		#endregion

		#region Props

		/// <summary>
		/// Gets or sets the status of the TrafficWatch.
		/// </summary>
		public static bool Active
		{
			get { return active; }
			set
			{
				if (active == value)
					return;

				active = value;
				if (value)
				{
					(thread = new Thread(WaitPoll)).Start();
				}
				else
				{
					lock (pollLock)
					{
						Monitor.PulseAll(pollLock);
					}
				}
			}
		}

		/// <summary>
		/// The timespan in which throttled connections should be polled (Default = 1 second).
		/// </summary>
		public static TimeSpan PollSpan
		{
			get { return pollSpan; }
			set
			{
				pollSpan = value;
				lock (pollLock)
				{
					foreach (Throttle t in throttles)
					{
						if (t.ThrottlesDownload)
							t.AdjustReceiveBuffer();
						if (t.ThrottlesUpload)
							t.AdjustSendBuffer();
					}
					if (active)
						Monitor.PulseAll(pollLock);
				}
			}
		}

		public static DateTime LastPollTime
		{
			get { return lastPollTime; }
		}

		#endregion

		#region Watching

		#region Delegates

		public delegate void PollHandler();

		#endregion

		private static void WaitPoll()
		{
			while (active)
			{
				lock (pollLock)
				{
					Monitor.Wait(pollLock, pollSpan);
				}
				PollAll();
			}
		}

		private static void PollAll()
		{
			if (Poll != null)
				Poll();

			lock (pollLock)
			{
				foreach (Throttle throttle in throttles)
				{
					throttle.Poll();
				}
			}
			lastPollTime = DateTime.Now;
		}

		/// <summary>
		/// When the TrafficWatch is active this event is raised for every poll 
		/// after the time specified by <code>PollSpan</code>.
		/// </summary>
		public static event PollHandler Poll;

		#endregion
	}
}