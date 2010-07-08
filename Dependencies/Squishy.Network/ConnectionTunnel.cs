using System;
using System.Collections.Generic;
using System.Text;

namespace Squishy.Network {
	/// <summary>
	/// TODO: Finish ConnectionTunnel
	/// Represents a tunnel that will listen for an incoming Connection. When the connection has been
	/// accepted it will connect to a given server and tunnel sent information of both sides.
	/// </summary>
	public class ConnectionTunnel {
		ServerConnection listener;
		Connection client, server;

		public ConnectionTunnel() {
			SetupListener();
			server = new Connection();
		}

		private void SetupListener() {
			listener = new ServerConnection();
		}
		
		public void Start(int listenPort, string serverHost, int serverPort) {
		}
	}
}
