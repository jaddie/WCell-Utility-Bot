using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Squishy.Network {
	public static class TrafficLimiter {
		#region Fields
		static bool active = false;
		static Thread thread;
		static List<ThrottleGroup> throttles;
		#endregion
	
		public static bool Active {
			get {
				return active;
			}
			set {
				if (active == value)
					return;
					
				if (value)
					thread = new Thread(new ThreadStart(Start));
				active = value;
			}
		}
		
		private static void Start() {
			while (active) {
				
				Thread.Sleep(1000);
			}
		}
	}
}
