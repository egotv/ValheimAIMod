using System;
using System.Net;
using System.Net.Sockets;

public abstract class ServerJoinData
{
	public string m_serverName;

	public virtual bool IsValid()
	{
		return false;
	}

	public virtual string GetDataName()
	{
		return "";
	}

	public override bool Equals(object obj)
	{
		return obj is ServerJoinData;
	}

	public override int GetHashCode()
	{
		return 0;
	}

	public static bool operator ==(ServerJoinData left, ServerJoinData right)
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

	public static bool operator !=(ServerJoinData left, ServerJoinData right)
	{
		return !(left == right);
	}

	public static bool URLToIP(string url, out IPAddress ip)
	{
		try
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(url);
			if (hostAddresses.Length == 0)
			{
				ip = null;
				return false;
			}
			ZLog.Log("Got dns entries: " + hostAddresses.Length);
			IPAddress[] array = hostAddresses;
			foreach (IPAddress iPAddress in array)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					ip = iPAddress;
					return true;
				}
			}
			ip = null;
			return false;
		}
		catch (Exception ex)
		{
			ZLog.Log("Exception while finding ip:" + ex.ToString());
			ip = null;
			return false;
		}
	}
}
