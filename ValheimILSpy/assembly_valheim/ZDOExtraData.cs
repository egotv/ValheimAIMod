using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ZDOExtraData
{
	public enum Type
	{
		Float,
		Vec3,
		Quat,
		Int,
		Long,
		String,
		ByteArray
	}

	[Flags]
	public enum ConnectionType : byte
	{
		None = 0,
		Portal = 1,
		SyncTransform = 2,
		Spawned = 3,
		Target = 0x10
	}

	private static readonly Dictionary<ZDOID, long> s_tempTimeCreated = new Dictionary<ZDOID, long>();

	private static int s_uniqueHashes = 0;

	private static readonly HashSet<int> s_usedHashes = new HashSet<int>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, float>> s_floats = new Dictionary<ZDOID, BinarySearchDictionary<int, float>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>> s_vec3 = new Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>> s_quats = new Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, int>> s_ints = new Dictionary<ZDOID, BinarySearchDictionary<int, int>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, long>> s_longs = new Dictionary<ZDOID, BinarySearchDictionary<int, long>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, string>> s_strings = new Dictionary<ZDOID, BinarySearchDictionary<int, string>>();

	private static readonly Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>> s_byteArrays = new Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>>();

	private static readonly Dictionary<ZDOID, ZDOConnectionHashData> s_connectionsHashData = new Dictionary<ZDOID, ZDOConnectionHashData>();

	private static readonly Dictionary<ZDOID, ZDOConnection> s_connections = new Dictionary<ZDOID, ZDOConnection>();

	private static readonly Dictionary<ZDOID, ushort> s_owner = new Dictionary<ZDOID, ushort>();

	private static Dictionary<ZDOID, BinarySearchDictionary<int, float>> s_saveFloats = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, Vector3>> s_saveVec3s = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, Quaternion>> s_saveQuats = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, int>> s_saveInts = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, long>> s_saveLongs = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, string>> s_saveStrings = null;

	private static Dictionary<ZDOID, BinarySearchDictionary<int, byte[]>> s_saveByteArrays = null;

	private static Dictionary<ZDOID, ZDOConnectionHashData> s_saveConnections = null;

	public static void Init()
	{
		Reset();
		for (int i = 0; i < 256; i++)
		{
			ZDOHelper.s_stripOldData.Add(("room" + i + "_seed").GetStableHashCode());
		}
	}

	public static void Reset()
	{
		s_floats.Clear();
		s_vec3.Clear();
		s_quats.Clear();
		s_ints.Clear();
		s_longs.Clear();
		s_strings.Clear();
		s_byteArrays.Clear();
		s_connections.Clear();
		s_owner.Clear();
		s_tempTimeCreated.Clear();
	}

	public static void Reserve(ZDOID zid, Type type, int size)
	{
		switch (type)
		{
		case Type.Float:
			s_floats.InitAndReserve(zid, size);
			break;
		case Type.Vec3:
			s_vec3.InitAndReserve(zid, size);
			break;
		case Type.Quat:
			s_quats.InitAndReserve(zid, size);
			break;
		case Type.Int:
			s_ints.InitAndReserve(zid, size);
			break;
		case Type.Long:
			s_longs.InitAndReserve(zid, size);
			break;
		case Type.String:
			s_strings.InitAndReserve(zid, size);
			break;
		case Type.ByteArray:
			s_byteArrays.InitAndReserve(zid, size);
			break;
		}
	}

	public static void Add(ZDOID zid, int hash, float value)
	{
		s_floats[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, string value)
	{
		s_strings[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, Vector3 value)
	{
		s_vec3[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, Quaternion value)
	{
		s_quats[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, int value)
	{
		s_ints[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, long value)
	{
		s_longs[zid][hash] = value;
	}

	public static void Add(ZDOID zid, int hash, byte[] value)
	{
		s_byteArrays[zid][hash] = value;
	}

	public static bool Set(ZDOID zid, int hash, float value)
	{
		return s_floats.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, string value)
	{
		return s_strings.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, Vector3 value)
	{
		return s_vec3.InitAndSet(zid, hash, value);
	}

	public static bool Update(ZDOID zid, int hash, Vector3 value)
	{
		return s_vec3.Update(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, Quaternion value)
	{
		return s_quats.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, int value)
	{
		return s_ints.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, long value)
	{
		return s_longs.InitAndSet(zid, hash, value);
	}

	public static bool Set(ZDOID zid, int hash, byte[] value)
	{
		return s_byteArrays.InitAndSet(zid, hash, value);
	}

	public static bool SetConnection(ZDOID zid, ConnectionType connectionType, ZDOID target)
	{
		ZDOConnection zDOConnection = new ZDOConnection(connectionType, target);
		if (s_connections.TryGetValue(zid, out var value) && value.m_type == zDOConnection.m_type && value.m_target == zDOConnection.m_target)
		{
			return false;
		}
		s_connections[zid] = zDOConnection;
		return true;
	}

	public static bool UpdateConnection(ZDOID zid, ConnectionType connectionType, ZDOID target)
	{
		ZDOConnection zDOConnection = new ZDOConnection(connectionType, target);
		if (s_connections.TryGetValue(zid, out var value))
		{
			if (value.m_type == zDOConnection.m_type && value.m_target == zDOConnection.m_target)
			{
				return false;
			}
			s_connections[zid] = zDOConnection;
			return true;
		}
		return false;
	}

	public static void SetConnectionData(ZDOID zid, ConnectionType connectionType, int hash)
	{
		ZDOConnectionHashData value = new ZDOConnectionHashData(connectionType, hash);
		s_connectionsHashData[zid] = value;
	}

	public static void SetOwner(ZDOID zid, ushort ownerKey)
	{
		if (!s_owner.ContainsKey(zid))
		{
			s_owner.Add(zid, ownerKey);
		}
		else if (ownerKey != 0)
		{
			s_owner[zid] = ownerKey;
		}
		else
		{
			s_owner.Remove(zid);
		}
	}

	public static long GetOwner(ZDOID zid)
	{
		if (!s_owner.ContainsKey(zid))
		{
			return 0L;
		}
		return ZDOID.GetUserID(s_owner[zid]);
	}

	public static bool GetFloat(ZDOID zid, int hash, out float value)
	{
		return s_floats.GetValue(zid, hash, out value);
	}

	public static bool GetVec3(ZDOID zid, int hash, out Vector3 value)
	{
		return s_vec3.GetValue(zid, hash, out value);
	}

	public static bool GetQuaternion(ZDOID zid, int hash, out Quaternion value)
	{
		return s_quats.GetValue(zid, hash, out value);
	}

	public static bool GetInt(ZDOID zid, int hash, out int value)
	{
		return s_ints.GetValue(zid, hash, out value);
	}

	public static bool GetLong(ZDOID zid, int hash, out long value)
	{
		return s_longs.GetValue(zid, hash, out value);
	}

	public static bool GetString(ZDOID zid, int hash, out string value)
	{
		return s_strings.GetValue(zid, hash, out value);
	}

	public static bool GetByteArray(ZDOID zid, int hash, out byte[] value)
	{
		return s_byteArrays.GetValue(zid, hash, out value);
	}

	public static bool GetBool(ZDOID zid, int hash, out bool value)
	{
		if (s_ints.GetValue(zid, hash, out var value2))
		{
			value = value2 != 0;
			return true;
		}
		value = false;
		return false;
	}

	public static float GetFloat(ZDOID zid, int hash, float defaultValue = 0f)
	{
		return s_floats.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static Vector3 GetVec3(ZDOID zid, int hash, Vector3 defaultValue)
	{
		return s_vec3.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static Quaternion GetQuaternion(ZDOID zid, int hash, Quaternion defaultValue)
	{
		return s_quats.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static int GetInt(ZDOID zid, int hash, int defaultValue = 0)
	{
		return s_ints.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static long GetLong(ZDOID zid, int hash, long defaultValue = 0L)
	{
		return s_longs.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static string GetString(ZDOID zid, int hash, string defaultValue = "")
	{
		return s_strings.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static byte[] GetByteArray(ZDOID zid, int hash, byte[] defaultValue = null)
	{
		return s_byteArrays.GetValueOrDefault(zid, hash, defaultValue);
	}

	public static ZDOConnection GetConnection(ZDOID zid)
	{
		return s_connections.GetValueOrDefaultPiktiv(zid, null);
	}

	public static ZDOID GetConnectionZDOID(ZDOID zid, ConnectionType type)
	{
		ZDOConnection valueOrDefaultPiktiv = s_connections.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv != null && valueOrDefaultPiktiv.m_type == type)
		{
			return valueOrDefaultPiktiv.m_target;
		}
		return ZDOID.None;
	}

	public static ConnectionType GetConnectionType(ZDOID zid)
	{
		return s_connections.GetValueOrDefaultPiktiv(zid, null)?.m_type ?? ConnectionType.None;
	}

	public static List<KeyValuePair<int, float>> GetFloats(ZDOID zid)
	{
		return s_floats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Vector3>> GetVec3s(ZDOID zid)
	{
		return s_vec3.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Quaternion>> GetQuaternions(ZDOID zid)
	{
		return s_quats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, int>> GetInts(ZDOID zid)
	{
		return s_ints.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, long>> GetLongs(ZDOID zid)
	{
		return s_longs.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, string>> GetStrings(ZDOID zid)
	{
		return s_strings.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, byte[]>> GetByteArrays(ZDOID zid)
	{
		return s_byteArrays.GetValuesOrEmpty(zid);
	}

	public static bool RemoveFloat(ZDOID zid, int hash)
	{
		return s_floats.Remove(zid, hash);
	}

	public static bool RemoveInt(ZDOID zid, int hash)
	{
		return s_ints.Remove(zid, hash);
	}

	public static bool RemoveLong(ZDOID zid, int hash)
	{
		return s_longs.Remove(zid, hash);
	}

	public static bool RemoveVec3(ZDOID zid, int hash)
	{
		return s_vec3.Remove(zid, hash);
	}

	public static bool RemoveQuaternion(ZDOID zid, int hash)
	{
		return s_quats.Remove(zid, hash);
	}

	public static void RemoveIfEmpty(ZDOID id)
	{
		RemoveIfEmpty(id, Type.Float);
		RemoveIfEmpty(id, Type.Vec3);
		RemoveIfEmpty(id, Type.Quat);
		RemoveIfEmpty(id, Type.Int);
		RemoveIfEmpty(id, Type.Long);
		RemoveIfEmpty(id, Type.String);
		RemoveIfEmpty(id, Type.ByteArray);
	}

	public static void RemoveIfEmpty(ZDOID id, Type type)
	{
		switch (type)
		{
		case Type.Float:
			if (s_floats.ContainsKey(id) && s_floats[id].Count == 0)
			{
				ReleaseFloats(id);
			}
			break;
		case Type.Vec3:
			if (s_vec3.ContainsKey(id) && s_vec3[id].Count == 0)
			{
				ReleaseVec3(id);
			}
			break;
		case Type.Quat:
			if (s_quats.ContainsKey(id) && s_quats[id].Count == 0)
			{
				ReleaseQuats(id);
			}
			break;
		case Type.Int:
			if (s_ints.ContainsKey(id) && s_ints[id].Count == 0)
			{
				ReleaseInts(id);
			}
			break;
		case Type.Long:
			if (s_longs.ContainsKey(id) && s_longs[id].Count == 0)
			{
				ReleaseLongs(id);
			}
			break;
		case Type.String:
			if (s_strings.ContainsKey(id) && s_strings[id].Count == 0)
			{
				ReleaseStrings(id);
			}
			break;
		case Type.ByteArray:
			if (s_byteArrays.ContainsKey(id) && s_byteArrays[id].Count == 0)
			{
				ReleaseByteArrays(id);
			}
			break;
		}
	}

	public static void Release(ZDO zdo, ZDOID zid)
	{
		ReleaseFloats(zid);
		ReleaseVec3(zid);
		ReleaseQuats(zid);
		ReleaseInts(zid);
		ReleaseLongs(zid);
		ReleaseStrings(zid);
		ReleaseByteArrays(zid);
		ReleaseOwner(zid);
		ReleaseConnection(zid);
	}

	private static void ReleaseFloats(ZDOID zid)
	{
		s_floats.Release(zid);
	}

	private static void ReleaseVec3(ZDOID zid)
	{
		s_vec3.Release(zid);
	}

	private static void ReleaseQuats(ZDOID zid)
	{
		s_quats.Release(zid);
	}

	private static void ReleaseInts(ZDOID zid)
	{
		s_ints.Release(zid);
	}

	private static void ReleaseLongs(ZDOID zid)
	{
		s_longs.Release(zid);
	}

	private static void ReleaseStrings(ZDOID zid)
	{
		s_strings.Release(zid);
	}

	private static void ReleaseByteArrays(ZDOID zid)
	{
		s_byteArrays.Release(zid);
	}

	public static void ReleaseOwner(ZDOID zid)
	{
		s_owner.Remove(zid);
	}

	private static void ReleaseConnection(ZDOID zid)
	{
		s_connections.Remove(zid);
	}

	public static void SetTimeCreated(ZDOID zid, long timeCreated)
	{
		s_tempTimeCreated.Add(zid, timeCreated);
	}

	public static long GetTimeCreated(ZDOID zid)
	{
		if (s_tempTimeCreated.TryGetValue(zid, out var value))
		{
			return value;
		}
		return 0L;
	}

	public static void ClearTimeCreated()
	{
		s_tempTimeCreated.Clear();
	}

	public static bool HasTimeCreated()
	{
		return s_tempTimeCreated.Count != 0;
	}

	public static List<ZDOID> GetAllZDOIDsWithHash(Type type, int hash)
	{
		switch (type)
		{
		case Type.Long:
			return s_longs.GetAllZDOIDsWithHash(hash);
		case Type.Int:
			return s_ints.GetAllZDOIDsWithHash(hash);
		default:
			Debug.LogError("This type isn't supported, yet.");
			return Array.Empty<ZDOID>().ToList();
		}
	}

	public static List<ZDOID> GetAllConnectionZDOIDs()
	{
		return s_connections.Keys.ToList();
	}

	public static List<ZDOID> GetAllConnectionZDOIDs(ConnectionType connectionType)
	{
		List<ZDOID> list = new List<ZDOID>();
		foreach (KeyValuePair<ZDOID, ZDOConnectionHashData> s_connectionsHashDatum in s_connectionsHashData)
		{
			if (s_connectionsHashDatum.Value.m_type == connectionType)
			{
				list.Add(s_connectionsHashDatum.Key);
			}
		}
		return list;
	}

	public static ZDOConnectionHashData GetConnectionHashData(ZDOID zid, ConnectionType type)
	{
		ZDOConnectionHashData valueOrDefaultPiktiv = s_connectionsHashData.GetValueOrDefaultPiktiv(zid, null);
		if (valueOrDefaultPiktiv != null && valueOrDefaultPiktiv.m_type == type)
		{
			return valueOrDefaultPiktiv;
		}
		return null;
	}

	private static int GetUniqueHash(string name)
	{
		int num = ZDOMan.GetSessionID().GetHashCode() + s_uniqueHashes;
		int num2 = 0;
		int num3;
		do
		{
			num2++;
			num3 = num ^ (name + "_" + num2).GetHashCode();
		}
		while (s_usedHashes.Contains(num3));
		s_usedHashes.Add(num3);
		s_uniqueHashes++;
		return num3;
	}

	private static void RegenerateConnectionHashData()
	{
		s_usedHashes.Clear();
		s_connectionsHashData.Clear();
		foreach (KeyValuePair<ZDOID, ZDOConnection> s_connection in s_connections)
		{
			ConnectionType type = s_connection.Value.m_type;
			if (type != 0 && (!(s_connection.Key == ZDOID.None) || type == ConnectionType.Spawned) && ZDOMan.instance.GetZDO(s_connection.Key) != null && (ZDOMan.instance.GetZDO(s_connection.Value.m_target) != null || type == ConnectionType.Spawned))
			{
				int uniqueHash = GetUniqueHash(type.ToStringFast());
				s_connectionsHashData[s_connection.Key] = new ZDOConnectionHashData(type, uniqueHash);
				if (s_connection.Value.m_target != ZDOID.None)
				{
					s_connectionsHashData[s_connection.Value.m_target] = new ZDOConnectionHashData(type | ConnectionType.Target, uniqueHash);
				}
			}
		}
	}

	public static void PrepareSave()
	{
		RegenerateConnectionHashData();
		s_saveFloats = s_floats.Clone();
		s_saveVec3s = s_vec3.Clone();
		s_saveQuats = s_quats.Clone();
		s_saveInts = s_ints.Clone();
		s_saveLongs = s_longs.Clone();
		s_saveStrings = s_strings.Clone();
		s_saveByteArrays = s_byteArrays.Clone();
		s_saveConnections = s_connectionsHashData.Clone();
	}

	public static void ClearSave()
	{
		s_saveFloats = null;
		s_saveVec3s = null;
		s_saveQuats = null;
		s_saveInts = null;
		s_saveLongs = null;
		s_saveStrings = null;
		s_saveByteArrays = null;
		s_saveConnections = null;
	}

	public static List<KeyValuePair<int, float>> GetSaveFloats(ZDOID zid)
	{
		return s_saveFloats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Vector3>> GetSaveVec3s(ZDOID zid)
	{
		return s_saveVec3s.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, Quaternion>> GetSaveQuaternions(ZDOID zid)
	{
		return s_saveQuats.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, int>> GetSaveInts(ZDOID zid)
	{
		return s_saveInts.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, long>> GetSaveLongs(ZDOID zid)
	{
		return s_saveLongs.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, string>> GetSaveStrings(ZDOID zid)
	{
		return s_saveStrings.GetValuesOrEmpty(zid);
	}

	public static List<KeyValuePair<int, byte[]>> GetSaveByteArrays(ZDOID zid)
	{
		return s_saveByteArrays.GetValuesOrEmpty(zid);
	}

	public static ZDOConnectionHashData GetSaveConnections(ZDOID zid)
	{
		return s_saveConnections.GetValueOrDefaultPiktiv(zid, null);
	}
}
