using UnityEngine;

public class StealthSystem : MonoBehaviour
{
	private static StealthSystem m_instance;

	public LayerMask m_shadowTestMask;

	public float m_minLightLevel = 0.2f;

	public float m_maxLightLevel = 1.6f;

	private Light[] m_allLights;

	private float m_lastLightListUpdate;

	private const float m_lightUpdateInterval = 1f;

	public static StealthSystem instance => m_instance;

	private void Awake()
	{
		m_instance = this;
	}

	private void OnDestroy()
	{
		m_instance = null;
	}

	public float GetLightFactor(Vector3 point)
	{
		float lightLevel = GetLightLevel(point);
		return Utils.LerpStep(m_minLightLevel, m_maxLightLevel, lightLevel);
	}

	public float GetLightLevel(Vector3 point)
	{
		if (Time.time - m_lastLightListUpdate > 1f)
		{
			m_lastLightListUpdate = Time.time;
			m_allLights = Object.FindObjectsOfType<Light>();
		}
		float num = RenderSettings.ambientIntensity * RenderSettings.ambientLight.grayscale;
		Light[] allLights = m_allLights;
		foreach (Light light in allLights)
		{
			if (light == null)
			{
				continue;
			}
			if (light.type == LightType.Directional)
			{
				float num2 = 1f;
				if (light.shadows != 0 && (Physics.Raycast(point - light.transform.forward * 1000f, light.transform.forward, 1000f, m_shadowTestMask) || Physics.Raycast(point, -light.transform.forward, 1000f, m_shadowTestMask)))
				{
					num2 = 1f - light.shadowStrength;
				}
				float num3 = light.intensity * light.color.grayscale * num2;
				num += num3;
				continue;
			}
			float num4 = Vector3.Distance(light.transform.position, point);
			if (num4 > light.range)
			{
				continue;
			}
			float num5 = 1f;
			if (light.shadows != 0)
			{
				Vector3 vector = point - light.transform.position;
				if (Physics.Raycast(light.transform.position, vector.normalized, vector.magnitude, m_shadowTestMask) || Physics.Raycast(point, -vector.normalized, vector.magnitude, m_shadowTestMask))
				{
					num5 = 1f - light.shadowStrength;
				}
			}
			float num6 = 1f - num4 / light.range;
			float num7 = light.intensity * light.color.grayscale * num6 * num5;
			num += num7;
		}
		return num;
	}
}
