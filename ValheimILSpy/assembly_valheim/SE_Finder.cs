using UnityEngine;

public class SE_Finder : StatusEffect
{
	[Header("SE_Finder")]
	public EffectList m_pingEffectNear = new EffectList();

	public EffectList m_pingEffectMed = new EffectList();

	public EffectList m_pingEffectFar = new EffectList();

	public float m_closerTriggerDistance = 2f;

	public float m_furtherTriggerDistance = 4f;

	public float m_closeFrequency = 1f;

	public float m_distantFrequency = 5f;

	private float m_updateBeaconTimer;

	private float m_pingTimer;

	private Beacon m_beacon;

	private float m_lastDistance;

	public override void UpdateStatusEffect(float dt)
	{
		m_updateBeaconTimer += dt;
		if (m_updateBeaconTimer > 1f)
		{
			m_updateBeaconTimer = 0f;
			Beacon beacon = Beacon.FindClosestBeaconInRange(m_character.transform.position);
			if (beacon != m_beacon)
			{
				m_beacon = beacon;
				if ((bool)m_beacon)
				{
					m_lastDistance = Utils.DistanceXZ(m_character.transform.position, m_beacon.transform.position);
					m_pingTimer = 0f;
				}
			}
		}
		if (!(m_beacon != null))
		{
			return;
		}
		float num = Utils.DistanceXZ(m_character.transform.position, m_beacon.transform.position);
		float num2 = Mathf.Clamp01(num / m_beacon.m_range);
		float num3 = Mathf.Lerp(m_closeFrequency, m_distantFrequency, num2);
		m_pingTimer += dt;
		if (m_pingTimer > num3)
		{
			m_pingTimer = 0f;
			if (num2 < 0.2f)
			{
				m_pingEffectNear.Create(m_character.transform.position, m_character.transform.rotation, m_character.transform);
			}
			else if (num2 < 0.6f)
			{
				m_pingEffectMed.Create(m_character.transform.position, m_character.transform.rotation, m_character.transform);
			}
			else
			{
				m_pingEffectFar.Create(m_character.transform.position, m_character.transform.rotation, m_character.transform);
			}
			m_lastDistance = num;
		}
	}
}
