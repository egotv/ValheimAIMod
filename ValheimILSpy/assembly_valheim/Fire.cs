using System.Collections.Generic;
using UnityEngine;

public class Fire : MonoBehaviour
{
	private static Collider[] m_colliders = new Collider[128];

	private static List<KeyValuePair<IDestructible, Collider>> m_destructibles = new List<KeyValuePair<IDestructible, Collider>>();

	public float m_dotInterval = 1f;

	public float m_dotRadius = 1f;

	public float m_fireDamage = 10f;

	public float m_chopDamage = 10f;

	public short m_toolTier = 2;

	public int m_spread = 4;

	public float m_updateRate = 2f;

	[Header("Terrain hit")]
	public float m_terrainHitDelay;

	public float m_terrainMaxDist;

	public bool m_terrainCheckCultivated;

	public bool m_terrainCheckCleared;

	public GameObject m_terrainHitSpawn;

	public Heightmap.Biome m_terrainHitBiomes = Heightmap.Biome.All;

	[Header("Burn fuel from fireplaces")]
	public float m_fuelBurnChance = 0.5f;

	public float m_fuelBurnAmount = 0.1f;

	[Header("Smoke")]
	public SmokeSpawner m_smokeSpawner;

	public float m_smokeCheckHeight = 0.25f;

	public float m_smokeCheckRadius = 0.5f;

	public float m_smokeOxygenCheckHeight = 1.25f;

	public float m_smokeOxygenCheckRadius = 1.5f;

	public float m_smokeSuffocationPerHit = 0.2f;

	public int m_oxygenSmokeTolerance = 2;

	public int m_oxygenInteriorChecks = 5;

	public float m_smokeDieChance = 0.5f;

	public float m_maxSmoke = 3f;

	[Header("Effects")]
	public EffectList m_hitEffect;

	private static int s_dotMask = 0;

	private static int s_solidMask = 0;

	private static int s_terrainMask = 0;

	private static int s_smokeRayMask = 0;

	private static readonly RaycastHit[] s_raycastHits = new RaycastHit[32];

	private static readonly Collider[] s_hits = new Collider[32];

	private int m_smokeHits;

	private bool m_inSmoke;

	private GameObject m_roof;

	private ZNetView m_nview;

	private float m_suffocating;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (s_dotMask == 0)
		{
			s_dotMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
			s_solidMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece");
			s_terrainMask = LayerMask.GetMask("terrain");
			s_smokeRayMask = LayerMask.GetMask("smoke");
		}
		InvokeRepeating("Dot", m_dotInterval, m_dotInterval);
		if ((bool)m_terrainHitSpawn && (m_terrainHitBiomes == Heightmap.Biome.All || m_terrainHitBiomes.HasFlag(WorldGenerator.instance.GetBiome(base.transform.position))))
		{
			Invoke("HitTerrain", m_terrainHitDelay);
		}
		InvokeRepeating("UpdateFire", Random.Range(m_updateRate / 2f, m_updateRate), m_updateRate);
	}

	private void Dot()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		m_destructibles.Clear();
		int num = Physics.OverlapSphereNonAlloc(base.transform.position, m_dotRadius, m_colliders, s_dotMask);
		for (int i = 0; i < num; i++)
		{
			GameObject gameObject = Projectile.FindHitObject(m_colliders[i]);
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (Random.Range(0f, 1f) < m_fuelBurnChance)
			{
				gameObject.GetComponent<Fireplace>()?.AddFuel(0f - m_fuelBurnAmount);
			}
			if ((bool)gameObject.GetComponent<Character>())
			{
				DoDamage(component, m_colliders[i]);
				continue;
			}
			WearNTear component2 = gameObject.GetComponent<WearNTear>();
			if (((object)component2 == null || component2.m_burnable) && component != null)
			{
				m_destructibles.Add(new KeyValuePair<IDestructible, Collider>(component, m_colliders[i]));
			}
		}
		if (m_destructibles.Count > 0)
		{
			KeyValuePair<IDestructible, Collider> keyValuePair = m_destructibles[Random.Range(0, m_destructibles.Count)];
			DoDamage(keyValuePair.Key, keyValuePair.Value);
		}
	}

	private void DoDamage(IDestructible toHit, Collider collider)
	{
		HitData hitData = new HitData();
		hitData.m_hitCollider = collider;
		hitData.m_damage.m_fire = m_fireDamage;
		hitData.m_damage.m_chop = m_chopDamage;
		hitData.m_toolTier = m_toolTier;
		hitData.m_point = (base.transform.position + collider.bounds.center) * 0.5f;
		hitData.m_dodgeable = false;
		hitData.m_blockable = false;
		hitData.m_hitType = HitData.HitType.CinderFire;
		m_hitEffect.Create(hitData.m_point, Quaternion.identity);
		toHit.Damage(hitData);
	}

	private void HitTerrain()
	{
		if (Physics.Raycast(base.transform.position, Vector3.down, out var hitInfo, m_terrainMaxDist, s_terrainMask))
		{
			Heightmap component = hitInfo.collider.GetComponent<Heightmap>();
			if ((object)component != null && !component.IsLava(hitInfo.point) && ((m_terrainCheckCultivated && !component.IsCultivated(hitInfo.point)) || (m_terrainCheckCleared && !component.IsCleared(hitInfo.point)) || (!m_terrainCheckCleared && !m_terrainCheckCultivated)))
			{
				Object.Instantiate(m_terrainHitSpawn, hitInfo.point, Quaternion.identity);
			}
		}
	}

	private void UpdateFire()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (!m_roof)
		{
			WearNTear.RoofCheck(base.transform.position, out m_roof);
		}
		if (!m_roof && EnvMan.IsWet())
		{
			ZNetScene.instance.Destroy(base.gameObject);
		}
		if ((bool)m_roof)
		{
			m_smokeHits = Physics.OverlapSphereNonAlloc(base.transform.position + Vector3.up * m_smokeOxygenCheckHeight, m_smokeOxygenCheckRadius, s_hits, s_smokeRayMask);
			m_smokeHits -= m_oxygenSmokeTolerance;
			if (m_smokeHits > 0)
			{
				m_suffocating += (float)m_smokeHits * m_smokeSuffocationPerHit;
				Terminal.Log($"Fire suffocation in interior with {m_smokeHits} smoke hits");
			}
			else
			{
				m_suffocating = Mathf.Max(0f, m_suffocating - 1f);
			}
		}
		else
		{
			m_inSmoke = Physics.CheckSphere(base.transform.position + Vector3.up * m_smokeCheckHeight, m_smokeCheckRadius, s_smokeRayMask);
			if (m_inSmoke)
			{
				m_suffocating += 1f;
				Terminal.Log("Fire in direct smoke");
			}
			else
			{
				m_suffocating = Mathf.Max(0f, m_suffocating - 1f);
			}
		}
		if (m_suffocating >= m_maxSmoke && (m_smokeDieChance >= 1f || Random.Range(0f, 1f) < m_smokeDieChance))
		{
			Terminal.Log("Fire suffocated");
			ZNetScene.instance.Destroy(base.gameObject);
		}
	}
}
