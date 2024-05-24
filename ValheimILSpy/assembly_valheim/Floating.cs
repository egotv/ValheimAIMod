using System.Collections.Generic;
using UnityEngine;

public class Floating : MonoBehaviour, IWaterInteractable, IMonoUpdater
{
	public float m_waterLevelOffset;

	public float m_forceDistance = 1f;

	public float m_force = 0.5f;

	public float m_balanceForceFraction = 0.02f;

	public float m_damping = 0.05f;

	public EffectList m_impactEffects = new EffectList();

	public GameObject m_surfaceEffects;

	private static int s_waterVolumeMask = 0;

	private static readonly Collider[] s_tempColliderArray = new Collider[256];

	private static readonly Dictionary<int, WaterVolume> s_waterVolumeCache = new Dictionary<int, WaterVolume>();

	private static readonly Dictionary<int, LiquidSurface> s_liquidSurfaceCache = new Dictionary<int, LiquidSurface>();

	private float m_waterLevel = -10000f;

	private float m_tarLevel = -10000f;

	private bool m_beenFloating;

	private bool m_wasInWater = true;

	private const float c_MinImpactEffectVelocity = 0.5f;

	private Rigidbody m_body;

	private Collider m_collider;

	private ZNetView m_nview;

	private readonly int[] m_liquids = new int[2];

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_nview = GetComponent<ZNetView>();
		m_body = GetComponent<Rigidbody>();
		m_collider = GetComponentInChildren<Collider>();
		SetSurfaceEffect(enabled: false);
		s_waterVolumeMask = LayerMask.GetMask("WaterVolume");
		InvokeRepeating("TerrainCheck", Random.Range(10f, 30f), 30f);
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public Transform GetTransform()
	{
		if (this == null)
		{
			return null;
		}
		return base.transform;
	}

	private void TerrainCheck()
	{
		if (!m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -1f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 1f;
			base.transform.position = position;
			Rigidbody component = GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.velocity = Vector3.zero;
			}
			ZLog.Log("Moved up item " + base.gameObject.name);
		}
	}

	public void CustomFixedUpdate(float fixedDeltaTime)
	{
		CheckBody();
		if (!m_body || !m_nview.IsValid() || !m_nview.IsOwner())
		{
			return;
		}
		if (!HaveLiquidLevel())
		{
			SetSurfaceEffect(enabled: false);
			return;
		}
		UpdateImpactEffect();
		float floatDepth = GetFloatDepth();
		if (floatDepth > 0f)
		{
			SetSurfaceEffect(enabled: false);
			return;
		}
		SetSurfaceEffect(enabled: true);
		Vector3 position = m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		float num = Mathf.Clamp01(Mathf.Abs(floatDepth) / m_forceDistance);
		Vector3 vector = m_force * num * (fixedDeltaTime * 50f) * Vector3.up;
		m_body.WakeUp();
		m_body.AddForceAtPosition(vector * m_balanceForceFraction, position, ForceMode.VelocityChange);
		m_body.AddForceAtPosition(vector, worldCenterOfMass, ForceMode.VelocityChange);
		m_body.velocity -= m_damping * num * m_body.velocity;
		m_body.angularVelocity -= m_damping * num * m_body.angularVelocity;
	}

	public bool HaveLiquidLevel()
	{
		if (!(m_waterLevel > -10000f))
		{
			return m_tarLevel > -10000f;
		}
		return true;
	}

	private void SetSurfaceEffect(bool enabled)
	{
		if (m_surfaceEffects != null)
		{
			m_surfaceEffects.SetActive(enabled);
		}
	}

	private void UpdateImpactEffect()
	{
		CheckBody();
		if (!m_body || m_body.IsSleeping() || !m_impactEffects.HasEffects())
		{
			return;
		}
		Vector3 vector = m_collider.ClosestPoint(base.transform.position + Vector3.down * 1000f);
		float num = Mathf.Max(m_waterLevel, m_tarLevel);
		if (vector.y < num)
		{
			if (!m_wasInWater)
			{
				m_wasInWater = true;
				Vector3 basePos = vector;
				basePos.y = num;
				if (m_body.GetPointVelocity(vector).magnitude > 0.5f)
				{
					m_impactEffects.Create(basePos, Quaternion.identity);
				}
			}
		}
		else
		{
			m_wasInWater = false;
		}
	}

	private float GetFloatDepth()
	{
		CheckBody();
		if (!m_body)
		{
			return 0f;
		}
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		float num = Mathf.Max(m_waterLevel, m_tarLevel);
		return worldCenterOfMass.y - num - m_waterLevelOffset;
	}

	public bool IsInTar()
	{
		CheckBody();
		if (m_tarLevel <= -10000f)
		{
			return false;
		}
		return m_body.worldCenterOfMass.y - m_tarLevel - m_waterLevelOffset < -0.2f;
	}

	public void SetLiquidLevel(float level, LiquidType type, Component liquidObj)
	{
		if (type == LiquidType.Water || type == LiquidType.Tar)
		{
			if (type == LiquidType.Water)
			{
				m_waterLevel = level;
			}
			else
			{
				m_tarLevel = level;
			}
			if (!m_beenFloating && level > -10000f && GetFloatDepth() < 0f)
			{
				m_beenFloating = true;
			}
		}
	}

	private void CheckBody()
	{
		if (!m_body)
		{
			m_body = FloatingTerrain.GetBody(base.gameObject);
		}
	}

	public bool BeenFloating()
	{
		return m_beenFloating;
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.blue;
		Gizmos.DrawWireCube(base.transform.position + Vector3.down * m_waterLevelOffset, new Vector3(1f, 0.05f, 1f));
	}

	public static float GetLiquidLevel(Vector3 p, float waveFactor = 1f, LiquidType type = LiquidType.All)
	{
		if (s_waterVolumeMask == 0)
		{
			s_waterVolumeMask = LayerMask.GetMask("WaterVolume");
		}
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, s_tempColliderArray, s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			if (!s_waterVolumeCache.TryGetValue(instanceID, out var value))
			{
				value = collider.GetComponent<WaterVolume>();
				s_waterVolumeCache[instanceID] = value;
			}
			if ((bool)value)
			{
				if (type == LiquidType.All || value.GetLiquidType() == type)
				{
					num = Mathf.Max(num, value.GetWaterSurface(p, waveFactor));
				}
				continue;
			}
			if (!s_liquidSurfaceCache.TryGetValue(instanceID, out var value2))
			{
				value2 = collider.GetComponent<LiquidSurface>();
				s_liquidSurfaceCache[instanceID] = value2;
			}
			if ((bool)value2 && (type == LiquidType.All || value2.GetLiquidType() == type))
			{
				num = Mathf.Max(num, value2.GetSurface(p));
			}
		}
		return num;
	}

	public static float GetWaterLevel(Vector3 p, ref WaterVolume previousAndOut)
	{
		if (previousAndOut != null && previousAndOut.gameObject.GetComponent<Collider>().bounds.Contains(p))
		{
			return previousAndOut.GetWaterSurface(p);
		}
		float num = -10000f;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, s_tempColliderArray, s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			if (!s_waterVolumeCache.TryGetValue(instanceID, out var value))
			{
				value = collider.GetComponent<WaterVolume>();
				s_waterVolumeCache[instanceID] = value;
			}
			if ((bool)value)
			{
				if (value.GetLiquidType() == LiquidType.Water)
				{
					float waterSurface = value.GetWaterSurface(p);
					if (waterSurface > num)
					{
						num = waterSurface;
						previousAndOut = value;
					}
				}
			}
			else
			{
				if (!s_liquidSurfaceCache.TryGetValue(instanceID, out var value2))
				{
					value2 = collider.GetComponent<LiquidSurface>();
					s_liquidSurfaceCache[instanceID] = value2;
				}
				if ((bool)value2 && value2.GetLiquidType() == LiquidType.Water)
				{
					num = Mathf.Max(num, value2.GetSurface(p));
				}
			}
		}
		return num;
	}

	public static bool IsUnderWater(Vector3 p, ref WaterVolume previousAndOut)
	{
		if (previousAndOut != null && previousAndOut.gameObject.GetComponent<Collider>().bounds.Contains(p))
		{
			return previousAndOut.GetWaterSurface(p) > p.y;
		}
		float num = -10000f;
		previousAndOut = null;
		int num2 = Physics.OverlapSphereNonAlloc(p, 0f, s_tempColliderArray, s_waterVolumeMask);
		for (int i = 0; i < num2; i++)
		{
			Collider collider = s_tempColliderArray[i];
			int instanceID = collider.GetInstanceID();
			if (!s_waterVolumeCache.TryGetValue(instanceID, out var value))
			{
				value = collider.GetComponent<WaterVolume>();
				s_waterVolumeCache[instanceID] = value;
			}
			if ((bool)value)
			{
				if (value.GetLiquidType() == LiquidType.Water)
				{
					float waterSurface = value.GetWaterSurface(p);
					if (waterSurface > num)
					{
						num = waterSurface;
						previousAndOut = value;
					}
				}
			}
			else
			{
				if (!s_liquidSurfaceCache.TryGetValue(instanceID, out var value2))
				{
					value2 = collider.GetComponent<LiquidSurface>();
					s_liquidSurfaceCache[instanceID] = value2;
				}
				if ((bool)value2 && value2.GetLiquidType() == LiquidType.Water)
				{
					num = Mathf.Max(num, value2.GetSurface(p));
				}
			}
		}
		return num > p.y;
	}

	public int Increment(LiquidType type)
	{
		return ++m_liquids[(int)type];
	}

	public int Decrement(LiquidType type)
	{
		return --m_liquids[(int)type];
	}
}
