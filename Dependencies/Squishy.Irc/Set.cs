using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Squishy.Irc
{
	public class Set<TEnum>
	{
		#region "Data"

		private readonly BitArray members;
		private bool flagsEnum;
		private Dictionary<TEnum, int> mapMember2Index;

		#endregion

		#region "ctors"

		public Set()
		{
			Initialize();
			members = new BitArray(mapMember2Index.Count);
		}

		private Set(BitArray members)
		{
			Initialize();
			this.members = members;
		}


		private void Initialize()
		{
			if (typeof (TEnum).BaseType != typeof (Enum))
				throw new ApplicationException(string.Format("Generic type parameter <{0}> is not an enum type!",
				                                             typeof (TEnum).FullName));

			flagsEnum = typeof (TEnum).GetCustomAttributes(typeof (FlagsAttribute), true).Length > 0;

			int i = 0;
			mapMember2Index = new Dictionary<TEnum, int>();
			foreach (TEnum m in Enum.GetValues(typeof (TEnum)))
				mapMember2Index.Add(m, i++);
		}

		#endregion

		#region "Working with the set"

		public int Cardinality
		{
			get { return members.Length; }
		}

		public void Clear()
		{
			members.SetAll(false);
		}

		public Set<TEnum> Add(TEnum member)
		{
			members.Set(mapMember2Index[member], true);
			return this;
		}

		public Set<TEnum> Add(Set<TEnum> otherSet)
		{
			if (otherSet != null) members.Or(otherSet.members);
			return this;
		}

		public Set<TEnum> Remove(TEnum member)
		{
			members.Set(mapMember2Index[member], false);
			return this;
		}

		public Set<TEnum> Remove(Set<TEnum> otherSet)
		{
			if (otherSet != null)
				for (int i = 0; i < members.Count; i++)
					if (otherSet.members[i])
						members[i] = false;
			return this;
		}

		public Set<TEnum> Intersect(Set<TEnum> otherSet)
		{
			if (otherSet != null) members.And(otherSet.members);
			return this;
		}

		public bool Contains(TEnum member)
		{
			return members[mapMember2Index[member]];
		}

		#endregion

		#region "Overrides"

		public override bool Equals(object obj)
		{
			return this == (Set<TEnum>) obj;
		}

		public override int GetHashCode()
		{
			return members.GetHashCode();
		}

		public override string ToString()
		{
			string[] names = Enum.GetNames(typeof (TEnum));

			var memberNames = new StringBuilder("[");
			for (int i = 0; i < members.Count; i++)
				if (members[i])
				{
					if (memberNames.Length > 1) memberNames.Append(",");
					memberNames.Append(names[i]);
				}
			memberNames.Append("]");

			return memberNames.ToString();
		}

		#endregion

		#region "Operators"

		// intersection
		public static Set<TEnum> operator &(Set<TEnum> left, Set<TEnum> right)
		{
			if (left == null || right == null)
			{
				return new Set<TEnum>();
			}
			var result = new BitArray(new bool[left.members.Count]);
			result.Or(left.members);
			result.And(right.members);
			return new Set<TEnum>(result);
		}

		// union
		public static Set<TEnum> operator |(Set<TEnum> left, Set<TEnum> right)
		{
			if (left == null && right == null)
			{
				return new Set<TEnum>();
			}
			if (left == null)
			{
				return right.Copy();
			}
			if (right == null)
			{
				return left.Copy();
			}

			var result = new BitArray(new bool[left.members.Count]);
			result.Or(left.members);
			result.Or(right.members);
			return new Set<TEnum>(result);
		}

		public static bool operator ==(Set<TEnum> left, Set<TEnum> right)
		{
			if (left == null && right == null)
			{
				return true;
			}
			if (right != null && left != null)
			{
				for (int i = 0; i < left.members.Count; i++)
					if (left.members[i] != right.members[i]) return false;
				return true;
			}
			else
			{
				return false;
			}
		}

		public static bool operator !=(Set<TEnum> left, Set<TEnum> right)
		{
			return !(left == right);
		}

		public Set<TEnum> Copy()
		{
			var arr =
				new BitArray(new bool[members.Count]);
			arr.Or(members);
			return new Set<TEnum>(arr);
		}

		#endregion

		#region "Iterator"

		public IEnumerator<TEnum> GetEnumerator()
		{
			var memberValues = (TEnum[]) Enum.GetValues(typeof (TEnum));
			for (int i = 0; i < members.Count; i++)
				if (members[i])
					yield return memberValues[i];
		}

		#endregion
	}
}