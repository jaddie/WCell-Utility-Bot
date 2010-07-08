using System;
using System.Collections.Generic;

namespace Squishy.Irc
{
	public class UserPrivSet
	{
		private static readonly IDictionary<Privilege, _PrivLevel> privMap = new Dictionary<Privilege, _PrivLevel>();

		private readonly Set<Privilege> set;
		private Privilege highestPriv;
		private _PrivLevel highestPrivLvl;

		static UserPrivSet()
		{
			try
			{
				var values = (Privilege[]) Enum.GetValues(typeof (Privilege));
				for (int i = 0; i < values.Length; i++)
				{
					var rank = (_PrivLevel) Enum.Parse(typeof (_PrivLevel), values[i].ToString());
					privMap.Add(values[i], rank);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}

		public UserPrivSet()
		{
			set = new Set<Privilege>();
			highestPriv = Privilege.Regular;
		}

		public UserPrivSet(string flags)
			: this()
		{
			Set(flags);
		}

		public bool this[char flag]
		{
			get { return HasAtLeast(flag); }
		}

		public bool this[Privilege flag]
		{
			get { return HasAtLeast(flag); }
		}

		public Privilege Highest
		{
			get { return highestPriv; }
		}

		public Set<Privilege> PrivSet
		{
			get { return set; }
		}

		internal void Add(char flag)
		{
            Add((Privilege)flag);
		}

        internal void Add(Privilege flag)
        {
            set.Add(flag);
        	_PrivLevel lvl;
            if (privMap.TryGetValue(highestPriv, out lvl) && privMap[flag] > lvl)
            {
                highestPriv = flag;
                highestPrivLvl = lvl;
            }
        }


		internal void Remove(char flag)
		{
			Remove((Privilege) flag);
		}

		internal void Remove(Privilege flag)
		{
			set.Remove(flag);
			if (privMap[flag] == highestPrivLvl)
			{
				FindHighest();
			}
		}

		internal void Set(string flags)
		{
			set.Clear();
			foreach (char c in flags)
			{
				Add(c);
			}
		}


		public bool Has(char flag)
		{
			return set.Contains((Privilege) flag);
		}

		public bool Has(Privilege flag)
		{
			return set.Contains(flag);
		}


		public bool HasAtLeast(char flag)
		{
			return highestPriv >= (Privilege) flag;
		}

		public bool HasAtLeast(Privilege flag)
		{
			return highestPriv >= flag;
		}


		private void FindHighest()
		{
			highestPrivLvl = _PrivLevel.Regular;
			foreach (Privilege priv in set)
			{
				_PrivLevel lvl = privMap[priv];
				if (lvl > highestPrivLvl)
				{
					highestPrivLvl = lvl;
				}
			}
			highestPriv = (Privilege) Enum.Parse(typeof (Privilege), highestPrivLvl.ToString());
			//highestPriv = (Privilege)highestPrivLvl;
		}

		#region Nested type: _PrivLevel

		private enum _PrivLevel
		{
			Regular = 0,
			Voice,
			HalfOp,
			Op,
			Admin,
			Owner
		}

		#endregion
	}
}