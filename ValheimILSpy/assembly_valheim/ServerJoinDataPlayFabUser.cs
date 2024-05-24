public class ServerJoinDataPlayFabUser : ServerJoinData
{
	public const string typeName = "PlayFab user";

	public string m_remotePlayerId { get; private set; }

	public ServerJoinDataPlayFabUser(string remotePlayerId)
	{
		m_remotePlayerId = remotePlayerId;
		m_serverName = ToString();
	}

	public override bool IsValid()
	{
		return m_remotePlayerId != null;
	}

	public override string GetDataName()
	{
		return "PlayFab user";
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerJoinDataPlayFabUser serverJoinDataPlayFabUser && base.Equals(obj))
		{
			return ToString() == serverJoinDataPlayFabUser.ToString();
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (1688301347 * -1521134295 + base.GetHashCode()) * -1521134295 + ToString().GetHashCode();
	}

	public static bool operator ==(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
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

	public static bool operator !=(ServerJoinDataPlayFabUser left, ServerJoinDataPlayFabUser right)
	{
		return !(left == right);
	}

	public override string ToString()
	{
		return m_remotePlayerId;
	}
}
