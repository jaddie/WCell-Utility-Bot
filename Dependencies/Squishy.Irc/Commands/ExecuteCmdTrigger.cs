using System;
using Squishy.Network;

namespace Squishy.Irc.Commands
{
	/// <summary>
	/// Uses the console for output.
	/// </summary>
	public class ExecuteCmdTrigger : CmdTrigger
	{
		public ExecuteCmdTrigger(StringStream args) : base(args)
		{
		}

		public ExecuteCmdTrigger(StringStream args, IrcUser user, IrcChannel chan)
			: base(args, user, chan)
		{
		}

		public override void Reply(string text)
		{
			Console.WriteLine(text);
		}
	}
}