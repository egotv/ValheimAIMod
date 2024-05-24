using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

public class HitData
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	private struct HitDefaults
	{
		[Flags]
		public enum SerializeFlags
		{
			None = 0,
			Damage = 1,
			DamageBlunt = 2,
			DamageSlash = 4,
			DamagePierce = 8,
			DamageChop = 0x10,
			DamagePickaxe = 0x20,
			DamageFire = 0x40,
			DamageFrost = 0x80,
			DamageLightning = 0x100,
			DamagePoison = 0x200,
			DamageSpirit = 0x400,
			PushForce = 0x800,
			BackstabBonus = 0x1000,
			StaggerMultiplier = 0x2000,
			Attacker = 0x4000,
			SkillRaiseAmount = 0x8000
		}

		public const float c_DamageDefault = 0f;

		public const float c_PushForceDefault = 0f;

		public const float c_BackstabBonusDefault = 1f;

		public const float c_StaggerMultiplierDefault = 1f;

		public static readonly ZDOID s_attackerDefault = ZDOID.None;

		public const float c_SkillRaiseAmountDefault = 1f;
	}

	[Flags]
	public enum DamageType
	{
		Blunt = 1,
		Slash = 2,
		Pierce = 4,
		Chop = 8,
		Pickaxe = 0x10,
		Fire = 0x20,
		Frost = 0x40,
		Lightning = 0x80,
		Poison = 0x100,
		Spirit = 0x200,
		Damage = 0x400,
		Physical = 0x1F,
		Elemental = 0xE0
	}

	public enum DamageModifier
	{
		Normal,
		Resistant,
		Weak,
		Immune,
		Ignore,
		VeryResistant,
		VeryWeak
	}

	public enum HitType : byte
	{
		Undefined,
		EnemyHit,
		PlayerHit,
		Fall,
		Drowning,
		Burning,
		Freezing,
		Poisoned,
		Water,
		Smoke,
		EdgeOfWorld,
		Impact,
		Cart,
		Tree,
		Self,
		Structural,
		Turret,
		Boat,
		Stalagtite,
		Catapult,
		CinderFire,
		AshlandsOcean
	}

	[Serializable]
	public struct DamageModPair
	{
		public DamageType m_type;

		public DamageModifier m_modifier;
	}

	[Serializable]
	public struct DamageModifiers
	{
		public DamageModifier m_blunt;

		public DamageModifier m_slash;

		public DamageModifier m_pierce;

		public DamageModifier m_chop;

		public DamageModifier m_pickaxe;

		public DamageModifier m_fire;

		public DamageModifier m_frost;

		public DamageModifier m_lightning;

		public DamageModifier m_poison;

		public DamageModifier m_spirit;

		public DamageModifiers Clone()
		{
			return (DamageModifiers)MemberwiseClone();
		}

		public void Apply(List<DamageModPair> modifiers)
		{
			foreach (DamageModPair modifier in modifiers)
			{
				switch (modifier.m_type)
				{
				case DamageType.Blunt:
					ApplyIfBetter(ref m_blunt, modifier.m_modifier);
					break;
				case DamageType.Slash:
					ApplyIfBetter(ref m_slash, modifier.m_modifier);
					break;
				case DamageType.Pierce:
					ApplyIfBetter(ref m_pierce, modifier.m_modifier);
					break;
				case DamageType.Chop:
					ApplyIfBetter(ref m_chop, modifier.m_modifier);
					break;
				case DamageType.Pickaxe:
					ApplyIfBetter(ref m_pickaxe, modifier.m_modifier);
					break;
				case DamageType.Fire:
					ApplyIfBetter(ref m_fire, modifier.m_modifier);
					break;
				case DamageType.Frost:
					ApplyIfBetter(ref m_frost, modifier.m_modifier);
					break;
				case DamageType.Lightning:
					ApplyIfBetter(ref m_lightning, modifier.m_modifier);
					break;
				case DamageType.Poison:
					ApplyIfBetter(ref m_poison, modifier.m_modifier);
					break;
				case DamageType.Spirit:
					ApplyIfBetter(ref m_spirit, modifier.m_modifier);
					break;
				}
			}
		}

		public DamageModifier GetModifier(DamageType type)
		{
			return type switch
			{
				DamageType.Blunt => m_blunt, 
				DamageType.Slash => m_slash, 
				DamageType.Pierce => m_pierce, 
				DamageType.Chop => m_chop, 
				DamageType.Pickaxe => m_pickaxe, 
				DamageType.Fire => m_fire, 
				DamageType.Frost => m_frost, 
				DamageType.Lightning => m_lightning, 
				DamageType.Poison => m_poison, 
				DamageType.Spirit => m_spirit, 
				_ => DamageModifier.Normal, 
			};
		}

		private void ApplyIfBetter(ref DamageModifier original, DamageModifier mod)
		{
			if (ShouldOverride(original, mod))
			{
				original = mod;
			}
		}

		private bool ShouldOverride(DamageModifier a, DamageModifier b)
		{
			if (a == DamageModifier.Ignore)
			{
				return false;
			}
			if (b == DamageModifier.Immune)
			{
				return true;
			}
			if (a == DamageModifier.VeryResistant && b == DamageModifier.Resistant)
			{
				return false;
			}
			if (a == DamageModifier.VeryWeak && b == DamageModifier.Weak)
			{
				return false;
			}
			if ((a == DamageModifier.Resistant || a == DamageModifier.VeryResistant || a == DamageModifier.Immune) && (b == DamageModifier.Weak || b == DamageModifier.VeryWeak))
			{
				return false;
			}
			return true;
		}

		public void Print()
		{
			ZLog.Log("m_blunt " + m_blunt);
			ZLog.Log("m_slash " + m_slash);
			ZLog.Log("m_pierce " + m_pierce);
			ZLog.Log("m_chop " + m_chop);
			ZLog.Log("m_pickaxe " + m_pickaxe);
			ZLog.Log("m_fire " + m_fire);
			ZLog.Log("m_frost " + m_frost);
			ZLog.Log("m_lightning " + m_lightning);
			ZLog.Log("m_poison " + m_poison);
			ZLog.Log("m_spirit " + m_spirit);
		}
	}

	[Serializable]
	public struct DamageTypes
	{
		public float m_damage;

		public float m_blunt;

		public float m_slash;

		public float m_pierce;

		public float m_chop;

		public float m_pickaxe;

		public float m_fire;

		public float m_frost;

		public float m_lightning;

		public float m_poison;

		public float m_spirit;

		private static StringBuilder m_sb = new StringBuilder();

		public bool HaveDamage()
		{
			if (!(m_damage > 0f) && !(m_blunt > 0f) && !(m_slash > 0f) && !(m_pierce > 0f) && !(m_chop > 0f) && !(m_pickaxe > 0f) && !(m_fire > 0f) && !(m_frost > 0f) && !(m_lightning > 0f) && !(m_poison > 0f))
			{
				return m_spirit > 0f;
			}
			return true;
		}

		public float GetTotalPhysicalDamage()
		{
			return m_blunt + m_slash + m_pierce;
		}

		public float GetTotalStaggerDamage()
		{
			return m_blunt + m_slash + m_pierce + m_lightning;
		}

		public float GetTotalBlockableDamage()
		{
			return m_blunt + m_slash + m_pierce + m_fire + m_frost + m_lightning + m_poison + m_spirit;
		}

		public float GetTotalElementalDamage()
		{
			return m_fire + m_frost + m_lightning;
		}

		public float GetTotalDamage()
		{
			return m_damage + m_blunt + m_slash + m_pierce + m_chop + m_pickaxe + m_fire + m_frost + m_lightning + m_poison + m_spirit;
		}

		public DamageTypes Clone()
		{
			return (DamageTypes)MemberwiseClone();
		}

		public void Add(DamageTypes other, int multiplier = 1)
		{
			m_damage += other.m_damage * (float)multiplier;
			m_blunt += other.m_blunt * (float)multiplier;
			m_slash += other.m_slash * (float)multiplier;
			m_pierce += other.m_pierce * (float)multiplier;
			m_chop += other.m_chop * (float)multiplier;
			m_pickaxe += other.m_pickaxe * (float)multiplier;
			m_fire += other.m_fire * (float)multiplier;
			m_frost += other.m_frost * (float)multiplier;
			m_lightning += other.m_lightning * (float)multiplier;
			m_poison += other.m_poison * (float)multiplier;
			m_spirit += other.m_spirit * (float)multiplier;
		}

		public void Modify(float multiplier)
		{
			m_damage *= multiplier;
			m_blunt *= multiplier;
			m_slash *= multiplier;
			m_pierce *= multiplier;
			m_chop *= multiplier;
			m_pickaxe *= multiplier;
			m_fire *= multiplier;
			m_frost *= multiplier;
			m_lightning *= multiplier;
			m_poison *= multiplier;
			m_spirit *= multiplier;
		}

		public void Modify(DamageTypes multipliers)
		{
			m_damage *= 1f + multipliers.m_damage;
			m_blunt *= 1f + multipliers.m_blunt;
			m_slash *= 1f + multipliers.m_slash;
			m_pierce *= 1f + multipliers.m_pierce;
			m_chop *= 1f + multipliers.m_chop;
			m_pickaxe *= 1f + multipliers.m_pickaxe;
			m_fire *= 1f + multipliers.m_fire;
			m_frost *= 1f + multipliers.m_frost;
			m_lightning *= 1f + multipliers.m_lightning;
			m_poison *= 1f + multipliers.m_poison;
			m_spirit *= 1f + multipliers.m_spirit;
		}

		public void IncreaseEqually(float totalDamageIncrease, bool seperateUtilityDamage = false)
		{
			float total = GetTotalDamage();
			if (!(total <= 0f))
			{
				if (seperateUtilityDamage)
				{
					float chop = m_chop;
					m_chop += m_chop / total * totalDamageIncrease;
					total -= chop;
				}
				else
				{
					increase(ref m_chop);
				}
				increase(ref m_damage);
				increase(ref m_blunt);
				increase(ref m_slash);
				increase(ref m_pierce);
				increase(ref m_pickaxe);
				increase(ref m_fire);
				increase(ref m_frost);
				increase(ref m_lightning);
				increase(ref m_poison);
				increase(ref m_spirit);
			}
			void increase(ref float damage)
			{
				damage += damage / total * totalDamageIncrease;
			}
		}

		public static float ApplyArmor(float dmg, float ac)
		{
			float result = Mathf.Clamp01(dmg / (ac * 4f)) * dmg;
			if (ac < dmg / 2f)
			{
				result = dmg - ac;
			}
			return result;
		}

		public void ApplyArmor(float ac)
		{
			if (!(ac <= 0f))
			{
				float num = m_blunt + m_slash + m_pierce + m_fire + m_frost + m_lightning + m_poison + m_spirit;
				if (!(num <= 0f))
				{
					float num2 = ApplyArmor(num, ac) / num;
					m_blunt *= num2;
					m_slash *= num2;
					m_pierce *= num2;
					m_fire *= num2;
					m_frost *= num2;
					m_lightning *= num2;
					m_poison *= num2;
					m_spirit *= num2;
				}
			}
		}

		public DamageType GetMajorityDamageType()
		{
			float damage;
			return GetMajorityDamageType(out damage);
		}

		public DamageType GetMajorityDamageType(out float damage)
		{
			damage = m_damage;
			DamageType result = DamageType.Damage;
			if (m_slash > damage)
			{
				damage = m_slash;
				result = DamageType.Slash;
			}
			if (m_pierce > damage)
			{
				damage = m_pierce;
				result = DamageType.Pierce;
			}
			if (m_chop > damage)
			{
				damage = m_chop;
				result = DamageType.Chop;
			}
			if (m_pickaxe > damage)
			{
				damage = m_pickaxe;
				result = DamageType.Pickaxe;
			}
			if (m_fire > damage)
			{
				damage = m_fire;
				result = DamageType.Fire;
			}
			if (m_frost > damage)
			{
				damage = m_frost;
				result = DamageType.Frost;
			}
			if (m_lightning > damage)
			{
				damage = m_lightning;
				result = DamageType.Lightning;
			}
			if (m_poison > damage)
			{
				damage = m_poison;
				result = DamageType.Poison;
			}
			if (m_spirit > damage)
			{
				damage = m_spirit;
				result = DamageType.Spirit;
			}
			return result;
		}

		public string GetTooltipString(Skills.SkillType skillType = Skills.SkillType.None)
		{
			if (Player.m_localPlayer == null)
			{
				return "";
			}
			Player.m_localPlayer.GetSkills().GetRandomSkillRange(out var min, out var max, skillType);
			m_sb.Clear();
			if (m_damage != 0f)
			{
				m_sb.Append($"\n$inventory_damage: <color=orange>{Mathf.RoundToInt(m_damage)}</color> <color=yellow>({Mathf.RoundToInt(m_damage * min)}-{Mathf.RoundToInt(m_damage * max)}) </color>");
			}
			if (m_blunt != 0f)
			{
				m_sb.Append($"\n$inventory_blunt: <color=orange>{Mathf.RoundToInt(m_blunt)}</color> <color=yellow>({Mathf.RoundToInt(m_blunt * min)}-{Mathf.RoundToInt(m_blunt * max)}) </color>");
			}
			if (m_slash != 0f)
			{
				m_sb.Append($"\n$inventory_slash: <color=orange>{Mathf.RoundToInt(m_slash)}</color> <color=yellow>({Mathf.RoundToInt(m_slash * min)}-{Mathf.RoundToInt(m_slash * max)}) </color>");
			}
			if (m_pierce != 0f)
			{
				m_sb.Append($"\n$inventory_pierce: <color=orange>{Mathf.RoundToInt(m_pierce)}</color> <color=yellow>({Mathf.RoundToInt(m_pierce * min)}-{Mathf.RoundToInt(m_pierce * max)}) </color>");
			}
			if (m_fire != 0f)
			{
				m_sb.Append($"\n$inventory_fire: <color=orange>{Mathf.RoundToInt(m_fire)}</color> <color=yellow>({Mathf.RoundToInt(m_fire * min)}-{Mathf.RoundToInt(m_fire * max)}) </color>");
			}
			if (m_frost != 0f)
			{
				m_sb.Append($"\n$inventory_frost: <color=orange>{Mathf.RoundToInt(m_frost)}</color> <color=yellow>({Mathf.RoundToInt(m_frost * min)}-{Mathf.RoundToInt(m_frost * max)}) </color>");
			}
			if (m_lightning != 0f)
			{
				m_sb.Append($"\n$inventory_lightning: <color=orange>{Mathf.RoundToInt(m_lightning)}</color> <color=yellow>({Mathf.RoundToInt(m_lightning * min)}-{Mathf.RoundToInt(m_lightning * max)}) </color>");
			}
			if (m_poison != 0f)
			{
				m_sb.Append($"\n$inventory_poison: <color=orange>{Mathf.RoundToInt(m_poison)}</color> <color=yellow>({Mathf.RoundToInt(m_poison * min)}-{Mathf.RoundToInt(m_poison * max)}) </color>");
			}
			if (m_spirit != 0f)
			{
				m_sb.Append($"\n$inventory_spirit: <color=orange>{Mathf.RoundToInt(m_spirit)}</color> <color=yellow>({Mathf.RoundToInt(m_spirit * min)}-{Mathf.RoundToInt(m_spirit * max)}) </color>");
			}
			return m_sb.ToString();
		}

		public string GetTooltipString()
		{
			m_sb.Clear();
			if (m_damage != 0f)
			{
				m_sb.Append($"\n$inventory_damage: <color=yellow>{m_damage}</color>");
			}
			if (m_blunt != 0f)
			{
				m_sb.Append($"\n$inventory_blunt: <color=yellow>{m_blunt}</color>");
			}
			if (m_slash != 0f)
			{
				m_sb.Append($"\n$inventory_slash: <color=yellow>{m_slash}</color>");
			}
			if (m_pierce != 0f)
			{
				m_sb.Append($"\n$inventory_pierce: <color=yellow>{m_pierce}</color>");
			}
			if (m_fire != 0f)
			{
				m_sb.Append($"\n$inventory_fire: <color=yellow>{m_fire}</color>");
			}
			if (m_frost != 0f)
			{
				m_sb.Append($"\n$inventory_frost: <color=yellow>{m_frost}</color>");
			}
			if (m_lightning != 0f)
			{
				m_sb.Append($"\n$inventory_lightning: <color=yellow>{m_frost}</color>");
			}
			if (m_poison != 0f)
			{
				m_sb.Append($"\n$inventory_poison: <color=yellow>{m_poison}</color>");
			}
			if (m_spirit != 0f)
			{
				m_sb.Append($"\n$inventory_spirit: <color=yellow>{m_spirit}</color>");
			}
			return m_sb.ToString();
		}

		public override string ToString()
		{
			m_sb.Clear();
			if (m_damage != 0f)
			{
				m_sb.Append($"Damage: {m_damage} ");
			}
			if (m_blunt != 0f)
			{
				m_sb.Append($"Blunt: {m_blunt} ");
			}
			if (m_slash != 0f)
			{
				m_sb.Append($"Slash: {m_slash} ");
			}
			if (m_pierce != 0f)
			{
				m_sb.Append($"Pierce: {m_pierce} ");
			}
			if (m_fire != 0f)
			{
				m_sb.Append($"Fire: {m_fire} ");
			}
			if (m_frost != 0f)
			{
				m_sb.Append($"Frost: {m_frost} ");
			}
			if (m_lightning != 0f)
			{
				m_sb.Append($"Lightning: {m_frost} ");
			}
			if (m_poison != 0f)
			{
				m_sb.Append($"Poison: {m_poison} ");
			}
			if (m_spirit != 0f)
			{
				m_sb.Append($"Spirit: {m_spirit} ");
			}
			if (m_chop != 0f)
			{
				m_sb.Append($"Chop: {m_chop} ");
			}
			if (m_pickaxe != 0f)
			{
				m_sb.Append($"Pickaxe: {m_pickaxe} ");
			}
			return m_sb.ToString();
		}
	}

	private static StringBuilder m_sb = new StringBuilder();

	public DamageTypes m_damage;

	public bool m_dodgeable;

	public bool m_blockable;

	public bool m_ranged;

	public bool m_ignorePVP;

	public short m_toolTier;

	public float m_pushForce;

	public float m_backstabBonus = 1f;

	public float m_staggerMultiplier = 1f;

	public Vector3 m_point = Vector3.zero;

	public Vector3 m_dir = Vector3.zero;

	public int m_statusEffectHash;

	public ZDOID m_attacker = ZDOID.None;

	public Skills.SkillType m_skill;

	public float m_skillRaiseAmount = 1f;

	public float m_skillLevel;

	public short m_itemLevel;

	public byte m_itemWorldLevel;

	public HitType m_hitType;

	public float m_healthReturn;

	public float m_radius;

	public short m_weakSpot = -1;

	public Collider m_hitCollider;

	public HitData()
	{
	}

	public HitData(float damage)
	{
		m_damage.m_damage = damage;
	}

	public HitData Clone()
	{
		return (HitData)MemberwiseClone();
	}

	public void Serialize(ref ZPackage pkg)
	{
		HitDefaults.SerializeFlags serializeFlags = HitDefaults.SerializeFlags.None;
		serializeFlags = (HitDefaults.SerializeFlags)((int)serializeFlags | ((!m_damage.m_damage.Equals(0f)) ? 1 : 0));
		serializeFlags |= ((!m_damage.m_blunt.Equals(0f)) ? HitDefaults.SerializeFlags.DamageBlunt : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_slash.Equals(0f)) ? HitDefaults.SerializeFlags.DamageSlash : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_pierce.Equals(0f)) ? HitDefaults.SerializeFlags.DamagePierce : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_chop.Equals(0f)) ? HitDefaults.SerializeFlags.DamageChop : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_pickaxe.Equals(0f)) ? HitDefaults.SerializeFlags.DamagePickaxe : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_fire.Equals(0f)) ? HitDefaults.SerializeFlags.DamageFire : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_frost.Equals(0f)) ? HitDefaults.SerializeFlags.DamageFrost : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_lightning.Equals(0f)) ? HitDefaults.SerializeFlags.DamageLightning : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_poison.Equals(0f)) ? HitDefaults.SerializeFlags.DamagePoison : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_damage.m_spirit.Equals(0f)) ? HitDefaults.SerializeFlags.DamageSpirit : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_pushForce.Equals(0f)) ? HitDefaults.SerializeFlags.PushForce : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_backstabBonus.Equals(1f)) ? HitDefaults.SerializeFlags.BackstabBonus : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_staggerMultiplier.Equals(1f)) ? HitDefaults.SerializeFlags.StaggerMultiplier : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((m_attacker != ZDOID.None) ? HitDefaults.SerializeFlags.Attacker : HitDefaults.SerializeFlags.None);
		serializeFlags |= ((!m_skillRaiseAmount.Equals(1f)) ? HitDefaults.SerializeFlags.SkillRaiseAmount : HitDefaults.SerializeFlags.None);
		pkg.Write((ushort)serializeFlags);
		if ((serializeFlags & HitDefaults.SerializeFlags.Damage) != 0)
		{
			pkg.Write(m_damage.m_damage);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageBlunt) != 0)
		{
			pkg.Write(m_damage.m_blunt);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageSlash) != 0)
		{
			pkg.Write(m_damage.m_slash);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamagePierce) != 0)
		{
			pkg.Write(m_damage.m_pierce);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageChop) != 0)
		{
			pkg.Write(m_damage.m_chop);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamagePickaxe) != 0)
		{
			pkg.Write(m_damage.m_pickaxe);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageFire) != 0)
		{
			pkg.Write(m_damage.m_fire);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageFrost) != 0)
		{
			pkg.Write(m_damage.m_frost);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageLightning) != 0)
		{
			pkg.Write(m_damage.m_lightning);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamagePoison) != 0)
		{
			pkg.Write(m_damage.m_poison);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.DamageSpirit) != 0)
		{
			pkg.Write(m_damage.m_spirit);
		}
		pkg.Write(m_toolTier);
		if ((serializeFlags & HitDefaults.SerializeFlags.PushForce) != 0)
		{
			pkg.Write(m_pushForce);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.BackstabBonus) != 0)
		{
			pkg.Write(m_backstabBonus);
		}
		if ((serializeFlags & HitDefaults.SerializeFlags.StaggerMultiplier) != 0)
		{
			pkg.Write(m_staggerMultiplier);
		}
		byte b = 0;
		if (m_dodgeable)
		{
			b = (byte)(b | 1u);
		}
		if (m_blockable)
		{
			b = (byte)(b | 2u);
		}
		if (m_ranged)
		{
			b = (byte)(b | 4u);
		}
		if (m_ignorePVP)
		{
			b = (byte)(b | 8u);
		}
		pkg.Write(b);
		pkg.Write(m_point);
		pkg.Write(m_dir);
		pkg.Write(m_statusEffectHash);
		if ((serializeFlags & HitDefaults.SerializeFlags.Attacker) != 0)
		{
			pkg.Write(m_attacker);
		}
		pkg.Write((short)m_skill);
		if ((serializeFlags & HitDefaults.SerializeFlags.SkillRaiseAmount) != 0)
		{
			pkg.Write(m_skillRaiseAmount);
		}
		pkg.Write((char)m_weakSpot);
		pkg.Write(m_skillLevel);
		pkg.Write(m_itemLevel);
		pkg.Write(m_itemWorldLevel);
		pkg.Write((byte)m_hitType);
		pkg.Write(m_healthReturn);
		pkg.Write(m_radius);
	}

	public void Deserialize(ref ZPackage pkg)
	{
		HitDefaults.SerializeFlags serializeFlags = HitDefaults.SerializeFlags.None;
		serializeFlags = (HitDefaults.SerializeFlags)pkg.ReadUShort();
		m_damage.m_damage = (((serializeFlags & HitDefaults.SerializeFlags.Damage) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_blunt = (((serializeFlags & HitDefaults.SerializeFlags.DamageBlunt) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_slash = (((serializeFlags & HitDefaults.SerializeFlags.DamageSlash) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_pierce = (((serializeFlags & HitDefaults.SerializeFlags.DamagePierce) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_chop = (((serializeFlags & HitDefaults.SerializeFlags.DamageChop) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_pickaxe = (((serializeFlags & HitDefaults.SerializeFlags.DamagePickaxe) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_fire = (((serializeFlags & HitDefaults.SerializeFlags.DamageFire) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_frost = (((serializeFlags & HitDefaults.SerializeFlags.DamageFrost) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_lightning = (((serializeFlags & HitDefaults.SerializeFlags.DamageLightning) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_poison = (((serializeFlags & HitDefaults.SerializeFlags.DamagePoison) != 0) ? pkg.ReadSingle() : 0f);
		m_damage.m_spirit = (((serializeFlags & HitDefaults.SerializeFlags.DamageSpirit) != 0) ? pkg.ReadSingle() : 0f);
		m_toolTier = pkg.ReadShort();
		m_pushForce = (((serializeFlags & HitDefaults.SerializeFlags.PushForce) != 0) ? pkg.ReadSingle() : 0f);
		m_backstabBonus = (((serializeFlags & HitDefaults.SerializeFlags.BackstabBonus) != 0) ? pkg.ReadSingle() : 1f);
		m_staggerMultiplier = (((serializeFlags & HitDefaults.SerializeFlags.StaggerMultiplier) != 0) ? pkg.ReadSingle() : 1f);
		byte b = pkg.ReadByte();
		m_dodgeable = (b & 1) != 0;
		m_blockable = (b & 2) != 0;
		m_ranged = (b & 4) != 0;
		m_ignorePVP = (b & 8) != 0;
		m_point = pkg.ReadVector3();
		m_dir = pkg.ReadVector3();
		m_statusEffectHash = pkg.ReadInt();
		m_attacker = (((serializeFlags & HitDefaults.SerializeFlags.Attacker) != 0) ? pkg.ReadZDOID() : HitDefaults.s_attackerDefault);
		m_skill = (Skills.SkillType)pkg.ReadShort();
		m_skillRaiseAmount = (((serializeFlags & HitDefaults.SerializeFlags.SkillRaiseAmount) != 0) ? pkg.ReadSingle() : 1f);
		m_weakSpot = (short)pkg.ReadChar();
		m_skillLevel = pkg.ReadSingle();
		m_itemLevel = pkg.ReadShort();
		m_itemWorldLevel = pkg.ReadByte();
		m_hitType = (HitType)pkg.ReadByte();
		m_healthReturn = pkg.ReadSingle();
		m_radius = pkg.ReadSingle();
	}

	public float GetTotalPhysicalDamage()
	{
		return m_damage.GetTotalPhysicalDamage();
	}

	public float GetTotalElementalDamage()
	{
		return m_damage.GetTotalElementalDamage();
	}

	public float GetTotalDamage()
	{
		Character attacker = GetAttacker();
		if (attacker != null && Game.m_worldLevel > 0 && !attacker.IsPlayer())
		{
			return m_damage.GetTotalDamage() + (float)(Game.m_worldLevel * Game.instance.m_worldLevelEnemyBaseDamage);
		}
		return m_damage.GetTotalDamage();
	}

	private float ApplyModifier(float baseDamage, DamageModifier mod, ref float normalDmg, ref float resistantDmg, ref float weakDmg, ref float immuneDmg)
	{
		if (mod == DamageModifier.Ignore)
		{
			return 0f;
		}
		float num = baseDamage;
		switch (mod)
		{
		case DamageModifier.Resistant:
			num /= 2f;
			resistantDmg += baseDamage;
			break;
		case DamageModifier.VeryResistant:
			num /= 4f;
			resistantDmg += baseDamage;
			break;
		case DamageModifier.Weak:
			num *= 1.5f;
			weakDmg += baseDamage;
			break;
		case DamageModifier.VeryWeak:
			num *= 2f;
			weakDmg += baseDamage;
			break;
		case DamageModifier.Immune:
			num = 0f;
			immuneDmg += baseDamage;
			break;
		default:
			normalDmg += baseDamage;
			break;
		}
		return num;
	}

	public void ApplyResistance(DamageModifiers modifiers, out DamageModifier significantModifier)
	{
		float normalDmg = m_damage.m_damage;
		float resistantDmg = 0f;
		float weakDmg = 0f;
		float immuneDmg = 0f;
		m_damage.m_blunt = ApplyModifier(m_damage.m_blunt, modifiers.m_blunt, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_slash = ApplyModifier(m_damage.m_slash, modifiers.m_slash, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_pierce = ApplyModifier(m_damage.m_pierce, modifiers.m_pierce, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_chop = ApplyModifier(m_damage.m_chop, modifiers.m_chop, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_pickaxe = ApplyModifier(m_damage.m_pickaxe, modifiers.m_pickaxe, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_fire = ApplyModifier(m_damage.m_fire, modifiers.m_fire, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_frost = ApplyModifier(m_damage.m_frost, modifiers.m_frost, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_lightning = ApplyModifier(m_damage.m_lightning, modifiers.m_lightning, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_poison = ApplyModifier(m_damage.m_poison, modifiers.m_poison, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		m_damage.m_spirit = ApplyModifier(m_damage.m_spirit, modifiers.m_spirit, ref normalDmg, ref resistantDmg, ref weakDmg, ref immuneDmg);
		significantModifier = DamageModifier.Immune;
		if (immuneDmg >= resistantDmg && immuneDmg >= weakDmg && immuneDmg >= normalDmg)
		{
			significantModifier = DamageModifier.Immune;
		}
		if (normalDmg >= resistantDmg && normalDmg >= weakDmg && normalDmg >= immuneDmg)
		{
			significantModifier = DamageModifier.Normal;
		}
		if (resistantDmg >= weakDmg && resistantDmg >= immuneDmg && resistantDmg >= normalDmg)
		{
			significantModifier = DamageModifier.Resistant;
		}
		if (weakDmg >= resistantDmg && weakDmg >= immuneDmg && weakDmg >= normalDmg)
		{
			significantModifier = DamageModifier.Weak;
		}
	}

	public void ApplyArmor(float ac)
	{
		m_damage.ApplyArmor(ac);
	}

	public void ApplyModifier(float multiplier)
	{
		m_damage.m_blunt *= multiplier;
		m_damage.m_slash *= multiplier;
		m_damage.m_pierce *= multiplier;
		m_damage.m_chop *= multiplier;
		m_damage.m_pickaxe *= multiplier;
		m_damage.m_fire *= multiplier;
		m_damage.m_frost *= multiplier;
		m_damage.m_lightning *= multiplier;
		m_damage.m_poison *= multiplier;
		m_damage.m_spirit *= multiplier;
	}

	public float GetTotalBlockableDamage()
	{
		return m_damage.GetTotalBlockableDamage();
	}

	public void BlockDamage(float damage)
	{
		float totalBlockableDamage = GetTotalBlockableDamage();
		float num = Mathf.Max(0f, totalBlockableDamage - damage);
		if (!(totalBlockableDamage <= 0f))
		{
			float num2 = num / totalBlockableDamage;
			m_damage.m_blunt *= num2;
			m_damage.m_slash *= num2;
			m_damage.m_pierce *= num2;
			m_damage.m_fire *= num2;
			m_damage.m_frost *= num2;
			m_damage.m_lightning *= num2;
			m_damage.m_poison *= num2;
			m_damage.m_spirit *= num2;
		}
	}

	public bool HaveAttacker()
	{
		return !m_attacker.IsNone();
	}

	public Character GetAttacker()
	{
		if (m_attacker.IsNone())
		{
			return null;
		}
		if (ZNetScene.instance == null)
		{
			return null;
		}
		GameObject gameObject = ZNetScene.instance.FindInstance(m_attacker);
		if (gameObject == null)
		{
			return null;
		}
		return gameObject.GetComponent<Character>();
	}

	public void SetAttacker(Character attacker)
	{
		if ((bool)attacker)
		{
			m_attacker = attacker.GetZDOID();
		}
		else
		{
			m_attacker = ZDOID.None;
		}
	}

	public bool CheckToolTier(int minToolTier, bool alwaysAllowTierZero = false)
	{
		if (m_itemWorldLevel < Game.m_worldLevel && ZoneSystem.instance.GetGlobalKey(GlobalKeys.WorldLevelLockedTools) && (minToolTier > 0 || !alwaysAllowTierZero))
		{
			return false;
		}
		if (m_toolTier < minToolTier)
		{
			return false;
		}
		return true;
	}

	public override string ToString()
	{
		m_sb.Clear();
		m_sb.Append($"Hit: {m_hitType}, {m_damage}");
		if (m_toolTier > 0)
		{
			m_sb.Append($", Tooltier: {m_toolTier}");
		}
		if (m_itemLevel > 0)
		{
			m_sb.Append($", ItemLevel: {m_itemLevel}");
		}
		if (m_skill != 0)
		{
			m_sb.Append($", Skill: {m_skill}");
		}
		if (m_statusEffectHash > 0)
		{
			m_sb.Append($", Statushash: {m_statusEffectHash}");
		}
		Character attacker = GetAttacker();
		if ((object)attacker != null)
		{
			m_sb.Append(", Attacker: " + attacker.m_name);
		}
		return m_sb.ToString();
	}
}
