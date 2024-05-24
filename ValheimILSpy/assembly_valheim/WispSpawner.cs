using System;
using System.Collections.Generic;
using UnityEngine;

public class WispSpawner : MonoBehaviour, Hoverable
{
	public enum Status
	{
		NoSpace,
		TooBright,
		Full,
		Ok
	}

	public string m_name = "$pieces_wisplure";

	public float m_spawnInterval = 5f;

	[Range(0f, 1f)]
	public float m_spawnChance = 0.5f;

	public int m_maxSpawned = 3;

	public bool m_onlySpawnAtNight = true;

	public bool m_dontSpawnInCover = true;

	[Range(0f, 1f)]
	public float m_maxCover = 0.6f;

	public GameObject m_wispPrefab;

	public GameObject m_wispsNearbyObject;

	public float m_nearbyTreshold = 5f;

	public Transform m_spawnPoint;

	public Transform m_coverPoint;

	public float m_spawnDistance = 20f;

	public float m_maxSpawnedArea = 10f;

	private ZNetView m_nview;

	private Status m_status = Status.Ok;

	private float m_lastStatusUpdate = -1000f;

	private static readonly List<WispSpawner> s_spawners = new List<WispSpawner>();

	private void Start()
	{
		s_spawners.Add(this);
		m_nview = GetComponentInParent<ZNetView>();
		InvokeRepeating("TrySpawn", 10f, 10f);
		InvokeRepeating("UpdateDemister", UnityEngine.Random.Range(0f, 2f), 2f);
	}

	private void OnDestroy()
	{
		s_spawners.Remove(this);
	}

	public string GetHoverText()
	{
		return GetStatus() switch
		{
			Status.NoSpace => Localization.instance.Localize(m_name + " ( $piece_wisplure_nospace )"), 
			Status.Full => Localization.instance.Localize(m_name + " ( $piece_wisplure_full )"), 
			Status.TooBright => Localization.instance.Localize(m_name + " ( $piece_wisplure_light )"), 
			Status.Ok => Localization.instance.Localize(m_name + " ( $piece_wisplure_ok )"), 
			_ => "", 
		};
	}

	public string GetHoverName()
	{
		return m_name;
	}

	private void UpdateDemister()
	{
		if ((bool)m_wispsNearbyObject)
		{
			int wispsInArea = LuredWisp.GetWispsInArea(m_spawnPoint.position, m_nearbyTreshold);
			m_wispsNearbyObject.SetActive(wispsInArea > 0);
		}
	}

	private Status GetStatus()
	{
		if (Time.time - m_lastStatusUpdate < 4f)
		{
			return m_status;
		}
		m_lastStatusUpdate = Time.time;
		m_status = Status.Ok;
		if (!HaveFreeSpace())
		{
			m_status = Status.NoSpace;
		}
		else if (m_onlySpawnAtNight && EnvMan.IsDaylight())
		{
			m_status = Status.TooBright;
		}
		else if (LuredWisp.GetWispsInArea(m_spawnPoint.position, m_maxSpawnedArea) >= m_maxSpawned)
		{
			m_status = Status.Full;
		}
		return m_status;
	}

	private void TrySpawn()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			DateTime time = ZNet.instance.GetTime();
			DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_lastSpawn, 0L));
			if (!((time - dateTime).TotalSeconds < (double)m_spawnInterval) && !(UnityEngine.Random.value > m_spawnChance) && GetStatus() == Status.Ok)
			{
				Vector3 position = m_spawnPoint.position + Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f) * Vector3.forward * m_spawnDistance;
				UnityEngine.Object.Instantiate(m_wispPrefab, position, Quaternion.identity);
				m_nview.GetZDO().Set(ZDOVars.s_lastSpawn, ZNet.instance.GetTime().Ticks);
			}
		}
	}

	private bool HaveFreeSpace()
	{
		if (m_maxCover <= 0f)
		{
			return true;
		}
		Cover.GetCoverForPoint(m_coverPoint.position, out var coverPercentage, out var _);
		return coverPercentage < m_maxCover;
	}

	private void OnDrawGizmos()
	{
	}

	public static WispSpawner GetBestSpawner(Vector3 p, float maxRange)
	{
		WispSpawner wispSpawner = null;
		float num = 0f;
		foreach (WispSpawner s_spawner in s_spawners)
		{
			float num2 = Vector3.Distance(s_spawner.m_spawnPoint.position, p);
			if (!(num2 > maxRange))
			{
				Status status = s_spawner.GetStatus();
				if (status != 0 && status != Status.TooBright && (status != Status.Full || !(num2 > s_spawner.m_maxSpawnedArea)) && (num2 < num || wispSpawner == null))
				{
					num = num2;
					wispSpawner = s_spawner;
				}
			}
		}
		return wispSpawner;
	}
}
