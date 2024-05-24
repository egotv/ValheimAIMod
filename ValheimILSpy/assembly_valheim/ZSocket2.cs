using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZSocket2 : ZNetStats, IDisposable, ISocket
{
	private TcpListener m_listner;

	private TcpClient m_socket;

	private Mutex m_mutex = new Mutex();

	private Mutex m_sendMutex = new Mutex();

	private static int m_maxRecvBuffer = 10485760;

	private int m_recvOffset;

	private byte[] m_recvBuffer;

	private int m_recvSizeOffset;

	private byte[] m_recvSizeBuffer = new byte[4];

	private Queue<ZPackage> m_pkgQueue = new Queue<ZPackage>();

	private bool m_isSending;

	private Queue<byte[]> m_sendQueue = new Queue<byte[]>();

	private IPEndPoint m_endpoint;

	private string m_originalHostName;

	private int m_listenPort;

	private int m_lastRecvPkgSize;

	private int m_totalSent;

	private int m_totalRecv;

	private bool m_gotData;

	public ZSocket2()
	{
	}

	public static TcpClient CreateSocket()
	{
		TcpClient tcpClient = new TcpClient(AddressFamily.InterNetwork);
		ConfigureSocket(tcpClient);
		return tcpClient;
	}

	private static void ConfigureSocket(TcpClient socket)
	{
		socket.NoDelay = true;
		socket.SendBufferSize = 2048;
	}

	public ZSocket2(TcpClient socket, string originalHostName = null)
	{
		m_socket = socket;
		m_originalHostName = originalHostName;
		try
		{
			m_endpoint = m_socket.Client.RemoteEndPoint as IPEndPoint;
		}
		catch
		{
			Close();
			return;
		}
		BeginReceive();
	}

	public void Dispose()
	{
		Close();
		m_mutex.Close();
		m_sendMutex.Close();
		m_recvBuffer = null;
	}

	public void Close()
	{
		ZLog.Log("Closing socket " + GetEndPointString());
		if (m_listner != null)
		{
			m_listner.Stop();
			m_listner = null;
		}
		if (m_socket != null)
		{
			m_socket.Close();
			m_socket = null;
		}
		m_endpoint = null;
	}

	public static IPEndPoint GetEndPoint(string host, int port)
	{
		return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
	}

	public bool StartHost(int port)
	{
		if (m_listner != null)
		{
			m_listner.Stop();
			m_listner = null;
		}
		if (!BindSocket(port, port + 10))
		{
			ZLog.LogWarning("Failed to bind socket");
			return false;
		}
		return true;
	}

	private bool BindSocket(int startPort, int endPort)
	{
		for (int i = startPort; i <= endPort; i++)
		{
			try
			{
				m_listner = new TcpListener(IPAddress.Any, i);
				m_listner.Start();
				m_listenPort = i;
				ZLog.Log("Bound socket port " + i);
				return true;
			}
			catch
			{
				ZLog.Log("Failed to bind port:" + i);
				m_listner = null;
			}
		}
		return false;
	}

	private void BeginReceive()
	{
		m_recvSizeOffset = 0;
		m_socket.GetStream().BeginRead(m_recvSizeBuffer, 0, m_recvSizeBuffer.Length, PkgSizeReceived, m_socket);
	}

	private void PkgSizeReceived(IAsyncResult res)
	{
		if (m_socket == null || !m_socket.Connected)
		{
			ZLog.LogWarning("PkgSizeReceived socket closed");
			Close();
			return;
		}
		int num;
		try
		{
			num = m_socket.GetStream().EndRead(res);
		}
		catch (Exception ex)
		{
			ZLog.LogWarning("PkgSizeReceived exception " + ex.ToString());
			Close();
			return;
		}
		if (num == 0)
		{
			ZLog.LogWarning("PkgSizeReceived Got 0 bytes data,closing socket");
			Close();
			return;
		}
		m_gotData = true;
		m_recvSizeOffset += num;
		if (m_recvSizeOffset < m_recvSizeBuffer.Length)
		{
			int count = m_recvSizeBuffer.Length - m_recvOffset;
			m_socket.GetStream().BeginRead(m_recvSizeBuffer, m_recvSizeOffset, count, PkgSizeReceived, m_socket);
			return;
		}
		int num2 = BitConverter.ToInt32(m_recvSizeBuffer, 0);
		if (num2 == 0 || num2 > 10485760)
		{
			ZLog.LogError("PkgSizeReceived Invalid pkg size " + num2);
			return;
		}
		m_lastRecvPkgSize = num2;
		m_recvOffset = 0;
		m_lastRecvPkgSize = num2;
		if (m_recvBuffer == null)
		{
			m_recvBuffer = new byte[m_maxRecvBuffer];
		}
		m_socket.GetStream().BeginRead(m_recvBuffer, m_recvOffset, m_lastRecvPkgSize, PkgReceived, m_socket);
	}

	private void PkgReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = m_socket.GetStream().EndRead(res);
		}
		catch (Exception ex)
		{
			ZLog.Log("PkgReceived error " + ex.ToString());
			Close();
			return;
		}
		if (num == 0)
		{
			ZLog.LogWarning("PkgReceived: Got 0 bytes data,closing socket");
			Close();
			return;
		}
		m_gotData = true;
		m_totalRecv += num;
		m_recvOffset += num;
		IncRecvBytes(num);
		if (m_recvOffset < m_lastRecvPkgSize)
		{
			int count = m_lastRecvPkgSize - m_recvOffset;
			if (m_recvBuffer == null)
			{
				m_recvBuffer = new byte[m_maxRecvBuffer];
			}
			m_socket.GetStream().BeginRead(m_recvBuffer, m_recvOffset, count, PkgReceived, m_socket);
		}
		else
		{
			ZPackage item = new ZPackage(m_recvBuffer, m_lastRecvPkgSize);
			m_mutex.WaitOne();
			m_pkgQueue.Enqueue(item);
			m_mutex.ReleaseMutex();
			BeginReceive();
		}
	}

	public ISocket Accept()
	{
		if (m_listner == null)
		{
			return null;
		}
		if (!m_listner.Pending())
		{
			return null;
		}
		TcpClient socket = m_listner.AcceptTcpClient();
		ConfigureSocket(socket);
		return new ZSocket2(socket);
	}

	public bool IsConnected()
	{
		if (m_socket != null)
		{
			return m_socket.Connected;
		}
		return false;
	}

	public void Send(ZPackage pkg)
	{
		if (pkg.Size() == 0 || m_socket == null || !m_socket.Connected)
		{
			return;
		}
		byte[] array = pkg.GetArray();
		byte[] bytes = BitConverter.GetBytes(array.Length);
		byte[] array2 = new byte[array.Length + bytes.Length];
		bytes.CopyTo(array2, 0);
		array.CopyTo(array2, 4);
		IncSentBytes(array.Length);
		m_sendMutex.WaitOne();
		if (!m_isSending)
		{
			if (array2.Length > 10485760)
			{
				ZLog.LogError("Too big data package: " + array2.Length);
			}
			try
			{
				m_totalSent += array2.Length;
				m_socket.GetStream().BeginWrite(array2, 0, array2.Length, PkgSent, m_socket);
				m_isSending = true;
			}
			catch (Exception ex)
			{
				ZLog.Log("Handled exception in ZSocket:Send:" + ex);
				Close();
			}
		}
		else
		{
			m_sendQueue.Enqueue(array2);
		}
		m_sendMutex.ReleaseMutex();
	}

	private void PkgSent(IAsyncResult res)
	{
		try
		{
			m_socket.GetStream().EndWrite(res);
		}
		catch (Exception ex)
		{
			ZLog.Log("PkgSent error " + ex.ToString());
			Close();
			return;
		}
		m_sendMutex.WaitOne();
		if (m_sendQueue.Count > 0 && IsConnected())
		{
			byte[] array = m_sendQueue.Dequeue();
			try
			{
				m_totalSent += array.Length;
				m_socket.GetStream().BeginWrite(array, 0, array.Length, PkgSent, m_socket);
			}
			catch (Exception ex2)
			{
				ZLog.Log("Handled exception in pkgsent:" + ex2);
				m_isSending = false;
				Close();
			}
		}
		else
		{
			m_isSending = false;
		}
		m_sendMutex.ReleaseMutex();
	}

	public ZPackage Recv()
	{
		if (m_socket == null)
		{
			return null;
		}
		if (m_pkgQueue.Count == 0)
		{
			return null;
		}
		ZPackage result = null;
		m_mutex.WaitOne();
		if (m_pkgQueue.Count > 0)
		{
			result = m_pkgQueue.Dequeue();
		}
		m_mutex.ReleaseMutex();
		return result;
	}

	public string GetEndPointString()
	{
		if (m_endpoint != null)
		{
			return m_endpoint.ToString();
		}
		return "None";
	}

	public string GetHostName()
	{
		if (m_endpoint != null)
		{
			return m_endpoint.Address.ToString();
		}
		return "None";
	}

	public IPEndPoint GetEndPoint()
	{
		return m_endpoint;
	}

	public bool IsPeer(string host, int port)
	{
		if (!IsConnected())
		{
			return false;
		}
		if (m_endpoint == null)
		{
			return false;
		}
		IPEndPoint endpoint = m_endpoint;
		if (endpoint.Address.ToString() == host && endpoint.Port == port)
		{
			return true;
		}
		if (m_originalHostName != null && m_originalHostName == host && endpoint.Port == port)
		{
			return true;
		}
		return false;
	}

	public bool IsHost()
	{
		return m_listenPort != 0;
	}

	public int GetHostPort()
	{
		return m_listenPort;
	}

	public int GetSendQueueSize()
	{
		if (!IsConnected())
		{
			return 0;
		}
		m_sendMutex.WaitOne();
		int num = 0;
		foreach (byte[] item in m_sendQueue)
		{
			num += item.Length;
		}
		m_sendMutex.ReleaseMutex();
		return num;
	}

	public bool IsSending()
	{
		if (!m_isSending)
		{
			return m_sendQueue.Count > 0;
		}
		return true;
	}

	public bool GotNewData()
	{
		bool gotData = m_gotData;
		m_gotData = false;
		return gotData;
	}

	public bool Flush()
	{
		return true;
	}

	public int GetCurrentSendRate()
	{
		return 0;
	}

	public int GetAverageSendRate()
	{
		return 0;
	}

	public void VersionMatch()
	{
	}
}
