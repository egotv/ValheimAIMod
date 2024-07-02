using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
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
        public float LastPositionDelta;

        //public ValheimAIModLoader.NPCMode eNPCMode;
        public NPCCommand.CommandType CurrentCommand;

        public Vector3 patrol_position;

        public Minimap.PinData npcPinData;


        /*protected override void Start()
        {
            Debug.Log("HumanoidNPC Start");
        }*/

        // Update is called once per frame
        /*void Update()
        {
            //Debug.Log("HumanoidNPC");
            UpdateCrouch(Time.deltaTime);
        }*/

        public override void CustomFixedUpdate(float fixedDeltaTime)
        {
            base.CustomFixedUpdate(fixedDeltaTime);


            UpdateStats(fixedDeltaTime);

            UpdateCrouch(fixedDeltaTime);
            AutoPickup(fixedDeltaTime);

            UpdateLastPosition(fixedDeltaTime);
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
            //UpdateModifiers();
            //UpdateFood(dt, forceUpdate: false);
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
                if (this.m_moveDir.magnitude > 0.1f)
                {
                    UseStamina(m_encumberedStaminaDrain * dt);
                }
                this.m_seman.AddStatusEffect(SEMan.s_statusEffectEncumbered);
                //ShowTutorial("encumbered");
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
            }
            UpdateEnvStatusEffects(dt);*/
        }

        public void UpdatePin()
        {
            if (npcPinData != null)
            {
                Minimap.instance.RemovePin(npcPinData);
            }

            npcPinData = Minimap.instance.AddPin(this.transform.position, Minimap.PinType.Player, "NPC", true, false);
        }

        public void UpdateLastPosition(float fixedDeltaTime)
        {
            if (this.transform.position.DistanceTo(LastPosition) < .15f)
            {
                LastPositionDelta += fixedDeltaTime;
            }
            else
            {
                LastPosition = this.transform.position;
                LastPositionDelta = 0f;
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
        }

        public void SetCurrentCommand(NPCCommand.CommandType NewCommand)
        {
            CurrentCommand = NewCommand;
            if (NewCommand == NPCCommand.CommandType.PatrolArea)
            {
                patrol_position = transform.position;
            }
            else
            {
                patrol_position = Vector3.zero;
            }
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
            return m_inventory.GetTotalWeight() > GetMaxCarryWeight();
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
            float skillFactor = Player.m_localPlayer.m_skills.GetSkillFactor(Skills.SkillType.Run);
            float num = Mathf.Lerp(1f, 0.5f, skillFactor);
            float num2 = m_runStaminaDrain * num;
            num2 -= num2 * Player.m_localPlayer.GetEquipmentMovementModifier();
            num2 += num2 * Player.m_localPlayer.GetEquipmentRunStaminaModifier();
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

        /*
         * 
         * Auto Pickup System from Player
         * 
         */
        private void AutoPickup(float dt)
        {
            if (IsTeleporting() || !m_enableAutoPickup)
            {
                return;
            }
            Vector3 vector = base.transform.position + Vector3.up;
            Collider[] array = Physics.OverlapSphere(vector, m_autoPickupRange, m_autoPickupMask);
            foreach (Collider collider in array)
            {
                if (!collider.attachedRigidbody)
                {
                    continue;
                }
                ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                FloatingTerrainDummy floatingTerrainDummy = null;
                if (component == null && (bool)(floatingTerrainDummy = collider.attachedRigidbody.gameObject.GetComponent<FloatingTerrainDummy>()) && (bool)floatingTerrainDummy)
                {
                    component = floatingTerrainDummy.m_parent.gameObject.GetComponent<ItemDrop>();
                }
                if (component == null || !component.m_autoPickup || HaveUniqueKey(component.m_itemData.m_shared.m_name) || !component.GetComponent<ZNetView>().IsValid())
                {
                    continue;
                }
                //Debug.Log("autopickup itemdrop is near");
                if (!component.CanPickup())
                {
                    //Debug.Log("RequestOwn");
                    component.RequestOwn();
                }
                else
                {
                    if (component.InTar())
                    {
                        //Debug.Log("InTar");
                        continue;
                    }
                    component.Load();
                    if (!m_inventory.CanAddItem(component.m_itemData) || component.m_itemData.GetWeight() + m_inventory.GetTotalWeight() > GetMaxCarryWeight())
                    {
                        //Debug.Log("CanAddItem");
                        continue;
                    }
                    float num = Vector3.Distance(component.transform.position, vector);
                    if (num > m_autoPickupRange)
                    {
                        //Debug.Log("num > m_autoPickupRange");
                        continue;
                    }
                    if (num < 0.3f)
                    {
                        Debug.Log("Picking up " + component.name);
                        Pickup(component.gameObject);
                        continue;
                    }

                    //Debug.Log("floatingTerrainDummy");
                    Vector3 vector2 = Vector3.Normalize(vector - component.transform.position);
                    float num2 = 15f;
                    Vector3 vector3 = vector2 * num2 * dt;
                    component.transform.position += vector3;
                    if ((bool)floatingTerrainDummy)
                    {
                        floatingTerrainDummy.transform.position += vector3;
                    }
                }
            }
        }

        /*
         * 
         * Food System from Player
         * 
         */
        private bool CanEat(ItemDrop.ItemData item, bool showMessages)
        {
            foreach (Player.Food food in m_foods)
            {
                if (food.m_item.m_shared.m_name == item.m_shared.m_name)
                {
                    if (food.CanEatAgain())
                    {
                        return true;
                    }
                    //Message(MessageHud.MessageType.Center, Localization.instance.Localize("$msg_nomore", item.m_shared.m_name));
                    return false;
                }
            }
            foreach (Player.Food food2 in m_foods)
            {
                if (food2.CanEatAgain())
                {
                    return true;
                }
            }
            if (m_foods.Count >= 3)
            {
                Message(MessageHud.MessageType.Center, "$msg_isfull");
                return false;
            }
            return true;
        }

        private Player.Food GetMostDepletedFood()
        {
            Player.Food food = null;
            foreach (Player.Food food2 in m_foods)
            {
                if (food2.CanEatAgain() && (food == null || food2.m_time < food.m_time))
                {
                    food = food2;
                }
            }
            return food;
        }

        public void ClearFood()
        {
            m_foods.Clear();
        }

        public bool RemoveOneFood()
        {
            if (m_foods.Count == 0)
            {
                return false;
            }
            m_foods.RemoveAt(UnityEngine.Random.Range(0, m_foods.Count));
            return true;
        }

        private bool EatFood(ItemDrop.ItemData item)
        {
            if (!CanEat(item, showMessages: false))
            {
                return false;
            }
            string text = "";
            if (item.m_shared.m_food > 0f)
            {
                text = text + " +" + item.m_shared.m_food + " $item_food_health ";
            }
            if (item.m_shared.m_foodStamina > 0f)
            {
                text = text + " +" + item.m_shared.m_foodStamina + " $item_food_stamina ";
            }
            if (item.m_shared.m_foodEitr > 0f)
            {
                text = text + " +" + item.m_shared.m_foodEitr + " $item_food_eitr ";
            }
            //Message(MessageHud.MessageType.Center, text);
            foreach (Player.Food food2 in m_foods)
            {
                if (food2.m_item.m_shared.m_name == item.m_shared.m_name)
                {
                    if (food2.CanEatAgain())
                    {
                        food2.m_time = item.m_shared.m_foodBurnTime;
                        food2.m_health = item.m_shared.m_food;
                        food2.m_stamina = item.m_shared.m_foodStamina;
                        food2.m_eitr = item.m_shared.m_foodEitr;
                        UpdateFood(0f, forceUpdate: true);
                        return true;
                    }
                    return false;
                }
            }
            if (m_foods.Count < 3)
            {
                Player.Food food = new Player.Food();
                food.m_name = item.m_dropPrefab.name;
                food.m_item = item;
                food.m_time = item.m_shared.m_foodBurnTime;
                food.m_health = item.m_shared.m_food;
                food.m_stamina = item.m_shared.m_foodStamina;
                food.m_eitr = item.m_shared.m_foodEitr;
                m_foods.Add(food);
                UpdateFood(0f, forceUpdate: true);
                return true;
            }
            Player.Food mostDepletedFood = GetMostDepletedFood();
            if (mostDepletedFood != null)
            {
                mostDepletedFood.m_name = item.m_dropPrefab.name;
                mostDepletedFood.m_item = item;
                mostDepletedFood.m_time = item.m_shared.m_foodBurnTime;
                mostDepletedFood.m_health = item.m_shared.m_food;
                mostDepletedFood.m_stamina = item.m_shared.m_foodStamina;
                UpdateFood(0f, forceUpdate: true);
                return true;
            }
            Game.instance.IncrementPlayerStat(PlayerStatType.FoodEaten);
            return false;
        }

        private void UpdateFood(float dt, bool forceUpdate)
        {
            m_foodUpdateTimer += dt;
            if (m_foodUpdateTimer >= 1f || forceUpdate)
            {
                m_foodUpdateTimer -= 1f;
                foreach (Player.Food food in m_foods)
                {
                    food.m_time -= 1f;
                    float f = Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime);
                    f = Mathf.Pow(f, 0.3f);
                    food.m_health = food.m_item.m_shared.m_food * f;
                    food.m_stamina = food.m_item.m_shared.m_foodStamina * f;
                    food.m_eitr = food.m_item.m_shared.m_foodEitr * f;
                    if (food.m_time <= 0f)
                    {
                        Message(MessageHud.MessageType.Center, "$msg_food_done");
                        m_foods.Remove(food);
                        break;
                    }
                }
                GetTotalFoodValue(out var hp, out var stamina, out var eitr);
                SetMaxHealth(hp);
                /*SetMaxStamina(stamina, flashBar: true);
                SetMaxEitr(eitr, flashBar: true);*/
                /*if (eitr > 0f)
                {
                    ShowTutorial("eitr");
                }*/
            }
            if (forceUpdate)
            {
                return;
            }
            m_foodRegenTimer += dt;
            if (!(m_foodRegenTimer >= 10f))
            {
                return;
            }
            m_foodRegenTimer = 0f;
            float num = 0f;
            foreach (Player.Food food2 in m_foods)
            {
                num += food2.m_item.m_shared.m_foodRegen;
            }
            if (num > 0f)
            {
                float regenMultiplier = 1f;
                m_seman.ModifyHealthRegen(ref regenMultiplier);
                num *= regenMultiplier;
                Heal(num);
            }
        }

        private void GetTotalFoodValue(out float hp, out float stamina, out float eitr)
        {
            hp = m_baseHP;
            stamina = m_baseStamina;
            eitr = 0f;
            foreach (Player.Food food in m_foods)
            {
                hp += food.m_health;
                stamina += food.m_stamina;
                eitr += food.m_eitr;
            }
        }

        public float GetBaseFoodHP()
        {
            return m_baseHP;
        }

        public List<Player.Food> GetFoods()
        {
            return m_foods;
        }

        public override bool CanConsumeItem(ItemDrop.ItemData item, bool checkWorldLevel = false)
        {
            /*if (!base.CanConsumeItem(item, checkWorldLevel))
            {
                return false;
            }
            if (item.m_shared.m_food > 0f && !CanEat(item, showMessages: true))
            {
                return false;
            }
            if ((bool)item.m_shared.m_consumeStatusEffect)
            {
                StatusEffect consumeStatusEffect = item.m_shared.m_consumeStatusEffect;
                if (m_seman.HaveStatusEffect(item.m_shared.m_consumeStatusEffect.NameHash()) || m_seman.HaveStatusEffectCategory(consumeStatusEffect.m_category))
                {
                    Message(MessageHud.MessageType.Center, "$msg_cantconsume");
                    return false;
                }
            }*/
            return true;
        }

        public override bool ConsumeItem(Inventory inventory, ItemDrop.ItemData item, bool checkWorldLevel = false)
        {
            if (!CanConsumeItem(item, checkWorldLevel))
            {
                return false;
            }
            if ((bool)item.m_shared.m_consumeStatusEffect)
            {
                _ = item.m_shared.m_consumeStatusEffect;
                m_seman.AddStatusEffect(item.m_shared.m_consumeStatusEffect, resetTime: true);
            }
            if (item.m_shared.m_food > 0f)
            {
                EatFood(item);
            }
            inventory.RemoveOneItem(item);
            return true;
        }
    }
}
