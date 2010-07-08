using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using System.Net.Sockets;


namespace Squishy.Network {
	/// <summary>
	/// The DataConnection provides several methods to send continuous data.
	/// </summary>
	public class DataConnection : Connection {
		public DataConnection() {
			
		}
		
		public DataConnection(Socket sock) {
		}
		
		/// <summary>
		/// Flags this connection busy and asynchronously sends the given file.
		/// </summary>
		public void SendFile(FileInfo file) {

		}

		/// <summary>
		/// Flags this connection busy and asynchronously sends everything that can be read from this 
		/// stream.
		/// </summary>
		public void SendAll(Stream stream) {

		}

		public void SendAll(Connection con) {
		}
		
	}
}
