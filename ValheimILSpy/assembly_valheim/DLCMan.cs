using System;
using System.Collections.Generic;
using Steamworks;
using UnityEngine;

public class DLCMan : MonoBehaviour
{
	[Serializable]
	public class DLCInfo
	{
		public string m_name = "DLC";

		public uint[] m_steamAPPID = new uint[0];

		[NonSerialized]
		public bool m_installed;
	}

	private static DLCMan m_instance;

	public List<DLCInfo> m_dlcs = new List<DLCInfo>();

	public static DLCMan instance => m_instance;

	private void Awake()
	{
		m_instance = this;
		CheckDLCsSTEAM();
	}

	private void OnDestroy()
	{
		if (m_instance == this)
		{
			m_instance = null;
		}
	}

	public bool IsDLCInstalled(string name)
	{
		if (name.Length == 0)
		{
			return true;
		}
		foreach (DLCInfo dlc in m_dlcs)
		{
			if (dlc.m_name == name)
			{
				return dlc.m_installed;
			}
		}
		ZLog.LogWarning("DLC " + name + " not registered in DLCMan");
		return false;
	}

	private void CheckDLCsSTEAM()
	{
		if (!SteamManager.Initialized)
		{
			ZLog.Log("Steam not initialized");
			return;
		}
		ZLog.Log("Checking for installed DLCs");
		foreach (DLCInfo dlc in m_dlcs)
		{
			dlc.m_installed = IsDLCInstalled(dlc);
			ZLog.Log("DLC:" + dlc.m_name + " installed:" + dlc.m_installed);
		}
	}

	private bool IsDLCInstalled(DLCInfo dlc)
	{
		uint[] steamAPPID = dlc.m_steamAPPID;
		foreach (uint id in steamAPPID)
		{
			if (IsDLCInstalled(id))
			{
				return true;
			}
		}
		return false;
	}

	private bool IsDLCInstalled(uint id)
	{
		AppId_t appId_t = new AppId_t(id);
		int dLCCount = SteamApps.GetDLCCount();
		for (int i = 0; i < dLCCount; i++)
		{
			if (SteamApps.BGetDLCDataByIndex(i, out var pAppID, out var _, out var _, 200) && appId_t == pAppID)
			{
				ZLog.Log("DLC installed:" + id);
				return SteamApps.BIsDlcInstalled(pAppID);
			}
		}
		return false;
	}
}
