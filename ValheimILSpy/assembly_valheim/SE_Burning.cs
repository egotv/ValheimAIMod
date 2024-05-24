using UnityEngine;

public class SE_Burning : StatusEffect
{
	[Header("SE_Burning")]
	public float m_damageInterval = 1f;

	private float m_timer;

	private float m_fireDamageLeft;

	private float m_fireDamagePerHit;

	private float m_spiritDamageLeft;

	private float m_spiritDamagePerHit;

	public EffectList m_tickEffect = new EffectList();

	private const float m_minimumDamageTick = 0.2f;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (m_fireDamageLeft > 0f && m_character.GetSEMan().HaveStatusEffect(SEMan.s_statusEffectWet))
		{
			m_time += dt * 5f;
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_point = m_character.GetCenterPoint();
			hitData.m_damage.m_fire = m_fireDamagePerHit;
			hitData.m_damage.m_spirit = m_spiritDamagePerHit;
			hitData.m_hitType = HitData.HitType.Burning;
			m_fireDamageLeft = Mathf.Max(0f, m_fireDamageLeft - m_fireDamagePerHit);
			m_spiritDamageLeft = Mathf.Max(0f, m_spiritDamageLeft - m_spiritDamagePerHit);
			m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: false);
			m_tickEffect.Create(m_character.transform.position, m_character.transform.rotation);
		}
	}

	public bool AddFireDamage(float damage)
	{
		int num = (int)(m_ttl / m_damageInterval);
		if (damage / (float)num < 0.2f && m_fireDamageLeft == 0f)
		{
			return false;
		}
		m_fireDamageLeft += damage;
		m_fireDamagePerHit = m_fireDamageLeft / (float)num;
		ResetTime();
		return true;
	}

	public bool AddSpiritDamage(float damage)
	{
		int num = (int)(m_ttl / m_damageInterval);
		if (damage / (float)num < 0.2f && m_spiritDamageLeft == 0f)
		{
			return false;
		}
		m_spiritDamageLeft += damage;
		m_spiritDamagePerHit = m_spiritDamageLeft / (float)num;
		ResetTime();
		return true;
	}
}
