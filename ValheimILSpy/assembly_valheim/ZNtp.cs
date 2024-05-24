using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class ZNtp : IDisposable
{
	private static ZNtp m_instance;

	private DateTime m_ntpTime;

	private bool m_status;

	private bool m_stop;

	private Thread m_ntpThread;

	private Mutex m_lock = new Mutex();

	public static ZNtp instance => m_instance;

	public ZNtp()
	{
		m_instance = this;
		m_ntpTime = DateTime.UtcNow;
		m_ntpThread = new Thread(NtpThread);
		m_ntpThread.Start();
	}

	public void Dispose()
	{
		if (m_ntpThread != null)
		{
			ZLog.Log("Stoping ntp thread");
			m_lock.WaitOne();
			m_stop = true;
			m_ntpThread.Abort();
			m_lock.ReleaseMutex();
			m_ntpThread = null;
		}
		if (m_lock != null)
		{
			m_lock.Close();
			m_lock = null;
		}
	}

	public bool GetStatus()
	{
		return m_status;
	}

	public void Update(float dt)
	{
		m_lock.WaitOne();
		m_ntpTime = m_ntpTime.AddSeconds(dt);
		m_lock.ReleaseMutex();
	}

	private void NtpThread()
	{
		while (!m_stop)
		{
			if (GetNetworkTime("pool.ntp.org", out var time))
			{
				m_status = true;
				m_lock.WaitOne();
				m_ntpTime = time;
				m_lock.ReleaseMutex();
			}
			else
			{
				m_status = false;
			}
			Thread.Sleep(60000);
		}
	}

	public DateTime GetTime()
	{
		return m_ntpTime;
	}

	private bool GetNetworkTime(string ntpServer, out DateTime time)
	{
		byte[] array = new byte[48];
		array[0] = 27;
		IPAddress[] addressList;
		try
		{
			addressList = Dns.GetHostEntry(ntpServer).AddressList;
			if (addressList.Length == 0)
			{
				ZLog.Log("Dns lookup failed");
				time = DateTime.UtcNow;
				return false;
			}
		}
		catch
		{
			ZLog.Log("Failed ntp dns lookup");
			time = DateTime.UtcNow;
			return false;
		}
		IPEndPoint remoteEP = new IPEndPoint(addressList[0], 123);
		Socket socket = null;
		try
		{
			socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			socket.ReceiveTimeout = 3000;
			socket.SendTimeout = 3000;
			socket.Connect(remoteEP);
			if (!socket.Connected)
			{
				ZLog.Log("Failed to connect to ntp");
				time = DateTime.UtcNow;
				socket.Close();
				return false;
			}
			socket.Send(array);
			socket.Receive(array);
			socket.Shutdown(SocketShutdown.Both);
			socket.Close();
		}
		catch
		{
			socket?.Close();
			time = DateTime.UtcNow;
			return false;
		}
		ulong num = ((ulong)array[40] << 24) | ((ulong)array[41] << 16) | ((ulong)array[42] << 8) | array[43];
		ulong num2 = ((ulong)array[44] << 24) | ((ulong)array[45] << 16) | ((ulong)array[46] << 8) | array[47];
		ulong num3 = num * 1000 + num2 * 1000 / 4294967296L;
		time = new DateTime(1900, 1, 1).AddMilliseconds((long)num3);
		return true;
	}
}
