using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Pathfinding : MonoBehaviour
{
	private class NavMeshTile
	{
		public Vector3Int m_tile;

		public Vector3 m_center;

		public float m_pokeTime = -1000f;

		public float m_buildTime = -1000f;

		public NavMeshData m_data;

		public NavMeshDataInstance m_instance;

		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links1 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();

		public List<KeyValuePair<Vector3, NavMeshLinkInstance>> m_links2 = new List<KeyValuePair<Vector3, NavMeshLinkInstance>>();
	}

	public enum AgentType
	{
		Humanoid = 1,
		TrollSize,
		HugeSize,
		HorseSize,
		HumanoidNoSwim,
		HumanoidAvoidWater,
		Fish,
		HumanoidBig,
		BigFish,
		GoblinBruteSize,
		HumanoidBigNoSwim,
		Abomination,
		SeekerQueen
	}

	public enum AreaType
	{
		Default,
		NotWalkable,
		Jump,
		Water
	}

	private class AgentSettings
	{
		public AgentType m_agentType;

		public NavMeshBuildSettings m_build;

		public bool m_canWalk = true;

		public bool m_avoidWater;

		public bool m_canSwim = true;

		public float m_swimDepth;

		public int m_areaMask = -1;

		public AgentSettings(AgentType type)
		{
			m_agentType = type;
			m_build = NavMesh.CreateSettings();
		}
	}

	private List<Vector3> tempPath = new List<Vector3>();

	private List<Vector3> optPath = new List<Vector3>();

	private List<Vector3> tempStitchPoints = new List<Vector3>();

	private RaycastHit[] tempHitArray = new RaycastHit[255];

	private static Pathfinding m_instance;

	public LayerMask m_layers;

	public LayerMask m_waterLayers;

	private Dictionary<Vector3Int, NavMeshTile> m_tiles = new Dictionary<Vector3Int, NavMeshTile>();

	public float m_tileSize = 32f;

	public float m_defaultCost = 1f;

	public float m_waterCost = 4f;

	public float m_linkCost = 10f;

	public float m_linkWidth = 1f;

	public float m_updateInterval = 5f;

	public float m_tileTimeout = 30f;

	private const float m_tileHeight = 6000f;

	private const float m_tileY = 2500f;

	private float m_updatePathfindingTimer;

	private Queue<Vector3Int> m_queuedAreas = new Queue<Vector3Int>();

	private Queue<NavMeshLinkInstance> m_linkRemoveQueue = new Queue<NavMeshLinkInstance>();

	private Queue<NavMeshDataInstance> m_tileRemoveQueue = new Queue<NavMeshDataInstance>();

	private Vector3Int m_cachedTileID = new Vector3Int(-9999999, -9999999, -9999999);

	private NavMeshTile m_cachedTile;

	private List<AgentSettings> m_agentSettings = new List<AgentSettings>();

	private AsyncOperation m_buildOperation;

	private NavMeshTile m_buildTile;

	private List<KeyValuePair<NavMeshTile, NavMeshTile>> m_edgeBuildQueue = new List<KeyValuePair<NavMeshTile, NavMeshTile>>();

	private NavMeshPath m_path;

	public static Pathfinding instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		SetupAgents();
		m_path = new NavMeshPath();
	}

	private void ClearAgentSettings()
	{
		List<NavMeshBuildSettings> list = new List<NavMeshBuildSettings>();
		for (int i = 0; i < NavMesh.GetSettingsCount(); i++)
		{
			list.Add(NavMesh.GetSettingsByIndex(i));
		}
		foreach (NavMeshBuildSettings item in list)
		{
			if (item.agentTypeID != 0)
			{
				NavMesh.RemoveSettings(item.agentTypeID);
			}
		}
	}

	private void OnDestroy()
	{
		foreach (NavMeshTile value in m_tiles.Values)
		{
			ClearLinks(value);
			if ((bool)value.m_data)
			{
				NavMesh.RemoveNavMeshData(value.m_instance);
			}
		}
		m_tiles.Clear();
		DestroyAllLinks();
	}

	private AgentSettings AddAgent(AgentType type, AgentSettings copy = null)
	{
		while ((int)(type + 1) > m_agentSettings.Count)
		{
			m_agentSettings.Add(null);
		}
		AgentSettings agentSettings = new AgentSettings(type);
		if (copy != null)
		{
			agentSettings.m_build.agentHeight = copy.m_build.agentHeight;
			agentSettings.m_build.agentClimb = copy.m_build.agentClimb;
			agentSettings.m_build.agentRadius = copy.m_build.agentRadius;
			agentSettings.m_build.agentSlope = copy.m_build.agentSlope;
		}
		m_agentSettings[(int)type] = agentSettings;
		return agentSettings;
	}

	private void SetupAgents()
	{
		ClearAgentSettings();
		AgentSettings agentSettings = AddAgent(AgentType.Humanoid);
		agentSettings.m_build.agentHeight = 1.8f;
		agentSettings.m_build.agentClimb = 0.3f;
		agentSettings.m_build.agentRadius = 0.4f;
		agentSettings.m_build.agentSlope = 85f;
		AddAgent(AgentType.HumanoidNoSwim, agentSettings).m_canSwim = false;
		AgentSettings agentSettings2 = AddAgent(AgentType.HumanoidBig, agentSettings);
		agentSettings2.m_build.agentHeight = 2.5f;
		agentSettings2.m_build.agentClimb = 0.3f;
		agentSettings2.m_build.agentRadius = 0.5f;
		agentSettings2.m_build.agentSlope = 85f;
		AgentSettings agentSettings3 = AddAgent(AgentType.HumanoidBigNoSwim);
		agentSettings3.m_build.agentHeight = 2.5f;
		agentSettings3.m_build.agentClimb = 0.3f;
		agentSettings3.m_build.agentRadius = 0.5f;
		agentSettings3.m_build.agentSlope = 85f;
		agentSettings3.m_canSwim = false;
		AddAgent(AgentType.HumanoidAvoidWater, agentSettings).m_avoidWater = true;
		AgentSettings agentSettings4 = AddAgent(AgentType.TrollSize);
		agentSettings4.m_build.agentHeight = 7f;
		agentSettings4.m_build.agentClimb = 0.6f;
		agentSettings4.m_build.agentRadius = 1f;
		agentSettings4.m_build.agentSlope = 85f;
		AgentSettings agentSettings5 = AddAgent(AgentType.Abomination);
		agentSettings5.m_build.agentHeight = 5f;
		agentSettings5.m_build.agentClimb = 0.6f;
		agentSettings5.m_build.agentRadius = 1.5f;
		agentSettings5.m_build.agentSlope = 85f;
		AgentSettings agentSettings6 = AddAgent(AgentType.SeekerQueen);
		agentSettings6.m_build.agentHeight = 7f;
		agentSettings6.m_build.agentClimb = 0.6f;
		agentSettings6.m_build.agentRadius = 1.5f;
		agentSettings6.m_build.agentSlope = 85f;
		AgentSettings agentSettings7 = AddAgent(AgentType.GoblinBruteSize);
		agentSettings7.m_build.agentHeight = 3.5f;
		agentSettings7.m_build.agentClimb = 0.3f;
		agentSettings7.m_build.agentRadius = 0.8f;
		agentSettings7.m_build.agentSlope = 85f;
		AgentSettings agentSettings8 = AddAgent(AgentType.HugeSize);
		agentSettings8.m_build.agentHeight = 10f;
		agentSettings8.m_build.agentClimb = 0.6f;
		agentSettings8.m_build.agentRadius = 2f;
		agentSettings8.m_build.agentSlope = 85f;
		AgentSettings agentSettings9 = AddAgent(AgentType.HorseSize);
		agentSettings9.m_build.agentHeight = 2.5f;
		agentSettings9.m_build.agentClimb = 0.3f;
		agentSettings9.m_build.agentRadius = 0.8f;
		agentSettings9.m_build.agentSlope = 85f;
		AgentSettings agentSettings10 = AddAgent(AgentType.Fish);
		agentSettings10.m_build.agentHeight = 0.5f;
		agentSettings10.m_build.agentClimb = 1f;
		agentSettings10.m_build.agentRadius = 0.5f;
		agentSettings10.m_build.agentSlope = 90f;
		agentSettings10.m_canSwim = true;
		agentSettings10.m_canWalk = false;
		agentSettings10.m_swimDepth = 0.4f;
		agentSettings10.m_areaMask = 12;
		AgentSettings agentSettings11 = AddAgent(AgentType.BigFish);
		agentSettings11.m_build.agentHeight = 1.5f;
		agentSettings11.m_build.agentClimb = 1f;
		agentSettings11.m_build.agentRadius = 1f;
		agentSettings11.m_build.agentSlope = 90f;
		agentSettings11.m_canSwim = true;
		agentSettings11.m_canWalk = false;
		agentSettings11.m_swimDepth = 1.5f;
		agentSettings11.m_areaMask = 12;
		NavMesh.SetAreaCost(0, m_defaultCost);
		NavMesh.SetAreaCost(3, m_waterCost);
	}

	private AgentSettings GetSettings(AgentType agentType)
	{
		return m_agentSettings[(int)agentType];
	}

	private int GetAgentID(AgentType agentType)
	{
		return GetSettings(agentType).m_build.agentTypeID;
	}

	private void Update()
	{
		if (!IsBuilding())
		{
			m_updatePathfindingTimer += Time.deltaTime;
			if (m_updatePathfindingTimer > 0.1f)
			{
				m_updatePathfindingTimer = 0f;
				UpdatePathfinding();
			}
			if (!IsBuilding())
			{
				DestroyQueuedNavmeshData();
			}
		}
	}

	private void DestroyAllLinks()
	{
		while (m_linkRemoveQueue.Count > 0 || m_tileRemoveQueue.Count > 0)
		{
			DestroyQueuedNavmeshData();
		}
	}

	private void DestroyQueuedNavmeshData()
	{
		if (m_linkRemoveQueue.Count > 0)
		{
			int num = Mathf.Min(m_linkRemoveQueue.Count, Mathf.Max(25, m_linkRemoveQueue.Count / 40));
			for (int i = 0; i < num; i++)
			{
				NavMesh.RemoveLink(m_linkRemoveQueue.Dequeue());
			}
		}
		else if (m_tileRemoveQueue.Count > 0)
		{
			NavMesh.RemoveNavMeshData(m_tileRemoveQueue.Dequeue());
		}
	}

	private void UpdatePathfinding()
	{
		Buildtiles();
		TimeoutTiles();
	}

	public bool HavePath(Vector3 from, Vector3 to, AgentType agentType)
	{
		return GetPath(from, to, null, agentType, requireFullPath: true, cleanup: false, havePath: true);
	}

	public bool FindValidPoint(out Vector3 point, Vector3 center, float range, AgentType agentType)
	{
		PokePoint(center, agentType);
		AgentSettings settings = GetSettings(agentType);
		NavMeshQueryFilter filter = default(NavMeshQueryFilter);
		filter.agentTypeID = (int)settings.m_agentType;
		filter.areaMask = settings.m_areaMask;
		if (NavMesh.SamplePosition(center, out var hit, range, filter))
		{
			point = hit.position;
			return true;
		}
		point = center;
		return false;
	}

	private bool IsUnderTerrain(Vector3 p)
	{
		if (ZoneSystem.instance.GetGroundHeight(p, out var height) && p.y < height - 1f)
		{
			return true;
		}
		return false;
	}

	public bool GetPath(Vector3 from, Vector3 to, List<Vector3> path, AgentType agentType, bool requireFullPath = false, bool cleanup = true, bool havePath = false)
	{
		path?.Clear();
		PokeArea(from, agentType);
		PokeArea(to, agentType);
		AgentSettings settings = GetSettings(agentType);
		if (!SnapToNavMesh(ref from, extendedSearchArea: true, settings))
		{
			return false;
		}
		if (!SnapToNavMesh(ref to, !havePath, settings))
		{
			return false;
		}
		NavMeshQueryFilter filter = default(NavMeshQueryFilter);
		filter.agentTypeID = settings.m_build.agentTypeID;
		filter.areaMask = settings.m_areaMask;
		if (NavMesh.CalculatePath(from, to, filter, m_path))
		{
			if (m_path.status == NavMeshPathStatus.PathPartial)
			{
				if (IsUnderTerrain(m_path.corners[0]) || IsUnderTerrain(m_path.corners[m_path.corners.Length - 1]))
				{
					return false;
				}
				if (requireFullPath)
				{
					return false;
				}
			}
			if (path != null)
			{
				path.AddRange(m_path.corners);
				if (cleanup)
				{
					CleanPath(path, settings);
				}
			}
			return true;
		}
		return false;
	}

	private void CleanPath(List<Vector3> basePath, AgentSettings settings)
	{
		if (basePath.Count <= 2)
		{
			return;
		}
		NavMeshQueryFilter filter = default(NavMeshQueryFilter);
		filter.agentTypeID = settings.m_build.agentTypeID;
		filter.areaMask = settings.m_areaMask;
		int num = 0;
		optPath.Clear();
		optPath.Add(basePath[num]);
		do
		{
			num = FindNextNode(basePath, filter, num);
			optPath.Add(basePath[num]);
		}
		while (num < basePath.Count - 1);
		tempPath.Clear();
		tempPath.Add(optPath[0]);
		for (int i = 1; i < optPath.Count - 1; i++)
		{
			Vector3 vector = optPath[i - 1];
			Vector3 vector2 = optPath[i];
			Vector3 vector3 = optPath[i + 1];
			Vector3 normalized = (vector3 - vector2).normalized;
			Vector3 normalized2 = (vector2 - vector).normalized;
			Vector3 vector4 = vector2 - (normalized + normalized2).normalized * Vector3.Distance(vector2, vector) * 0.33f;
			vector4.y = (vector2.y + vector.y) * 0.5f;
			Vector3 normalized3 = (vector4 - vector2).normalized;
			if (!NavMesh.Raycast(vector2 + normalized3 * 0.1f, vector4, out var hit, filter) && !NavMesh.Raycast(vector4, vector, out hit, filter))
			{
				tempPath.Add(vector4);
			}
			tempPath.Add(vector2);
			Vector3 vector5 = vector2 + (normalized + normalized2).normalized * Vector3.Distance(vector2, vector3) * 0.33f;
			vector5.y = (vector2.y + vector3.y) * 0.5f;
			Vector3 normalized4 = (vector5 - vector2).normalized;
			if (!NavMesh.Raycast(vector2 + normalized4 * 0.1f, vector5, out hit, filter) && !NavMesh.Raycast(vector5, vector3, out hit, filter))
			{
				tempPath.Add(vector5);
			}
		}
		tempPath.Add(optPath[optPath.Count - 1]);
		basePath.Clear();
		basePath.AddRange(tempPath);
	}

	private int FindNextNode(List<Vector3> path, NavMeshQueryFilter filter, int start)
	{
		for (int i = start + 2; i < path.Count; i++)
		{
			if (NavMesh.Raycast(path[start], path[i], out var _, filter))
			{
				return i - 1;
			}
		}
		return path.Count - 1;
	}

	private bool SnapToNavMesh(ref Vector3 point, bool extendedSearchArea, AgentSettings settings)
	{
		if ((bool)ZoneSystem.instance)
		{
			if (ZoneSystem.instance.GetGroundHeight(point, out var height) && point.y < height)
			{
				point.y = height;
			}
			if (settings.m_canSwim)
			{
				point.y = Mathf.Max(30f - settings.m_swimDepth, point.y);
			}
		}
		NavMeshQueryFilter filter = default(NavMeshQueryFilter);
		filter.agentTypeID = settings.m_build.agentTypeID;
		filter.areaMask = settings.m_areaMask;
		NavMeshHit hit;
		if (extendedSearchArea)
		{
			if (NavMesh.SamplePosition(point, out hit, 1.5f, filter))
			{
				point = hit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out hit, 3f, filter))
			{
				point = hit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out hit, 6f, filter))
			{
				point = hit.position;
				return true;
			}
			if (NavMesh.SamplePosition(point, out hit, 12f, filter))
			{
				point = hit.position;
				return true;
			}
		}
		else if (NavMesh.SamplePosition(point, out hit, 1f, filter))
		{
			point = hit.position;
			return true;
		}
		return false;
	}

	private void TimeoutTiles()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		foreach (KeyValuePair<Vector3Int, NavMeshTile> tile in m_tiles)
		{
			if (realtimeSinceStartup - tile.Value.m_pokeTime > m_tileTimeout)
			{
				ClearLinks(tile.Value);
				if (tile.Value.m_instance.valid)
				{
					m_tileRemoveQueue.Enqueue(tile.Value.m_instance);
				}
				m_tiles.Remove(tile.Key);
				break;
			}
		}
	}

	private void PokeArea(Vector3 point, AgentType agentType)
	{
		Vector3Int tile = GetTile(point, agentType);
		PokeTile(tile);
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (j != 0 || i != 0)
				{
					Vector3Int tileID = new Vector3Int(tile.x + j, tile.y + i, tile.z);
					PokeTile(tileID);
				}
			}
		}
	}

	private void PokePoint(Vector3 point, AgentType agentType)
	{
		Vector3Int tile = GetTile(point, agentType);
		PokeTile(tile);
	}

	private void PokeTile(Vector3Int tileID)
	{
		GetNavTile(tileID).m_pokeTime = Time.realtimeSinceStartup;
	}

	private void Buildtiles()
	{
		if (UpdateAsyncBuild())
		{
			return;
		}
		NavMeshTile navMeshTile = null;
		float num = 0f;
		foreach (KeyValuePair<Vector3Int, NavMeshTile> tile in m_tiles)
		{
			float num2 = tile.Value.m_pokeTime - tile.Value.m_buildTime;
			if (num2 > m_updateInterval && (navMeshTile == null || num2 > num))
			{
				navMeshTile = tile.Value;
				num = num2;
			}
		}
		if (navMeshTile != null)
		{
			BuildTile(navMeshTile);
			navMeshTile.m_buildTime = Time.realtimeSinceStartup;
		}
	}

	private void BuildTile(NavMeshTile tile)
	{
		_ = DateTime.Now;
		List<NavMeshBuildSource> list = new List<NavMeshBuildSource>();
		List<NavMeshBuildMarkup> markups = new List<NavMeshBuildMarkup>();
		AgentType z = (AgentType)tile.m_tile.z;
		AgentSettings settings = GetSettings(z);
		Bounds includedWorldBounds = new Bounds(tile.m_center, new Vector3(m_tileSize, 6000f, m_tileSize));
		Bounds localBounds = new Bounds(Vector3.zero, new Vector3(m_tileSize, 6000f, m_tileSize));
		int defaultArea = ((!settings.m_canWalk) ? 1 : 0);
		NavMeshBuilder.CollectSources(includedWorldBounds, m_layers.value, NavMeshCollectGeometry.PhysicsColliders, defaultArea, markups, list);
		if (settings.m_avoidWater)
		{
			List<NavMeshBuildSource> list2 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(includedWorldBounds, m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 1, markups, list2);
			foreach (NavMeshBuildSource item in list2)
			{
				NavMeshBuildSource current = item;
				current.transform *= Matrix4x4.Translate(Vector3.down * 0.2f);
				list.Add(current);
			}
		}
		else if (settings.m_canSwim)
		{
			List<NavMeshBuildSource> list3 = new List<NavMeshBuildSource>();
			NavMeshBuilder.CollectSources(includedWorldBounds, m_waterLayers.value, NavMeshCollectGeometry.PhysicsColliders, 3, markups, list3);
			if (settings.m_swimDepth != 0f)
			{
				foreach (NavMeshBuildSource item2 in list3)
				{
					NavMeshBuildSource current2 = item2;
					current2.transform *= Matrix4x4.Translate(Vector3.down * settings.m_swimDepth);
					list.Add(current2);
				}
			}
			else
			{
				list.AddRange(list3);
			}
		}
		if (tile.m_data == null)
		{
			tile.m_data = new NavMeshData();
			tile.m_data.position = tile.m_center;
		}
		m_buildOperation = NavMeshBuilder.UpdateNavMeshDataAsync(tile.m_data, settings.m_build, list, localBounds);
		m_buildTile = tile;
	}

	private bool IsBuilding()
	{
		if (m_buildOperation != null)
		{
			return !m_buildOperation.isDone;
		}
		return false;
	}

	private bool UpdateAsyncBuild()
	{
		if (m_buildOperation == null)
		{
			return false;
		}
		if (!m_buildOperation.isDone)
		{
			return true;
		}
		if (!m_buildTile.m_instance.valid)
		{
			m_buildTile.m_instance = NavMesh.AddNavMeshData(m_buildTile.m_data);
		}
		RebuildLinks(m_buildTile);
		m_buildOperation = null;
		m_buildTile = null;
		return true;
	}

	private void ClearLinks(NavMeshTile tile)
	{
		ClearLinks(tile.m_links1);
		ClearLinks(tile.m_links2);
	}

	private void ClearLinks(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		foreach (KeyValuePair<Vector3, NavMeshLinkInstance> link in links)
		{
			m_linkRemoveQueue.Enqueue(link.Value);
		}
		links.Clear();
	}

	private void RebuildLinks(NavMeshTile tile)
	{
		AgentType z = (AgentType)tile.m_tile.z;
		AgentSettings settings = GetSettings(z);
		float num = m_tileSize / 2f;
		ConnectAlongEdge(tile.m_links1, tile.m_center + new Vector3(num, 0f, num), tile.m_center + new Vector3(num, 0f, 0f - num), m_linkWidth, settings);
		ConnectAlongEdge(tile.m_links2, tile.m_center + new Vector3(0f - num, 0f, num), tile.m_center + new Vector3(num, 0f, num), m_linkWidth, settings);
	}

	private void ConnectAlongEdge(List<KeyValuePair<Vector3, NavMeshLinkInstance>> links, Vector3 p0, Vector3 p1, float step, AgentSettings settings)
	{
		Vector3 normalized = (p1 - p0).normalized;
		Vector3 vector = Vector3.Cross(Vector3.up, normalized);
		float num = Vector3.Distance(p0, p1);
		bool canSwim = settings.m_canSwim;
		tempStitchPoints.Clear();
		for (float num2 = step / 2f; num2 <= num; num2 += step)
		{
			Vector3 p2 = p0 + normalized * num2;
			FindGround(p2, canSwim, tempStitchPoints, settings);
		}
		if (CompareLinks(tempStitchPoints, links))
		{
			return;
		}
		ClearLinks(links);
		foreach (Vector3 tempStitchPoint in tempStitchPoints)
		{
			NavMeshLinkData link = default(NavMeshLinkData);
			link.startPosition = tempStitchPoint - vector * 0.1f;
			link.endPosition = tempStitchPoint + vector * 0.1f;
			link.width = step;
			link.costModifier = m_linkCost;
			link.bidirectional = true;
			link.agentTypeID = settings.m_build.agentTypeID;
			link.area = 2;
			NavMeshLinkInstance value = NavMesh.AddLink(link);
			if (value.valid)
			{
				links.Add(new KeyValuePair<Vector3, NavMeshLinkInstance>(tempStitchPoint, value));
			}
		}
	}

	private bool CompareLinks(List<Vector3> tempStitchPoints, List<KeyValuePair<Vector3, NavMeshLinkInstance>> links)
	{
		if (tempStitchPoints.Count != links.Count)
		{
			return false;
		}
		for (int i = 0; i < tempStitchPoints.Count; i++)
		{
			if (tempStitchPoints[i] != links[i].Key)
			{
				return false;
			}
		}
		return true;
	}

	private bool SnapToNearestGround(Vector3 p, out Vector3 pos, float range)
	{
		if (Physics.Raycast(p + Vector3.up, Vector3.down, out var hitInfo, range + 1f, m_layers.value | m_waterLayers.value))
		{
			pos = hitInfo.point;
			return true;
		}
		if (Physics.Raycast(p + Vector3.up * range, Vector3.down, out hitInfo, range, m_layers.value | m_waterLayers.value))
		{
			pos = hitInfo.point;
			return true;
		}
		pos = p;
		return false;
	}

	private void FindGround(Vector3 p, bool testWater, List<Vector3> hits, AgentSettings settings)
	{
		p.y = 6000f;
		int layerMask = (testWater ? (m_layers.value | m_waterLayers.value) : m_layers.value);
		float agentHeight = settings.m_build.agentHeight;
		float y = p.y;
		int num = Physics.RaycastNonAlloc(p, Vector3.down, tempHitArray, 10000f, layerMask);
		for (int i = 0; i < num; i++)
		{
			Vector3 point = tempHitArray[i].point;
			if (!(Mathf.Abs(point.y - y) < agentHeight))
			{
				y = point.y;
				if (((1 << tempHitArray[i].collider.gameObject.layer) & (int)m_waterLayers) != 0)
				{
					point.y -= settings.m_swimDepth;
				}
				hits.Add(point);
			}
		}
	}

	private NavMeshTile GetNavTile(Vector3 point, AgentType agent)
	{
		Vector3Int tile = GetTile(point, agent);
		return GetNavTile(tile);
	}

	private NavMeshTile GetNavTile(Vector3Int tile)
	{
		if (tile == m_cachedTileID)
		{
			return m_cachedTile;
		}
		if (m_tiles.TryGetValue(tile, out var value))
		{
			m_cachedTileID = tile;
			m_cachedTile = value;
			return value;
		}
		value = new NavMeshTile();
		value.m_tile = tile;
		value.m_center = GetTilePos(tile);
		m_tiles.Add(tile, value);
		m_cachedTileID = tile;
		m_cachedTile = value;
		return value;
	}

	private Vector3Int GetTile(Vector3 point, AgentType agent)
	{
		int x = Mathf.FloorToInt((point.x + m_tileSize / 2f) / m_tileSize);
		int y = Mathf.FloorToInt((point.z + m_tileSize / 2f) / m_tileSize);
		return new Vector3Int(x, y, (int)agent);
	}

	public Vector3 GetTilePos(Vector3Int id)
	{
		return new Vector3((float)id.x * m_tileSize, 2500f, (float)id.y * m_tileSize);
	}
}
