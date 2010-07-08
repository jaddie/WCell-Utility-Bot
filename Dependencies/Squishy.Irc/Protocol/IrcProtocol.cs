using System;
using System.Collections.Generic;
using Squishy.Network;
using Squishy.Network.Prot;

namespace Squishy.Irc.Protocol
{
	public class IrcProtocol
	{
		#region Delegates

		public delegate void PacketHandler(IrcPacket packet);

		#endregion

		public const int MaxLineLength = 510;
		private static IrcProtocol instance;

		private readonly IDictionary<string, IList<PacketHandler>> packetHandlers;

		private PacketHandler defaultPacketHandler;
		private List<IProtocolHandler> protHandlers;
		private static int m_maxModCount = 256;

		/// <summary>
		/// Create a singleton instance.
		/// </summary>
		protected IrcProtocol()
		{
			packetHandlers = new Dictionary<string, IList<PacketHandler>>(StringComparer.InvariantCultureIgnoreCase);
			protHandlers = new List<IProtocolHandler>();

			SetupHandlers();
		}

		public List<IProtocolHandler> ProtHandlers
		{
			get { return protHandlers; }
		}

		/// <summary>
		/// Singleton
		/// </summary>
		public static IrcProtocol Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new IrcProtocol();
				}
				return instance;
			}
		}

		public IList<PacketHandler> this[string key]
		{
			get
			{
				IList<PacketHandler> handlers;
				packetHandlers.TryGetValue(key, out handlers);
				return handlers;
			}
		}

		public IList<PacketHandler> GetOrCreateHandlers(string key)
		{
			IList<PacketHandler> handlers;
			if (!packetHandlers.TryGetValue(key, out handlers))
			{
				packetHandlers.Add(key, handlers = new List<PacketHandler>(5));
			}
			return handlers;
		}

		public PacketHandler DefaultPacketHandler
		{
			get { return defaultPacketHandler; }
			set { defaultPacketHandler = value; }
		}

		public static int MaxModCount
		{
			get
			{
				return m_maxModCount;
			}
			set
			{
				m_maxModCount = value;
			}
		}

		/// <summary>
		/// Adds a new packet handler for a certain action.
		/// </summary>
		public void AddPacketHandler(string key, PacketHandler handler)
		{
			GetOrCreateHandlers(key).Add(handler);
		}

		/// <summary>
		/// Removes the packet handler for a certain action.
		/// </summary>
		public bool RemovePacketHandler(string key, PacketHandler handler)
		{
			IList<PacketHandler> handlers;
			if (packetHandlers.TryGetValue(key, out handlers))
			{
				return handlers.Remove(handler);
			}
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		private void SetupHandlers()
		{
			// If its not recognized:
			defaultPacketHandler = packet =>
			{
				// PING is special
				if (packet.Prefix == "PING")
				{
					packet.IrcClient.Send("PONG " + packet.Key);
				}
				else if (packet.Key.StartsWith("4") || packet.Key.StartsWith("5"))
				{
					// Error Replies
					packet.IrcClient.ErrorNotify(packet);
				}
				else
				{
					// Normal replies
					packet.IrcClient.UnspecifiedInfoNotify(packet);
				}
			};

			// Connection Handling
			AddPacketHandler("001", packet =>
			{
				// :Serv 001 Me :<Welcomemessage>
				packet.IrcClient.Me.ChangeNick(packet.Content.NextWord());
				defaultPacketHandler(packet);
			});

			AddPacketHandler("005", packet => { packet.IrcClient.ConnectionInfoNotify(packet); });
			AddPacketHandler("376", packet => { packet.IrcClient.PerformNotify(); });

			// Messaging
			AddPacketHandler("PRIVMSG", packet =>
			{
				var strStream = new StringStream(packet.Content);
				packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());

				var args = packet.Args;
				packet.IrcClient.TextNotify(packet.User, packet.channel, new StringStream(args));

				if (args.StartsWith("") && args.EndsWith(""))
				{
					// CTCP
					args = packet.Args.Split('')[1];

					var argsStream = new StringStream(args);
					packet.IrcClient.CtcpRequestNotify(packet.User, packet.channel, argsStream.NextWord(),
													   argsStream.Remainder);
				}
				else
				{
					// Text message
					var argsStream = new StringStream(args);
					if (packet.channel == null)
					{
						packet.IrcClient.QueryMsgNotify(packet.User, argsStream);
					}
					else
					{
						packet.IrcClient.ChannelMsgNotify(packet.User, packet.channel, argsStream);
					}
				}
			});

			AddPacketHandler("NOTICE", packet =>
			{
				var strStream = new StringStream(packet.Content);
				packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());

				string args = packet.Args;
				var argsStream = new StringStream(strStream.Remainder.Trim());
				packet.IrcClient.TextNotify(packet.User, packet.channel, argsStream);
				if (packet.Args.StartsWith("") && packet.Args.EndsWith(""))
				{
					args = packet.Args.Substring(1, packet.Args.Length - 2);

					// TODO: Check if this is right?
					packet.IrcClient.CtcpReplyNotify(packet.User, packet.channel, argsStream.NextWord(),
													 packet.Args);
				}
				else
				{
					packet.IrcClient.NoticeNotify(packet.User, packet.channel, new StringStream(packet.Args));
				}
			});

			// Nick / Quit
			AddPacketHandler("NICK", packet => { packet.IrcClient.NickNotify(packet.User, packet.Args); });
			AddPacketHandler("QUIT", packet => { packet.IrcClient.QuitNotify(packet.User, packet.Args); });
			AddPacketHandler("431", packet => { packet.IrcClient.InvalidNickNotify("431", "", packet.Args); }); // :Serv 431 Me :No nickname given
			AddPacketHandler("433", packet => // :Serv 433 Me <Nick> :Nickname is already in use.
									{
										var strStream = new StringStream(packet.Content);
										strStream.SkipWord();
										packet.IrcClient.InvalidNickNotify(packet.Key, strStream.NextWord(), packet.Args);
									});

			AddPacketHandler("432", packetHandlers["433"][0]); // :Serv 432 Me <Nick> :Invalid nickname: ...

			// Channel related Stuff
			AddPacketHandler("INVITE", packet => { packet.IrcClient.InviteNotify(packet.User, packet.ArgsOrFirstWord); });
			AddPacketHandler("JOIN", packet => { packet.IrcClient.JoinNotify(packet.User, packet.ArgsOrFirstWord); });
			AddPacketHandler("485", packet =>										// cannot join channel
			{
				packet.channel = packet.IrcClient.GetChannel(packet.Content.NextWord());
				packet.IrcClient.CannotJoinNotify(packet.channel, packet.Args);
			});
			AddPacketHandler("471", delegate { });
			AddPacketHandler("473", delegate { });
			AddPacketHandler("474", delegate { });
			AddPacketHandler("475", delegate { });
			AddPacketHandler("477", delegate { });

			AddPacketHandler("TOPIC", packet =>
			{
				// User TOPIC Channel :Topic
				var content = packet.Content;

				packet.channel = packet.IrcClient.GetOrCreateChannel(content.NextWord());
				packet.channel.TopicSetter = packet.User;
				packet.channel.TopicSetTime = DateTime.Now;
				packet.IrcClient.TopicNotify(packet.User, packet.channel, packet.Args, false);
				packet.channel.SetTopic(packet.Args);
			});
			AddPacketHandler("332", packet =>
			{
				// :Sender 332 Me Channel :Topic
				var content = packet.Content;
				//packet.User = packet.IrcClient.GetOrCreateUser(content.NextWord());
				content.SkipWord();
				packet.channel = packet.IrcClient.GetOrCreateChannel(content.NextWord());
				packet.IrcClient.TopicNotify(packet.User, packet.channel, packet.Args, true);
				packet.channel.SetTopic(packet.Args);
			});

			AddPacketHandler("333", packet =>
			{
				// :Sender 333 Me Channel TopicSetter TopicTimestamp
				StringStream content = packet.Content;
				content.SkipWord();
				packet.channel = packet.IrcClient.GetOrCreateChannel(content.NextWord());
				packet.User = packet.IrcClient.GetOrCreateUser(content.NextWord());
				packet.channel.TopicSetter = packet.User;
				packet.channel.TopicSetTime = new DateTime(1970, 1, 1) +
											  TimeSpan.FromSeconds(content.NextInt());
			});

			AddPacketHandler("353", packet => // :Serv 353 Me = Channel :Namelist
									{
										var strStream = new StringStream(packet.Content);
										strStream.SkipWords(2);
										packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());
										IrcUser[] users = packet.channel.AddNames(packet.Args);
										packet.IrcClient.UsersAddedNotify(packet.channel, users);
									});
			AddPacketHandler("MODE", packet => // :Serv MODE Channel Modes [ModeArgs]
									 {
										 var strStream = new StringStream(packet.Content);
										 packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());
										 strStream.Consume(':', 1);
										 string modes = strStream.NextWord();
										 strStream.Consume(':', 1);
										 string[] prms = strStream.RemainingWords();
										 packet.ProtHandler.ParseModes(packet.User, packet.channel, modes, prms);
									 });
			AddPacketHandler("PART", packet =>
			{
				var strStream = new StringStream(packet.Content);
				packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());
				packet.IrcClient.PartNotify(packet.User, packet.channel, packet.Args);
			});
			AddPacketHandler("KICK", packet =>
			{
				var strStream = new StringStream(packet.Content);
				packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());
				IrcUser user = packet.IrcClient.GetOrCreateUser(strStream.NextWord());
				packet.IrcClient.KickNotify(packet.User, packet.channel, user, packet.Args);
			});
			AddPacketHandler("324", packet => // :Serv 324 Me Channel Modes [ModeArgs]
									{
										var strStream = new StringStream(packet.Content);
										strStream.SkipWord(); // skip "Me"
										packet.channel = packet.IrcClient.GetChannel(strStream.NextWord());

										strStream.Consume(':', 1);
										string modes = strStream.NextWord();
										strStream.Consume(':', 1);
										string[] prms = strStream.RemainingWords();
										packet.ProtHandler.ParseModes(packet.User, packet.channel, modes, prms);
									});
			AddPacketHandler("329", packet => // :Serv 329 [Me] #Channel CreationTime
									{
										var strStream = new StringStream(packet.Content);
										String chanName = strStream.NextWord();
										if (!chanName.StartsWith("#"))
										{
											chanName = strStream.NextWord();
										}
										packet.channel = packet.IrcClient.GetChannel(chanName);
										DateTime time = new DateTime(1970, 1, 1) +
														TimeSpan.FromSeconds(Convert.ToInt32(strStream.NextWord()));
										if (packet.channel != null)
											packet.channel.SetCreationTime(time);
										packet.IrcClient.ChanCreationTimeNotify(packet.channel, time);
									});
			AddPacketHandler("352", packet =>
			{
				// who info
				//strStream.SkipWord();
				var strStream = new StringStream(packet.Content);
				string channame = strStream.NextWord();
				string username = strStream.NextWord();
				string host = strStream.NextWord();
				string server = strStream.NextWord();
				string nick = strStream.NextWord();
				string flags = strStream.NextWord();
				string hops = strStream.NextWord().Remove(1, 1);
				string info = packet.Args.Substring(packet.Args.IndexOf(" ") + 1);
				packet.IrcClient.GetOrCreateUser(nick).SetInfo(username, host, info);
				packet.IrcClient.WhoReplyNotify(channame, username, host, server, nick, flags, hops, info);
			});
			AddPacketHandler("367", packet => // :Serv 367 Me Channel banMask bannerName banTime
									{
										var content = packet.Content;
										content.SkipWord();
										packet.channel = packet.IrcClient.GetChannel(content.NextWord());

										var banmask = content.NextWord();
										var banner = content.NextWord();
										var bantime = content.NextWord();
										var entry = new BanEntry(banmask, banner, new DateTime(1970, 1, 1) +
																				  TimeSpan.FromSeconds(Convert.ToInt32(bantime)));
										if (packet.channel != null && !packet.channel.BanMasks.ContainsKey(banmask))
										{
											packet.channel.BanMasks.Add(banmask, entry);
										}
										packet.IrcClient.BanListEntryNotify(packet.channel, entry);
									});
			AddPacketHandler("368", packet =>
			{
				packet.channel = packet.IrcClient.GetChannel(packet.Content.NextWord());
				packet.IrcClient.BanListCompleteNotify(packet.channel);
			});
		}
	}
}