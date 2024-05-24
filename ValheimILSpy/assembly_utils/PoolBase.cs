using System;
using UnityEngine.Pool;

public abstract class PoolBase<T> where T : class
{
	protected T m_prefab;

	private ObjectPool<T> m_pool;

	private ObjectPool<T> Pool
	{
		get
		{
			if (m_pool == null)
			{
				throw new InvalidOperationException("You need to call InitPool before using it.");
			}
			return m_pool;
		}
		set
		{
			m_pool = value;
		}
	}

	public void InitPool(T prefab, int initial = 10, int max = 20, bool collectionChecks = false)
	{
		m_prefab = prefab;
		Pool = new ObjectPool<T>(CreateSetup, GetSetup, ReleaseSetup, DestroySetup, collectionChecks, initial, max);
	}

	protected abstract T CreateSetup();

	protected abstract void DestroySetup(T obj);

	protected abstract void GetSetup(T obj);

	protected abstract void ReleaseSetup(T obj);

	public T Get()
	{
		return Pool.Get();
	}

	public void Release(T obj)
	{
		Pool.Release(obj);
	}
}
