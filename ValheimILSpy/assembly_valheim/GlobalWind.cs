using UnityEngine;

public class GlobalWind : MonoBehaviour
{
	public float m_multiplier = 1f;

	public bool m_smoothUpdate;

	public bool m_alignToWindDirection;

	[Header("Particles")]
	public bool m_particleVelocity = true;

	public bool m_particleForce;

	public bool m_particleEmission;

	public int m_particleEmissionMin;

	public int m_particleEmissionMax = 1;

	[Header("Cloth")]
	public float m_clothRandomAccelerationFactor = 0.5f;

	public bool m_checkPlayerShelter;

	private ParticleSystem m_ps;

	private Cloth m_cloth;

	private Player m_player;

	private void Start()
	{
		if (!(EnvMan.instance == null))
		{
			m_ps = GetComponent<ParticleSystem>();
			m_cloth = GetComponent<Cloth>();
			if (m_checkPlayerShelter)
			{
				m_player = GetComponentInParent<Player>();
			}
			if (m_smoothUpdate)
			{
				InvokeRepeating("UpdateWind", 0f, 0.01f);
				return;
			}
			InvokeRepeating("UpdateWind", Random.Range(1.5f, 2.5f), 2f);
			UpdateWind();
		}
	}

	private void UpdateWind()
	{
		if (m_alignToWindDirection)
		{
			Vector3 windDir = EnvMan.instance.GetWindDir();
			base.transform.rotation = Quaternion.LookRotation(windDir, Vector3.up);
		}
		if ((bool)m_ps)
		{
			if (!m_ps.emission.enabled)
			{
				return;
			}
			Vector3 windForce = EnvMan.instance.GetWindForce();
			if (m_particleVelocity)
			{
				ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = m_ps.velocityOverLifetime;
				velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
				velocityOverLifetime.x = windForce.x * m_multiplier;
				velocityOverLifetime.z = windForce.z * m_multiplier;
			}
			if (m_particleForce)
			{
				ParticleSystem.ForceOverLifetimeModule forceOverLifetime = m_ps.forceOverLifetime;
				forceOverLifetime.space = ParticleSystemSimulationSpace.World;
				forceOverLifetime.x = windForce.x * m_multiplier;
				forceOverLifetime.z = windForce.z * m_multiplier;
			}
			if (m_particleEmission)
			{
				ParticleSystem.EmissionModule emission = m_ps.emission;
				emission.rateOverTimeMultiplier = Mathf.Lerp(m_particleEmissionMin, m_particleEmissionMax, EnvMan.instance.GetWindIntensity());
			}
		}
		if ((bool)m_cloth)
		{
			Vector3 vector = EnvMan.instance.GetWindForce();
			if (m_checkPlayerShelter && m_player != null && m_player.InShelter())
			{
				vector = Vector3.zero;
			}
			m_cloth.externalAcceleration = vector * m_multiplier;
			m_cloth.randomAcceleration = vector * m_multiplier * m_clothRandomAccelerationFactor;
		}
	}

	public void UpdateClothReference(Cloth cloth)
	{
		m_cloth = cloth;
	}
}
