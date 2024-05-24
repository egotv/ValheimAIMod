using UnityEngine;

public class SE_Shield : StatusEffect
{
	[Header("__SE_Shield__")]
	public float m_absorbDamage = 100f;

	public float m_absorbDamageWorldLevel = 100f;

	public Skills.SkillType m_levelUpSkillOnBreak;

	public float m_levelUpSkillFactor = 1f;

	public int m_ttlPerItemLevel;

	public float m_absorbDamagePerSkillLevel;

	public EffectList m_breakEffects = new EffectList();

	public EffectList m_hitEffects = new EffectList();

	private float m_totalAbsorbDamage;

	private float m_damage;

	public override void Setup(Character character)
	{
		base.Setup(character);
	}

	public override bool IsDone()
	{
		if (m_damage > m_totalAbsorbDamage)
		{
			m_breakEffects.Create(m_character.GetCenterPoint(), m_character.transform.rotation, m_character.transform, m_character.GetRadius() * 2f);
			if (m_levelUpSkillOnBreak != 0)
			{
				Skills skills = m_character.GetSkills();
				if ((object)skills != null && (bool)skills)
				{
					skills.RaiseSkill(m_levelUpSkillOnBreak, m_levelUpSkillFactor);
					Terminal.Log($"{m_name} is leveling up {m_levelUpSkillOnBreak} at factor {m_levelUpSkillFactor}");
				}
			}
			return true;
		}
		return base.IsDone();
	}

	public override void OnDamaged(HitData hit, Character attacker)
	{
		float totalDamage = hit.GetTotalDamage();
		m_damage += totalDamage;
		hit.ApplyModifier(0f);
		m_hitEffects.Create(hit.m_point, Quaternion.LookRotation(-hit.m_dir), m_character.transform);
	}

	public override void SetLevel(int itemLevel, float skillLevel)
	{
		if (m_ttlPerItemLevel > 0)
		{
			m_ttl = m_ttlPerItemLevel * itemLevel;
		}
		m_totalAbsorbDamage = m_absorbDamage + m_absorbDamagePerSkillLevel * skillLevel;
		if (Game.m_worldLevel > 0)
		{
			m_totalAbsorbDamage += m_absorbDamageWorldLevel * (float)Game.m_worldLevel;
		}
		Terminal.Log($"Shield setting itemlevel: {itemLevel} = ttl: {m_ttl}, skilllevel: {skillLevel} = absorb: {m_totalAbsorbDamage}");
		base.SetLevel(itemLevel, skillLevel);
	}

	public override string GetTooltipString()
	{
		return base.GetTooltipString() + "\n$se_shield_ttl <color=orange>" + m_ttl.ToString("0") + "</color>\n$se_shield_damage <color=orange>" + m_totalAbsorbDamage.ToString("0") + "</color>";
	}
}
