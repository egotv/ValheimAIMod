using System;
using System.Collections.Generic;
using UnityEngine;

public class ZNetPeer : IDisposable
{
	public ZRpc m_rpc;

	public ISocket m_socket;

	public long m_uid;

	public bool m_server;

	public Vector3 m_refPos = Vector3.zero;

	public bool m_publicRefPos;

	public ZDOID m_characterID = ZDOID.None;

	public Dictionary<string, string> m_serverSyncedPlayerData = new Dictionary<string, string>();

	public string m_playerName = "";

	public ZNetPeer(ISocket socket, bool server)
	{
		m_socket = socket;
		m_rpc = new ZRpc(m_socket);
		m_server = server;
	}

	public void Dispose()
	{
		m_socket.Dispose();
		m_rpc.Dispose();
	}

	public bool IsReady()
	{
		return m_uid != 0;
	}

	public Vector3 GetRefPos()
	{
		return m_refPos;
	}
}
