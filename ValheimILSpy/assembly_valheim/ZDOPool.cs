using System.Collections.Generic;
using UnityEngine;

public static class ZDOPool
{
	private const int c_BatchSize = 64;

	private static readonly Stack<ZDO> s_free = new Stack<ZDO>();

	private static int s_active;

	public static ZDO Create(ZDOID id, Vector3 position)
	{
		ZDO zDO = Get();
		zDO.Initialize(id, position);
		return zDO;
	}

	public static ZDO Create()
	{
		return Get();
	}

	public static void Release(Dictionary<ZDOID, ZDO> objects)
	{
		foreach (ZDO value in objects.Values)
		{
			Release(value);
		}
	}

	public static void Release(ZDO zdo)
	{
		zdo.Reset();
		s_free.Push(zdo);
		s_active--;
	}

	private static ZDO Get()
	{
		if (s_free.Count <= 0)
		{
			for (int i = 0; i < 64; i++)
			{
				ZDO item = new ZDO();
				s_free.Push(item);
			}
		}
		s_active++;
		ZDO zDO = s_free.Pop();
		zDO.Init();
		return zDO;
	}

	public static int GetPoolSize()
	{
		return s_free.Count;
	}

	public static int GetPoolActive()
	{
		return s_active;
	}

	public static int GetPoolTotal()
	{
		return s_active + s_free.Count;
	}
}
