using System;
using UnityEngine;

[Serializable]
public class Timer
{
	public float m_interval;

	private float m_startTime = -1f;

	private bool m_started;

	public Timer(float interval)
	{
		m_interval = interval;
	}

	public Timer()
	{
	}

	public void SetInerval(float i)
	{
		m_interval = i;
	}

	public void Start()
	{
		m_startTime = Time.time;
		m_started = true;
	}

	public void Stop()
	{
		m_started = false;
	}

	public bool IsStarted()
	{
		return m_started;
	}

	public bool IsDue()
	{
		if (m_started)
		{
			return Time.time >= m_startTime + m_interval;
		}
		return false;
	}
}
