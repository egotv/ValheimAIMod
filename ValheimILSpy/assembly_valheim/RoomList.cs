using System.Collections.Generic;
using UnityEngine;

public class RoomList : MonoBehaviour
{
	private static List<RoomList> s_allRoomLists = new List<RoomList>();

	public List<DungeonDB.RoomData> m_rooms = new List<DungeonDB.RoomData>();

	private void Awake()
	{
		s_allRoomLists.Add(this);
	}

	private void OnDestroy()
	{
		s_allRoomLists.Remove(this);
	}

	public static List<RoomList> GetAllRoomLists()
	{
		return s_allRoomLists;
	}
}
