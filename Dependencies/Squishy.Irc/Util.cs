using System;
using System.Net;
using System.Text.RegularExpressions;

namespace Squishy.Irc
{
	public class Util
	{
		public static IPAddress ExternalAddress;

		public static readonly IPAddress LocalHostAddress = new IPAddress(new byte[] {1, 0, 0, 127});


		/// <summary>
		/// Evaluates if the text fits into the specified wildcard pattern.
		/// </summary>
		/// <param name="text">The text to be compared</param>
		/// <param name="wildcard">The pattern which has to be matched</param>
		public static bool IsWildmatch(string text, string wildcard)
		{
			return Regex.IsMatch(text, Regex.Escape(wildcard).Replace(@"\*", ".*").Replace(@"\?", "."), RegexOptions.IgnoreCase);
		}

		public static string GetWords(string text, int from)
		{
			return GetWords(text.Split(' '), from);
		}

		public static string GetWords(object[] words, int from)
		{
			return GetWords(words, from, words.Length - from);
		}

		public static string GetWords(string text, int from, int count)
		{
			return GetWords(text.Split(' '), from, count);
		}

		public static string GetWords(object[] words, int from, int count)
		{
			string result = "";
			try
			{
				for (int i = from; i < from + count; i++)
					result += words[i] + (i < from + count - 1 ? " " : "");
			}
			catch (Exception)
			{
			}
			return result;
		}

		public static long GetTcpAddress(IPAddress addr)
		{
			byte[] bytes = addr.GetAddressBytes();
			long result = 0;
			for (int i = 0; i < bytes.Length; i++)
			{
				result += bytes[i]*(long) Math.Pow(2, 8*(bytes.Length - 1 - i));
			}
			return result;
		}

		public static IPAddress GetTcpAddress(long addr)
		{
			string result = "";
			for (int i = 0; i < 4; i++)
			{
				var b = (byte) (addr/(int) Math.Pow(2, 8*(3 - i)));
				result += b.ToString();
				if (i < 3)
					result += ".";
			}
			return IPAddress.Parse(result);
		}
	}
}