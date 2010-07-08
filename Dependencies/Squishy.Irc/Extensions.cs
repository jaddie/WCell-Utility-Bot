using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Squishy.Irc
{
	public static class Extensions
	{

		#region String Building / Verbosity
		/// <summary>
		/// Returns the string representation of an IEnumerable (all elements, joined by comma)
		/// </summary>
		/// <param name="conj">The conjunction to be used between each elements of the collection</param>
		public static string ToString<T>(this IEnumerable<T> collection, string conj)
		{
			string vals;
			if (collection != null)
			{
				vals = string.Join(conj, ToStringArrT(collection));
			}
			else
				vals = "(null)";

			return vals;
		}

		/// <summary>
		/// Returns the string representation of an IEnumerable (all elements, joined by comma)
		/// </summary>
		/// <param name="conj">The conjunction to be used between each elements of the collection</param>
		public static string ToString<T>(this IEnumerable<T> collection, string conj, Func<T, object> converter)
		{
			string vals;
			if (collection != null)
			{
				vals = string.Join(conj, ToStringArrT(collection, converter));
			}
			else
				vals = "(null)";

			return vals;
		}

		/// <summary>
		/// Returns the string representation of an IEnumerable (all elements, joined by comma)
		/// </summary>
		/// <param name="conj">The conjunction to be used between each elements of the collection</param>
		public static string ToStringCol(this ICollection collection, string conj)
		{
			string vals;
			if (collection != null)
			{
				vals = string.Join(conj, ToStringArr(collection));
			}
			else
				vals = "(null)";

			return vals;
		}

		public static string[] ToStringArr(ICollection collection)
		{
			var strArr = new string[collection.Count];
			var colEnum = collection.GetEnumerator();
			for (var i = 0; i < strArr.Length; i++)
			{
				colEnum.MoveNext();
				var cur = colEnum.Current;
				if (cur != null)
				{
					strArr[i] = cur.ToString();
				}
			}
			return strArr;
		}

		public static string[] ToStringArrT<T>(IEnumerable<T> collection)
		{
			return ToStringArrT(collection, null);
		}

		public static string[] ToStringArrT<T>(IEnumerable<T> collection, Func<T, object> converter)
		{
			var strArr = new string[collection.Count()];
			var colEnum = collection.GetEnumerator();
			for (var i = 0; i < strArr.Length; i++)
			{
				colEnum.MoveNext();
				var cur = colEnum.Current;
				if (!Equals(cur, default(T)))
				{
					strArr[i] = (converter != null ? converter(cur) : cur).ToString();
				}
			}
			return strArr;
		}

		public static string[] ToJoinedStringArr<T>(IEnumerable<T> col, int partCount, string conj)
		{
			var strs = ToStringArrT(col);

			var list = new List<string>();
			var current = new List<string>(partCount);
			for (int index = 0, i = 0; index < strs.Length; i++, index++)
			{
				current.Add(strs[index]);
				if (i == partCount)
				{
					i = 0;
					list.Add(string.Join(conj, current.ToArray()));
					current.Clear();
				}
			}
			if (current.Count > 0)
				list.Add(string.Join(conj, current.ToArray()));

			return list.ToArray();
		}

		public static string ToString<K, V>(this IEnumerable<KeyValuePair<K, V>> args, string indent, string seperator)
		{
			string s = "";
			var i = 0;
			foreach (var arg in args)
			{
				i++;
				s += indent + arg.Key + " = " + arg.Value;

				if (i < args.Count())
				{
					s += seperator;
				}
			}
			return s;
		}
		#endregion
	}
}
