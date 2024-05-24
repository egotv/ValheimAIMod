using System;
using System.Threading;

public class SemaphoreIL2CPPFix
{
	private Mutex m_countLock = new Mutex();

	private int m_count;

	private readonly int m_maxCount;

	private readonly bool m_allowContextSwitch;

	public int CurrentCount => m_count;

	public SemaphoreIL2CPPFix(int initialCount, int maxCount, bool allowContextSwitch = false)
	{
		if (initialCount < 0)
		{
			throw new InvalidOperationException("initialCount must be greater than or equal to 0!");
		}
		if (maxCount <= 0)
		{
			throw new InvalidOperationException("maxCount must be greater than 0!");
		}
		m_count = initialCount;
		m_allowContextSwitch = allowContextSwitch;
		m_maxCount = maxCount;
	}

	public void Release()
	{
		m_countLock.WaitOne();
		if (m_count >= m_maxCount)
		{
			throw new InvalidOperationException("Can't increment semaphore when it's already at its max value!");
		}
		m_count++;
		m_countLock.ReleaseMutex();
	}

	public void Wait()
	{
		while (true)
		{
			m_countLock.WaitOne();
			if (m_count > 0)
			{
				break;
			}
			m_countLock.ReleaseMutex();
			if (m_allowContextSwitch)
			{
				Thread.Sleep(1);
			}
		}
		m_count--;
		m_countLock.ReleaseMutex();
	}
}
