using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

public class ZDOMan
{
	private class ZDOPeer
	{
		public struct PeerZDOInfo
		{
			public readonly uint m_dataRevision;

			public readonly ushort m_ownerRevision;

			public readonly float m_syncTime;

			public PeerZDOInfo(uint dataRevision, ushort ownerRevision, float syncTime)
			{
				m_dataRevision = dataRevision;
				m_ownerRevision = ownerRevision;
				m_syncTime = syncTime;
			}
		}

		public ZNetPeer m_peer;

		public readonly Dictionary<ZDOID, PeerZDOInfo> m_zdos = new Dictionary<ZDOID, PeerZDOInfo>();

		public readonly HashSet<ZDOID> m_forceSend = new HashSet<ZDOID>();

		public readonly HashSet<ZDOID> m_invalidSector = new HashSet<ZDOID>();

		public int m_sendIndex;

		public void ZDOSectorInvalidated(ZDO zdo)
		{
			if (zdo.GetOwner() != m_peer.m_uid && m_zdos.ContainsKey(zdo.m_uid) && !ZNetScene.InActiveArea(zdo.GetSector(), m_peer.GetRefPos()))
			{
				m_invalidSector.Add(zdo.m_uid);
				m_zdos.Remove(zdo.m_uid);
			}
		}

		public void ForceSendZDO(ZDOID id)
		{
			m_forceSend.Add(id);
		}

		public bool ShouldSend(ZDO zdo)
		{
			if (m_zdos.TryGetValue(zdo.m_uid, out var value))
			{
				if (zdo.OwnerRevision <= value.m_ownerRevision)
				{
					return zdo.DataRevision > value.m_dataRevision;
				}
				return true;
			}
			return true;
		}
	}

	private class SaveData
	{
		public long m_sessionID;

		public uint m_nextUid = 1u;

		public List<ZDO> m_zdos;
	}

	public Action<ZDO> m_onZDODestroyed;

	private readonly long m_sessionID = Utils.GenerateUID();

	private uint m_nextUid = 1u;

	private readonly List<ZDO> m_portalObjects = new List<ZDO>();

	private readonly Dictionary<Vector2i, List<ZDO>> m_objectsByOutsideSector = new Dictionary<Vector2i, List<ZDO>>();

	private readonly List<ZDOPeer> m_peers = new List<ZDOPeer>();

	private readonly Dictionary<ZDOID, long> m_deadZDOs = new Dictionary<ZDOID, long>();

	private readonly List<ZDOID> m_destroySendList = new List<ZDOID>();

	private readonly HashSet<ZDOID> m_clientChangeQueue = new HashSet<ZDOID>();

	private readonly Dictionary<ZDOID, ZDO> m_objectsByID = new Dictionary<ZDOID, ZDO>();

	private List<ZDO>[] m_objectsBySector;

	private readonly int m_width;

	private readonly int m_halfWidth;

	private float m_sendTimer;

	private const float c_SendFPS = 20f;

	private float m_releaseZDOTimer;

	private int m_zdosSent;

	private int m_zdosRecv;

	private int m_zdosSentLastSec;

	private int m_zdosRecvLastSec;

	private float m_statTimer;

	private SaveData m_saveData;

	private int m_nextSendPeer = -1;

	private readonly List<ZDO> m_tempToSync = new List<ZDO>();

	private readonly List<ZDO> m_tempToSyncDistant = new List<ZDO>();

	private readonly List<ZDO> m_tempNearObjects = new List<ZDO>();

	private readonly List<ZDOID> m_tempRemoveList = new List<ZDOID>();

	private readonly List<ZDO> m_tempSectorObjects = new List<ZDO>();

	private static ZDOMan s_instance;

	private static long s_compareReceiver = 0L;

	private static readonly List<int> s_brokenPrefabsToFilterOut = new List<int> { 1332933305, -1334479845 };

	public static ZDOMan instance => s_instance;

	public ZDOMan(int width)
	{
		s_instance = this;
		ZRoutedRpc.instance.Register<ZPackage>("DestroyZDO", RPC_DestroyZDO);
		ZRoutedRpc.instance.Register<ZDOID>("RequestZDO", RPC_RequestZDO);
		m_width = width;
		m_halfWidth = m_width / 2;
		ResetSectorArray();
	}

	private void ResetSectorArray()
	{
		m_objectsBySector = new List<ZDO>[m_width * m_width];
		m_objectsByOutsideSector.Clear();
	}

	public void ShutDown()
	{
		if (!ZNet.instance.IsServer())
		{
			FlushClientObjects();
		}
		ZDOPool.Release(m_objectsByID);
		m_objectsByID.Clear();
		m_tempToSync.Clear();
		m_tempToSyncDistant.Clear();
		m_tempNearObjects.Clear();
		m_tempRemoveList.Clear();
		m_peers.Clear();
		ResetSectorArray();
		Game.instance.CollectResources();
	}

	public void PrepareSave()
	{
		m_saveData = new SaveData();
		m_saveData.m_sessionID = m_sessionID;
		m_saveData.m_nextUid = m_nextUid;
		Stopwatch stopwatch = Stopwatch.StartNew();
		m_saveData.m_zdos = GetSaveClone();
		ZLog.Log("PrepareSave: clone done in " + stopwatch.ElapsedMilliseconds + "ms");
		stopwatch = Stopwatch.StartNew();
		ZDOExtraData.PrepareSave();
		ZLog.Log("PrepareSave: ZDOExtraData.PrepareSave done in " + stopwatch.ElapsedMilliseconds + " ms");
	}

	public void SaveAsync(BinaryWriter writer)
	{
		writer.Write(m_saveData.m_sessionID);
		writer.Write(m_saveData.m_nextUid);
		ZPackage zPackage = new ZPackage();
		writer.Write(m_saveData.m_zdos.Count);
		zPackage.SetWriter(writer);
		foreach (ZDO zdo in m_saveData.m_zdos)
		{
			zdo.Save(zPackage);
		}
		ZLog.Log("Saved " + m_saveData.m_zdos.Count + " ZDOs");
		foreach (ZDO zdo2 in m_saveData.m_zdos)
		{
			zdo2.Reset();
		}
		m_saveData.m_zdos.Clear();
		m_saveData = null;
		ZDOExtraData.ClearSave();
	}

	private void FilterZDO(ZDO zdo, ref List<ZDO> zdos, ref List<ZDO> warningZDOs, ref List<ZDO> brokenZDOs)
	{
		if (s_brokenPrefabsToFilterOut.Contains(zdo.GetPrefab()))
		{
			brokenZDOs.Add(zdo);
			return;
		}
		if (!ZNetScene.instance.HasPrefab(zdo.GetPrefab()))
		{
			warningZDOs.Add(zdo);
		}
		zdos.Add(zdo);
	}

	private void WarnAndRemoveBrokenZDOs(List<ZDO> warningZDOs, List<ZDO> brokenZDOs, int totalNumZDOs, int numZDOs)
	{
		int num2 = default(int);
		int num;
		if (warningZDOs.Count > 0)
		{
			num = warningZDOs.Count;
			ZLog.LogWarning("Found " + num + " ZDOs with unknown prefabs. Will load anyway.");
			Dictionary<int, int> dictionary = new Dictionary<int, int>();
			foreach (ZDO warningZDO in warningZDOs)
			{
				int prefab = warningZDO.GetPrefab();
				if (!dictionary.TryAdd(prefab, 1))
				{
					num = prefab;
					num2 = dictionary[num]++;
				}
			}
			foreach (KeyValuePair<int, int> item in dictionary)
			{
				item.Deconstruct(ref num2, ref num);
				int num3 = num2;
				int num4 = num;
				ZLog.LogWarning("    Hash " + num3 + " appeared " + num4 + " times.");
			}
		}
		if (brokenZDOs.Count <= 0)
		{
			return;
		}
		string[] obj = new string[7] { "Found ", null, null, null, null, null, null };
		num = brokenZDOs.Count;
		obj[1] = num.ToString();
		obj[2] = " ZDOs with prefabs not supported. Removing. ";
		obj[3] = totalNumZDOs.ToString();
		obj[4] = " => ";
		obj[5] = numZDOs.ToString();
		obj[6] = " ZDOs loaded.";
		ZLog.LogError(string.Concat(obj));
		Dictionary<int, int> dictionary2 = new Dictionary<int, int>();
		foreach (ZDO brokenZDO in brokenZDOs)
		{
			int prefab2 = brokenZDO.GetPrefab();
			if (!dictionary2.TryAdd(prefab2, 1))
			{
				num = prefab2;
				num2 = dictionary2[num]++;
			}
			ZDOPool.Release(brokenZDO);
		}
		foreach (KeyValuePair<int, int> item2 in dictionary2)
		{
			item2.Deconstruct(ref num2, ref num);
			int num5 = num2;
			int num6 = num;
			ZLog.LogError("    Hash " + num5 + " filtered out " + num6 + " times.");
		}
	}

	public void Load(BinaryReader reader, int version)
	{
		reader.ReadInt64();
		uint nextUid = reader.ReadUInt32();
		int num = reader.ReadInt32();
		ZDOPool.Release(m_objectsByID);
		m_objectsByID.Clear();
		ResetSectorArray();
		ZDOExtraData.Init();
		ZLog.Log("Loading " + num + " zdos, my sessionID: " + m_sessionID + ", data version: " + version);
		List<ZDO> zdos = new List<ZDO>();
		zdos.Capacity = num;
		_ = ZNetScene.instance;
		List<ZDO> brokenZDOs = new List<ZDO>();
		List<ZDO> warningZDOs = new List<ZDO>();
		ZLog.Log("Loading in ZDOs");
		ZPackage zPackage = new ZPackage();
		if (version < 31)
		{
			for (int i = 0; i < num; i++)
			{
				ZDO zDO = ZDOPool.Create();
				zDO.m_uid = new ZDOID(reader);
				int count = reader.ReadInt32();
				byte[] data = reader.ReadBytes(count);
				zPackage.Load(data);
				zDO.LoadOldFormat(zPackage, version);
				zDO.SetOwner(0L);
				FilterZDO(zDO, ref zdos, ref warningZDOs, ref brokenZDOs);
			}
		}
		else
		{
			zPackage.SetReader(reader);
			for (int j = 0; j < num; j++)
			{
				ZDO zDO2 = ZDOPool.Create();
				zDO2.Load(zPackage, version);
				FilterZDO(zDO2, ref zdos, ref warningZDOs, ref brokenZDOs);
			}
			nextUid = (uint)(zdos.Count + 1);
		}
		WarnAndRemoveBrokenZDOs(warningZDOs, brokenZDOs, num, zdos.Count);
		ZLog.Log("Adding to Dictionary");
		foreach (ZDO item in zdos)
		{
			m_objectsByID.Add(item.m_uid, item);
			if (Game.instance.PortalPrefabHash.Contains(item.GetPrefab()))
			{
				m_portalObjects.Add(item);
			}
		}
		ZLog.Log("Adding to Sectors");
		foreach (ZDO item2 in zdos)
		{
			AddToSector(item2, item2.GetSector());
		}
		if (version < 31)
		{
			ZLog.Log("Converting Ships & Fishing-rods ownership");
			ConvertOwnerships(zdos);
			ZLog.Log("Converting & mapping CreationTime");
			ConvertCreationTime(zdos);
			ZLog.Log("Converting portals");
			ConvertPortals();
			ZLog.Log("Converting spawners");
			ConvertSpawners();
			ZLog.Log("Converting ZSyncTransforms");
			ConvertSyncTransforms();
			ZLog.Log("Converting ItemSeeds");
			ConvertSeed();
			ZLog.Log("Converting Dungeons");
			ConvertDungeonRooms(zdos);
		}
		else
		{
			ZLog.Log("Connecting Portals, Spawners & ZSyncTransforms");
			ConnectPortals();
			ConnectSpawners();
			ConnectSyncTransforms();
		}
		Game.instance.ConnectPortals();
		m_deadZDOs.Clear();
		if (version < 31)
		{
			int num2 = reader.ReadInt32();
			for (int k = 0; k < num2; k++)
			{
				reader.ReadInt64();
				reader.ReadUInt32();
				reader.ReadInt64();
			}
		}
		m_nextUid = nextUid;
	}

	public ZDO CreateNewZDO(Vector3 position, int prefabHash)
	{
		ZDOID zDOID = new ZDOID(m_sessionID, m_nextUid++);
		while (GetZDO(zDOID) != null)
		{
			zDOID = new ZDOID(m_sessionID, m_nextUid++);
		}
		return CreateNewZDO(zDOID, position, prefabHash);
	}

	private ZDO CreateNewZDO(ZDOID uid, Vector3 position, int prefabHashIn = 0)
	{
		ZDO zDO = ZDOPool.Create(uid, position);
		zDO.SetOwnerInternal(m_sessionID);
		m_objectsByID.Add(uid, zDO);
		int item = ((prefabHashIn != 0) ? prefabHashIn : zDO.GetPrefab());
		if (Game.instance.PortalPrefabHash.Contains(item))
		{
			m_portalObjects.Add(zDO);
		}
		return zDO;
	}

	public void AddToSector(ZDO zdo, Vector2i sector)
	{
		int num = SectorToIndex(sector);
		List<ZDO> value;
		if (num >= 0)
		{
			if (m_objectsBySector[num] != null)
			{
				m_objectsBySector[num].Add(zdo);
				return;
			}
			List<ZDO> list = new List<ZDO>();
			list.Add(zdo);
			m_objectsBySector[num] = list;
		}
		else if (m_objectsByOutsideSector.TryGetValue(sector, out value))
		{
			value.Add(zdo);
		}
		else
		{
			value = new List<ZDO>();
			value.Add(zdo);
			m_objectsByOutsideSector.Add(sector, value);
		}
	}

	public void ZDOSectorInvalidated(ZDO zdo)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			peer.ZDOSectorInvalidated(zdo);
		}
	}

	public void RemoveFromSector(ZDO zdo, Vector2i sector)
	{
		int num = SectorToIndex(sector);
		List<ZDO> value;
		if (num >= 0)
		{
			if (m_objectsBySector[num] != null)
			{
				m_objectsBySector[num].Remove(zdo);
			}
		}
		else if (m_objectsByOutsideSector.TryGetValue(sector, out value))
		{
			value.Remove(zdo);
		}
	}

	public ZDO GetZDO(ZDOID id)
	{
		if (id == ZDOID.None)
		{
			return null;
		}
		if (m_objectsByID.TryGetValue(id, out var value))
		{
			return value;
		}
		return null;
	}

	public void AddPeer(ZNetPeer netPeer)
	{
		ZDOPeer zDOPeer = new ZDOPeer();
		zDOPeer.m_peer = netPeer;
		m_peers.Add(zDOPeer);
		zDOPeer.m_peer.m_rpc.Register<ZPackage>("ZDOData", RPC_ZDOData);
	}

	public void RemovePeer(ZNetPeer netPeer)
	{
		ZDOPeer zDOPeer = FindPeer(netPeer);
		if (zDOPeer != null)
		{
			m_peers.Remove(zDOPeer);
			if (ZNet.instance.IsServer())
			{
				RemoveOrphanNonPersistentZDOS();
			}
		}
	}

	private ZDOPeer FindPeer(ZNetPeer netPeer)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer == netPeer)
			{
				return peer;
			}
		}
		return null;
	}

	private ZDOPeer FindPeer(ZRpc rpc)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer.m_rpc == rpc)
			{
				return peer;
			}
		}
		return null;
	}

	public void Update(float dt)
	{
		if (ZNet.instance.IsServer())
		{
			ReleaseZDOS(dt);
		}
		SendZDOToPeers2(dt);
		SendDestroyed();
		UpdateStats(dt);
	}

	private void UpdateStats(float dt)
	{
		m_statTimer += dt;
		if (m_statTimer >= 1f)
		{
			m_statTimer = 0f;
			m_zdosSentLastSec = m_zdosSent;
			m_zdosRecvLastSec = m_zdosRecv;
			m_zdosRecv = 0;
			m_zdosSent = 0;
		}
	}

	private void SendZDOToPeers2(float dt)
	{
		if (m_peers.Count == 0)
		{
			return;
		}
		m_sendTimer += dt;
		if (m_nextSendPeer < 0)
		{
			if (m_sendTimer > 0.05f)
			{
				m_nextSendPeer = 0;
				m_sendTimer = 0f;
			}
			return;
		}
		if (m_nextSendPeer < m_peers.Count)
		{
			SendZDOs(m_peers[m_nextSendPeer], flush: false);
		}
		m_nextSendPeer++;
		if (m_nextSendPeer >= m_peers.Count)
		{
			m_nextSendPeer = -1;
		}
	}

	private void FlushClientObjects()
	{
		foreach (ZDOPeer peer in m_peers)
		{
			SendAllZDOs(peer);
		}
	}

	private void ReleaseZDOS(float dt)
	{
		m_releaseZDOTimer += dt;
		if (!(m_releaseZDOTimer > 2f))
		{
			return;
		}
		m_releaseZDOTimer = 0f;
		ReleaseNearbyZDOS(ZNet.instance.GetReferencePosition(), m_sessionID);
		foreach (ZDOPeer peer in m_peers)
		{
			ReleaseNearbyZDOS(peer.m_peer.m_refPos, peer.m_peer.m_uid);
		}
	}

	private bool IsInPeerActiveArea(Vector2i sector, long uid)
	{
		if (uid == m_sessionID)
		{
			return ZNetScene.InActiveArea(sector, ZNet.instance.GetReferencePosition());
		}
		ZNetPeer peer = ZNet.instance.GetPeer(uid);
		if (peer == null)
		{
			return false;
		}
		return ZNetScene.InActiveArea(sector, peer.GetRefPos());
	}

	private void ReleaseNearbyZDOS(Vector3 refPosition, long uid)
	{
		Vector2i zone = ZoneSystem.instance.GetZone(refPosition);
		m_tempNearObjects.Clear();
		FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, 0, m_tempNearObjects);
		foreach (ZDO tempNearObject in m_tempNearObjects)
		{
			if (!tempNearObject.Persistent)
			{
				continue;
			}
			if (tempNearObject.GetOwner() == uid)
			{
				if (!ZNetScene.InActiveArea(tempNearObject.GetSector(), zone))
				{
					tempNearObject.SetOwner(0L);
				}
			}
			else if ((!tempNearObject.HasOwner() || !IsInPeerActiveArea(tempNearObject.GetSector(), tempNearObject.GetOwner())) && ZNetScene.InActiveArea(tempNearObject.GetSector(), zone))
			{
				tempNearObject.SetOwner(uid);
			}
		}
	}

	public void DestroyZDO(ZDO zdo)
	{
		if (zdo.IsOwner())
		{
			m_destroySendList.Add(zdo.m_uid);
		}
	}

	private void SendDestroyed()
	{
		if (m_destroySendList.Count == 0)
		{
			return;
		}
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_destroySendList.Count);
		foreach (ZDOID destroySend in m_destroySendList)
		{
			zPackage.Write(destroySend);
		}
		m_destroySendList.Clear();
		ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, "DestroyZDO", zPackage);
	}

	private void RPC_DestroyZDO(long sender, ZPackage pkg)
	{
		int num = pkg.ReadInt();
		for (int i = 0; i < num; i++)
		{
			ZDOID uid = pkg.ReadZDOID();
			HandleDestroyedZDO(uid);
		}
	}

	private void HandleDestroyedZDO(ZDOID uid)
	{
		if (uid.UserID == m_sessionID && uid.ID >= m_nextUid)
		{
			m_nextUid = uid.ID + 1;
		}
		ZDO zDO = GetZDO(uid);
		if (zDO == null)
		{
			return;
		}
		if (m_onZDODestroyed != null)
		{
			m_onZDODestroyed(zDO);
		}
		RemoveFromSector(zDO, zDO.GetSector());
		m_objectsByID.Remove(zDO.m_uid);
		if (Game.instance.PortalPrefabHash.Contains(zDO.GetPrefab()))
		{
			m_portalObjects.Remove(zDO);
		}
		ZDOPool.Release(zDO);
		foreach (ZDOPeer peer in m_peers)
		{
			peer.m_zdos.Remove(uid);
		}
		if (ZNet.instance.IsServer())
		{
			long ticks = ZNet.instance.GetTime().Ticks;
			m_deadZDOs[uid] = ticks;
		}
	}

	private void SendAllZDOs(ZDOPeer peer)
	{
		while (SendZDOs(peer, flush: true))
		{
		}
	}

	private bool SendZDOs(ZDOPeer peer, bool flush)
	{
		int sendQueueSize = peer.m_peer.m_socket.GetSendQueueSize();
		if (!flush && sendQueueSize > 10240)
		{
			return false;
		}
		int num = 10240 - sendQueueSize;
		if (num < 2048)
		{
			return false;
		}
		m_tempToSync.Clear();
		CreateSyncList(peer, m_tempToSync);
		if (m_tempToSync.Count == 0 && peer.m_invalidSector.Count == 0)
		{
			return false;
		}
		ZPackage zPackage = new ZPackage();
		bool flag = false;
		if (peer.m_invalidSector.Count > 0)
		{
			flag = true;
			zPackage.Write(peer.m_invalidSector.Count);
			foreach (ZDOID item in peer.m_invalidSector)
			{
				zPackage.Write(item);
			}
			peer.m_invalidSector.Clear();
		}
		else
		{
			zPackage.Write(0);
		}
		float time = Time.time;
		ZPackage zPackage2 = new ZPackage();
		bool flag2 = false;
		foreach (ZDO item2 in m_tempToSync)
		{
			if (zPackage.Size() > num)
			{
				break;
			}
			peer.m_forceSend.Remove(item2.m_uid);
			if (!ZNet.instance.IsServer())
			{
				m_clientChangeQueue.Remove(item2.m_uid);
			}
			zPackage.Write(item2.m_uid);
			zPackage.Write(item2.OwnerRevision);
			zPackage.Write(item2.DataRevision);
			zPackage.Write(item2.GetOwner());
			zPackage.Write(item2.GetPosition());
			zPackage2.Clear();
			item2.Serialize(zPackage2);
			zPackage.Write(zPackage2);
			peer.m_zdos[item2.m_uid] = new ZDOPeer.PeerZDOInfo(item2.DataRevision, item2.OwnerRevision, time);
			flag2 = true;
			m_zdosSent++;
		}
		zPackage.Write(ZDOID.None);
		if (flag2 || flag)
		{
			peer.m_peer.m_rpc.Invoke("ZDOData", zPackage);
		}
		return flag2 || flag;
	}

	private void RPC_ZDOData(ZRpc rpc, ZPackage pkg)
	{
		ZDOPeer zDOPeer = FindPeer(rpc);
		if (zDOPeer == null)
		{
			ZLog.Log("ZDO data from unkown host, ignoring");
			return;
		}
		float time = Time.time;
		int num = 0;
		ZPackage pkg2 = new ZPackage();
		int num2 = pkg.ReadInt();
		for (int i = 0; i < num2; i++)
		{
			ZDOID id = pkg.ReadZDOID();
			GetZDO(id)?.InvalidateSector();
		}
		while (true)
		{
			ZDOID zDOID = pkg.ReadZDOID();
			if (zDOID.IsNone())
			{
				break;
			}
			num++;
			ushort num3 = pkg.ReadUShort();
			uint num4 = pkg.ReadUInt();
			long ownerInternal = pkg.ReadLong();
			Vector3 vector = pkg.ReadVector3();
			pkg.ReadPackage(ref pkg2);
			ZDO zDO = GetZDO(zDOID);
			bool flag = false;
			if (zDO != null)
			{
				if (num4 <= zDO.DataRevision)
				{
					if (num3 > zDO.OwnerRevision)
					{
						zDO.SetOwnerInternal(ownerInternal);
						zDO.OwnerRevision = num3;
						zDOPeer.m_zdos[zDOID] = new ZDOPeer.PeerZDOInfo(num4, num3, time);
					}
					continue;
				}
			}
			else
			{
				zDO = CreateNewZDO(zDOID, vector);
				flag = true;
			}
			zDO.OwnerRevision = num3;
			zDO.DataRevision = num4;
			zDO.SetOwnerInternal(ownerInternal);
			zDO.InternalSetPosition(vector);
			zDOPeer.m_zdos[zDOID] = new ZDOPeer.PeerZDOInfo(zDO.DataRevision, zDO.OwnerRevision, time);
			zDO.Deserialize(pkg2);
			if (Game.instance.PortalPrefabHash.Contains(zDO.GetPrefab()))
			{
				AddPortal(zDO);
			}
			if (ZNet.instance.IsServer() && flag && m_deadZDOs.ContainsKey(zDOID))
			{
				zDO.SetOwner(m_sessionID);
				DestroyZDO(zDO);
			}
		}
		m_zdosRecv += num;
	}

	public void FindSectorObjects(Vector2i sector, int area, int distantArea, List<ZDO> sectorObjects, List<ZDO> distantSectorObjects = null)
	{
		FindObjects(sector, sectorObjects);
		for (int i = 1; i <= area; i++)
		{
			for (int j = sector.x - i; j <= sector.x + i; j++)
			{
				FindObjects(new Vector2i(j, sector.y - i), sectorObjects);
				FindObjects(new Vector2i(j, sector.y + i), sectorObjects);
			}
			for (int k = sector.y - i + 1; k <= sector.y + i - 1; k++)
			{
				FindObjects(new Vector2i(sector.x - i, k), sectorObjects);
				FindObjects(new Vector2i(sector.x + i, k), sectorObjects);
			}
		}
		List<ZDO> objects = distantSectorObjects ?? sectorObjects;
		for (int l = area + 1; l <= area + distantArea; l++)
		{
			for (int m = sector.x - l; m <= sector.x + l; m++)
			{
				FindDistantObjects(new Vector2i(m, sector.y - l), objects);
				FindDistantObjects(new Vector2i(m, sector.y + l), objects);
			}
			for (int n = sector.y - l + 1; n <= sector.y + l - 1; n++)
			{
				FindDistantObjects(new Vector2i(sector.x - l, n), objects);
				FindDistantObjects(new Vector2i(sector.x + l, n), objects);
			}
		}
	}

	private void CreateSyncList(ZDOPeer peer, List<ZDO> toSync)
	{
		if (ZNet.instance.IsServer())
		{
			Vector3 refPos = peer.m_peer.GetRefPos();
			Vector2i zone = ZoneSystem.instance.GetZone(refPos);
			m_tempSectorObjects.Clear();
			m_tempToSyncDistant.Clear();
			FindSectorObjects(zone, ZoneSystem.instance.m_activeArea, ZoneSystem.instance.m_activeDistantArea, m_tempSectorObjects, m_tempToSyncDistant);
			foreach (ZDO tempSectorObject in m_tempSectorObjects)
			{
				if (peer.ShouldSend(tempSectorObject))
				{
					toSync.Add(tempSectorObject);
				}
			}
			ServerSortSendZDOS(toSync, refPos, peer);
			if (toSync.Count < 10)
			{
				foreach (ZDO item in m_tempToSyncDistant)
				{
					if (peer.ShouldSend(item))
					{
						toSync.Add(item);
					}
				}
			}
			AddForceSendZdos(peer, toSync);
			return;
		}
		m_tempRemoveList.Clear();
		foreach (ZDOID item2 in m_clientChangeQueue)
		{
			ZDO zDO = GetZDO(item2);
			if (zDO != null && peer.ShouldSend(zDO))
			{
				toSync.Add(zDO);
			}
			else
			{
				m_tempRemoveList.Add(item2);
			}
		}
		foreach (ZDOID tempRemove in m_tempRemoveList)
		{
			m_clientChangeQueue.Remove(tempRemove);
		}
		ClientSortSendZDOS(toSync, peer);
		AddForceSendZdos(peer, toSync);
	}

	private void AddForceSendZdos(ZDOPeer peer, List<ZDO> syncList)
	{
		if (peer.m_forceSend.Count <= 0)
		{
			return;
		}
		m_tempRemoveList.Clear();
		foreach (ZDOID item in peer.m_forceSend)
		{
			ZDO zDO = GetZDO(item);
			if (zDO != null && peer.ShouldSend(zDO))
			{
				syncList.Insert(0, zDO);
			}
			else
			{
				m_tempRemoveList.Add(item);
			}
		}
		foreach (ZDOID tempRemove in m_tempRemoveList)
		{
			peer.m_forceSend.Remove(tempRemove);
		}
	}

	private static int ServerSendCompare(ZDO x, ZDO y)
	{
		bool flag = x.Type == ZDO.ObjectType.Prioritized && x.HasOwner() && x.GetOwner() != s_compareReceiver;
		bool flag2 = y.Type == ZDO.ObjectType.Prioritized && y.HasOwner() && y.GetOwner() != s_compareReceiver;
		if (flag && flag2)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		if (flag != flag2)
		{
			if (!flag)
			{
				return 1;
			}
			return -1;
		}
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		return ((int)y.Type).CompareTo((int)x.Type);
	}

	private void ServerSortSendZDOS(List<ZDO> objects, Vector3 refPos, ZDOPeer peer)
	{
		float time = Time.time;
		foreach (ZDO @object in objects)
		{
			Vector3 position = @object.GetPosition();
			@object.m_tempSortValue = Vector3.Distance(position, refPos);
			float num = 100f;
			if (peer.m_zdos.TryGetValue(@object.m_uid, out var value))
			{
				num = Mathf.Clamp(time - value.m_syncTime, 0f, 100f);
			}
			@object.m_tempSortValue -= num * 1.5f;
		}
		s_compareReceiver = peer.m_peer.m_uid;
		objects.Sort(ServerSendCompare);
	}

	private static int ClientSendCompare(ZDO x, ZDO y)
	{
		if (x.Type == y.Type)
		{
			return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
		}
		if (x.Type == ZDO.ObjectType.Prioritized)
		{
			return -1;
		}
		if (y.Type == ZDO.ObjectType.Prioritized)
		{
			return 1;
		}
		return Utils.CompareFloats(x.m_tempSortValue, y.m_tempSortValue);
	}

	private void ClientSortSendZDOS(List<ZDO> objects, ZDOPeer peer)
	{
		float time = Time.time;
		foreach (ZDO @object in objects)
		{
			@object.m_tempSortValue = 0f;
			float num = 100f;
			if (peer.m_zdos.TryGetValue(@object.m_uid, out var value))
			{
				num = Mathf.Clamp(time - value.m_syncTime, 0f, 100f);
			}
			@object.m_tempSortValue -= num * 1.5f;
		}
		objects.Sort(ClientSendCompare);
	}

	private void AddDistantObjects(ZDOPeer peer, int maxItems, List<ZDO> toSync)
	{
		if (peer.m_sendIndex >= m_objectsByID.Count)
		{
			peer.m_sendIndex = 0;
		}
		IEnumerable<KeyValuePair<ZDOID, ZDO>> enumerable = m_objectsByID.Skip(peer.m_sendIndex).Take(maxItems);
		peer.m_sendIndex += maxItems;
		foreach (KeyValuePair<ZDOID, ZDO> item in enumerable)
		{
			toSync.Add(item.Value);
		}
	}

	public static long GetSessionID()
	{
		return s_instance.m_sessionID;
	}

	private int SectorToIndex(Vector2i s)
	{
		int num = s.x + m_halfWidth;
		int num2 = s.y + m_halfWidth;
		if (num < 0 || num2 < 0 || num >= m_width || num2 >= m_width)
		{
			return -1;
		}
		return num2 * m_width + num;
	}

	private void FindObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = SectorToIndex(sector);
		List<ZDO> value;
		if (num >= 0)
		{
			if (m_objectsBySector[num] != null)
			{
				objects.AddRange(m_objectsBySector[num]);
			}
		}
		else if (m_objectsByOutsideSector.TryGetValue(sector, out value))
		{
			objects.AddRange(value);
		}
	}

	private void FindDistantObjects(Vector2i sector, List<ZDO> objects)
	{
		int num = SectorToIndex(sector);
		if (num >= 0)
		{
			List<ZDO> list = m_objectsBySector[num];
			if (list == null)
			{
				return;
			}
			{
				foreach (ZDO item in list)
				{
					if (item.Distant)
					{
						objects.Add(item);
					}
				}
				return;
			}
		}
		if (!m_objectsByOutsideSector.TryGetValue(sector, out var value))
		{
			return;
		}
		foreach (ZDO item2 in value)
		{
			if (item2.Distant)
			{
				objects.Add(item2);
			}
		}
	}

	private void RemoveOrphanNonPersistentZDOS()
	{
		foreach (KeyValuePair<ZDOID, ZDO> item in m_objectsByID)
		{
			ZDO value = item.Value;
			if (!value.Persistent && (!value.HasOwner() || !IsPeerConnected(value.GetOwner())))
			{
				ZDOID uid = value.m_uid;
				ZLog.Log("Destroying abandoned non persistent zdo " + uid.ToString() + " owner " + value.GetOwner());
				value.SetOwner(m_sessionID);
				DestroyZDO(value);
			}
		}
	}

	private bool IsPeerConnected(long uid)
	{
		if (m_sessionID == uid)
		{
			return true;
		}
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer.m_uid == uid)
			{
				return true;
			}
		}
		return false;
	}

	private static bool InvalidZDO(ZDO zdo)
	{
		return !zdo.IsValid();
	}

	public bool GetAllZDOsWithPrefabIterative(string prefab, List<ZDO> zdos, ref int index)
	{
		int stableHashCode = prefab.GetStableHashCode();
		if (index >= m_objectsBySector.Length)
		{
			foreach (List<ZDO> value in m_objectsByOutsideSector.Values)
			{
				foreach (ZDO item in value)
				{
					if (item.GetPrefab() == stableHashCode)
					{
						zdos.Add(item);
					}
				}
			}
			zdos.RemoveAll(InvalidZDO);
			return true;
		}
		int num = 0;
		while (index < m_objectsBySector.Length)
		{
			List<ZDO> list = m_objectsBySector[index];
			if (list != null)
			{
				foreach (ZDO item2 in list)
				{
					if (item2.GetPrefab() == stableHashCode)
					{
						zdos.Add(item2);
					}
				}
				num++;
				if (num > 400)
				{
					break;
				}
			}
			index++;
		}
		return false;
	}

	private List<ZDO> GetSaveClone()
	{
		List<ZDO> list = new List<ZDO>();
		for (int i = 0; i < m_objectsBySector.Length; i++)
		{
			if (m_objectsBySector[i] == null)
			{
				continue;
			}
			foreach (ZDO item in m_objectsBySector[i])
			{
				if (item.Persistent)
				{
					list.Add(item.Clone());
				}
			}
		}
		foreach (List<ZDO> value in m_objectsByOutsideSector.Values)
		{
			foreach (ZDO item2 in value)
			{
				if (item2.Persistent)
				{
					list.Add(item2.Clone());
				}
			}
		}
		return list;
	}

	public List<ZDO> GetPortals()
	{
		return m_portalObjects;
	}

	public int NrOfObjects()
	{
		return m_objectsByID.Count;
	}

	public int GetSentZDOs()
	{
		return m_zdosSentLastSec;
	}

	public int GetRecvZDOs()
	{
		return m_zdosRecvLastSec;
	}

	public int GetClientChangeQueue()
	{
		return m_clientChangeQueue.Count;
	}

	public void GetAverageStats(out float sentZdos, out float recvZdos)
	{
		sentZdos = (float)m_zdosSentLastSec / 20f;
		recvZdos = (float)m_zdosRecvLastSec / 20f;
	}

	public void RequestZDO(ZDOID id)
	{
		ZRoutedRpc.instance.InvokeRoutedRPC("RequestZDO", id);
	}

	private void RPC_RequestZDO(long sender, ZDOID id)
	{
		GetPeer(sender)?.ForceSendZDO(id);
	}

	private ZDOPeer GetPeer(long uid)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			if (peer.m_peer.m_uid == uid)
			{
				return peer;
			}
		}
		return null;
	}

	public void ForceSendZDO(ZDOID id)
	{
		foreach (ZDOPeer peer in m_peers)
		{
			peer.ForceSendZDO(id);
		}
	}

	public void ForceSendZDO(long peerID, ZDOID id)
	{
		if (ZNet.instance.IsServer())
		{
			GetPeer(peerID)?.ForceSendZDO(id);
			return;
		}
		foreach (ZDOPeer peer in m_peers)
		{
			peer.ForceSendZDO(id);
		}
	}

	public void ClientChanged(ZDOID id)
	{
		m_clientChangeQueue.Add(id);
	}

	private void AddPortal(ZDO zdo)
	{
		if (!m_portalObjects.Contains(zdo))
		{
			m_portalObjects.Add(zdo);
		}
	}

	private void ConvertOwnerships(List<ZDO> zdos)
	{
		foreach (ZDO zdo in zdos)
		{
			ZDOID zDOID = zdo.GetZDOID(ZDOVars.s_zdoidUser);
			if (zDOID != ZDOID.None)
			{
				zdo.SetOwnerInternal(GetSessionID());
				zdo.Set(ZDOVars.s_user, zDOID.UserID);
			}
			ZDOID zDOID2 = zdo.GetZDOID(ZDOVars.s_zdoidRodOwner);
			if (zDOID2 != ZDOID.None)
			{
				zdo.SetOwnerInternal(GetSessionID());
				zdo.Set(ZDOVars.s_rodOwner, zDOID2.UserID);
			}
		}
	}

	private void ConvertCreationTime(List<ZDO> zdos)
	{
		if (!ZDOExtraData.HasTimeCreated())
		{
			return;
		}
		List<int> list = new List<int>
		{
			"cultivate".GetStableHashCode(),
			"raise".GetStableHashCode(),
			"path".GetStableHashCode(),
			"paved_road".GetStableHashCode(),
			"HeathRockPillar".GetStableHashCode(),
			"HeathRockPillar_frac".GetStableHashCode(),
			"ship_construction".GetStableHashCode(),
			"replant".GetStableHashCode(),
			"digg".GetStableHashCode(),
			"mud_road".GetStableHashCode(),
			"LevelTerrain".GetStableHashCode(),
			"digg_v2".GetStableHashCode()
		};
		int num = 0;
		foreach (ZDO zdo in zdos)
		{
			if (list.Contains(zdo.GetPrefab()))
			{
				num++;
				long timeCreated = ZDOExtraData.GetTimeCreated(zdo.m_uid);
				zdo.SetOwner(GetSessionID());
				zdo.Set(ZDOVars.s_terrainModifierTimeCreated, timeCreated);
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("Converted " + num + " Creation Times.");
		}
	}

	private void ConvertPortals()
	{
		UnityEngine.Debug.Log("ConvertPortals => Make sure all " + m_portalObjects.Count + " portals are in a good state.");
		int num = 0;
		foreach (ZDO portalObject in m_portalObjects)
		{
			string @string = portalObject.GetString(ZDOVars.s_tag);
			ZDOID zDOID = portalObject.GetZDOID(ZDOVars.s_toRemoveTarget);
			portalObject.RemoveZDOID(ZDOVars.s_toRemoveTarget);
			if (zDOID == ZDOID.None)
			{
				continue;
			}
			ZDO zDO = GetZDO(zDOID);
			if (zDO != null)
			{
				ZDOID zDOID2 = zDO.GetZDOID(ZDOVars.s_toRemoveTarget);
				string string2 = zDO.GetString(ZDOVars.s_tag);
				zDO.RemoveZDOID(ZDOVars.s_toRemoveTarget);
				if (@string == string2 && zDOID == zDO.m_uid && zDOID2 == portalObject.m_uid)
				{
					portalObject.SetOwner(GetSessionID());
					zDO.SetOwner(GetSessionID());
					num++;
					portalObject.SetConnection(ZDOExtraData.ConnectionType.Portal, zDO.m_uid);
					zDO.SetConnection(ZDOExtraData.ConnectionType.Portal, portalObject.m_uid);
					instance.ForceSendZDO(portalObject.m_uid);
					instance.ForceSendZDO(zDO.m_uid);
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertPortals => fixed " + num + " portals.");
		}
	}

	private void ConnectPortals()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		foreach (ZDOID item in allConnectionZDOIDs)
		{
			ZDO zDO = GetZDO(item);
			if (zDO == null)
			{
				continue;
			}
			ZDOConnectionHashData connectionHashData = zDO.GetConnectionHashData(ZDOExtraData.ConnectionType.Portal);
			if (connectionHashData == null)
			{
				continue;
			}
			foreach (ZDOID item2 in allConnectionZDOIDs2)
			{
				if (item2 == item || ZDOExtraData.GetConnectionType(item2) != 0)
				{
					continue;
				}
				ZDO zDO2 = GetZDO(item2);
				if (zDO2 != null)
				{
					ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(item2, ZDOExtraData.ConnectionType.Portal | ZDOExtraData.ConnectionType.Target);
					if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
					{
						num++;
						zDO.SetOwner(GetSessionID());
						zDO2.SetOwner(GetSessionID());
						zDO.SetConnection(ZDOExtraData.ConnectionType.Portal, item2);
						zDO2.SetConnection(ZDOExtraData.ConnectionType.Portal, item);
						break;
					}
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConnectPortals => Connected " + num + " portals.");
		}
	}

	private void ConvertSpawners()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long, "spawn_id_u".GetStableHashCode());
		if (allZDOIDsWithHash.Count > 0)
		{
			UnityEngine.Debug.Log("ConvertSpawners => Will try and convert " + allZDOIDsWithHash.Count + " spawners.");
		}
		int num = 0;
		int num2 = 0;
		foreach (ZDO item in allZDOIDsWithHash.Select((ZDOID id) => GetZDO(id)))
		{
			item.SetOwner(GetSessionID());
			ZDOID zDOID = item.GetZDOID(ZDOVars.s_toRemoveSpawnID);
			item.RemoveZDOID(ZDOVars.s_toRemoveSpawnID);
			ZDO zDO = GetZDO(zDOID);
			if (zDO != null)
			{
				num++;
				item.SetConnection(ZDOExtraData.ConnectionType.Spawned, zDO.m_uid);
			}
			else
			{
				num2++;
				item.SetConnection(ZDOExtraData.ConnectionType.Spawned, ZDOID.None);
			}
		}
		if (num > 0 || num2 > 0)
		{
			UnityEngine.Debug.Log("ConvertSpawners => Converted " + num + " spawners, and " + num2 + " 'done' spawners.");
		}
	}

	private void ConnectSpawners()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Spawned);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.Spawned | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		int num2 = 0;
		foreach (ZDOID item in allConnectionZDOIDs)
		{
			ZDO zDO = GetZDO(item);
			if (zDO == null)
			{
				continue;
			}
			zDO.SetOwner(GetSessionID());
			bool flag = false;
			ZDOConnectionHashData connectionHashData = zDO.GetConnectionHashData(ZDOExtraData.ConnectionType.Spawned);
			if (connectionHashData != null)
			{
				foreach (ZDOID item2 in allConnectionZDOIDs2)
				{
					if (!(item2 == item))
					{
						ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(item2, ZDOExtraData.ConnectionType.Spawned | ZDOExtraData.ConnectionType.Target);
						if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
						{
							flag = true;
							num++;
							zDO.SetConnection(ZDOExtraData.ConnectionType.Spawned, item2);
							break;
						}
					}
				}
			}
			if (!flag)
			{
				num2++;
				zDO.SetConnection(ZDOExtraData.ConnectionType.Spawned, ZDOID.None);
			}
		}
		if (num > 0 || num2 > 0)
		{
			UnityEngine.Debug.Log("ConnectSpawners => Connected " + num + " spawners and " + num2 + " 'done' spawners.");
		}
	}

	private void ConvertSyncTransforms()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Long, "parentID_u".GetStableHashCode());
		if (allZDOIDsWithHash.Count > 0)
		{
			UnityEngine.Debug.Log("ConvertSyncTransforms => Will try and convert " + allZDOIDsWithHash.Count + " SyncTransforms.");
		}
		int num = 0;
		foreach (ZDO item in allZDOIDsWithHash.Select(GetZDO))
		{
			item.SetOwner(GetSessionID());
			ZDOID zDOID = item.GetZDOID(ZDOVars.s_toRemoveParentID);
			item.RemoveZDOID(ZDOVars.s_toRemoveParentID);
			ZDO zDO = GetZDO(zDOID);
			if (zDO != null)
			{
				num++;
				item.SetConnection(ZDOExtraData.ConnectionType.SyncTransform, zDO.m_uid);
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertSyncTransforms => Converted " + num + " SyncTransforms.");
		}
	}

	private void ConvertSeed()
	{
		List<ZDOID> allZDOIDsWithHash = ZDOExtraData.GetAllZDOIDsWithHash(ZDOExtraData.Type.Int, ZDOVars.s_leftItem);
		int num = 0;
		foreach (ZDO item in allZDOIDsWithHash.Select(GetZDO))
		{
			num++;
			item.Set(value: item.m_uid.GetHashCode(), hash: ZDOVars.s_seed, okForNotOwner: true);
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConvertSeed => Converted " + num + " ZDOs.");
		}
	}

	private void ConvertDungeonRooms(List<ZDO> zdos)
	{
		MemoryStream memoryStream = new MemoryStream();
		BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
		foreach (ZDO zdo in zdos)
		{
			int @int = zdo.GetInt(ZDOVars.s_rooms);
			zdo.RemoveInt(ZDOVars.s_rooms);
			if (@int != 0)
			{
				memoryStream.SetLength(0L);
				binaryWriter.Write(@int);
				for (int i = 0; i < @int; i++)
				{
					string text = "room" + i;
					int int2 = zdo.GetInt(text);
					Vector3 vec = zdo.GetVec3(text + "_pos", Vector3.zero);
					Quaternion quaternion = zdo.GetQuaternion(text + "_rot", Quaternion.identity);
					zdo.RemoveInt(text);
					zdo.RemoveVec3(text + "_pos");
					zdo.RemoveQuaternion(text + "_rot");
					zdo.RemoveInt(text + "_seed");
					binaryWriter.Write(int2);
					binaryWriter.Write(vec);
					binaryWriter.Write(quaternion);
				}
				zdo.Set(ZDOVars.s_roomData, memoryStream.ToArray());
				ZLog.Log($"Cleaned up old dungeon data format for {@int} rooms.");
			}
		}
	}

	private void ConnectSyncTransforms()
	{
		List<ZDOID> allConnectionZDOIDs = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.SyncTransform);
		List<ZDOID> allConnectionZDOIDs2 = ZDOExtraData.GetAllConnectionZDOIDs(ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
		int num = 0;
		foreach (ZDOID item in allConnectionZDOIDs)
		{
			ZDOConnectionHashData connectionHashData = ZDOExtraData.GetConnectionHashData(item, ZDOExtraData.ConnectionType.SyncTransform);
			if (connectionHashData == null)
			{
				continue;
			}
			foreach (ZDOID item2 in allConnectionZDOIDs2)
			{
				ZDOConnectionHashData connectionHashData2 = ZDOExtraData.GetConnectionHashData(item2, ZDOExtraData.ConnectionType.SyncTransform | ZDOExtraData.ConnectionType.Target);
				if (connectionHashData2 != null && connectionHashData.m_hash == connectionHashData2.m_hash)
				{
					num++;
					ZDOExtraData.SetConnection(item, ZDOExtraData.ConnectionType.SyncTransform, item2);
					break;
				}
			}
		}
		if (num > 0)
		{
			UnityEngine.Debug.Log("ConnectSyncTransforms => Connected " + num + " SyncTransforms.");
		}
	}
}
