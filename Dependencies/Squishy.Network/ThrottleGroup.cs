using System;
using System.Collections.Generic;
using System.Text;

namespace Squishy.Network {
	public abstract class ThrottleGroup {
		private int maxBytesPerSecond;
		private List<Connection> cons;
		private ThrottleType type;
		private long rcvdBytes, sentBytes;

		public ThrottleGroup(int maxBytesPerSecond, ThrottleType type, params Connection[] connections) {
			this.maxBytesPerSecond = maxBytesPerSecond;
			this.type = type;
			cons = new List<Connection>(connections.Length);
			foreach (Connection con in connections) {
				cons.Add(con);
				sentBytes += con.SentBytes;
				rcvdBytes += con.RcvdBytes;
			}
		}
		
		public int Speed {
			get {
				return maxBytesPerSecond;
			}
			set {
				maxBytesPerSecond = value;
			}
		}
		
		public ThrottleType Type {
			get {
				return type;
			}
			set {
				type = value;
			}
		}
		
		public int Count {
			get {
				return cons.Count;
			}
		}
		
		public void Add(Connection con) {
			cons.Add(con);
		}

		/// <returns>The amount of bytes that has been received by this group since the last poll.</returns>
		internal long PollRead() {
			long read = rcvdBytes;
			rcvdBytes = 0;
			foreach (Connection con in cons) {
				rcvdBytes += con.RcvdBytes;
			}
		}

		/// <returns>The amount of bytes that has been sent by this group since the last poll.</returns>
		internal long PollSend() {
			long sent = sentBytes;
			sentBytes = 0;
			foreach (Connection con in cons) {
				sentBytes += con.SentBytes;
			}
		}
	}
}
