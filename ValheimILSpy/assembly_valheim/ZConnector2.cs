using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class ZConnector2 : IDisposable
{
	private TcpClient m_socket;

	private IAsyncResult m_result;

	private IPEndPoint m_endPoint;

	private string m_host;

	private int m_port;

	private bool m_dnsError;

	private bool m_abort;

	private float m_timer;

	private static float m_timeout = 5f;

	public ZConnector2(string host, int port)
	{
		m_host = host;
		m_port = port;
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
			return true;
		}
		m_timer += dt;
		if (m_timer > m_timeout)
		{
			Close();
			return true;
		}
		return false;
	}

	public ZSocket2 Complete()
	{
		if (m_socket != null && m_socket.Connected)
		{
			ZSocket2 result = new ZSocket2(m_socket, m_host);
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
		}
		else if (iPHostEntry.AddressList.Length == 0)
		{
			m_dnsError = true;
			ZLog.Log("Host lookup adress list empty");
		}
		else
		{
			iPHostEntry.AddressList = KeepInetAddrs(iPHostEntry.AddressList);
			m_socket = ZSocket2.CreateSocket();
			m_result = m_socket.BeginConnect(iPHostEntry.AddressList, m_port, null, null);
		}
	}

	private IPAddress[] KeepInetAddrs(IPAddress[] inetAddrs)
	{
		List<IPAddress> list = new List<IPAddress>();
		foreach (IPAddress iPAddress in inetAddrs)
		{
			if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
			{
				list.Add(iPAddress);
			}
		}
		return list.ToArray();
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
