using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZSocket : IDisposable
{
	private Socket m_socket;

	private Mutex m_mutex = new Mutex();

	private Mutex m_sendMutex = new Mutex();

	private Queue<Socket> m_newConnections = new Queue<Socket>();

	private static int m_maxRecvBuffer = 10485760;

	private int m_recvOffset;

	private byte[] m_recvBuffer;

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

	public ZSocket()
	{
		m_socket = CreateSocket();
	}

	public static Socket CreateSocket()
	{
		return new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
		{
			NoDelay = true
		};
	}

	public ZSocket(Socket socket, string originalHostName = null)
	{
		m_socket = socket;
		m_originalHostName = originalHostName;
		try
		{
			m_endpoint = m_socket.RemoteEndPoint as IPEndPoint;
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
		if (m_socket != null)
		{
			try
			{
				if (m_socket.Connected)
				{
					m_socket.Shutdown(SocketShutdown.Both);
				}
			}
			catch (Exception)
			{
			}
			m_socket.Close();
		}
		m_socket = null;
		m_endpoint = null;
	}

	public static IPEndPoint GetEndPoint(string host, int port)
	{
		return new IPEndPoint(Dns.GetHostEntry(host).AddressList[0], port);
	}

	public bool Connect(string host, int port)
	{
		ZLog.Log("Connecting to " + host + " : " + port);
		IPEndPoint endPoint = GetEndPoint(host, port);
		m_socket.BeginConnect(endPoint, null, null).AsyncWaitHandle.WaitOne(3000, exitContext: true);
		if (!m_socket.Connected)
		{
			return false;
		}
		try
		{
			m_endpoint = m_socket.RemoteEndPoint as IPEndPoint;
		}
		catch
		{
			Close();
			return false;
		}
		BeginReceive();
		ZLog.Log(" connected");
		return true;
	}

	public bool StartHost(int port)
	{
		if (m_listenPort != 0)
		{
			Close();
		}
		if (!BindSocket(m_socket, IPAddress.Any, port, port + 10))
		{
			ZLog.LogWarning("Failed to bind socket");
			return false;
		}
		m_socket.Listen(100);
		m_socket.BeginAccept(AcceptCallback, m_socket);
		return true;
	}

	private bool BindSocket(Socket socket, IPAddress ipAddress, int startPort, int endPort)
	{
		for (int i = startPort; i <= endPort; i++)
		{
			try
			{
				IPEndPoint localEP = new IPEndPoint(ipAddress, i);
				m_socket.Bind(localEP);
				m_listenPort = i;
				ZLog.Log("Bound socket port " + i);
				return true;
			}
			catch
			{
				ZLog.Log("Failed to bind port:" + i);
			}
		}
		return false;
	}

	private void BeginReceive()
	{
		m_socket.BeginReceive(m_recvSizeBuffer, 0, m_recvSizeBuffer.Length, SocketFlags.None, PkgSizeReceived, m_socket);
	}

	private void PkgSizeReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = m_socket.EndReceive(res);
		}
		catch (Exception)
		{
			Disconnect();
			return;
		}
		m_totalRecv += num;
		if (num != 4)
		{
			Disconnect();
			return;
		}
		int num2 = BitConverter.ToInt32(m_recvSizeBuffer, 0);
		if (num2 == 0 || num2 > 10485760)
		{
			ZLog.LogError("Invalid pkg size " + num2);
			return;
		}
		m_lastRecvPkgSize = num2;
		m_recvOffset = 0;
		m_lastRecvPkgSize = num2;
		if (m_recvBuffer == null)
		{
			m_recvBuffer = new byte[m_maxRecvBuffer];
		}
		m_socket.BeginReceive(m_recvBuffer, m_recvOffset, m_lastRecvPkgSize, SocketFlags.None, PkgReceived, m_socket);
	}

	private void Disconnect()
	{
		if (m_socket != null)
		{
			try
			{
				m_socket.Disconnect(reuseSocket: true);
			}
			catch
			{
			}
		}
	}

	private void PkgReceived(IAsyncResult res)
	{
		int num;
		try
		{
			num = m_socket.EndReceive(res);
		}
		catch (Exception)
		{
			Disconnect();
			return;
		}
		m_totalRecv += num;
		m_recvOffset += num;
		if (m_recvOffset < m_lastRecvPkgSize)
		{
			int size = m_lastRecvPkgSize - m_recvOffset;
			if (m_recvBuffer == null)
			{
				m_recvBuffer = new byte[m_maxRecvBuffer];
			}
			m_socket.BeginReceive(m_recvBuffer, m_recvOffset, size, SocketFlags.None, PkgReceived, m_socket);
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

	private void AcceptCallback(IAsyncResult res)
	{
		Socket item;
		try
		{
			item = m_socket.EndAccept(res);
		}
		catch
		{
			Disconnect();
			return;
		}
		m_mutex.WaitOne();
		m_newConnections.Enqueue(item);
		m_mutex.ReleaseMutex();
		m_socket.BeginAccept(AcceptCallback, m_socket);
	}

	public ZSocket Accept()
	{
		if (m_newConnections.Count == 0)
		{
			return null;
		}
		Socket socket = null;
		m_mutex.WaitOne();
		if (m_newConnections.Count > 0)
		{
			socket = m_newConnections.Dequeue();
		}
		m_mutex.ReleaseMutex();
		if (socket != null)
		{
			return new ZSocket(socket);
		}
		return null;
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
		m_sendMutex.WaitOne();
		if (!m_isSending)
		{
			if (array.Length > 10485760)
			{
				ZLog.LogError("Too big data package: " + array.Length);
			}
			try
			{
				m_totalSent += bytes.Length;
				m_socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, PkgSent, null);
				m_isSending = true;
				m_sendQueue.Enqueue(array);
			}
			catch (Exception ex)
			{
				ZLog.Log("Handled exception in ZSocket:Send:" + ex);
				Disconnect();
			}
		}
		else
		{
			m_sendQueue.Enqueue(bytes);
			m_sendQueue.Enqueue(array);
		}
		m_sendMutex.ReleaseMutex();
	}

	private void PkgSent(IAsyncResult res)
	{
		m_sendMutex.WaitOne();
		if (m_sendQueue.Count > 0 && IsConnected())
		{
			byte[] array = m_sendQueue.Dequeue();
			try
			{
				m_totalSent += array.Length;
				m_socket.BeginSend(array, 0, array.Length, SocketFlags.None, PkgSent, null);
			}
			catch (Exception ex)
			{
				ZLog.Log("Handled exception in pkgsent:" + ex);
				m_isSending = false;
				Disconnect();
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

	public string GetEndPointHost()
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

	public bool IsSending()
	{
		if (!m_isSending)
		{
			return m_sendQueue.Count > 0;
		}
		return true;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = m_totalSent;
		totalRecv = m_totalRecv;
		m_totalSent = 0;
		m_totalRecv = 0;
	}
}
