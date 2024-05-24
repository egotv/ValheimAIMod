using UnityEngine;

public class Cinder : MonoBehaviour
{
	public GameObject m_firePrefab;

	public GameObject m_houseFirePrefab;

	public float m_gravity = 10f;

	public float m_drag;

	public float m_windStrength;

	public int m_spread = 4;

	[Range(0f, 1f)]
	public float m_chanceToIgniteGrass = 0.1f;

	public EffectList m_hitEffects;

	private Vector3 m_vel;

	private static int m_raymask;

	private ZNetView m_nview;

	private bool m_haveHit;

	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		if (m_raymask == 0)
		{
			m_raymask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "piece_nonsolid", "terrain", "character", "character_net", "character_ghost", "hitbox", "character_noenv", "vehicle");
		}
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			Vector3 position = base.transform.position;
			position -= EnvMan.instance.GetWindForce() * m_windStrength * 10f;
			base.transform.position = position;
		}
	}

	private void FixedUpdate()
	{
		if (!m_haveHit && m_nview.IsValid() && m_nview.IsOwner())
		{
			float fixedDeltaTime = Time.fixedDeltaTime;
			m_vel += EnvMan.instance.GetWindForce() * (fixedDeltaTime * m_windStrength);
			m_vel += Vector3.down * (m_gravity * fixedDeltaTime);
			float num = Mathf.Pow(m_vel.magnitude, 2f) * m_drag * Time.fixedDeltaTime;
			m_vel += num * -m_vel.normalized;
			Vector3 position = base.transform.position;
			Vector3 vector = position + m_vel * fixedDeltaTime;
			base.transform.position = vector;
			if (Physics.Raycast(position, m_vel.normalized, out var hitInfo, Vector3.Distance(position, vector), m_raymask))
			{
				OnHit(hitInfo.collider, hitInfo.point, hitInfo.normal);
			}
			ShieldGenerator.CheckObjectInsideShield(this);
		}
	}

	private void OnHit(Collider collider, Vector3 point, Vector3 normal)
	{
		m_hitEffects.Create(point, Quaternion.identity);
		if (CanBurn(collider, point, out var isTerrain, m_chanceToIgniteGrass))
		{
			GameObject gameObject = ((!isTerrain) ? Object.Instantiate(m_houseFirePrefab, point + normal * 0.1f, Quaternion.identity) : Object.Instantiate(m_firePrefab, point + normal * 0.1f, Quaternion.identity));
			gameObject.GetComponent<CinderSpawner>()?.Setup(GetSpread(), collider.gameObject);
		}
		m_haveHit = true;
		base.transform.position = point;
		InvokeRepeating("DestroyNow", 0.25f, 1f);
	}

	private void OnShieldHit()
	{
	}

	private void DestroyNow()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_nview.Destroy();
		}
	}

	public static bool CanBurn(Collider collider, Vector3 point, out bool isTerrain, float chanceToIgniteGrass = 0f)
	{
		isTerrain = false;
		if (point.y < 30f)
		{
			return false;
		}
		if (Floating.GetLiquidLevel(point) > point.y)
		{
			return false;
		}
		Piece componentInParent = collider.gameObject.GetComponentInParent<Piece>();
		if ((object)componentInParent != null && Player.IsPlacementGhost(componentInParent.gameObject))
		{
			return false;
		}
		WearNTear componentInParent2 = collider.gameObject.GetComponentInParent<WearNTear>();
		if ((object)componentInParent2 != null)
		{
			if (componentInParent2.m_burnable && !componentInParent2.IsWet())
			{
				return true;
			}
		}
		else
		{
			if ((object)collider.gameObject.GetComponentInParent<TreeBase>() != null)
			{
				return true;
			}
			if ((object)collider.gameObject.GetComponentInParent<TreeLog>() != null)
			{
				return true;
			}
		}
		if (EnvMan.IsWet())
		{
			return false;
		}
		if (chanceToIgniteGrass > 0f)
		{
			Heightmap component = collider.GetComponent<Heightmap>();
			if ((bool)component)
			{
				if (component.IsCleared(point))
				{
					return false;
				}
				Heightmap.Biome biome = component.GetBiome(point);
				if (biome == Heightmap.Biome.Mountain || biome == Heightmap.Biome.DeepNorth)
				{
					return false;
				}
				isTerrain = true;
				return Random.value <= chanceToIgniteGrass;
			}
		}
		return false;
	}

	public void Setup(Vector3 vel, int spread)
	{
		m_vel = vel;
		m_nview.GetZDO().Set(ZDOVars.s_spread, spread);
	}

	private int GetSpread()
	{
		return m_nview.GetZDO().GetInt(ZDOVars.s_spread, m_spread);
	}
}
