using System.Collections.Generic;
using UnityEngine;

public class WaterTrigger : MonoBehaviour, IMonoUpdater
{
	public EffectList m_effects = new EffectList();

	public float m_cooldownDelay = 2f;

	private float m_cooldownTimer;

	private WaterVolume m_previousAndOut;

	public static List<IMonoUpdater> Instances { get; } = new List<IMonoUpdater>();


	private void Start()
	{
		m_cooldownTimer = Random.Range(0f, 2f);
	}

	private void OnEnable()
	{
		Instances.Add(this);
	}

	private void OnDisable()
	{
		Instances.Remove(this);
	}

	public void CustomUpdate(float deltaTime, float time)
	{
		m_cooldownTimer += deltaTime;
		if (!(m_cooldownTimer <= m_cooldownDelay))
		{
			Transform transform = base.transform;
			Vector3 position = transform.position;
			if (Floating.IsUnderWater(position, ref m_previousAndOut))
			{
				m_effects.Create(position, transform.rotation, transform);
				m_cooldownTimer = 0f;
			}
		}
	}
}
