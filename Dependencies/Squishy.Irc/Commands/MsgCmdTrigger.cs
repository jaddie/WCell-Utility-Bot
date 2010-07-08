using Squishy.Network;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// Triggers through PRIVMSG (normal chatting in channels and queries)
	/// </summary>
	internal class MsgCmdTrigger : CmdTrigger
	{
		public MsgCmdTrigger(StringStream args, IrcUser user)
			: this(args, user, null)
		{
		}

		public MsgCmdTrigger(StringStream args, IrcUser user, IrcChannel chan)
			: base(args, user, chan)
		{
		}

		public override void Reply(string text)
		{
			Target.Msg(text);
		}
	}
}