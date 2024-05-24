using System;
using System.IO;

public class ZNat : IDisposable
{
	private FileStream m_output;

	private bool m_mappingOK;

	private int m_port;

	public void Dispose()
	{
	}

	public void SetPort(int port)
	{
		if (m_port != port)
		{
			m_port = port;
		}
	}

	public void Update(float dt)
	{
	}

	public bool GetStatus()
	{
		return m_mappingOK;
	}
}
