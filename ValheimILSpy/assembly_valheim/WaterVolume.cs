using System.Collections.Generic;
using UnityEngine;

public class WaterVolume : MonoBehaviour
{
	private Collider m_collider;

	private readonly float[] m_normalizedDepth = new float[4];

	private readonly List<IWaterInteractable> m_inWater = new List<IWaterInteractable>();

	public MeshRenderer m_waterSurface;

	public Heightmap m_heightmap;

	public float m_forceDepth = -1f;

	public float m_surfaceOffset;

	public bool m_useGlobalWind = true;

	private const bool c_MenuWater = false;

	private static float s_waterTime = 0f;

	private static readonly int s_shaderWaterTime = Shader.PropertyToID("_WaterTime");

	private static readonly int s_shaderDepth = Shader.PropertyToID("_depth");

	private static readonly int s_shaderUseGlobalWind = Shader.PropertyToID("_UseGlobalWind");

	private static Vector4 s_globalWind1 = new Vector4(1f, 0f, 0f, 0f);

	private static Vector4 s_globalWind2 = new Vector4(1f, 0f, 0f, 0f);

	private static float s_globalWindAlpha = 0f;

	private static float s_wrappedDayTimeSeconds = 0f;

	private static readonly List<int> s_inWaterRemoveIndices = new List<int>();

	private static readonly Vector2[] s_createWaveDirections = new Vector2[10]
	{
		new Vector2(1.0312f, 0.312f).normalized,
		new Vector2(1.0312f, 0.312f).normalized,
		new Vector2(-0.123f, 1.12f).normalized,
		new Vector2(0.423f, 0.124f).normalized,
		new Vector2(0.123f, -0.64f).normalized,
		new Vector2(-0.523f, -0.64f).normalized,
		new Vector2(0.223f, 0.74f).normalized,
		new Vector2(0.923f, -0.24f).normalized,
		new Vector2(-0.323f, 0.44f).normalized,
		new Vector2(0.5312f, -0.812f).normalized
	};

	private static Vector2[] s_createWaveTangents = null;

	public static List<WaterVolume> Instances { get; } = new List<WaterVolume>();


	private void Awake()
	{
		m_collider = GetComponent<Collider>();
		if (s_createWaveTangents == null)
		{
			s_createWaveTangents = new Vector2[10]
			{
				new Vector2(0f - s_createWaveDirections[0].y, s_createWaveDirections[0].x),
				new Vector2(0f - s_createWaveDirections[1].y, s_createWaveDirections[1].x),
				new Vector2(0f - s_createWaveDirections[2].y, s_createWaveDirections[2].x),
				new Vector2(0f - s_createWaveDirections[3].y, s_createWaveDirections[3].x),
				new Vector2(0f - s_createWaveDirections[4].y, s_createWaveDirections[4].x),
				new Vector2(0f - s_createWaveDirections[5].y, s_createWaveDirections[5].x),
				new Vector2(0f - s_createWaveDirections[6].y, s_createWaveDirections[6].x),
				new Vector2(0f - s_createWaveDirections[7].y, s_createWaveDirections[7].x),
				new Vector2(0f - s_createWaveDirections[8].y, s_createWaveDirections[8].x),
				new Vector2(0f - s_createWaveDirections[9].y, s_createWaveDirections[9].x)
			};
		}
	}

	private void Start()
	{
		DetectWaterDepth();
		SetupMaterial();
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	private void DetectWaterDepth()
	{
		if ((bool)m_heightmap)
		{
			float[] oceanDepth = m_heightmap.GetOceanDepth();
			m_normalizedDepth[0] = Mathf.Clamp01(oceanDepth[0] / 10f);
			m_normalizedDepth[1] = Mathf.Clamp01(oceanDepth[1] / 10f);
			m_normalizedDepth[2] = Mathf.Clamp01(oceanDepth[2] / 10f);
			m_normalizedDepth[3] = Mathf.Clamp01(oceanDepth[3] / 10f);
		}
		else
		{
			m_normalizedDepth[0] = m_forceDepth;
			m_normalizedDepth[1] = m_forceDepth;
			m_normalizedDepth[2] = m_forceDepth;
			m_normalizedDepth[3] = m_forceDepth;
		}
	}

	public static void StaticUpdate()
	{
		UpdateWaterTime(Time.deltaTime);
		if ((bool)EnvMan.instance)
		{
			EnvMan.instance.GetWindData(out s_globalWind1, out s_globalWind2, out s_globalWindAlpha);
		}
	}

	private static void UpdateWaterTime(float dt)
	{
		s_wrappedDayTimeSeconds = ZNet.instance.GetWrappedDayTimeSeconds();
		float num = s_wrappedDayTimeSeconds;
		s_waterTime += dt;
		if (Mathf.Abs(num - s_waterTime) > 10f)
		{
			s_waterTime = num;
		}
		s_waterTime = Mathf.Lerp(s_waterTime, num, 0.05f);
	}

	public void UpdateMaterials()
	{
		m_waterSurface.material.SetFloat(s_shaderWaterTime, s_waterTime);
	}

	private void SetupMaterial()
	{
		if (m_forceDepth >= 0f)
		{
			m_waterSurface.material.SetFloatArray(s_shaderDepth, new float[4] { m_forceDepth, m_forceDepth, m_forceDepth, m_forceDepth });
		}
		else
		{
			m_waterSurface.material.SetFloatArray(s_shaderDepth, m_normalizedDepth);
		}
		m_waterSurface.material.SetFloat(s_shaderUseGlobalWind, m_useGlobalWind ? 1f : 0f);
	}

	public LiquidType GetLiquidType()
	{
		return LiquidType.Water;
	}

	public float GetWaterSurface(Vector3 point, float waveFactor = 1f)
	{
		float num = 0f;
		if (m_useGlobalWind)
		{
			float waterTime = s_wrappedDayTimeSeconds;
			float num2 = Depth(point);
			num = ((num2 == 0f) ? 0f : CalcWave(point, num2, waterTime, waveFactor));
		}
		float num3 = base.transform.position.y + num + m_surfaceOffset;
		if (m_forceDepth < 0f && Utils.LengthXZ(point) > 10500f)
		{
			num3 -= 100f;
		}
		return num3;
	}

	private float TrochSin(float x, float k)
	{
		return Mathf.Sin(x - Mathf.Cos(x) * k) * 0.5f + 0.5f;
	}

	private float CreateWave(Vector3 worldPos, float time, float waveSpeed, float waveLength, float waveHeight, Vector2 dir, Vector2 tangent, float sharpness)
	{
		Vector2 vector = -(worldPos.z * dir + worldPos.x * tangent);
		float num = time * waveSpeed;
		return (TrochSin(num + vector.y * waveLength, sharpness) * TrochSin(num * 0.123f + vector.x * 0.13123f * waveLength, sharpness) - 0.2f) * waveHeight;
	}

	private float CalcWave(Vector3 worldPos, float depth, Vector4 wind, float waterTime, float waveFactor)
	{
		s_createWaveDirections[0].x = wind.x;
		s_createWaveDirections[0].y = wind.z;
		s_createWaveDirections[0].Normalize();
		s_createWaveTangents[0].x = 0f - s_createWaveDirections[0].y;
		s_createWaveTangents[0].y = s_createWaveDirections[0].x;
		float w = wind.w;
		float num = Mathf.Lerp(0f, w, depth);
		float time = waterTime / 20f;
		float num2 = CreateWave(worldPos, time, 10f, 0.04f, 8f, s_createWaveDirections[0], s_createWaveTangents[0], 0.5f);
		float num3 = CreateWave(worldPos, time, 14.123f, 0.08f, 6f, s_createWaveDirections[1], s_createWaveTangents[1], 0.5f);
		float num4 = CreateWave(worldPos, time, 22.312f, 0.1f, 4f, s_createWaveDirections[2], s_createWaveTangents[2], 0.5f);
		float num5 = CreateWave(worldPos, time, 31.42f, 0.2f, 2f, s_createWaveDirections[3], s_createWaveTangents[3], 0.5f);
		float num6 = CreateWave(worldPos, time, 35.42f, 0.4f, 1f, s_createWaveDirections[4], s_createWaveTangents[4], 0.5f);
		float num7 = CreateWave(worldPos, time, 38.1223f, 1f, 0.8f, s_createWaveDirections[5], s_createWaveTangents[5], 0.7f);
		float num8 = CreateWave(worldPos, time, 41.1223f, 1.2f, 0.6f * waveFactor, s_createWaveDirections[6], s_createWaveTangents[6], 0.8f);
		float num9 = CreateWave(worldPos, time, 51.5123f, 1.3f, 0.4f * waveFactor, s_createWaveDirections[7], s_createWaveTangents[7], 0.9f);
		float num10 = CreateWave(worldPos, time, 54.2f, 1.3f, 0.3f * waveFactor, s_createWaveDirections[8], s_createWaveTangents[8], 0.9f);
		float num11 = CreateWave(worldPos, time, 56.123f, 1.5f, 0.2f * waveFactor, s_createWaveDirections[9], s_createWaveTangents[9], 0.9f);
		return (num2 + num3 + num4 + num5 + num6 + num7 + num8 + num9 + num10 + num11) * num;
	}

	public float CalcWave(Vector3 worldPos, float depth, float waterTime, float waveFactor)
	{
		if (s_globalWindAlpha == 0f)
		{
			return CalcWave(worldPos, depth, s_globalWind1, waterTime, waveFactor);
		}
		float a = CalcWave(worldPos, depth, s_globalWind1, waterTime, waveFactor);
		float b = CalcWave(worldPos, depth, s_globalWind2, waterTime, waveFactor);
		return Mathf.LerpUnclamped(a, b, s_globalWindAlpha);
	}

	public float Depth(Vector3 point)
	{
		Vector3 vector = base.transform.InverseTransformPoint(point);
		Vector3 size = m_collider.bounds.size;
		float t = (vector.x + size.x / 2f) / size.x;
		float t2 = (vector.z + size.z / 2f) / size.z;
		float a = Mathf.Lerp(m_normalizedDepth[3], m_normalizedDepth[2], t);
		float b = Mathf.Lerp(m_normalizedDepth[0], m_normalizedDepth[1], t);
		return Mathf.Lerp(a, b, t2);
	}

	private void OnTriggerEnter(Collider triggerCollider)
	{
		IWaterInteractable component = triggerCollider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			component.Increment(LiquidType.Water);
			if (!m_inWater.Contains(component))
			{
				m_inWater.Add(component);
			}
		}
	}

	public void UpdateFloaters()
	{
		int count = m_inWater.Count;
		if (count == 0)
		{
			return;
		}
		s_inWaterRemoveIndices.Clear();
		for (int i = 0; i < count; i++)
		{
			IWaterInteractable waterInteractable = m_inWater[i];
			if (waterInteractable == null)
			{
				s_inWaterRemoveIndices.Add(i);
				continue;
			}
			Transform transform = waterInteractable.GetTransform();
			if ((bool)transform)
			{
				float waterSurface = GetWaterSurface(transform.position);
				waterInteractable.SetLiquidLevel(waterSurface, LiquidType.Water, this);
			}
			else
			{
				s_inWaterRemoveIndices.Add(i);
			}
		}
		for (int num = s_inWaterRemoveIndices.Count - 1; num >= 0; num--)
		{
			m_inWater.RemoveAt(s_inWaterRemoveIndices[num]);
		}
	}

	private void OnTriggerExit(Collider triggerCollider)
	{
		IWaterInteractable component = triggerCollider.attachedRigidbody.GetComponent<IWaterInteractable>();
		if (component != null)
		{
			if (component.Decrement(LiquidType.Water) == 0)
			{
				component.SetLiquidLevel(-10000f, LiquidType.Water, this);
			}
			m_inWater.Remove(component);
		}
	}

	private void OnDestroy()
	{
		foreach (IWaterInteractable item in m_inWater)
		{
			if (item != null && item.Decrement(LiquidType.Water) == 0)
			{
				item.SetLiquidLevel(-10000f, LiquidType.Water, this);
			}
		}
		m_inWater.Clear();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireCube(base.transform.position + Vector3.up * m_surfaceOffset, new Vector3(2f, 0.05f, 2f));
	}
}
