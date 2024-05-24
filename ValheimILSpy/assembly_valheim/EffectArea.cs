using System;
using System.Collections.Generic;
using UnityEngine;

public class EffectArea : MonoBehaviour
{
	[Flags]
	public enum Type : byte
	{
		None = 0,
		Heat = 1,
		Fire = 2,
		PlayerBase = 4,
		Burning = 8,
		Teleport = 0x10,
		NoMonsters = 0x20,
		WarmCozyArea = 0x40,
		PrivateProperty = 0x80
	}

	private KeyValuePair<Bounds, EffectArea> noMonsterArea;

	private KeyValuePair<Bounds, EffectArea> noMonsterCloseToArea;

	[BitMask(typeof(Type))]
	public Type m_type;

	public string m_statusEffect = "";

	public bool m_playerOnly;

	private int m_statusEffectHash;

	private Collider m_collider;

	private static int s_characterMask = 0;

	private static readonly List<EffectArea> s_allAreas = new List<EffectArea>();

	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_noMonsterAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	private static readonly List<KeyValuePair<Bounds, EffectArea>> s_noMonsterCloseToAreas = new List<KeyValuePair<Bounds, EffectArea>>();

	private static Collider[] m_tempColliders = new Collider[128];

	private void Awake()
	{
		if (!string.IsNullOrEmpty(m_statusEffect))
		{
			m_statusEffectHash = m_statusEffect.GetStableHashCode();
		}
		if (s_characterMask == 0)
		{
			s_characterMask = LayerMask.GetMask("character_trigger");
		}
		m_collider = GetComponent<Collider>();
		m_collider.isTrigger = true;
		if ((m_type & Type.NoMonsters) != 0)
		{
			noMonsterArea = new KeyValuePair<Bounds, EffectArea>(m_collider.bounds, this);
			s_noMonsterAreas.Add(noMonsterArea);
			Bounds bounds = m_collider.bounds;
			bounds.Expand(new Vector3(15f, 15f, 15f));
			noMonsterCloseToArea = new KeyValuePair<Bounds, EffectArea>(bounds, this);
			s_noMonsterCloseToAreas.Add(noMonsterCloseToArea);
		}
		s_allAreas.Add(this);
	}

	private void OnDestroy()
	{
		s_allAreas.Remove(this);
		if (s_noMonsterAreas.Contains(noMonsterArea))
		{
			s_noMonsterAreas.Remove(noMonsterArea);
		}
		if (s_noMonsterCloseToAreas.Contains(noMonsterCloseToArea))
		{
			s_noMonsterCloseToAreas.Remove(noMonsterCloseToArea);
		}
	}

	private void OnTriggerStay(Collider collider)
	{
		if (ZNet.instance == null)
		{
			return;
		}
		Character component = collider.GetComponent<Character>();
		if ((bool)component && component.IsOwner() && (!m_playerOnly || component.IsPlayer()))
		{
			if (!string.IsNullOrEmpty(m_statusEffect))
			{
				component.GetSEMan().AddStatusEffect(m_statusEffectHash, resetTime: true);
			}
			if ((m_type & Type.Heat) != 0)
			{
				component.OnNearFire(base.transform.position);
			}
		}
	}

	public float GetRadius()
	{
		Collider collider = m_collider;
		if (!(collider is SphereCollider { radius: var radius }))
		{
			if (!(collider is CapsuleCollider { radius: var radius2 }))
			{
				return m_collider.bounds.size.magnitude;
			}
			return radius2;
		}
		return radius;
	}

	public static EffectArea IsPointInsideNoMonsterArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> s_noMonsterArea in s_noMonsterAreas)
		{
			if (s_noMonsterArea.Key.Contains(p))
			{
				return s_noMonsterArea.Value;
			}
		}
		return null;
	}

	public static EffectArea IsPointCloseToNoMonsterArea(Vector3 p)
	{
		foreach (KeyValuePair<Bounds, EffectArea> s_noMonsterCloseToArea in s_noMonsterCloseToAreas)
		{
			if (s_noMonsterCloseToArea.Key.Contains(p))
			{
				return s_noMonsterCloseToArea.Value;
			}
		}
		return null;
	}

	public static EffectArea IsPointInsideArea(Vector3 p, Type type, float radius = 0f)
	{
		int num = Physics.OverlapSphereNonAlloc(p, radius, m_tempColliders, s_characterMask);
		for (int i = 0; i < num; i++)
		{
			EffectArea component = m_tempColliders[i].GetComponent<EffectArea>();
			if ((bool)component && (component.m_type & type) != 0)
			{
				return component;
			}
		}
		return null;
	}

	public static int GetBaseValue(Vector3 p, float radius)
	{
		int num = 0;
		int num2 = Physics.OverlapSphereNonAlloc(p, radius, m_tempColliders, s_characterMask);
		for (int i = 0; i < num2; i++)
		{
			EffectArea component = m_tempColliders[i].GetComponent<EffectArea>();
			if ((bool)component && (component.m_type & Type.PlayerBase) != 0)
			{
				num++;
			}
		}
		return num;
	}

	public static List<EffectArea> GetAllAreas()
	{
		return s_allAreas;
	}
}
