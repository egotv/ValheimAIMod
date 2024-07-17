using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using SimpleJson;



using UnityEngine;
using ValheimAIModLoader;
using System.IO;
using System.Net;
using Jotunn.Managers;
using UnityEngine.UI;
using UnityEngine.InputSystem.Utilities;

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

    /*private ConfigEntry<KeyboardShortcut> spawnCompanionKey;
    private ConfigEntry<KeyboardShortcut> TogglePatrolKey;
    private ConfigEntry<KeyboardShortcut> ToggleFollowKey;
    private ConfigEntry<KeyboardShortcut> ToggleHarvestKey;
    private ConfigEntry<KeyboardShortcut> ToggleAttackKey;
    private ConfigEntry<KeyboardShortcut> InventoryKey;
    private ConfigEntry<KeyboardShortcut> TalkKey;
    private ConfigEntry<KeyboardShortcut> SendToBrainKey;

    private ConfigEntry<int> MicrophoneIndex;
    private ConfigEntry<float> CompanionVolume;*/
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


    private List<GameObject> AllPickableInstances = new List<GameObject>();
    private float AllPickableInstancesLastRefresh = 0f;

    private GameObject[] SmallTrees;

    // NPC VARS
    private NPCCommand.CommandType eNPCMode;

    private float FollowUntilDistance = .5f;
    private float RunUntilDistance = 3f;
    public Vector3 patrol_position = Vector3.zero;
    public float patrol_radius = 10f;
    public bool patrol_harvest = false;
    public string CurrentHarvestResourceName = "Beech";
    public bool MovementLock = false;
    public float chaseUntilPatrolRadiusDistance = 20f;

    public bool PlayingAnim = false;

    private AudioClip recordedAudioClip;
    public bool IsRecording = false;
    private float recordingStartedTime = 0f;
    public bool IsModMenuShowing = false;


    private void Awake()
    {
        Debug.Log("ValheimAIModLivePatch Loaded!");
        instance = this;

        ConfigBindings();

        /*PopulateCraftingRequirements();
        PopulateBuildingRequirements();*/
        PopulateMonsterPrefabs();
        PopulateAllItems();

        FindPlayerNPCs();

        playerDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "playerdialogue.wav");
        npcDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue.wav");
        npcDialogueRawAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");

        Chat.instance.SendText(Talker.Type.Normal, "EGO.AI MOD LOADED!");

        GetRecordingDevices();

        //MinimapManager.OnVanillaMapAvailable += CreateMapOverlay;

        /*if (!ZInput.instance.m_buttons.ContainsKey("FollowPlayer"))
        {
            ZInput.instance.AddButton("FollowPlayer", Key.F);
        }*/

        harmony.PatchAll(typeof(ValheimAIModLivePatch));
    }

    private void CreateMapOverlay()
    {
        // Get or create a map overlay instance by name
        var zoneOverlay = MinimapManager.Instance.GetMapOverlay("ZoneOverlay");

        // Create a Color array with space for every pixel of the map
        int mapSize = zoneOverlay.TextureSize * zoneOverlay.TextureSize;
        Color[] mainPixels = new Color[mapSize];

        // Iterate over the dimensions of the overlay and set a color for
        // every pixel in our mainPixels array wherever a zone boundary is
        Color color = Color.white;
        int zoneSize = 64;
        int index = 0;
        for (int x = 0; x < zoneOverlay.TextureSize; ++x)
        {
            for (int y = 0; y < zoneOverlay.TextureSize; ++y, ++index)
            {
                if (x % zoneSize == 0 || y % zoneSize == 0)
                {
                    mainPixels[index] = color;
                }
            }
        }

        // Set the pixel array on the overlay texture
        // This is much faster than setting every pixel individually
        zoneOverlay.OverlayTex.SetPixels(mainPixels);

        // Apply the changes to the overlay
        // This also triggers the MinimapManager to display this overlay
        zoneOverlay.OverlayTex.Apply();
    }

    private void ConfigBindings()
    {
        /*spawnCompanionKey = Config.Bind<KeyboardShortcut>("Keybinds", "SpawnCompanionKey", new KeyboardShortcut(KeyCode.G), "The key used to spawn an NPC.");
        TogglePatrolKey = Config.Bind<KeyboardShortcut>("Keybinds", "TogglePatrolKey", new KeyboardShortcut(KeyCode.P), "The key used to command all NPCs to patrol the area the player is at.");
        ToggleFollowKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleFollowKey", new KeyboardShortcut(KeyCode.F), "The key used to command all NPCs to follow you.");
        ToggleHarvestKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleHarvestKey", new KeyboardShortcut(KeyCode.H), "The key used to command all NPCs to go harvest.");
        ToggleAttackKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleAttackKey", new KeyboardShortcut(KeyCode.K), "The key used to command all NPCs to attack enemies.");
        InventoryKey = Config.Bind<KeyboardShortcut>("Keybinds", "InventoryKey", new KeyboardShortcut(KeyCode.U), "The key used to command all NPCs to -");
        TalkKey = Config.Bind<KeyboardShortcut>("Keybinds", "TalkKey", new KeyboardShortcut(KeyCode.T), "The key used to talk into the game");
        SendToBrainKey = Config.Bind<KeyboardShortcut>("Keybinds", "SendToBrainKey", new KeyboardShortcut(KeyCode.Y), "The key used to ");

        MicrophoneIndex = Config.Bind<int>("Integer", "MicrophoneIndex", 0, "Input device index in Windows Sound Settings.");
        CompanionVolume = Config.Bind<float>("Float", "CompanionVolume", 1f, "NPC dialogue volume (0-1)");*/
        DisableAutoSave = Config.Bind<bool>("Bool", "DisableAutoSave", false, "Disable auto saving the game world?");
    }

    private void OnDestroy() 
    {
        TestPanel.SetActive(false);
        Destroy(TestPanel);
        Destroy(instance);

        harmony.UnpatchSelf();
    }

    // PROCESS PLAYER INPUT
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), "Update")]
    private static void Player_Update_Postfix(Player __instance)
    {
        if (!ZNetScene.instance || !Player.m_localPlayer)
        {
            // Player is not in a world, allow input
            return;
        }

        if (Menu.IsVisible() || Console.IsVisible() || Chat.instance.HasFocus() || instance.IsModMenuShowing)
        {
            //Debug.Log("Menu visible");
            return;
        }


        if (ZInput.GetKeyDown(KeyCode.G))
        {
            instance.SpawnCompanion();
            instance.StartFollowing(__instance);
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.F))
        {
            if (instance.eNPCMode == NPCCommand.CommandType.Idle || instance.eNPCMode == NPCCommand.CommandType.PatrolArea)
            {
                instance.Follow_Start(__instance.gameObject);
            }
            else if (instance.eNPCMode == NPCCommand.CommandType.FollowPlayer)
            {
                instance.Patrol_Start();
            }
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.H))
        {
            if (instance.eNPCMode == NPCCommand.CommandType.HarvestResource)
                instance.Harvesting_Stop();
            else
                instance.Harvesting_Start("");
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.K))
        {
            if (instance.eNPCMode == NPCCommand.CommandType.CombatSneakAttack || instance.eNPCMode == NPCCommand.CommandType.CombatAttack)
                instance.Combat_StopAttacking();
            else
                instance.Combat_StartAttacking(null);
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.I))
        {
            instance.OnInventoryKeyPressed(__instance);
            return;
        }

        if (ZInput.GetKey(KeyCode.T) && !instance.IsRecording)
        {
            instance.StartRecording();
            return;
        }
        else if (!ZInput.GetKey(KeyCode.T) && instance.IsRecording)
        {
            instance.StopRecording();
            if (Time.time - instance.recordingStartedTime > 1f)
            {
                instance.SendToBrain();
            }
            else
            {
                Debug.Log("Recording was too short. Has to be atleast 1 second long");
            }
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.Y))
        {
            instance.TogglePanel();

            return;
        }
        
        if (ZInput.GetKeyDown(KeyCode.L))
        {
            instance.GetNearbyResources(__instance.gameObject);

            return;
        }

        //instance.PlayRecordedAudio("");
        //instance.LoadAndPlayAudioFromBase64(instance.npcDialogueAudioPath);
        //instance.PlayWavFile(instance.npcDialogueRawAudioPath);
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

            if (instance.eNPCMode == NPCCommand.CommandType.PatrolArea && instance.patrol_harvest)
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
            if (humanoidNPC.LastPositionDelta > 2.5f && !humanoidNPC.InAttack() && humanoidNPC.GetTimeSinceLastAttack() > 1f)
            {
                humanoidNPC.StartAttack(humanoidNPC, false);
            }

            if (__instance.m_follow == null || __instance.m_follow.HasAnyComponent("Character", "Humanoid"))
            {
                GameObject newfollow;
                
                newfollow = FindClosestResource(__instance.gameObject, instance.CurrentHarvestResourceName);
                
                if (newfollow != null)
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
        if (instance.eNPCMode == NPCCommand.CommandType.CombatSneakAttack)
            run = false;
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

            __result = RemoveCustomText(__result);

            __result += "\n<color=orange><b>" + humanoidNPC_component.m_stamina.ToString("F2") + "</b></color>";
            __result += "\n<color=purple><b>" + instance.eNPCMode.ToString().ToUpper() + "</b></color>";
        }
    }

    private static string RemoveCustomText(string text)
    {
        string[] lines = text.Split('\n');
        System.Collections.Generic.List<string> newLines = new System.Collections.Generic.List<string>();

        foreach (string line in lines)
        {
            if (!line.Contains("<color=orange>") && !line.Contains("<color=purple>"))
            {
                newLines.Add(line);
            }
        }

        return string.Join("\n", newLines);
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


    // Inventory transfer hotfix
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
    private static bool OnSelectedItem(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
    {
        Player localPlayer = Player.m_localPlayer;
        if (localPlayer.IsTeleporting())
        {
            return false;
        }
        if ((bool)__instance.m_dragGo)
        {
            __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
            bool flag = localPlayer.IsItemEquiped(__instance.m_dragItem);
            bool flag2 = item != null && localPlayer.IsItemEquiped(item);
            Vector2i gridPos = __instance.m_dragItem.m_gridPos;
            if ((__instance.m_dragItem.m_shared.m_questItem || (item != null && item.m_shared.m_questItem)) && __instance.m_dragInventory != grid.GetInventory())
            {
                return false;
            }
            if (!__instance.m_dragInventory.ContainsItem(__instance.m_dragItem))
            {
                __instance.SetupDragItem(null, null, 1);
                return false;
            }
            localPlayer.RemoveEquipAction(item);
            localPlayer.RemoveEquipAction(__instance.m_dragItem);
            localPlayer.UnequipItem(__instance.m_dragItem, triggerEquipEffects: false);
            localPlayer.UnequipItem(item, triggerEquipEffects: false);
            bool num = grid.DropItem(__instance.m_dragInventory, __instance.m_dragItem, __instance.m_dragAmount, pos);
            if (__instance.m_dragItem.m_stack < __instance.m_dragAmount)
            {
                __instance.m_dragAmount = __instance.m_dragItem.m_stack;
            }
            if (flag)
            {
                ItemDrop.ItemData itemAt = grid.GetInventory().GetItemAt(pos.x, pos.y);
                if (itemAt != null)
                {
                    localPlayer.EquipItem(itemAt, triggerEquipEffects: false);
                }
                if (localPlayer.GetInventory().ContainsItem(__instance.m_dragItem))
                {
                    localPlayer.EquipItem(__instance.m_dragItem, triggerEquipEffects: false);
                }
            }
            if (flag2)
            {
                ItemDrop.ItemData itemAt2 = __instance.m_dragInventory.GetItemAt(gridPos.x, gridPos.y);
                if (itemAt2 != null)
                {
                    localPlayer.EquipItem(itemAt2, triggerEquipEffects: false);
                }
                if (localPlayer.GetInventory().ContainsItem(item))
                {
                    localPlayer.EquipItem(item, triggerEquipEffects: false);
                }
            }
            if (num)
            {
                __instance.SetupDragItem(null, null, 1);
                __instance.UpdateCraftingPanel();
            }
        }
        else
        {
            if (item == null)
            {
                return false;
            }
            switch (mod)
            {
                case InventoryGrid.Modifier.Move:
                    if (item.m_shared.m_questItem)
                    {
                        return false;
                    }
                    if (__instance.m_currentContainer != null)
                    {
                        localPlayer.RemoveEquipAction(item);
                        localPlayer.UnequipItem(item);
                        if (grid.GetInventory() == __instance.m_currentContainer.GetInventory())
                        {
                            localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);

                            Debug.Log("Moving from npc/container to player");
                            if (instance.PlayerNPC)
                            {
                                HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                                if (grid.GetInventory() == humanoidNPC_component.m_inventory)
                                {
                                    if (item.IsEquipable())
                                    {
                                        humanoidNPC_component.UnequipItem(item, true);
                                        //humanoidNPC_component.m_inventory.RemoveItem(item);
                                    }
                                }
                            }
                        }
                        else
                        {
                            __instance.m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), item);

                            Debug.Log("Moving from player to npc/container");
                            if (instance.PlayerNPC)
                            {
                                HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                                if (__instance.m_currentContainer.GetInventory() == humanoidNPC_component.m_inventory)
                                {
                                    if (item.IsEquipable())
                                    {
                                        humanoidNPC_component.EquipItem(item, true);
                                        //humanoidNPC_component.m_inventory.RemoveItem(item);
                                    }
                                }
                            }
                        }
                        __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
                    }
                    else if (Player.m_localPlayer.DropItem(grid.GetInventory(), item, item.m_stack))
                    {
                        __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
                    }
                    return false;
                case InventoryGrid.Modifier.Drop:
                    if (Player.m_localPlayer.DropItem(grid.GetInventory(), item, item.m_stack))
                    {
                        __instance.m_moveItemEffects.Create(__instance.transform.position, Quaternion.identity);
                    }
                    return false;
                case InventoryGrid.Modifier.Split:
                    if (item.m_stack > 1)
                    {
                        __instance.ShowSplitDialog(item, grid.GetInventory());
                        return false;
                    }
                    break;
            }
            __instance.SetupDragItem(item, grid.GetInventory(), item.m_stack);
        }


        return false;
    }


    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
    private static bool OnSelectedItem(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
    //private void OnSelectedItem(InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
    {
        Player localPlayer = Player.m_localPlayer;
        if (localPlayer.IsTeleporting())
        {
            return false;
        }
        if ((bool)m_dragGo)
        {
            m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
            bool flag = localPlayer.IsItemEquiped(m_dragItem);
            bool flag2 = item != null && localPlayer.IsItemEquiped(item);
            Vector2i gridPos = m_dragItem.m_gridPos;
            if ((m_dragItem.m_shared.m_questItem || (item != null && item.m_shared.m_questItem)) && m_dragInventory != grid.GetInventory())
            {
                return false;
            }
            if (!m_dragInventory.ContainsItem(m_dragItem))
            {
                SetupDragItem(null, null, 1);
                return false;
            }
            localPlayer.RemoveEquipAction(item);
            localPlayer.RemoveEquipAction(m_dragItem);
            localPlayer.UnequipItem(m_dragItem, triggerEquipEffects: false);
            localPlayer.UnequipItem(item, triggerEquipEffects: false);
            bool num = grid.DropItem(m_dragInventory, m_dragItem, m_dragAmount, pos);
            if (m_dragItem.m_stack < m_dragAmount)
            {
                m_dragAmount = m_dragItem.m_stack;
            }
            if (flag)
            {
                ItemDrop.ItemData itemAt = grid.GetInventory().GetItemAt(pos.x, pos.y);
                if (itemAt != null)
                {
                    localPlayer.EquipItem(itemAt, triggerEquipEffects: false);
                }
                if (localPlayer.GetInventory().ContainsItem(m_dragItem))
                {
                    localPlayer.EquipItem(m_dragItem, triggerEquipEffects: false);
                }
            }
            if (flag2)
            {
                ItemDrop.ItemData itemAt2 = m_dragInventory.GetItemAt(gridPos.x, gridPos.y);
                if (itemAt2 != null)
                {
                    localPlayer.EquipItem(itemAt2, triggerEquipEffects: false);
                }
                if (localPlayer.GetInventory().ContainsItem(item))
                {
                    localPlayer.EquipItem(item, triggerEquipEffects: false);
                }
            }
            if (num)
            {
                SetupDragItem(null, null, 1);
                UpdateCraftingPanel();
            }
        }
        else
        {
            if (item == null)
            {
                return false;
            }
            switch (mod)
            {
                case InventoryGrid.Modifier.Move:
                    if (item.m_shared.m_questItem)
                    {
                        return false;
                    }
                    if (m_currentContainer != null)
                    {
                        localPlayer.RemoveEquipAction(item);
                        localPlayer.UnequipItem(item);
                        if (grid.GetInventory() == m_currentContainer.GetInventory())
                        {
                            localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), item);
                        }
                        else
                        {
                            m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), item);
                        }
                        m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
                    }
                    else if (Player.m_localPlayer.DropItem(grid.GetInventory(), item, item.m_stack))
                    {
                        m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
                    }
                    return;
                case InventoryGrid.Modifier.Drop:
                    if (Player.m_localPlayer.DropItem(grid.GetInventory(), item, item.m_stack))
                    {
                        m_moveItemEffects.Create(base.transform.position, Quaternion.identity);
                    }
                    return;
                case InventoryGrid.Modifier.Split:
                    if (item.m_stack > 1)
                    {
                        ShowSplitDialog(item, grid.GetInventory());
                        return;
                    }
                    break;
            }
            SetupDragItem(item, grid.GetInventory(), item.m_stack);
        }
    }*/







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
            Console.instance.TryRunCommand("despawn_all");
            Debug.Log("Spawning more than one NPC is disabled");
            //return;
        }
        Player localPlayer = Player.m_localPlayer;
        GameObject npcPrefab = ZNetScene.instance.GetPrefab("HumanoidNPC");
        GameObject playerPrefab = ZNetScene.instance.GetPrefab("Player");
        GameObject vanillaPlayer = localPlayer.gameObject;
        //GameObject vanillaPlayer = Resources.Load("Player") as GameObject;
        //GameObject npcPrefab = HumanoidNPCPrefab;

        

        if (npcPrefab == null)
        {
            Logger.LogError("ScriptNPC prefab not found!");
        }

        // spawn NPC
        Vector3 spawnPosition = localPlayer.transform.position + localPlayer.transform.forward * 2f;
        //Vector3 spawnPosition = GetRandomSpawnPosition(10f);
        Quaternion spawnRotation = localPlayer.transform.rotation;

        /*GameObject npcInstance = Instantiate<GameObject>(playerPrefab, spawnPosition, spawnRotation);
        npcInstance.name = "HumanoidNPC";
        Player npcPlayerComp = npcInstance.GetComponent<Player>();
        Destroy(npcPlayerComp);
        PlayerController npcPCComp = npcInstance.GetComponent<PlayerController>();
        Destroy(npcPCComp);
        BaseAI npcBaseAIComp = npcInstance.GetComponent<BaseAI>();
        Destroy(npcBaseAIComp);*/

        

        /*MonsterAI monsterAIcomp = npcInstance.AddComponent<MonsterAI>();
        HumanoidNPC humanoidNpc_Component = npcInstance.AddComponent<HumanoidNPC>();

        humanoidNpc_Component.m_walkSpeed = localPlayer.m_walkSpeed;
        humanoidNpc_Component.m_runSpeed = localPlayer.m_runSpeed;*/

        GameObject npcInstance = Instantiate<GameObject>(npcPrefab, spawnPosition, spawnRotation);
        npcInstance.SetActive(true);

        instance.TogglePanel();

        VisEquipment npcInstanceVis = npcInstance.GetComponent<VisEquipment>();
        VisEquipment playerInstanceVis = localPlayer.GetComponent<VisEquipment>();

        npcInstanceVis.m_isPlayer = true;
        npcInstanceVis.m_emptyBodyTexture = playerInstanceVis.m_emptyBodyTexture;
        npcInstanceVis.m_emptyLegsTexture = playerInstanceVis.m_emptyLegsTexture;

        instance.PlayerNPC = npcInstance;

        // make the monster tame
        MonsterAI monsterAIcomp = npcInstance.GetComponent<MonsterAI>();

        SetMonsterAIAggravated(monsterAIcomp, false);
        monsterAIcomp.MakeTame();
        monsterAIcomp.SetFollowTarget(localPlayer.gameObject);
        monsterAIcomp.m_viewRange = 80f;


        // add item to inventory
        HumanoidNPC humanoidNpc_Component = npcInstance.GetComponent<HumanoidNPC>();
        if (humanoidNpc_Component != null)
        {
            LoadNPCData(humanoidNpc_Component);

            /*humanoidNpc_Component.m_name = "NPC";
            humanoidNpc_Component.m_visEquipment.m_isPlayer = true;
            humanoidNpc_Component.m_visEquipment.SetSkinColor(new Vector3(0.8f, 0.6f, 0.4f));
            humanoidNpc_Component.m_visEquipment.SetHairColor(new Vector3(1f, 1f, 1f));*/

            GameObject itemPrefab;

            /*itemPrefab = ZNetScene.instance.GetPrefab("ArmorRagsChest");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);

            itemPrefab = ZNetScene.instance.GetPrefab("ArmorRagsLegs");
            humanoidNpc_Component.GiveDefaultItem(itemPrefab);*/

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

    // helper function to copy component values (not tested/not being used)
    T CopyComponent<T>(GameObject source, GameObject destination) where T : Component
    {
        T sourceComp = source.GetComponent<T>();
        if (sourceComp != null)
        {
            //T newComp = destination.AddComponent<T>();
            T newComp = destination.GetComponent<T>();
            System.Reflection.FieldInfo[] fields = typeof(T).GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(newComp, field.GetValue(sourceComp));
            }
            return newComp;
        }
        return null;
    }


    private void Follow_Start(GameObject target, string NPCDialogueMessage = "Right behind ya!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Follow_Start failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        SetMonsterAIAggravated(monsterAIcomponent, false);
        monsterAIcomponent.SetFollowTarget(target);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.FollowPlayer;
        Debug.Log("Follow_Start activated!");
    }

    private void Follow_Stop(string NPCDialogueMessage = "I'm gonna wander off on my own now!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Follow_Stop failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        SetMonsterAIAggravated(monsterAIcomponent, false);
        monsterAIcomponent.SetFollowTarget(null);  

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Follow_Stop activated!");
    }

    private void Combat_StartAttacking(string EnemyName, string NPCDialogueMessage = "Watch out, here I come!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Combat_StartAttacking failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        /*GameObject[] allEnemies = FindEnemies();
        GameObject nearestUntamedEnemy = allEnemies
        .Where(enemy => enemy.GetComponent<MonsterAI>() != null && !enemy.GetComponent<MonsterAI>().m_character.m_tamed)
        .OrderBy(enemy => Vector3.Distance(instance.PlayerNPC.transform.position, enemy.transform.position))
        .FirstOrDefault();*/

        GameObject closestEnemy = null;

        if (EnemyName != "")
        {
            Debug.Log($"Trying to find enemy {EnemyName}");
            closestEnemy = FindClosestEnemy(instance.PlayerNPC, EnemyName);
        }
        else
        {
            Debug.Log("EnemyName was null");
        }

            

        if (closestEnemy != null)
        {
            monsterAIcomponent.SetFollowTarget(closestEnemy);
            Debug.Log($"Combat_StartAttacking closestEnemy found! " + closestEnemy.name);
        }
        else
        {
            monsterAIcomponent.SetFollowTarget(null);
            Debug.Log("Combat_StartAttacking closestEnemy not found!");
        }
            

        monsterAIcomponent.m_alerted = false;
        monsterAIcomponent.m_aggravatable = true;
        monsterAIcomponent.SetHuntPlayer(true);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.CombatAttack;
        Debug.Log("Combat_StartAttacking activated!");
    }

    private void Combat_StartSneakAttacking(GameObject target, string NPCDialogueMessage = "Sneaking up on em!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Combat_StartSneakAttacking failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        // disregard nearby enemies
        monsterAIcomponent.SetFollowTarget(null);
        monsterAIcomponent.m_alerted = false;
        monsterAIcomponent.m_aggravatable = true;
        monsterAIcomponent.SetHuntPlayer(true);
        humanoidnpc_component.SetCrouch(true);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.CombatSneakAttack;
        Debug.Log("Combat_StartSneakAttacking activated!");
    }

    private void Combat_StartDefending(GameObject target, string NPCDialogueMessage = "Don't worry, I'm here with you!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Combat_StartDefending failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        // disregard nearby enemies
        monsterAIcomponent.SetFollowTarget(null);
        SetMonsterAIAggravated(monsterAIcomponent, false);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.CombatDefend;
        Debug.Log("Combat_StartDefending activated!");
    }

    private void Combat_StopAttacking(string NPCDialogueMessage = "I'll give em a break!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Combat_StopAttacking failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        // disregard nearby enemies
        monsterAIcomponent.SetFollowTarget(null);
        SetMonsterAIAggravated(monsterAIcomponent, false);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Combat_StopAttacking activated!");
    }

    private void Inventory_DropAll(string NPCDialogueMessage = "Here is all I got!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Inventory_DropAll failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        DropAllItems(humanoidnpc_component);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        //instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Inventory_DropAll activated!");
    }
    private void Inventory_DropItem(string ItemName, string NPCDialogueMessage = "Here is what you asked for!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Inventory_DropItem failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        DropItem(ItemName, humanoidnpc_component);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        //instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Inventory_DropItem activated!");
    }

    private void Inventory_EquipItem(string ItemName, string NPCDialogueMessage = "On it boss!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Inventory_EquipItem failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        EquipItem(ItemName, humanoidnpc_component);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        //instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Inventory_EquipItem activated!");
    }

    private void Harvesting_Start(string ResourceName, string NPCDialogueMessage = "On it boss!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Harvesting_Start failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        instance.CurrentHarvestResourceName = ResourceName;
        Debug.Log("resource name " + instance.CurrentHarvestResourceName);

        //ResourceName = "Beech";
        GameObject resource = FindClosestResource(instance.PlayerNPC, instance.CurrentHarvestResourceName);
        if (resource == null)
        {
            // inform API that resource was not found and wasn't processed
            Debug.Log($"couldn't find resource name {resource}");
            return;
        }
        else
        {
            Debug.Log("resource valid");
        }

        monsterAIcomponent.SetFollowTarget(resource);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.HarvestResource;
        Debug.Log("Harvesting_Start activated!");
    }

    private void Harvesting_Stop(string NPCDialogueMessage = "No more labor!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Harvesting_Stop failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        monsterAIcomponent.SetFollowTarget(null);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Harvesting_Stop activated!");
    }

    private void Patrol_Start(string NPCDialogueMessage = "I'm keeping guard around here! They know not to try!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Patrol_Start failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        instance.patrol_position = Player.m_localPlayer.transform.position;
        instance.patrol_harvest = true;

        //Vector3 randLocation = GetRandomReachableLocationInRadius(humanoidNPC_component.patrol_position, patrol_radius);

        monsterAIcomponent.SetFollowTarget(null);
        SetMonsterAIAggravated(monsterAIcomponent, false);
        SetMonsterAIAggravated(monsterAIcomponent, true);
        

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.PatrolArea;
        Debug.Log("Patrol_Start activated!");
    }

    private void Patrol_Stop(string NPCDialogueMessage = "My job is done here!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Patrol_Stop failed, instance.PlayerNPC == null");
            return;
        }

        MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
        HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        monsterAIcomponent.SetFollowTarget(null);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.eNPCMode = NPCCommand.CommandType.Idle;
        Debug.Log("Patrol_Stop activated!");
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
        instance.eNPCMode = ValheimAIModLoader.NPCCommand.CommandType.CombatAttack;

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

            //Debug.Log("SendUpdateToBrain");
            SendUpdateToBrain(npc);
            instance.lastSentToBrainTime = Time.time;

            AddChatTalk(humanoidComponent, "NPC", "...");
        }
    }

    private void OnInventoryKeyPressed(Player player)
    {
        if (instance.PlayerNPC)
        {
            HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
            InventoryGui.instance.Show(humanoidNPC_component.inventoryContainer);
            PrintInventoryItems(humanoidNPC_component.m_inventory);
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

    private void DropItem(string ItemName, HumanoidNPC humanoidNPC)
    {
        List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
        int num = 1;
        foreach (ItemDrop.ItemData item in allItems)
        {
            if (ItemName == item.m_shared.m_name)
            {
                Debug.Log("Dropping " + item.m_shared.m_name);
                //Vector3 position = humanoidNPC.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                Vector3 position = humanoidNPC.transform.position + Vector3.up * 2f + UnityEngine.Random.insideUnitSphere * 0.3f + humanoidNPC.transform.forward * 2.5f;
                Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                ItemDrop.DropItem(item, item.m_stack, position, rotation);
                num++;
                humanoidNPC.m_inventory.RemoveOneItem(item);
                return;
            }
        }
       
    }

    private void EquipItem(string ItemName, HumanoidNPC humanoidNPC)
    {
        List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
        foreach (ItemDrop.ItemData item in allItems)
        {
            if (ItemName == item.m_shared.m_name)
            {
                Debug.Log("Equipping  " + item.m_shared.m_name);
                humanoidNPC.EquipItem(item);
                return;
            }
        }

    }




    private void AddChatTalk(Character character, string name, string text)
    {
        UserInfo userInfo = new UserInfo();
        userInfo.Name = name;
        Vector3 headPoint = character.GetEyePoint() + (Vector3.up * -100f);
        Chat.instance.AddInworldText(character.gameObject, 0, headPoint, Talker.Type.Shout, userInfo, text);
        Chat.instance.AddString("NPC", text, Talker.Type.Normal);
    }

    public void BrainSynthesizeAudio(string text, string voice, Action<byte[]> onComplete, Action<string> onError)
    {
        using (WebClient client = new WebClient())
        {
            // Construct the URL with query parameters
            string url = $"{brainBaseURL}/synthesize_audio?text={Uri.EscapeDataString(text)}&voice={Uri.EscapeDataString(voice)}";

            client.DownloadStringCompleted += OnBrainSynthesizeAudioResponse;
            client.DownloadStringAsync(new Uri(url));
        }
    }

    private void OnBrainSynthesizeAudioResponse(object sender, DownloadStringCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            Debug.Log($"Download failed: {e.Error.Message}");
            return;
        }

        try
        {
            JsonObject responseObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(e.Result);
            string audio_file_id = responseObject["audio_file_id"].ToString();
            string text = responseObject["text"].ToString();
            HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            AddChatTalk(npc, "NPC", text);
            DownloadAudioFile(audio_file_id);
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to parse JSON: {ex.Message}");
        }
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
        webClient.UploadStringCompleted += OnSendUpdateToBrainResponse;
    }

    private void OnSendUpdateToBrainResponse(object sender, UploadStringCompletedEventArgs e)
    {
        if (e.Error == null)
        {
            string responseJson = e.Result;

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
                for (int i=0; i<agentCommands.Count; i++)
                {
                    JsonObject commandObject = agentCommands[i] as JsonObject;

                    if (!(commandObject.ContainsKey("action") && commandObject.ContainsKey("category")))
                    {
                        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                        AddChatTalk(npc, "NPC", agent_text_response);

                        Debug.Log("Agent command response from brain was incomplete. Command's Action or Category is missing!");
                        continue;
                    }

                    string action = commandObject["action"].ToString();
                    string category = commandObject["category"].ToString();

                    string[] parameters = {};
                    string p = "";

                    if (commandObject.ContainsKey("parameters"))
                    {
                        JsonArray jsonparams = commandObject["parameters"] as JsonArray;
                        if (jsonparams != null && jsonparams.Count > 0)
                        {
                            p = jsonparams[0].ToString();
                        }
                    }

                    foreach (string pa in parameters)
                    {
                        Debug.Log($"param {pa}");
                    }

                    Debug.Log("NEW COMMAND: Category: " + category + ". Action : " + action + ". Parameters: " + parameters);
                    ProcessNPCCommand(category, action, p, agent_text_response);
                }
            }
            else
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                AddChatTalk(npc, "NPC", agent_text_response);
                Debug.Log("No agent commands found.");
            }

            Debug.Log("Response from brain");
            Debug.Log("You said: " + player_instruction_transcription);
            Debug.Log("NPC said: " + agent_text_response);
            Debug.Log("Full response from brain: " + responseJson);

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

    private void ProcessNPCCommand(string category, string action, string parameter, string agent_text_response)
    {
        Player localPlayer = Player.m_localPlayer;

        //string firstParameter = parameters.Length > 0 ? parameters[0] : "NULL";

        if (category == "Follow")
        {
            if (action == "Start")
            {
                instance.Follow_Start(localPlayer.gameObject, agent_text_response);
            }
            else if (action == "Stop")
            {
                instance.Follow_Stop(agent_text_response);
            }
        }

        else if (category == "Combat")
        {
            if (action == "StartAttacking")
            {
                instance.Combat_StartAttacking(parameter, agent_text_response);
            }
            else if (action == "Sneak")
            {
                instance.Combat_StartSneakAttacking(null, agent_text_response);
            }
            else if (action == "Defend")
            {
                instance.Combat_StartDefending(null, agent_text_response);
            }
            else if (action == "StopAttacking")
            {
                instance.Combat_StopAttacking(agent_text_response);
            }
        }

        else if (category == "Inventory")
        {
            if (action == "DropAll")
            {
                instance.Inventory_DropAll(agent_text_response);
            }
            else if (action == "DropItem")
            {
                instance.Inventory_DropItem(parameter, agent_text_response);
            }
            else if (action == "EquipItem")
            {
                instance.Inventory_EquipItem(parameter, agent_text_response);
            }
            else if (action == "PickupItem")
            {

            }
        }
        else if (category == "Harvesting")
        {
            if (action == "Start")
            {
                Debug.Log($"harvesting start {parameter}");
                instance.Harvesting_Start(parameter, agent_text_response);
            }
            else if (action == "Stop")
            {
                instance.Harvesting_Stop(agent_text_response);
            }
        }
        else if (category == "Patrol")
        {
            if (action == "Start")
            {
                instance.Patrol_Start(agent_text_response);
            }
            else if (action == "Stop")
            {
                instance.Patrol_Stop(agent_text_response);
            }
        }
        else
        {
            Debug.Log($"ProcessNPCCommand failed {category} {action}");
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

    private static GameObject FindClosestEnemy(GameObject character, string EnemyName)
    {
        //return GameObject.FindObjectsOfType<GameObject>(true)
            return instance.FindEnemies()
                //.Where(go => go.name.Contains(EnemyName) && go.HasAnyComponent("Character", "Humanoid" , "BaseAI", "MonsterAI"))
                .Where(go => go.name.StartsWith(EnemyName))
                .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .FirstOrDefault();
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
                .Where(go => go.name.Contains(ResourceName) && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
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


    Dictionary<string, int> nearbyResources = new Dictionary<string, int>();
    Dictionary<string, float> nearbyResourcesDistance = new Dictionary<string, float>();

    string CleanKey(string key)
    {
        // Remove everything after and including the last opening parenthesis
        int lastParenIndex = key.LastIndexOf('(');
        if (lastParenIndex != -1)
        {
            key = key.Substring(0, lastParenIndex);
        }

        // Trim any remaining whitespace
        key = key.Trim();

        // Remove any trailing numbers
        while (key.Length > 0 && char.IsDigit(key[key.Length - 1]))
        {
            key = key.Substring(0, key.Length - 1);
        }

        // Trim again in case there was whitespace before the numbers
        return key.Trim();
    }

    private string GetNearbyResources(GameObject source)
    {
        Pickable[] pickables = GameObject.FindObjectsOfType<Pickable>(true);
        Destructible[] destructibles = GameObject.FindObjectsOfType<Destructible>(true);
        TreeBase[] trees = GameObject.FindObjectsOfType<TreeBase>(true);

        Debug.Log("pickables len " + pickables.Length);
        Debug.Log("destructibles len " + destructibles.Length);
        Debug.Log("trees len " + trees.Length);

        void ProcessResource(Component resource, string key)
        {
            key = CleanKey(key);

            if (nearbyResources.ContainsKey(key))
                nearbyResources[key]++;
            else
                nearbyResources[key] = 1;

            float distance = resource.transform.position.DistanceTo(source.transform.position);
            if (nearbyResourcesDistance.ContainsKey(key))
                nearbyResourcesDistance[key] = Mathf.Min(nearbyResourcesDistance[key], distance);
            else
                nearbyResourcesDistance[key] = distance;

            /*Vector3 directionToResource = resource.transform.position - source.transform.position;
            float xRotationDifference = Vector3.SignedAngle(Vector3.ProjectOnPlane(source.transform.forward, Vector3.up), Vector3.ProjectOnPlane(directionToResource, Vector3.up), Vector3.up);

            if (nearbyResourcesXRotation.ContainsKey(key))
            {
                float currentRotation = nearbyResourcesXRotation[key];
                if (Mathf.Abs(xRotationDifference) < Mathf.Abs(currentRotation) ||
                    (Mathf.Abs(xRotationDifference) == Mathf.Abs(currentRotation) && distance < nearbyResourcesDistance[key]))
                {
                    nearbyResourcesXRotation[key] = xRotationDifference;
                }
            }
            else
                nearbyResourcesXRotation[key] = xRotationDifference;*/
        }

        foreach (Pickable pickable in pickables)
            ProcessResource(pickable, pickable.name);

        foreach (Destructible destructible in destructibles)
            ProcessResource(destructible, destructible.name);

        foreach (TreeBase tree in trees)
            ProcessResource(tree, tree.name);

        var jarray = new JsonArray();

        foreach (var kvp in nearbyResources)
        {
            //Debug.Log($"{kvp.Key}: {kvp.Value} | nearest's distance: {nearbyResourcesDistance[kvp.Key]:F2} | X rotation difference: {nearbyResourcesXRotation[kvp.Key]:F2}°");

            JsonObject thisJobject = new JsonObject();
            thisJobject["name"] = kvp.Key;
            thisJobject["quantity"] = kvp.Value;
            thisJobject["nearestDistance"] = nearbyResourcesDistance[kvp.Key];

            jarray.Add(thisJobject);
        }

        int totalResources = nearbyResources.Values.Sum();
        Debug.Log($"Total resources: {totalResources}");

        //string json = jarray.ToString();
        string json = SimpleJson.SimpleJson.SerializeObject(jarray);
        Debug.Log(json);
        return json;
    }

    Dictionary<string, int> nearbyEnemies = new Dictionary<string, int>();
    Dictionary<string, float> nearbyEnemiesDistance = new Dictionary<string, float>();
    private string GetNearbyEnemies(GameObject source)
    {
        Character[] characters = GameObject.FindObjectsOfType<Character>(true);
        Humanoid[] humanoids = GameObject.FindObjectsOfType<Humanoid>(true);

        Debug.Log("characters len " + characters.Length);
        Debug.Log("humanoids len " + humanoids.Length);

        

        void ProcessResource(Component resource, string key)
        {
            key = CleanKey(key);

            if (nearbyEnemies.ContainsKey(key))
                nearbyEnemies[key]++;
            else
                nearbyEnemies[key] = 1;

            float distance = resource.transform.position.DistanceTo(source.transform.position);
            if (nearbyEnemiesDistance.ContainsKey(key))
                nearbyEnemiesDistance[key] = Mathf.Min(nearbyEnemiesDistance[key], distance);
            else
                nearbyEnemiesDistance[key] = distance;
        }

        foreach (Character character in characters)
            ProcessResource(character, character.name);

        foreach (Humanoid humanoid in humanoids)
            ProcessResource(humanoid, humanoid.name);

        var jarray = new JsonArray();

        foreach (var kvp in nearbyEnemies)
        {
            JsonObject thisJobject = new JsonObject();
            thisJobject["name"] = kvp.Key;
            thisJobject["quantity"] = kvp.Value;
            thisJobject["nearestDistance"] = nearbyEnemiesDistance[kvp.Key];

            jarray.Add(thisJobject);
        }

        int totalEnemies = nearbyEnemies.Values.Sum();
        Debug.Log($"Total enemies: {totalEnemies}");

        //string json = jarray.ToString();
        string json = SimpleJson.SimpleJson.SerializeObject(jarray);
        Debug.Log(json);
        return json;
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

    /*private string GetRecordingDevice()
    {
        if (Microphone.devices.Length > MicrophoneIndex.Value)
        {
            return Microphone.devices[MicrophoneIndex.Value];
        }

        return null;
    }*/

    private void StartRecording()
    {
        instance.recordedAudioClip = Microphone.Start(Microphone.devices[instance.MicrophoneIndex], false, recordingLength, sampleRate);
        instance.IsRecording = true;
        instance.recordingStartedTime = Time.time;
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
        audioSource.volume = instance.NPCVolume;
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


        var npcInventoryItems = new JsonArray();
        foreach (ItemDrop.ItemData item in humanoidNPC.m_inventory.m_inventory)
        {
            var itemData = new JsonObject
            {
                ["name"] = item.m_shared.m_name,
                ["amount"] = item.m_stack,
            };
            npcInventoryItems.Add(itemData);
        }

        var playerInventoryItems = new JsonArray();
        foreach (ItemDrop.ItemData item in Player.m_localPlayer.m_inventory.m_inventory)
        {
            var itemData = new JsonObject
            {
                ["name"] = item.m_shared.m_name,
                ["amount"] = item.m_stack,
            };
            playerInventoryItems.Add(itemData);
        }

        var gameState = new JsonObject
        {
            ["Name"] = humanoidNPC.m_name,
            ["Health"] = humanoidNPC.GetHealth(),
            ["Stamina"] = humanoidNPC.m_stamina,
            ["Inventory"] = npcInventoryItems,
            ["PlayerInventory"] = playerInventoryItems,
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
            ["nearbyItems"] = instance.GetNearbyResources(character),
            ["nearbyEnemies"] = instance.GetNearbyEnemies(character),
    };

        //string base64audio = instance.GetBase64AudioData(instance.recordedAudioClip);
        string base64audio = instance.GetBase64FileData(instance.playerDialogueAudioPath);

        var jsonObject = new JsonObject
        {
            ["player_id"] = humanoidNPC.GetZDOID().ToString(),
            ["game_state"] = gameState,
            ["player_instruction_audio_file_base64"] = base64audio,
            ["timestamp"] = Time.time,
            ["personality"] = instance.personalityText,
            ["voice"] = npcVoices[instance.NPCVoice].ToLower(),
            ["gender"] = instance.NPCGender,
        };

        string jsonString = SimpleJson.SimpleJson.SerializeObject(jsonObject);

        jsonObject["player_instruction_audio_file_base64"] = "";
        string jsonString2 = SimpleJson.SimpleJson.SerializeObject(jsonObject);
        Debug.Log("Sending to brain: " + jsonString2);

        return jsonString;
    }

    public static void SaveNPCData(GameObject character)
    {
        HumanoidNPC humanoidNPC = character.GetComponent<HumanoidNPC>();
        MonsterAI monsterAI = character.GetComponent<MonsterAI>();

        JsonObject data = new JsonObject();

        data["name"] = humanoidNPC.m_name;
        data["personality"] = instance.personalityText;
        data["voice"] = instance.NPCVoice;
        data["gender"] = instance.NPCGender;
        

        // inventory
        var inventoryItems = new JsonArray();
        foreach (ItemDrop.ItemData item in humanoidNPC.m_inventory.m_inventory)
        {
            var itemData = new JsonObject
            {
                ["name"] = item.m_shared.m_name,
                ["stack"] = item.m_stack,
            };
            inventoryItems.Add(itemData);
        }
        data["inventory"] = inventoryItems;


        JsonArray skinColorArray = new JsonArray();
        skinColorArray.Add(humanoidNPC.m_visEquipment.m_skinColor.x);
        skinColorArray.Add(humanoidNPC.m_visEquipment.m_skinColor.y);
        skinColorArray.Add(humanoidNPC.m_visEquipment.m_skinColor.z);
        data["skinColor"] = skinColorArray;

        JsonArray hairColorArray = new JsonArray();
        hairColorArray.Add(humanoidNPC.m_visEquipment.m_hairColor.x);
        hairColorArray.Add(humanoidNPC.m_visEquipment.m_hairColor.y);
        hairColorArray.Add(humanoidNPC.m_visEquipment.m_hairColor.z);
        data["hairColor"] = skinColorArray;

        string json = SimpleJson.SimpleJson.SerializeObject(data);

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string filePath = Path.Combine(desktopPath, "egoaimod.json");

        File.WriteAllText(filePath, json);
        Debug.Log("Saved NPC data to " + filePath);
    }

    public static void LoadNPCData(HumanoidNPC npc)
    {
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string filePath = Path.Combine(desktopPath, "egoaimod.json");
        Debug.Log("Loading NPC data from " + filePath);

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            JsonObject data = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);


            npc.m_name = data["name"].ToString();
            instance.personalityText = data["personality"].ToString();
            instance.personalityInputText.text = instance.personalityText;
            instance.personalityInputField.text = instance.personalityText;
            instance.personalityInputField.textComponent.text = instance.personalityText;
            instance.personalityInputField.SetTextWithoutNotify(instance.personalityText);


            instance.NPCVoice = int.Parse(data["voice"].ToString());
            instance.voiceDropdownComp.SetValueWithoutNotify(instance.NPCVoice);

            instance.NPCGender = int.Parse(data["gender"].ToString());
            if (instance.NPCGender == 0)
            {
                instance.toggleMasculine.isOn = true;
                instance.toggleFeminine.isOn = false;
            }
            else
            {
                instance.toggleMasculine.isOn = false;
                instance.toggleFeminine.isOn = true;
            }

            // Load inventory
            JsonArray inventoryArray = data["inventory"] as JsonArray;
            npc.m_inventory.RemoveAll();
            foreach (JsonObject itemData in inventoryArray)
            {
                string itemName = itemData["name"].ToString();
                int stack = int.Parse(itemData["stack"].ToString());
                // Create and add item to inventory
                // You'll need to implement this part based on how your item system works
                // For example:
                // ItemDrop.ItemData newItem = ItemDrop.ItemData.CreateItem(itemName, stack);
                // npc.m_inventory.AddItem(newItem);

                GameObject itemPrefab = ZNetScene.instance.GetPrefab(itemName);
                if (itemPrefab != null)
                {
                    ItemDrop.ItemData itemdata = npc.PickupPrefab(itemPrefab, stack);
                    npc.EquipItem(itemdata);
                }
            }

            // Load skin color
            JsonArray skinColorArray = data["skinColor"] as JsonArray;
            if (skinColorArray.Count == 3)
            {
                npc.m_visEquipment.SetSkinColor(new Vector3(
                    int.Parse(skinColorArray[0].ToString()),
                    int.Parse(skinColorArray[1].ToString()),
                    int.Parse(skinColorArray[2].ToString())
                ));
            }

            // Load skin color
            JsonArray hairColorArray = data["hairColor"] as JsonArray;
            if (hairColorArray.Count == 3)
            {
                npc.m_visEquipment.SetHairColor(new Vector3(
                    int.Parse(hairColorArray[0].ToString()),
                    int.Parse(hairColorArray[1].ToString()),
                    int.Parse(hairColorArray[2].ToString())
                ));
            }

            Debug.Log($"NPC data loaded for {npc.m_name}");
        }
        else
        {
            Debug.LogWarning("No saved NPC data found.");
        }
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

    private void PopulateMonsterPrefabs()
    {
        var monsterList = new JsonArray();

        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            Character prechar = prefab.GetComponent<Character>();
            Humanoid prehum = prefab.GetComponent<Humanoid>();

            if (prechar != null)
            {
                JsonObject thisJsonObject = new JsonObject();
                thisJsonObject["name"] = prechar.name;
                thisJsonObject["itemName"] = prechar.m_name;
                monsterList.Add(thisJsonObject);
            }

            if (prehum != null)
            {
                JsonObject thisJsonObject = new JsonObject();
                thisJsonObject["name"] = prehum.name;
                thisJsonObject["itemName"] = prehum.m_name;
                monsterList.Add(thisJsonObject);
            }
        }

        string json = monsterList.ToString();

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "monsters.json");

        File.WriteAllText(filePath, json);
        Debug.Log($"Monster prefab list exported to {filePath}");
    }

    private void PopulateAllItems()
    {
        var allItemsList = new JsonArray();
        foreach (GameObject prefab in ObjectDB.instance.m_items)
        {
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                var thisJsonObject = new JsonObject();

                thisJsonObject["name"] = itemDrop.name;
                thisJsonObject["itemName"] = itemDrop.m_itemData.m_shared.m_name;

                allItemsList.Add(thisJsonObject);
            }
        }

        string json = allItemsList.ToString();

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string filePath = Path.Combine(desktopPath, "all_items_list.json");

        File.WriteAllText(filePath, json);
        Debug.Log($"Crafting requirements exported to {filePath}");
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



    /*
     * 
     * UI
     * 
     */

    private Texture2D TestTex;
    private Sprite TestSprite;
    private GameObject TestPanel;

    // Toggle our test panel with button
    private void TogglePanel()
    {
        // Create the panel if it does not exist
        if (!TestPanel)
        {
            if (GUIManager.Instance == null)
            {
                Logger.LogError("GUIManager instance is null");
                return;
            }

            if (!GUIManager.CustomGUIFront)
            {
                Logger.LogError("GUIManager CustomGUI is null");
                return;
            }

            // Create the panel object
            TestPanel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
                position: new Vector2(0, 0),
                width: 850,
                height: 700,
                draggable: true);
            TestPanel.SetActive(false);

            CreatePanel();
        }

        // Switch the current state
        bool state = !TestPanel.activeSelf;

        // Set the active state of the panel
        TestPanel.SetActive(state);
        instance.IsModMenuShowing = state;

        // Toggle input for the player and camera while displaying the GUI
        GUIManager.BlockInput(state);
    }

    private void CreatePanel()
    {
        // Task Queue
        //CreateTaskQueue(leftBox);
        //CreateTaskQueue(new Vector2(-200, 200));
        CreateScrollableTaskQueue(new Vector2(200, -150));

        // Key Bindings
        CreateKeyBindings(new Vector2(25, -320));

        // Ego Banner
        CreateMicInput(new Vector2(25, -530));

        // Ego Banner
        CreateEgoBanner(new Vector2(25, -630));

        // Personality
        CreatePersonalitySection(new Vector2(200, -20));

        // Voice and Volume   
        CreateVoiceAndVolumeControls(new Vector2(200, -250));

        // Body Type
        CreateBodyTypeToggle(new Vector2(200, -400));

        // Buttons
        CreateButtons(new Vector2(0, -400));

       
    }

    private ScrollRect scrollRectTaskQueue;
    private RectTransform contentPanelTaskQueue;
    private RectTransform viewportRectTaskQueue;
    private void CreateScrollableTaskQueue(Vector2 position)
    {
        GUIManager.Instance.CreateText(
            text: "Task Queue",
            parent: TestPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f),*/
            position: new Vector2(200f, -40f),
            //position: startPosition + new Vector2(170, 0),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        Debug.Log("Creating scrollable task queue");

        // Create a ScrollRect
        GameObject scrollObject = new GameObject("TaskQueueScroll", typeof(RectTransform), typeof(ScrollRect));
        scrollRectTaskQueue = scrollObject.GetComponent<ScrollRect>();
        scrollRectTaskQueue.transform.SetParent(TestPanel.transform, false);

        // Set up the ScrollRect
        scrollRectTaskQueue.horizontal = false;
        scrollRectTaskQueue.vertical = true;

        // Create viewport
        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewportRectTaskQueue = viewportObject.GetComponent<RectTransform>();
        viewportRectTaskQueue.SetParent(scrollRectTaskQueue.transform, false);
        viewportRectTaskQueue.anchorMin = Vector2.zero;
        viewportRectTaskQueue.anchorMax = Vector2.one;
        viewportRectTaskQueue.sizeDelta = Vector2.zero;
        viewportRectTaskQueue.anchoredPosition = Vector2.zero;

        // Set up mask
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.sprite = null;
        //viewportImage.color = Color.black; // Changed to white for visibility
        viewportImage.color = new Color(0, 0, 0, 0.3f); // Changed to white for visibility

        // Create content panel
        GameObject contentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
        contentPanelTaskQueue = contentObject.GetComponent<RectTransform>();
        contentPanelTaskQueue.SetParent(viewportRectTaskQueue, false);

        // Set up the content panel
        contentPanelTaskQueue.anchorMin = new Vector2(0, 1);
        contentPanelTaskQueue.anchorMax = new Vector2(1, 1);
        contentPanelTaskQueue.anchoredPosition = new Vector2(0, 0);
        contentPanelTaskQueue.sizeDelta = new Vector2(0, 0);

        // Add a Content Size Fitter
        ContentSizeFitter contentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Modify the VerticalLayoutGroup setup
        VerticalLayoutGroup layoutGroup = contentPanelTaskQueue.GetComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(5, 5, 5, 5);

        // Assign the viewport and content panel to the ScrollRect
        scrollRectTaskQueue.viewport = viewportRectTaskQueue;
        scrollRectTaskQueue.content = contentPanelTaskQueue;

        // Set up the ScrollRect's RectTransform
        RectTransform scrollRectTransform = scrollRectTaskQueue.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0, 1);
        scrollRectTransform.anchorMax = new Vector2(0, 1);
        scrollRectTransform.anchoredPosition = position + new Vector2(0, -30f);
        scrollRectTransform.sizeDelta = new Vector2(350, 250);

        scrollRectTaskQueue.movementType = ScrollRect.MovementType.Clamped;
        contentPanelTaskQueue.pivot = new Vector2(0, 1);
        //scrollRectTransform.pivot = new Vector2(0, 0);

        Debug.Log($"ScrollRect created at position: {position}, size: {scrollRectTransform.sizeDelta}");


        CreateTaskQueue();
    }

    private void CreateTaskQueue()
    {
        Debug.Log("Creating task queue contents");
        //CreateTaskQueueTitle();

        CreateTask("UNDER DEVELOPMENT...");
    }

    /*private void CreateTaskQueueTitle()
    {
        GameObject titleObject = new GameObject("TaskQueueTitle", typeof(RectTransform), typeof(Text));
        titleObject.transform.SetParent(contentPanelTaskQueue, false);

        Text titleText = titleObject.GetComponent<Text>();
        titleText.text = "Task Queue";
        titleText.font = GUIManager.Instance.AveriaSerifBold;
        titleText.fontSize = 26;
        titleText.color = GUIManager.Instance.ValheimOrange;
        titleText.alignment = TextAnchor.UpperLeft;

        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(340, 40);

        Debug.Log("Task queue title created");
    }*/

    private void CreateTask(string taskText)
    {
        GameObject taskObject = new GameObject("Task", typeof(RectTransform), typeof(Text));
        taskObject.transform.SetParent(contentPanelTaskQueue, false);

        Text taskTextComponent = taskObject.GetComponent<Text>();
        taskTextComponent.text = taskText;
        taskTextComponent.font = GUIManager.Instance.AveriaSerif;
        taskTextComponent.fontSize = 18;
        taskTextComponent.color = Color.white;
        taskTextComponent.alignment = TextAnchor.MiddleLeft;

        RectTransform taskRect = taskObject.GetComponent<RectTransform>();
        taskRect.sizeDelta = new Vector2(340, 30);

        // Create X button
        GameObject xButton = GUIManager.Instance.CreateButton(
            text: "X",
            parent: taskObject.transform,
            anchorMin: new Vector2(1, 0.5f),
            anchorMax: new Vector2(1, 0.5f),
            position: new Vector2(-10, 0),
            width: 20f,
            height: 20f);
        xButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(-10, 0);

        Debug.Log($"Task created: {taskText}");
    }

    // Call this method after adding or removing tasks to update the content size
    private void UpdateContentSize()
    {
        // Calculate the total height of all tasks
        float totalHeight = 40f; // Initial height for the "Task Queue" text
        foreach (RectTransform child in contentPanelTaskQueue)
        {
            totalHeight += child.rect.height;
        }

        // Set the content panel's height
        contentPanelTaskQueue.sizeDelta = new Vector2(contentPanelTaskQueue.sizeDelta.x, totalHeight);
    }

    private void CreateKeyBindings(Vector2 startPosition)
    {
        // Create background panel
        GameObject backgroundPanel = new GameObject("PersonalityBackground", typeof(RectTransform), typeof(Image));
        backgroundPanel.transform.SetParent(TestPanel.transform, false);

        RectTransform backgroundRect = backgroundPanel.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        /*backgroundRect.anchorMin = new Vector2(1, 1);
        backgroundRect.anchorMax = new Vector2(1, 1);*/
        backgroundRect.anchoredPosition = startPosition;
        backgroundRect.sizeDelta = new Vector2(300f, 200f); // Adjust size as needed
        backgroundRect.pivot = new Vector2(0f, 1);

        Image backgroundImage = backgroundPanel.GetComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black, adjust as needed

        string[] bindings = {
            "[Y] Open/close menu",
            "[T] Talk",
            "[G] Spawn/Reset Spawn",
            "[K] Attack",
            "[H] Harvest",
            "[F] Follow/Patrol"
        };

        GUIManager.Instance.CreateText(
            text: "Keybinds",
            parent: backgroundPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f),*/
            position: new Vector2(190f, -25f),
            //position: startPosition + new Vector2(170, 0),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        for (int i = 0; i < bindings.Length; i++)
        {
            GUIManager.Instance.CreateText(
                text: bindings[i],
                parent: backgroundPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                //position: startPosition + new Vector2(170, (-i * 20)),
                position: new Vector2(100f, -75f) + new Vector2(100, (-i * 20)),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 16,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);
        }
    }


    private void CreateMicInput(Vector2 startPosition)
    {
        // Create background panel
        GameObject backgroundPanel = new GameObject("PersonalityBackground", typeof(RectTransform), typeof(Image));
        backgroundPanel.transform.SetParent(TestPanel.transform, false);

        RectTransform backgroundRect = backgroundPanel.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        /*backgroundRect.anchorMin = new Vector2(1, 1);
        backgroundRect.anchorMax = new Vector2(1, 1);*/
        backgroundRect.anchoredPosition = startPosition;
        backgroundRect.sizeDelta = new Vector2(300f, 90f); // Adjust size as needed
        backgroundRect.pivot = new Vector2(0f, 1);

        Image backgroundImage = backgroundPanel.GetComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black, adjust as needed

        GUIManager.Instance.CreateText(
            text: "Mic Input",
            parent: backgroundImage.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /**//*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f), *//**/
            position: new Vector2(190f, -22.5f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            //color: GUIManager.Instance.ValheimOrange,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        var micDropdown = GUIManager.Instance.CreateDropDown(
            parent: backgroundPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(110f, -60f),
            fontSize: 16,
            width: 280f,
            height: 30f);

        Dropdown micDropdownComp = micDropdown.GetComponent<Dropdown>();
        List<string> truncatedOptions = Microphone.devices.ToList().Select(option => TruncateText(option, 27)).ToList();
        micDropdownComp.AddOptions(truncatedOptions);

        RectTransform dropdownRect = micDropdown.GetComponent<RectTransform>();

        // Set the pivot
        // (0,0) is bottom-left, (1,1) is top-right, (0.5,0.5) is center
        dropdownRect.pivot = new Vector2(0f, 1f);  // This sets the pivot to top-left
        dropdownRect.anchoredPosition = new Vector2(10f, -50f);


        /*// Load the saved value
        int savedIndex = PlayerPrefs.GetInt("SelectedVoiceIndex", 0);
        voiceDropdownComp.value = savedIndex;*/

        // Add listener for value change
        micDropdownComp.onValueChanged.AddListener(OnMicInputDropdownChanged);
    }

    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;
        return text.Substring(0, maxLength - 3) + "...";
    }

    private void OnMicInputDropdownChanged(int index)
    {
        Debug.Log("new MicrophoneName " + Microphone.devices[MicrophoneIndex]);
    }

    private void CreateEgoBanner(Vector2 startPosition)
    {
        // Create background panel
        GameObject backgroundPanel = new GameObject("PersonalityBackground", typeof(RectTransform), typeof(Image));
        backgroundPanel.transform.SetParent(TestPanel.transform, false);

        RectTransform backgroundRect = backgroundPanel.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        /*backgroundRect.anchorMin = new Vector2(1, 1);
        backgroundRect.anchorMax = new Vector2(1, 1);*/
        backgroundRect.anchoredPosition = startPosition;
        backgroundRect.sizeDelta = new Vector2(300f, 50f); // Adjust size as needed
        backgroundRect.pivot = new Vector2(0f, 1);

        Image backgroundImage = backgroundPanel.GetComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black, adjust as needed


        GUIManager.Instance.CreateText(
            text: "egovalheimmod.ai",
            parent: backgroundPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f),*/
            position: new Vector2(190f, -30f),
            //position: startPosition + new Vector2(170, 0),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

    }

    private void CreatePersonalitySection(Vector2 position)
    {
        // Create background panel
        GameObject backgroundPanel = new GameObject("PersonalityBackground", typeof(RectTransform), typeof(Image));
        backgroundPanel.transform.SetParent(TestPanel.transform, false);

        RectTransform backgroundRect = backgroundPanel.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        /*backgroundRect.anchorMin = new Vector2(1, 1);
        backgroundRect.anchorMax = new Vector2(1, 1);*/
        backgroundRect.anchoredPosition = position;
        backgroundRect.sizeDelta = new Vector2(420f, 220f); // Adjust size as needed
        backgroundRect.pivot = new Vector2(0.5f, 1);

        Image backgroundImage = backgroundPanel.GetComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black, adjust as needed

        GUIManager.Instance.CreateText(
            text: "Personality",
            parent: backgroundImage.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /**//*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f), *//**/
            position: new Vector2(190f, -22.5f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            //color: GUIManager.Instance.ValheimOrange,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        CreateMultilineInputField(
            parent: backgroundPanel.transform,
            placeholder: "She's strong, stoic, tomboyish, confident and serious...",
            fontSize: 14,
            width: 400,
            height: 150
        );

        /* GUIManager.Instance.CreateInputField(
            parent: backgroundPanel.transform,
            anchorMin: new Vector2(0.5f, 1f),
            anchorMax: new Vector2(0.5f, 1f),
            *//*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f),*//*
            position: new Vector2(0, -120f),
            contentType: InputField.ContentType.Standard,
            placeholderText: "She's strong, stoic, tomboyish, confident and serious... Behind her cold exterior she is soft and caring, but she's not always good at showing it. She secretly wants a husband but is not good when it comes to romance and love, very oblivious to it.",
            fontSize: 18,
            width: 400f,
            height: 150f);*/
    }

    private GameObject inputFieldObject;
    private InputField personalityInputField;
    private Text placeholderText;
    public Text personalityInputText;
    public string personalityText = "";
    
    static public List<String> npcVoices = new List<string> { 
        "Asteria",
        "Luna",
        "Stella",
        "Athena",
        "Hera",
        "Orion",
        "Arcas",
        "Perseus",
        "Orpheus",
        "Angus",
        "Helios",
        "Zeus"
    };
    public int NPCGender = 0;
    public int NPCVoice = 0;
    public float NPCVolume = 90f;
    private int MicrophoneIndex = 0;

    public void CreateMultilineInputField(Transform parent, string placeholder, int fontSize = 16, int width = 300, int height = 100)
    {
        // Create main GameObject for the input field
        inputFieldObject = new GameObject("CustomInputField");
        inputFieldObject.transform.SetParent(parent, false);

        // Add Image component for background
        Image background = inputFieldObject.AddComponent<Image>();
        background.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);

        // Create InputField component
        personalityInputField = inputFieldObject.AddComponent<InputField>();
        personalityInputField.lineType = InputField.LineType.MultiLineNewline;
        personalityInputField.onValueChanged.AddListener(OnPersonalityTextChanged);

        // Set up RectTransform
        RectTransform rectTransform = personalityInputField.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.position = rectTransform.position + new Vector3(0, -20, 0);

        // Create placeholder text
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputFieldObject.transform, false);
        placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.font = GUIManager.Instance.AveriaSerifBold;
        placeholderText.fontSize = fontSize;
        placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        // Set up placeholder RectTransform
        RectTransform placeholderTransform = placeholderText.GetComponent<RectTransform>();
        placeholderTransform.anchorMin = new Vector2(0, 0);
        placeholderTransform.anchorMax = new Vector2(1, 1);
        placeholderTransform.offsetMin = new Vector2(10, 10);
        placeholderTransform.offsetMax = new Vector2(-10, -10);

        // Create input text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputFieldObject.transform, false);
        instance.personalityInputText = textObj.AddComponent<Text>();
        instance.personalityInputText.font = GUIManager.Instance.AveriaSerifBold;
        instance.personalityInputText.fontSize = fontSize;
        instance.personalityInputText.color = Color.white;

        // Set up input text RectTransform
        RectTransform textTransform = instance.personalityInputText.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0, 0);
        textTransform.anchorMax = new Vector2(1, 1);
        textTransform.offsetMin = new Vector2(10, 10);
        textTransform.offsetMax = new Vector2(-10, -10);

        // Assign text components to InputField
        personalityInputField.placeholder = placeholderText;
        personalityInputField.textComponent = instance.personalityInputText;
    }

    private void OnPersonalityTextChanged(string newText)
    {
        instance.personalityInputText.text = newText;
        instance.personalityText = newText;
        Debug.Log("New personality " + instance.personalityInputText.text);
    }

    Dropdown voiceDropdownComp;
    Slider volumeSliderComp;

    private void CreateVoiceAndVolumeControls(Vector2 position)
    {
        // Create background panel
        GameObject backgroundPanel = new GameObject("PersonalityBackground", typeof(RectTransform), typeof(Image));
        backgroundPanel.transform.SetParent(TestPanel.transform, false);

        RectTransform backgroundRect = backgroundPanel.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        /*backgroundRect.anchorMin = new Vector2(1, 1);
        backgroundRect.anchorMax = new Vector2(1, 1);*/
        backgroundRect.anchoredPosition = position;
        backgroundRect.sizeDelta = new Vector2(420f, 120f); // Adjust size as needed
        backgroundRect.pivot = new Vector2(0.5f, 1);

        Image backgroundImage = backgroundPanel.GetComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black, adjust as needed

        GUIManager.Instance.CreateText(
            text: "Voice",
            parent: backgroundImage.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /**//*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f), *//**/
            position: new Vector2(190f, -22.5f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            //color: GUIManager.Instance.ValheimOrange,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        var voiceDropdown = GUIManager.Instance.CreateDropDown(
            parent: backgroundPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(110f, -60f),
            fontSize: 20,
            width: 200f,
            height: 30f);

        instance.voiceDropdownComp = voiceDropdown.GetComponent<Dropdown>();
        instance.voiceDropdownComp.AddOptions(npcVoices);

        /*// Load the saved value
        int savedIndex = PlayerPrefs.GetInt("SelectedVoiceIndex", 0);
        voiceDropdownComp.value = savedIndex;*/

        // Add listener for value change
        instance.voiceDropdownComp.onValueChanged.AddListener(OnNPCVoiceDropdownChanged);


        GUIManager.Instance.CreateText(
            text: "Volume",
            parent: backgroundImage.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /**//*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f), *//**/
            position: new Vector2(190f, -105f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            //color: GUIManager.Instance.ValheimOrange,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);



        var volumeSlider = CreateSlider(
            parent: backgroundPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(250f, -100f),
            width: 200f,
            height: 20f);

        instance.volumeSliderComp = volumeSlider.GetComponent<Slider>();
        instance.volumeSliderComp.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }

    private void OnNPCVoiceDropdownChanged(int index)
    {
        instance.NPCVoice = index;
        Debug.Log("new instance.NPCVoice " + instance.NPCVoice);
    }

    private void OnVolumeSliderValueChanged(float value)
    {
        instance.NPCVolume = value;
        Debug.Log("new companion volume " + instance.NPCVolume);
    }


    Toggle toggleMasculine;
    Toggle toggleFeminine;

    private void CreateBodyTypeToggle(Vector2 position)
    {
        // Create background panel
        GameObject backgroundPanel = new GameObject("PersonalityBackground", typeof(RectTransform), typeof(Image));
        backgroundPanel.transform.SetParent(TestPanel.transform, false);

        RectTransform backgroundRect = backgroundPanel.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0.5f, 1f);
        backgroundRect.anchorMax = new Vector2(0.5f, 1f);
        /*backgroundRect.anchorMin = new Vector2(1, 1);
        backgroundRect.anchorMax = new Vector2(1, 1);*/
        backgroundRect.anchoredPosition = position;
        backgroundRect.sizeDelta = new Vector2(420f, 120f); // Adjust size as needed
        backgroundRect.pivot = new Vector2(0.5f, 1);

        Image backgroundImage = backgroundPanel.GetComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 0.3f); // Semi-transparent black, adjust as needed

        GUIManager.Instance.CreateText(
            text: "Body Type",
            parent: backgroundImage.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /**//*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f), *//**/
            position: new Vector2(190f, -22.5f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            //color: GUIManager.Instance.ValheimOrange,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);


        GameObject toggleObj1 = CreateToggle(backgroundPanel.transform, "Masculine", "Masculine", -25);
        GameObject toggleObj2 = CreateToggle(backgroundPanel.transform, "Feminine", "Feminine", -55);

        instance.toggleMasculine = toggleObj1.GetComponent<Toggle>();
        instance.toggleFeminine = toggleObj2.GetComponent<Toggle>();

        instance.toggleMasculine.isOn = true;

        // Add listeners
        instance.toggleMasculine.onValueChanged.AddListener(isOn => OnToggleChanged(instance.toggleMasculine, instance.toggleFeminine, isOn));
        instance.toggleFeminine.onValueChanged.AddListener(isOn => OnToggleChanged(instance.toggleFeminine, instance.toggleMasculine, isOn));
    }

    GameObject CreateToggle(Transform parent, string name, string label, float positionY)
    {
        GameObject toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        toggleObj.transform.SetParent(parent, false);

        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(20, positionY);
        toggleRect.sizeDelta = Vector2.zero;

        Toggle toggle = toggleObj.GetComponent<Toggle>();
        CreateToggleVisuals(toggle, label);

        return toggleObj;
    }

    void CreateToggleVisuals(Toggle toggle, string label)
    {
        // Background (Circle or Rectangle)
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(toggle.transform, false);

        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 0.5f);
        backgroundRect.anchorMax = new Vector2(0, 0.5f);
        backgroundRect.anchoredPosition = new Vector2(10, -30);
        backgroundRect.sizeDelta = new Vector2(20, 20);

        Image backgroundImage = background.GetComponent<Image>();
        backgroundImage.sprite = CreateCircleSprite(); // Or use CreateRectangleSprite() for rectangular buttons
        backgroundImage.color = Color.white;

        // Checkmark
        GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(background.transform, false);

        RectTransform checkmarkRect = checkmark.GetComponent<RectTransform>();
        checkmarkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkmarkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkmarkRect.sizeDelta = Vector2.zero;

        Image checkmarkImage = checkmark.GetComponent<Image>();
        checkmarkImage.sprite = CreateCircleSprite();
        checkmarkImage.color = GUIManager.Instance.ValheimOrange;

        toggle.targetGraphic = backgroundImage;
        toggle.graphic = checkmarkImage;

        // Label
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(toggle.transform, false);

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(1, 1);
        labelRect.anchorMax = new Vector2(1, 1);
        //labelRect.anchorMax = new Vector2(1, 1);
        /*labelRect.offsetMin = new Vector2(40, 0);
        labelRect.offsetMax = new Vector2(0, 0);*/
        labelRect.anchoredPosition = new Vector2(80, -30);

        Text labelText = labelObj.GetComponent<Text>();
        labelText.text = label;
        labelText.color = Color.white;
        labelText.font = GUIManager.Instance.AveriaSerif;
        //labelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        labelText.fontSize = 18;
        labelText.alignment = TextAnchor.MiddleLeft;
    }

    Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(128, 128);
        Color[] colors = new Color[128 * 128];
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
                colors[y * 128 + x] = distance < 64 ? Color.white : Color.clear;
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateRectangleSprite()
    {
        Texture2D texture = new Texture2D(128, 128);
        Color[] colors = new Color[128 * 128];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateCheckmarkSprite()
    {
        Texture2D texture = new Texture2D(128, 128);
        Color[] colors = new Color[128 * 128];
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
            {
                if ((x > y - 30 && x < y + 10 && y > 64) || (x > 128 - y - 30 && x < 128 - y + 10 && y < 64))
                {
                    colors[y * 128 + x] = Color.white;
                }
                else
                {
                    colors[y * 128 + x] = Color.clear;
                }
            }
        }
        texture.SetPixels(colors);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    void OnToggleChanged(Toggle changedToggle, Toggle otherToggle, bool isOn)
    {
        if (isOn && otherToggle.isOn)
        {
            otherToggle.isOn = false;
        }
        instance.NPCGender = changedToggle.name == "Masculine" ? 0 : 1;

        if (instance.PlayerNPC)
        {
            VisEquipment npcVisEquipment = instance.PlayerNPC.GetComponent<VisEquipment>();
            npcVisEquipment.SetModel(instance.NPCGender);
        }
        else
        {
            Debug.Log("ontogglechanged instance.PlayerNPC is null");
        }

        Debug.Log("new NPCGender " + instance.NPCGender);
    }

    private void CreateButtons(Vector2 position)
    {
        GameObject saveButton = GUIManager.Instance.CreateButton(
            text: "Close",
            parent: TestPanel.transform,
            anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
            position: position + new Vector2(0, 50),
            width: 250f,
            height: 40f);

        Button saveButtonComp = saveButton.GetComponent<Button>();
        saveButtonComp.onClick.AddListener(() => OnSaveButtonClick(saveButtonComp));

        /*GUIManager.Instance.CreateButton(
            text: "Save",
            parent: TestPanel.transform,
            anchorMin: new Vector2(0.5f, 0.5f),
                anchorMax: new Vector2(0.5f, 0.5f),
            position: position,
            width: 100f,
            height: 40f);*/
    }

    private void OnSaveButtonClick(Button button)
    {
        TestPanel.SetActive(false);
        instance.IsModMenuShowing = false;
        GUIManager.BlockInput(false);
        if (instance.PlayerNPC)
            SaveNPCData(instance.PlayerNPC);
    }

    // Make sure to include your existing CreateTask and CreateSlider methods here

    private Slider CreateSlider(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width, float height)
    {
        GameObject sliderObject = new GameObject("VolumeSlider", typeof(RectTransform));
        sliderObject.transform.SetParent(parent, false);

        RectTransform rectTransform = sliderObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 100f;
        slider.value = 90f; // Default value

        // Create background
        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderObject.transform, false);
        background.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f);
        RectTransform backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.sizeDelta = Vector2.zero;

        // Create fill area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = Vector2.zero;

        // Create fill
        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        fill.GetComponent<Image>().color = GUIManager.Instance.ValheimOrange;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;

        // Create handle slide area
        GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleSlideAreaRect = handleSlideArea.GetComponent<RectTransform>();
        handleSlideAreaRect.anchorMin = Vector2.zero;
        handleSlideAreaRect.anchorMax = Vector2.one;
        handleSlideAreaRect.sizeDelta = Vector2.zero;

        // Create handle
        GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleSlideArea.transform, false);
        handle.GetComponent<Image>().color = Color.white;
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.sizeDelta = new Vector2(20, 0);

        // Set up slider components
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();

        return slider;
    }
}




/*
 * 
 * 
 * UI
 * 
 * 
 */
