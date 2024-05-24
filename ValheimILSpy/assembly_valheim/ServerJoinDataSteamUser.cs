using Steamworks;

public class ServerJoinDataSteamUser : ServerJoinData
{
	public const string typeName = "Steam user";

	public CSteamID m_joinUserID { get; private set; }

	public ServerJoinDataSteamUser(ulong joinUserID)
	{
		m_joinUserID = new CSteamID(joinUserID);
		m_serverName = ToString();
	}

	public ServerJoinDataSteamUser(CSteamID joinUserID)
	{
		m_joinUserID = joinUserID;
		m_serverName = ToString();
	}

	public override bool IsValid()
	{
		return m_joinUserID.IsValid();
	}

	public override string GetDataName()
	{
		return "Steam user";
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerJoinDataSteamUser serverJoinDataSteamUser && base.Equals(obj))
		{
			return m_joinUserID.Equals(serverJoinDataSteamUser.m_joinUserID);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (-995281327 * -1521134295 + base.GetHashCode()) * -1521134295 + m_joinUserID.GetHashCode();
	}

	public static bool operator ==(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		if ((object)left == null || (object)right == null)
		{
			if ((object)left == null)
			{
				return (object)right == null;
			}
			return false;
		}
		return left.Equals(right);
	}

	public static bool operator !=(ServerJoinDataSteamUser left, ServerJoinDataSteamUser right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return m_joinUserID.ToString();
	}
}
