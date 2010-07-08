using System;
using System.Collections.Generic;

namespace Squishy.Network
{
	/// <summary>
	/// Wraps a string for convinient string parsing.
	/// It is using an internal position for the given string so you can read
	/// continuesly the next part.
	/// 
	/// TODO: Make it an actualy stream class which can work with any kind of streams.
	/// </summary>
	public class StringStream : ICloneable
	{
		private readonly string str;
		private int pos;

		public StringStream(string s)
			: this(s, 0)
		{
		}

		public StringStream(string s, int initialPos)
		{
			str = s;
			pos = initialPos;
		}

		public StringStream(StringStream stream)
			: this(stream.str, stream.pos)
		{
		}

		/// <summary>
		/// Indicates wether we did not reach the end yet.
		/// </summary>
		public bool HasNext
		{
			get { return pos < str.Length; }
		}

		/// <summary>
		/// The position within the initial string.
		/// </summary>
		public int Position
		{
			get { return pos; }
			set { pos = value; }
		}

		/// <summary>
		/// The remaining length (from the current position until the end).
		/// </summary>
		public int Length
		{
			get { return str.Length - pos; }
		}

		/// <summary>
		/// The remaining string (from the current position until the end).
		/// </summary>
		public string Remainder
		{
			get
			{
				if (!HasNext)
					return "";
				return str.Substring(pos, Length);
			}
		}

		/// <summary>
		/// The wrapped string.
		/// </summary>
		public string String
		{
			get { return str; }
		}

		/// <summary>
		/// [Not implemented]
		/// </summary>
		public string this[int index]
		{
			get { return ""; }
		}

		#region ICloneable Members

		public Object Clone()
		{
			var ss = new StringStream(str);
			ss.pos = pos;
			return ss;
		}

		#endregion

		/// <summary>
		/// Resets the position to the beginning.
		/// </summary>
		public void Reset()
		{
			pos = 0;
		}

		/// <summary>
		/// Increases the position by the given count.
		/// </summary>
		public void Ignore(int charCount)
		{
			pos += charCount;
		}

		/// <returns><code>NextLong(-1, \" \")</code></returns>
		public long NextLong()
		{
			return NextLong(-1, " ");
		}

		/// <returns><code>NextLong(defaultVal, \" \")</code></returns>
		public long NextLong(long defaultVal)
		{
			return NextLong(defaultVal, " ");
		}

		/// <returns>The next word as long.</returns>
		/// <param name="defaultVal">What should be returned if the next word cannot be converted into a long.</param>
		/// <param name="seperator">What the next word should be seperated by.</param>
		public long NextLong(long defaultVal, string seperator)
		{
			try
			{
				return long.Parse(NextWord(seperator));
			}
			catch
			{
				return defaultVal;
			}
		}

		/// <returns><code>NextInt(-1, \" \")</code></returns>
		public int NextInt()
		{
			return NextInt(-1, " ");
		}

		/// <returns><code>NextInt(defaultVal, \" \")</code></returns>
		public int NextInt(int defaultVal)
		{
			return NextInt(defaultVal, " ");
		}

		/// <returns>The next word as int.</returns>
		/// <param name="defaultVal">What should be returned if the next word cannot be converted into an int.</param>
		/// <param name="seperator">What the next word should be seperated by.</param>
		public int NextInt(int defaultVal, string seperator)
		{
			try
			{
				return int.Parse(NextWord(seperator));
			}
			catch
			{
				return defaultVal;
			}
		}

		/// <summary>
		/// Calls <code>NextWord(" ")</code>.
		/// </summary>
		public string NextWord()
		{
			return NextWord(" ");
		}

		/// <summary>
		/// Moves the position behind the next word in the string, seperated by <code>seperator</code> and returns the word.
		/// </summary>
		public string NextWord(string seperator)
		{
			int length = str.Length;
			if (pos >= length)
				return "";

			int x;
			while ((x = str.IndexOf(seperator, pos)) == 0)
			{
				pos += seperator.Length;
			}

			string word;
			if (x < 0)
			{
				if (pos == length)
					return "";
				else
					x = length;
			}
			word = str.Substring(pos, x - pos);

			pos = x + seperator.Length;
			if (pos > length)
				pos = length;

			return word;
		}

		/// <returns><code>NextWords(count, \" \")</code></returns>
		public string NextWords(int count)
		{
			return NextWords(count, " ");
		}

		/// <returns>The next <code>count</code> word seperated by <code>seperator</code> as a string.</returns>
		public string NextWords(int count, string seperator)
		{
			string result = "";
			for (int i = 0; i < count && HasNext; i++)
			{
				if (i > 0)
					result += seperator;
				result += NextWord(seperator);
			}
			return result;
		}

		/// <returns><code>NextWordsArray(count, " ")</code></returns>
		public string[] NextWordsArray(int count)
		{
			return NextWordsArray(count, " ");
		}

		/// <returns>The next <code>count</code> word seperated by <code>seperator</code> as an array of strings.</returns>
		public string[] NextWordsArray(int count, string sep)
		{
			var words = new string[count];
			for (int i = 0; i < count && HasNext; i++)
			{
				words[i] = NextWord(sep);
			}
			return words;
		}

		/// <summary>
		/// Calls <code>RemainingWords(" ")</code>
		/// </summary>
		public string[] RemainingWords()
		{
			return RemainingWords(" ");
		}

		public string[] RemainingWords(string seperator)
		{
			var words = new List<string>();
			while (HasNext)
			{
				words.Add(NextWord(seperator));
			}
			return words.ToArray();
		}

		/// <returns><code>Consume(' ')</code></returns>
		public void ConsumeSpace()
		{
			Consume(' ');
		}

		/// <summary>
		/// Calls <code>SkipWord(" ")</code>.
		/// </summary>
		public void SkipWord()
		{
			SkipWord(" ");
		}

		/// <summary>
		/// Skips the next word, seperated by the given seperator.
		/// </summary>
		public void SkipWord(string seperator)
		{
			SkipWords(1, seperator);
		}

		/// <summary>
		/// Calls <code>SkipWords(count, " ")</code>.
		/// </summary>
		/// <param name="count">The amount of words to be skipped.</param>
		public void SkipWords(int count)
		{
			SkipWords(count, " ");
		}

		/// <summary>
		/// Skips <code>count</code> words, seperated by the given seperator.
		/// </summary>
		/// <param name="count">The amount of words to be skipped.</param>
		public void SkipWords(int count, string seperator)
		{
			NextWords(count, seperator);
		}

		/// <summary>
		/// Consume a whole string, as often as it occurs.
		/// </summary>
		public void Consume(string rs)
		{
			while (HasNext)
			{
				int i = 0;
				for (; i < rs.Length; i++)
				{
					if (str[pos + i] != rs[i])
					{
						return;
					}
				}
				pos += i;
			}
		}

		/// <summary>
		/// Ignores all directly following characters that are equal to <code>c</code>.
		/// </summary>
		public void Consume(char c)
		{
			while (HasNext && str[pos] == c)
				pos++;
		}

		/// <summary>
		/// Ignores a maximum of <code>amount</code> characters that are equal to <code>c</code>.
		/// </summary>
		public void Consume(char c, int amount)
		{
			for (int i = 0; i < amount && HasNext && str[pos] == c; i++)
				pos++;
		}

		/// <summary>
		/// Consumes the next character, if it equals <code>c</code>.
		/// </summary>
		/// <returns>Wether the character was equal to <code>c</code> (and thus has been deleted)</returns>
		public bool ConsumeNext(char c)
		{
			if (HasNext && str[pos] == c)
			{
				pos++;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Consumes the next character, if it equals <code>c</code>.
		/// </summary>
		/// <returns>Wether the character was equal to <code>c</code> (and thus has been deleted)</returns>
		public bool ConsumeNext(string str)
		{
			if (Remainder.StartsWith(str))
			{
				pos += str.Length;
				return true;
			}
			return false;
		}

		/// <returns>Wether or not the remainder contains the given string.</returns>
		public bool Contains(string s)
		{
			return s.IndexOf(s, pos) > -1;
		}

		/// <returns>Wether or not the remainder contains the given char.</returns>
		public bool Contains(char c)
		{
			return str.IndexOf(c, pos) > -1;
		}

		public override string ToString()
		{
			return Remainder.Trim();
		}

		public StringStream CloneStream()
		{
			return Clone() as StringStream;
		}
        /// <summary>
        /// Reads the next word as string of modifiers. 
        /// Modifiers are a string (usually representing a set of different modifiers per char), preceeded by a -.
        /// </summary>
        /// <remarks>Doesn't do anything if the next word does not start with a -.</remarks>
        /// <returns>The set of flags without the - or "" if none found</returns>
        public string NextModifiers()
        {
            var i = pos;
            var word = NextWord();
            if (word.StartsWith("-") && word.Length > 1)
            {
                return word.Substring(1);
            }
            pos = i;
            return "";
        }
	}
}
