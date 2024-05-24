using System;
using System.Collections.Generic;

public class ZRoutedRpc
{
	public class RoutedRPCData
	{
		public long m_msgID;

		public long m_senderPeerID;

		public long m_targetPeerID;

		public ZDOID m_targetZDO;

		public int m_methodHash;

		public ZPackage m_parameters = new ZPackage();

		public void Serialize(ZPackage pkg)
		{
			pkg.Write(m_msgID);
			pkg.Write(m_senderPeerID);
			pkg.Write(m_targetPeerID);
			pkg.Write(m_targetZDO);
			pkg.Write(m_methodHash);
			pkg.Write(m_parameters);
		}

		public void Deserialize(ZPackage pkg)
		{
			m_msgID = pkg.ReadLong();
			m_senderPeerID = pkg.ReadLong();
			m_targetPeerID = pkg.ReadLong();
			m_targetZDO = pkg.ReadZDOID();
			m_methodHash = pkg.ReadInt();
			m_parameters = pkg.ReadPackage();
		}
	}

	public static long Everybody;

	public Action<long> m_onNewPeer;

	private int m_rpcMsgID = 1;

	private bool m_server;

	private long m_id;

	private readonly List<ZNetPeer> m_peers = new List<ZNetPeer>();

	private readonly Dictionary<int, RoutedMethodBase> m_functions = new Dictionary<int, RoutedMethodBase>();

	private static ZRoutedRpc s_instance;

	public static ZRoutedRpc instance => s_instance;

	public ZRoutedRpc(bool server)
	{
		s_instance = this;
		m_server = server;
	}

	public void SetUID(long uid)
	{
		m_id = uid;
	}

	public void AddPeer(ZNetPeer peer)
	{
		m_peers.Add(peer);
		peer.m_rpc.Register<ZPackage>("RoutedRPC", RPC_RoutedRPC);
		if (m_onNewPeer != null)
		{
			m_onNewPeer(peer.m_uid);
		}
	}

	public void RemovePeer(ZNetPeer peer)
	{
		m_peers.Remove(peer);
	}

	private ZNetPeer GetPeer(long uid)
	{
		foreach (ZNetPeer peer in m_peers)
		{
			if (peer.m_uid == uid)
			{
				return peer;
			}
		}
		return null;
	}

	public void InvokeRoutedRPC(long targetPeerID, string methodName, params object[] parameters)
	{
		InvokeRoutedRPC(targetPeerID, ZDOID.None, methodName, parameters);
	}

	public void InvokeRoutedRPC(string methodName, params object[] parameters)
	{
		InvokeRoutedRPC(GetServerPeerID(), methodName, parameters);
	}

	private long GetServerPeerID()
	{
		if (m_server)
		{
			return m_id;
		}
		if (m_peers.Count > 0)
		{
			return m_peers[0].m_uid;
		}
		return 0L;
	}

	public void InvokeRoutedRPC(long targetPeerID, ZDOID targetZDO, string methodName, params object[] parameters)
	{
		RoutedRPCData routedRPCData = new RoutedRPCData();
		routedRPCData.m_msgID = m_id + m_rpcMsgID++;
		routedRPCData.m_senderPeerID = m_id;
		routedRPCData.m_targetPeerID = targetPeerID;
		routedRPCData.m_targetZDO = targetZDO;
		routedRPCData.m_methodHash = methodName.GetStableHashCode();
		ZRpc.Serialize(parameters, ref routedRPCData.m_parameters);
		routedRPCData.m_parameters.SetPos(0);
		if (targetPeerID == m_id || targetPeerID == 0L)
		{
			HandleRoutedRPC(routedRPCData);
		}
		if (targetPeerID != m_id)
		{
			RouteRPC(routedRPCData);
		}
	}

	private void RouteRPC(RoutedRPCData rpcData)
	{
		ZPackage zPackage = new ZPackage();
		rpcData.Serialize(zPackage);
		if (m_server)
		{
			if (rpcData.m_targetPeerID != 0L)
			{
				ZNetPeer peer = GetPeer(rpcData.m_targetPeerID);
				if (peer != null && peer.IsReady())
				{
					peer.m_rpc.Invoke("RoutedRPC", zPackage);
				}
				return;
			}
			{
				foreach (ZNetPeer peer2 in m_peers)
				{
					if (rpcData.m_senderPeerID != peer2.m_uid && peer2.IsReady())
					{
						peer2.m_rpc.Invoke("RoutedRPC", zPackage);
					}
				}
				return;
			}
		}
		foreach (ZNetPeer peer3 in m_peers)
		{
			if (peer3.IsReady())
			{
				peer3.m_rpc.Invoke("RoutedRPC", zPackage);
			}
		}
	}

	private void RPC_RoutedRPC(ZRpc rpc, ZPackage pkg)
	{
		RoutedRPCData routedRPCData = new RoutedRPCData();
		routedRPCData.Deserialize(pkg);
		if (routedRPCData.m_targetPeerID == m_id || routedRPCData.m_targetPeerID == 0L)
		{
			HandleRoutedRPC(routedRPCData);
		}
		if (m_server && routedRPCData.m_targetPeerID != m_id)
		{
			RouteRPC(routedRPCData);
		}
	}

	private void HandleRoutedRPC(RoutedRPCData data)
	{
		if (data.m_targetZDO.IsNone())
		{
			if (m_functions.TryGetValue(data.m_methodHash, out var value))
			{
				value.Invoke(data.m_senderPeerID, data.m_parameters);
			}
			return;
		}
		ZDO zDO = ZDOMan.instance.GetZDO(data.m_targetZDO);
		if (zDO != null)
		{
			ZNetView zNetView = ZNetScene.instance.FindInstance(zDO);
			if (zNetView != null)
			{
				zNetView.HandleRoutedRPC(data);
			}
		}
	}

	public void Register(string name, Action<long> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod(f));
	}

	public void Register<T>(string name, Action<long, T> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T>(f));
	}

	public void Register<T, U>(string name, Action<long, T, U> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U>(f));
	}

	public void Register<T, U, V>(string name, Action<long, T, U, V> f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V>(f));
	}

	public void Register<T, U, V, B>(string name, RoutedMethod<T, U, V, B>.Method f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B>(f));
	}

	public void Register<T, U, V, B, K>(string name, RoutedMethod<T, U, V, B, K>.Method f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B, K>(f));
	}

	public void Register<T, U, V, B, K, M>(string name, RoutedMethod<T, U, V, B, K, M>.Method f)
	{
		m_functions.Add(name.GetStableHashCode(), new RoutedMethod<T, U, V, B, K, M>(f));
	}
}
