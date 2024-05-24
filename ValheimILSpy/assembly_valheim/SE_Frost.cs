using UnityEngine;

public class SE_Frost : StatusEffect
{
	[Header("SE_Frost")]
	public float m_freezeTimeEnemy = 10f;

	public float m_freezeTimePlayer = 10f;

	public float m_minSpeedFactor = 0.1f;

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
	}

	public void AddDamage(float damage)
	{
		float num = (m_character.IsPlayer() ? m_freezeTimePlayer : m_freezeTimeEnemy);
		float num2 = Mathf.Clamp01(damage / m_character.GetMaxHealth()) * num;
		float num3 = m_ttl - m_time;
		if (num2 > num3)
		{
			m_ttl = num2;
			ResetTime();
			TriggerStartEffects();
		}
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
	{
		HitData.DamageModifiers damageModifiers = character.GetDamageModifiers();
		if (damageModifiers.m_frost != HitData.DamageModifier.Resistant && damageModifiers.m_frost != HitData.DamageModifier.VeryResistant && damageModifiers.m_frost != HitData.DamageModifier.Immune)
		{
			float f = Mathf.Clamp01(m_time / m_ttl);
			f = Mathf.Pow(f, 2f);
			speed -= baseSpeed * Mathf.Lerp(1f - m_minSpeedFactor, 0f, f);
			if (speed < 0f)
			{
				speed = 0f;
			}
		}
	}
}
