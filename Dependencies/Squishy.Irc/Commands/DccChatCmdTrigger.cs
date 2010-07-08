using Squishy.Irc.Dcc;
using Squishy.Network;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// Triggers through DCC-chat
	/// </summary>
	public class DccChatCmdTrigger : CmdTrigger
	{
		public DccChatCmdTrigger(StringStream args, IrcUser user)
			: base(args, user, null)
		{
		}

		public override void Reply(string text)
		{
			DccChatClient client = Irc.Dcc.GetChatClient(User);
			//if (client != null) {
			client.Send(text);
			//}
		}
	}
}