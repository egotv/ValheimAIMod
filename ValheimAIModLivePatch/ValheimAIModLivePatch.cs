using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using Jotunn.Managers;
using SimpleJson;



/*using Jotunn.Entities;
using Jotunn.Managers;*/
using UnityEngine;
using ValheimAIModLoader;
using static System.Net.Mime.MediaTypeNames;

[BepInPlugin("sahejhundal.ValheimAIModLivePatch", "Valheim AI NPC Mod Live Patch", "1.0.0")]
[BepInProcess("valheim.exe")]
public class ValheimAIModLivePatch : BaseUnityPlugin
{
    /*public class SpawnNPCCommand : ConsoleCommand
    {
        public override string Name => "ego_spawnNPC";

        public override string Help => "Spawns a friend in the world";

        public override void Run(string[] args)
        {
            instance.SpawnCompanion();
        }
    }*/

    private static ValheimAIModLivePatch instance;
    private readonly Harmony harmony = new Harmony("sahejhundal.ValheimAIModLivePatch");

    private ConfigEntry<KeyboardShortcut> spawnCompanionKey;
    private ConfigEntry<KeyboardShortcut> TogglePatrolKey;
    private ConfigEntry<KeyboardShortcut> ToggleFollowKey;
    private ConfigEntry<KeyboardShortcut> ToggleHarvestKey;
    private ConfigEntry<KeyboardShortcut> ToggleAttackKey;
    private ConfigEntry<KeyboardShortcut> InventoryKey;
    private ConfigEntry<bool> DisableAutoSave;

    private static string NPCPrefabName = "HumanoidNPC";

    private GameObject[] AllPlayerNPCInstances;
    private float AllPlayerNPCInstancesLastRefresh = 0f;
    private GameObject[] AllEnemiesInstances;
    private float AllEnemiesInstancesLastRefresh = 0f;
    private GameObject[] SmallTrees;

    // NPC VARS
    private ValheimAIModLoader.NPCCommand.CommandType eNPCMode;

    private float FollowUntilDistance = .5f;
    private float RunUntilDistance = 3f;

    private float StaminaExhaustedMinimumBreakTime = 3f;
    private float MinimumStaminaToRun = 5f;

    public Vector3 patrol_position = Vector3.zero;
    public float patrol_radius = 10f;
    public bool MovementLock = false;
    public float chaseUntilPatrolRadiusDistance = 15f;




    private void Awake()
    {
        Debug.Log("ValheimAIModLivePatch Loaded!");
        instance = this;

        ConfigBindings();

        harmony.PatchAll(typeof(ValheimAIModLivePatch));
    }

    private void ConfigBindings()
    {
        spawnCompanionKey = Config.Bind<KeyboardShortcut>("Keybinds", "SpawnCompanionKey", new KeyboardShortcut(KeyCode.G), "The key used to spawn an NPC.");
        TogglePatrolKey = Config.Bind<KeyboardShortcut>("Keybinds", "TogglePatrolKey", new KeyboardShortcut(KeyCode.P), "The key used to command all NPCs to patrol the area the player is at.");
        ToggleFollowKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleFollowKey", new KeyboardShortcut(KeyCode.F), "The key used to command all NPCs to follow you.");
        ToggleHarvestKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleHarvestKey", new KeyboardShortcut(KeyCode.H), "The key used to command all NPCs to go harvest.");
        ToggleAttackKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleAttackKey", new KeyboardShortcut(KeyCode.K), "The key used to command all NPCs to attack enemies.");
        InventoryKey = Config.Bind<KeyboardShortcut>("Keybinds", "InventoryKey", new KeyboardShortcut(KeyCode.Y), "The key used to command all NPCs to -");
        DisableAutoSave = Config.Bind<bool>("Bool", "DisableAutoSave", false, "Disable auto saving the game world?");
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }

    // PROCESS PLAYER INPUT
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), "Update")]
    private static void Player_Update_Postfix(Player __instance)
    {
        KeyboardShortcut value = instance.spawnCompanionKey.Value;
        if (value.IsDown())
        {
            instance.SpawnCompanion();
            return;
        }
        value = instance.TogglePatrolKey.Value;
        if (value.IsDown())
        {
            instance.StartPatrol(__instance);
            return;
        }
        value = instance.ToggleFollowKey.Value;
        if (value.IsDown())
        {
            instance.StartFollowing(__instance);
            return;
        }
        value = instance.ToggleHarvestKey.Value;
        if (value.IsDown())
        {
            instance.StartHarvesting(__instance);
            return;
        }
        value = instance.ToggleAttackKey.Value;
        if (value.IsDown())
        {
            instance.StartAttacking(__instance);
            return;
        }
        value = instance.InventoryKey.Value;
        if (value.IsDown())
        {
            instance.OnInventoryKeyPressed(__instance);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HumanoidNPC), "CustomFixedUpdate")]
    private static void HumanoidNPC_CustomFixedUpdate_Postfix(HumanoidNPC __instance)
    {
        //Debug.Log(__instance.m_crouchToggled);
        //Debug.Log(__instance.GetVelocity().ToString());
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    private static bool MonsterAI_CustomFixedUpdate_Prefix(MonsterAI __instance)
    {
        if (!__instance.name.Contains("HumanoidNPC")) return true;

        if (instance.eNPCMode == NPCCommand.CommandType.PatrolArea && instance.patrol_position != Vector3.zero)
        {
            float dist = __instance.transform.position.DistanceTo(instance.patrol_position);
            if (dist > instance.chaseUntilPatrolRadiusDistance)
            {
                SetMonsterAIAggravated(__instance, false);
                instance.MovementLock = true;
            }

            else if (dist < 3f)
            {
                instance.MovementLock = false;
                return true;
            }
            else if (dist < instance.patrol_radius)
            {
                __instance.m_aggravatable = true;
            }

            

            if (instance.MovementLock)
            {
                __instance.MoveTo(Time.deltaTime, instance.patrol_position, 0f, false);
                return false;
            }
        }
           

        return true;

        /*if (instance.eNPCMode == NPCMode.PatrolArea && instance.patrol_position != Vector3.zero && instance.patrol_position.DistanceTo(__instance.transform.position) > instance.PatrolArea_radius)
        {
            __instance.MoveTo(Time.deltaTime, instance.patrol_position, 0f, false);
        }
        else if (instance.patrol_position.DistanceTo(__instance.transform.position) < 2f)
        {
            instance.patrol_position = Vector3.zero;
        }*/
    }


    // OVERRIDE CheckRun: NPC can run when Player has over 5 stamina
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), "CheckRun")]
    public static void Character_CheckRun_Postfix(Character __instance, Vector3 moveDir, float dt, ref bool __result)
    {
        if (__instance.name.Contains("HumanoidNPC"))
        {
            ValheimAIModLoader.HumanoidNPC humanoidnpc_component = __instance.GetComponent<ValheimAIModLoader.HumanoidNPC>();
            if (humanoidnpc_component.CurrentCommand == NPCCommand.CommandType.FollowPlayer)
            {
                __result = Player.m_localPlayer.HaveStamina(5f);
            }
        }
    }

    // OVERRIDE CheckRun: if this character can run? with stamina logic
    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "CheckRun")]
    public static bool Character_CheckRun_Prefix(Character __instance, Vector3 moveDir, float dt)
    {
        // 
        ValheimAIModLoader.NPCScript npcscript_component = IsScriptNPC(__instance);
        if (npcscript_component)
        {
            if (Time.time - npcscript_component.m_staminaLastBreakTime < instance.StaminaExhaustedMinimumBreakTime)
            {
                return false;
            }
            else if (npcscript_component.m_stamina < instance.MinimumStaminaToRun)
            {
                npcscript_component.m_staminaLastBreakTime = Time.time;
                return false;
            }
        }
        return true;
    }*/

    /* AI starts running again instantly when stamina > min_stamina.
     * This isn't the case with Player's since you have to press shift again to run after your stamina has been drained.
     * So after NPC's stamina has been drained completely, the min_stamina to run again should be higher to avoid stamina being stuck around min_stamina */
    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(BaseAI), "MoveTo")]
    public static void BaseAI_MoveTo_Prefix(BaseAI __instance, ref float dt, ref Vector3 point, ref float dist, ref bool run)
    {
        ValheimAIModLoader.NPCScript npcscript_component = IsScriptNPC(__instance.m_character);
        if (npcscript_component)
        {
            if (npcscript_component.m_stamina < min_stamina + 5)
            {
                run = false;
                Debug.Log("MoveTo told ai bot to stop running");
            }
        }
    }*/


    // OVERRIDE AI "RUN OR WALK?" LOGIC WHEN FOLLOWING A CHARACTER
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseAI), "Follow")]
    private static bool BaseAI_Follow_Prefix(BaseAI __instance, GameObject go, float dt)
    {
        // EXECUTES ON BASEAI::TICK
        if (!__instance.name.Contains(NPCPrefabName)) return true;


        float RunDistance;
        float FollowDistance;

        if (go == Player.m_localPlayer.gameObject)
        {
            RunDistance = 10f;
            FollowDistance = 7f;
        }
        else
        {
            RunDistance = instance.RunUntilDistance;
            FollowDistance = instance.FollowUntilDistance;
        }


        float num = Vector3.Distance(go.transform.position, __instance.transform.position);
        bool run = num > RunDistance && !Player.m_localPlayer.IsCrouching() && !Player.m_localPlayer.m_walk;
        if (num < FollowDistance - (Player.m_localPlayer.IsCrouching() ? 5f : 2f))
        {
            __instance.StopMoving();
        }
        else
        {
            __instance.MoveTo(dt, go.transform.position, 0f, run);
        }

        return false;
    }

    // OVERRIDE ON PET/ ON TAME
    /*[HarmonyPostfix]
    [HarmonyPatch(typeof(Tameable), "Interact")]
    private static void Tameable_Interact_Postfix(Tameable __instance, Humanoid user, bool hold, bool alt, ref bool __result)
    {
    }*/

    // OVERRIDE NPC OVERLAY HUD
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), "GetHoverText")]
    private static void Character_GetHoverText_Postfix(Character __instance, ref string __result)
    {
        if (__instance.name.Contains("HumanoidNPC"))
            __result += "\n<color=purple><b>" + instance.eNPCMode.ToString().ToUpper() + "</b></color>";
    }

    // TO READ NAMES OF ATTACK ANIMS
    /*[HarmonyPostfix]
    [HarmonyPatch(typeof(Attack), "Start")]
    private static void Attack_Start_Postfix(Attack __instance)
    {
        // TO FIND OUT ANIMATIONS NAMES
        Debug.Log("Attack anim " + __instance.m_attackAnimation);
    }*/



    /*
     * 
     * PLAYER STATS MIRRORING
     * 
     */

    // HEAL NPC WHEN HEALING PLAYER
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Player), "UpdateFood")]
    private static bool Player_UpdateFood_Prefix(Player __instance, float dt, bool forceUpdate)
    {
        __instance.m_foodUpdateTimer += dt;
        if (__instance.m_foodUpdateTimer >= 1f || forceUpdate)
        {
            __instance.m_foodUpdateTimer -= 1f;
            foreach (Player.Food food in __instance.m_foods)
            {
                food.m_time -= 1f;
                float f = Mathf.Clamp01(food.m_time / food.m_item.m_shared.m_foodBurnTime);
                f = Mathf.Pow(f, 0.3f);
                food.m_health = food.m_item.m_shared.m_food * f;
                food.m_stamina = food.m_item.m_shared.m_foodStamina * f;
                food.m_eitr = food.m_item.m_shared.m_foodEitr * f;
                if (food.m_time <= 0f)
                {
                    //Message(MessageHud.MessageType.Center, "$msg_food_done");
                    __instance.m_foods.Remove(food);
                    break;
                }
            }
            __instance.GetTotalFoodValue(out var hp, out var stamina, out var eitr);
            __instance.SetMaxHealth(hp, flashBar: true);
            __instance.SetMaxStamina(stamina, flashBar: true);
            __instance.SetMaxEitr(eitr, flashBar: true);
            if (eitr > 0f)
            {
                __instance.ShowTutorial("eitr");
            }
        }
        if (forceUpdate)
        {
            return false;
        }
        __instance.m_foodRegenTimer += dt;
        if (!(__instance.m_foodRegenTimer >= 10f))
        {
            return false;
        }
        __instance.m_foodRegenTimer = 0f;
        float num = 0f;
        foreach (Player.Food food2 in __instance.m_foods)
        {
            num += food2.m_item.m_shared.m_foodRegen;
        }
        if (num > 0f)
        {
            float regenMultiplier = 1f;
            __instance.m_seman.ModifyHealthRegen(ref regenMultiplier);
            num *= regenMultiplier;
            __instance.Heal(num);

            GameObject[] allNpcs = instance.FindPlayerNPCs();
            foreach (GameObject npc in allNpcs)
            {
                MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
                ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

                if (humanoidComponent != null)
                {
                    humanoidComponent.Heal(num);
                }
            }
        }

        return false;
    }
    
    // NPC CROUCHES WHEN PLAYER CROUCHES
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), "SetCrouch")]
    private static void Player_SetCrouch_Postfix(Player __instance, bool crouch)
    {

        GameObject[] allNpcs = instance.FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

            if (humanoidComponent != null)
            {
                humanoidComponent.SetCrouch(crouch);
            }
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), "SetWalk")]
    private static void Character_SetWalk_Postfix(Character __instance, bool walk)
    {
        if (__instance != Player.m_localPlayer) return;

        GameObject[] allNpcs = instance.FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

            if (humanoidComponent != null)
            {
                humanoidComponent.SetWalk(walk);
            }
        }
    }

    // NPC HIDES WEAPON WHEN PLAYER HIDES WEAPON
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), "HideHandItems")]
    private static void Humanoid_HideHandItems_Postfix(Humanoid __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            GameObject[] allNpcs = instance.FindPlayerNPCs();
            foreach (GameObject npc in allNpcs)
            {
                MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
                ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

                if (humanoidComponent != null)
                {
                    humanoidComponent.HideHandItems();
                }
            }
        }
    }
    
    // NPC SHOWS WEAPON WHEN PLAYER SHOWS WEAPON
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), "ShowHandItems")]
    private static void Humanoid_ShowHandItems_Postfix(Humanoid __instance)
    {
        if (__instance == Player.m_localPlayer)
        {
            GameObject[] allNpcs = instance.FindPlayerNPCs();
            foreach (GameObject npc in allNpcs)
            {
                MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
                ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

                if (humanoidComponent != null)
                {
                    humanoidComponent.ShowHandItems();
                }
            }
        }
    }




    // SOMETIMES AI DOESNT START ATTACKING EVEN THOUGH IT IS IN CLOSE RANGE, SO CHECK AND ATTACK ON UPDATE
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), "CustomFixedUpdate")]
    private static void Humanoid_CustomFixedUpdate_Postfix(Humanoid __instance, float fixedDeltaTime)
    {
        if (__instance && __instance.name.Contains(NPCPrefabName))
        {
            MonsterAI monsterAIcomponent = __instance.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidcomponent = __instance.GetComponent<ValheimAIModLoader.HumanoidNPC>();

            if (monsterAIcomponent && monsterAIcomponent.m_follow && monsterAIcomponent.m_follow != Player.m_localPlayer.gameObject)
            {
                if (monsterAIcomponent.m_follow.transform.position.DistanceTo(__instance.transform.position) < instance.FollowUntilDistance + .2f && !humanoidcomponent.InAttack())
                {
                    //Debug.Log("Close enough");
                    monsterAIcomponent.LookAt(monsterAIcomponent.m_follow.transform.position);
                    humanoidcomponent.StartAttack(humanoidcomponent, false);
                }
            }
        }
    }

    // HELPER FUNCTION TO DISABLE AI AGGRAVATION TOWARDS ENEMIES
    private static void SetMonsterAIAggravated(MonsterAI monsterAIcomponent, bool Aggravated)
    {
        if (Aggravated)
        {
            monsterAIcomponent.m_aggravatable = true;
        }
        else
        {
            monsterAIcomponent.m_aggravated = false;
            monsterAIcomponent.m_aggravatable = false;
            monsterAIcomponent.m_alerted = false;

            monsterAIcomponent.m_eventCreature = false;
            monsterAIcomponent.m_targetCreature = null;
            monsterAIcomponent.m_targetStatic = null;
            //monsterAIcomponent.m_viewRange = 0f;
            monsterAIcomponent.SetHuntPlayer(false);
        }
    }




    /*
     * 
     * NPC COMMANDS
     * 
     */

    private void StartPatrol(Player player)
    {
        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidNPC_component = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

            patrol_position = player.transform.position;
            eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.PatrolArea;

            //Vector3 randLocation = GetRandomReachableLocationInRadius(humanoidNPC_component.patrol_position, patrol_radius);

            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(null);
        }
    }
    
    private void StartFollowing(Player player)
    {
        eNPCMode = NPCCommand.CommandType.FollowPlayer;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidnpc_component = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();
            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(player.gameObject);
            Debug.Log("Everyone now following player!");

            

            string text = "Coming!";
            UserInfo userInfo = new UserInfo();
            userInfo.Name = "npc";

            Vector3 headPoint = humanoidnpc_component.GetEyePoint();
            Chat.instance.AddInworldText(npc, 0, headPoint, Talker.Type.Shout, userInfo, text);
            //humanoidnpc_component.m_zanim.SetTrigger("Talk");
        }
    }

    private void StartHarvesting(Player player)
    {
        eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.HarvestResource;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            GameObject ClosestTree = FindClosestTreeFor(npc);

            // disregard nearby enemies
            monsterAIcomponent.m_eventCreature = false;
            monsterAIcomponent.m_targetCreature = null;
            monsterAIcomponent.m_viewRange = 0f;
            monsterAIcomponent.m_alerted = false;
            monsterAIcomponent.m_aggravatable = false;
            monsterAIcomponent.SetHuntPlayer(false);
            monsterAIcomponent.m_aggravated = false;
            monsterAIcomponent.SetFollowTarget(ClosestTree);

            Debug.Log("Everyone harvesting!");

            //TODO: AVOID MULTIPLE NPCS GOING TO CHOP THE SAME TREE
            //TODO: LOOP FUNCTION TO KEEP HARVESTING RESOURCES UNTIL A CONDITION IS MET
        }
    }

    private void StartAttacking(Player player)
    {
        eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.AttackTarget;

        GameObject[] allEnemies = FindEnemies();
        foreach (GameObject npc in allEnemies)
        {
            Debug.Log(npc.name);
        } 

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();

            // disregard nearby enemies
            monsterAIcomponent.SetFollowTarget(null);
            monsterAIcomponent.m_viewRange = 50f;
            monsterAIcomponent.m_alerted = false;
            monsterAIcomponent.m_aggravatable = true;
            monsterAIcomponent.SetHuntPlayer(true);

            Debug.Log("Everyone attacking!");
        }
    }

    private void OnInventoryKeyPressed(Player player)
    {
        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();
            if (monsterAIcomponent != null && humanoidComponent != null)
            {
                /*GameObject[] pickable_stones = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.Contains("Pickable_"))
                .ToArray();

                Debug.Log("ps len " + pickable_stones.Length);
                int i = 0;

                foreach (GameObject go in pickable_stones)
                {
                    if (i > 20) break;
                    i++;
                    player.Interact(go, false, false);
                    player.Pickup(go);
                }*/


                /*GameObject itemPrefab = ZNetScene.instance.GetPrefab("Bread");
                if (itemPrefab != null)
                {
                    humanoidComponent.GetInventory().AddItem(itemPrefab.gameObject, 5);
                    player.GetInventory().AddItem(itemPrefab.gameObject, 5);
                }
                else
                {
                    Debug.LogError($"itemprefab was null");
                }

                ItemDrop.ItemData bread_itemdata = humanoidComponent.GetInventory().GetItem("$item_bread");
                if (bread_itemdata != null)
                {
                    humanoidComponent.UseItem(humanoidComponent.GetInventory(), bread_itemdata, true);
                }
                else
                {
                    Debug.LogError("bread_itemdata was null");
                }*/

                //humanoidComponent.m_zanim.SetTrigger("eat");

                Debug.Log(GetNPCGameState(npc));
                //PrintInventoryItems(humanoidComponent.m_inventory);
            }
        }
    }

    private void SpawnCompanion()
    {
        Player localPlayer = Player.m_localPlayer;
        GameObject npcPrefab = ZNetScene.instance.GetPrefab("HumanoidNPC");
        //GameObject npcPrefab = HumanoidNPCPrefab;

        if (npcPrefab == null)
        {
            Logger.LogError("ScriptNPC prefab not found!");
        }

        // spawn NPC
        Vector3 spawnPosition = localPlayer.transform.position + localPlayer.transform.forward * 2f;
        Quaternion spawnRotation = localPlayer.transform.rotation;
        GameObject npcInstance = Instantiate<GameObject>(npcPrefab, spawnPosition, spawnRotation);
        npcInstance.SetActive(true);

        // make the monster tame
        MonsterAI monsterAIcomp = npcInstance.GetComponent<MonsterAI>();

        SetMonsterAIAggravated(monsterAIcomp, false);
        //monsterAIcomp.SetFollowTarget(localPlayer.gameObject);
        monsterAIcomp.MakeTame();
        //monsterAIcomp.SetHuntPlayer(true);





        // add item to inventory
        ValheimAIModLoader.HumanoidNPC humanoidNpc_Component = npcInstance.GetComponent<ValheimAIModLoader.HumanoidNPC>();
        if (humanoidNpc_Component != null)
        {
            GameObject itemPrefab = ZNetScene.instance.GetPrefab("AxeBronze");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);
            
            itemPrefab = ZNetScene.instance.GetPrefab("ArmorBronzeChest");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);
            
            itemPrefab = ZNetScene.instance.GetPrefab("ArmorBronzeLegs");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);

            humanoidNpc_Component.m_crouchSpeed = 100f;

            humanoidNpc_Component.SetHair("Hair17"); 

            humanoidNpc_Component.SetMaxHealth(300);
            humanoidNpc_Component.SetHealth(300);

            HitData hitData = new HitData(80);
            humanoidNpc_Component.Damage(hitData);

            /*GameObject itemPrefab = ZNetScene.instance.GetPrefab("Bread");
            if (itemPrefab != null)
            {
                humanoidNpc_Component.GetInventory().AddItem(itemPrefab.gameObject, 15);
            }
            else
            {
                Debug.LogError("bread prefab was null");
            }*/
        }
        else
        {
            Logger.LogError("humanoidNpc_Component component not found on the instantiated ScriptNPC prefab!");
        }
    }

    public static string GetNPCGameState(GameObject character)
    {
        Dictionary<string, object> characterData = new Dictionary<string, object>();

        HumanoidNPC humanoidNPC = character.GetComponent<HumanoidNPC>();
        MonsterAI monsterAI = character.GetComponent<MonsterAI>();


        var inventoryItems = new JsonArray();
        foreach (ItemDrop.ItemData item in humanoidNPC.m_inventory.m_inventory)
        {
            var itemData = new JsonObject
            {
                ["name"] = item.m_shared.m_name,
                ["amount"] = item.m_stack,
            };
            inventoryItems.Add(itemData);
        }

        var gameState = new JsonObject
        {
            ["Name"] = humanoidNPC.m_name,
            ["Health"] = humanoidNPC.GetHealth(),
            ["Stamina"] = Player.m_localPlayer.GetStamina(),
            ["Inventory"] = inventoryItems,
            //["position"] = humanoidNPC.transform.position.ToString(),

            
            //["npcMode"] = humanoidNPC.CurrentCommand.ToString(),
            ["NPC_Mode"] = instance.eNPCMode.ToString(),
            ["Alerted"] = monsterAI.m_alerted,

            

            ["IsCold"] = EnvMan.IsCold(),
            ["IsFreezing"] = EnvMan.IsFreezing(),
            ["IsWet"] = EnvMan.IsWet(),

            ["Time"] = EnvMan.instance.GetDayFraction(),
        };

        var jsonObject = new JsonObject
        {
            ["player_id"] = humanoidNPC.GetZDOID().ToString(),
            ["game_state"] = gameState,
        };

        string jsonString = SimpleJson.SimpleJson.SerializeObject(jsonObject);

        return jsonString;
    }

    private GameObject[] FindEnemies()
    {
        if (Time.time - AllEnemiesInstancesLastRefresh < 1f)
        {
            return instance.AllEnemiesInstances;
        }
        instance.AllEnemiesInstances = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.HasAnyComponent("MonsterAI"))
                .ToArray();
        AllEnemiesInstancesLastRefresh = Time.time;
        return instance.AllEnemiesInstances;
    }

    private GameObject[] FindPlayerNPCs()
    {
        if (Time.time - AllPlayerNPCInstancesLastRefresh < 1f)
        {
            return instance.AllPlayerNPCInstances;
        }
        instance.AllPlayerNPCInstances = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.Contains(NPCPrefabName))
                .ToArray();
        AllPlayerNPCInstancesLastRefresh = Time.time;
        return instance.AllPlayerNPCInstances;
    }

    private GameObject FindClosestTreeFor(GameObject go, string TreeType = "small")
    {
        if (TreeType == "small")
            return FindSmallTrees().Where(t => t.gameObject.name.StartsWith("Beech_small"))// || t.gameObject.name.StartsWith("Pine"))
                .OrderBy(t => Vector3.Distance(go.transform.position, t.transform.position))
                .FirstOrDefault();
        return null;
    }

    private static GameObject[] FindSmallTrees()
    {
        instance.SmallTrees = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.StartsWith("Beech_small"))
                .ToArray();
        return instance.SmallTrees;
    }

    private static void PrintInventoryItems(Inventory inventory)
    {
        Debug.Log("Character Inventory Items:");

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        foreach (ItemDrop.ItemData item in items)
        {
            Debug.Log($"- {item.m_shared.m_name} (Quantity: {item.m_stack})");
        }
    }

    public static Vector3 GetRandomReachableLocationInRadius(Vector3 center, float radius)
    {
        Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        Vector3 randomLocation = center + randomDirection;

        int maxAttempts = 10;
        int attempts = 0;

        while (!IsLocationReachable(randomLocation) && attempts < maxAttempts)
        {
            randomDirection = UnityEngine.Random.insideUnitSphere * radius;
            randomLocation = center + randomDirection;
            attempts++;
        }

        return randomLocation;
    }

    private static bool IsLocationReachable(Vector3 location)
    {
        // Perform a sphere cast to check if the location is reachable
        RaycastHit hit;
        if (Physics.SphereCast(location + Vector3.up * 500f, 0.5f, Vector3.down, out hit, 1000f, LayerMask.GetMask("Default", "static_solid", "Default_small", "Terrain")))
        {
            // Check if the hit point is close enough to the desired location
            float distance = Vector3.Distance(hit.point, location);
            if (distance <= 1f)
            {
                return true;
            }
        }
        return false;
    }

    // Disable auto save
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), "UpdateSaving")]
    private static bool Game_UpdateSaving_Prefix()
    {
        return !instance.DisableAutoSave.Value;
    }
}
