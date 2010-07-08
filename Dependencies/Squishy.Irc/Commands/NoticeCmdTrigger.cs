using Squishy.Network;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// Triggers through NOTICE
	/// </summary>
	public class NoticeCmdTrigger : CmdTrigger
	{
		public NoticeCmdTrigger(StringStream args, IrcUser user) :
			this(args, user, null)
		{
		}

		public NoticeCmdTrigger(StringStream args, IrcUser user, IrcChannel chan)
			: base(args, user, chan)
		{
		}

		public override void Reply(string text)
		{
			Target.Notice(text);
		}
	}
}