using UnityEngine;

public class OceanBubbleParticles : MonoBehaviour
{
	private ParticleSystem m_particleSystem;

	private ParticleSystem.Particle[] m_particles;

	private void Start()
	{
		m_particleSystem = GetComponent<ParticleSystem>();
	}

	private void Update()
	{
		if (m_particles == null)
		{
			m_particles = new ParticleSystem.Particle[m_particleSystem.main.maxParticles];
		}
		int particles = m_particleSystem.GetParticles(m_particles);
		for (int i = 0; i < particles; i++)
		{
			float liquidLevel = Floating.GetLiquidLevel(m_particles[i].position);
			Vector3 position = m_particles[i].position;
			position.y = liquidLevel;
			m_particles[i].position = position;
		}
		m_particleSystem.SetParticles(m_particles);
	}
}
