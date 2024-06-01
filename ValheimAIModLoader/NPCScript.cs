using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimAIModLoader
{
    public enum NPCMode
    {
        Follow,
        Attack,
        Harvest,
        Idle
    }

    public class NPCScript : MonoBehaviour
    {
        protected Humanoid m_humanoid;

        [Header("NPCScript")]
        public float m_maxPlaceDistance = 5f;

        public float m_maxInteractDistance = 5f;

        public float m_scrollSens = 4f;

        public float m_staminaRegen = 5f;

        public float m_staminaRegenTimeMultiplier = 1f;

        public float m_staminaRegenDelay = 1f;

        public float m_runStaminaDrain = 10f;

        public float m_sneakStaminaDrain = 5f;

        public float m_swimStaminaDrainMinSkill = 5f;

        public float m_swimStaminaDrainMaxSkill = 2f;

        public float m_dodgeStaminaUsage = 10f;

        public float m_weightStaminaFactor = 0.1f;

        public float m_eiterRegen = 5f;

        public float m_eitrRegenDelay = 1f;

        public float m_autoPickupRange = 2f;

        public float m_maxCarryWeight = 300f;

        public float m_encumberedStaminaDrain = 10f;

        public float m_hardDeathCooldown = 10f;

        public float m_baseCameraShake = 4f;

        public float m_placeDelay = 0.4f;

        public float m_removeDelay = 0.25f;

        public float m_baseHP = 25f;

        public float m_baseStamina = 75f;

        private float m_timeSinceDeath = 999999f;

        private float m_nearFireTimer;

        private bool m_underRoof = true;

        private bool m_safeInHome;

        private float m_timeSinceSensed;

        private float m_coverPercentage;

        private int m_baseValue;

        private int m_baseValueOld = -1;

        private readonly List<Player.Food> m_foods = new List<Player.Food>();

        private float m_foodUpdateTimer;

        private float m_foodRegenTimer;

        public float m_staminaLastBreakTime;

        public float m_stamina = 100f;

        public float m_maxStamina = 100f;

        private float m_staminaRegenTimer;

        private float m_eitr;

        private float m_maxEitr;

        private float m_eitrRegenTimer;

        private float deltaAdd = 0f;


        void Start()
        {
            m_humanoid = GetComponent<Humanoid>();

            Debug.Log("NPCScript started for " + m_humanoid.GetHoverName());
        }

        // Update is called once per frame
        void Update()
        {
            /*m_testkey++;
            Debug.Log("DonScript Update " + m_testkey);*/
            UpdateStats(Time.deltaTime);

            if (deltaAdd > 1f)
            {
                deltaAdd = 0f;
                //Debug.Log(m_humanoid.GetHoverName() + " stamina: " + m_stamina);
            }
            else
            {
                deltaAdd += Time.deltaTime;
            }

            
        }

        private void UpdateStats(float dt)
        {
            if (m_humanoid == null)
            {
                return;
            }

            if (m_humanoid.InIntro() || m_humanoid.IsTeleporting())
            {
                return;
            }
            m_timeSinceDeath += dt;
            UpdateModifiers();
            UpdateFood(dt, forceUpdate: false);
            bool flag = m_humanoid.IsEncumbered();
            float maxStamina = m_maxStamina;
            float num = 1f;
            if (m_humanoid.IsBlocking())
            {
                num *= 0.8f;
            }
            if ((m_humanoid.IsSwimming() && !m_humanoid.IsOnGround()) || m_humanoid.InAttack() || m_humanoid.InDodge() || m_humanoid.m_wallRunning || flag)
            {
                num = 0f;
            }
            float num2 = (m_staminaRegen + (1f - m_stamina / maxStamina) * m_staminaRegen * m_staminaRegenTimeMultiplier) * num;
            float staminaMultiplier = 1f;
            m_humanoid.m_seman.ModifyStaminaRegen(ref staminaMultiplier);
            num2 *= staminaMultiplier;
            m_staminaRegenTimer -= dt;
            //Debug.Log(m_staminaRegenTimer);
            if (m_stamina < maxStamina && m_staminaRegenTimer <= 0f)
            {
                m_stamina = Mathf.Min(maxStamina, m_stamina + num2 * dt * Game.m_staminaRegenRate);
            }
            //m_humanoid.m_nview.GetZDO().Set(ZDOVars.s_stamina, m_stamina);
            float maxEitr = m_humanoid.GetMaxEitr();
            float num3 = 1f;
            if (m_humanoid.IsBlocking())
            {
                num3 *= 0.8f;
            }
            if (m_humanoid.InAttack() || m_humanoid.InDodge())
            {
                num3 = 0f;
            }
            /*float num4 = (m_eiterRegen + (1f - m_eitr / maxEitr) * m_eiterRegen) * num3;
            float eitrMultiplier = 1f;
            m_humanoid.m_seman.ModifyEitrRegen(ref eitrMultiplier);
            eitrMultiplier += GetEquipmentEitrRegenModifier();
            num4 *= eitrMultiplier;
            m_eitrRegenTimer -= dt;
            if (m_eitr < maxEitr && m_eitrRegenTimer <= 0f)
            {
                m_eitr = Mathf.Min(maxEitr, m_eitr + num4 * dt);
            }*/
            //m_humanoid.m_nview.GetZDO().Set(ZDOVars.s_eitr, m_eitr);
            if (flag)
            {
                if (m_humanoid.m_moveDir.magnitude > 0.1f)
                {
                    UseStamina(m_encumberedStaminaDrain * dt);
                }
                m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectEncumbered);
                //ShowTutorial("encumbered");
            }
            else if (m_humanoid.CheckRun(m_humanoid.m_moveDir, dt))
            {
                UseStamina(m_runStaminaDrain * dt);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectEncumbered);
            }
            if (!HardDeath())
            {
                m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectSoftDeath);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectSoftDeath);
            }
            UpdateEnvStatusEffects(dt);
        }

        private void UpdateEnvStatusEffects(float dt)
        {
            m_nearFireTimer += dt;
            HitData.DamageModifiers damageModifiers = m_humanoid.GetDamageModifiers();
            bool flag = m_nearFireTimer < 0.25f;
            bool flag2 = m_humanoid.m_seman.HaveStatusEffect(SEMan.s_statusEffectBurning);
            bool flag3 = InShelter();
            HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
            bool flag4 = EnvMan.IsFreezing();
            bool num = EnvMan.IsCold();
            bool flag5 = EnvMan.IsWet();
            bool flag6 = IsSensed();
            bool flag7 = m_humanoid.m_seman.HaveStatusEffect(SEMan.s_statusEffectWet);
            bool flag8 = m_humanoid.IsSitting();
            bool flag9 = EffectArea.IsPointInsideArea(base.transform.position, EffectArea.Type.WarmCozyArea, 1f);
            bool flag10 = ShieldGenerator.IsInsideShield(base.transform.position);
            bool flag11 = flag4 && !flag && !flag3;
            bool flag12 = (num && !flag) || (flag4 && flag && !flag3) || (flag4 && !flag && flag3);
            if (modifier == HitData.DamageModifier.Resistant || modifier == HitData.DamageModifier.VeryResistant || flag9)
            {
                flag11 = false;
                flag12 = false;
            }
            if (flag5 && !m_underRoof && !flag10)
            {
                m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectWet, resetTime: true);
            }
            if (flag3)
            {
                m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectShelter);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectShelter);
            }
            if (flag)
            {
                m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectCampFire);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectCampFire);
            }
            bool flag13 = !flag6 && (flag8 || flag3) && !flag12 && !flag11 && (!flag7 || flag9) && !flag2 && flag;
            if (flag13)
            {
                m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectResting);
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectResting);
            }
            m_safeInHome = flag13 && flag3 && (float)GetBaseValue() >= 1f;
            if (flag11)
            {
                if (!m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold, quiet: true))
                {
                    m_humanoid.m_seman.AddStatusEffect(SEMan.s_statusEffectFreezing);
                }
            }
            else
            {
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold);
                m_humanoid.m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing);
            }
        }

        public bool InShelter()
        {
            if (m_coverPercentage >= 0.8f)
            {
                return m_underRoof;
            }
            return false;
        }

        public float GetStamina()
        {
            return m_stamina;
        }

        public void UseStamina(float v, bool isHomeUsage = false)
        {
            if (v == 0f)
            {
                return;
            }
            v *= Game.m_staminaRate;
            if (isHomeUsage)
            {
                v *= 1f + m_humanoid.GetEquipmentHomeItemModifier();
                m_humanoid.m_seman.ModifyHomeItemStaminaUsage(v, ref v);
            }
            if (m_humanoid.m_nview.IsValid())
            {
                if (m_humanoid.m_nview.IsOwner())
                {
                    RPC_UseStamina(0L, v);
                    return;
                }
                //m_humanoid.m_nview.InvokeRPC("UseStamina", v);
                RPC_UseStamina(0L, v);
            }
        }

        private void RPC_UseStamina(long sender, float v)
        {
            if (v != 0f)
            {
                m_stamina -= v;
                if (m_stamina < 0f)
                {
                    m_stamina = 0f;
                }
                m_staminaRegenTimer = m_staminaRegenDelay;
            }
        }

        public bool HaveStamina(float amount = 0f)
        {
            if (m_humanoid.m_nview.IsValid() && !m_humanoid.m_nview.IsOwner())
            {
                return m_humanoid.m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, m_maxStamina) > amount;
            }
            return m_stamina > amount;
        }

        public int GetBaseValue()
        {
            if (!m_humanoid.m_nview.IsValid())
            {
                return 0;
            }
            if (m_humanoid.m_nview.IsOwner())
            {
                return m_baseValue;
            }
            return m_humanoid.m_nview.GetZDO().GetInt(ZDOVars.s_baseValue);
        }

        public bool IsSensed()
        {
            return m_timeSinceSensed < 1f;
        }

        private bool HardDeath()
        {
            return m_timeSinceDeath > m_hardDeathCooldown;
        }

        public float GetEquipmentEitrRegenModifier()
        {
            float num = 0f;
            if (m_humanoid.m_chestItem != null)
            {
                num += m_humanoid.m_chestItem.m_shared.m_eitrRegenModifier;
            }
            if (m_humanoid.m_legItem != null)
            {
                num += m_humanoid.m_legItem.m_shared.m_eitrRegenModifier;
            }
            if (m_humanoid.m_helmetItem != null)
            {
                num += m_humanoid.m_helmetItem.m_shared.m_eitrRegenModifier;
            }
            if (m_humanoid.m_shoulderItem != null)
            {
                num += m_humanoid.m_shoulderItem.m_shared.m_eitrRegenModifier;
            }
            if (m_humanoid.m_leftItem != null)
            {
                num += m_humanoid.m_leftItem.m_shared.m_eitrRegenModifier;
            }
            if (m_humanoid.m_rightItem != null)
            {
                num += m_humanoid.m_rightItem.m_shared.m_eitrRegenModifier;
            }
            if (m_humanoid.m_utilityItem != null)
            {
                num += m_humanoid.m_utilityItem.m_shared.m_eitrRegenModifier;
            }
            return num;
        }

        private void UpdateFood(float dt, bool forceUpdate)
        {
            //throw new NotImplementedException();
        }

        private void UpdateModifiers()
        {
            //throw new NotImplementedException();
        }
    }
}
