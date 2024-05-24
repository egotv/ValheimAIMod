using System;
using UnityEngine;

public class Plant : SlowUpdate, Hoverable
{
	public enum Status
	{
		Healthy,
		NoSun,
		NoSpace,
		WrongBiome,
		NotCultivated,
		NoAttachPiece,
		TooHot,
		TooCold
	}

	private static Collider[] s_colliders = new Collider[30];

	private static Collider[] s_hits = new Collider[10];

	public string m_name = "Plant";

	public float m_growTime = 10f;

	public float m_growTimeMax = 2000f;

	public GameObject[] m_grownPrefabs = new GameObject[0];

	public float m_minScale = 1f;

	public float m_maxScale = 1f;

	public float m_growRadius = 1f;

	public float m_growRadiusVines;

	public bool m_needCultivatedGround;

	public bool m_destroyIfCantGrow;

	public bool m_tolerateHeat;

	public bool m_tolerateCold;

	[SerializeField]
	private GameObject m_healthy;

	[SerializeField]
	private GameObject m_unhealthy;

	[SerializeField]
	private GameObject m_healthyGrown;

	[SerializeField]
	private GameObject m_unhealthyGrown;

	[BitMask(typeof(Heightmap.Biome))]
	public Heightmap.Biome m_biome;

	public EffectList m_growEffect = new EffectList();

	[Header("Attach to buildpiece (Vines)")]
	public float m_attachDistance;

	private Status m_status;

	private ZNetView m_nview;

	private float m_updateTime;

	private float m_spawnTime;

	private int m_seed;

	private Vector3 m_attachPos;

	private Vector3 m_attachNormal;

	private Quaternion m_attachRot;

	private Collider m_attachCollider;

	private static int m_spaceMask = 0;

	private static int m_roofMask = 0;

	private static int m_pieceMask = 0;

	public override void Awake()
	{
		base.Awake();
		m_nview = base.gameObject.GetComponent<ZNetView>();
		if (m_nview.GetZDO() != null)
		{
			m_seed = m_nview.GetZDO().GetInt(ZDOVars.s_seed);
			if (m_seed == 0)
			{
				m_seed = (int)(m_nview.GetZDO().m_uid.ID + m_nview.GetZDO().m_uid.UserID);
				m_nview.GetZDO().Set(ZDOVars.s_seed, m_seed, okForNotOwner: true);
			}
			if (m_nview.IsOwner() && m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, 0L) == 0L)
			{
				m_nview.GetZDO().Set(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks);
			}
			m_spawnTime = Time.time;
		}
	}

	public string GetHoverText()
	{
		return m_status switch
		{
			Status.Healthy => Localization.instance.Localize(m_name + " ( $piece_plant_healthy )"), 
			Status.NoSpace => Localization.instance.Localize(m_name + " ( $piece_plant_nospace )"), 
			Status.NoSun => Localization.instance.Localize(m_name + " ( $piece_plant_nosun )"), 
			Status.WrongBiome => Localization.instance.Localize(m_name + " ( $piece_plant_wrongbiome )"), 
			Status.NotCultivated => Localization.instance.Localize(m_name + " ( $piece_plant_notcultivated )"), 
			Status.TooHot => Localization.instance.Localize(m_name + " ( $piece_plant_toohot )"), 
			Status.TooCold => Localization.instance.Localize(m_name + " ( $piece_plant_toocold )"), 
			Status.NoAttachPiece => Localization.instance.Localize(m_name + " ( $piece_plant_nowall )"), 
			_ => "", 
		};
	}

	public string GetHoverName()
	{
		return Localization.instance.Localize(m_name);
	}

	private double TimeSincePlanted()
	{
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_plantTime, ZNet.instance.GetTime().Ticks));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds;
	}

	public override void SUpdate()
	{
		if (m_nview.IsValid() && !(Time.time - m_updateTime < 10f))
		{
			m_updateTime = Time.time;
			double num = TimeSincePlanted();
			UpdateHealth(num);
			float growTime = GetGrowTime();
			if ((bool)m_healthyGrown)
			{
				bool flag = num > (double)(growTime * 0.5f);
				m_healthy.SetActive(!flag && m_status == Status.Healthy);
				m_unhealthy.SetActive(!flag && m_status != Status.Healthy);
				m_healthyGrown.SetActive(flag && m_status == Status.Healthy);
				m_unhealthyGrown.SetActive(flag && m_status != Status.Healthy);
			}
			else
			{
				m_healthy.SetActive(m_status == Status.Healthy);
				m_unhealthy.SetActive(m_status != Status.Healthy);
			}
			if (m_nview.IsOwner() && Time.time - m_spawnTime > 10f && num > (double)growTime)
			{
				Grow();
			}
		}
	}

	private float GetGrowTime()
	{
		UnityEngine.Random.State state = UnityEngine.Random.state;
		UnityEngine.Random.InitState(m_seed);
		float value = UnityEngine.Random.value;
		UnityEngine.Random.state = state;
		return Mathf.Lerp(m_growTime, m_growTimeMax, value);
	}

	public GameObject Grow()
	{
		if (m_status != 0)
		{
			if (m_destroyIfCantGrow)
			{
				Destroy();
			}
			return null;
		}
		float num = 11.25f;
		GameObject original = m_grownPrefabs[UnityEngine.Random.Range(0, m_grownPrefabs.Length)];
		GameObject gameObject = null;
		Vector3 position = ((m_attachDistance > 0f) ? m_attachPos : base.transform.position);
		Quaternion quaternion = ((m_attachDistance > 0f) ? m_attachRot : Quaternion.Euler(base.transform.rotation.eulerAngles.x, base.transform.rotation.eulerAngles.y + UnityEngine.Random.Range(0f - num, num), base.transform.rotation.eulerAngles.z));
		gameObject = UnityEngine.Object.Instantiate(original, position, quaternion);
		if (m_attachDistance > 0f)
		{
			PlaceAgainst(gameObject, m_attachRot, m_attachPos, m_attachNormal);
		}
		Quaternion quaternion2 = quaternion;
		ZLog.Log("Starting to grow plant with rotation: " + quaternion2.ToString());
		ZNetView component = gameObject.GetComponent<ZNetView>();
		float num2 = UnityEngine.Random.Range(m_minScale, m_maxScale);
		component.SetLocalScale(new Vector3(num2, num2, num2));
		gameObject.GetComponent<TreeBase>()?.Grow();
		if ((bool)m_nview)
		{
			m_nview.Destroy();
			m_growEffect.Create(base.transform.position, quaternion, null, num2);
		}
		return gameObject;
	}

	public void UpdateHealth(double timeSincePlanted)
	{
		if (timeSincePlanted < 10.0)
		{
			m_status = Status.Healthy;
			return;
		}
		Heightmap heightmap = Heightmap.FindHeightmap(base.transform.position);
		if ((bool)heightmap)
		{
			Heightmap.Biome biome = heightmap.GetBiome(base.transform.position);
			if ((biome & m_biome) == 0)
			{
				m_status = Status.WrongBiome;
				return;
			}
			if (m_needCultivatedGround && !heightmap.IsCultivated(base.transform.position))
			{
				m_status = Status.NotCultivated;
				return;
			}
			if (!m_tolerateHeat && biome == Heightmap.Biome.AshLands && !ShieldGenerator.IsInsideShield(base.transform.position))
			{
				m_status = Status.TooHot;
				return;
			}
			if (!m_tolerateCold && (biome == Heightmap.Biome.DeepNorth || biome == Heightmap.Biome.Mountain) && !ShieldGenerator.IsInsideShield(base.transform.position))
			{
				m_status = Status.TooCold;
				return;
			}
		}
		if (HaveRoof())
		{
			m_status = Status.NoSun;
		}
		else if (!HaveGrowSpace())
		{
			m_status = Status.NoSpace;
		}
		else if (m_attachDistance > 0f && !GetClosestAttachPosRot(out m_attachPos, out m_attachRot, out m_attachNormal))
		{
			m_status = Status.NoAttachPiece;
		}
		else
		{
			m_status = Status.Healthy;
		}
	}

	public Collider GetClosestAttachObject()
	{
		return GetClosestAttachObject(base.transform.position);
	}

	public Collider GetClosestAttachObject(Vector3 from)
	{
		if (m_pieceMask == 0)
		{
			m_pieceMask = LayerMask.GetMask("piece");
		}
		int num = Physics.OverlapSphereNonAlloc(from, m_attachDistance, s_hits, m_pieceMask);
		Collider result = null;
		float num2 = float.MaxValue;
		for (int i = 0; i < num; i++)
		{
			Collider collider = s_hits[i];
			float num3 = Vector3.Distance(from, collider.bounds.center);
			if (num3 < num2)
			{
				Piece componentInParent = collider.GetComponentInParent<Piece>();
				if ((object)componentInParent != null && !componentInParent.m_noVines)
				{
					result = collider;
					num2 = num3;
				}
			}
		}
		return result;
	}

	public bool GetClosestAttachPosRot(out Vector3 pos, out Quaternion rot, out Vector3 normal)
	{
		return GetClosestAttachPosRot(base.transform.position, out pos, out rot, out normal);
	}

	public bool GetClosestAttachPosRot(Vector3 from, out Vector3 pos, out Quaternion rot, out Vector3 normal)
	{
		Collider closestAttachObject = GetClosestAttachObject(from);
		if ((object)closestAttachObject != null)
		{
			if (m_pieceMask == 0)
			{
				m_pieceMask = LayerMask.GetMask("piece");
			}
			if (from.y < closestAttachObject.bounds.min.y)
			{
				from.y += closestAttachObject.bounds.min.y - from.y + 0.01f;
			}
			if (from.y > closestAttachObject.bounds.max.y)
			{
				from.y += closestAttachObject.bounds.max.y - from.y - 0.01f;
			}
			Vector3 vector = closestAttachObject.ClosestPoint(from);
			if (Physics.Raycast(from, vector - from, out var hitInfo, 50f, m_pieceMask) && (bool)hitInfo.collider && !hitInfo.collider.attachedRigidbody)
			{
				pos = hitInfo.point;
				rot = Quaternion.Euler(0f, 90f, 0f) * Quaternion.LookRotation(hitInfo.normal);
				normal = hitInfo.normal;
				Terminal.Log("Plant found grow normal: " + hitInfo.normal.ToString());
				return true;
			}
			Terminal.Log("Plant ray didn't hit any valid colliders");
		}
		pos = (normal = Vector3.zero);
		rot = Quaternion.identity;
		Terminal.Log("Plant found no attach obj.");
		return false;
	}

	public void PlaceAgainst(GameObject obj, Quaternion rot, Vector3 hitPos, Vector3 hitNormal)
	{
		obj.transform.position = hitPos + hitNormal * 50f;
		obj.transform.rotation = rot;
		Vector3 vector = Vector3.zero;
		float num = 999999f;
		Collider[] componentsInChildren = obj.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			if (collider.isTrigger || !collider.enabled)
			{
				continue;
			}
			MeshCollider meshCollider = collider as MeshCollider;
			if (!(meshCollider != null) || meshCollider.convex)
			{
				Vector3 vector2 = collider.ClosestPoint(hitPos);
				float num2 = Vector3.Distance(vector2, hitPos);
				if (num2 < num)
				{
					vector = vector2;
					num = num2;
				}
			}
		}
		Vector3 vector3 = obj.transform.position - vector;
		obj.transform.position = hitPos + vector3;
		obj.transform.rotation = rot;
	}

	private void Destroy()
	{
		IDestructible component = GetComponent<IDestructible>();
		if (component != null)
		{
			HitData hitData = new HitData();
			hitData.m_damage.m_damage = 9999f;
			component.Damage(hitData);
		}
	}

	private bool HaveRoof()
	{
		if (m_roofMask == 0)
		{
			m_roofMask = LayerMask.GetMask("Default", "static_solid", "piece");
		}
		if (Physics.Raycast(base.transform.position, Vector3.up, 100f, m_roofMask))
		{
			return true;
		}
		return false;
	}

	private bool HaveGrowSpace()
	{
		if (m_spaceMask == 0)
		{
			m_spaceMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid");
		}
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, m_growRadius, s_colliders, m_spaceMask);
		for (int i = 0; i < num; i++)
		{
			Plant component = s_colliders[i].GetComponent<Plant>();
			if (!component || (!(component == this) && component.GetStatus() == Status.Healthy))
			{
				return false;
			}
		}
		if (m_growRadiusVines > 0f)
		{
			num = Physics.OverlapSphereNonAlloc(base.transform.position, m_growRadiusVines, s_colliders, m_spaceMask);
			for (int j = 0; j < num; j++)
			{
				if (s_colliders[j].GetComponentInParent<Vine>() != null)
				{
					return false;
				}
			}
		}
		return true;
	}

	public Status GetStatus()
	{
		return m_status;
	}
}
