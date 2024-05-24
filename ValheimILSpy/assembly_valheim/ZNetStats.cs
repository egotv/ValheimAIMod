using UnityEngine;

public class ZNetStats
{
	private int m_recvBytes;

	private int m_statRecvBytes;

	private int m_sentBytes;

	private int m_statSentBytes;

	private float m_recvRate;

	private float m_sendRate;

	private float m_statStart = Time.time;

	internal void IncRecvBytes(int count)
	{
		m_recvBytes += count;
	}

	internal void IncSentBytes(int count)
	{
		m_sentBytes += count;
	}

	public void GetAndResetStats(out int totalSent, out int totalRecv)
	{
		totalSent = m_sentBytes;
		totalRecv = m_recvBytes;
		m_sentBytes = 0;
		m_statSentBytes = 0;
		m_recvBytes = 0;
		m_statRecvBytes = 0;
		m_statStart = Time.time;
	}

	public void GetConnectionQuality(out float localQuality, out float remoteQuality, out int ping, out float outByteSec, out float inByteSec)
	{
		float num = Time.time - m_statStart;
		if (num >= 1f)
		{
			m_sendRate = ((float)(m_sentBytes - m_statSentBytes) / num * 2f + m_sendRate) / 3f;
			m_recvRate = ((float)(m_recvBytes - m_statRecvBytes) / num * 2f + m_recvRate) / 3f;
			m_statSentBytes = m_sentBytes;
			m_statRecvBytes = m_recvBytes;
			m_statStart = Time.time;
		}
		localQuality = 0f;
		remoteQuality = 0f;
		ping = 0;
		outByteSec = m_sendRate;
		inByteSec = m_recvRate;
	}
}
