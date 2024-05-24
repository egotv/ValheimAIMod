using System;
using System.Collections.Generic;
using UnityEngine;

public class MineRock5 : MonoBehaviour, IDestructible, Hoverable
{
	private struct BoundData
	{
		public Vector3 m_pos;

		public Quaternion m_rot;

		public Vector3 m_size;
	}

	private class HitArea
	{
		public Collider m_collider;

		public MeshRenderer m_meshRenderer;

		public MeshFilter m_meshFilter;

		public StaticPhysics m_physics;

		public float m_health;

		public BoundData m_bound;

		public bool m_supported;

		public float m_baseScale;
	}

	private static Mesh m_tempMeshA;

	private static Mesh m_tempMeshB;

	private static List<CombineInstance> m_tempInstancesA = new List<CombineInstance>();

	private static List<CombineInstance> m_tempInstancesB = new List<CombineInstance>();

	public string m_name = "";

	public float m_health = 2f;

	public HitData.DamageModifiers m_damageModifiers;

	public int m_minToolTier;

	public bool m_supportCheck = true;

	public bool m_triggerPrivateArea;

	public EffectList m_destroyedEffect = new EffectList();

	public EffectList m_hitEffect = new EffectList();

	public DropTable m_dropItems;

	public bool m_hitEffectAreaCenter = true;

	private List<HitArea> m_hitAreas;

	private List<Renderer> m_extraRenderers;

	private bool m_haveSetupBounds;

	private ZNetView m_nview;

	private MeshFilter m_meshFilter;

	private MeshRenderer m_meshRenderer;

	private uint m_lastDataRevision = uint.MaxValue;

	private const int m_supportIterations = 3;

	private bool m_allDestroyed;

	private static int m_rayMask = 0;

	private static int m_groundLayer = 0;

	private static Collider[] m_tempColliders = new Collider[128];

	private static HashSet<Collider> m_tempColliderSet = new HashSet<Collider>();

	private void Awake()
	{
		Collider[] componentsInChildren = base.gameObject.GetComponentsInChildren<Collider>();
		m_hitAreas = new List<HitArea>(componentsInChildren.Length);
		m_extraRenderers = new List<Renderer>();
		foreach (Collider collider in componentsInChildren)
		{
			HitArea hitArea = new HitArea();
			hitArea.m_collider = collider;
			hitArea.m_meshFilter = collider.GetComponent<MeshFilter>();
			hitArea.m_meshRenderer = collider.GetComponent<MeshRenderer>();
			hitArea.m_physics = collider.GetComponent<StaticPhysics>();
			hitArea.m_health = m_health + (float)Game.m_worldLevel * m_health * Game.instance.m_worldLevelMineHPMultiplier;
			hitArea.m_baseScale = hitArea.m_collider.transform.localScale.x;
			for (int j = 0; j < collider.transform.childCount; j++)
			{
				Renderer[] componentsInChildren2 = collider.transform.GetChild(j).GetComponentsInChildren<Renderer>();
				m_extraRenderers.AddRange(componentsInChildren2);
			}
			m_hitAreas.Add(hitArea);
		}
		if (m_rayMask == 0)
		{
			m_rayMask = LayerMask.GetMask("piece", "Default", "static_solid", "Default_small", "terrain");
		}
		if (m_groundLayer == 0)
		{
			m_groundLayer = LayerMask.NameToLayer("terrain");
		}
		Material[] array = null;
		foreach (HitArea hitArea2 in m_hitAreas)
		{
			if (array == null || hitArea2.m_meshRenderer.sharedMaterials.Length > array.Length)
			{
				array = hitArea2.m_meshRenderer.sharedMaterials;
			}
		}
		m_meshFilter = base.gameObject.AddComponent<MeshFilter>();
		m_meshRenderer = base.gameObject.AddComponent<MeshRenderer>();
		m_meshRenderer.sharedMaterials = array;
		m_meshFilter.mesh = new Mesh();
		m_meshFilter.name = "___MineRock5 m_meshFilter";
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview && m_nview.GetZDO() != null)
		{
			m_nview.Register<HitData, int>("RPC_Damage", RPC_Damage);
			m_nview.Register<int, float>("RPC_SetAreaHealth", RPC_SetAreaHealth);
		}
		CheckForUpdate();
		InvokeRepeating("CheckForUpdate", UnityEngine.Random.Range(5f, 10f), 10f);
	}

	private void CheckSupport()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		UpdateSupport();
		for (int i = 0; i < m_hitAreas.Count; i++)
		{
			HitArea hitArea = m_hitAreas[i];
			if (hitArea.m_health > 0f && !hitArea.m_supported)
			{
				HitData hitData = new HitData();
				hitData.m_damage.m_damage = m_health;
				hitData.m_point = hitArea.m_collider.bounds.center;
				hitData.m_toolTier = 100;
				hitData.m_hitType = HitData.HitType.Structural;
				DamageArea(i, hitData);
			}
		}
	}

	private void CheckForUpdate()
	{
		if (m_nview.IsValid() && m_nview.GetZDO().DataRevision != m_lastDataRevision)
		{
			LoadHealth();
			UpdateMesh();
		}
	}

	private void LoadHealth()
	{
		string @string = m_nview.GetZDO().GetString(ZDOVars.s_health);
		if (@string.Length > 0)
		{
			ZPackage zPackage = new ZPackage(Convert.FromBase64String(@string));
			int num = zPackage.ReadInt();
			for (int i = 0; i < num; i++)
			{
				float health = zPackage.ReadSingle();
				HitArea hitArea = GetHitArea(i);
				if (hitArea != null)
				{
					hitArea.m_health = health;
				}
			}
		}
		m_lastDataRevision = m_nview.GetZDO().DataRevision;
	}

	private void SaveHealth()
	{
		ZPackage zPackage = new ZPackage();
		zPackage.Write(m_hitAreas.Count);
		foreach (HitArea hitArea in m_hitAreas)
		{
			zPackage.Write(hitArea.m_health);
		}
		string value = Convert.ToBase64String(zPackage.GetArray());
		m_nview.GetZDO().Set(ZDOVars.s_health, value);
		m_lastDataRevision = m_nview.GetZDO().DataRevision;
	}

	private void UpdateMesh()
	{
		m_tempInstancesA.Clear();
		m_tempInstancesB.Clear();
		Material material = m_meshRenderer.sharedMaterials[0];
		Matrix4x4 inverse = base.transform.localToWorldMatrix.inverse;
		for (int i = 0; i < m_hitAreas.Count; i++)
		{
			HitArea hitArea = m_hitAreas[i];
			if (hitArea.m_health > 0f)
			{
				CombineInstance item = default(CombineInstance);
				item.mesh = hitArea.m_meshFilter.sharedMesh;
				item.transform = inverse * hitArea.m_meshFilter.transform.localToWorldMatrix;
				for (int j = 0; j < hitArea.m_meshFilter.sharedMesh.subMeshCount; j++)
				{
					item.subMeshIndex = j;
					if (hitArea.m_meshRenderer.sharedMaterials[j] == material)
					{
						m_tempInstancesA.Add(item);
					}
					else
					{
						m_tempInstancesB.Add(item);
					}
				}
				hitArea.m_meshRenderer.enabled = false;
				hitArea.m_collider.gameObject.SetActive(value: true);
			}
			else
			{
				hitArea.m_collider.gameObject.SetActive(value: false);
			}
		}
		if (m_tempMeshA == null)
		{
			m_tempMeshA = new Mesh();
			m_tempMeshB = new Mesh();
			m_tempMeshA.name = "___MineRock5 m_tempMeshA";
			m_tempMeshB.name = "___MineRock5 m_tempMeshB";
		}
		m_tempMeshA.CombineMeshes(m_tempInstancesA.ToArray());
		m_tempMeshB.CombineMeshes(m_tempInstancesB.ToArray());
		CombineInstance combineInstance = default(CombineInstance);
		combineInstance.mesh = m_tempMeshA;
		CombineInstance combineInstance2 = default(CombineInstance);
		combineInstance2.mesh = m_tempMeshB;
		m_meshFilter.mesh.CombineMeshes(new CombineInstance[2] { combineInstance, combineInstance2 }, mergeSubMeshes: false, useMatrices: false);
		m_meshRenderer.enabled = true;
		Renderer[] array = new Renderer[m_extraRenderers.Count + 1];
		m_extraRenderers.CopyTo(0, array, 0, m_extraRenderers.Count);
		array[array.Length - 1] = m_meshRenderer;
		LODGroup component = base.gameObject.GetComponent<LODGroup>();
		LOD[] lODs = component.GetLODs();
		lODs[0].renderers = array;
		component.SetLODs(lODs);
	}

	public string GetHoverText()
	{
		return Localization.instance.Localize(m_name);
	}

	public string GetHoverName()
	{
		return m_name;
	}

	public DestructibleType GetDestructibleType()
	{
		return DestructibleType.Default;
	}

	public void Damage(HitData hit)
	{
		if (m_nview == null || !m_nview.IsValid() || m_hitAreas == null)
		{
			return;
		}
		if (hit.m_hitCollider == null || hit.m_radius > 0f)
		{
			int num = 0;
			m_tempColliderSet.Clear();
			int num2 = Physics.OverlapSphereNonAlloc(hit.m_point, (hit.m_radius > 0f) ? hit.m_radius : 0.05f, m_tempColliders, m_rayMask);
			for (int i = 0; i < num2; i++)
			{
				if (m_tempColliders[i].transform.parent == base.transform || m_tempColliders[i].transform.parent.parent == base.transform)
				{
					m_tempColliderSet.Add(m_tempColliders[i]);
				}
			}
			if (m_tempColliderSet.Count > 0)
			{
				foreach (Collider item in m_tempColliderSet)
				{
					int areaIndex = GetAreaIndex(item);
					if (areaIndex >= 0)
					{
						num++;
						m_nview.InvokeRPC("RPC_Damage", hit, areaIndex);
						if (m_allDestroyed)
						{
							return;
						}
					}
				}
			}
			if (num == 0)
			{
				ZLog.Log("Minerock hit has no collider or invalid hit area on " + base.gameObject.name);
			}
		}
		else
		{
			int areaIndex2 = GetAreaIndex(hit.m_hitCollider);
			if (areaIndex2 < 0)
			{
				ZLog.Log("Invalid hit area on " + base.gameObject.name);
				return;
			}
			m_nview.InvokeRPC("RPC_Damage", hit, areaIndex2);
		}
	}

	private void RPC_Damage(long sender, HitData hit, int hitAreaIndex)
	{
		if (m_nview == null || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		bool flag = DamageArea(hitAreaIndex, hit);
		if (flag && m_supportCheck)
		{
			CheckSupport();
		}
		if (m_triggerPrivateArea)
		{
			Character attacker = hit.GetAttacker();
			if ((object)attacker != null)
			{
				PrivateArea.OnObjectDamaged(base.transform.position, attacker, flag);
			}
		}
	}

	private bool DamageArea(int hitAreaIndex, HitData hit)
	{
		ZLog.Log("hit mine rock " + hitAreaIndex);
		HitArea hitArea = GetHitArea(hitAreaIndex);
		if (hitArea == null)
		{
			ZLog.Log("Missing hit area " + hitAreaIndex);
			return false;
		}
		LoadHealth();
		if (hitArea.m_health <= 0f)
		{
			ZLog.Log("Already destroyed");
			return false;
		}
		hit.ApplyResistance(m_damageModifiers, out var significantModifier);
		float totalDamage = hit.GetTotalDamage();
		Vector3 vector = ((m_hitEffectAreaCenter && hitArea.m_collider != null) ? hitArea.m_collider.bounds.center : hit.m_point);
		if (!hit.CheckToolTier(m_minToolTier))
		{
			DamageText.instance.ShowText(DamageText.TextType.TooHard, vector, 0f);
			return false;
		}
		DamageText.instance.ShowText(significantModifier, vector, totalDamage);
		if (totalDamage <= 0f)
		{
			return false;
		}
		hitArea.m_health -= totalDamage;
		SaveHealth();
		m_hitEffect.Create(vector, Quaternion.identity);
		if (hit.m_hitType != HitData.HitType.CinderFire)
		{
			Player.GetClosestPlayer(vector, 10f)?.AddNoise(100f);
		}
		if (hit.GetAttacker() == Player.m_localPlayer)
		{
			Game.instance.IncrementPlayerStat(PlayerStatType.MineHits);
		}
		if (hitArea.m_health <= 0f)
		{
			m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetAreaHealth", hitAreaIndex, hitArea.m_health);
			m_destroyedEffect.Create(vector, Quaternion.identity);
			foreach (GameObject drop in m_dropItems.GetDropList())
			{
				Vector3 position = vector + UnityEngine.Random.insideUnitSphere * 0.3f;
				UnityEngine.Object.Instantiate(drop, position, Quaternion.identity);
				ItemDrop.OnCreateNew(drop);
			}
			if (AllDestroyed())
			{
				m_nview.Destroy();
				m_allDestroyed = true;
			}
			if (hit.GetAttacker() == Player.m_localPlayer)
			{
				Game.instance.IncrementPlayerStat(PlayerStatType.Mines);
				switch (m_minToolTier)
				{
				case 0:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier0);
					break;
				case 1:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier1);
					break;
				case 2:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier2);
					break;
				case 3:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier3);
					break;
				case 4:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier4);
					break;
				case 5:
					Game.instance.IncrementPlayerStat(PlayerStatType.MineTier5);
					break;
				default:
					ZLog.LogWarning("No stat for mine tier: " + m_minToolTier);
					break;
				}
			}
			return true;
		}
		return false;
	}

	private bool AllDestroyed()
	{
		for (int i = 0; i < m_hitAreas.Count; i++)
		{
			if (m_hitAreas[i].m_health > 0f)
			{
				return false;
			}
		}
		return true;
	}

	private bool NonDestroyed()
	{
		for (int i = 0; i < m_hitAreas.Count; i++)
		{
			if (m_hitAreas[i].m_health <= 0f)
			{
				return false;
			}
		}
		return true;
	}

	private void RPC_SetAreaHealth(long sender, int index, float health)
	{
		HitArea hitArea = GetHitArea(index);
		if (hitArea != null)
		{
			hitArea.m_health = health;
		}
		UpdateMesh();
	}

	private int GetAreaIndex(Collider area)
	{
		for (int i = 0; i < m_hitAreas.Count; i++)
		{
			if (m_hitAreas[i].m_collider == area)
			{
				return i;
			}
		}
		return -1;
	}

	private HitArea GetHitArea(int index)
	{
		if (index < 0 || index >= m_hitAreas.Count)
		{
			return null;
		}
		return m_hitAreas[index];
	}

	private void UpdateSupport()
	{
		float realtimeSinceStartup = Time.realtimeSinceStartup;
		if (!m_haveSetupBounds)
		{
			SetupColliders();
			m_haveSetupBounds = true;
		}
		foreach (HitArea hitArea in m_hitAreas)
		{
			hitArea.m_supported = false;
		}
		Vector3 position = base.transform.position;
		for (int i = 0; i < 3; i++)
		{
			foreach (HitArea hitArea2 in m_hitAreas)
			{
				if (hitArea2.m_supported)
				{
					continue;
				}
				int num = Physics.OverlapBoxNonAlloc(position + hitArea2.m_bound.m_pos, hitArea2.m_bound.m_size, m_tempColliders, hitArea2.m_bound.m_rot, m_rayMask);
				for (int j = 0; j < num; j++)
				{
					Collider collider = m_tempColliders[j];
					if (!(collider == hitArea2.m_collider) && !(collider.attachedRigidbody != null) && !collider.isTrigger)
					{
						hitArea2.m_supported = hitArea2.m_supported || GetSupport(collider);
						if (hitArea2.m_supported)
						{
							break;
						}
					}
				}
			}
		}
		ZLog.Log("Suport time " + (Time.realtimeSinceStartup - realtimeSinceStartup) * 1000f);
	}

	private bool GetSupport(Collider c)
	{
		if (c.gameObject.layer == m_groundLayer)
		{
			return true;
		}
		IDestructible componentInParent = c.gameObject.GetComponentInParent<IDestructible>();
		if (componentInParent != null)
		{
			if (componentInParent == this)
			{
				foreach (HitArea hitArea in m_hitAreas)
				{
					if (hitArea.m_collider == c)
					{
						return hitArea.m_supported;
					}
				}
			}
			return c.transform.position.y < base.transform.position.y;
		}
		return true;
	}

	private void SetupColliders()
	{
		Vector3 position = base.transform.position;
		foreach (HitArea hitArea in m_hitAreas)
		{
			hitArea.m_bound.m_rot = Quaternion.identity;
			hitArea.m_bound.m_pos = hitArea.m_collider.bounds.center - position;
			hitArea.m_bound.m_size = hitArea.m_collider.bounds.size * 0.5f;
		}
	}
}
