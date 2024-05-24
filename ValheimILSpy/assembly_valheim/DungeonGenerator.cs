using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoftReferenceableAssets;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
	[Serializable]
	public class DoorDef
	{
		public GameObject m_prefab;

		public string m_connectionType = "";

		[Tooltip("Will use default door chance set in DungeonGenerator if set to zero to default to old behaviour")]
		[Range(0f, 1f)]
		public float m_chance;
	}

	private struct RoomPlacementData
	{
		public DungeonDB.RoomData m_roomData;

		public Vector3 m_position;

		public Quaternion m_rotation;

		public RoomPlacementData(DungeonDB.RoomData roomData, Vector3 position, Quaternion rotation)
		{
			m_roomData = roomData;
			m_position = position;
			m_rotation = rotation;
		}
	}

	public enum Algorithm
	{
		Dungeon,
		CampGrid,
		CampRadial
	}

	private static MemoryStream saveStream = new MemoryStream();

	private static BinaryWriter saveWriter = new BinaryWriter(saveStream);

	public static int m_forceSeed = int.MinValue;

	public Algorithm m_algorithm;

	public int m_maxRooms = 3;

	public int m_minRooms = 20;

	public int m_minRequiredRooms;

	public List<string> m_requiredRooms = new List<string>();

	[Tooltip("Rooms and endcaps will be placed using weights.")]
	public bool m_alternativeFunctionality;

	[BitMask(typeof(Room.Theme))]
	public Room.Theme m_themes = Room.Theme.Crypt;

	[Header("Dungeon")]
	public List<DoorDef> m_doorTypes = new List<DoorDef>();

	[Range(0f, 1f)]
	public float m_doorChance = 0.5f;

	[Header("Camp")]
	public float m_maxTilt = 10f;

	public float m_tileWidth = 8f;

	public int m_gridSize = 4;

	public float m_spawnChance = 1f;

	[Header("Camp radial")]
	public float m_campRadiusMin = 15f;

	public float m_campRadiusMax = 30f;

	public float m_minAltitude = 1f;

	public int m_perimeterSections;

	public float m_perimeterBuffer = 2f;

	[Header("Misc")]
	public Vector3 m_zoneCenter = new Vector3(0f, 0f, 0f);

	public Vector3 m_zoneSize = new Vector3(64f, 64f, 64f);

	[Tooltip("Makes the dungeon entrance start at the given interior transform (including rotation) rather than straight above the entrance, which gives the dungeon much more room to fill out the entire zone. Must use together with Location.m_useCustomInteriorTransform to make sure seeds are deterministic.")]
	public bool m_useCustomInteriorTransform;

	[HideInInspector]
	public int m_generatedSeed;

	private bool m_hasGeneratedSeed;

	private ZDO m_zdoSetToBeLoadingInZone;

	private int m_roomsToLoad;

	private RoomPlacementData[] m_loadedRooms;

	private List<IReferenceCounted> m_heldReferences = new List<IReferenceCounted>();

	private static List<Room> m_placedRooms = new List<Room>();

	private static List<RoomConnection> m_openConnections = new List<RoomConnection>();

	private static List<RoomConnection> m_doorConnections = new List<RoomConnection>();

	private static List<DungeonDB.RoomData> m_availableRooms = new List<DungeonDB.RoomData>();

	private static List<DungeonDB.RoomData> m_tempRooms = new List<DungeonDB.RoomData>();

	private BoxCollider m_colliderA;

	private BoxCollider m_colliderB;

	private ZNetView m_nview;

	[HideInInspector]
	public Vector3 m_originalPosition;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		Load();
		if (m_loadedRooms.Length != 0)
		{
			LoadRoomPrefabsAsync();
		}
	}

	private void OnDestroy()
	{
		ReleaseHeldReferences();
	}

	private void ReleaseHeldReferences()
	{
		for (int i = 0; i < m_heldReferences.Count; i++)
		{
			m_heldReferences[i].Release();
		}
		m_heldReferences.Clear();
		if (m_zdoSetToBeLoadingInZone != null)
		{
			ZoneSystem.instance.UnsetLoadingInZone(m_zdoSetToBeLoadingInZone);
			m_zdoSetToBeLoadingInZone = null;
		}
	}

	public void Clear()
	{
		while (base.transform.childCount > 0)
		{
			UnityEngine.Object.DestroyImmediate(base.transform.GetChild(0).gameObject);
		}
	}

	public void Generate(ZoneSystem.SpawnMode mode)
	{
		int seed = GetSeed();
		Generate(seed, mode);
	}

	public int GetSeed()
	{
		if (m_hasGeneratedSeed)
		{
			return m_generatedSeed;
		}
		if (m_forceSeed != int.MinValue)
		{
			m_generatedSeed = m_forceSeed;
			m_forceSeed = int.MinValue;
		}
		else
		{
			int seed = WorldGenerator.instance.GetSeed();
			Vector3 position = base.transform.position;
			Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
			m_generatedSeed = seed + zone.x * 4271 + zone.y * -7187 + (int)position.x * -4271 + (int)position.y * 9187 + (int)position.z * -2134;
		}
		m_hasGeneratedSeed = true;
		return m_generatedSeed;
	}

	public void Generate(int seed, ZoneSystem.SpawnMode mode)
	{
		DateTime now = DateTime.Now;
		m_generatedSeed = seed;
		Clear();
		SetupColliders();
		SetupAvailableRooms();
		for (int i = 0; i < m_availableRooms.Count; i++)
		{
			m_availableRooms[i].m_prefab.Load();
		}
		if ((bool)ZoneSystem.instance)
		{
			Vector2i zone = ZoneSystem.instance.GetZone(base.transform.position);
			m_zoneCenter = ZoneSystem.instance.GetZonePos(zone);
			m_zoneCenter.y = base.transform.position.y - m_originalPosition.y;
		}
		Bounds bounds = new Bounds(m_zoneCenter, m_zoneSize);
		ZLog.Log($"Generating {base.name}, Seed: {seed}, Bounds diff: {bounds.min - base.transform.position} / {bounds.max - base.transform.position}");
		ZLog.Log("Available rooms:" + m_availableRooms.Count);
		ZLog.Log("To place:" + m_maxRooms);
		m_placedRooms.Clear();
		m_openConnections.Clear();
		m_doorConnections.Clear();
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		GenerateRooms(mode);
		for (int j = 0; j < m_availableRooms.Count; j++)
		{
			m_availableRooms[j].m_prefab.Release();
		}
		Save();
		ZLog.Log("Placed " + m_placedRooms.Count + " rooms");
		UnityEngine.Random.state = state;
		SnapToGround.SnappAll();
		if (mode == ZoneSystem.SpawnMode.Ghost)
		{
			foreach (Room placedRoom in m_placedRooms)
			{
				UnityEngine.Object.DestroyImmediate(placedRoom.gameObject);
			}
		}
		UnityEngine.Object.DestroyImmediate(m_colliderA);
		UnityEngine.Object.DestroyImmediate(m_colliderB);
		m_placedRooms.Clear();
		m_openConnections.Clear();
		m_doorConnections.Clear();
		_ = DateTime.Now - now;
	}

	private void LoadRoomPrefabsAsync()
	{
		ZLog.Log("Loading room prefabs asynchronously");
		if (m_zdoSetToBeLoadingInZone == null)
		{
			m_zdoSetToBeLoadingInZone = m_nview.GetZDO();
			ZoneSystem.instance.SetLoadingInZone(m_zdoSetToBeLoadingInZone);
		}
		m_roomsToLoad = m_loadedRooms.Length;
		int num = m_loadedRooms.Length;
		for (int i = 0; i < num; i++)
		{
			m_heldReferences.Add(m_loadedRooms[i].m_roomData.m_prefab);
			m_loadedRooms[i].m_roomData.m_prefab.LoadAsync(OnRoomLoaded);
		}
	}

	private void OnRoomLoaded(LoadResult result)
	{
		if (result == LoadResult.Succeeded && !(this == null) && !(base.gameObject == null))
		{
			m_roomsToLoad--;
			if (m_roomsToLoad <= 0)
			{
				Spawn();
				ReleaseHeldReferences();
			}
		}
	}

	private void Spawn()
	{
		ZLog.Log("Spawning dungeon");
		for (int i = 0; i < m_loadedRooms.Length; i++)
		{
			PlaceRoom(m_loadedRooms[i].m_roomData, m_loadedRooms[i].m_position, m_loadedRooms[i].m_rotation, null, ZoneSystem.SpawnMode.Client);
		}
		SnapToGround.SnappAll();
		m_loadedRooms = null;
	}

	private void GenerateRooms(ZoneSystem.SpawnMode mode)
	{
		switch (m_algorithm)
		{
		case Algorithm.Dungeon:
			GenerateDungeon(mode);
			break;
		case Algorithm.CampGrid:
			GenerateCampGrid(mode);
			break;
		case Algorithm.CampRadial:
			GenerateCampRadial(mode);
			break;
		}
	}

	private void GenerateDungeon(ZoneSystem.SpawnMode mode)
	{
		PlaceStartRoom(mode);
		PlaceRooms(mode);
		PlaceEndCaps(mode);
		PlaceDoors(mode);
	}

	private void GenerateCampGrid(ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos((float)Math.PI / 180f * m_maxTilt);
		Vector3 vector = base.transform.position + new Vector3((float)(-m_gridSize) * m_tileWidth * 0.5f, 0f, (float)(-m_gridSize) * m_tileWidth * 0.5f);
		for (int i = 0; i < m_gridSize; i++)
		{
			for (int j = 0; j < m_gridSize; j++)
			{
				if (UnityEngine.Random.value > m_spawnChance)
				{
					continue;
				}
				Vector3 p = vector + new Vector3((float)j * m_tileWidth, 0f, (float)i * m_tileWidth);
				DungeonDB.RoomData randomWeightedRoom = GetRandomWeightedRoom(perimeterRoom: false);
				if (randomWeightedRoom == null)
				{
					continue;
				}
				if ((bool)ZoneSystem.instance)
				{
					ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
					if (normal.y < num)
					{
						continue;
					}
				}
				Quaternion rot = Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
				PlaceRoom(randomWeightedRoom, p, rot, null, mode);
			}
		}
	}

	private void GenerateCampRadial(ZoneSystem.SpawnMode mode)
	{
		float num = UnityEngine.Random.Range(m_campRadiusMin, m_campRadiusMax);
		float num2 = Mathf.Cos((float)Math.PI / 180f * m_maxTilt);
		int num3 = UnityEngine.Random.Range(m_minRooms, m_maxRooms);
		int num4 = num3 * 20;
		int num5 = 0;
		for (int i = 0; i < num4; i++)
		{
			Vector3 p = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * UnityEngine.Random.Range(0f, num - m_perimeterBuffer);
			DungeonDB.RoomData randomWeightedRoom = GetRandomWeightedRoom(perimeterRoom: false);
			if (randomWeightedRoom == null)
			{
				continue;
			}
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
				if (normal.y < num2 || p.y - 30f < m_minAltitude)
				{
					continue;
				}
			}
			Quaternion campRoomRotation = GetCampRoomRotation(randomWeightedRoom, p);
			if (!TestCollision(randomWeightedRoom.RoomInPrefab, p, campRoomRotation))
			{
				PlaceRoom(randomWeightedRoom, p, campRoomRotation, null, mode);
				num5++;
				if (num5 >= num3)
				{
					break;
				}
			}
		}
		if (m_perimeterSections > 0)
		{
			PlaceWall(num, m_perimeterSections, mode);
		}
	}

	private Quaternion GetCampRoomRotation(DungeonDB.RoomData room, Vector3 pos)
	{
		if (room.RoomInPrefab.m_faceCenter)
		{
			Vector3 vector = base.transform.position - pos;
			vector.y = 0f;
			if (vector == Vector3.zero)
			{
				vector = Vector3.forward;
			}
			vector.Normalize();
			float y = Mathf.Round(Utils.YawFromDirection(vector) / 22.5f) * 22.5f;
			return Quaternion.Euler(0f, y, 0f);
		}
		return Quaternion.Euler(0f, (float)UnityEngine.Random.Range(0, 16) * 22.5f, 0f);
	}

	private void PlaceWall(float radius, int sections, ZoneSystem.SpawnMode mode)
	{
		float num = Mathf.Cos((float)Math.PI / 180f * m_maxTilt);
		int num2 = 0;
		int num3 = sections * 20;
		for (int i = 0; i < num3; i++)
		{
			DungeonDB.RoomData randomWeightedRoom = GetRandomWeightedRoom(perimeterRoom: true);
			if (randomWeightedRoom == null)
			{
				continue;
			}
			Vector3 p = base.transform.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * radius;
			Quaternion campRoomRotation = GetCampRoomRotation(randomWeightedRoom, p);
			if ((bool)ZoneSystem.instance)
			{
				ZoneSystem.instance.GetGroundData(ref p, out var normal, out var _, out var _, out var _);
				if (normal.y < num || p.y - 30f < m_minAltitude)
				{
					continue;
				}
			}
			if (!TestCollision(randomWeightedRoom.RoomInPrefab, p, campRoomRotation))
			{
				PlaceRoom(randomWeightedRoom, p, campRoomRotation, null, mode);
				num2++;
				if (num2 >= sections)
				{
					break;
				}
			}
		}
	}

	private void Save()
	{
		if (m_nview == null)
		{
			return;
		}
		ZDO zDO = m_nview.GetZDO();
		saveStream.SetLength(0L);
		saveWriter.Write(m_placedRooms.Count);
		for (int i = 0; i < m_placedRooms.Count; i++)
		{
			Room room = m_placedRooms[i];
			saveWriter.Write(room.GetHash());
			saveWriter.Write(room.transform.position);
			saveWriter.Write(room.transform.rotation);
		}
		zDO.Set(ZDOVars.s_roomData, saveStream.ToArray());
		if (zDO.GetInt(ZDOVars.s_rooms, out var value))
		{
			zDO.RemoveInt(ZDOVars.s_rooms);
			for (int j = 0; j < value; j++)
			{
				string text = "room" + j;
				zDO.RemoveInt(text);
				zDO.RemoveVec3(text + "_pos");
				zDO.RemoveQuaternion(text + "_rot");
				zDO.RemoveInt(text + "_seed");
			}
			ZLog.Log($"Cleaned up old dungeon data format for {value} rooms.");
		}
	}

	private void Load()
	{
		if (m_nview == null)
		{
			return;
		}
		DateTime now = DateTime.Now;
		ZLog.Log("Loading dungeon");
		ZDO zDO = m_nview.GetZDO();
		int num = 0;
		if (zDO.GetByteArray(ZDOVars.s_roomData, out var value))
		{
			BinaryReader binaryReader = new BinaryReader(new MemoryStream(value));
			int num2 = binaryReader.ReadInt32();
			m_loadedRooms = new RoomPlacementData[num2];
			for (int i = 0; i < num2; i++)
			{
				int hash = binaryReader.ReadInt32();
				Vector3 position = binaryReader.ReadVector3();
				Quaternion rotation = binaryReader.ReadQuaternion();
				DungeonDB.RoomData room = DungeonDB.instance.GetRoom(hash);
				if (room == null)
				{
					ZLog.LogWarning("Missing room:" + hash);
				}
				else
				{
					m_loadedRooms[num++] = new RoomPlacementData(room, position, rotation);
				}
			}
			ZLog.Log($"Dungeon loaded with {num2} rooms in {(DateTime.Now - now).TotalMilliseconds} ms.");
		}
		else
		{
			int @int = zDO.GetInt("rooms");
			m_loadedRooms = new RoomPlacementData[@int];
			for (int j = 0; j < @int; j++)
			{
				string text = "room" + j;
				int int2 = zDO.GetInt(text);
				Vector3 vec = zDO.GetVec3(text + "_pos", Vector3.zero);
				Quaternion quaternion = zDO.GetQuaternion(text + "_rot", Quaternion.identity);
				DungeonDB.RoomData room2 = DungeonDB.instance.GetRoom(int2);
				if (room2 == null)
				{
					ZLog.LogWarning("Missing room:" + int2);
				}
				else
				{
					m_loadedRooms[num++] = new RoomPlacementData(room2, vec, quaternion);
				}
			}
			ZLog.Log($"Dungeon loaded with {@int} rooms from old format in {(DateTime.Now - now).TotalMilliseconds} ms.");
		}
		if (num < m_loadedRooms.Length)
		{
			RoomPlacementData[] array = new RoomPlacementData[num];
			Array.Copy(m_loadedRooms, array, num);
			m_loadedRooms = array;
		}
	}

	private void SetupAvailableRooms()
	{
		m_availableRooms.Clear();
		foreach (DungeonDB.RoomData room in DungeonDB.GetRooms())
		{
			if ((room.m_theme & m_themes) != 0 && room.m_enabled)
			{
				m_availableRooms.Add(room);
			}
		}
	}

	public SoftReference<GameObject>[] GetAvailableRoomPrefabs()
	{
		SetupAvailableRooms();
		SoftReference<GameObject>[] array = new SoftReference<GameObject>[m_availableRooms.Count];
		for (int i = 0; i < m_availableRooms.Count; i++)
		{
			array[i] = m_availableRooms[i].m_prefab;
		}
		return array;
	}

	private DoorDef FindDoorType(string type)
	{
		List<DoorDef> list = new List<DoorDef>();
		foreach (DoorDef doorType in m_doorTypes)
		{
			if (doorType.m_connectionType == type)
			{
				list.Add(doorType);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[UnityEngine.Random.Range(0, list.Count)];
	}

	private void PlaceDoors(ZoneSystem.SpawnMode mode)
	{
		int num = 0;
		foreach (RoomConnection doorConnection in m_doorConnections)
		{
			DoorDef doorDef = FindDoorType(doorConnection.m_type);
			if (doorDef == null)
			{
				ZLog.Log("No door type for connection:" + doorConnection.m_type);
			}
			else if ((!(doorDef.m_chance > 0f) || !(UnityEngine.Random.value > doorDef.m_chance)) && (!(doorDef.m_chance <= 0f) || !(UnityEngine.Random.value > m_doorChance)))
			{
				GameObject obj = UnityEngine.Object.Instantiate(doorDef.m_prefab, doorConnection.transform.position, doorConnection.transform.rotation);
				if (mode == ZoneSystem.SpawnMode.Ghost)
				{
					UnityEngine.Object.Destroy(obj);
				}
				num++;
			}
		}
		ZLog.Log("placed " + num + " doors");
	}

	private void PlaceEndCaps(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < m_openConnections.Count; i++)
		{
			RoomConnection roomConnection = m_openConnections[i];
			RoomConnection roomConnection2 = null;
			for (int j = 0; j < m_openConnections.Count; j++)
			{
				if (j != i && roomConnection.TestContact(m_openConnections[j]))
				{
					roomConnection2 = m_openConnections[j];
					break;
				}
			}
			if (roomConnection2 != null)
			{
				if (roomConnection.m_type != roomConnection2.m_type)
				{
					FindDividers(m_tempRooms);
					if (m_tempRooms.Count > 0)
					{
						DungeonDB.RoomData weightedRoom = GetWeightedRoom(m_tempRooms);
						RoomConnection[] connections = weightedRoom.RoomInPrefab.GetConnections();
						CalculateRoomPosRot(connections[0], roomConnection.transform.position, roomConnection.transform.rotation, out var pos, out var rot);
						bool flag = false;
						foreach (Room placedRoom in m_placedRooms)
						{
							if (placedRoom.m_divider && Vector3.Distance(placedRoom.transform.position, pos) < 0.5f)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							PlaceRoom(weightedRoom, pos, rot, roomConnection, mode);
							ZLog.Log("Cyclic detected. Door missmatch for cyclic room '" + roomConnection.m_type + "'-'" + roomConnection2.m_type + "', placing divider: " + weightedRoom.m_prefab.Name);
						}
					}
					else
					{
						ZLog.LogWarning("Cyclic detected. Door missmatch for cyclic room '" + roomConnection.m_type + "'-'" + roomConnection2.m_type + "', but no dividers defined!");
					}
				}
				else
				{
					ZLog.Log("cyclic detected and door types match, cool");
				}
				continue;
			}
			FindEndCaps(roomConnection, m_tempRooms);
			bool flag2 = false;
			if (m_alternativeFunctionality)
			{
				for (int k = 0; k < 5; k++)
				{
					DungeonDB.RoomData weightedRoom2 = GetWeightedRoom(m_tempRooms);
					if (PlaceRoom(roomConnection, weightedRoom2, mode))
					{
						flag2 = true;
						break;
					}
				}
			}
			IOrderedEnumerable<DungeonDB.RoomData> orderedEnumerable = m_tempRooms.OrderByDescending((DungeonDB.RoomData item) => item.RoomInPrefab.m_endCapPrio);
			if (!flag2)
			{
				foreach (DungeonDB.RoomData item in orderedEnumerable)
				{
					if (PlaceRoom(roomConnection, item, mode))
					{
						flag2 = true;
						break;
					}
				}
			}
			if (!flag2)
			{
				ZLog.LogWarning("Failed to place end cap " + roomConnection.name + " " + roomConnection.transform.parent.gameObject.name);
			}
		}
	}

	private void FindDividers(List<DungeonDB.RoomData> rooms)
	{
		rooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.RoomInPrefab.m_divider)
			{
				rooms.Add(availableRoom);
			}
		}
		rooms.Shuffle(useUnityRandom: true);
	}

	private void FindEndCaps(RoomConnection connection, List<DungeonDB.RoomData> rooms)
	{
		rooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.RoomInPrefab.m_endCap && availableRoom.RoomInPrefab.HaveConnection(connection))
			{
				rooms.Add(availableRoom);
			}
		}
		rooms.Shuffle(useUnityRandom: true);
	}

	private DungeonDB.RoomData FindEndCap(RoomConnection connection)
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.RoomInPrefab.m_endCap && availableRoom.RoomInPrefab.HaveConnection(connection))
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		return m_tempRooms[UnityEngine.Random.Range(0, m_tempRooms.Count)];
	}

	private void PlaceRooms(ZoneSystem.SpawnMode mode)
	{
		for (int i = 0; i < m_maxRooms; i++)
		{
			PlaceOneRoom(mode);
			if (CheckRequiredRooms() && m_placedRooms.Count > m_minRooms)
			{
				ZLog.Log("All required rooms have been placed, stopping generation");
				break;
			}
		}
	}

	private void PlaceStartRoom(ZoneSystem.SpawnMode mode)
	{
		DungeonDB.RoomData roomData = FindStartRoom();
		RoomConnection entrance = roomData.RoomInPrefab.GetEntrance();
		Quaternion rotation = base.transform.rotation;
		CalculateRoomPosRot(entrance, base.transform.position, rotation, out var pos, out var rot);
		PlaceRoom(roomData, pos, rot, entrance, mode);
	}

	private bool PlaceOneRoom(ZoneSystem.SpawnMode mode)
	{
		RoomConnection openConnection = GetOpenConnection();
		if (openConnection == null)
		{
			return false;
		}
		for (int i = 0; i < 10; i++)
		{
			DungeonDB.RoomData roomData = (m_alternativeFunctionality ? GetRandomWeightedRoom(openConnection) : GetRandomRoom(openConnection));
			if (roomData == null)
			{
				break;
			}
			if (PlaceRoom(openConnection, roomData, mode))
			{
				return true;
			}
		}
		return false;
	}

	private void CalculateRoomPosRot(RoomConnection roomCon, Vector3 exitPos, Quaternion exitRot, out Vector3 pos, out Quaternion rot)
	{
		Quaternion quaternion = Quaternion.Inverse(roomCon.transform.localRotation);
		rot = exitRot * quaternion;
		Vector3 localPosition = roomCon.transform.localPosition;
		pos = exitPos - rot * localPosition;
	}

	private bool PlaceRoom(RoomConnection connection, DungeonDB.RoomData roomData, ZoneSystem.SpawnMode mode)
	{
		SoftReference<GameObject> prefab = roomData.m_prefab;
		prefab.Load();
		Room component = prefab.Asset.GetComponent<Room>();
		Quaternion rotation = connection.transform.rotation;
		rotation *= Quaternion.Euler(0f, 180f, 0f);
		RoomConnection connection2 = component.GetConnection(connection);
		if (connection2.transform.parent.gameObject != component.gameObject)
		{
			ZLog.LogWarning("Connection '" + component.name + "->" + connection2.name + "' is not placed as a direct child of room!");
		}
		CalculateRoomPosRot(connection2, connection.transform.position, rotation, out var pos, out var rot);
		if (component.m_size.x != 0 && component.m_size.z != 0 && TestCollision(component, pos, rot))
		{
			prefab.Release();
			return false;
		}
		PlaceRoom(roomData, pos, rot, connection, mode);
		if (!component.m_endCap)
		{
			if (connection.m_allowDoor && (!connection.m_doorOnlyIfOtherAlsoAllowsDoor || connection2.m_allowDoor))
			{
				m_doorConnections.Add(connection);
			}
			m_openConnections.Remove(connection);
		}
		prefab.Release();
		return true;
	}

	private Room PlaceRoom(DungeonDB.RoomData roomData, Vector3 pos, Quaternion rot, RoomConnection fromConnection, ZoneSystem.SpawnMode mode)
	{
		roomData.m_prefab.Load();
		Room component = roomData.m_prefab.Asset.GetComponent<Room>();
		ZNetView[] enabledComponentsInChildren = Utils.GetEnabledComponentsInChildren<ZNetView>(roomData.m_prefab.Asset);
		RandomSpawn[] enabledComponentsInChildren2 = Utils.GetEnabledComponentsInChildren<RandomSpawn>(roomData.m_prefab.Asset);
		for (int i = 0; i < enabledComponentsInChildren2.Length; i++)
		{
			enabledComponentsInChildren2[i].Prepare();
		}
		Vector3 vector = pos;
		if (m_useCustomInteriorTransform)
		{
			vector = pos - base.transform.position;
		}
		int seed = (int)vector.x * 4271 + (int)vector.y * 9187 + (int)vector.z * 2134;
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(seed);
		Vector3 position = component.transform.position;
		Quaternion quaternion = Quaternion.Inverse(component.transform.rotation);
		RandomSpawn[] array;
		ZNetView[] array2;
		if (mode == ZoneSystem.SpawnMode.Full || mode == ZoneSystem.SpawnMode.Ghost)
		{
			UnityEngine.Random.InitState(seed);
			array = enabledComponentsInChildren2;
			foreach (RandomSpawn randomSpawn in array)
			{
				Vector3 vector2 = quaternion * (randomSpawn.gameObject.transform.position - position);
				Vector3 pos2 = pos + rot * vector2;
				randomSpawn.Randomize(pos2, null, this);
			}
			array2 = enabledComponentsInChildren;
			foreach (ZNetView zNetView in array2)
			{
				if (zNetView.gameObject.activeSelf)
				{
					Vector3 vector3 = quaternion * (zNetView.gameObject.transform.position - position);
					Vector3 position2 = pos + rot * vector3;
					Quaternion quaternion2 = quaternion * zNetView.gameObject.transform.rotation;
					Quaternion rotation = rot * quaternion2;
					GameObject gameObject = UnityEngine.Object.Instantiate(zNetView.gameObject, position2, rotation);
					gameObject.HoldReferenceTo(roomData.m_prefab);
					if (mode == ZoneSystem.SpawnMode.Ghost)
					{
						UnityEngine.Object.Destroy(gameObject);
					}
				}
			}
		}
		else
		{
			UnityEngine.Random.InitState(seed);
			array = enabledComponentsInChildren2;
			foreach (RandomSpawn randomSpawn2 in array)
			{
				Vector3 vector4 = quaternion * (randomSpawn2.gameObject.transform.position - position);
				Vector3 pos3 = pos + rot * vector4;
				randomSpawn2.Randomize(pos3, null, this);
			}
		}
		array2 = enabledComponentsInChildren;
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j].gameObject.SetActive(value: false);
		}
		Room component2 = SoftReferenceableAssets.Utils.Instantiate(roomData.m_prefab, pos, rot, base.transform).GetComponent<Room>();
		component2.gameObject.name = roomData.m_prefab.Name;
		if (mode != ZoneSystem.SpawnMode.Client)
		{
			component2.m_placeOrder = (fromConnection ? (fromConnection.m_placeOrder + 1) : 0);
			component2.m_seed = seed;
			m_placedRooms.Add(component2);
			AddOpenConnections(component2, fromConnection);
		}
		UnityEngine.Random.state = state;
		array = enabledComponentsInChildren2;
		for (int j = 0; j < array.Length; j++)
		{
			array[j].Reset();
		}
		array2 = enabledComponentsInChildren;
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j].gameObject.SetActive(value: true);
		}
		roomData.m_prefab.Release();
		return component2;
	}

	private void AddOpenConnections(Room newRoom, RoomConnection skipConnection)
	{
		RoomConnection[] connections = newRoom.GetConnections();
		if (skipConnection != null)
		{
			RoomConnection[] array = connections;
			foreach (RoomConnection roomConnection in array)
			{
				if (!roomConnection.m_entrance && !(Vector3.Distance(roomConnection.transform.position, skipConnection.transform.position) < 0.1f))
				{
					roomConnection.m_placeOrder = newRoom.m_placeOrder;
					m_openConnections.Add(roomConnection);
				}
			}
		}
		else
		{
			RoomConnection[] array = connections;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].m_placeOrder = newRoom.m_placeOrder;
			}
			m_openConnections.AddRange(connections);
		}
	}

	private void SetupColliders()
	{
		if (!(m_colliderA != null))
		{
			BoxCollider[] componentsInChildren = base.gameObject.GetComponentsInChildren<BoxCollider>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				UnityEngine.Object.DestroyImmediate(componentsInChildren[i]);
			}
			m_colliderA = base.gameObject.AddComponent<BoxCollider>();
			m_colliderB = base.gameObject.AddComponent<BoxCollider>();
		}
	}

	public void Derp()
	{
	}

	private bool IsInsideDungeon(Room room, Vector3 pos, Quaternion rot)
	{
		Bounds bounds = new Bounds(m_zoneCenter, m_zoneSize);
		Vector3 vector = room.m_size;
		vector *= 0.5f;
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, vector.y, vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, vector.y, vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, 0f - vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, 0f - vector.y, 0f - vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(vector.x, 0f - vector.y, vector.z)))
		{
			return false;
		}
		if (!bounds.Contains(pos + rot * new Vector3(0f - vector.x, 0f - vector.y, vector.z)))
		{
			return false;
		}
		return true;
	}

	private bool TestCollision(Room room, Vector3 pos, Quaternion rot)
	{
		if (!IsInsideDungeon(room, pos, rot))
		{
			return true;
		}
		m_colliderA.size = new Vector3((float)room.m_size.x - 0.1f, (float)room.m_size.y - 0.1f, (float)room.m_size.z - 0.1f);
		foreach (Room placedRoom in m_placedRooms)
		{
			m_colliderB.size = placedRoom.m_size;
			if (Physics.ComputePenetration(m_colliderA, pos, rot, m_colliderB, placedRoom.transform.position, placedRoom.transform.rotation, out var _, out var _))
			{
				return true;
			}
		}
		return false;
	}

	private DungeonDB.RoomData GetRandomWeightedRoom(bool perimeterRoom)
	{
		m_tempRooms.Clear();
		float num = 0f;
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (!availableRoom.RoomInPrefab.m_entrance && !availableRoom.RoomInPrefab.m_endCap && !availableRoom.RoomInPrefab.m_divider && availableRoom.RoomInPrefab.m_perimeter == perimeterRoom)
			{
				num += availableRoom.RoomInPrefab.m_weight;
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (DungeonDB.RoomData tempRoom in m_tempRooms)
		{
			num3 += tempRoom.RoomInPrefab.m_weight;
			if (num2 <= num3)
			{
				return tempRoom;
			}
		}
		return m_tempRooms[0];
	}

	private DungeonDB.RoomData GetRandomWeightedRoom(RoomConnection connection)
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (!availableRoom.RoomInPrefab.m_entrance && !availableRoom.RoomInPrefab.m_endCap && !availableRoom.RoomInPrefab.m_divider && (!connection || (availableRoom.RoomInPrefab.HaveConnection(connection) && connection.m_placeOrder >= availableRoom.RoomInPrefab.m_minPlaceOrder)))
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		return GetWeightedRoom(m_tempRooms);
	}

	private DungeonDB.RoomData GetWeightedRoom(List<DungeonDB.RoomData> rooms)
	{
		float num = 0f;
		foreach (DungeonDB.RoomData room in rooms)
		{
			num += room.RoomInPrefab.m_weight;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		float num3 = 0f;
		foreach (DungeonDB.RoomData room2 in rooms)
		{
			num3 += room2.RoomInPrefab.m_weight;
			if (num2 <= num3)
			{
				return room2;
			}
		}
		return m_tempRooms[0];
	}

	private DungeonDB.RoomData GetRandomRoom(RoomConnection connection)
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (!availableRoom.RoomInPrefab.m_entrance && !availableRoom.RoomInPrefab.m_endCap && !availableRoom.RoomInPrefab.m_divider && (!connection || (availableRoom.RoomInPrefab.HaveConnection(connection) && connection.m_placeOrder >= availableRoom.RoomInPrefab.m_minPlaceOrder)))
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		if (m_tempRooms.Count == 0)
		{
			return null;
		}
		return m_tempRooms[UnityEngine.Random.Range(0, m_tempRooms.Count)];
	}

	private RoomConnection GetOpenConnection()
	{
		if (m_openConnections.Count == 0)
		{
			return null;
		}
		return m_openConnections[UnityEngine.Random.Range(0, m_openConnections.Count)];
	}

	private DungeonDB.RoomData FindStartRoom()
	{
		m_tempRooms.Clear();
		foreach (DungeonDB.RoomData availableRoom in m_availableRooms)
		{
			if (availableRoom.RoomInPrefab.m_entrance)
			{
				m_tempRooms.Add(availableRoom);
			}
		}
		return m_tempRooms[UnityEngine.Random.Range(0, m_tempRooms.Count)];
	}

	private bool CheckRequiredRooms()
	{
		if (m_minRequiredRooms == 0 || m_requiredRooms.Count == 0)
		{
			return false;
		}
		int num = 0;
		foreach (Room placedRoom in m_placedRooms)
		{
			if (m_requiredRooms.Contains(placedRoom.gameObject.name))
			{
				num++;
			}
		}
		return num >= m_minRequiredRooms;
	}

	private void OnDrawGizmos()
	{
		Gizmos.color = new Color(0f, 1.5f, 0f, 0.5f);
		Gizmos.DrawWireCube(m_zoneCenter, new Vector3(m_zoneSize.x, m_zoneSize.y, m_zoneSize.z));
		Gizmos.matrix = Matrix4x4.identity;
	}
}
