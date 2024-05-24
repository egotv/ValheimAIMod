public class ZDOConnectionHashData
{
	public readonly ZDOExtraData.ConnectionType m_type;

	public readonly int m_hash;

	public ZDOConnectionHashData(ZDOExtraData.ConnectionType type, int hash)
	{
		m_type = type;
		m_hash = hash;
	}
}
