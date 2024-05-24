using System;
using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
	[Serializable]
	public class SpawnData
	{
		public string m_name = "";

		public bool m_enabled = true;

		public bool m_devDisabled;

		public GameObject m_prefab;

		[BitMask(typeof(Heightmap.Biome))]
		public Heightmap.Biome m_biome;

		[BitMask(typeof(Heightmap.BiomeArea))]
		public Heightmap.BiomeArea m_biomeArea = Heightmap.BiomeArea.Everything;

		[Header("Total nr of instances (if near player is set, only instances within the max spawn radius is counted)")]
		public int m_maxSpawned = 1;

		[Header("How often do we spawn")]
		public float m_spawnInterval = 4f;

		[Header("Chanse to spawn each spawn interval")]
		[Range(0f, 100f)]
		public float m_spawnChance = 100f;

		[Header("Minimum distance to another instance")]
		public float m_spawnDistance = 10f;

		[Header("Spawn range ( 0 = use global setting )")]
		public float m_spawnRadiusMin;

		public float m_spawnRadiusMax;

		[Header("Only spawn if this key is set")]
		public string m_requiredGlobalKey = "";

		[Header("Only spawn if this environment is active")]
		public List<string> m_requiredEnvironments = new List<string>();

		[Header("Group spawning")]
		public int m_groupSizeMin = 1;

		public int m_groupSizeMax = 1;

		public float m_groupRadius = 3f;

		[Header("Time of day & Environment")]
		public bool m_spawnAtNight = true;

		public bool m_spawnAtDay = true;

		[Header("Altitude")]
		public float m_minAltitude = -1000f;

		public float m_maxAltitude = 1000f;

		[Header("Terrain tilt")]
		public float m_minTilt;

		public float m_maxTilt = 35f;

		[Header("Areas")]
		public bool m_inForest = true;

		public bool m_outsideForest = true;

		public bool m_inLava;

		public bool m_outsideLava = true;

		public bool m_canSpawnCloseToPlayer;

		public bool m_insidePlayerBase;

		[Header("Ocean depth ")]
		public float m_minOceanDepth;

		public float m_maxOceanDepth;

		[Header("States")]
		public bool m_huntPlayer;

		public float m_groundOffset = 0.5f;

		public float m_groundOffsetRandom;

		[Header("Level")]
		public int m_maxLevel = 1;

		public int m_minLevel = 1;

		public float m_levelUpMinCenterDistance;

		public float m_overrideLevelupChance = -1f;

		[HideInInspector]
		public bool m_foldout;

		public SpawnData Clone()
		{
			SpawnData obj = MemberwiseClone() as SpawnData;
			obj.m_requiredEnvironments = new List<string>(m_requiredEnvironments);
			return obj;
		}
	}

	private static List<SpawnSystem> m_instances = new List<SpawnSystem>();

	private const float m_spawnDistanceMin = 40f;

	private const float m_spawnDistanceMax = 80f;

	private const float m_levelupChance = 10f;

	public List<SpawnSystemList> m_spawnLists = new List<SpawnSystemList>();

	[HideInInspector]
	public List<Heightmap.Biome> m_biomeFolded = new List<Heightmap.Biome>();

	private static List<Player> m_tempNearPlayers = new List<Player>();

	private ZNetView m_nview;

	private Heightmap m_heightmap;

	private void Awake()
	{
		m_instances.Add(this);
		m_nview = GetComponent<ZNetView>();
		m_heightmap = Heightmap.FindHeightmap(base.transform.position);
		InvokeRepeating("UpdateSpawning", 10f, 1f);
	}

	private void OnDestroy()
	{
		m_instances.Remove(this);
	}

	private void UpdateSpawning()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner() || Player.m_localPlayer == null)
		{
			return;
		}
		m_tempNearPlayers.Clear();
		GetPlayersInZone(m_tempNearPlayers);
		if (m_tempNearPlayers.Count == 0)
		{
			return;
		}
		DateTime time = ZNet.instance.GetTime();
		foreach (SpawnSystemList spawnList in m_spawnLists)
		{
			UpdateSpawnList(spawnList.m_spawners, time, eventSpawners: false);
		}
		List<SpawnData> currentSpawners = RandEventSystem.instance.GetCurrentSpawners();
		if (currentSpawners != null)
		{
			UpdateSpawnList(currentSpawners, time, eventSpawners: true);
		}
	}

	private void UpdateSpawnList(List<SpawnData> spawners, DateTime currentTime, bool eventSpawners)
	{
		string text = (eventSpawners ? "e_" : "b_");
		int num = 0;
		foreach (SpawnData spawner in spawners)
		{
			num++;
			if (!spawner.m_enabled || !m_heightmap.HaveBiome(spawner.m_biome))
			{
				continue;
			}
			int stableHashCode = (text + spawner.m_prefab.name + num).GetStableHashCode();
			DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(stableHashCode, 0L));
			TimeSpan timeSpan = currentTime - dateTime;
			int num2 = Mathf.Min((spawner.m_maxSpawned == 0) ? 1 : spawner.m_maxSpawned, (int)(timeSpan.TotalSeconds / (double)spawner.m_spawnInterval));
			if (num2 > 0)
			{
				m_nview.GetZDO().Set(stableHashCode, currentTime.Ticks);
			}
			for (int i = 0; i < num2; i++)
			{
				if (UnityEngine.Random.Range(0f, 100f) > spawner.m_spawnChance)
				{
					continue;
				}
				if ((!string.IsNullOrEmpty(spawner.m_requiredGlobalKey) && !ZoneSystem.instance.GetGlobalKey(spawner.m_requiredGlobalKey)) || (spawner.m_requiredEnvironments.Count > 0 && !EnvMan.instance.IsEnvironment(spawner.m_requiredEnvironments)) || (!spawner.m_spawnAtDay && EnvMan.IsDay()) || (!spawner.m_spawnAtNight && EnvMan.IsNight()))
				{
					break;
				}
				int num3 = 0;
				if (spawner.m_maxSpawned > 0)
				{
					num3 = GetNrOfInstances(spawner.m_prefab, Vector3.zero, 0f, eventSpawners);
					if (num3 >= spawner.m_maxSpawned)
					{
						break;
					}
				}
				if (!FindBaseSpawnPoint(spawner, m_tempNearPlayers, out var spawnCenter, out var _) || (spawner.m_spawnDistance > 0f && HaveInstanceInRange(spawner.m_prefab, spawnCenter, spawner.m_spawnDistance)))
				{
					continue;
				}
				int num4 = Mathf.Min(UnityEngine.Random.Range(spawner.m_groupSizeMin, spawner.m_groupSizeMax + 1), (spawner.m_maxSpawned > 0) ? (spawner.m_maxSpawned - num3) : 100);
				float num5 = ((num4 > 1) ? spawner.m_groupRadius : 0f);
				int num6 = 0;
				for (int j = 0; j < num4 * 2; j++)
				{
					Vector2 insideUnitCircle = UnityEngine.Random.insideUnitCircle;
					Vector3 spawnPoint = spawnCenter + new Vector3(insideUnitCircle.x, 0f, insideUnitCircle.y) * num5;
					if (IsSpawnPointGood(spawner, ref spawnPoint))
					{
						Spawn(spawner, spawnPoint + Vector3.up * (spawner.m_groundOffset + UnityEngine.Random.Range(0f, spawner.m_groundOffsetRandom)), eventSpawners);
						num6++;
						if (num6 >= num4)
						{
							break;
						}
					}
				}
				ZLog.Log("Spawned " + spawner.m_prefab.name + " x " + num6);
			}
		}
	}

	private void Spawn(SpawnData critter, Vector3 spawnPoint, bool eventSpawner)
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(critter.m_prefab, spawnPoint, Quaternion.identity);
		if (Terminal.m_showTests && Terminal.m_testList.ContainsKey("spawns"))
		{
			Terminal.Log($"Spawning {critter.m_prefab.name} at {spawnPoint}");
			Chat.instance.SendPing(spawnPoint);
		}
		BaseAI component = gameObject.GetComponent<BaseAI>();
		if (component != null && critter.m_huntPlayer && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs))
		{
			component.SetHuntPlayer(hunt: true);
		}
		if (critter.m_levelUpMinCenterDistance <= 0f || spawnPoint.magnitude > critter.m_levelUpMinCenterDistance)
		{
			int i = critter.m_minLevel;
			float levelUpChance = GetLevelUpChance(critter);
			for (; i < critter.m_maxLevel; i++)
			{
				if (!(UnityEngine.Random.Range(0f, 100f) <= levelUpChance))
				{
					break;
				}
			}
			if (i > 1)
			{
				gameObject.GetComponent<Character>()?.SetLevel(i);
				if ((object)gameObject.GetComponent<Fish>() != null)
				{
					gameObject.GetComponent<ItemDrop>()?.SetQuality(i);
				}
			}
		}
		if (component is MonsterAI monsterAI)
		{
			if (!critter.m_spawnAtDay)
			{
				monsterAI.SetDespawnInDay(despawn: true);
			}
			if (eventSpawner)
			{
				monsterAI.SetEventCreature(despawn: true);
			}
		}
	}

	private bool IsSpawnPointGood(SpawnData spawn, ref Vector3 spawnPoint)
	{
		ZoneSystem.instance.GetGroundData(ref spawnPoint, out var normal, out var biome, out var biomeArea, out var hmap);
		if ((spawn.m_biome & biome) == 0)
		{
			return false;
		}
		if ((spawn.m_biomeArea & biomeArea) == 0)
		{
			return false;
		}
		if (ZoneSystem.instance.IsBlocked(spawnPoint))
		{
			return false;
		}
		float num = spawnPoint.y - 30f;
		if (num < spawn.m_minAltitude || num > spawn.m_maxAltitude)
		{
			return false;
		}
		float num2 = Mathf.Cos((float)Math.PI / 180f * spawn.m_maxTilt);
		float num3 = Mathf.Cos((float)Math.PI / 180f * spawn.m_minTilt);
		if (normal.y < num2 || normal.y > num3)
		{
			return false;
		}
		float range = ((spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f);
		if (!spawn.m_canSpawnCloseToPlayer && Player.IsPlayerInRange(spawnPoint, range))
		{
			return false;
		}
		if (!spawn.m_insidePlayerBase && (bool)EffectArea.IsPointInsideArea(spawnPoint, EffectArea.Type.PlayerBase))
		{
			return false;
		}
		if (!spawn.m_inForest || !spawn.m_outsideForest)
		{
			bool flag = WorldGenerator.InForest(spawnPoint);
			if (!spawn.m_inForest && flag)
			{
				return false;
			}
			if (!spawn.m_outsideForest && !flag)
			{
				return false;
			}
		}
		if (!spawn.m_inLava || !spawn.m_outsideLava)
		{
			if (!spawn.m_inLava && ZoneSystem.instance.IsLava(spawnPoint, defaultTrue: true))
			{
				return false;
			}
			if (!spawn.m_outsideLava && !ZoneSystem.instance.IsLava(spawnPoint))
			{
				return false;
			}
		}
		if (spawn.m_minOceanDepth != spawn.m_maxOceanDepth && hmap != null)
		{
			float oceanDepth = hmap.GetOceanDepth(spawnPoint);
			if (oceanDepth < spawn.m_minOceanDepth || oceanDepth > spawn.m_maxOceanDepth)
			{
				return false;
			}
		}
		return true;
	}

	private bool FindBaseSpawnPoint(SpawnData spawn, List<Player> allPlayers, out Vector3 spawnCenter, out Player targetPlayer)
	{
		float minInclusive = ((spawn.m_spawnRadiusMin > 0f) ? spawn.m_spawnRadiusMin : 40f);
		float maxInclusive = ((spawn.m_spawnRadiusMax > 0f) ? spawn.m_spawnRadiusMax : 80f);
		for (int i = 0; i < 20; i++)
		{
			Player player = allPlayers[UnityEngine.Random.Range(0, allPlayers.Count)];
			Vector3 vector = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward;
			Vector3 spawnPoint = player.transform.position + vector * UnityEngine.Random.Range(minInclusive, maxInclusive);
			if (IsSpawnPointGood(spawn, ref spawnPoint))
			{
				spawnCenter = spawnPoint;
				targetPlayer = player;
				return true;
			}
		}
		spawnCenter = Vector3.zero;
		targetPlayer = null;
		return false;
	}

	private int GetNrOfInstances(string prefabName)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		int num = 0;
		foreach (Character item in allCharacters)
		{
			if (item.gameObject.name.CustomStartsWith(prefabName) && InsideZone(item.transform.position))
			{
				num++;
			}
		}
		return num;
	}

	private void GetPlayersInZone(List<Player> players)
	{
		foreach (Player allPlayer in Player.GetAllPlayers())
		{
			if (InsideZone(allPlayer.transform.position))
			{
				players.Add(allPlayer);
			}
		}
	}

	private void GetPlayersNearZone(List<Player> players, float marginDistance)
	{
		foreach (Player allPlayer in Player.GetAllPlayers())
		{
			if (InsideZone(allPlayer.transform.position, marginDistance))
			{
				players.Add(allPlayer);
			}
		}
	}

	private bool IsPlayerTooClose(List<Player> players, Vector3 point, float minDistance)
	{
		foreach (Player player in players)
		{
			if (Vector3.Distance(player.transform.position, point) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	private bool InPlayerRange(List<Player> players, Vector3 point, float minDistance, float maxDistance)
	{
		bool result = false;
		foreach (Player player in players)
		{
			float num = Utils.DistanceXZ(player.transform.position, point);
			if (num < minDistance)
			{
				return false;
			}
			if (num < maxDistance)
			{
				result = true;
			}
		}
		return result;
	}

	private static bool HaveInstanceInRange(GameObject prefab, Vector3 centerPoint, float minDistance)
	{
		string b = prefab.name;
		if (prefab.GetComponent<BaseAI>() != null)
		{
			foreach (BaseAI baseAIInstance in BaseAI.BaseAIInstances)
			{
				if (baseAIInstance.gameObject.name.CustomStartsWith(b) && Utils.DistanceXZ(baseAIInstance.transform.position, centerPoint) < minDistance)
				{
					return true;
				}
			}
			return false;
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		foreach (GameObject gameObject in array)
		{
			if (gameObject.gameObject.name.CustomStartsWith(b) && Utils.DistanceXZ(gameObject.transform.position, centerPoint) < minDistance)
			{
				return true;
			}
		}
		return false;
	}

	public static int GetNrOfInstances(GameObject prefab)
	{
		return GetNrOfInstances(prefab, Vector3.zero, 0f);
	}

	public static int GetNrOfInstances(GameObject prefab, Vector3 center, float maxRange, bool eventCreaturesOnly = false, bool procreationOnly = false)
	{
		string text = prefab.name + "(Clone)";
		if (prefab.GetComponent<BaseAI>() != null)
		{
			List<BaseAI> baseAIInstances = BaseAI.BaseAIInstances;
			int num = 0;
			{
				foreach (BaseAI item in baseAIInstances)
				{
					if (item.gameObject.name != text || (maxRange > 0f && Vector3.Distance(center, item.transform.position) > maxRange))
					{
						continue;
					}
					if (eventCreaturesOnly)
					{
						MonsterAI monsterAI = item as MonsterAI;
						if ((bool)monsterAI && !monsterAI.IsEventCreature())
						{
							continue;
						}
					}
					if (procreationOnly)
					{
						Procreation component = item.GetComponent<Procreation>();
						if ((bool)component && !component.ReadyForProcreation())
						{
							continue;
						}
					}
					num++;
				}
				return num;
			}
		}
		GameObject[] array = GameObject.FindGameObjectsWithTag("spawned");
		int num2 = 0;
		GameObject[] array2 = array;
		foreach (GameObject gameObject in array2)
		{
			if (gameObject.name.CustomStartsWith(text) && (!(maxRange > 0f) || !(Vector3.Distance(center, gameObject.transform.position) > maxRange)))
			{
				num2++;
			}
		}
		return num2;
	}

	private bool InsideZone(Vector3 point, float extra = 0f)
	{
		float num = ZoneSystem.instance.m_zoneSize * 0.5f + extra;
		Vector3 position = base.transform.position;
		if (point.x < position.x - num || point.x > position.x + num)
		{
			return false;
		}
		if (point.z < position.z - num || point.z > position.z + num)
		{
			return false;
		}
		return true;
	}

	private bool HaveGlobalKeys(SpawnData ev)
	{
		if (!string.IsNullOrEmpty(ev.m_requiredGlobalKey))
		{
			return ZoneSystem.instance.GetGlobalKey(ev.m_requiredGlobalKey);
		}
		return true;
	}

	public static float GetLevelUpChance(SpawnData creature)
	{
		return GetLevelUpChance((creature.m_overrideLevelupChance >= 0f) ? creature.m_overrideLevelupChance : 0f);
	}

	public static float GetLevelUpChance(float levelUpChanceOverride = 0f)
	{
		float num = ((levelUpChanceOverride > 0f) ? levelUpChanceOverride : 10f);
		if (Game.m_worldLevel > 0 && Game.instance.m_worldLevelEnemyLevelUpExponent > 0f)
		{
			return Mathf.Min(70f, Mathf.Pow(num, (float)Game.m_worldLevel * Game.instance.m_worldLevelEnemyLevelUpExponent));
		}
		return num * Game.m_enemyLevelUpRate;
	}
}
