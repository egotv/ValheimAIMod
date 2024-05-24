using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TerrainModifier : MonoBehaviour
{
	public enum PaintType
	{
		Dirt,
		Cultivate,
		Paved,
		Reset,
		ClearVegetation
	}

	private static bool m_triggerOnPlaced = false;

	public int m_sortOrder;

	public bool m_useTerrainCompiler;

	public bool m_playerModifiction;

	public float m_levelOffset;

	[Header("Level")]
	public bool m_level;

	public float m_levelRadius = 2f;

	public bool m_square = true;

	[Header("Smooth")]
	public bool m_smooth;

	public float m_smoothRadius = 2f;

	public float m_smoothPower = 3f;

	[Header("Paint")]
	public bool m_paintCleared = true;

	public bool m_paintHeightCheck;

	public PaintType m_paintType;

	public float m_paintRadius = 2f;

	[Header("Effects")]
	public EffectList m_onPlacedEffect = new EffectList();

	[Header("Spawn items")]
	public GameObject m_spawnOnPlaced;

	public float m_chanceToSpawn = 1f;

	public int m_maxSpawned = 1;

	public bool m_spawnAtMaxLevelDepth = true;

	private bool m_wasEnabled;

	private long m_creationTime;

	private ZNetView m_nview;

	private static readonly List<TerrainModifier> s_instances = new List<TerrainModifier>();

	private static bool s_needsSorting = false;

	private static bool s_delayedPokeHeightmaps = false;

	private static int s_lastFramePoked = 0;

	private void Awake()
	{
		s_instances.Add(this);
		s_needsSorting = true;
		m_nview = GetComponent<ZNetView>();
		m_wasEnabled = base.enabled;
		if (base.enabled)
		{
			if (m_triggerOnPlaced)
			{
				OnPlaced();
			}
			PokeHeightmaps(forcedDelay: true);
		}
		m_creationTime = GetCreationTime();
	}

	private void OnDestroy()
	{
		s_instances.Remove(this);
		s_needsSorting = true;
		if (m_wasEnabled)
		{
			PokeHeightmaps();
		}
	}

	public static void RemoveAll()
	{
		s_instances.Clear();
	}

	private void PokeHeightmaps(bool forcedDelay = false)
	{
		bool delayed = !m_triggerOnPlaced || forcedDelay;
		foreach (Heightmap allHeightmap in Heightmap.GetAllHeightmaps())
		{
			if (allHeightmap.TerrainVSModifier(this))
			{
				allHeightmap.Poke(delayed);
			}
		}
		if ((bool)ClutterSystem.instance)
		{
			ClutterSystem.instance.ResetGrass(base.transform.position, GetRadius());
		}
	}

	public float GetRadius()
	{
		float num = 0f;
		if (m_level && m_levelRadius > num)
		{
			num = m_levelRadius;
		}
		if (m_smooth && m_smoothRadius > num)
		{
			num = m_smoothRadius;
		}
		if (m_paintCleared && m_paintRadius > num)
		{
			num = m_paintRadius;
		}
		return num;
	}

	public static void SetTriggerOnPlaced(bool trigger)
	{
		m_triggerOnPlaced = trigger;
	}

	private void OnPlaced()
	{
		RemoveOthers(base.transform.position, GetRadius() / 4f);
		m_onPlacedEffect.Create(base.transform.position, Quaternion.identity);
		if ((bool)m_spawnOnPlaced && (m_spawnAtMaxLevelDepth || !Heightmap.AtMaxLevelDepth(base.transform.position + Vector3.up * m_levelOffset)) && Random.value <= m_chanceToSpawn)
		{
			Vector3 vector = Random.insideUnitCircle * 0.2f;
			GameObject obj = Object.Instantiate(m_spawnOnPlaced, base.transform.position + Vector3.up * 0.5f + vector, Quaternion.identity);
			obj.GetComponent<ItemDrop>().m_itemData.m_stack = Random.Range(1, m_maxSpawned + 1);
			obj.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;
		}
	}

	private static void GetModifiers(Vector3 point, float range, List<TerrainModifier> modifiers, TerrainModifier ignore = null)
	{
		foreach (TerrainModifier s_instance in s_instances)
		{
			if (!(s_instance == ignore) && Utils.DistanceXZ(point, s_instance.transform.position) < range)
			{
				modifiers.Add(s_instance);
			}
		}
	}

	public static Piece FindClosestModifierPieceInRange(Vector3 point, float range)
	{
		float num = 999999f;
		TerrainModifier terrainModifier = null;
		foreach (TerrainModifier s_instance in s_instances)
		{
			if (!(s_instance.m_nview == null))
			{
				float num2 = Utils.DistanceXZ(point, s_instance.transform.position);
				if (!(num2 > range) && !(num2 > num))
				{
					num = num2;
					terrainModifier = s_instance;
				}
			}
		}
		if ((bool)terrainModifier)
		{
			return terrainModifier.GetComponent<Piece>();
		}
		return null;
	}

	private void RemoveOthers(Vector3 point, float range)
	{
		List<TerrainModifier> list = new List<TerrainModifier>();
		GetModifiers(point, range, list, this);
		int num = 0;
		foreach (TerrainModifier item in list)
		{
			if ((m_level || !item.m_level) && (!m_paintCleared || m_paintType != PaintType.Reset || (item.m_paintCleared && item.m_paintType == PaintType.Reset)) && (bool)item.m_nview && item.m_nview.IsValid())
			{
				num++;
				item.m_nview.ClaimOwnership();
				item.m_nview.Destroy();
			}
		}
	}

	private static int SortByModifiers(TerrainModifier a, TerrainModifier b)
	{
		if (a.m_playerModifiction != b.m_playerModifiction)
		{
			return a.m_playerModifiction.CompareTo(b.m_playerModifiction);
		}
		if (a.m_sortOrder != b.m_sortOrder)
		{
			return a.m_sortOrder.CompareTo(b.m_sortOrder);
		}
		if (a.m_creationTime != b.m_creationTime)
		{
			return a.m_creationTime.CompareTo(b.m_creationTime);
		}
		return a.transform.position.sqrMagnitude.CompareTo(b.transform.position.sqrMagnitude);
	}

	public static List<TerrainModifier> GetAllInstances()
	{
		if (s_needsSorting)
		{
			s_instances.Sort(SortByModifiers);
			s_needsSorting = false;
		}
		return s_instances;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.matrix = Matrix4x4.TRS(base.transform.position + Vector3.up * m_levelOffset, Quaternion.identity, new Vector3(1f, 0f, 1f));
		if (m_level)
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireSphere(Vector3.zero, m_levelRadius);
		}
		if (m_smooth)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireSphere(Vector3.zero, m_smoothRadius);
		}
		if (m_paintCleared)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(Vector3.zero, m_paintRadius);
		}
		Gizmos.matrix = Matrix4x4.identity;
	}

	public ZDOID GetZDOID()
	{
		if ((bool)m_nview && m_nview.GetZDO() != null)
		{
			return m_nview.GetZDO().m_uid;
		}
		return ZDOID.None;
	}

	private long GetCreationTime()
	{
		long num = 0L;
		if ((bool)m_nview && m_nview.GetZDO() != null)
		{
			m_nview.GetZDO().GetPrefab();
			ZDO zDO = m_nview.GetZDO();
			ZDOID uid = zDO.m_uid;
			num = zDO.GetLong(ZDOVars.s_terrainModifierTimeCreated, 0L);
			if (num == 0L)
			{
				num = ZDOExtraData.GetTimeCreated(uid);
				if (num != 0L)
				{
					zDO.Set(ZDOVars.s_terrainModifierTimeCreated, num);
					Debug.LogError("CreationTime should already be set for " + m_nview.name + "  Prefab: " + m_nview.GetZDO().GetPrefab());
				}
			}
		}
		return num;
	}
}
