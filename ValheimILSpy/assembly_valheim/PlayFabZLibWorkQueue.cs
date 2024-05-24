using System;
using System.Collections.Generic;
using System.Threading;
using Ionic.Zlib;

public class PlayFabZLibWorkQueue : IDisposable
{
	private static Thread s_thread;

	private static bool s_moreWork;

	private static readonly List<PlayFabZLibWorkQueue> s_workers = new List<PlayFabZLibWorkQueue>();

	private readonly Queue<byte[]> m_inCompress = new Queue<byte[]>();

	private readonly Queue<byte[]> m_outCompress = new Queue<byte[]>();

	private readonly Queue<byte[]> m_inDecompress = new Queue<byte[]>();

	private readonly Queue<byte[]> m_outDecompress = new Queue<byte[]>();

	private static Mutex s_workersMutex = new Mutex();

	private Mutex m_buffersMutex = new Mutex();

	private static SemaphoreSlim s_workSemaphore = new SemaphoreSlim(0, 1);

	public PlayFabZLibWorkQueue()
	{
		s_workersMutex.WaitOne();
		if (s_thread == null)
		{
			ZLog.Log("Semaphore type: " + s_workSemaphore.GetType().Name);
			s_thread = new Thread(WorkerMain);
			s_thread.Name = "PlayfabZlibThread";
			s_thread.Start();
		}
		s_workers.Add(this);
		s_workersMutex.ReleaseMutex();
	}

	public void Compress(byte[] buffer)
	{
		m_buffersMutex.WaitOne();
		m_inCompress.Enqueue(buffer);
		m_buffersMutex.ReleaseMutex();
		if (s_workSemaphore.CurrentCount < 1)
		{
			s_workSemaphore.Release();
		}
	}

	public void Decompress(byte[] buffer)
	{
		m_buffersMutex.WaitOne();
		m_inDecompress.Enqueue(buffer);
		m_buffersMutex.ReleaseMutex();
		if (s_workSemaphore.CurrentCount < 1)
		{
			s_workSemaphore.Release();
		}
	}

	public void Poll(out List<byte[]> compressedBuffers, out List<byte[]> decompressedBuffers)
	{
		compressedBuffers = null;
		decompressedBuffers = null;
		m_buffersMutex.WaitOne();
		if (m_outCompress.Count > 0)
		{
			compressedBuffers = new List<byte[]>();
			while (m_outCompress.Count > 0)
			{
				compressedBuffers.Add(m_outCompress.Dequeue());
			}
		}
		if (m_outDecompress.Count > 0)
		{
			decompressedBuffers = new List<byte[]>();
			while (m_outDecompress.Count > 0)
			{
				decompressedBuffers.Add(m_outDecompress.Dequeue());
			}
		}
		m_buffersMutex.ReleaseMutex();
	}

	private void WorkerMain()
	{
		while (true)
		{
			s_workSemaphore.Wait();
			s_workersMutex.WaitOne();
			foreach (PlayFabZLibWorkQueue s_worker in s_workers)
			{
				s_worker.Execute();
			}
			s_workersMutex.ReleaseMutex();
		}
	}

	private void Execute()
	{
		m_buffersMutex.WaitOne();
		DoUncompress();
		m_buffersMutex.ReleaseMutex();
		m_buffersMutex.WaitOne();
		DoCompress();
		m_buffersMutex.ReleaseMutex();
	}

	private void DoUncompress()
	{
		while (m_inDecompress.Count > 0)
		{
			try
			{
				byte[] payload = m_inDecompress.Dequeue();
				byte[] item = UncompressOnThisThread(payload);
				m_outDecompress.Enqueue(item);
			}
			catch
			{
			}
		}
	}

	private void DoCompress()
	{
		while (m_inCompress.Count > 0)
		{
			try
			{
				byte[] payload = m_inCompress.Dequeue();
				byte[] item = CompressOnThisThread(payload);
				m_outCompress.Enqueue(item);
			}
			catch
			{
			}
		}
	}

	public void Dispose()
	{
		s_workersMutex.WaitOne();
		s_workers.Remove(this);
		s_workersMutex.ReleaseMutex();
	}

	internal byte[] CompressOnThisThread(byte[] payload)
	{
		return ZlibStream.CompressBuffer(payload);
	}

	internal byte[] UncompressOnThisThread(byte[] payload)
	{
		return ZlibStream.UncompressBuffer(payload);
	}
}
