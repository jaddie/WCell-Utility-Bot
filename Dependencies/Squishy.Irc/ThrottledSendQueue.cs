using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Timers;

namespace Squishy.Irc
{
	public class ThrottledSendQueue
	{
		#region Delegates

		public delegate void DequeuedHandler(string line);

		#endregion

		private static int s_charsPerSecond = 40;

		//private static int m_maxModCount = 512;
		private static int m_maxModCount = 20;
		private readonly Timer m_delayTimer;
		private readonly Queue m_queue;
		private DateTime m_last = DateTime.Now;
		private int m_modCount;

		public ThrottledSendQueue()
		{
			m_delayTimer = new Timer();
			m_delayTimer.Elapsed += OnTick;
			m_queue = new Queue();
			Clear();
		}

		/// <summary>
		/// The maximum amount of characters that can be dequeued before the throttling starts. Will also
		/// decrease with the speed of CharsPerSecond (Default = 256).
		/// </summary>
		public int MaxModCount
		{
			get { return m_maxModCount; }
			set
			{
				if (value < 1)
					throw new ArgumentException("MaxModCount must not be equal to or lower than zero.");
				m_maxModCount = value;
			}
		}

		private int ModCount
		{
			get { return m_modCount; }
			set
			{
				m_modCount = value;
				if (m_modCount < 0)
					m_modCount = 0;
			}
		}

		/// <summary>
		/// The maximum amount of characters that can be dequeued within one second (Default = 40).
		/// </summary>
		public static int CharsPerSecond
		{
			get { return s_charsPerSecond; }
			set
			{
				if (value < 1)
					throw new ArgumentException("The speed of the ThrottledSendQueue cannot be equal or lower zero.");
				s_charsPerSecond = value;
			}
		}

		/// <summary>
		/// The count of lines being enqueued currently.
		/// </summary>
		public int Length
		{
			get { return m_queue.Count; }
		}

		/// <summary>
		/// Splits the text by line terminators and enqueues each line as one element.
		/// </summary>
		public void EnqueueRaw(string text)
		{
			var lines = text.Split(new [] {'\r', '\n'}, StringSplitOptions.RemoveEmptyEntries);
			for (var i = 0; i < lines.Length; i++)
			{
				var line = lines[i].Trim();
				m_queue.Enqueue(line);
			}

			if (!m_delayTimer.Enabled)
			{
				DequeueNoitfy();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="line"></param>
		public void Enqueue(string line)
		{
			if (line.Length > 0)
			{
				m_queue.Enqueue(line);

				if (!m_delayTimer.Enabled)
				{
					DequeueNoitfy();
				}
			}
		}

		/// <summary>
		/// Raised when another line of string is ready to be dequeued.
		/// </summary>
		public event DequeuedHandler Dequeued;

		private void DequeueNoitfy()
		{
			if (Length == 0)
				return;

			var time = DateTime.Now - m_last;
			ModCount -= (int)(time.TotalSeconds * CharsPerSecond);
			var line = (string)m_queue.Dequeue();

			var evt = Dequeued;
			if (evt != null)
			{
				evt(line);
			}
			ModCount += line.Length;
			m_last = DateTime.Now;
			if (ModCount >= MaxModCount)
			{
				m_delayTimer.Interval = (int)((line.Length / (double)s_charsPerSecond) * 1000);
				m_delayTimer.Start();
			}
			else
			{
				DequeueNoitfy();
			}
		}

		private void OnTick(object sender, ElapsedEventArgs args)
		{
			lock (this)
			{
				m_delayTimer.Stop();
				DequeueNoitfy();
			}
		}

		/// <summary>
		/// Clears and resets this queue.
		/// </summary>
		public void Clear()
		{
			m_delayTimer.Stop();
			m_queue.Clear();
		}
	}
}