namespace Squishy.Network.Prot
{
	public interface Packet<K, V>
	{
		K Key { get; }

		V Content { get; }
	}
}