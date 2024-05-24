using System.Collections.Generic;
using UnityEngine;

public class DynamicParticleReduction : MonoBehaviour
{
	[SerializeField]
	private List<ParticleSystem> affectedParticleSystems;

	private Dictionary<ParticleSystem, bool> originalCollisionStates = new Dictionary<ParticleSystem, bool>();

	private void Awake()
	{
		SaveOriginalStates();
		ApplySettings();
	}

	public void SaveOriginalStates()
	{
		foreach (ParticleSystem affectedParticleSystem in affectedParticleSystems)
		{
			ParticleSystem component = affectedParticleSystem.GetComponent<ParticleSystem>();
			if (!originalCollisionStates.ContainsKey(component))
			{
				ParticleSystem.CollisionModule collision = component.collision;
				originalCollisionStates[component] = collision.enabled;
			}
		}
	}

	public void ApplySettings()
	{
		if (PlatformPrefs.GetInt("DetailedParticleSystems", 1) == 0)
		{
			for (int i = 0; i < affectedParticleSystems.Count; i++)
			{
				ParticleSystem.CollisionModule collision = affectedParticleSystems[i].collision;
				collision.enabled = false;
			}
		}
		else
		{
			RestoreOriginalStates();
		}
	}

	public void RestoreOriginalStates()
	{
		foreach (KeyValuePair<ParticleSystem, bool> originalCollisionState in originalCollisionStates)
		{
			ParticleSystem key = originalCollisionState.Key;
			bool value = originalCollisionState.Value;
			ParticleSystem.CollisionModule collision = key.collision;
			collision.enabled = value;
		}
	}
}
