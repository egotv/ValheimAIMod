using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using SimpleJson;



/*using Jotunn.Entities;
using Jotunn.Managers;*/
using UnityEngine;
using ValheimAIModLoader;
using System.IO;
using System.Net;
using Jotunn.Managers;
using System.ComponentModel;

[BepInPlugin("egovalheimmod.ValheimAIModLivePatch", "EGO.AI Valheim AI NPC Mod Live Patch", "0.0.1")]
[BepInProcess("valheim.exe")]
public class ValheimAIModLivePatch : BaseUnityPlugin
{
    private static ValheimAIModLivePatch instance;
    private readonly Harmony harmony = new Harmony("egovalheimmod.ValheimAIModLivePatch");
    
    //private const string brainBaseURL = "http://localhost:5000";
    private const string brainBaseURL = "https://valheim-agent-brain.fly.dev";

    private string playerDialogueAudioPath;
    private string npcDialogueAudioPath;
    private string npcDialogueRawAudioPath;

    private const int NUMBER_OF_CHANNELS = 2;
    private const int SAMPLE_WIDTH = 2;
    private const int FRAME_RATE = 48000;

    private ConfigEntry<KeyboardShortcut> spawnCompanionKey;
    private ConfigEntry<KeyboardShortcut> TogglePatrolKey;
    private ConfigEntry<KeyboardShortcut> ToggleFollowKey;
    private ConfigEntry<KeyboardShortcut> ToggleHarvestKey;
    private ConfigEntry<KeyboardShortcut> ToggleAttackKey;
    private ConfigEntry<KeyboardShortcut> InventoryKey;
    private ConfigEntry<KeyboardShortcut> TalkKey;
    private ConfigEntry<KeyboardShortcut> SendToBrainKey;

    private ConfigEntry<int> MicrophoneIndex;
    private ConfigEntry<float> CompanionVolume;
    private ConfigEntry<bool> DisableAutoSave;

    private Dictionary<string, Piece.Requirement[]> craftingRequirements = new Dictionary<string, Piece.Requirement[]>();
    private Dictionary<string, Piece.Requirement[]> buildingRequirements = new Dictionary<string, Piece.Requirement[]>();
    private Dictionary<string, List<string>> resourceLocations = new Dictionary<string, List<string>>();

    private static string NPCPrefabName = "HumanoidNPC";


    private GameObject PlayerNPC;
    private GameObject[] AllPlayerNPCInstances;
    private float AllPlayerNPCInstancesLastRefresh = 0f;

    private GameObject[] AllEnemiesInstances;
    private float AllEnemiesInstancesLastRefresh = 0f;

    //private GameObject[] AllPickableInstances;
    private List<GameObject> AllPickableInstances = new List<GameObject>();
    private float AllPickableInstancesLastRefresh = 0f;

    private GameObject[] SmallTrees;

    // NPC VARS
    private ValheimAIModLoader.NPCCommand.CommandType eNPCMode;

    private float FollowUntilDistance = .5f;
    private float RunUntilDistance = 3f;
    public Vector3 patrol_position = Vector3.zero;
    public float patrol_radius = 10f;
    public bool patrol_harvest = false;
    public bool MovementLock = false;
    public float chaseUntilPatrolRadiusDistance = 20f;

    public bool PlayingAnim = false;

    private AudioClip recordedAudioClip;
    public bool IsRecording = false;


    private void Awake()
    {
        Debug.Log("ValheimAIModLivePatch Loaded!");
        instance = this;

        ConfigBindings();

        /*PopulateCraftingRequirements();
        PopulateBuildingRequirements();*/

        playerDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "playerdialogue.wav");
        npcDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue.wav");
        npcDialogueRawAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");

        Chat.instance.SendText(Talker.Type.Normal, "EGO.AI MOD LOADED!");

        GetRecordingDevices();

        harmony.PatchAll(typeof(ValheimAIModLivePatch));
    }

    private void ConfigBindings()
    {
        spawnCompanionKey = Config.Bind<KeyboardShortcut>("Keybinds", "SpawnCompanionKey", new KeyboardShortcut(KeyCode.G), "The key used to spawn an NPC.");
        TogglePatrolKey = Config.Bind<KeyboardShortcut>("Keybinds", "TogglePatrolKey", new KeyboardShortcut(KeyCode.P), "The key used to command all NPCs to patrol the area the player is at.");
        ToggleFollowKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleFollowKey", new KeyboardShortcut(KeyCode.F), "The key used to command all NPCs to follow you.");
        ToggleHarvestKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleHarvestKey", new KeyboardShortcut(KeyCode.H), "The key used to command all NPCs to go harvest.");
        ToggleAttackKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleAttackKey", new KeyboardShortcut(KeyCode.K), "The key used to command all NPCs to attack enemies.");
        InventoryKey = Config.Bind<KeyboardShortcut>("Keybinds", "InventoryKey", new KeyboardShortcut(KeyCode.U), "The key used to command all NPCs to -");
        TalkKey = Config.Bind<KeyboardShortcut>("Keybinds", "TalkKey", new KeyboardShortcut(KeyCode.T), "The key used to talk into the game");
        SendToBrainKey = Config.Bind<KeyboardShortcut>("Keybinds", "SendToBrainKey", new KeyboardShortcut(KeyCode.Y), "The key used to ");

        MicrophoneIndex = Config.Bind<int>("Integer", "MicrophoneIndex", 0, "Input device index in Windows Sound Settings.");
        CompanionVolume = Config.Bind<float>("Float", "CompanionVolume", 1f, "NPC dialogue volume (0-1)");
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
        /*GameObject resource = FindClosestResource(__instance.gameObject, "Pickable_");
        Pickable pick = resource.GetComponent<Pickable>();
        pick.Interact(__instance, false, false);*/

        if (!ZNetScene.instance || !Player.m_localPlayer)
        {
            // Player is not in a world, allow input
            return;
        }

        if (Menu.IsVisible() || Console.IsVisible() || Chat.instance.HasFocus())
        {
            //Debug.Log("Menu visible");
            return;
        }

        KeyboardShortcut value = instance.spawnCompanionKey.Value;
        if (value.IsDown())
        {
            instance.SpawnCompanion();
            instance.StartFollowing(__instance);
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
        value = instance.TalkKey.Value;
        if (value.IsDown())
        {
            if (!instance.IsRecording)
            {
                instance.StartRecording();
            }
            else
            {
                instance.StopRecording();
                instance.SendToBrain();
            }
            return;
        }
        value = instance.SendToBrainKey.Value;
        if (value.IsDown())
        {
            //instance.PlayRecordedAudio("");
            //instance.LoadAndPlayAudioFromBase64(instance.npcDialogueAudioPath);
            //instance.PlayWavFile(instance.npcDialogueRawAudioPath);
            /*if (instance.PlayerNPC)
            {
                HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                InventoryGui.instance.Show(humanoidNPC_component.inventoryContainer);
            }*/
            
            return;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    private static bool MonsterAI_CustomFixedUpdate_Prefix(MonsterAI __instance)
    {
        if (!__instance.name.Contains("HumanoidNPC")) return true;

        if (instance.eNPCMode == NPCCommand.CommandType.PatrolArea && instance.patrol_position != Vector3.zero)
        {
            float dist = __instance.transform.position.DistanceTo(instance.patrol_position);

            if (dist > instance.chaseUntilPatrolRadiusDistance && !instance.MovementLock)
            {
                SetMonsterAIAggravated(__instance, false);
                instance.MovementLock = true;
                Debug.Log("Running away from patrol radius, heading back to patrol position");
            }
            else if (dist < instance.patrol_radius - 3f)
            {
                instance.MovementLock = false;
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

            GameObject followtarget = __instance.m_follow;

            if (followtarget != null && (followtarget.HasAnyComponent("Character") || followtarget.HasAnyComponent("Humanoid")))
            {
                Debug.Log("follow target is not null and either character or humanoid");
                Debug.Log(followtarget.name + " " + followtarget.transform.position.DistanceTo(instance.patrol_position));
                return true;
            }

            if (instance.patrol_harvest)
            {
                //Debug.Log("patrol harvest");
                if (followtarget == null || followtarget.transform.position.DistanceTo(instance.patrol_position) > instance.chaseUntilPatrolRadiusDistance ||
                                (!followtarget.HasAnyComponent("Pickable") && !followtarget.HasAnyComponent("ItemDrop")))
                {
                    //Debug.Log("new follow");
                    GameObject newfollow;
                    newfollow = FindClosestPickableResource(__instance.gameObject, instance.patrol_position, instance.chaseUntilPatrolRadiusDistance);
                    if (newfollow == null)
                    {
                        Debug.Log("Stopped harvesting while patrolling!");
                        instance.patrol_harvest = false;
                        return true;
                    }

                    else if (newfollow.transform.position.DistanceTo(instance.patrol_position) < instance.chaseUntilPatrolRadiusDistance)
                    {
                        Debug.Log("Going to loot " + newfollow.name + ", distance: " + newfollow.transform.position.DistanceTo(__instance.transform.position));
                        __instance.SetFollowTarget(newfollow);
                    }
                    else
                    {
                        Debug.Log("distance too far!");
                    }
                }
            }
            
            return true;
        }

        else if (instance.eNPCMode == NPCCommand.CommandType.HarvestResource)
        {
            HumanoidNPC humanoidNPC = __instance.gameObject.GetComponent<HumanoidNPC>();
            //Debug.Log("LastPositionDelta " + humanoidNPC.LastPositionDelta);
            if (humanoidNPC.LastPositionDelta > 2.5f && !humanoidNPC.InAttack())
            {
                humanoidNPC.StartAttack(humanoidNPC, false);
            }

            if (__instance.m_follow == null)
            {
                GameObject newfollow;
                newfollow = FindClosestResource(__instance.gameObject, "Beech_small");

                __instance.SetFollowTarget(newfollow);
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

    // SOMETIMES AI DOESNT START ATTACKING EVEN THOUGH IT IS IN CLOSE RANGE, SO CHECK AND ATTACK ON UPDATE

    public Minimap.PinType pinType = Minimap.PinType.Icon0;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HumanoidNPC), "CustomFixedUpdate")]
    private static void HumanoidNPC_CustomFixedUpdate_Postfix(HumanoidNPC __instance)
    {
        /*Minimap.instance.UpdatePins();
        Minimap.instance.SetMapPin(__instance.name, __instance.transform.position);*/



        MonsterAI monsterAIcomponent = __instance.GetComponent<MonsterAI>();

        if (__instance.LastPosition.DistanceTo(__instance.transform.position) > 1f)
        {
            __instance.LastPosition = __instance.transform.position;
            __instance.LastPositionDelta = 0;
        }
        else
        {
            __instance.LastPositionDelta += Time.deltaTime;
        }

        

        if (monsterAIcomponent && monsterAIcomponent.m_follow != null && monsterAIcomponent.m_follow != Player.m_localPlayer.gameObject)
        {
            if (!__instance.InAttack())
            {
                if (monsterAIcomponent.m_follow.transform.position.DistanceTo(__instance.transform.position) < instance.FollowUntilDistance + .5f)
                {
                    if (monsterAIcomponent.m_follow.HasAnyComponent("ItemDrop"))
                    {
                        __instance.DoInteractAnimation(monsterAIcomponent.m_follow.transform.position);

                        Destroy(monsterAIcomponent.m_follow);
                        instance.AllPickableInstances.Remove(monsterAIcomponent.m_follow);

                        monsterAIcomponent.SetFollowTarget(null);
                        monsterAIcomponent.m_targetCreature = null;
                        monsterAIcomponent.m_targetStatic = null;
                    }
                    else if (monsterAIcomponent.m_follow.HasAnyComponent("Pickable"))
                    {
                        __instance.DoInteractAnimation(monsterAIcomponent.m_follow.transform.position);

                        Pickable pick = monsterAIcomponent.m_follow.GetComponent<Pickable>();
                        pick.Interact(Player.m_localPlayer, false, false);
                        Destroy(monsterAIcomponent.m_follow);
                        instance.AllPickableInstances.Remove(monsterAIcomponent.m_follow);

                        monsterAIcomponent.SetFollowTarget(null);
                        monsterAIcomponent.m_targetCreature = null;
                        monsterAIcomponent.m_targetStatic = null;

                        
                    }
                    //else if (monsterAIcomponent.m_follow.HasAnyComponent("Character") || monsterAIcomponent.m_follow.HasAnyComponent("Humanoid"))
                    else
                    {
                        monsterAIcomponent.LookAt(monsterAIcomponent.m_follow.transform.position);
                        __instance.StartAttack(__instance, false);
                    }
                }
                else if (__instance.GetVelocity().magnitude < .2f && __instance.LastPositionDelta > 3f && !monsterAIcomponent.CanMove(__instance.transform.position - monsterAIcomponent.m_follow.transform.position, 1f, 1f))
                {
                    monsterAIcomponent.LookAt(monsterAIcomponent.m_follow.transform.position);
                    __instance.StartAttack(__instance, false);
                }
                /*else
                    Debug.Log("velocity " + __instance.GetVelocity());*/
            }
            
        }
    }

    /*[HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), "CustomFixedUpdate")]
    private static void Humanoid_CustomFixedUpdate_Postfix(Humanoid __instance, float fixedDeltaTime)
    {
        if (__instance && __instance.name.Contains(NPCPrefabName))
        {
            MonsterAI monsterAIcomponent = __instance.GetComponent<MonsterAI>();
            HumanoidNPC humanoidcomponent = __instance.GetComponent<HumanoidNPC>();

            if (monsterAIcomponent && monsterAIcomponent.m_follow && monsterAIcomponent.m_follow != Player.m_localPlayer.gameObject)
            {
                if (monsterAIcomponent.m_follow.transform.position.DistanceTo(__instance.transform.position) < instance.FollowUntilDistance + .5f && !humanoidcomponent.InAttack())
                {
                    if (monsterAIcomponent.m_follow.HasAnyComponent("Pickable"))
                    {
                        Pickable pick = monsterAIcomponent.m_follow.GetComponent<Pickable>();
                        pick.Interact(Player.m_localPlayer, false, false);
                    }
                    else
                    {
                        monsterAIcomponent.LookAt(monsterAIcomponent.m_follow.transform.position);
                        humanoidcomponent.StartAttack(humanoidcomponent, false);
                    }
                }
            }
        }
    }*/


    // OVERRIDE CheckRun: NPC can run when Player has over 5 stamina
    /*[HarmonyPostfix]
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
    }*/

    // OVERRIDE CheckRun: if this character can run? with stamina logic
    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "CheckRun")]
    public static bool Character_CheckRun_Prefix(Character __instance, Vector3 moveDir, float dt)
    {
        if (!__instance.name.Contains("HumanoidNPC")) return true;



        HumanoidNPC humanoidNPC_component = __instance.GetComponent<HumanoidNPC>();

        if (humanoidNPC_component)
        {
            if (Time.time - humanoidNPC_component.m_staminaLastBreakTime < humanoidNPC_component.StaminaExhaustedMinimumBreakTime)
            {
                return false;
            }
            else if (humanoidNPC_component.m_stamina < humanoidNPC_component.MinimumStaminaToRun)
            {
                humanoidNPC_component.m_staminaLastBreakTime = Time.time;
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
        bool run = num > RunDistance;
        if (instance.eNPCMode == NPCCommand.CommandType.FollowPlayer)
            run = run && !Player.m_localPlayer.IsCrouching() && !Player.m_localPlayer.m_walk;
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

    // OVERRIDE NPC OVERLAY HUD
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Character), "GetHoverText")]
    private static void Character_GetHoverText_Postfix(Character __instance, ref string __result)
    {
        if (__instance.name.Contains("HumanoidNPC"))
        {
            HumanoidNPC humanoidNPC_component = __instance.GetComponent<HumanoidNPC>();
            __result += "\n<color=orange><b>" + humanoidNPC_component.m_stamina.ToString("F2") + "</b></color>";
            __result += "\n<color=purple><b>" + instance.eNPCMode.ToString().ToUpper() + "</b></color>";
        }
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



    [HarmonyPrefix]
    [HarmonyPatch(typeof(Pickable), "Interact")]
    private static bool Pickable_Interact_Prefix(Pickable __instance, Humanoid character, bool repeat, bool alt, bool __result)
    {
        //Debug.Log("In Interact");
        if (__instance.m_nview == null)
        {
            __instance.m_nview = __instance.GetComponent<ZNetView>();
        }
        if (!__instance.m_nview.IsValid())// || __instance.m_enabled == 0)
        {
            //Debug.Log("!m_nview.IsValid()");
            __result = false;
            return false;
        }

        if (__instance.m_tarPreventsPicking)
        {
            //Debug.Log("m_tarPreventsPicking");
            if (__instance.m_floating == null)
            {
                __instance.m_floating = __instance.GetComponent<Floating>();
                //Debug.Log("m_floating == null");
            }

            if ((bool)__instance.m_floating && __instance.m_floating.IsInTar())
            {
                //Debug.Log("message");
                character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar");
                __result = __instance.m_useInteractAnimation;
                return false;
            }
        }

        //Debug.Log("calling RPC_Pick");
        //character.m_nview.InvokeRPC("RPC_Pick");
        __instance.RPC_Pick(0L);
        __result = __instance.m_useInteractAnimation;
        return false;
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

    private void SpawnCompanion()
    {
        GameObject[] npcs = FindPlayerNPCs();
        if (npcs.Length > 0)
        {
            Debug.Log("Spawning more than one NPC is disabled");
            return;
        }
        Player localPlayer = Player.m_localPlayer;
        GameObject npcPrefab = ZNetScene.instance.GetPrefab("HumanoidNPC");
        //GameObject npcPrefab = HumanoidNPCPrefab;

        if (npcPrefab == null)
        {
            Logger.LogError("ScriptNPC prefab not found!");
        }

        // spawn NPC
        Vector3 spawnPosition = localPlayer.transform.position + localPlayer.transform.forward * 2f;
        //Vector3 spawnPosition = GetRandomSpawnPosition(10f);
        Quaternion spawnRotation = localPlayer.transform.rotation;

        GameObject npcInstance = Instantiate<GameObject>(npcPrefab, spawnPosition, spawnRotation);
        npcInstance.SetActive(true);

        instance.PlayerNPC = npcInstance;

        // make the monster tame
        MonsterAI monsterAIcomp = npcInstance.GetComponent<MonsterAI>();

        SetMonsterAIAggravated(monsterAIcomp, false);
        monsterAIcomp.MakeTame();


        // add item to inventory
        ValheimAIModLoader.HumanoidNPC humanoidNpc_Component = npcInstance.GetComponent<ValheimAIModLoader.HumanoidNPC>();
        if (humanoidNpc_Component != null)
        {
            humanoidNpc_Component.m_name = "NPC";

            GameObject itemPrefab;

            // ADD DEFAULT SPAWN ITEMS TO NPC
            itemPrefab = ZNetScene.instance.GetPrefab("AxeBronze");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);

            itemPrefab = ZNetScene.instance.GetPrefab("ArmorBronzeChest");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);

            itemPrefab = ZNetScene.instance.GetPrefab("ArmorBronzeLegs");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);

            // COPY PROPERTIES FROM PLAYER
            humanoidNpc_Component.m_walkSpeed = localPlayer.m_walkSpeed;

            // COSMETICS
            humanoidNpc_Component.SetHair("Hair17");


            // SETUP HEALTH AND MAX HEALTH
            humanoidNpc_Component.SetMaxHealth(300);
            humanoidNpc_Component.SetHealth(300);

            // ADD CONTAINER TO NPC TO ENABLE PLAYER-NPC INVENTORY INTERACTION
            humanoidNpc_Component.inventoryContainer = npcInstance.AddComponent<Container>();
            humanoidNpc_Component.inventoryContainer.m_inventory = humanoidNpc_Component.m_inventory;
        }
        else
        {
            Logger.LogError("humanoidNpc_Component component not found on the instantiated ScriptNPC prefab!");
        }


    }

    private void StartPatrol(Player player)
    {
        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidNPC_component = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();

            patrol_position = player.transform.position;
            instance.eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.PatrolArea;
            instance.patrol_harvest = true;

            //Vector3 randLocation = GetRandomReachableLocationInRadius(humanoidNPC_component.patrol_position, patrol_radius);

            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(null);
        }
    }
    
    private void StartFollowing(Player player)
    {
        instance.eNPCMode = NPCCommand.CommandType.FollowPlayer;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidnpc_component = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();
            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(player.gameObject);
            Debug.Log("Everyone now following player!");

            AddChatTalk(humanoidnpc_component, "NPC", "Coming!");

            /*string text = "Coming!";
            UserInfo userInfo = new UserInfo();
            userInfo.Name = "NPC";

            Vector3 headPoint = humanoidnpc_component.GetEyePoint();
            //Chat.instance.AddInworldText(npc, 0, headPoint, Talker.Type.Shout, userInfo, text);
            Chat.instance.AddInworldText(npc, 0, headPoint, Talker.Type.Normal, userInfo, text);
            Chat.instance.AddString("NPC", text, Talker.Type.Normal);*/
            //humanoidnpc_component.m_zanim.SetTrigger("Talk");
        }
    }

    private void StartHarvesting(Player player)
    {
        instance.eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.HarvestResource;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            GameObject ClosestTree = FindClosestTreeFor(npc);

            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(ClosestTree);

            Debug.Log("Everyone harvesting!");

            //TODO: AVOID MULTIPLE NPCS GOING TO CHOP THE SAME TREE
            //TODO: LOOP FUNCTION TO KEEP HARVESTING RESOURCES UNTIL A CONDITION IS MET
        }
    }

    private void StartAttacking(Player player)
    {
        instance.eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.AttackTarget;

        GameObject[] allEnemies = FindEnemies();
        /*foreach (GameObject npc in allEnemies)
        {
            Debug.Log(npc.name);
        }*/ 

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();

            // disregard nearby enemies
            monsterAIcomponent.SetFollowTarget(null);
            monsterAIcomponent.m_viewRange = 80f;
            monsterAIcomponent.m_alerted = false;
            monsterAIcomponent.m_aggravatable = true;
            monsterAIcomponent.SetHuntPlayer(true);

            Debug.Log("Everyone attacking!");
        }
    }

    float lastSentToBrainTime = 0f;
    private void SendToBrain()
    {
        if (instance.IsRecording)
        {
            instance.StopRecording();
        }

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            HumanoidNPC humanoidComponent = npc.GetComponent<HumanoidNPC>();

            Debug.Log("SendUpdateToBrain");
            SendUpdateToBrain(npc);
            instance.lastSentToBrainTime = Time.time;

            AddChatTalk(humanoidComponent, "NPC", "...");

            /*UserInfo userInfo = new UserInfo();
            userInfo.Name = "NPC";
            Vector3 headPoint = humanoidComponent.GetEyePoint();
            Chat.instance.AddInworldText(npc, 0, headPoint, Talker.Type.Shout, userInfo, "...");*/
        }
    }

    private void OnInventoryKeyPressed(Player player)
    {
        if (instance.PlayerNPC)
        {
            HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
            InventoryGui.instance.Show(humanoidNPC_component.inventoryContainer);
        }

        /*Debug.Log(craftingRequirements.Count());

        foreach (KeyValuePair<string, Piece.Requirement[]> s in craftingRequirements.ToArray())
        {
            Debug.Log(s.Key);
            Debug.Log("\n\n" + s.Key + " requires ");
            foreach (Piece.Requirement x in s.Value)
            {
                Debug.Log(x.m_amount + "x " + x.m_resItem.name);
            }
        }*/

        /*Debug.Log("OnInventoryKeyPressed");

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            ValheimAIModLoader.HumanoidNPC humanoidComponent = npc.GetComponent<ValheimAIModLoader.HumanoidNPC>();
            if (monsterAIcomponent != null && humanoidComponent != null)
            {
                *//*GameObject itemPrefab = ZNetScene.instance.GetPrefab("Bread");
                humanoidComponent.GetInventory().AddItem(itemPrefab.gameObject, 15);*//*

                //PrintInventoryItems(humanoidComponent.m_inventory);


                DropAllItems(humanoidComponent);

                *//*GameObject[] pickable_stones = GameObject.FindObjectsOfType<GameObject>(true)
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
        }*//*

        //humanoidComponent.m_zanim.SetTrigger("eat");

        //Debug.Log(GetJSONForBrain(npc));


    }
}*/
    }

    private void DropAllItems(HumanoidNPC humanoidNPC)
    {
        List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
        int num = 1;
        foreach (ItemDrop.ItemData item in allItems)
        {
            Debug.Log("Dropping " + item.m_shared.m_name);
            //Vector3 position = humanoidNPC.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
            Vector3 position = humanoidNPC.transform.position + Vector3.up * 2f + UnityEngine.Random.insideUnitSphere * 0.3f + humanoidNPC.transform.forward * 2.5f;
            Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
            ItemDrop.DropItem(item, item.m_stack, position, rotation);
            num++;
        }
        humanoidNPC.m_inventory.RemoveAll();
    }


   

    private void AddChatTalk(Character character, string name, string text)
    {
        UserInfo userInfo = new UserInfo();
        userInfo.Name = name;
        Vector3 headPoint = character.GetEyePoint() + (Vector3.up * -100f);
        Chat.instance.AddInworldText(character.gameObject, 0, headPoint, Talker.Type.Shout, userInfo, text);
        Chat.instance.AddString("NPC", text, Talker.Type.Normal);
    }

    private void SendUpdateToBrain(GameObject npc)
    {
        string jsonData = GetJSONForBrain(npc);

        //Debug.Log("jsonData\n " + jsonData);

        // Create a new WebClient
        WebClient webClient = new WebClient();
        webClient.Headers.Add("Content-Type", "application/json");

        // Send the POST request
        webClient.UploadStringAsync(new System.Uri($"{brainBaseURL}/instruct_agent"), jsonData);
        webClient.UploadStringCompleted += OnSendUpdateToBrainCompleted;
    }

    private void OnSendUpdateToBrainCompleted(object sender, UploadStringCompletedEventArgs e)
    {
        if (e.Error == null)
        {
            string responseJson = e.Result;
            Debug.Log("Response from /instruct_agent: " + responseJson);

            // Parse the response JSON using SimpleJSON's DeserializeObject
            JsonObject responseObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(responseJson);
            string audioFileId = responseObject["agent_text_response_audio_file_id"].ToString();
            string agent_text_response = responseObject["agent_text_response"].ToString();
            string player_instruction_transcription = responseObject["player_instruction_transcription"].ToString();

            // Get the agent_commands array
            JsonArray agentCommands = responseObject["agent_commands"] as JsonArray;

            // Check if agent_commands array exists and has at least one element
            if (agentCommands != null && agentCommands.Count > 0)
            {
                // Get the first command object from the array
                JsonObject commandObject = agentCommands[agentCommands.Count - 1] as JsonObject;

                // Get the action_str value from the command object
                string actionStr = commandObject["action_str"].ToString();
                Debug.Log("Action String: " + actionStr);
                ProcessNPCCommand(actionStr);
            }
            else
            {
                Debug.Log("No agent commands found.");
            }

            Chat.instance.AddString(Player.m_localPlayer.GetPlayerName(), player_instruction_transcription, Talker.Type.Normal);

            GameObject[] npcs = FindPlayerNPCs();
            if (npcs.Length > 0 && npcs[0])
            {
                HumanoidNPC humanoidnpc_component = npcs[0].GetComponent<HumanoidNPC>();

                AddChatTalk(humanoidnpc_component, "NPC", agent_text_response);

                /*UserInfo userInfo = new UserInfo();
                userInfo.Name = "NPC";
                Vector3 headPoint = humanoidnpc_component.GetEyePoint() + Vector3.up * 15f;
                Chat.instance.AddInworldText(npcs[0], 0, headPoint, Talker.Type.Normal, userInfo, agent_text_response);*/
            }

            // Download the audio file asynchronously
            DownloadAudioFile(audioFileId);
        }
        else
        {
            Debug.LogError("Request failed: " + e.Error.Message);
        }
    }

    private void DownloadAudioFile(string audioFileId)
    {
        // Create a new WebClient for downloading the audio file
        WebClient webClient = new WebClient();

        // Download the audio file asynchronously
        webClient.DownloadDataAsync(new System.Uri($"{brainBaseURL}/get_audio_file?audio_file_id={audioFileId}"));
        webClient.DownloadDataCompleted += OnAudioFileDownloaded;
    }

    private void OnAudioFileDownloaded(object sender, DownloadDataCompletedEventArgs e)
    {
        if (e.Error == null)
        {
            // Save the audio file to disk
            System.IO.File.WriteAllBytes(npcDialogueRawAudioPath, e.Result);
            //Debug.Log("Audio file downloaded to: " + npcDialogueRawAudioPath);

            if (instance.lastSentToBrainTime > 0)
                Debug.Log("Brain response time: " + (Time.time - instance.lastSentToBrainTime));

            PlayWavFile(npcDialogueRawAudioPath);
            
        }
        else if (e.Error is WebException webException && webException.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)webException.Response).StatusCode == HttpStatusCode.NotFound)
        {
            Debug.LogError("Audio file does not exist.");
        }
        else
        {
            Debug.LogError("Download failed: " + e.Error.Message);
        }
    }

    private void ProcessNPCCommand(string BrainResponseCommand)
    {
        Player localPlayer = Player.m_localPlayer;
        if (BrainResponseCommand == "StartFollowingPlayer")
        {
            instance.StartFollowing(localPlayer);
        }
        else if (BrainResponseCommand == "StartAttacking")
        {
            instance.StartAttacking(localPlayer);
        }
        else if (BrainResponseCommand == "StartHarvesting")
        {
            instance.StartHarvesting(localPlayer);
        }
        else if (BrainResponseCommand == "StartPatrolling")
        {
            instance.StartPatrol(localPlayer);
        }
    }

    

    

    


    /*
     * 
     * 
     * FIND 
     * 
     * 
     */

    
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
        if (instance.AllPlayerNPCInstances.Length > 0)
        {
            instance.PlayerNPC = instance.AllPlayerNPCInstances[0];
        }
        return instance.AllPlayerNPCInstances;
    }

    private static void RefreshPickables()
    {
        GameObject[] pickables = GameObject.FindObjectsOfType<GameObject>(false)
                //.Where(go => go != null && !ZoneSystem.instance.IsBlocked(go.transform.position) && (go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")))
                .Where(go => go != null  && (go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")))
                .ToArray();
        instance.AllPickableInstances.Clear();
        foreach (GameObject pickable in pickables)
        {
            instance.AllPickableInstances.Add(pickable);
        }
        instance.AllPickableInstancesLastRefresh = Time.time;
        //Debug.Log("pickables len " + instance.AllPickableInstances.Count());
    }

    private static GameObject FindClosestPickableResource(GameObject character, Vector3 p_position, float radius)
    {
        if (!(instance.AllPickableInstances.Count > 0 && Time.time - instance.AllPickableInstancesLastRefresh < 30f && instance.AllPickableInstancesLastRefresh != 0f))
        {
            //Debug.Log("Updated AllPickableInstances");
            RefreshPickables();
        }
            
        IOrderedEnumerable<GameObject> results = instance.AllPickableInstances.ToArray()
            .Where(t => t != null && Vector3.Distance(p_position, t.transform.position) <= radius)
            .OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position));
        //Debug.Log("result2 " + results.Count());
        if (results != null && results.Count() > 0)
        {
            try
            {
                int i = 0;
                bool found = false;
                GameObject result = null;

                while (!found)
                {
                    if (i >= results.Count()) return null;

                    result = results.ElementAt(i);
                    if (result != null)
                    {
                        if (result.transform.position.DistanceTo(p_position) < radius)
                        {
                            found = true;
                        }
                        else
                        {
                            Debug.Log("DistanceTo(p_position) < radius");
                            if (result.transform.position.DistanceTo(character.transform.position) > radius)
                                return null;
                            i++;
                        }
                    }
                    else
                    {
                        Debug.Log("result == null");
                        return null;
                    }
                }

                return result;
                    
            }
            catch (NullReferenceException ex)
            {
                Debug.Log($"An error occurred: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("No more pickable items within " + radius + " units  from patrol position");
        }
              
        return null;
    }

    private static GameObject FindClosestResource(GameObject character, string ResourceName)
    {
        return GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.StartsWith(ResourceName))
                .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .FirstOrDefault();
    }

    private GameObject FindClosestTreeFor(GameObject go, string TreeType = "small")
    {
        if (TreeType == "small")
            return FindSmallTrees()//.Where(t => t.gameObject.name.StartsWith("Beech_small"))// || t.gameObject.name.StartsWith("Pine"))
                .OrderBy(t => Vector3.Distance(go.transform.position, t.transform.position))
                .FirstOrDefault();
        return null;
    }

    private static GameObject[] FindSmallTrees()
    {
        instance.SmallTrees = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.StartsWith("Beech_small"))
                //.Where(go => go.name.StartsWith("Beech_small") || go.name.StartsWith("Beech"))
                //.Where(go => go.HasAnyComponent("TreeBase") || go.HasAnyComponent("Destructible"))
                .ToArray();
        return instance.SmallTrees;
    }


    /*
     * 
     * 
     * AUDIO
     * 
     * 
     */

    private int recordingLength = 6; // Maximum recording length in seconds
    private int sampleRate = 22050; // Reduced from 44100
    private int bitDepth = 8; // Reduced from 16

    private void GetRecordingDevices()
    {
        string[] microphoneDevices = Microphone.devices;

        foreach (string deviceName in microphoneDevices)
        {
            Debug.Log("Microphone device: " + deviceName);
        }
    }

    private string GetRecordingDevice()
    {
        if (Microphone.devices.Length > MicrophoneIndex.Value)
        {
            return Microphone.devices[MicrophoneIndex.Value];
        }

        return null;
    }

    private void StartRecording()
    {
        instance.recordedAudioClip = Microphone.Start(GetRecordingDevice(), false, recordingLength, sampleRate);
        instance.IsRecording = true;
        AddChatTalk(Player.m_localPlayer, Player.m_localPlayer.GetPlayerName(), "...");
        //Debug.Log("Recording started");
    }

    private void StopRecording()
    {
        // Stop the audio recording
        Microphone.End(null);
        instance.IsRecording = false;
        //Debug.Log("Recording stopped");

        TrimSilence();

        SaveRecording();
    }

    private void TrimSilence()
    {
        float[] samples = new float[recordedAudioClip.samples];
        recordedAudioClip.GetData(samples, 0);

        int lastNonZeroIndex = samples.Length - 1;
        while (lastNonZeroIndex > 0 && samples[lastNonZeroIndex] == 0)
        {
            lastNonZeroIndex--;
        }

        Array.Resize(ref samples, lastNonZeroIndex + 1);

        AudioClip trimmedClip = AudioClip.Create("TrimmedRecording", samples.Length, recordedAudioClip.channels, 44100, false);
        trimmedClip.SetData(samples, 0);

        recordedAudioClip = trimmedClip;
    }

    private byte[] EncodeToWav(AudioClip clip)
    {
        float[] samples = new float[clip.samples];
        clip.GetData(samples, 0);

        using (MemoryStream stream = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // RIFF header
                writer.Write("RIFF".ToCharArray());
                writer.Write(36 + samples.Length * (bitDepth / 8));
                writer.Write("WAVE".ToCharArray());

                // Format chunk
                writer.Write("fmt ".ToCharArray());
                writer.Write(16);
                writer.Write((ushort)1); // Audio format (1 = PCM)
                writer.Write((ushort)clip.channels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * clip.channels * (bitDepth / 8)); // Byte rate
                writer.Write((ushort)(clip.channels * (bitDepth / 8))); // Block align
                writer.Write((ushort)bitDepth); // Bits per sample

                // Data chunk
                writer.Write("data".ToCharArray());
                writer.Write(samples.Length * (bitDepth / 8));

                // Convert float samples to 8-bit PCM
                if (bitDepth == 8)
                {
                    foreach (float sample in samples)
                    {
                        writer.Write((byte)((sample + 1f) * 127.5f));
                    }
                }
                else // 16-bit PCM
                {
                    foreach (float sample in samples)
                    {
                        writer.Write((short)(sample * 32767));
                    }
                }
            }
            return stream.ToArray();
        }
    }

    private void SaveRecording()
    {
        byte[] wavData = EncodeToWav(recordedAudioClip);

        try
        {
            File.WriteAllBytes(playerDialogueAudioPath, wavData);
            //Debug.Log("Recording saved to: " + playerDialogueAudioPath);
        }
        catch (Exception e)
        {
            Debug.LogError("Error saving recording: " + e.Message);
        }
    }

    private AudioClip LoadAudioClip(string audioPath)
    {
        AudioClip loadedClip;

        if (File.Exists(audioPath))
        {
            byte[] audioData = File.ReadAllBytes(audioPath);

            // Read the WAV file header to determine the audio format
            int channels = BitConverter.ToInt16(audioData, 22);
            int frequency = BitConverter.ToInt32(audioData, 24);

            // Convert the audio data to float[] format
            int headerSize = 44; // WAV header size is typically 44 bytes
            int dataSize = audioData.Length - headerSize;
            float[] floatData = new float[dataSize / 4];
            for (int i = 0; i < floatData.Length; i++)
            {
                floatData[i] = BitConverter.ToSingle(audioData, i * 4 + headerSize);
            }

            bool stream = false;
            loadedClip = AudioClip.Create("AudioClipName", floatData.Length / channels, channels, frequency, stream);
            loadedClip.SetData(floatData, 0);
            //Debug.Log("AudioClip loaded successfully.");
            return loadedClip;
        }
        else
        {
            Debug.LogError("Audio file not found: " + audioPath);
            return null;
        }
    }

    private void PlayRecordedAudio(string fileName)
    {
        /*string audioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");
        AudioClip audioClip = LoadAudioClip(audioPath);*/
        AudioClip recordedClip = LoadAudioClip(playerDialogueAudioPath);
        AudioClip downloadedClip = LoadAudioClip(npcDialogueAudioPath);

        if (recordedClip && downloadedClip)
            CompareAudioFormats(recordedClip, downloadedClip);

        if (recordedClip != null)
            AudioSource.PlayClipAtPoint(recordedClip, Player.m_localPlayer.transform.position, 1f);

        Debug.Log("Playing last recorded clip audio");
    }

    public void MyPlayAudio(AudioClip clip)
    {
        GameObject gameObject = new GameObject("One shot audio");
        gameObject.transform.position = Player.m_localPlayer.transform.position;
        AudioSource audioSource = (AudioSource)gameObject.AddComponent(typeof(AudioSource));
        audioSource.clip = clip;
        audioSource.spatialBlend = 0f;
        audioSource.volume = instance.CompanionVolume.Value;
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
        audioSource.Play();
        UnityEngine.Object.Destroy(gameObject, clip.length * ((Time.timeScale < 0.01f) ? 0.01f : Time.timeScale));
    }

    public void PlayWavFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return;
        }

        try
        {
            byte[] wavData = File.ReadAllBytes(filePath);
            AudioClip clip = WavToAudioClip(wavData, Path.GetFileNameWithoutExtension(filePath));

            MyPlayAudio(clip);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error playing WAV file: {e.Message}");
        }
    }

    private AudioClip WavToAudioClip(byte[] wavData, string clipName)
    {
        // Parse WAV header
        int channels = BitConverter.ToInt16(wavData, 22);
        int sampleRate = BitConverter.ToInt32(wavData, 24);
        int bitsPerSample = BitConverter.ToInt16(wavData, 34);

        //Debug.Log($"Channels: {channels}, Sample Rate: {sampleRate}, Bits per Sample: {bitsPerSample}");

        // Find data chunk
        int dataChunkStart = 12; // Start searching after "RIFF" + size + "WAVE"
        while (!(wavData[dataChunkStart] == 'd' && wavData[dataChunkStart + 1] == 'a' && wavData[dataChunkStart + 2] == 't' && wavData[dataChunkStart + 3] == 'a'))
        {
            dataChunkStart += 4;
            int chunkSize = BitConverter.ToInt32(wavData, dataChunkStart);
            dataChunkStart += 4 + chunkSize;
        }
        int dataStart = dataChunkStart + 8;

        // Extract audio data
        float[] audioData = new float[(wavData.Length - dataStart) / (bitsPerSample / 8)];

        for (int i = 0; i < audioData.Length; i++)
        {
            if (bitsPerSample == 16)
            {
                short sample = BitConverter.ToInt16(wavData, dataStart + i * 2);
                audioData[i] = sample / 32768f;
            }
            else if (bitsPerSample == 8)
            {
                audioData[i] = (wavData[dataStart + i] - 128) / 128f;
            }
        }

        AudioClip audioClip = AudioClip.Create(clipName, audioData.Length / channels, channels, sampleRate, false);
        audioClip.SetData(audioData, 0);

        return audioClip;
    }

    private void CompareAudioFormats(AudioClip firstClip, AudioClip secondClip)
    {
        // Check the audio format of the recorded clip
        Debug.Log("First Clip:");
        Debug.Log("Channels: " + firstClip.channels);
        Debug.Log("Frequency: " + firstClip.frequency);
        Debug.Log("Samples: " + firstClip.samples);
        Debug.Log("Length: " + firstClip.length);

        // Check the audio format of the loaded clip
        Debug.Log("Second Clip:");
        Debug.Log("Channels: " + secondClip.channels);
        Debug.Log("Frequency: " + secondClip.frequency);
        Debug.Log("Samples: " + secondClip.samples);
        Debug.Log("Length: " + secondClip.length);
    }

    private string GetBase64FileData(string audioPath)
    {
        if (File.Exists(audioPath))
        {
            byte[] audioData = File.ReadAllBytes(audioPath);
            string base64AudioData = Convert.ToBase64String(audioData);

            return base64AudioData;
        }
        else
        {
            Debug.LogError("Audio file not found: " + audioPath);
            return null;
        }
    }


    /*
     * 
     * 
     * OTHER
     * 
     * 
     * 
     */


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

    private Vector3 GetRandomSpawnPosition(float radius)
    {
        //Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
        Vector3 randomDirection = Player.m_localPlayer.transform.position + (Vector3.up * 20f) + (Vector3.forward * 20f);

        RaycastHit hit;
        if (Physics.Raycast(randomDirection, Vector3.down, out hit, 1000f, LayerMask.GetMask("Default", "static_solid", "Default_small", "Terrain")))
        {
            return hit.point;
        }

        //return Player.m_localPlayer.transform.position;
        return randomDirection;
    }

    public static string GetJSONForBrain(GameObject character)
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

            ["currentTime"] = EnvMan.instance.GetDayFraction(),
            ["currentWeather"] = EnvMan.instance.GetCurrentEnvironment().m_name,
            ["currentBiome"] = Heightmap.FindBiome(character.transform.position).ToString(),

            //["nearbyVegetationCount"] = instance.DetectVegetation(),
    };

        //string base64audio = instance.GetBase64AudioData(instance.recordedAudioClip);
        string base64audio = instance.GetBase64FileData(instance.playerDialogueAudioPath);

        var jsonObject = new JsonObject
        {
            ["player_id"] = humanoidNPC.GetZDOID().ToString(),
            ["game_state"] = gameState,
            ["player_instruction_audio_file_base64"] = base64audio,
        };

        Debug.Log(gameState);

        string jsonString = SimpleJson.SimpleJson.SerializeObject(jsonObject);

        return jsonString;
    }



    /*
     * 
     * 
     * MISC
     * 
     * 
     */

    public float detectionRadius = 30f;
    public LayerMask terrainLayer;
    public LayerMask vegetationLayer;
    public int DetectVegetation()
    {
        // Check terrain type
        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, 10f, terrainLayer))
        {
            TerrainData terrainData = Terrain.activeTerrain.terrainData;
            float height = hit.point.y;
            // You can use the height to determine if it's lowland, mountain, etc.
        }

        // Check vegetation density
        Collider[] vegetation = Physics.OverlapSphere(transform.position, detectionRadius, vegetationLayer);
        return vegetation.Length;
        // Use vegetationCount to determine if it's densely forested, sparse, or barren
    }

    private void PopulateCraftingRequirements()
    {
        var jsonObject = new JsonObject();
        foreach (GameObject prefab in ObjectDB.instance.m_items)
        {
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                var thisJsonObject = new JsonObject();

                thisJsonObject["name"] = itemDrop.name;
                thisJsonObject["itemName"] = itemDrop.m_itemData.m_shared.m_name;

                JsonObject itemDropCustomData = new JsonObject();
                foreach (var s in itemDrop.m_itemData.m_customData)
                {
                    itemDropCustomData[s.Key] = s.Value;
                }
                if (itemDropCustomData.Count > 0)
                    thisJsonObject["customData"] = itemDropCustomData;

                if (itemDrop.m_itemData.m_shared.m_description != "")
                {
                    string description = LocalizationManager.Instance.TryTranslate(itemDrop.m_itemData.m_shared.m_description);

                    // If the description is the same as the key, it means no translation was found
                    if (description != "")
                    {
                        thisJsonObject["description"] = description;
                    }
                }


                thisJsonObject["armor"] = itemDrop.m_itemData.m_shared.m_armor;
                thisJsonObject["maxDurability"] = itemDrop.m_itemData.m_shared.m_maxDurability;
                thisJsonObject["weight"] = itemDrop.m_itemData.m_shared.m_weight;

                Recipe recipe = ObjectDB.instance.GetRecipe(itemDrop.m_itemData);
                if (recipe != null)
                {
                    craftingRequirements[itemDrop.m_itemData.m_shared.m_name] = recipe.m_resources;
                    JsonArray requirementsArray = new JsonArray();
                    foreach (var req in recipe.m_resources)
                    {
                        JsonObject reqObject = new JsonObject();

                        reqObject["name"] = req.m_resItem.name;
                        reqObject["itemName"] = req.m_resItem.m_itemData.m_shared.m_name;

                        if (req.m_resItem.m_itemData.m_shared.m_description != "")
                        {
                            string description = LocalizationManager.Instance.TryTranslate(req.m_resItem.m_itemData.m_shared.m_description);

                            // If the description is the same as the key, it means no translation was found
                            if (description != "")
                            {
                                reqObject["description"] = description;
                            }
                        }

                        reqObject["amount"] = req.m_amount;
                        /*reqObject["amountPerLevel"] = req.m_amountPerLevel;
                        reqObject["m_recover"] = req.m_recover;
                        reqObject["m_extraAmountOnlyOneIngredient"] = req.m_extraAmountOnlyOneIngredient;*/

                        requirementsArray.Add(reqObject);
                    }
                    thisJsonObject["m_resources"] = requirementsArray;
                }

                jsonObject[itemDrop.m_itemData.m_shared.m_name] = thisJsonObject;
            }
        }

        string json = jsonObject.ToString();

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "crafting_requirements.json");

        File.WriteAllText(filePath, json);
        Debug.Log($"Crafting requirements exported to {filePath}");
    }

    private void PopulateBuildingRequirements()
    {
        var jsonObject = new JsonObject();
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            Piece piece = prefab.GetComponent<Piece>();
            if (piece != null)
            {
                string pieceName = piece.m_name;
                buildingRequirements[pieceName] = piece.m_resources;

                JsonObject thisJsonObject = new JsonObject();
                thisJsonObject["name"] = piece.name;
                thisJsonObject["itemName"] = piece.m_name;

                if (piece.m_description != "")
                {
                    string description = LocalizationManager.Instance.TryTranslate(piece.m_description);

                    // If the description is the same as the key, it means no translation was found
                    if (description != "")
                    {
                        thisJsonObject["description"] = description;
                    }
                }

                thisJsonObject["category"] = piece.m_category.ToString();
                thisJsonObject["comfort"] = piece.m_comfort;
                thisJsonObject["groundPiece"] = piece.m_groundPiece;
                thisJsonObject["allowedInDungeons"] = piece.m_allowedInDungeons;
                thisJsonObject["spaceRequirement"] = piece.m_spaceRequirement;

                JsonArray requirementsArray = new JsonArray();
                foreach (var req in piece.m_resources)
                {
                    JsonObject reqObject = new JsonObject();
                    reqObject["name"] = req.m_resItem.name;
                    reqObject["itemName"] = req.m_resItem.m_itemData.m_shared.m_name;

                    if (req.m_resItem.m_itemData.m_shared.m_description != "")
                    {
                        string description = LocalizationManager.Instance.TryTranslate(req.m_resItem.m_itemData.m_shared.m_description);

                        // If the description is the same as the key, it means no translation was found
                        if (description != "")
                        {
                            reqObject["description"] = description;
                        }
                    }

                    reqObject["amount"] = req.m_amount;
                    reqObject["amountPerLevel"] = req.m_amountPerLevel;
                    reqObject["m_recover"] = req.m_recover;
                    reqObject["m_extraAmountOnlyOneIngredient"] = req.m_extraAmountOnlyOneIngredient;

                    requirementsArray.Add(reqObject);
                }
                thisJsonObject["m_resources"] = requirementsArray;
                jsonObject[pieceName] = thisJsonObject;
            }
        }

        string json = jsonObject.ToString();

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "building_requirements.json");

        File.WriteAllText(filePath, json);
        Debug.Log($"Building requirements exported to {filePath}");
    }

    /*private void PopulateResourceLocations()
    {
        GameObject prefab = ZNetScene.instance.GetPrefab("Resin");
        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
        if (itemDrop != null)
        {
            ZoneSystem.ZoneVegetation z = ZoneSystem.instance.m_vegetation.First();

            foreach (ZoneSystem.ZoneVegetation zoneVegetation in ZoneSystem.instance.m_vegetation)
            {
                if (zoneVegetation.m_prefab == prefab)
                {
                    string biomeName = ZoneSystem.instance.GetZone(zoneVegetation.m_biome).m_name;
                    resourceLocations[itemName].Add(biomeName);
                }
            }
        }
    }*/

    public Piece.Requirement[] GetCraftingRequirements(string itemName)
    {
        if (craftingRequirements.ContainsKey(itemName))
        {
            return craftingRequirements[itemName];
        }
        return null;
    }

    // Disable auto save
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), "UpdateSaving")]
    private static bool Game_UpdateSaving_Prefix()
    {
        return !instance.DisableAutoSave.Value;
    }


    /*
     * 
     * 
     * DEBUG FUNCTIONS
     * 
     * 
     */

    private static void PrintInventoryItems(Inventory inventory)
    {
        Debug.Log("Character Inventory Items:");

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        foreach (ItemDrop.ItemData item in items)
        {
            Debug.Log($"- {item.m_shared.m_name} (Quantity: {item.m_stack})");
        }
    }
}