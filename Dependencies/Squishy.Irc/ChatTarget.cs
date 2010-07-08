namespace Squishy.Irc
{
	/// <summary>
	/// User and Channel are the only entities that can be chatted with
	/// within an IRC netowrk and both implement this interface.
	/// </summary>
	public interface ChatTarget
	{
		/// <summary>
		/// The user's nick or channel's name. Something that uniquely defines a target on a network.
		/// </summary>
		string Identifier { get; }

		/// <summary>
		/// Send a PRIVMSG to the target (normal chat-message).
		/// </summary>
		void Msg(object format, params object[] args);

		/// <summary>
		/// Send a NOTICE to the target.
		/// </summary>
		void Notice(string line);
	}
}