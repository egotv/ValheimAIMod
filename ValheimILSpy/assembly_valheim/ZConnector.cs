using System;
using System.Net;
using System.Net.Sockets;

public class ZConnector : IDisposable
{
	private Socket m_socket;

	private IAsyncResult m_result;

	private IPEndPoint m_endPoint;

	private string m_host;

	private int m_port;

	private bool m_dnsError;

	private bool m_abort;

	private float m_timer;

	private static float m_timeout = 5f;

	public ZConnector(string host, int port)
	{
		m_host = host;
		m_port = port;
		ZLog.Log("Zconnect " + host + " " + port);
		Dns.BeginGetHostEntry(host, OnHostLookupDone, null);
	}

	public void Dispose()
	{
		Close();
	}

	private void Close()
	{
		if (m_socket != null)
		{
			try
			{
				if (m_socket.Connected)
				{
					m_socket.Shutdown(SocketShutdown.Both);
				}
			}
			catch (Exception ex)
			{
				ZLog.Log("Some excepetion when shuting down ZConnector socket, ignoring:" + ex);
			}
			m_socket.Close();
			m_socket = null;
		}
		m_abort = true;
	}

	public bool IsPeer(string host, int port)
	{
		if (m_host == host && m_port == port)
		{
			return true;
		}
		return false;
	}

	public bool UpdateStatus(float dt, bool logErrors = false)
	{
		if (m_abort)
		{
			ZLog.Log("ZConnector - Abort");
			return true;
		}
		if (m_dnsError)
		{
			ZLog.Log("ZConnector - dns error");
			return true;
		}
		if (m_result != null && m_result.IsCompleted)
		{
			ZLog.Log("ZConnector - result completed");
			return true;
		}
		m_timer += dt;
		if (m_timer > m_timeout)
		{
			ZLog.Log("ZConnector - timeout");
			Close();
			return true;
		}
		return false;
	}

	public ZSocket Complete()
	{
		if (m_socket != null && m_socket.Connected)
		{
			ZSocket result = new ZSocket(m_socket, m_host);
			m_socket = null;
			return result;
		}
		Close();
		return null;
	}

	public bool CompareEndPoint(IPEndPoint endpoint)
	{
		return m_endPoint.Equals(endpoint);
	}

	private void OnHostLookupDone(IAsyncResult res)
	{
		IPHostEntry iPHostEntry = Dns.EndGetHostEntry(res);
		if (m_abort)
		{
			ZLog.Log("Host lookup abort");
			return;
		}
		if (iPHostEntry.AddressList.Length == 0)
		{
			m_dnsError = true;
			ZLog.Log("Host lookup adress list empty");
			return;
		}
		ZLog.Log("Host lookup done , addresses: " + iPHostEntry.AddressList.Length);
		IPAddress[] addressList = iPHostEntry.AddressList;
		for (int i = 0; i < addressList.Length; i++)
		{
			ZLog.Log(" " + addressList[i]);
		}
		m_socket = ZSocket.CreateSocket();
		m_result = m_socket.BeginConnect(iPHostEntry.AddressList, m_port, null, null);
	}

	public string GetEndPointString()
	{
		return m_host + ":" + m_port;
	}

	public string GetHostName()
	{
		return m_host;
	}

	public int GetHostPort()
	{
		return m_port;
	}
}
