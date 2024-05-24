public class ZDOConnection
{
	public readonly ZDOExtraData.ConnectionType m_type;

	public readonly ZDOID m_target = ZDOID.None;

	public ZDOConnection(ZDOExtraData.ConnectionType type, ZDOID target)
	{
		m_type = type;
		m_target = target;
	}
}
