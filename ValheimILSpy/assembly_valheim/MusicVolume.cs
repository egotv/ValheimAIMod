using System;
using System.Collections.Generic;
using UnityEngine;

public class MusicVolume : MonoBehaviour
{
	private ZNetView m_nview;

	public static List<MusicVolume> m_proximityMusicVolumes = new List<MusicVolume>();

	private static MusicVolume m_lastProximityVolume;

	private static List<MusicVolume> m_close = new List<MusicVolume>();

	public bool m_addRadiusFromLocation = true;

	public float m_radius = 10f;

	public float m_outerRadiusExtra = 0.5f;

	public float m_surroundingPlayersAdditionalRadius = 50f;

	public Bounds m_boundsInner;

	[Tooltip("Takes dimension from the room it's a part of and sets bounds to it's size.")]
	public Room m_sizeFromRoom;

	[Header("Music")]
	public string m_musicName = "";

	public float m_musicChance = 0.7f;

	[Tooltip("If the music can play again before playing a different location music first.")]
	public bool m_musicCanRepeat = true;

	public bool m_loopMusic;

	public bool m_stopMusicOnExit;

	public int m_maxPlaysPerActivation;

	[Tooltip("Makes the music fade by distance between inner/outer bounds. With this enabled loop, repeat, stoponexit, chance, etc is ignored.")]
	public bool m_fadeByProximity;

	[HideInInspector]
	public int m_PlayCount;

	private double m_lastEnterCheck;

	private bool m_lastWasInside;

	private bool m_lastWasInsideWide;

	private bool m_isLooping;

	private float m_proximity;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview)
		{
			m_PlayCount = m_nview.GetZDO().GetInt(ZDOVars.s_plays);
			m_nview.Register("RPC_PlayMusic", RPC_PlayMusic);
		}
		if (m_addRadiusFromLocation)
		{
			Location componentInParent = GetComponentInParent<Location>();
			if ((object)componentInParent != null)
			{
				m_radius += componentInParent.GetMaxRadius();
			}
		}
		if (m_fadeByProximity)
		{
			m_proximityMusicVolumes.Add(this);
		}
	}

	private void OnDestroy()
	{
		m_proximityMusicVolumes.Remove(this);
	}

	private void RPC_PlayMusic(long sender)
	{
		bool flag = Vector3.Distance(Player.m_localPlayer.transform.position, base.transform.position) < m_radius + m_surroundingPlayersAdditionalRadius;
		if (flag)
		{
			PlayMusic();
		}
		if ((bool)m_nview && m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.GetZDO().Set(ZDOVars.s_plays, flag ? m_PlayCount : (m_PlayCount + 1));
		}
	}

	private void PlayMusic()
	{
		ZLog.Log("MusicLocation '" + base.name + "' Playing Music: " + m_musicName);
		m_PlayCount++;
		MusicMan.instance.LocationMusic(m_musicName);
		if (m_loopMusic)
		{
			m_isLooping = true;
		}
	}

	private void Update()
	{
		if (Player.m_localPlayer == null || m_fadeByProximity)
		{
			return;
		}
		double timeSeconds = ZNet.instance.GetTimeSeconds();
		if (timeSeconds > m_lastEnterCheck + 1.0)
		{
			m_lastEnterCheck = timeSeconds;
			if (IsInside(Player.m_localPlayer.transform.position))
			{
				if (!m_lastWasInside)
				{
					m_lastWasInside = (m_lastWasInsideWide = true);
					OnEnter();
				}
			}
			else
			{
				if (m_lastWasInside)
				{
					m_lastWasInside = false;
					OnExit();
				}
				if (m_lastWasInsideWide && !IsInside(Player.m_localPlayer.transform.position, checkOuter: true))
				{
					m_lastWasInsideWide = false;
					OnExitWide();
				}
			}
		}
		if (m_isLooping && m_lastWasInside && !string.IsNullOrEmpty(m_musicName))
		{
			MusicMan.instance.LocationMusic(m_musicName);
		}
	}

	private void OnEnter()
	{
		ZLog.Log("MusicLocation.OnEnter: " + base.name);
		if (!string.IsNullOrEmpty(m_musicName) && (m_maxPlaysPerActivation == 0 || m_PlayCount < m_maxPlaysPerActivation) && UnityEngine.Random.Range(0f, 1f) <= m_musicChance && (m_musicCanRepeat || MusicMan.instance.m_lastLocationMusic != m_musicName))
		{
			if ((bool)m_nview)
			{
				m_nview.InvokeRPC(ZNetView.Everybody, "RPC_PlayMusic");
			}
			else
			{
				PlayMusic();
			}
		}
	}

	private void OnExit()
	{
		ZLog.Log("MusicLocation.OnExit: " + base.name);
	}

	private void OnExitWide()
	{
		ZLog.Log("MusicLocation.OnExitWide: " + base.name);
		if (MusicMan.instance.m_lastLocationMusic == m_musicName && (m_stopMusicOnExit || m_loopMusic))
		{
			MusicMan.instance.LocationMusic(null);
		}
		m_isLooping = false;
	}

	public bool IsInside(Vector3 point, bool checkOuter = false)
	{
		if (IsBox())
		{
			if (!checkOuter)
			{
				return GetInnerBounds().Contains(point);
			}
			return GetOuterBounds().Contains(point);
		}
		float num = Vector3.Distance(base.transform.position, point);
		if (checkOuter)
		{
			return num < m_radius + m_outerRadiusExtra;
		}
		return num < m_radius;
	}

	private void OnDrawGizmos()
	{
		if (!IsBox())
		{
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
			Gizmos.DrawWireSphere(base.transform.position, m_radius);
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
			Gizmos.DrawWireSphere(base.transform.position, m_radius + m_outerRadiusExtra);
		}
		else
		{
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.5f);
			Gizmos.DrawWireCube(GetInnerBounds().center, GetBox().size);
			Gizmos.color = new Color(0.6f, 0.8f, 0.8f, 0.25f);
			Gizmos.DrawWireCube(GetOuterBounds().center, GetOuterBounds().size);
		}
	}

	private bool IsBox()
	{
		return GetBox().size.x != 0f;
	}

	private Bounds GetBox()
	{
		if (!m_sizeFromRoom)
		{
			return m_boundsInner;
		}
		return new Bounds(Vector3.zero, m_sizeFromRoom.m_size);
	}

	private Bounds GetInnerBounds()
	{
		Bounds box = GetBox();
		return new Bounds(box.center + base.transform.position, box.size);
	}

	private Bounds GetOuterBounds()
	{
		Bounds box = GetBox();
		return new Bounds(box.center + base.transform.position, box.size + new Vector3(m_outerRadiusExtra, m_outerRadiusExtra, m_outerRadiusExtra));
	}

	private float MinBoundDimension()
	{
		Bounds box = GetBox();
		if (!(box.size.x < box.size.y) || !(box.size.x < box.size.z))
		{
			if (!(box.size.y < box.size.z))
			{
				return box.size.z;
			}
			return box.size.y;
		}
		return box.size.x;
	}

	public static float UpdateProximityVolumes(AudioSource musicSource)
	{
		if (!Player.m_localPlayer)
		{
			return 1f;
		}
		float num = 0f;
		if (m_lastProximityVolume != null && m_lastProximityVolume.GetInnerBounds().Contains(Player.m_localPlayer.transform.position))
		{
			num = 1f;
		}
		else
		{
			m_lastProximityVolume = null;
			m_close.Clear();
			foreach (MusicVolume proximityMusicVolume in m_proximityMusicVolumes)
			{
				if ((bool)proximityMusicVolume && proximityMusicVolume.IsInside(Player.m_localPlayer.transform.position, checkOuter: true))
				{
					m_close.Add(proximityMusicVolume);
				}
			}
			if (m_close.Count == 0)
			{
				MusicMan.instance.LocationMusic(null);
				return 1f;
			}
			foreach (MusicVolume item in m_close)
			{
				if (item.IsInside(Player.m_localPlayer.transform.position))
				{
					m_lastProximityVolume = item;
					num = 1f;
				}
			}
			if (num == 0f)
			{
				MusicVolume musicVolume = null;
				foreach (MusicVolume item2 in m_close)
				{
					float num2;
					float num3;
					if (item2.IsBox())
					{
						num2 = Vector3.Distance(item2.GetInnerBounds().ClosestPoint(Player.m_localPlayer.transform.position), Player.m_localPlayer.transform.position);
						num3 = item2.m_outerRadiusExtra - num2;
					}
					else
					{
						float num4 = Vector3.Distance(item2.transform.position, Player.m_localPlayer.transform.position);
						num2 = num4 - item2.m_radius;
						num3 = item2.m_radius + item2.m_outerRadiusExtra - num4;
					}
					item2.m_proximity = 1f - Math.Min(1f, num2 / (num2 + num3));
					if (musicVolume == null || item2.m_proximity > musicVolume.m_proximity)
					{
						musicVolume = item2;
					}
				}
				m_lastProximityVolume = musicVolume;
				num = musicVolume.m_proximity;
			}
		}
		MusicMan.instance.LocationMusic(m_lastProximityVolume.m_musicName);
		return num;
	}
}
