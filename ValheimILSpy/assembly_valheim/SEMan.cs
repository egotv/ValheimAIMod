using System.Collections.Generic;
using UnityEngine;

public class SEMan
{
	private readonly HashSet<int> m_statusEffectsHashSet = new HashSet<int>();

	private readonly List<StatusEffect> m_statusEffects = new List<StatusEffect>();

	private readonly List<StatusEffect> m_removeStatusEffects = new List<StatusEffect>();

	private int m_statusEffectAttributes;

	private int m_statusEffectAttributesOld = -1;

	private Character m_character;

	private ZNetView m_nview;

	public static readonly int s_statusEffectRested = "Rested".GetStableHashCode();

	public static readonly int s_statusEffectEncumbered = "Encumbered".GetStableHashCode();

	public static readonly int s_statusEffectSoftDeath = "SoftDeath".GetStableHashCode();

	public static readonly int s_statusEffectWet = "Wet".GetStableHashCode();

	public static readonly int s_statusEffectShelter = "Shelter".GetStableHashCode();

	public static readonly int s_statusEffectCampFire = "CampFire".GetStableHashCode();

	public static readonly int s_statusEffectResting = "Resting".GetStableHashCode();

	public static readonly int s_statusEffectCold = "Cold".GetStableHashCode();

	public static readonly int s_statusEffectFreezing = "Freezing".GetStableHashCode();

	public static readonly int s_statusEffectBurning = "Burning".GetStableHashCode();

	public static readonly int s_statusEffectFrost = "Frost".GetStableHashCode();

	public static readonly int s_statusEffectLightning = "Lightning".GetStableHashCode();

	public static readonly int s_statusEffectPoison = "Poison".GetStableHashCode();

	public static readonly int s_statusEffectSmoked = "Smoked".GetStableHashCode();

	public static readonly int s_statusEffectSpirit = "Spirit".GetStableHashCode();

	public static readonly int s_statusEffectTared = "Tared".GetStableHashCode();

	public SEMan(Character character, ZNetView nview)
	{
		m_character = character;
		m_nview = nview;
		m_nview.Register<int, bool, int, float>("RPC_AddStatusEffect", RPC_AddStatusEffect);
	}

	public void OnDestroy()
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.OnDestroy();
		}
		m_statusEffects.Clear();
		m_statusEffectsHashSet.Clear();
	}

	public void ApplyStatusEffectSpeedMods(ref float speed, Vector3 dir)
	{
		float baseSpeed = speed;
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifySpeed(baseSpeed, ref speed, m_character, dir);
		}
	}

	public void ApplyStatusEffectJumpMods(ref Vector3 jump)
	{
		Vector3 baseJump = jump;
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyJump(baseJump, ref jump);
		}
	}

	public void ApplyDamageMods(ref HitData.DamageModifiers mods)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyDamageMods(ref mods);
		}
	}

	public void Update(ZDO zdo, float dt)
	{
		m_statusEffectAttributes = 0;
		int count = m_statusEffects.Count;
		for (int i = 0; i < count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			statusEffect.UpdateStatusEffect(dt);
			if (statusEffect.IsDone())
			{
				m_removeStatusEffects.Add(statusEffect);
			}
			else
			{
				m_statusEffectAttributes |= (int)statusEffect.m_attributes;
			}
		}
		if (m_removeStatusEffects.Count > 0)
		{
			foreach (StatusEffect removeStatusEffect in m_removeStatusEffects)
			{
				removeStatusEffect.Stop();
				m_statusEffects.Remove(removeStatusEffect);
				m_statusEffectsHashSet.Remove(removeStatusEffect.NameHash());
			}
			m_removeStatusEffects.Clear();
		}
		if (m_statusEffectAttributes != m_statusEffectAttributesOld)
		{
			zdo.Set(ZDOVars.s_seAttrib, m_statusEffectAttributes);
			m_statusEffectAttributesOld = m_statusEffectAttributes;
		}
	}

	public StatusEffect AddStatusEffect(int nameHash, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
	{
		if (nameHash == 0)
		{
			return null;
		}
		if (m_nview.IsOwner())
		{
			return Internal_AddStatusEffect(nameHash, resetTime, itemLevel, skillLevel);
		}
		m_nview.InvokeRPC("RPC_AddStatusEffect", nameHash, resetTime, itemLevel, skillLevel);
		return null;
	}

	private void RPC_AddStatusEffect(long sender, int nameHash, bool resetTime, int itemLevel, float skillLevel)
	{
		if (m_nview.IsOwner())
		{
			Internal_AddStatusEffect(nameHash, resetTime, itemLevel, skillLevel);
		}
	}

	private StatusEffect Internal_AddStatusEffect(int nameHash, bool resetTime, int itemLevel, float skillLevel)
	{
		StatusEffect statusEffect = GetStatusEffect(nameHash);
		if ((bool)statusEffect)
		{
			if (resetTime)
			{
				statusEffect.ResetTime();
				statusEffect.SetLevel(itemLevel, skillLevel);
			}
			return null;
		}
		StatusEffect statusEffect2 = ObjectDB.instance.GetStatusEffect(nameHash);
		if (statusEffect2 == null)
		{
			return null;
		}
		return AddStatusEffect(statusEffect2, resetTime: false, itemLevel, skillLevel);
	}

	public StatusEffect AddStatusEffect(StatusEffect statusEffect, bool resetTime = false, int itemLevel = 0, float skillLevel = 0f)
	{
		StatusEffect statusEffect2 = GetStatusEffect(statusEffect.NameHash());
		if ((bool)statusEffect2)
		{
			if (resetTime)
			{
				statusEffect2.ResetTime();
				statusEffect2.SetLevel(itemLevel, skillLevel);
			}
			return null;
		}
		if (!statusEffect.CanAdd(m_character))
		{
			return null;
		}
		StatusEffect statusEffect3 = statusEffect.Clone();
		m_statusEffects.Add(statusEffect3);
		m_statusEffectsHashSet.Add(statusEffect3.NameHash());
		statusEffect3.Setup(m_character);
		statusEffect3.SetLevel(itemLevel, skillLevel);
		if (m_character.IsPlayer())
		{
			Gogan.LogEvent("Game", "StatusEffect", statusEffect.name, 0L);
		}
		return statusEffect3;
	}

	public bool RemoveStatusEffect(StatusEffect se, bool quiet = false)
	{
		return RemoveStatusEffect(se.NameHash(), quiet);
	}

	public bool RemoveStatusEffect(int nameHash, bool quiet = false)
	{
		if (nameHash == 0)
		{
			return false;
		}
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			if (statusEffect.NameHash() == nameHash)
			{
				if (quiet)
				{
					statusEffect.m_stopMessage = "";
				}
				statusEffect.Stop();
				m_statusEffects.Remove(statusEffect);
				m_statusEffectsHashSet.Remove(nameHash);
				return true;
			}
		}
		return false;
	}

	public void RemoveAllStatusEffects(bool quiet = false)
	{
		for (int num = m_statusEffects.Count - 1; num >= 0; num--)
		{
			StatusEffect statusEffect = m_statusEffects[num];
			if (quiet)
			{
				statusEffect.m_stopMessage = "";
			}
			statusEffect.Stop();
			m_statusEffects.Remove(statusEffect);
		}
		m_statusEffectsHashSet.Clear();
	}

	public bool HaveStatusEffectCategory(string cat)
	{
		if (cat.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < m_statusEffects.Count; i++)
		{
			StatusEffect statusEffect = m_statusEffects[i];
			if (statusEffect.m_category.Length > 0 && statusEffect.m_category == cat)
			{
				return true;
			}
		}
		return false;
	}

	public bool HaveStatusAttribute(StatusEffect.StatusAttribute value)
	{
		if (!m_nview.IsValid())
		{
			return false;
		}
		if (m_nview.IsOwner())
		{
			return ((uint)m_statusEffectAttributes & (uint)value) != 0;
		}
		return ((uint)m_nview.GetZDO().GetInt(ZDOVars.s_seAttrib) & (uint)value) != 0;
	}

	public bool HaveStatusEffect(int nameHash)
	{
		return m_statusEffectsHashSet.Contains(nameHash);
	}

	public List<StatusEffect> GetStatusEffects()
	{
		return m_statusEffects;
	}

	public StatusEffect GetStatusEffect(int nameHash)
	{
		if (nameHash == 0)
		{
			return null;
		}
		if (!m_statusEffectsHashSet.Contains(nameHash))
		{
			return null;
		}
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			if (statusEffect.NameHash() == nameHash)
			{
				return statusEffect;
			}
		}
		return null;
	}

	public void GetHUDStatusEffects(List<StatusEffect> effects)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			if ((bool)statusEffect.m_icon)
			{
				effects.Add(statusEffect);
			}
		}
	}

	public void ModifyFallDamage(float baseDamage, ref float damage)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyFallDamage(baseDamage, ref damage);
		}
	}

	public void ModifyWalkVelocity(ref Vector3 vel)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyWalkVelocity(ref vel);
		}
	}

	public void ModifyNoise(float baseNoise, ref float noise)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyNoise(baseNoise, ref noise);
		}
	}

	public void ModifySkillLevel(Skills.SkillType skill, ref float level)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifySkillLevel(skill, ref level);
		}
	}

	public void ModifyRaiseSkill(Skills.SkillType skill, ref float multiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyRaiseSkill(skill, ref multiplier);
		}
	}

	public void ModifyStaminaRegen(ref float staminaMultiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyStaminaRegen(ref staminaMultiplier);
		}
	}

	public void ModifyEitrRegen(ref float eitrMultiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyEitrRegen(ref eitrMultiplier);
		}
	}

	public void ModifyHealthRegen(ref float regenMultiplier)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyHealthRegen(ref regenMultiplier);
		}
	}

	public void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyMaxCarryWeight(baseLimit, ref limit);
		}
	}

	public void ModifyStealth(float baseStealth, ref float stealth)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyStealth(baseStealth, ref stealth);
		}
	}

	public void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyAttack(skill, ref hitData);
		}
	}

	public void ModifyRunStaminaDrain(float baseDrain, ref float drain, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyRunStaminaDrain(baseDrain, ref drain);
		}
		if (minZero && drain < 0f)
		{
			drain = 0f;
		}
	}

	public void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyJumpStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyAttackStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyBlockStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyDodgeStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifySwimStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifyHomeItemStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse, bool minZero = true)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.ModifySneakStaminaUsage(baseStaminaUse, ref staminaUse);
		}
		if (minZero && staminaUse < 0f)
		{
			staminaUse = 0f;
		}
	}

	public void OnDamaged(HitData hit, Character attacker)
	{
		foreach (StatusEffect statusEffect in m_statusEffects)
		{
			statusEffect.OnDamaged(hit, attacker);
		}
	}
}
