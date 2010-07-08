using System;

namespace Squishy.Irc
{
	public class BanEntry
	{
		/// <summary>
		/// The banmask itself.
		/// </summary>
		public readonly string Banmask;

		/// <summary>
		/// The nick of the User who added this banmask.
		/// </summary>
		public readonly string BannerName;

		/// <summary>
		/// The DateTime instance which represents the time when the ban has been set.
		/// </summary>
		public readonly DateTime Bantime;

		public BanEntry(string banmask, string bannerName, DateTime bantime)
		{
			Banmask = banmask;
			BannerName = bannerName;
			Bantime = bantime;
		}
	}
}