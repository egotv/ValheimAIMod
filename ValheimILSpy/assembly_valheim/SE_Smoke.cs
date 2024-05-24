using UnityEngine;

public class SE_Smoke : StatusEffect
{
	[Header("SE_Burning")]
	public HitData.DamageTypes m_damage;

	public float m_damageInterval = 1f;

	private float m_timer;

	public override bool CanAdd(Character character)
	{
		if (character.m_tolerateSmoke)
		{
			return false;
		}
		return base.CanAdd(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer += dt;
		if (m_timer > m_damageInterval)
		{
			m_timer = 0f;
			HitData hitData = new HitData();
			hitData.m_point = m_character.GetCenterPoint();
			hitData.m_damage = m_damage;
			hitData.m_hitType = HitData.HitType.Smoke;
			m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: false);
		}
	}
}
