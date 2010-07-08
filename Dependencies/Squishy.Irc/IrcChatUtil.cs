using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Squishy.Irc
{
	public static class IrcChatUtil
	{
		/// <summary>
		/// Ctrl + B in mIRC (\u0002)
		/// </summary>
		public const string BoldControlCode = "\u0002";

		/// <summary>
		/// Ctrl + K in mIRC (\u0003)
		/// </summary>
		public const string ColorControlCode = "\u0003";

		/// <summary>
		/// Ctrl + U in mIRC (\u001F)
		/// </summary>
		public const string UnderlineControlCode = "\u001F";

		/// <summary>
		/// Ctrl + O in mIRC (\u000E)
		/// </summary>
		public const string ControlEndCode = "\u000E";

		public static string Colorize(this string text, IrcColorCode color)
		{
			return ColorControlCode + (int)color + text + ColorControlCode;
		}

		public static string Bold(this string text)
		{
			return BoldControlCode + text + BoldControlCode;
		}

		public static string GetColor(IrcColorCode color)
		{
			return ColorControlCode + (int)color;
		}
	}
}
