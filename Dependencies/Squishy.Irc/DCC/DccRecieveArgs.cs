using System;
using System.Net;

namespace Squishy.Irc.Dcc
{
	public class DccReceiveArgs
	{
		/// <summary>
		/// The name of the file which is supposed to be sent.
		/// </summary>
		public readonly string FileName;

		/// <summary>
		/// The Remote IPEndPoint which the ReceiveClient is supposed to connect to.
		/// </summary>
		public readonly IPEndPoint RemoteEndPoint;

		/// <summary>
		/// The total size of the incoming file.
		/// </summary>
		public readonly long Size;

		/// <summary>
		/// The User who is trying to send the file.
		/// </summary>
		public readonly IrcUser User;

		/// <summary>
		/// Determines if this request is accepted. Change to deny/accept the sending.
		/// (Default = Dcc.AcceptByDefault)
		/// </summary>
		public bool Accept;

		/// <summary>
		/// The directory where the file should be saved to.
		/// </summary>
		public string DestinationDir;

		/// <summary>
		/// The name of the file on the local harddisk.
		/// </summary>
		public string DestinationFile;

		/// <summary>
		/// The TimeSpan that the DccReceiveClient should wait for an acceptance for a resume request,
		/// in case that the file has to be resumed.
		/// (Default = 1 minute)
		/// </summary>
		public TimeSpan Timeout;

		internal DccReceiveArgs(IrcUser user, string filename, IPEndPoint remoteEndPoint, long size)
		{
			User = user;
			FileName = filename;
			DestinationFile = filename;
			DestinationDir = "";
			RemoteEndPoint = remoteEndPoint;
			Size = size;
			Accept = Dcc.AcceptTransfer;
			Timeout = TimeSpan.FromMinutes(1);
		}
	}
}