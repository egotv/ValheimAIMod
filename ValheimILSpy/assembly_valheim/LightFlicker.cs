using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class LightFlicker : MonoBehaviour, IMonoUpdater
{
	public enum LightFlashSettings
	{
		[InspectorName("Unchanged")]
		Default,
		[InspectorName("Remove Flicker, Keep Fade")]
		OnIncludeFade,
		[InspectorName("Disable light")]
		Off,
		AlwaysOn,
		SmoothedFlicker
	}

	public float m_flickerIntensity = 0.1f;

	public float m_flickerSpeed = 10f;

	public float m_movement = 0.1f;

	public float m_ttl;

	public float m_fadeDuration = 0.2f;

	public float m_fadeInDuration;

	[FormerlySerializedAs("m_flashingLightingsAccessibility")]
	[Header("Accessibility")]
	public LightFlashSettings m_flashingLightsSetting;

	[Range(0f, 1f)]
	public float m_accessibilityBrightnessMultiplier = 1f;

	private Light m_light;

	private float m_baseIntensity = 1f;

	private Vector3 m_basePosition = Vector3.zero;

	private float m_time;

	private float m_flickerOffset;

	private float m_smoothedIntensity;

	private float m_targetIntensity;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Awake()
	{
		m_light = GetComponent<Light>();
		m_baseIntensity = m_light.intensity;
		m_basePosition = base.transform.localPosition;
		m_flickerOffset = Random.Range(0f, 10f);
		if (Settings.ReduceFlashingLights)
		{
			m_light.intensity = 0f;
		}
	}

	private void OnEnable()
	{
		m_time = 0f;
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		if (!m_light)
		{
			return;
		}
		if (Settings.ReduceFlashingLights)
		{
			if (m_flashingLightsSetting == LightFlashSettings.Off)
			{
				m_light.intensity = 0f;
				return;
			}
			if (m_flashingLightsSetting == LightFlashSettings.AlwaysOn)
			{
				m_light.intensity = 1f;
				return;
			}
		}
		m_time += deltaTime;
		float num = m_flickerOffset + Time.time * m_flickerSpeed;
		if (Settings.ReduceFlashingLights && m_flashingLightsSetting == LightFlashSettings.OnIncludeFade)
		{
			m_targetIntensity = 1f;
		}
		else
		{
			m_targetIntensity = 1f + Mathf.Sin(num) * Mathf.Sin(num * 0.56436f) * Mathf.Cos(num * 0.758348f) * m_flickerIntensity;
		}
		if (m_fadeInDuration > 0f)
		{
			m_targetIntensity *= Utils.LerpStep(0f, m_fadeInDuration, m_time);
		}
		if (m_ttl > 0f)
		{
			if (m_time > m_ttl)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			float l = m_ttl - m_fadeDuration;
			m_targetIntensity *= 1f - Utils.LerpStep(l, m_ttl, m_time);
		}
		if (Settings.ReduceFlashingLights && m_flashingLightsSetting == LightFlashSettings.SmoothedFlicker)
		{
			float h = ((m_time > m_ttl - m_fadeDuration && m_ttl > 0f) ? 0.075f : 0.15f);
			m_smoothedIntensity = Utils.LerpSmooth(m_smoothedIntensity, m_targetIntensity, deltaTime, h);
		}
		else
		{
			m_smoothedIntensity = m_targetIntensity;
		}
		float num2 = (Settings.ReduceFlashingLights ? m_accessibilityBrightnessMultiplier : 1f);
		m_light.intensity = m_baseIntensity * m_smoothedIntensity * num2;
		Vector3 vector = new Vector3(Mathf.Sin(num) * Mathf.Sin(num * 0.56436f), Mathf.Sin(num * 0.56436f) * Mathf.Sin(num * 0.688742f), Mathf.Cos(num * 0.758348f) * Mathf.Cos(num * 0.4563696f)) * m_movement;
		base.transform.localPosition = m_basePosition + vector;
	}
}
