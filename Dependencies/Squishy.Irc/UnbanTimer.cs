using System;
using System.Timers;

namespace Squishy.Irc
{
	/// <summary>
	/// Instances of this class represent a delayed unbanning, completely self-managed.
	/// </summary>
	public class UnbanTimer
	{
		public readonly IrcChannel Channel;
		private readonly Timer m_timer;
		public readonly string Mask;

		public UnbanTimer(IrcChannel chan, string mask, TimeSpan timeout)
		{
			Channel = chan;
			Mask = mask;
			m_timer = new Timer(timeout.TotalMilliseconds);
			m_timer.Elapsed += OnTick;
			m_timer.AutoReset = false;
			chan.AddUnbanTimer(this);
			m_timer.Start();
		}

		private void OnTick(object sender, ElapsedEventArgs info)
		{
			Channel.ElapsUnbanTimer(this);
		}
	}
}