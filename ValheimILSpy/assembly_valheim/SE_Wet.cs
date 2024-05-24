using UnityEngine;

public class SE_Wet : SE_Stats
{
	[Header("__SE_Wet__")]
	public float m_waterDamage;

	public float m_damageInterval = 0.5f;

	private float m_timer;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (!m_character.m_tolerateWater)
		{
			m_timer += dt;
			if (m_timer > m_damageInterval)
			{
				m_timer = 0f;
				HitData hitData = new HitData();
				hitData.m_point = m_character.transform.position;
				hitData.m_damage.m_damage = m_waterDamage;
				hitData.m_hitType = HitData.HitType.Water;
				m_character.Damage(hitData);
			}
		}
		if (m_character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectCampFire))
		{
			m_time += dt * 10f;
		}
		if (m_character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectBurning))
		{
			m_time += dt * 50f;
		}
	}
}
