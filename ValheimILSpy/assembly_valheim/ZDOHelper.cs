using System;
using System.Collections.Generic;
using System.Linq;

public static class ZDOHelper
{
	public static readonly HashSet<int> s_stripOldData = new HashSet<int>
	{
		"generated".GetStableHashCode(),
		"patrolSpawnPoint".GetStableHashCode(),
		"autoDespawn".GetStableHashCode(),
		"targetHear".GetStableHashCode(),
		"targetSee".GetStableHashCode(),
		"burnt0".GetStableHashCode(),
		"burnt1".GetStableHashCode(),
		"burnt2".GetStableHashCode(),
		"burnt3".GetStableHashCode(),
		"burnt4".GetStableHashCode(),
		"burnt5".GetStableHashCode(),
		"burnt6".GetStableHashCode(),
		"burnt7".GetStableHashCode(),
		"burnt8".GetStableHashCode(),
		"burnt9".GetStableHashCode(),
		"burnt10".GetStableHashCode(),
		"LookDir".GetStableHashCode(),
		"RideSpeed".GetStableHashCode()
	};

	public static readonly List<int> s_stripOldLongData = new List<int>
	{
		ZDOVars.s_zdoidUser.Key,
		ZDOVars.s_zdoidUser.Value,
		ZDOVars.s_zdoidRodOwner.Key,
		ZDOVars.s_zdoidRodOwner.Value,
		ZDOVars.s_sessionCatchID.Key,
		ZDOVars.s_sessionCatchID.Value
	};

	public static readonly List<int> s_stripOldDataByteArray = new List<int> { "health".GetStableHashCode() };

	public static string ToStringFast(this ZDOExtraData.ConnectionType value)
	{
		return (value & ~ZDOExtraData.ConnectionType.Target) switch
		{
			ZDOExtraData.ConnectionType.Portal => "Portal", 
			ZDOExtraData.ConnectionType.SyncTransform => "SyncTransform", 
			ZDOExtraData.ConnectionType.Spawned => "Spawned", 
			_ => value.ToString(), 
		};
	}

	public static TValue GetValueOrDefaultPiktiv<TKey, TValue>(this IDictionary<TKey, TValue> container, TKey zid, TValue defaultValue)
	{
		if (!container.ContainsKey(zid))
		{
			return defaultValue;
		}
		return container[zid];
	}

	public static bool InitAndSet<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType value)
	{
		container.Init(zid);
		return container[zid].SetValue(hash, value);
	}

	public static bool Update<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType value)
	{
		return container[zid].SetValue(hash, value);
	}

	public static void InitAndReserve<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int size)
	{
		container.Init(zid);
		container[zid].Reserve(size);
	}

	public static List<ZDOID> GetAllZDOIDsWithHash<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, int hash)
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> item in container)
		{
			foreach (KeyValuePair<int, TType> item2 in item.Value)
			{
				if (item2.Key == hash)
				{
					list.Add(item.Key);
					break;
				}
			}
		}
		return list;
	}

	public static List<KeyValuePair<int, TType>> GetValuesOrEmpty<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			return Array.Empty<KeyValuePair<int, TType>>().ToList();
		}
		return container[zid].ToList();
	}

	public static bool GetValue<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, out TType value)
	{
		if (!container.ContainsKey(zid))
		{
			value = default(TType);
			return false;
		}
		return container[zid].TryGetValue(hash, out value);
	}

	public static TType GetValueOrDefault<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid, int hash, TType defaultValue)
	{
		if (!container.ContainsKey(zid))
		{
			return defaultValue;
		}
		return container[zid].GetValueOrDefault(hash, defaultValue);
	}

	public static void Release<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (container.ContainsKey(zid))
		{
			container[zid].Clear();
			Pool<BinarySearchDictionary<int, TType>>.Release(container[zid]);
			container[zid] = null;
			container.Remove(zid);
		}
	}

	private static void Init<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID zid)
	{
		if (!container.ContainsKey(zid))
		{
			container.Add(zid, Pool<BinarySearchDictionary<int, TType>>.Create());
		}
	}

	public static bool Remove<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container, ZDOID id, int hash)
	{
		if (!container.ContainsKey(id) || !container[id].ContainsKey(hash))
		{
			return false;
		}
		container[id].Remove(hash);
		if (container[id].Count == 0)
		{
			Pool<BinarySearchDictionary<int, TType>>.Release(container[id]);
			container[id] = null;
			container.Remove(id);
		}
		return true;
	}

	public static Dictionary<ZDOID, BinarySearchDictionary<int, TType>> Clone<TType>(this Dictionary<ZDOID, BinarySearchDictionary<int, TType>> container)
	{
		return container.ToDictionary((KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> entry) => entry.Key, (KeyValuePair<ZDOID, BinarySearchDictionary<int, TType>> entry) => (BinarySearchDictionary<int, TType>)entry.Value.Clone());
	}

	public static Dictionary<ZDOID, ZDOConnectionHashData> Clone(this Dictionary<ZDOID, ZDOConnectionHashData> container)
	{
		return container.ToDictionary((KeyValuePair<ZDOID, ZDOConnectionHashData> entry) => entry.Key, (KeyValuePair<ZDOID, ZDOConnectionHashData> entry) => entry.Value);
	}
}
