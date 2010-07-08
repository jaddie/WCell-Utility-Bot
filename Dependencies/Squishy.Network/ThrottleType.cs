namespace Squishy.Network
{
	public enum ThrottleType
	{
		/// <summary>
		/// Represents a type of Throttle that throttles Upload.
		/// </summary>
		Up = 1,

		/// <summary>
		/// Represents a type of Throttle that throttles Download.
		/// </summary>
		Down = 2,

		/// <summary>
		/// Represents a type of Throttle that throttles both Upload and Download
		/// which equals <code>ThrottleType.Up|ThrottleType.Down</code>.
		/// </summary>
		Both = 3
	}
}