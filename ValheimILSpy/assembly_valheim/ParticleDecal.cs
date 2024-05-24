using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ParticleDecal : MonoBehaviour
{
	public ParticleSystem m_decalSystem;

	[Range(0f, 100f)]
	public float m_chance = 100f;

	private ParticleSystem part;

	private List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

	private void Awake()
	{
		part = GetComponent<ParticleSystem>();
		collisionEvents = new List<ParticleCollisionEvent>();
	}

	private void OnParticleCollision(GameObject other)
	{
		if (!(m_chance < 100f) || !(Random.Range(0f, 100f) > m_chance))
		{
			int num = part.GetCollisionEvents(other, collisionEvents);
			for (int i = 0; i < num; i++)
			{
				ParticleCollisionEvent particleCollisionEvent = collisionEvents[i];
				Vector3 eulerAngles = Quaternion.LookRotation(particleCollisionEvent.normal).eulerAngles;
				eulerAngles.x = 0f - eulerAngles.x + 180f;
				eulerAngles.y = 0f - eulerAngles.y;
				eulerAngles.z = Random.Range(0, 360);
				ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
				emitParams.position = particleCollisionEvent.intersection;
				emitParams.rotation3D = eulerAngles;
				emitParams.velocity = -particleCollisionEvent.normal * 0.001f;
				m_decalSystem.Emit(emitParams, 1);
			}
		}
	}
}
