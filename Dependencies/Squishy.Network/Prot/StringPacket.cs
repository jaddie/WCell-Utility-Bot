namespace Squishy.Network.Prot
{
	public abstract class StringPacket : Packet<string, StringStream>
	{
		private readonly string key;
		protected StringStream content;

		public StringPacket(string key, StringStream content)
		{
			this.key = key;
			this.content = content;
		}

		#region Packet<string,StringStream> Members

		public string Key
		{
			get { return key; }
		}

		public StringStream Content
		{
			get { return content; }
		}

		#endregion
	}
}