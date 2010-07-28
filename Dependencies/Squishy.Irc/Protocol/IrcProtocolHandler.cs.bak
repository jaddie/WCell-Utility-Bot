using System;
using System.Collections.Generic;
using System.Text;
using Squishy.Network;
using Squishy.Network.Prot;

namespace Squishy.Irc.Protocol
{
	public class IrcProtocolHandler : IProtocolHandler
	{
		private const string PacketTerminator = "\r\n";

		private readonly Connection.BytesReceivedHandler rcvdHandler;
		private Connection con;
		protected Encoding encoding;
		private IrcClient irc;
		private string lastResponsePart;

		internal IrcProtocolHandler(IrcClient irc, Encoding encoding)
			: this(encoding)
		{
			ApplyTo(irc);
		}

		internal IrcProtocolHandler(Encoding encoding)
		{
			rcvdHandler = HandleBytes;
			lastResponsePart = "";
			this.encoding = encoding;
		}

		public Encoding Encoding
		{
			get { return encoding; }
			set { encoding = value; }
		}

		public Connection Connection
		{
			get { return con; }
		}

		#region IProtocolHandler Members

		/// <summary>
		/// 
		/// </summary>
		public void HandleBytes(Connection con, ByteBuffer buf)
		{
			var packets = ExtractPackets(buf);
			foreach (var packet in packets)
			{
				var handlers = IrcProtocol.Instance[packet.Key];
				if (handlers == null)
				{
					//if (IrcProtocol.Instance.DefaultPacketHandler == null) {
					//    throw new NullReferenceException("DefaultPacketHandler has not been set.");
					//}
					IrcProtocol.Instance.DefaultPacketHandler(packet);
				}
				else
				{
					if (handlers.Count == 1)
					{
						handlers[0](packet);
					}
					else if (handlers.Count > 1)
					{
						var pos = packet.Content.Position;
						for (var i = 0; i < handlers.Count; i++)
						{
							var handler = handlers[i];
							handler(packet);
							packet.Content.Position = pos;
						}
					}
				}

				if (PacketReceived != null)
				{
					PacketReceived(packet);
				}
			}
		}

		#endregion

		public event IrcProtocol.PacketHandler PacketReceived;

		public void ApplyTo(IrcClient irc)
		{
			if (irc != null)
			{
				// add handler
				con = irc.Client;
				con.BytesReceived += rcvdHandler;
			}
			else
			{
				// remove handler
				con.BytesReceived -= rcvdHandler;
				con = null;
			}
			this.irc = irc;
		}

		/// <summary>
		/// Build a packet from a new line of content from the server.
		/// Do as much parsing as possible here before the packet-handler
		/// will then work with the gathered information.
		/// </summary>
		public IrcPacket CreatePacket(string content)
		{
			var line = new StringStream(content.Trim());

			string prefix;
			if (content[0] == ':')
			{
				prefix = line.NextWord().Substring(1);
			}
			else
			{
				prefix = line.NextWord();
			}

			var action = line.NextWord();
			var packet = new IrcPacket(irc, prefix, action, new StringStream(line.Remainder.Trim()), line.String)
			{
				protHandler = this
			};

			return packet;
		}

		public IrcPacket[] ExtractPackets(ByteBuffer partialResponse)
		{
			var str = partialResponse.GetString(encoding);
			var response = lastResponsePart + str;
			var ss = new StringStream(response);
			var packets = new List<IrcPacket>(3);

			while (ss.HasNext)
			{
				var content = ss.NextWord(PacketTerminator);
				if (!ss.HasNext && !response.EndsWith(PacketTerminator))
				{
					lastResponsePart = content;
				}
				else
				{
					packets.Add(CreatePacket(content));
					if (!ss.HasNext)
					{
						lastResponsePart = "";
					}
				}
			}
			return packets.ToArray();
		}

		internal void ParseModes(IrcUser user, IrcChannel chan, string flags, string[] args)
		{
			bool add = true;

			if (chan == null)
			{
				// User Modes
				for (int i = 0; i < flags.Length; i++)
				{
					string c = Convert.ToString(flags[i]);
					if (c == "+")
					{
						add = true;
						continue;
					}
					else if (c == "-")
					{
						add = false;
						continue;
					}

					if (add)
						irc.Me.AddMode(c);
					else
						irc.Me.DeleteMode(c);
					irc.UserModeChangedNotify();
				}
				return;
			}

			int n = 0;
			for (int i = 0; i < flags.Length; i++)
			{
				// Chan Modes/Flags
				string c = Convert.ToString(flags[i]);
				if (c == "+")
				{
					add = true;
					continue;
				}
				else if (c == "-")
				{
					add = false;
					continue;
				}

				string arg = "";

				if (irc.HasChanMode(c))
				{
					if (add)
					{
						if (c == "b" || c == "k" || c == "l")
							arg = args[n++];

						if (c == "b")
						{
							var entry = new BanEntry(arg, user.Nick, DateTime.Now);
							if (!chan.BanMasks.ContainsKey(arg))
							{
								chan.BanMasks.Add(arg, entry);
							}
						}
						else
						{
							chan.AddMode(c, arg);
						}
						irc.ModeAddedNotify(user, chan, c, arg);
					}
					else
					{
						if (c == "b")
						{
							arg = args[n++];
							chan.BanMasks.Remove(arg);
						}
						else
						{
							chan.DeleteMode(c);
						}
						irc.ModeDeletedNotify(user, chan, c, arg);
					}
				}
				else
				{
					// user privs
					var priv = irc.GetPrivForFlag(flags[i]);
					if (priv != Privilege.Regular)
					{
						arg = args[n++];
						var targ = irc.GetOrCreateUser(arg);
						if (add)
						{
							chan.AddFlag(priv, targ);
							irc.FlagAddedNotify(user, chan, priv, targ);
						}
						else
						{
							chan.DeleteFlag(priv, targ);
							irc.FlagDeletedNotify(user, chan, priv, targ);
						}
					}
				}
			}
		}
	}
}