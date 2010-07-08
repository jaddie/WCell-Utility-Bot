namespace Squishy.Irc
{
	/// <summary>
	/// Unused
	/// TODO: Define complete Protocol graph that defines actions and reactions.
	/// Problem: Packets that contain more complex information (eg MODE etc)
	/// </summary>
	public enum IrcToken
	{
		Server,
		Nick,
		Mask,
		Action,
		User,
		Channel,
		Args,
		Other
	}
}