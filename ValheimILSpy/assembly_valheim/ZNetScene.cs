using System;
using System.Collections.Generic;
using UnityEngine;

public class ZNetScene : MonoBehaviour
{
	private static ZNetScene s_instance;

	private const int m_maxCreatedPerFrame = 10;

	private const float m_createDestroyFps = 30f;

	public List<GameObject> m_prefabs = new List<GameObject>();

	public List<GameObject> m_nonNetViewPrefabs = new List<GameObject>();

	private readonly Dictionary<int, GameObject> m_namedPrefabs = new Dictionary<int, GameObject>();

	private readonly Dictionary<ZDO, ZNetView> m_instances = new Dictionary<ZDO, ZNetView>();

	private readonly List<ZDO> m_tempCurrentObjects = new List<ZDO>();

	private readonly List<ZDO> m_tempCurrentObjects2 = new List<ZDO>();

	private readonly List<ZDO> m_tempCurrentDistantObjects = new List<ZDO>();

	private readonly List<ZNetView> m_tempRemoved = new List<ZNetView>();

	private float m_createDestroyTimer;

	public static ZNetScene instance => s_instance;

	private void Awake()
	{
		s_instance = this;
		foreach (GameObject prefab in m_prefabs)
		{
			m_namedPrefabs.Add(prefab.name.GetStableHashCode(), prefab);
		}
		foreach (GameObject nonNetViewPrefab in m_nonNetViewPrefabs)
		{
			m_namedPrefabs.Add(nonNetViewPrefab.name.GetStableHashCode(), nonNetViewPrefab);
		}
		ZDOMan zDOMan = ZDOMan.instance;
		zDOMan.m_onZDODestroyed = (Action<ZDO>)Delegate.Combine(zDOMan.m_onZDODestroyed, new Action<ZDO>(OnZDODestroyed));
		ZRoutedRpc.instance.Register<Vector3, Quaternion, int>("SpawnObject", RPC_SpawnObject);
	}

	private void OnDestroy()
	{
		ZLog.Log("Net scene destroyed");
		if (s_instance == this)
		{
			s_instance = null;
		}
	}

	public void Shutdown()
	{
		foreach (KeyValuePair<ZDO, ZNetView> instance in m_instances)
		{
			if ((bool)instance.Value)
			{
				instance.Value.ResetZDO();
				UnityEngine.Object.Destroy(instance.Value.gameObject);
			}
		}
		m_instances.Clear();
		base.enabled = false;
	}

	public void AddInstance(ZDO zdo, ZNetView nview)
	{
		zdo.Created = true;
		m_instances[zdo] = nview;
	}

	private bool IsPrefabZDOValid(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return false;
		}
		return GetPrefab(prefab) != null;
	}

	private GameObject CreateObject(ZDO zdo)
	{
		int prefab = zdo.GetPrefab();
		if (prefab == 0)
		{
			return null;
		}
		GameObject prefab2 = GetPrefab(prefab);
		if (prefab2 == null)
		{
			return null;
		}
		Vector3 position = zdo.GetPosition();
		Quaternion rotation = zdo.GetRotation();
		ZNetView.m_useInitZDO = true;
		ZNetView.m_initZDO = zdo;
		GameObject result = UnityEngine.Object.Instantiate(prefab2, position, rotation);
		if (ZNetView.m_initZDO != null)
		{
			ZDOID uid = zdo.m_uid;
			ZLog.LogWarning("ZDO " + uid.ToString() + " not used when creating object " + prefab2.name);
			ZNetView.m_initZDO = null;
		}
		ZNetView.m_useInitZDO = false;
		return result;
	}

	public void Destroy(GameObject go)
	{
		ZNetView component = go.GetComponent<ZNetView>();
		if ((bool)component && component.GetZDO() != null)
		{
			ZDO zDO = component.GetZDO();
			component.ResetZDO();
			m_instances.Remove(zDO);
			if (zDO.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zDO);
			}
		}
		UnityEngine.Object.Destroy(go);
	}

	public bool HasPrefab(int hash)
	{
		return m_namedPrefabs.ContainsKey(hash);
	}

	public GameObject GetPrefab(int hash)
	{
		if (m_namedPrefabs.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	public GameObject GetPrefab(string name)
	{
		return GetPrefab(name.GetStableHashCode());
	}

	public int GetPrefabHash(GameObject go)
	{
		return go.name.GetStableHashCode();
	}

	public bool IsAreaReady(Vector3 point)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(point);
		if (!ZoneSystem.instance.IsZoneLoaded(zone))
		{
			return false;
		}
		m_tempCurrentObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, 1, 0, m_tempCurrentObjects);
		foreach (ZDO tempCurrentObject in m_tempCurrentObjects)
		{
			if (IsPrefabZDOValid(tempCurrentObject) && !FindInstance(tempCurrentObject))
			{
				return false;
			}
		}
		return true;
	}

	private bool InLoadingScreen()
	{
		if (!(Player.m_localPlayer == null))
		{
			return Player.m_localPlayer.IsTeleporting();
		}
		return true;
	}

	private void CreateObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		int maxCreatedPerFrame = 10;
		if (InLoadingScreen())
		{
			maxCreatedPerFrame = 100;
		}
		int created = 0;
		CreateObjectsSorted(currentNearObjects, maxCreatedPerFrame, ref created);
		CreateDistantObjects(currentDistantObjects, maxCreatedPerFrame, ref created);
	}

	private void CreateObjectsSorted(List<ZDO> currentNearObjects, int maxCreatedPerFrame, ref int created)
	{
		if (!ZoneSystem.instance.IsActiveAreaLoaded())
		{
			return;
		}
		m_tempCurrentObjects2.Clear();
		Vector3 referencePosition = ZNet.instance.GetReferencePosition();
		foreach (ZDO currentNearObject in currentNearObjects)
		{
			if (!currentNearObject.Created)
			{
				currentNearObject.m_tempSortValue = Utils.DistanceSqr(referencePosition, currentNearObject.GetPosition());
				m_tempCurrentObjects2.Add(currentNearObject);
			}
		}
		int num = Mathf.Max(m_tempCurrentObjects2.Count / 100, maxCreatedPerFrame);
		m_tempCurrentObjects2.Sort(ZDOCompare);
		foreach (ZDO item in m_tempCurrentObjects2)
		{
			if (!ZoneSystem.instance.IsZoneReadyForType(item.GetSector(), item.Type))
			{
				continue;
			}
			if (CreateObject(item) != null)
			{
				created++;
				if (created > num)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				item.SetOwner(ZDOMan.GetSessionID());
				ZDOID uid = item.m_uid;
				ZLog.Log("Destroyed invalid predab ZDO:" + uid.ToString());
				ZDOMan.instance.DestroyZDO(item);
			}
		}
	}

	private static int ZDOCompare(ZDO x, ZDO y)
	{
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		return ((int)y.Type).CompareTo((int)x.Type);
	}

	private void CreateDistantObjects(List<ZDO> objects, int maxCreatedPerFrame, ref int created)
	{
		if (created > maxCreatedPerFrame)
		{
			return;
		}
		foreach (ZDO @object in objects)
		{
			if (@object.Created)
			{
				continue;
			}
			if (CreateObject(@object) != null)
			{
				created++;
				if (created > maxCreatedPerFrame)
				{
					break;
				}
			}
			else if (ZNet.instance.IsServer())
			{
				@object.SetOwner(ZDOMan.GetSessionID());
				ZDOID uid = @object.m_uid;
				ZLog.Log("Destroyed invalid predab ZDO:" + uid.ToString() + "  prefab hash:" + @object.GetPrefab());
				ZDOMan.instance.DestroyZDO(@object);
			}
		}
	}

	private void OnZDODestroyed(ZDO zdo)
	{
		if (m_instances.TryGetValue(zdo, out var value))
		{
			value.ResetZDO();
			UnityEngine.Object.Destroy(value.gameObject);
			m_instances.Remove(zdo);
		}
	}

	private void RemoveObjects(List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
	{
		byte b = (byte)((uint)Time.frameCount & 0xFFu);
		foreach (ZDO currentNearObject in currentNearObjects)
		{
			currentNearObject.TempRemoveEarmark = b;
		}
		foreach (ZDO currentDistantObject in currentDistantObjects)
		{
			currentDistantObject.TempRemoveEarmark = b;
		}
		m_tempRemoved.Clear();
		foreach (ZNetView value in m_instances.Values)
		{
			if (value.GetZDO().TempRemoveEarmark != b)
			{
				m_tempRemoved.Add(value);
			}
		}
		for (int i = 0; i < m_tempRemoved.Count; i++)
		{
			ZNetView zNetView = m_tempRemoved[i];
			ZDO zDO = zNetView.GetZDO();
			zNetView.ResetZDO();
			UnityEngine.Object.Destroy(zNetView.gameObject);
			if (!zDO.Persistent && zDO.IsOwner())
			{
				ZDOMan.instance.DestroyZDO(zDO);
			}
			m_instances.Remove(zDO);
		}
	}

	public ZNetView FindInstance(ZDO zdo)
	{
		if (m_instances.TryGetValue(zdo, out var value))
		{
			return value;
		}
		return null;
	}

	public bool HaveInstance(ZDO zdo)
	{
		return m_instances.ContainsKey(zdo);
	}

	public GameObject FindInstance(ZDOID id)
	{
		ZDO zDO = ZDOMan.instance.GetZDO(id);
		if (zDO != null)
		{
			ZNetView zNetView = FindInstance(zDO);
			if ((bool)zNetView)
			{
				return zNetView.gameObject;
			}
		}
		return null;
	}

	private void Update()
	{
		float deltaTime = Time.deltaTime;
		m_createDestroyTimer += deltaTime;
		if (m_createDestroyTimer >= 1f / 30f)
		{
			m_createDestroyTimer = 0f;
			CreateDestroyObjects();
		}
	}

	private void CreateDestroyObjects()
	{
		Vector2i zone = ZoneSystem.instance.GetZone(ZNet.instance.GetReferencePosition());
		m_tempCurrentObjects.Clear();
		m_tempCurrentDistantObjects.Clear();
		ZDOMan.instance.FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, m_tempCurrentObjects, m_tempCurrentDistantObjects);
		CreateObjects(m_tempCurrentObjects, m_tempCurrentDistantObjects);
		RemoveObjects(m_tempCurrentObjects, m_tempCurrentDistantObjects);
	}

	public static bool InActiveArea(Vector2i zone, Vector3 refPoint)
	{
		Vector2i zone2 = ZoneSystem.instance.GetZone(refPoint);
		return InActiveArea(zone, zone2);
	}

	public static bool InActiveArea(Vector2i zone, Vector2i refCenterZone)
	{
		int num = ZoneSystem.instance.m_activeArea - 1;
		if (zone.x >= refCenterZone.x - num && zone.x <= refCenterZone.x + num && zone.y <= refCenterZone.y + num)
		{
			return zone.y >= refCenterZone.y - num;
		}
		return false;
	}

	public bool OutsideActiveArea(Vector3 point)
	{
		return OutsideActiveArea(point, ZNet.instance.GetReferencePosition());
	}

	private bool OutsideActiveArea(Vector3 point, Vector3 refPoint)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPoint);
		Vector2i zone2 = ZoneSystem.instance.GetZone(point);
		if (zone2.x > zone.x - ZoneSystem.instance.m_activeArea && zone2.x < zone.x + ZoneSystem.instance.m_activeArea && zone2.y < zone.y + ZoneSystem.instance.m_activeArea)
		{
			return zone2.y <= zone.y - ZoneSystem.instance.m_activeArea;
		}
		return true;
	}

	public bool HaveInstanceInSector(Vector2i sector)
	{
		foreach (KeyValuePair<ZDO, ZNetView> instance in m_instances)
		{
			if ((bool)instance.Value && !instance.Value.m_distant && ZoneSystem.instance.GetZone(instance.Value.transform.position) == sector)
			{
				return true;
			}
		}
		return false;
	}

	public int NrOfInstances()
	{
		return m_instances.Count;
	}

	public void SpawnObject(Vector3 pos, Quaternion rot, GameObject prefab)
	{
		int prefabHash = GetPrefabHash(prefab);
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "SpawnObject", pos, rot, prefabHash);
	}

	public List<string> GetPrefabNames()
	{
		List<string> list = new List<string>();
		foreach (KeyValuePair<int, GameObject> namedPrefab in m_namedPrefabs)
		{
			list.Add(namedPrefab.Value.name);
		}
		return list;
	}

	private void RPC_SpawnObject(long spawner, Vector3 pos, Quaternion rot, int prefabHash)
	{
		GameObject prefab = GetPrefab(prefabHash);
		if (prefab == null)
		{
			ZLog.Log("Missing prefab " + prefabHash);
		}
		else
		{
			UnityEngine.Object.Instantiate(prefab, pos, rot);
		}
	}
}
