using System;
using System.Collections.Generic;
using SoftReferenceableAssets;
using UnityEngine;

public class DungeonDB : MonoBehaviour
{
	[Serializable]
	public class RoomData
	{
		public SoftReference<GameObject> m_prefab;

		public bool m_enabled;

		[BitMask(typeof(Room.Theme))]
		public Room.Theme m_theme;

		[NonSerialized]
		private Room m_loadedRoom;

		public Room RoomInPrefab
		{
			get
			{
				if (m_loadedRoom == null)
				{
					if (m_prefab.Asset != null)
					{
						m_loadedRoom = m_prefab.Asset.GetComponent<Room>();
					}
					else
					{
						Debug.LogError($"Room {m_prefab} wasn't loaded!");
					}
				}
				return m_loadedRoom;
			}
		}

		public int Hash => m_prefab.Name.GetStableHashCode();
	}

	private static DungeonDB m_instance;

	public List<string> m_roomScenes = new List<string>();

	public List<GameObject> m_roomLists = new List<GameObject>();

	private List<RoomData> m_rooms = new List<RoomData>();

	private Dictionary<int, RoomData> m_roomByHash = new Dictionary<int, RoomData>();

	private bool m_error;

	public static DungeonDB instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		ZLog.Log("DungeonDB Awake " + Time.frameCount);
	}

	public bool SkipSaving()
	{
		return m_error;
	}

	private void Start()
	{
		ZLog.Log("DungeonDB Start " + Time.frameCount);
		SetupRooms();
		GenerateHashList();
		LoadRooms();
	}

	private void SetupRooms()
	{
		foreach (GameObject roomList in m_roomLists)
		{
			UnityEngine.Object.Instantiate(roomList);
		}
		foreach (RoomList allRoomList in RoomList.GetAllRoomLists())
		{
			m_rooms.AddRange(allRoomList.m_rooms);
		}
	}

	public static List<RoomData> GetRooms()
	{
		return m_instance.m_rooms;
	}

	public RoomData GetRoom(int hash)
	{
		if (m_roomByHash.TryGetValue(hash, out var value))
		{
			return value;
		}
		return null;
	}

	private void GenerateHashList()
	{
		m_roomByHash.Clear();
		foreach (RoomData room in m_rooms)
		{
			int hash = room.Hash;
			if (m_roomByHash.ContainsKey(hash))
			{
				ZLog.LogError("Room with name " + room.m_prefab.Name + " already registered");
			}
			else
			{
				m_roomByHash.Add(hash, room);
			}
		}
	}

	private void LoadRooms()
	{
		if (!Settings.AssetMemoryUsagePolicy.HasFlag(AssetMemoryUsagePolicy.KeepAsynchronousLoadedBit))
		{
			return;
		}
		ReferenceHolder referenceHolder = base.gameObject.AddComponent<ReferenceHolder>();
		foreach (RoomData room in m_rooms)
		{
			if (room.m_enabled)
			{
				room.m_prefab.Load();
				referenceHolder.HoldReferenceTo(room.m_prefab);
				room.m_prefab.Release();
			}
		}
	}
}
