using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

namespace ValheimAIModLoader
{
    public class HumanoidNPC : Humanoid
    {
        [Header("HumanoidNPC")]
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

        public readonly List<Player.Food> m_foods = new List<Player.Food>();
        private float m_foodUpdateTimer;
        private float m_foodRegenTimer;

        public float m_stamina = 100f;
        public float m_maxStamina = 100f;
        public float m_staminaRegenTimer;

        public float m_staminaLastBreakTime = 0f;
        public float StaminaExhaustedMinimumBreakTime = 2f;
        public float MinimumStaminaToRun = 5f;

        private float m_eitr;
        private float m_maxEitr;
        private float m_eitrRegenTimer;


        private static bool m_enableAutoPickup = true;
        private int m_autoPickupMask;

        public bool m_crouchToggled;
        private static readonly int s_crouching = ZSyncAnimation.GetHash("crouching");
        private static readonly int s_animatorTagCrouch = ZSyncAnimation.GetHash("crouch");

        public Vector3 LastPosition;
        public float LastMovedAtTime;

        public Vector3 patrol_position;
        public Minimap.PinData npcPinData;
        public Container inventoryContainer;

        public override void CustomFixedUpdate(float fixedDeltaTime)
        {
            base.CustomFixedUpdate(fixedDeltaTime);


            UpdateStats(fixedDeltaTime);

            UpdateCrouch(fixedDeltaTime);
            //AutoPickup(fixedDeltaTime);

            UpdateLastPosition();
            UpdatePin();

            //Debug.Log(IsCrouching());
        }

        private void UpdateStats(float dt)
        {
            if (this == null)
            {
                return;
            }

            if (this.InIntro() || this.IsTeleporting())
            {
                return;
            }
            m_timeSinceDeath += dt;
            bool flag = this.IsEncumbered();
            float maxStamina = m_maxStamina;
            float num = 1f;
            if (this.IsBlocking())
            {
                num *= 0.8f;
            }
            if ((this.IsSwimming() && !this.IsOnGround()) || this.InAttack() || this.InDodge() || this.m_wallRunning || flag)
            {
                num = 0f;
            }
            float num2 = (m_staminaRegen + (1f - m_stamina / maxStamina) * m_staminaRegen * m_staminaRegenTimeMultiplier) * num;
            float staminaMultiplier = 1f;
            this.m_seman.ModifyStaminaRegen(ref staminaMultiplier);
            num2 *= staminaMultiplier;
            m_staminaRegenTimer -= dt;
            //Debug.Log(m_staminaRegenTimer);
            if (m_stamina < maxStamina && m_staminaRegenTimer <= 0f)
            {
                m_stamina = Mathf.Min(maxStamina, m_stamina + num2 * dt * Game.m_staminaRegenRate);
            }
            //m_humanoid.m_nview.GetZDO().Set(ZDOVars.s_stamina, m_stamina);
            float maxEitr = this.GetMaxEitr();
            float num3 = 1f;
            if (this.IsBlocking())
            {
                num3 *= 0.8f;
            }
            if (this.InAttack() || this.InDodge())
            {
                num3 = 0f;
            }

            if (flag)
            {
                if (this.m_moveDir.magnitude > 0.1f)
                {
                    //UseStamina(m_encumberedStaminaDrain * dt);
                }
                this.m_seman.AddStatusEffect(SEMan.s_statusEffectEncumbered);
            }
            /*else if (m_humanoid.CheckRun(m_humanoid.m_moveDir, dt))
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
            }*/

            UpdateEnvStatusEffects(dt);
        }

        public void O_DoInteractAnimation(Vector3 target)
        {
            Vector3 forward = target - base.transform.position;
            forward.y = 0f;
            forward.Normalize();
            base.transform.rotation = Quaternion.LookRotation(forward);
            Physics.SyncTransforms();
            m_zanim.SetTrigger("interact");
        }

        public override void UseStamina(float v, bool isHomeUsage = false)
        {
            if (v == 0f)
            {
                return;
            }
            v *= Game.m_staminaRate;
            if (isHomeUsage)
            {
                v *= 1f + GetEquipmentHomeItemModifier();
                m_seman.ModifyHomeItemStaminaUsage(v, ref v);
            }
            if (m_nview.IsValid())
            {
                if (m_nview.IsOwner())
                {
                    RPC_UseStamina(0L, v);
                    return;
                }
                m_nview.InvokeRPC("UseStamina", v);
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

        public override bool HaveStamina(float amount = 0f)
        {
            if (m_nview.IsValid() && !m_nview.IsOwner())
            {
                return m_nview.GetZDO().GetFloat(ZDOVars.s_stamina, m_maxStamina) > amount;
            }
            return m_stamina > amount;
        }

        public bool InShelter()
        {
            if (m_coverPercentage >= 0.8f)
            {
                return m_underRoof;
            }
            return false;
        }

        public bool IsSensed()
        {
            return m_timeSinceSensed < 1f;
        }

        private void UpdateEnvStatusEffects(float dt)
        {
            m_nearFireTimer += dt;
            HitData.DamageModifiers damageModifiers = GetDamageModifiers();
            bool flag = m_nearFireTimer < 0.25f;
            bool flag2 = m_seman.HaveStatusEffect(SEMan.s_statusEffectBurning);
            bool flag3 = InShelter();
            HitData.DamageModifier modifier = damageModifiers.GetModifier(HitData.DamageType.Frost);
            bool flag4 = EnvMan.IsFreezing();
            bool num = EnvMan.IsCold();
            bool flag5 = EnvMan.IsWet();
            bool flag6 = IsSensed();
            bool flag7 = m_seman.HaveStatusEffect(SEMan.s_statusEffectWet);
            bool flag8 = IsSitting();
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
                m_seman.AddStatusEffect(SEMan.s_statusEffectWet, resetTime: true);
            }
            if (flag3)
            {
                m_seman.AddStatusEffect(SEMan.s_statusEffectShelter);
            }
            else
            {
                m_seman.RemoveStatusEffect(SEMan.s_statusEffectShelter);
            }
            if (flag)
            {
                m_seman.AddStatusEffect(SEMan.s_statusEffectCampFire);
            }
            else
            {
                m_seman.RemoveStatusEffect(SEMan.s_statusEffectCampFire);
            }
            bool flag13 = !flag6 && (flag8 || flag3) && !flag12 && !flag11 && (!flag7 || flag9) && !flag2 && flag;
            if (flag13)
            {
                m_seman.AddStatusEffect(SEMan.s_statusEffectResting);
            }
            else
            {
                m_seman.RemoveStatusEffect(SEMan.s_statusEffectResting);
            }
            m_safeInHome = flag13 && flag3;
            if (flag11)
            {
                if (!m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold, quiet: true))
                {
                    m_seman.AddStatusEffect(SEMan.s_statusEffectFreezing);
                }
            }
            else if (flag12)
            {
                if (!m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing, quiet: true) && (bool)m_seman.AddStatusEffect(SEMan.s_statusEffectCold))
                {
                    //ShowTutorial("cold");
                }
            }
            else
            {
                m_seman.RemoveStatusEffect(SEMan.s_statusEffectCold);
                m_seman.RemoveStatusEffect(SEMan.s_statusEffectFreezing);
            }
        }

        public void UpdatePin()
        {
            if (npcPinData != null)
                Minimap.instance.RemovePin(npcPinData);

            npcPinData = Minimap.instance.AddPin(this.transform.position, Minimap.PinType.Player, m_name, false, false, 9990, "NPC");
        }

        public void UpdateLastPosition()
        {
            if (this.transform.position.DistanceTo(LastPosition) > .15f)
            {
                LastPosition = this.transform.position;
                LastMovedAtTime = Time.time;
            }
        }

        /*public override bool IsPlayer()
        {
            return true;
        }*/

        public override void Awake()
        {
            base.Awake();

            m_autoPickupMask = LayerMask.GetMask("item");
            m_nview = GetComponent<ZNetView>();

            if (m_nview == null)
            {
                Debug.LogError("PersistentNPC: Missing ZNetView component");
                return;
            }

            Debug.Log($"m_autoPickupMask: {m_autoPickupMask}");

            // Set the persistent flag
            m_nview.m_persistent = true;

            /*m_inventory.m_width = 10;
            m_inventory.m_height = 6;*/
        }

        public bool HasEnoughResource(string resourceName, int requiredAmount)
        {
            // Get the player's inventory
            Inventory playerInventory = GetInventory();

            if (playerInventory == null)
            {
                Debug.LogError("Player inventory not found!");
                return false;
            }

            // Find all items in the inventory that match the resource name
            List<ItemDrop.ItemData> items = playerInventory.GetAllItems()
                .Where(item => item.m_dropPrefab.name.ToLower() == resourceName.ToLower())
                .ToList();

            // Sum up the total amount of the resource
            int totalAmount = items.Sum(item => item.m_stack);

            // Check if the total amount is greater than or equal to the required amount
            return totalAmount >= requiredAmount;
        }

        public override bool IsCrouching()
        {
            //return GetCurrentAnimHash() == s_animatorTagCrouch;
            /*if (m_crouchToggled)
                Debug.Log("HumanoidNPC IsCrouching " + m_crouchToggled);*/
            return m_crouchToggled;
        }

        private void UpdateCrouch(float dt)
        {
            if (m_crouchToggled)
            {
                if (!HaveStamina() || IsSwimming() || InBed() || InPlaceMode() || m_run || IsBlocking() || IsFlying())
                {
                    SetCrouch(crouch: false);
                }
                bool flag = InAttack() || IsDrawingBow();
                m_zanim.SetBool(s_crouching, m_crouchToggled && !flag);
            }
            else
            {
                m_zanim.SetBool(s_crouching, value: false);
            }
        }

        public override void SetCrouch(bool crouch)
        {
            m_crouchToggled = crouch;
        }

        public override bool IsEncumbered()
        {
            return false;
            //return m_inventory.GetTotalWeight() > GetMaxCarryWeight();
        }

        public float GetMaxCarryWeight()
        {
            float limit = m_maxCarryWeight;
            m_seman.ModifyMaxCarryWeight(limit, ref limit);
            return limit;
        }

        public override bool CheckRun(Vector3 moveDir, float dt)
        {
            if (!base.CheckRun(moveDir, dt))
            {
                return false;
            }
            bool flag = HaveStamina();

            float skillFactor = 1;
            if (Player.m_localPlayer)
                skillFactor = Player.m_localPlayer.m_skills.GetSkillFactor(Skills.SkillType.Run);
            float num = Mathf.Lerp(1f, 0.5f, skillFactor);
            float num2 = m_runStaminaDrain * num;
            if (Player.m_localPlayer)
            {
                num2 -= num2 * Player.m_localPlayer.GetEquipmentMovementModifier();
                num2 += num2 * Player.m_localPlayer.GetEquipmentRunStaminaModifier();
            }
                
            //m_seman.ModifyRunStaminaDrain(num2, ref num2);
            if (m_stamina > MinimumStaminaToRun)
            {
                //UseStamina();
                m_stamina -= dt * num2 * Game.m_moveStaminaRate;
                return true;
            }

            return false;
        }

        public override void SetupVisEquipment(VisEquipment visEq, bool isRagdoll)
        {
            if (!isRagdoll)
            {
                visEq.SetLeftItem((m_leftItem != null) ? m_leftItem.m_dropPrefab.name : "", (m_leftItem != null) ? m_leftItem.m_variant : 0);
                visEq.SetRightItem((m_rightItem != null) ? m_rightItem.m_dropPrefab.name : "");
                
                visEq.SetLeftBackItem((m_hiddenLeftItem != null) ? m_hiddenLeftItem.m_dropPrefab.name : "", (m_hiddenLeftItem != null) ? m_hiddenLeftItem.m_variant : 0);
                visEq.SetRightBackItem((m_hiddenRightItem != null) ? m_hiddenRightItem.m_dropPrefab.name : "");
            }
            visEq.SetChestItem((m_chestItem != null) ? m_chestItem.m_dropPrefab.name : "");
            visEq.SetLegItem((m_legItem != null) ? m_legItem.m_dropPrefab.name : "");
            visEq.SetHelmetItem((m_helmetItem != null) ? m_helmetItem.m_dropPrefab.name : "");
            visEq.SetShoulderItem((m_shoulderItem != null) ? m_shoulderItem.m_dropPrefab.name : "", (m_shoulderItem != null) ? m_shoulderItem.m_variant : 0);
            visEq.SetUtilityItem((m_utilityItem != null) ? m_utilityItem.m_dropPrefab.name : "");
            visEq.SetBeardItem(m_beardItem);
            visEq.SetHairItem(m_hairItem);
        }

        public override void ApplyArmorDamageMods(ref HitData.DamageModifiers mods)
        {
            if (m_chestItem != null)
            {
                mods.Apply(m_chestItem.m_shared.m_damageModifiers);
            }
            if (m_legItem != null)
            {
                mods.Apply(m_legItem.m_shared.m_damageModifiers);
            }
            if (m_helmetItem != null)
            {
                mods.Apply(m_helmetItem.m_shared.m_damageModifiers);
            }
            if (m_shoulderItem != null)
            {
                mods.Apply(m_shoulderItem.m_shared.m_damageModifiers);
            }
        }

        public override float GetBodyArmor()
        {
            float num = 0f;
            if (m_chestItem != null)
            {
                num += m_chestItem.GetArmor();
            }
            if (m_legItem != null)
            {
                num += m_legItem.GetArmor();
            }
            if (m_helmetItem != null)
            {
                num += m_helmetItem.GetArmor();
            }
            if (m_shoulderItem != null)
            {
                num += m_shoulderItem.GetArmor();
            }
            return num;
        }

        public override void DamageArmorDurability(HitData hit)
        {
            List<ItemDrop.ItemData> list = new List<ItemDrop.ItemData>();
            if (m_chestItem != null)
            {
                list.Add(m_chestItem);
            }
            if (m_legItem != null)
            {
                list.Add(m_legItem);
            }
            if (m_helmetItem != null)
            {
                list.Add(m_helmetItem);
            }
            if (m_shoulderItem != null)
            {
                list.Add(m_shoulderItem);
            }
            if (list.Count != 0)
            {
                float num = hit.GetTotalPhysicalDamage() + hit.GetTotalElementalDamage();
                if (!(num <= 0f))
                {
                    int index = UnityEngine.Random.Range(0, list.Count);
                    ItemDrop.ItemData itemData = list[index];
                    itemData.m_durability = Mathf.Max(0f, itemData.m_durability - num);
                }
            }
        }
    }
}
