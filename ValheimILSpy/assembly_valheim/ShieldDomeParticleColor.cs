using System;
using UnityEngine;

public class ShieldDomeParticleColor : MonoBehaviour
{
	[Serializable]
	public enum ColorMode
	{
		ClosestShieldWall,
		ClosestShieldGenerator
	}

	public ColorMode m_colorMode;

	public ParticleSystem[] m_particleSystems;

	private void Start()
	{
		Color domeColor = ShieldDomeImageEffect.GetDomeColor(ShieldGenerator.GetClosestShieldGenerator(base.transform.position, m_colorMode == ColorMode.ClosestShieldGenerator).GetFuelRatio());
		ParticleSystem[] particleSystems = m_particleSystems;
		foreach (ParticleSystem obj in particleSystems)
		{
			ParticleSystem.MainModule main = obj.main;
			domeColor.a = obj.main.startColor.color.a;
			main.startColor = domeColor;
		}
	}
}
