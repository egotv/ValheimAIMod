using System;
using UnityEngine;

public class StatusEffect : ScriptableObject
{
	public enum StatusAttribute
	{
		None = 0,
		ColdResistance = 1,
		DoubleImpactDamage = 2,
		SailingPower = 4
	}

	[Header("__Common__")]
	public string m_name = "";

	public string m_category = "";

	public Sprite m_icon;

	public bool m_flashIcon;

	public bool m_cooldownIcon;

	[TextArea]
	public string m_tooltip = "";

	[BitMask(typeof(StatusAttribute))]
	public StatusAttribute m_attributes;

	public MessageHud.MessageType m_startMessageType = MessageHud.MessageType.TopLeft;

	public string m_startMessage = "";

	public MessageHud.MessageType m_stopMessageType = MessageHud.MessageType.TopLeft;

	public string m_stopMessage = "";

	public MessageHud.MessageType m_repeatMessageType = MessageHud.MessageType.TopLeft;

	public string m_repeatMessage = "";

	public float m_repeatInterval;

	public float m_ttl;

	public EffectList m_startEffects = new EffectList();

	public EffectList m_stopEffects = new EffectList();

	[Header("__Guardian power__")]
	public float m_cooldown;

	public string m_activationAnimation = "gpower";

	[NonSerialized]
	public bool m_isNew = true;

	private float m_msgTimer;

	protected Character m_character;

	protected float m_time;

	protected GameObject[] m_startEffectInstances;

	private int m_nameHash;

	public StatusEffect Clone()
	{
		return MemberwiseClone() as StatusEffect;
	}

	public virtual bool CanAdd(Character character)
	{
		return true;
	}

	public virtual void Setup(Character character)
	{
		m_character = character;
		if (!string.IsNullOrEmpty(m_startMessage))
		{
			m_character.Message(m_startMessageType, m_startMessage);
		}
		TriggerStartEffects();
	}

	public virtual void SetAttacker(Character attacker)
	{
	}

	public virtual string GetTooltipString()
	{
		return m_tooltip;
	}

	protected virtual void OnApplicationQuit()
	{
		m_startEffectInstances = null;
	}

	public virtual void OnDestroy()
	{
		RemoveStartEffects();
	}

	protected void TriggerStartEffects()
	{
		RemoveStartEffects();
		float radius = m_character.GetRadius();
		int variant = -1;
		Player player = m_character as Player;
		if ((bool)player)
		{
			variant = player.GetPlayerModel();
		}
		m_startEffectInstances = m_startEffects.Create(m_character.GetCenterPoint(), m_character.transform.rotation, m_character.transform, radius * 2f, variant);
	}

	private void RemoveStartEffects()
	{
		if (m_startEffectInstances == null || !(ZNetScene.instance != null))
		{
			return;
		}
		GameObject[] startEffectInstances = m_startEffectInstances;
		foreach (GameObject gameObject in startEffectInstances)
		{
			if ((bool)gameObject)
			{
				ZNetView component = gameObject.GetComponent<ZNetView>();
				if (component.IsValid())
				{
					component.ClaimOwnership();
					component.Destroy();
				}
			}
		}
		m_startEffectInstances = null;
	}

	public virtual void Stop()
	{
		RemoveStartEffects();
		m_stopEffects.Create(m_character.transform.position, m_character.transform.rotation);
		if (!string.IsNullOrEmpty(m_stopMessage))
		{
			m_character.Message(m_stopMessageType, m_stopMessage);
		}
	}

	public virtual void UpdateStatusEffect(float dt)
	{
		m_time += dt;
		if (m_repeatInterval > 0f && !string.IsNullOrEmpty(m_repeatMessage))
		{
			m_msgTimer += dt;
			if (m_msgTimer > m_repeatInterval)
			{
				m_msgTimer = 0f;
				m_character.Message(m_repeatMessageType, m_repeatMessage);
			}
		}
	}

	public virtual bool IsDone()
	{
		if (m_ttl > 0f && m_time > m_ttl)
		{
			return true;
		}
		return false;
	}

	public virtual void ResetTime()
	{
		m_time = 0f;
	}

	public virtual void SetLevel(int itemLevel, float skillLevel)
	{
	}

	public float GetDuration()
	{
		return m_time;
	}

	public float GetRemaningTime()
	{
		return m_ttl - m_time;
	}

	public virtual string GetIconText()
	{
		if (m_ttl > 0f)
		{
			return GetTimeString(m_ttl - GetDuration());
		}
		return "";
	}

	public static string GetTimeString(float time, bool sufix = false, bool alwaysShowMinutes = false)
	{
		if (time > 0f)
		{
			int num = Mathf.CeilToInt(time);
			int num2 = (int)((float)num / 60f);
			int num3 = Mathf.Max(0, num - num2 * 60);
			if (sufix)
			{
				if (num2 > 0 || alwaysShowMinutes)
				{
					return num2 + "m:" + num3.ToString("00") + "s";
				}
				return num3 + "s";
			}
			if (num2 > 0 || alwaysShowMinutes)
			{
				return num2 + ":" + num3.ToString("00");
			}
			return num3.ToString();
		}
		return "";
	}

	public virtual void ModifyAttack(Skills.SkillType skill, ref HitData hitData)
	{
	}

	public virtual void ModifyHealthRegen(ref float regenMultiplier)
	{
	}

	public virtual void ModifyStaminaRegen(ref float staminaRegen)
	{
	}

	public virtual void ModifyEitrRegen(ref float eitrRegen)
	{
	}

	public virtual void ModifyDamageMods(ref HitData.DamageModifiers modifiers)
	{
	}

	public virtual void ModifyRaiseSkill(Skills.SkillType skill, ref float value)
	{
	}

	public virtual void ModifySkillLevel(Skills.SkillType skill, ref float level)
	{
	}

	public virtual void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
	{
	}

	public virtual void ModifyJump(Vector3 baseJump, ref Vector3 jump)
	{
	}

	public virtual void ModifyWalkVelocity(ref Vector3 vel)
	{
	}

	public virtual void ModifyFallDamage(float baseDamage, ref float damage)
	{
	}

	public virtual void ModifyNoise(float baseNoise, ref float noise)
	{
	}

	public virtual void ModifyStealth(float baseStealth, ref float stealth)
	{
	}

	public virtual void ModifyMaxCarryWeight(float baseLimit, ref float limit)
	{
	}

	public virtual void ModifyRunStaminaDrain(float baseDrain, ref float drain)
	{
	}

	public virtual void ModifyJumpStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void ModifyAttackStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void ModifyBlockStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void ModifyDodgeStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void ModifySwimStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void ModifyHomeItemStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void ModifySneakStaminaUsage(float baseStaminaUse, ref float staminaUse)
	{
	}

	public virtual void OnDamaged(HitData hit, Character attacker)
	{
	}

	public bool HaveAttribute(StatusAttribute value)
	{
		return (m_attributes & value) != 0;
	}

	public int NameHash()
	{
		if (m_nameHash == 0)
		{
			m_nameHash = base.name.GetStableHashCode();
		}
		return m_nameHash;
	}
}
