using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZBroastcast : IDisposable
{
	public class HostData
	{
		public string m_host;

		public int m_port;

		public float m_timeout;
	}

	private List<HostData> m_hosts = new List<HostData>();

	private static ZBroastcast m_instance;

	private const int m_port = 6542;

	private const float m_pingInterval = 5f;

	private const float m_hostTimeout = 10f;

	private float m_timer;

	private int m_myPort;

	private Socket m_socket;

	private UdpClient m_listner;

	private Mutex m_lock = new Mutex();

	public static ZBroastcast instance => m_instance;

	public static void Initialize()
	{
		if (m_instance == null)
		{
			m_instance = new ZBroastcast();
		}
	}

	private ZBroastcast()
	{
		ZLog.Log("opening zbroadcast");
		m_socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
		m_socket.EnableBroadcast = true;
		try
		{
			m_listner = new UdpClient(6542);
			m_listner.EnableBroadcast = true;
			m_listner.BeginReceive(GotPackage, null);
		}
		catch (Exception ex)
		{
			m_listner = null;
			ZLog.Log("Error creating zbroadcast socket " + ex.ToString());
		}
	}

	public void SetServerPort(int port)
	{
		m_myPort = port;
	}

	public void Dispose()
	{
		ZLog.Log("Clozing zbroadcast");
		if (m_listner != null)
		{
			m_listner.Close();
		}
		m_socket.Close();
		m_lock.Close();
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public void Update(float dt)
	{
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = 5f;
			if (m_myPort != 0)
			{
				Ping();
			}
		}
		TimeoutHosts(dt);
	}

	private void GotPackage(IAsyncResult ar)
	{
		IPEndPoint remoteEP = new IPEndPoint(0L, 0);
		byte[] array;
		try
		{
			array = m_listner.EndReceive(ar, ref remoteEP);
		}
		catch (ObjectDisposedException)
		{
			return;
		}
		if (array.Length >= 5)
		{
			ZPackage zPackage = new ZPackage(array);
			if (zPackage.ReadChar() == 'F' && zPackage.ReadChar() == 'E' && zPackage.ReadChar() == 'J' && zPackage.ReadChar() == 'D')
			{
				int port = zPackage.ReadInt();
				m_lock.WaitOne();
				AddHost(remoteEP.Address.ToString(), port);
				m_lock.ReleaseMutex();
				m_listner.BeginReceive(GotPackage, null);
			}
		}
	}

	private void Ping()
	{
		IPEndPoint remoteEP = new IPEndPoint(IPAddress.Broadcast, 6542);
		ZPackage zPackage = new ZPackage();
		zPackage.Write('F');
		zPackage.Write('E');
		zPackage.Write('J');
		zPackage.Write('D');
		zPackage.Write(m_myPort);
		m_socket.SendTo(zPackage.GetArray(), remoteEP);
	}

	private void AddHost(string host, int port)
	{
		foreach (HostData host2 in m_hosts)
		{
			if (host2.m_port == port && host2.m_host == host)
			{
				host2.m_timeout = 0f;
				return;
			}
		}
		HostData hostData = new HostData();
		hostData.m_host = host;
		hostData.m_port = port;
		hostData.m_timeout = 0f;
		m_hosts.Add(hostData);
	}

	private void TimeoutHosts(float dt)
	{
		m_lock.WaitOne();
		foreach (HostData host in m_hosts)
		{
			host.m_timeout += dt;
			if (host.m_timeout > 10f)
			{
				m_hosts.Remove(host);
				return;
			}
		}
		m_lock.ReleaseMutex();
	}

	public void GetHostList(List<HostData> hosts)
	{
		hosts.AddRange(m_hosts);
	}
}
