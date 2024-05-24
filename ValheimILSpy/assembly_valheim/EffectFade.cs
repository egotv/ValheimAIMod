using UnityEngine;

public class EffectFade : MonoBehaviour
{
	public float m_fadeDuration = 1f;

	private ParticleSystem[] m_particles;

	private Light m_light;

	private AudioSource m_audioSource;

	private float m_baseVolume;

	private float m_lightBaseIntensity;

	private bool m_active = true;

	private float m_intensity;

	private void Awake()
	{
		m_particles = base.gameObject.GetComponentsInChildren<ParticleSystem>();
		m_light = base.gameObject.GetComponentInChildren<Light>();
		m_audioSource = base.gameObject.GetComponentInChildren<AudioSource>();
		if ((bool)m_light)
		{
			m_lightBaseIntensity = m_light.intensity;
			m_light.intensity = 0f;
		}
		if ((bool)m_audioSource)
		{
			m_baseVolume = m_audioSource.volume;
			m_audioSource.volume = 0f;
		}
		SetActive(active: false);
	}

	private void Update()
	{
		m_intensity = Mathf.MoveTowards(m_intensity, m_active ? 1f : 0f, Time.deltaTime / m_fadeDuration);
		if ((bool)m_light)
		{
			m_light.intensity = m_intensity * m_lightBaseIntensity;
			m_light.enabled = m_light.intensity > 0f;
		}
		if ((bool)m_audioSource)
		{
			m_audioSource.volume = m_intensity * m_baseVolume;
		}
	}

	public void SetActive(bool active)
	{
		if (m_active != active)
		{
			m_active = active;
			ParticleSystem[] particles = m_particles;
			for (int i = 0; i < particles.Length; i++)
			{
				ParticleSystem.EmissionModule emission = particles[i].emission;
				emission.enabled = active;
			}
		}
	}
}
