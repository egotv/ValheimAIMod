using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ItemDrop : MonoBehaviour, Hoverable, Interactable
{
	[Serializable]
	public class ItemData
	{
		public enum ItemType
		{
			None = 0,
			Material = 1,
			Consumable = 2,
			OneHandedWeapon = 3,
			Bow = 4,
			Shield = 5,
			Helmet = 6,
			Chest = 7,
			Ammo = 9,
			Customization = 10,
			Legs = 11,
			Hands = 12,
			Trophy = 13,
			TwoHandedWeapon = 14,
			Torch = 15,
			Misc = 16,
			Shoulder = 17,
			Utility = 18,
			Tool = 19,
			Attach_Atgeir = 20,
			Fish = 21,
			TwoHandedWeaponLeft = 22,
			AmmoNonEquipable = 23
		}

		public enum AnimationState
		{
			Unarmed,
			OneHanded,
			TwoHandedClub,
			Bow,
			Shield,
			Torch,
			LeftTorch,
			Atgeir,
			TwoHandedAxe,
			FishingRod,
			Crossbow,
			Knives,
			Staves,
			Greatsword,
			MagicItem,
			DualAxes
		}

		public enum AiTarget
		{
			Enemy,
			FriendHurt,
			Friend
		}

		public enum HelmetHairType
		{
			Default,
			Hidden,
			HiddenHat,
			HiddenHood,
			HiddenNeck,
			HiddenScarf
		}

		public enum AccessoryType
		{
			Hair,
			Beard
		}

		[Serializable]
		public class HelmetHairSettings
		{
			public HelmetHairType m_setting;

			public ItemDrop m_hairPrefab;
		}

		[Serializable]
		public class SharedData
		{
			public string m_name = "";

			public string m_dlc = "";

			public ItemType m_itemType = ItemType.Misc;

			public Sprite[] m_icons = Array.Empty<Sprite>();

			public ItemType m_attachOverride;

			[TextArea]
			public string m_description = "";

			public int m_maxStackSize = 1;

			public bool m_autoStack = true;

			public int m_maxQuality = 1;

			public float m_scaleByQuality;

			public float m_weight = 1f;

			public float m_scaleWeightByQuality;

			public int m_value;

			public bool m_teleportable = true;

			public bool m_questItem;

			public float m_equipDuration = 1f;

			public int m_variants;

			public Vector2Int m_trophyPos = Vector2Int.zero;

			public PieceTable m_buildPieces;

			public bool m_centerCamera;

			public string m_setName = "";

			public int m_setSize;

			public StatusEffect m_setStatusEffect;

			public StatusEffect m_equipStatusEffect;

			[Header("Stat modifiers")]
			public float m_eitrRegenModifier;

			public float m_movementModifier;

			public float m_homeItemsStaminaModifier;

			public float m_heatResistanceModifier;

			public float m_jumpStaminaModifier;

			public float m_attackStaminaModifier;

			public float m_blockStaminaModifier;

			public float m_dodgeStaminaModifier;

			public float m_swimStaminaModifier;

			public float m_sneakStaminaModifier;

			public float m_runStaminaModifier;

			[Header("Food settings")]
			public float m_food;

			public float m_foodStamina;

			public float m_foodEitr;

			public float m_foodBurnTime;

			public float m_foodRegen;

			[Header("Armor settings")]
			public Material m_armorMaterial;

			public HelmetHairType m_helmetHideHair = HelmetHairType.Hidden;

			public HelmetHairType m_helmetHideBeard;

			public List<HelmetHairSettings> m_helmetHairSettings = new List<HelmetHairSettings>();

			public List<HelmetHairSettings> m_helmetBeardSettings = new List<HelmetHairSettings>();

			public float m_armor = 10f;

			public float m_armorPerLevel = 1f;

			public List<HitData.DamageModPair> m_damageModifiers = new List<HitData.DamageModPair>();

			[Header("Shield settings")]
			public float m_blockPower = 10f;

			public float m_blockPowerPerLevel;

			public float m_deflectionForce;

			public float m_deflectionForcePerLevel;

			public float m_timedBlockBonus = 1.5f;

			[Header("Weapon")]
			public AnimationState m_animationState = AnimationState.OneHanded;

			public Skills.SkillType m_skillType = Skills.SkillType.Swords;

			public int m_toolTier;

			public HitData.DamageTypes m_damages;

			public HitData.DamageTypes m_damagesPerLevel;

			public float m_attackForce = 30f;

			public float m_backstabBonus = 4f;

			public bool m_dodgeable;

			public bool m_blockable;

			public bool m_tamedOnly;

			public bool m_alwaysRotate;

			public StatusEffect m_attackStatusEffect;

			public float m_attackStatusEffectChance = 1f;

			public GameObject m_spawnOnHit;

			public GameObject m_spawnOnHitTerrain;

			public bool m_projectileToolTip = true;

			[Header("Ammo")]
			public string m_ammoType = "";

			[Header("Attacks")]
			public Attack m_attack;

			public Attack m_secondaryAttack;

			[Header("Durability")]
			public bool m_useDurability;

			public bool m_destroyBroken = true;

			public bool m_canBeReparied = true;

			public float m_maxDurability = 100f;

			public float m_durabilityPerLevel = 50f;

			public float m_useDurabilityDrain = 1f;

			public float m_durabilityDrain;

			[Header("AI")]
			public float m_aiAttackRange = 2f;

			public float m_aiAttackRangeMin;

			public float m_aiAttackInterval = 2f;

			public float m_aiAttackMaxAngle = 5f;

			public bool m_aiInvertAngleCheck;

			public bool m_aiWhenFlying = true;

			public float m_aiWhenFlyingAltitudeMin;

			public float m_aiWhenFlyingAltitudeMax = 999999f;

			public bool m_aiWhenWalking = true;

			public bool m_aiWhenSwiming = true;

			public bool m_aiPrioritized;

			public bool m_aiInDungeonOnly;

			public bool m_aiInMistOnly;

			[Range(0f, 1f)]
			public float m_aiMaxHealthPercentage = 1f;

			[Range(0f, 1f)]
			public float m_aiMinHealthPercentage;

			public AiTarget m_aiTargetType;

			[Header("Effects")]
			public EffectList m_hitEffect = new EffectList();

			public EffectList m_hitTerrainEffect = new EffectList();

			public EffectList m_blockEffect = new EffectList();

			public EffectList m_startEffect = new EffectList();

			public EffectList m_holdStartEffect = new EffectList();

			public EffectList m_equipEffect = new EffectList();

			public EffectList m_unequipEffect = new EffectList();

			public EffectList m_triggerEffect = new EffectList();

			public EffectList m_trailStartEffect = new EffectList();

			[Header("Consumable")]
			public StatusEffect m_consumeStatusEffect;

			public override string ToString()
			{
				return string.Format("{0}: {1}, max stack: {2}, attacks: {3} / {4}", "SharedData", m_name, m_maxStackSize, m_attack, m_secondaryAttack);
			}
		}

		private static StringBuilder m_stringBuilder = new StringBuilder(256);

		public int m_stack = 1;

		public float m_durability = 100f;

		public int m_quality = 1;

		public int m_variant;

		public int m_worldLevel = Game.m_worldLevel;

		public bool m_pickedUp;

		public SharedData m_shared;

		[NonSerialized]
		public long m_crafterID;

		[NonSerialized]
		public string m_crafterName = "";

		[NonSerialized]
		public Dictionary<string, string> m_customData = new Dictionary<string, string>();

		[NonSerialized]
		public Vector2i m_gridPos = Vector2i.zero;

		[NonSerialized]
		public bool m_equipped;

		[NonSerialized]
		public GameObject m_dropPrefab;

		[NonSerialized]
		public float m_lastAttackTime;

		[NonSerialized]
		public GameObject m_lastProjectile;

		public ItemData Clone()
		{
			ItemData obj = MemberwiseClone() as ItemData;
			obj.m_customData = new Dictionary<string, string>(m_customData);
			return obj;
		}

		public bool IsEquipable()
		{
			if (m_shared.m_itemType != ItemType.Tool && m_shared.m_itemType != ItemType.OneHandedWeapon && m_shared.m_itemType != ItemType.TwoHandedWeapon && m_shared.m_itemType != ItemType.TwoHandedWeaponLeft && m_shared.m_itemType != ItemType.Bow && m_shared.m_itemType != ItemType.Shield && m_shared.m_itemType != ItemType.Helmet && m_shared.m_itemType != ItemType.Chest && m_shared.m_itemType != ItemType.Legs && m_shared.m_itemType != ItemType.Shoulder && m_shared.m_itemType != ItemType.Ammo && m_shared.m_itemType != ItemType.Torch)
			{
				return m_shared.m_itemType == ItemType.Utility;
			}
			return true;
		}

		public bool IsWeapon()
		{
			if (m_shared.m_itemType != ItemType.OneHandedWeapon && m_shared.m_itemType != ItemType.Bow && m_shared.m_itemType != ItemType.TwoHandedWeapon && m_shared.m_itemType != ItemType.TwoHandedWeaponLeft)
			{
				return m_shared.m_itemType == ItemType.Torch;
			}
			return true;
		}

		public bool IsTwoHanded()
		{
			if (m_shared.m_itemType != ItemType.TwoHandedWeapon && m_shared.m_itemType != ItemType.TwoHandedWeaponLeft)
			{
				return m_shared.m_itemType == ItemType.Bow;
			}
			return true;
		}

		public bool HavePrimaryAttack()
		{
			return !string.IsNullOrEmpty(m_shared.m_attack.m_attackAnimation);
		}

		public bool HaveSecondaryAttack()
		{
			return !string.IsNullOrEmpty(m_shared.m_secondaryAttack.m_attackAnimation);
		}

		public float GetArmor()
		{
			return GetArmor(m_quality, m_worldLevel);
		}

		public float GetArmor(int quality, float worldLevel)
		{
			return m_shared.m_armor + (float)Mathf.Max(0, quality - 1) * m_shared.m_armorPerLevel + worldLevel * (float)Game.instance.m_worldLevelGearBaseAC;
		}

		public int GetValue()
		{
			return m_shared.m_value * m_stack;
		}

		public float GetWeight()
		{
			float num = m_shared.m_weight * (float)m_stack;
			if (m_shared.m_scaleWeightByQuality != 0f && m_quality != 1)
			{
				num += num * (float)(m_quality - 1) * m_shared.m_scaleWeightByQuality;
			}
			return num;
		}

		public float GetNonStackedWeight()
		{
			float num = m_shared.m_weight;
			if (m_shared.m_scaleWeightByQuality != 0f && m_quality != 1)
			{
				num += num * (float)(m_quality - 1) * m_shared.m_scaleWeightByQuality;
			}
			return num;
		}

		public HitData.DamageTypes GetDamage()
		{
			return GetDamage(m_quality, m_worldLevel);
		}

		public float GetDurabilityPercentage()
		{
			float maxDurability = GetMaxDurability();
			if (maxDurability == 0f)
			{
				return 1f;
			}
			return Mathf.Clamp01(m_durability / maxDurability);
		}

		public float GetMaxDurability()
		{
			return GetMaxDurability(m_quality);
		}

		public float GetMaxDurability(int quality)
		{
			return m_shared.m_maxDurability + (float)Mathf.Max(0, quality - 1) * m_shared.m_durabilityPerLevel;
		}

		public HitData.DamageTypes GetDamage(int quality, float worldLevel)
		{
			HitData.DamageTypes damages = m_shared.m_damages;
			if (quality > 1)
			{
				damages.Add(m_shared.m_damagesPerLevel, quality - 1);
			}
			if (worldLevel > 0f)
			{
				damages.IncreaseEqually(worldLevel * (float)Game.instance.m_worldLevelGearBaseDamage, seperateUtilityDamage: true);
			}
			return damages;
		}

		public float GetBaseBlockPower()
		{
			return GetBaseBlockPower(m_quality);
		}

		public float GetBaseBlockPower(int quality)
		{
			return m_shared.m_blockPower + (float)Mathf.Max(0, quality - 1) * m_shared.m_blockPowerPerLevel;
		}

		public float GetBlockPower(float skillFactor)
		{
			return GetBlockPower(m_quality, skillFactor);
		}

		public float GetBlockPower(int quality, float skillFactor)
		{
			float baseBlockPower = GetBaseBlockPower(quality);
			return baseBlockPower + baseBlockPower * skillFactor * 0.5f;
		}

		public float GetBlockPowerTooltip(int quality)
		{
			if (Player.m_localPlayer == null)
			{
				return 0f;
			}
			float skillFactor = Player.m_localPlayer.GetSkillFactor(Skills.SkillType.Blocking);
			return GetBlockPower(quality, skillFactor);
		}

		public float GetDrawStaminaDrain()
		{
			if (m_shared.m_attack.m_drawStaminaDrain <= 0f)
			{
				return 0f;
			}
			float drawStaminaDrain = m_shared.m_attack.m_drawStaminaDrain;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(m_shared.m_skillType);
			return drawStaminaDrain - drawStaminaDrain * 0.33f * skillFactor;
		}

		public float GetDrawEitrDrain()
		{
			if (m_shared.m_attack.m_drawEitrDrain <= 0f)
			{
				return 0f;
			}
			float drawEitrDrain = m_shared.m_attack.m_drawEitrDrain;
			float skillFactor = Player.m_localPlayer.GetSkillFactor(m_shared.m_skillType);
			return drawEitrDrain - drawEitrDrain * 0.33f * skillFactor;
		}

		public float GetWeaponLoadingTime()
		{
			if (m_shared.m_attack.m_requiresReload)
			{
				float skillFactor = Player.m_localPlayer.GetSkillFactor(m_shared.m_skillType);
				return Mathf.Lerp(m_shared.m_attack.m_reloadTime, m_shared.m_attack.m_reloadTime * 0.5f, skillFactor);
			}
			return 1f;
		}

		public float GetDeflectionForce()
		{
			return GetDeflectionForce(m_quality);
		}

		public float GetDeflectionForce(int quality)
		{
			return m_shared.m_deflectionForce + (float)Mathf.Max(0, quality - 1) * m_shared.m_deflectionForcePerLevel;
		}

		public Vector3 GetScale()
		{
			return GetScale(m_quality);
		}

		public Vector3 GetScale(float quality)
		{
			float num = 1f + (quality - 1f) * m_shared.m_scaleByQuality;
			return new Vector3(num, num, num);
		}

		public string GetTooltip()
		{
			return GetTooltip(this, m_quality, crafting: false, m_worldLevel);
		}

		public Sprite GetIcon()
		{
			return m_shared.m_icons[m_variant];
		}

		private static void AddHandedTip(ItemData item, StringBuilder text)
		{
			switch (item.m_shared.m_itemType)
			{
			case ItemType.OneHandedWeapon:
			case ItemType.Shield:
			case ItemType.Torch:
				text.Append("\n$item_onehanded");
				break;
			case ItemType.Bow:
			case ItemType.TwoHandedWeapon:
			case ItemType.Tool:
			case ItemType.TwoHandedWeaponLeft:
				text.Append("\n$item_twohanded");
				break;
			}
		}

		private static void AddBlockTooltip(ItemData item, int qualityLevel, StringBuilder text)
		{
			text.AppendFormat("\n$item_blockarmor: <color=orange>{0}</color> <color=yellow>({1})</color>", item.GetBaseBlockPower(qualityLevel), item.GetBlockPowerTooltip(qualityLevel).ToString("0"));
			text.AppendFormat("\n$item_blockforce: <color=orange>{0}</color>", item.GetDeflectionForce(qualityLevel));
			if (item.m_shared.m_timedBlockBonus > 1f)
			{
				text.AppendFormat("\n$item_parrybonus: <color=orange>{0}x</color>", item.m_shared.m_timedBlockBonus);
			}
			string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
			if (damageModifiersTooltipString.Length > 0)
			{
				text.Append(damageModifiersTooltipString);
			}
		}

		public static string GetTooltip(ItemData item, int qualityLevel, bool crafting, float worldLevel)
		{
			Player localPlayer = Player.m_localPlayer;
			m_stringBuilder.Clear();
			m_stringBuilder.Append(item.m_shared.m_description);
			m_stringBuilder.Append("\n");
			if (item.m_shared.m_dlc.Length > 0)
			{
				m_stringBuilder.Append("\n<color=#00FFFF>$item_dlc</color>");
			}
			if (item.m_worldLevel > 0)
			{
				m_stringBuilder.Append("\n<color=orange>$item_newgameplusitem " + ((item.m_worldLevel != 1) ? item.m_worldLevel.ToString() : "") + "</color>");
			}
			AddHandedTip(item, m_stringBuilder);
			if (item.m_crafterID != 0L)
			{
				m_stringBuilder.AppendFormat("\n$item_crafter: <color=orange>{0}</color>", CensorShittyWords.FilterUGC(item.m_crafterName, UGCType.CharacterName, null, item.m_crafterID));
			}
			if (!item.m_shared.m_teleportable && !ZoneSystem.instance.GetGlobalKey(GlobalKeys.TeleportAll))
			{
				m_stringBuilder.Append("\n<color=orange>$item_noteleport</color>");
			}
			if (item.m_shared.m_value > 0)
			{
				m_stringBuilder.AppendFormat("\n$item_value: <color=orange>{0}  ({1})</color>", item.GetValue(), item.m_shared.m_value);
			}
			if (item.m_shared.m_maxStackSize > 1)
			{
				m_stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0} ({1} $item_total)</color>", item.GetNonStackedWeight().ToString("0.0"), item.GetWeight().ToString("0.0"));
			}
			else
			{
				m_stringBuilder.AppendFormat("\n$item_weight: <color=orange>{0}</color>", item.GetWeight().ToString("0.0"));
			}
			if (item.m_shared.m_maxQuality > 1 && !crafting)
			{
				m_stringBuilder.AppendFormat("\n$item_quality: <color=orange>{0}</color>", qualityLevel);
			}
			if (item.m_shared.m_useDurability)
			{
				if (crafting)
				{
					float maxDurability = item.GetMaxDurability(qualityLevel);
					m_stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}</color>", maxDurability);
				}
				else
				{
					float maxDurability2 = item.GetMaxDurability(qualityLevel);
					float durability = item.m_durability;
					m_stringBuilder.AppendFormat("\n$item_durability: <color=orange>{0}%</color> <color=yellow>({1}/{2})</color>", (item.GetDurabilityPercentage() * 100f).ToString("0"), durability.ToString("0"), maxDurability2.ToString("0"));
				}
				if (item.m_shared.m_canBeReparied && !crafting)
				{
					Recipe recipe = ObjectDB.instance.GetRecipe(item);
					if (recipe != null)
					{
						int minStationLevel = recipe.m_minStationLevel;
						m_stringBuilder.AppendFormat("\n$item_repairlevel: <color=orange>{0}</color>", minStationLevel.ToString());
					}
				}
			}
			switch (item.m_shared.m_itemType)
			{
			case ItemType.Ammo:
			case ItemType.AmmoNonEquipable:
				m_stringBuilder.Append(item.GetDamage(qualityLevel, worldLevel).GetTooltipString(item.m_shared.m_skillType));
				m_stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				break;
			case ItemType.OneHandedWeapon:
			case ItemType.Bow:
			case ItemType.TwoHandedWeapon:
			case ItemType.Torch:
			case ItemType.TwoHandedWeaponLeft:
			{
				m_stringBuilder.Append(item.GetDamage(qualityLevel, worldLevel).GetTooltipString(item.m_shared.m_skillType));
				if (item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_damagemultipliertotal: <color=orange>{0}%</color>", item.m_shared.m_attack.m_damageMultiplierByTotalHealthMissing * 100f);
				}
				if (item.m_shared.m_attack.m_damageMultiplierPerMissingHP > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_damagemultiplierhp: <color=orange>{0}%</color>", item.m_shared.m_attack.m_damageMultiplierPerMissingHP * 100f);
				}
				if (item.m_shared.m_attack.m_attackStamina > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_staminause: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackStamina);
				}
				if (item.m_shared.m_attack.m_attackEitr > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_eitruse: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackEitr);
				}
				if (item.m_shared.m_attack.m_attackHealth > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_healthuse: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackHealth);
				}
				if (item.m_shared.m_attack.m_attackHealthReturnHit > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_healthhitreturn: <color=orange>{0}</color>", item.m_shared.m_attack.m_attackHealthReturnHit);
				}
				if (item.m_shared.m_attack.m_attackHealthPercentage > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_healthuse: <color=orange>{0}%</color>", item.m_shared.m_attack.m_attackHealthPercentage.ToString("0.0"));
				}
				if (item.m_shared.m_attack.m_drawStaminaDrain > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_staminahold: <color=orange>{0}</color>/s", item.m_shared.m_attack.m_drawStaminaDrain);
				}
				AddBlockTooltip(item, qualityLevel, m_stringBuilder);
				if (item.m_shared.m_attackForce > 0f)
				{
					m_stringBuilder.AppendFormat("\n$item_knockback: <color=orange>{0}</color>", item.m_shared.m_attackForce);
				}
				if (item.m_shared.m_backstabBonus > 1f)
				{
					m_stringBuilder.AppendFormat("\n$item_backstab: <color=orange>{0}x</color>", item.m_shared.m_backstabBonus);
				}
				if (item.m_shared.m_tamedOnly)
				{
					m_stringBuilder.AppendFormat("\n<color=orange>$item_tamedonly</color>");
				}
				string projectileTooltip = item.GetProjectileTooltip(qualityLevel);
				if (projectileTooltip.Length > 0 && item.m_shared.m_projectileToolTip)
				{
					m_stringBuilder.Append("\n\n");
					m_stringBuilder.Append(projectileTooltip);
				}
				break;
			}
			case ItemType.Helmet:
			case ItemType.Chest:
			case ItemType.Legs:
			case ItemType.Shoulder:
			{
				m_stringBuilder.AppendFormat("\n$item_armor: <color=orange>{0}</color>", item.GetArmor(qualityLevel, worldLevel));
				string damageModifiersTooltipString = SE_Stats.GetDamageModifiersTooltipString(item.m_shared.m_damageModifiers);
				if (damageModifiersTooltipString.Length > 0)
				{
					m_stringBuilder.Append(damageModifiersTooltipString);
				}
				break;
			}
			case ItemType.Shield:
				AddBlockTooltip(item, qualityLevel, m_stringBuilder);
				break;
			case ItemType.Consumable:
				if (item.m_shared.m_food > 0f || item.m_shared.m_foodStamina > 0f || item.m_shared.m_foodEitr > 0f)
				{
					float maxHealth = localPlayer.GetMaxHealth();
					float maxStamina = localPlayer.GetMaxStamina();
					float maxEitr = localPlayer.GetMaxEitr();
					if (item.m_shared.m_food > 0f)
					{
						m_stringBuilder.AppendFormat("\n$item_food_health: <color=#ff8080ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_food, maxHealth.ToString("0"));
					}
					if (item.m_shared.m_foodStamina > 0f)
					{
						m_stringBuilder.AppendFormat("\n$item_food_stamina: <color=#ffff80ff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodStamina, maxStamina.ToString("0"));
					}
					if (item.m_shared.m_foodEitr > 0f)
					{
						m_stringBuilder.AppendFormat("\n$item_food_eitr: <color=#9090ffff>{0}</color>  ($item_current:<color=yellow>{1}</color>)", item.m_shared.m_foodEitr, maxEitr.ToString("0"));
					}
					m_stringBuilder.AppendFormat("\n$item_food_duration: <color=orange>{0}</color>", GetDurationString(item.m_shared.m_foodBurnTime));
					if (item.m_shared.m_foodRegen > 0f)
					{
						m_stringBuilder.AppendFormat("\n$item_food_regen: <color=orange>{0} hp/tick</color>", item.m_shared.m_foodRegen);
					}
				}
				break;
			}
			float skillLevel = Player.m_localPlayer.GetSkillLevel(item.m_shared.m_skillType);
			string statusEffectTooltip = item.GetStatusEffectTooltip(qualityLevel, skillLevel);
			if (statusEffectTooltip.Length > 0)
			{
				m_stringBuilder.Append("\n\n");
				m_stringBuilder.Append(statusEffectTooltip);
			}
			string chainTooltip = item.GetChainTooltip(qualityLevel, skillLevel);
			if (chainTooltip.Length > 0)
			{
				m_stringBuilder.Append("\n\n");
				m_stringBuilder.Append(chainTooltip);
			}
			if (item.m_shared.m_eitrRegenModifier > 0f && localPlayer != null)
			{
				float equipmentEitrRegenModifier = localPlayer.GetEquipmentEitrRegenModifier();
				m_stringBuilder.AppendFormat("\n$item_eitrregen_modifier: <color=orange>{0}%</color> ($item_total:<color=yellow>{1}%</color>)", (item.m_shared.m_eitrRegenModifier * 100f).ToString("+0;-0"), (equipmentEitrRegenModifier * 100f).ToString("+0;-0"));
			}
			if (localPlayer != null)
			{
				localPlayer.AppendEquipmentModifierTooltips(item, m_stringBuilder);
			}
			string setStatusEffectTooltip = item.GetSetStatusEffectTooltip(qualityLevel, skillLevel);
			if (setStatusEffectTooltip.Length > 0)
			{
				m_stringBuilder.AppendFormat("\n\n$item_seteffect (<color=orange>{0}</color> $item_parts):<color=orange>{1}</color>\n{2}", item.m_shared.m_setSize, item.m_shared.m_setStatusEffect.m_name, setStatusEffectTooltip);
			}
			return m_stringBuilder.ToString();
		}

		public static string GetDurationString(float time)
		{
			int num = Mathf.CeilToInt(time);
			int num2 = (int)((float)num / 60f);
			int num3 = Mathf.Max(0, num - num2 * 60);
			if (num2 > 0 && num3 > 0)
			{
				return num2 + "m " + num3 + "s";
			}
			if (num2 > 0)
			{
				return num2 + "m ";
			}
			return num3 + "s";
		}

		private string GetStatusEffectTooltip(int quality, float skillLevel)
		{
			if ((bool)m_shared.m_attackStatusEffect)
			{
				m_shared.m_attackStatusEffect.SetLevel(quality, skillLevel);
				string text = ((m_shared.m_attackStatusEffectChance < 1f) ? $"$item_chancetoapplyse <color=orange>{m_shared.m_attackStatusEffectChance * 100f}%</color>\n" : "");
				return text + "<color=orange>" + m_shared.m_attackStatusEffect.m_name + "</color>\n" + m_shared.m_attackStatusEffect.GetTooltipString();
			}
			if ((bool)m_shared.m_consumeStatusEffect)
			{
				m_shared.m_consumeStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + m_shared.m_consumeStatusEffect.m_name + "</color>\n" + m_shared.m_consumeStatusEffect.GetTooltipString();
			}
			if ((bool)m_shared.m_equipStatusEffect)
			{
				m_shared.m_equipStatusEffect.SetLevel(quality, skillLevel);
				return "<color=orange>" + m_shared.m_equipStatusEffect.m_name + "</color>\n" + m_shared.m_equipStatusEffect.GetTooltipString();
			}
			return "";
		}

		private string GetChainTooltip(int quality, float skillLevel)
		{
			if (m_shared.m_attack.m_spawnOnHitChance > 0f && m_shared.m_attack.m_spawnOnHit != null)
			{
				return ((m_shared.m_attack.m_spawnOnHitChance < 1f) ? $"$item_chancetoapplyse <color=orange>{m_shared.m_attack.m_spawnOnHitChance * 100f}%</color>\n" : "") + "<color=orange>" + getName(primary: true) + "</color>";
			}
			if (m_shared.m_secondaryAttack.m_spawnOnHitChance > 0f && m_shared.m_secondaryAttack.m_spawnOnHit != null)
			{
				return ((m_shared.m_secondaryAttack.m_spawnOnHitChance < 1f) ? $"$item_chancetoapplyse <color=orange>{m_shared.m_secondaryAttack.m_spawnOnHitChance * 100f}%</color>\n" : "") + "<color=orange>" + getName(primary: false) + "</color>";
			}
			return "";
			string getName(bool primary)
			{
				GameObject gameObject = (primary ? m_shared.m_attack.m_spawnOnHit : m_shared.m_secondaryAttack.m_spawnOnHit);
				Aoe component = gameObject.GetComponent<Aoe>();
				if ((object)component != null)
				{
					return component.m_name;
				}
				ItemDrop component2 = gameObject.GetComponent<ItemDrop>();
				if ((object)component2 != null)
				{
					return component2.m_itemData.m_shared.m_name;
				}
				return gameObject.name;
			}
		}

		private string GetEquipStatusEffectTooltip(int quality, float skillLevel)
		{
			if ((bool)m_shared.m_equipStatusEffect)
			{
				StatusEffect equipStatusEffect = m_shared.m_equipStatusEffect;
				m_shared.m_equipStatusEffect.SetLevel(quality, skillLevel);
				if (equipStatusEffect != null)
				{
					return equipStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		private string GetSetStatusEffectTooltip(int quality, float skillLevel)
		{
			if ((bool)m_shared.m_setStatusEffect)
			{
				StatusEffect setStatusEffect = m_shared.m_setStatusEffect;
				m_shared.m_setStatusEffect.SetLevel(quality, skillLevel);
				if (setStatusEffect != null)
				{
					return setStatusEffect.GetTooltipString();
				}
			}
			return "";
		}

		private string GetProjectileTooltip(int itemQuality)
		{
			string text = "";
			if ((bool)m_shared.m_attack.m_attackProjectile)
			{
				IProjectile component = m_shared.m_attack.m_attackProjectile.GetComponent<IProjectile>();
				if (component != null)
				{
					text += component.GetTooltipString(itemQuality);
				}
			}
			if ((bool)m_shared.m_spawnOnHit)
			{
				IProjectile component2 = m_shared.m_spawnOnHit.GetComponent<IProjectile>();
				if (component2 != null)
				{
					text += component2.GetTooltipString(itemQuality);
				}
			}
			return text;
		}

		public override string ToString()
		{
			return string.Format("{0}: stack: {1}, quality: {2}, Shared: {3}", "ItemData", m_stack, m_quality, m_shared);
		}
	}

	private static List<ItemDrop> s_instances = new List<ItemDrop>();

	private int m_myIndex = -1;

	public bool m_autoPickup = true;

	public bool m_autoDestroy = true;

	public ItemData m_itemData = new ItemData();

	[HideInInspector]
	public Action<ItemDrop> m_onDrop;

	private int m_nameHash;

	private Floating m_floating;

	private Rigidbody m_body;

	private ZNetView m_nview;

	private Character m_pickupRequester;

	private float m_lastOwnerRequest;

	private int m_ownerRetryCounter;

	private float m_ownerRetryTimeout;

	private float m_spawnTime;

	private uint m_loadedRevision = uint.MaxValue;

	private const double c_AutoDestroyTimeout = 3600.0;

	private const double c_AutoPickupDelay = 0.5;

	private const float c_AutoDespawnBaseMinAltitude = -2f;

	private const int c_AutoStackThreshold = 200;

	private const float c_AutoStackRange = 4f;

	private bool m_haveAutoStacked;

	private static int s_itemMask = 0;

	private void Awake()
	{
		if (!string.IsNullOrEmpty(base.name))
		{
			m_nameHash = base.name.GetStableHashCode();
		}
		m_myIndex = s_instances.Count;
		s_instances.Add(this);
		string prefabName = GetPrefabName(base.gameObject.name);
		GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefabName);
		m_itemData.m_dropPrefab = itemPrefab;
		if (Application.isEditor)
		{
			m_itemData.m_shared = itemPrefab.GetComponent<ItemDrop>().m_itemData.m_shared;
		}
		m_floating = GetComponent<Floating>();
		m_body = GetComponent<Rigidbody>();
		if ((bool)m_body)
		{
			m_body.maxDepenetrationVelocity = 1f;
		}
		m_spawnTime = Time.time;
		m_nview = GetComponent<ZNetView>();
		if ((bool)m_nview && m_nview.IsValid())
		{
			if (m_nview.IsOwner() && new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L)).Ticks == 0L)
			{
				m_nview.GetZDO().Set(ZDOVars.s_spawnTime, ZNet.instance.GetTime().Ticks);
			}
			m_nview.Register("RequestOwn", RPC_RequestOwn);
			Load();
			InvokeRepeating("SlowUpdate", UnityEngine.Random.Range(1f, 2f), 10f);
		}
		SetQuality(m_itemData.m_quality);
	}

	private void OnDestroy()
	{
		s_instances[m_myIndex] = s_instances[s_instances.Count - 1];
		s_instances[m_myIndex].m_myIndex = m_myIndex;
		s_instances.RemoveAt(s_instances.Count - 1);
	}

	private void Start()
	{
		Save();
		base.gameObject.GetComponentInChildren<IEquipmentVisual>()?.Setup(m_itemData.m_variant);
	}

	public static void OnCreateNew(GameObject go)
	{
		ItemDrop component = go.GetComponent<ItemDrop>();
		if ((object)component != null)
		{
			OnCreateNew(component);
		}
	}

	public static void OnCreateNew(ItemDrop item)
	{
		item.m_itemData.m_worldLevel = (byte)Game.m_worldLevel;
	}

	private double GetTimeSinceSpawned()
	{
		DateTime dateTime = new DateTime(m_nview.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L));
		return (ZNet.instance.GetTime() - dateTime).TotalSeconds;
	}

	private void SlowUpdate()
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			TerrainCheck();
			if (m_autoDestroy)
			{
				TimedDestruction();
			}
			if (s_instances.Count > 200)
			{
				AutoStackItems();
			}
		}
	}

	private void TerrainCheck()
	{
		float groundHeight = ZoneSystem.instance.GetGroundHeight(base.transform.position);
		if (base.transform.position.y - groundHeight < -0.5f)
		{
			Vector3 position = base.transform.position;
			position.y = groundHeight + 0.5f;
			base.transform.position = position;
			Rigidbody component = GetComponent<Rigidbody>();
			if ((bool)component)
			{
				component.velocity = Vector3.zero;
			}
		}
	}

	private void TimedDestruction()
	{
		if (!(GetTimeSinceSpawned() < 3600.0) && !IsInsideBase() && !Player.IsPlayerInRange(base.transform.position, 25f) && !InTar())
		{
			m_nview.Destroy();
		}
	}

	private bool IsInsideBase()
	{
		if (base.transform.position.y > 28f && (bool)EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.PlayerBase))
		{
			return true;
		}
		return false;
	}

	private void AutoStackItems()
	{
		if (m_itemData.m_shared.m_maxStackSize <= 1 || m_itemData.m_stack >= m_itemData.m_shared.m_maxStackSize || m_haveAutoStacked)
		{
			return;
		}
		m_haveAutoStacked = true;
		if (s_itemMask == 0)
		{
			s_itemMask = LayerMask.GetMask("item");
		}
		bool flag = false;
		Collider[] array = Physics.OverlapSphere(base.transform.position, 4f, s_itemMask);
		foreach (Collider collider in array)
		{
			if (!collider.attachedRigidbody)
			{
				continue;
			}
			ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
			if (!(component == null) && !(component == this) && component.m_itemData.m_shared.m_autoStack && !(component.m_nview == null) && component.m_nview.IsValid() && component.m_nview.IsOwner() && !(component.m_itemData.m_shared.m_name != m_itemData.m_shared.m_name) && component.m_itemData.m_quality == m_itemData.m_quality)
			{
				int num = m_itemData.m_shared.m_maxStackSize - m_itemData.m_stack;
				if (num == 0)
				{
					break;
				}
				if (component.m_itemData.m_stack <= num)
				{
					m_itemData.m_stack += component.m_itemData.m_stack;
					flag = true;
					component.m_nview.Destroy();
				}
			}
		}
		if (flag)
		{
			Save();
		}
	}

	public string GetHoverText()
	{
		Load();
		string text = m_itemData.m_shared.m_name;
		if (m_itemData.m_quality > 1)
		{
			text = text + "[" + m_itemData.m_quality + "] ";
		}
		if (m_itemData.m_stack > 1)
		{
			text = text + " x" + m_itemData.m_stack;
		}
		return Localization.instance.Localize(text + "\n[<color=yellow><b>$KEY_Use</b></color>] $inventory_pickup");
	}

	public string GetHoverName()
	{
		return m_itemData.m_shared.m_name;
	}

	private string GetPrefabName(string name)
	{
		char[] anyOf = new char[2] { '(', ' ' };
		int num = name.IndexOfAny(anyOf);
		if (num >= 0)
		{
			return name.Substring(0, num);
		}
		return name;
	}

	public bool Interact(Humanoid character, bool repeat, bool alt)
	{
		if (repeat)
		{
			return false;
		}
		if (InTar())
		{
			character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar");
			return true;
		}
		Pickup(character);
		return true;
	}

	public bool InTar()
	{
		if (m_body == null)
		{
			return false;
		}
		if (m_floating != null)
		{
			return m_floating.IsInTar();
		}
		Vector3 worldCenterOfMass = m_body.worldCenterOfMass;
		float liquidLevel = Floating.GetLiquidLevel(worldCenterOfMass, 1f, LiquidType.Tar);
		return worldCenterOfMass.y < liquidLevel;
	}

	public bool UseItem(Humanoid user, ItemData item)
	{
		return false;
	}

	public void SetStack(int stack)
	{
		if (m_nview.IsValid() && m_nview.IsOwner())
		{
			m_itemData.m_stack = stack;
			if (m_itemData.m_stack > m_itemData.m_shared.m_maxStackSize)
			{
				m_itemData.m_stack = m_itemData.m_shared.m_maxStackSize;
			}
			Save();
		}
	}

	public void Pickup(Humanoid character)
	{
		if (m_nview.IsValid())
		{
			if (CanPickup())
			{
				Load();
				character.Pickup(base.gameObject);
				Save();
			}
			else
			{
				m_pickupRequester = character;
				CancelInvoke("PickupUpdate");
				float num = 0.05f;
				InvokeRepeating("PickupUpdate", num, num);
				RequestOwn();
			}
		}
	}

	public void RequestOwn()
	{
		if (!(Time.time - m_lastOwnerRequest < m_ownerRetryTimeout) && !m_nview.IsOwner())
		{
			m_lastOwnerRequest = Time.time;
			m_ownerRetryTimeout = Mathf.Min(0.2f * Mathf.Pow(2f, m_ownerRetryCounter), 30f);
			m_ownerRetryCounter++;
			m_nview.InvokeRPC("RequestOwn");
		}
	}

	public bool RemoveOne()
	{
		if (!CanPickup())
		{
			RequestOwn();
			return false;
		}
		if (m_itemData.m_stack <= 1)
		{
			m_nview.Destroy();
			return true;
		}
		m_itemData.m_stack--;
		Save();
		return true;
	}

	public void OnPlayerDrop()
	{
		m_autoPickup = false;
	}

	public bool CanPickup(bool autoPickupDelay = true)
	{
		if (m_nview == null || !m_nview.IsValid())
		{
			return true;
		}
		if (autoPickupDelay && (double)(Time.time - m_spawnTime) < 0.5)
		{
			return false;
		}
		if (m_nview.IsOwner())
		{
			m_ownerRetryCounter = 0;
			m_ownerRetryTimeout = 0f;
		}
		return m_nview.IsOwner();
	}

	private void RPC_RequestOwn(long uid)
	{
		ZLog.Log("Player " + uid + " wants to pickup " + base.gameObject.name + "   im: " + ZDOMan.GetSessionID());
		if (m_nview.IsOwner())
		{
			m_nview.GetZDO().SetOwner(uid);
		}
		else if (m_nview.GetZDO().GetOwner() == uid)
		{
			ZLog.Log("  but they are already the owner");
		}
		else
		{
			ZLog.Log("  but neither I nor the requesting player are the owners");
		}
	}

	private void PickupUpdate()
	{
		if (m_nview.IsValid())
		{
			if (CanPickup())
			{
				ZLog.Log("Im finally the owner");
				CancelInvoke("PickupUpdate");
				Load();
				(m_pickupRequester as Player).Pickup(base.gameObject);
				Save();
			}
			else
			{
				ZLog.Log("Im still nto the owner");
			}
		}
	}

	private void Save()
	{
		if (!(m_nview == null) && m_nview.IsValid() && m_nview.IsOwner())
		{
			SaveToZDO(m_itemData, m_nview.GetZDO());
		}
	}

	public void Load()
	{
		if (!(m_nview == null) && m_nview.IsValid())
		{
			ZDO zDO = m_nview.GetZDO();
			if (zDO.DataRevision != m_loadedRevision)
			{
				m_loadedRevision = zDO.DataRevision;
				LoadFromZDO(m_itemData, zDO);
				SetQuality(m_itemData.m_quality);
			}
		}
	}

	public void LoadFromExternalZDO(ZDO zdo)
	{
		LoadFromZDO(m_itemData, zdo);
		SaveToZDO(m_itemData, m_nview.GetZDO());
		SetQuality(m_itemData.m_quality);
	}

	public static void SaveToZDO(ItemData itemData, ZDO zdo)
	{
		zdo.Set(ZDOVars.s_durability, itemData.m_durability);
		zdo.Set(ZDOVars.s_stack, itemData.m_stack);
		zdo.Set(ZDOVars.s_quality, itemData.m_quality);
		zdo.Set(ZDOVars.s_variant, itemData.m_variant);
		zdo.Set(ZDOVars.s_crafterID, itemData.m_crafterID);
		zdo.Set(ZDOVars.s_crafterName, itemData.m_crafterName);
		zdo.Set(ZDOVars.s_dataCount, itemData.m_customData.Count);
		int num = 0;
		foreach (KeyValuePair<string, string> customDatum in itemData.m_customData)
		{
			zdo.Set($"data_{num}", customDatum.Key);
			zdo.Set($"data__{num++}", customDatum.Value);
		}
		zdo.Set(ZDOVars.s_worldLevel, itemData.m_worldLevel);
		zdo.Set(ZDOVars.s_pickedUp, itemData.m_pickedUp);
	}

	private static void LoadFromZDO(ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat(ZDOVars.s_durability, itemData.m_durability);
		itemData.m_stack = zdo.GetInt(ZDOVars.s_stack, itemData.m_stack);
		itemData.m_quality = zdo.GetInt(ZDOVars.s_quality, itemData.m_quality);
		itemData.m_variant = zdo.GetInt(ZDOVars.s_variant, itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong(ZDOVars.s_crafterID, itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString(ZDOVars.s_crafterName, itemData.m_crafterName);
		int @int = zdo.GetInt(ZDOVars.s_dataCount);
		itemData.m_customData.Clear();
		for (int i = 0; i < @int; i++)
		{
			itemData.m_customData[zdo.GetString($"data_{i}")] = zdo.GetString($"data__{i}");
		}
		itemData.m_worldLevel = (byte)zdo.GetInt(ZDOVars.s_worldLevel, itemData.m_worldLevel);
		itemData.m_pickedUp = zdo.GetBool(ZDOVars.s_pickedUp, itemData.m_pickedUp);
	}

	public static void SaveToZDO(int index, ItemData itemData, ZDO zdo)
	{
		zdo.Set(index + "_durability", itemData.m_durability);
		zdo.Set(index + "_stack", itemData.m_stack);
		zdo.Set(index + "_quality", itemData.m_quality);
		zdo.Set(index + "_variant", itemData.m_variant);
		zdo.Set(index + "_crafterID", itemData.m_crafterID);
		zdo.Set(index + "_crafterName", itemData.m_crafterName);
		zdo.Set(index + "_dataCount", itemData.m_customData.Count);
		int num = 0;
		foreach (KeyValuePair<string, string> customDatum in itemData.m_customData)
		{
			zdo.Set($"{index}_data_{num}", customDatum.Key);
			zdo.Set($"{index}_data__{num++}", customDatum.Value);
		}
		zdo.Set(index + "_worldLevel", itemData.m_worldLevel);
		zdo.Set(index + "_pickedUp", itemData.m_pickedUp);
	}

	public static void LoadFromZDO(int index, ItemData itemData, ZDO zdo)
	{
		itemData.m_durability = zdo.GetFloat(index + "_durability", itemData.m_durability);
		itemData.m_stack = zdo.GetInt(index + "_stack", itemData.m_stack);
		itemData.m_quality = zdo.GetInt(index + "_quality", itemData.m_quality);
		itemData.m_variant = zdo.GetInt(index + "_variant", itemData.m_variant);
		itemData.m_crafterID = zdo.GetLong(index + "_crafterID", itemData.m_crafterID);
		itemData.m_crafterName = zdo.GetString(index + "_crafterName", itemData.m_crafterName);
		int @int = zdo.GetInt(index + "_dataCount");
		for (int i = 0; i < @int; i++)
		{
			itemData.m_customData[zdo.GetString($"{index}_data_{i}")] = zdo.GetString($"{index}_data__{i}");
		}
		itemData.m_worldLevel = (byte)zdo.GetInt(index + "_worldLevel", itemData.m_worldLevel);
		itemData.m_pickedUp = zdo.GetBool(index + "_pickedUp", itemData.m_pickedUp);
	}

	public static ItemDrop DropItem(ItemData item, int amount, Vector3 position, Quaternion rotation)
	{
		ItemDrop component = UnityEngine.Object.Instantiate(item.m_dropPrefab, position, rotation).GetComponent<ItemDrop>();
		component.m_itemData = item.Clone();
		if (component.m_itemData.m_quality > 1)
		{
			component.SetQuality(component.m_itemData.m_quality);
		}
		if (amount > 0)
		{
			component.m_itemData.m_stack = amount;
		}
		if (component.m_onDrop != null)
		{
			component.m_onDrop(component);
		}
		component.Save();
		return component;
	}

	public void SetQuality(int quality)
	{
		m_itemData.m_quality = quality;
		base.transform.localScale = m_itemData.GetScale();
	}

	private void OnDrawGizmos()
	{
	}

	public int NameHash()
	{
		return m_nameHash;
	}
}
