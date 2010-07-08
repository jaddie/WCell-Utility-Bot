namespace Squishy.Irc.Commands
{
	public interface ICommandAccessController
	{
		/// <summary>
		/// Needs to return wether or not people are allowed to use certain commands.
		/// </summary>
		bool MayExecute(CmdTrigger trigger, Command cmd);
	}
}