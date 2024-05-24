using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SE_Stats : StatusEffect
{
	[Header("__SE_Stats__")]
	[Header("HP per tick")]
	public float m_tickInterval;

	public float m_healthPerTickMinHealthPercentage;

	public float m_healthPerTick;

	public HitData.HitType m_hitType;

	[Header("Health over time")]
	public float m_healthOverTime;

	public float m_healthOverTimeDuration;

	public float m_healthOverTimeInterval = 5f;

	[Header("Stamina")]
	public float m_staminaOverTime;

	public float m_staminaOverTimeDuration;

	public float m_staminaDrainPerSec;

	public float m_runStaminaDrainModifier;

	public float m_jumpStaminaUseModifier;

	public float m_attackStaminaUseModifier;

	public float m_blockStaminaUseModifier;

	public float m_dodgeStaminaUseModifier;

	public float m_swimStaminaUseModifier;

	public float m_homeItemStaminaUseModifier;

	public float m_sneakStaminaUseModifier;

	public float m_runStaminaUseModifier;

	[Header("Eitr")]
	public float m_eitrOverTime;

	public float m_eitrOverTimeDuration;

	[Header("Regen modifiers")]
	public float m_healthRegenMultiplier = 1f;

	public float m_staminaRegenMultiplier = 1f;

	public float m_eitrRegenMultiplier = 1f;

	[Header("Modify raise skill")]
	public Skills.SkillType m_raiseSkill;

	public float m_raiseSkillModifier;

	[Header("Modify skill level")]
	public Skills.SkillType m_skillLevel;

	public float m_skillLevelModifier;

	public Skills.SkillType m_skillLevel2;

	public float m_skillLevelModifier2;

	[Header("Hit modifier")]
	public List<HitData.DamageModPair> m_mods = new List<HitData.DamageModPair>();

	[Header("Attack")]
	public Skills.SkillType m_modifyAttackSkill;

	public float m_damageModifier = 1f;

	public HitData.DamageTypes m_percentigeDamageModifiers;

	[Header("Sneak")]
	public float m_noiseModifier;

	public float m_stealthModifier;

	[Header("Carry weight")]
	public float m_addMaxCarryWeight;

	[Header("Speed")]
	public float m_speedModifier;

	public Vector3 m_jumpModifier;

	[Header("Fall")]
	public float m_maxMaxFallSpeed;

	public float m_fallDamageModifier;

	[Header("Wind")]
	public float m_windMovementModifier;

	private float m_tickTimer;

	private float m_healthOverTimeTimer;

	private float m_healthOverTimeTicks;

	private float m_healthOverTimeTickHP;

	public override void Setup(Character character)
	{
		base.Setup(character);
		if (m_healthOverTime > 0f && m_healthOverTimeInterval > 0f)
		{
			if (m_healthOverTimeDuration <= 0f)
			{
				m_healthOverTimeDuration = m_ttl;
			}
			m_healthOverTimeTicks = m_healthOverTimeDuration / m_healthOverTimeInterval;
			m_healthOverTimeTickHP = m_healthOverTime / m_healthOverTimeTicks;
		}
		if (m_staminaOverTime > 0f && m_staminaOverTimeDuration <= 0f)
		{
			m_staminaOverTimeDuration = m_ttl;
		}
		if (m_eitrOverTime > 0f && m_eitrOverTimeDuration <= 0f)
		{
			m_eitrOverTimeDuration = m_ttl;
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (m_tickInterval > 0f)
		{
			m_tickTimer += dt;
			if (m_tickTimer >= m_tickInterval)
			{
				m_tickTimer = 0f;
				if (m_character.GetHealthPercentage() >= m_healthPerTickMinHealthPercentage)
				{
					if (m_healthPerTick > 0f)
					{
						m_character.Heal(m_healthPerTick);
					}
					else
					{
						HitData hitData = new HitData();
						hitData.m_damage.m_damage = 0f - m_healthPerTick;
						hitData.m_point = m_character.GetTopPoint();
						hitData.m_hitType = m_hitType;
						m_character.Damage(hitData);
					}
				}
			}
		}
		if (m_healthOverTimeTicks > 0f)
		{
			m_healthOverTimeTimer += dt;
			if (m_healthOverTimeTimer > m_healthOverTimeInterval)
			{
				m_healthOverTimeTimer = 0f;
				m_healthOverTimeTicks -= 1f;
				m_character.Heal(m_healthOverTimeTickHP);
			}
		}
		if (m_staminaOverTime != 0f && m_time <= m_staminaOverTimeDuration)
		{
			float num = m_staminaOverTimeDuration / dt;
			m_character.AddStamina(m_staminaOverTime / num);
		}
		if (m_eitrOverTime != 0f && m_time <= m_eitrOverTimeDuration)
		{
			float num2 = m_eitrOverTimeDuration / dt;
			m_character.AddEitr(m_eitrOverTime / num2);
		}
		if (m_staminaDrainPerSec > 0f)
		{
			m_character.UseStamina(m_staminaDrainPerSec * dt);
		}
	}

	public override void ModifyHealthRegen(ref float regenMultiplier)
	{
		if (m_healthRegenMultiplier > 1f)
		{
			regenMultiplier += m_healthRegenMultiplier - 1f;
		}
		else
		{
			regenMultiplier *= m_healthRegenMultiplier;
		}
	}

	public override void ModifyStaminaRegen(ref float staminaRegen)
	{
		if (m_staminaRegenMultiplier > 1f)
		{
			staminaRegen += m_staminaRegenMultiplier - 1f;
		}
		else
		{
			staminaRegen *= m_staminaRegenMultiplier;
		}
	}

	public override void ModifyEitrRegen(ref float staminaRegen)
	{
		if (m_eitrRegenMultiplier > 1f)
		{
			staminaRegen += m_eitrRegenMultiplier - 1f;
		}
		else
		{
			staminaRegen *= m_eitrRegenMultiplier;
		}
	}

	public override void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
		modifiers.Apply(m_mods);
	}

	public override void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
		if (m_raiseSkill != 0 && (m_raiseSkill == Skills.SkillType.All || m_raiseSkill == skill))
		{
			value += m_raiseSkillModifier;
		}
	}

	public override void ModifySkillLevel(Skills.SkillType skill, ref float value)
	{
		if (m_skillLevel != 0)
		{
			if (m_skillLevel == Skills.SkillType.All || m_skillLevel == skill)
			{
				value += m_skillLevelModifier;
			}
			if (m_skillLevel2 == Skills.SkillType.All || m_skillLevel2 == skill)
			{
				value += m_skillLevelModifier2;
			}
		}
	}

	public override void ModifyNoise(float baseNoise, ref float noise)
	{
		noise += baseNoise * m_noiseModifier;
	}

	public override void ModifyStealth(float baseStealth, ref float stealth)
	{
		stealth += baseStealth * m_stealthModifier;
	}

	public override void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		limit += m_addMaxCarryWeight;
		if (limit < 0f)
		{
			limit = 0f;
		}
	}

	public override void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		if (skill == m_modifyAttackSkill || m_modifyAttackSkill == Skills.SkillType.All)
		{
			hitData.m_damage.Modify(m_damageModifier);
		}
		hitData.m_damage.Modify(m_percentigeDamageModifiers);
	}

	public override void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
		drain += baseDrain * m_runStaminaDrainModifier;
	}

	public override void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_jumpStaminaUseModifier;
	}

	public override void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_attackStaminaUseModifier;
	}

	public override void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_blockStaminaUseModifier;
	}

	public override void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_dodgeStaminaUseModifier;
	}

	public override void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_swimStaminaUseModifier;
	}

	public override void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_homeItemStaminaUseModifier;
	}

	public override void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
		staminaUse += baseStaminaUse * m_sneakStaminaUseModifier;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
	{
		if (m_character.IsSwimming())
		{
			speed += baseSpeed * m_speedModifier * 0.5f;
		}
		else
		{
			speed += baseSpeed * m_speedModifier;
		}
		if (m_windMovementModifier > 0f)
		{
			dir.Normalize();
			float num = (Vector3.Dot(dir, EnvMan.instance.GetWindDir()) + 1f) / 2f;
			num *= EnvMan.instance.GetWindIntensity();
			speed *= 1f + num;
		}
		if (speed < 0f)
		{
			speed = 0f;
		}
	}

	public override void ModifyJump(Vector3 baseJump, ref Vector3 jump)
	{
		jump += new Vector3(baseJump.x * m_jumpModifier.x, baseJump.y * m_jumpModifier.y, baseJump.z * m_jumpModifier.z);
	}

	public override void ModifyWalkVelocity(ref Vector3 vel)
	{
		if (m_maxMaxFallSpeed > 0f && vel.y < 0f - m_maxMaxFallSpeed)
		{
			vel.y = 0f - m_maxMaxFallSpeed;
		}
	}

	public override void ModifyFallDamage(float baseDamage, ref float damage)
	{
		damage += baseDamage * m_fallDamageModifier;
		if (damage < 0f)
		{
			damage = 0f;
		}
	}

	public override string GetTooltipString()
	{
		StringBuilder stringBuilder = new StringBuilder(256);
		if (m_tooltip.Length > 0)
		{
			stringBuilder.AppendFormat("{0}\n", m_tooltip);
		}
		if (m_runStaminaDrainModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (m_runStaminaDrainModifier * 100f).ToString("+0;-0"));
		}
		if (m_healthOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_health: <color=orange>{0}</color>\n", m_healthOverTime.ToString());
		}
		if (m_staminaOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_stamina: <color=orange>{0}</color>\n", m_staminaOverTime.ToString());
		}
		if (m_eitrOverTime != 0f)
		{
			stringBuilder.AppendFormat("$se_eitr: <color=orange>{0}</color>\n", m_eitrOverTime.ToString());
		}
		if (m_healthRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_healthregen: <color=orange>{0}%</color>\n", ((m_healthRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (m_staminaRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_staminaregen: <color=orange>{0}%</color>\n", ((m_staminaRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (m_eitrRegenMultiplier != 1f)
		{
			stringBuilder.AppendFormat("$se_eitrregen: <color=orange>{0}%</color>\n", ((m_eitrRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
		}
		if (m_addMaxCarryWeight != 0f)
		{
			stringBuilder.AppendFormat("$se_max_carryweight: <color=orange>{0}</color>\n", m_addMaxCarryWeight.ToString("+0;-0"));
		}
		if (m_mods.Count > 0)
		{
			stringBuilder.Append(GetDamageModifiersTooltipString(m_mods));
			stringBuilder.Append("\n");
		}
		if (m_noiseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_noisemod: <color=orange>{0}%</color>\n", (m_noiseModifier * 100f).ToString("+0;-0"));
		}
		if (m_stealthModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_sneakmod: <color=orange>{0}%</color>\n", (m_stealthModifier * 100f).ToString("+0;-0"));
		}
		if (m_speedModifier != 0f)
		{
			stringBuilder.AppendFormat("$item_movement_modifier: <color=orange>{0}%</color>\n", (m_speedModifier * 100f).ToString("+0;-0"));
		}
		if (m_maxMaxFallSpeed != 0f)
		{
			stringBuilder.AppendFormat("$item_limitfallspeed: <color=orange>{0}m/s</color>\n", m_maxMaxFallSpeed.ToString("0"));
		}
		if (m_fallDamageModifier != 0f)
		{
			stringBuilder.AppendFormat("$item_falldamage: <color=orange>{0}%</color>\n", (m_fallDamageModifier * 100f).ToString("+0;-0"));
		}
		if (m_jumpModifier.y != 0f)
		{
			stringBuilder.AppendFormat("$se_jumpheight: <color=orange>{0}%</color>\n", (m_jumpModifier.y * 100f).ToString("+0;-0"));
		}
		if (m_jumpModifier.x != 0f || m_jumpModifier.z != 0f)
		{
			stringBuilder.AppendFormat("$se_jumplength: <color=orange>{0}%</color>\n", (Mathf.Max(m_jumpModifier.x, m_jumpModifier.z) * 100f).ToString("+0;-0"));
		}
		if (m_jumpStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_jumpstamina: <color=orange>{0}%</color>\n", (m_jumpStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_attackStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_attackstamina: <color=orange>{0}%</color>\n", (m_attackStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_blockStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_blockstamina: <color=orange>{0}%</color>\n", (m_blockStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_dodgeStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_dodgestamina: <color=orange>{0}%</color>\n", (m_dodgeStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_swimStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_swimstamina: <color=orange>{0}%</color>\n", (m_dodgeStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_homeItemStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$base_item_modifier: <color=orange>{0}%</color>\n", (m_homeItemStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_sneakStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_sneakstamina: <color=orange>{0}%</color>\n", (m_sneakStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_runStaminaUseModifier != 0f)
		{
			stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (m_runStaminaUseModifier * 100f).ToString("+0;-0"));
		}
		if (m_skillLevel != 0)
		{
			stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + m_skillLevel.ToString().ToLower()), m_skillLevelModifier.ToString("+0;-0"));
		}
		if (m_skillLevel2 != 0)
		{
			stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + m_skillLevel2.ToString().ToLower()), m_skillLevelModifier2.ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_blunt != 0f)
		{
			stringBuilder.AppendFormat("$inventory_blunt: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_blunt * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_slash != 0f)
		{
			stringBuilder.AppendFormat("$inventory_slash: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_slash * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_pierce != 0f)
		{
			stringBuilder.AppendFormat("$inventory_pierce: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_pierce * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_chop != 0f)
		{
			stringBuilder.AppendFormat("$inventory_chop: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_chop * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_pickaxe != 0f)
		{
			stringBuilder.AppendFormat("$inventory_pickaxe: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_pickaxe * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_fire != 0f)
		{
			stringBuilder.AppendFormat("$inventory_fire: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_fire * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_frost != 0f)
		{
			stringBuilder.AppendFormat("$inventory_frost: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_frost * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_lightning != 0f)
		{
			stringBuilder.AppendFormat("$inventory_lightning: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_lightning * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_poison != 0f)
		{
			stringBuilder.AppendFormat("$inventory_poison: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_poison * 100f).ToString("+0;-0"));
		}
		if (m_percentigeDamageModifiers.m_spirit != 0f)
		{
			stringBuilder.AppendFormat("$inventory_spirit: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_spirit * 100f).ToString("+0;-0"));
		}
		return stringBuilder.ToString();
	}

	public static string GetDamageModifiersTooltipString(List<HitData.DamageModPair> mods)
	{
		if (mods.Count == 0)
		{
			return "";
		}
		string text = "";
		foreach (HitData.DamageModPair mod in mods)
		{
			if (mod.m_modifier != HitData.DamageModifier.Ignore && mod.m_modifier != 0)
			{
				switch (mod.m_modifier)
				{
				case HitData.DamageModifier.Immune:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_immune</color> VS ";
					break;
				case HitData.DamageModifier.Resistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_resistant</color> VS ";
					break;
				case HitData.DamageModifier.VeryResistant:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryresistant</color> VS ";
					break;
				case HitData.DamageModifier.Weak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_weak</color> VS ";
					break;
				case HitData.DamageModifier.VeryWeak:
					text += "\n$inventory_dmgmod: <color=orange>$inventory_veryweak</color> VS ";
					break;
				}
				text += "<color=orange>";
				switch (mod.m_type)
				{
				case HitData.DamageType.Blunt:
					text += "$inventory_blunt";
					break;
				case HitData.DamageType.Slash:
					text += "$inventory_slash";
					break;
				case HitData.DamageType.Pierce:
					text += "$inventory_pierce";
					break;
				case HitData.DamageType.Chop:
					text += "$inventory_chop";
					break;
				case HitData.DamageType.Pickaxe:
					text += "$inventory_pickaxe";
					break;
				case HitData.DamageType.Fire:
					text += "$inventory_fire";
					break;
				case HitData.DamageType.Frost:
					text += "$inventory_frost";
					break;
				case HitData.DamageType.Lightning:
					text += "$inventory_lightning";
					break;
				case HitData.DamageType.Poison:
					text += "$inventory_poison";
					break;
				case HitData.DamageType.Spirit:
					text += "$inventory_spirit";
					break;
				}
				text += "</color>";
			}
		}
		return text;
	}
}
