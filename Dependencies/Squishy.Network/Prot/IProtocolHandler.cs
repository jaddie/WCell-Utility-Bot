namespace Squishy.Network.Prot
{
	public interface IProtocolHandler
	{
		void HandleBytes(Connection con, ByteBuffer buf);
	}
}