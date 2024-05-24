using UnityEngine;

public class SE_Poison : StatusEffect
{
	[Header("SE_Poison")]
	public float m_damageInterval = 1f;

	public float m_baseTTL = 2f;

	public float m_TTLPerDamagePlayer = 2f;

	public float m_TTLPerDamage = 2f;

	public float m_TTLPower = 0.5f;

	private float m_timer;

	private float m_damageLeft;

	private float m_damagePerHit;

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = m_character.GetCenterPoint();
			hitData.m_damage.m_poison = m_damagePerHit;
			hitData.m_hitType = HitData.HitType.Poisoned;
			m_damageLeft -= m_damagePerHit;
			m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: false);
		}
	}

	public void AddDamage(float damage)
	{
		if (damage >= m_damageLeft)
		{
			m_damageLeft = damage;
			float num = (m_character.IsPlayer() ? m_TTLPerDamagePlayer : m_TTLPerDamage);
			m_ttl = m_baseTTL + Mathf.Pow(m_damageLeft * num, m_TTLPower);
			int num2 = (int)(m_ttl / m_damageInterval);
			m_damagePerHit = m_damageLeft / (float)num2;
			ZLog.Log("Poison damage: " + m_damageLeft + " ttl:" + m_ttl + " hits:" + num2 + " dmg perhit:" + m_damagePerHit);
			ResetTime();
		}
	}
}
