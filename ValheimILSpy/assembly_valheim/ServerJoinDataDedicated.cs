using System;
using System.ComponentModel;

public class ServerJoinDataDedicated : ServerJoinData
{
	public enum AddressType
	{
		None,
		IP,
		URL
	}

	public const string typeName = "Dedicated";

	private bool? m_isValid;

	private string m_ipString;

	public AddressType AddressVariant { get; private set; }

	public string m_host { get; private set; }

	public ushort m_port { get; private set; }

	public ServerJoinDataDedicated(string address)
	{
		string[] array = address.Split(':', StringSplitOptions.None);
		if (array.Length < 1 || array.Length > 2)
		{
			m_isValid = false;
			return;
		}
		SetHost(array[0]);
		if (array.Length == 2 && ushort.TryParse(array[1], out var result))
		{
			m_port = result;
		}
		else
		{
			m_port = 2456;
		}
		m_serverName = ToString();
	}

	public ServerJoinDataDedicated(string host, ushort port)
	{
		if (host.Split(':', StringSplitOptions.None).Length != 1)
		{
			m_isValid = false;
			return;
		}
		SetHost(host);
		m_port = port;
		m_serverName = ToString();
	}

	public ServerJoinDataDedicated(uint host, ushort port)
	{
		SetHost(host);
		m_port = port;
		m_serverName = ToString();
	}

	public override bool IsValid()
	{
		if (m_isValid.HasValue)
		{
			return m_isValid.Value;
		}
		if (m_ipString == null)
		{
			m_isValid = ServerJoinData.URLToIP(m_host, out var ip);
			if (m_isValid.Value)
			{
				byte[] addressBytes = ip.GetAddressBytes();
				m_ipString = BytesToIPString(addressBytes);
			}
			return m_isValid.Value;
		}
		ZLog.LogError("This part of the code should never run!");
		return false;
	}

	public static string BytesToIPString(byte[] ipBytes)
	{
		string text = ipBytes[0].ToString();
		for (int i = 1; i < 4; i++)
		{
			text = text + "." + ipBytes[i];
		}
		return text;
	}

	public static string UIntToIPString(uint host)
	{
		string text = "";
		uint num = 255u;
		for (int num2 = 24; num2 >= 0; num2 -= 8)
		{
			text += ((num << num2) & host) >> num2;
			if (num2 != 0)
			{
				text += ".";
			}
		}
		return text;
	}

	public void IsValidAsync(Action<bool> resultCallback)
	{
		bool result = false;
		BackgroundWorker backgroundWorker = new BackgroundWorker();
		backgroundWorker.DoWork += delegate
		{
			result = IsValid();
		};
		backgroundWorker.RunWorkerCompleted += delegate
		{
			resultCallback(result);
		};
		backgroundWorker.RunWorkerAsync();
	}

	public override string GetDataName()
	{
		return "Dedicated";
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerJoinDataDedicated serverJoinDataDedicated && base.Equals(obj) && m_host == serverJoinDataDedicated.m_host)
		{
			return m_port == serverJoinDataDedicated.m_port;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return ((-468063053 * -1521134295 + base.GetHashCode()) * -1521134295 + m_host.GetHashCode()) * -1521134295 + m_port.GetHashCode();
	}

	public static bool operator ==(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
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

	public static bool operator !=(ServerJoinDataDedicated left, ServerJoinDataDedicated right)
	{
		return !(left == right);
	}

	private void SetHost(uint host)
	{
		string ipString = (m_host = UIntToIPString(host));
		m_ipString = ipString;
		m_isValid = true;
		AddressVariant = AddressType.IP;
	}

	private void SetHost(string host)
	{
		string[] array = host.Split('.', StringSplitOptions.None);
		if (array.Length == 4)
		{
			byte[] array2 = new byte[4];
			bool flag = true;
			for (int i = 0; i < 4; i++)
			{
				if (!byte.TryParse(array[i], out array2[i]))
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				m_host = BytesToIPString(array2);
				m_ipString = m_host;
				m_isValid = true;
				AddressVariant = AddressType.IP;
				return;
			}
		}
		string text = host;
		if (!host.StartsWith("http://") && !host.StartsWith("https://"))
		{
			text = "http://" + host;
		}
		if (!host.EndsWith("/"))
		{
			text += "/";
		}
		if (Uri.TryCreate(text, UriKind.Absolute, out var _))
		{
			m_host = host;
			m_isValid = null;
			AddressVariant = AddressType.URL;
		}
		else
		{
			m_host = host;
			m_isValid = false;
			AddressVariant = AddressType.None;
		}
	}

	public string GetHost()
	{
		return m_host;
	}

	public string GetIPString()
	{
		if (!IsValid())
		{
			ZLog.LogError("Can't get IP from invalid server data");
			return null;
		}
		return m_ipString;
	}

	public string GetIPPortString()
	{
		return string.Concat(GetIPString() + ":", m_port.ToString());
	}

	public override string ToString()
	{
		return string.Concat(GetHost() + ":", m_port.ToString());
	}
}
