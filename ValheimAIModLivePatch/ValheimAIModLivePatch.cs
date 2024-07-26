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
using UnityEngine.EventSystems;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine.Assertions.Must;
using System.Security.Policy;

[BepInPlugin("egovalheimmod.ValheimAIModLivePatch", "EGO.AI Valheim AI NPC Mod Live Patch", "0.0.1")]
[BepInProcess("valheim.exe")]
[BepInDependency("egovalheimmod.ValheimAIModLoader", BepInDependency.DependencyFlags.HardDependency)]
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
    private ConfigEntry<KeyboardShortcut> SendRecordingToBrainKey;

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

    //private List<GameObject> AllGOInstances = new List<GameObject>();
    private GameObject[] AllGOInstances = {};
    private float AllGOInstancesLastRefresh = 0f;

    private List<GameObject> AllTreeInstances = new List<GameObject>();
    //private float AllTreeInstancesLastRefresh = 0f;

    private List<GameObject> AllDestructibleInstances = new List<GameObject>();
    //private float AllDestructibleInstancesLastRefresh = 0f;

    private List<GameObject> AllPickableInstances = new List<GameObject>();
    //private float AllPickableInstancesLastRefresh = 0f;
    



    private GameObject[] SmallTrees;

    // NPC VARS
    private NPCCommand.CommandType eNPCMode;

    private float FollowUntilDistance = .5f;
    private float RunUntilDistance = 3f;
    public Vector3 patrol_position = Vector3.zero;
    public float patrol_radius = 10f;
    public bool patrol_harvest = false;
    public string CurrentEnemyName = "Greyling";
    public string CurrentHarvestResourceName = "Beech";
    public string CurrentWeaponName = "";
    public bool MovementLock = false;
    public float chaseUntilPatrolRadiusDistance = 20f;

    public bool PlayingAnim = false;

    private AudioClip recordedAudioClip;
    public bool IsRecording = false;
    private float recordingStartedTime = 0f;
    private bool shortRecordingWarningShown = false;
    public bool IsModMenuShowing = false;


    private void Awake()
    {
        Debug.Log("ValheimAIModLivePatch Loaded!");
        instance = this;

        ConfigBindings();

        /*PopulateCraftingRequirements();
        PopulateBuildingRequirements();
        PopulateMonsterPrefabs();
        PopulateAllItems();*/

        instance.FindPlayerNPCs();

        playerDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "playerdialogue.wav");
        npcDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue.wav");
        npcDialogueRawAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");

        Chat.instance.SendText(Talker.Type.Normal, "EGO.AI MOD LOADED!");

        CreateModMenuUI();

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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
    private static bool Prefix(ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
    {
        //Debug.Log("enter RemoveObjects");
        byte b = (byte)((uint)Time.frameCount & 0xFFu);
        
        foreach (ZDO currentNearObject in currentNearObjects)
        {
            currentNearObject.TempRemoveEarmark = b;
        }
        //Debug.Log("after 1 loop");
        foreach (ZDO currentDistantObject in currentDistantObjects)
        {
            currentDistantObject.TempRemoveEarmark = b;
        }
        __instance.m_tempRemoved.Clear();
        //Debug.Log("after 2 loop n clear");
        foreach (ZNetView value in __instance.m_instances.Values)
        {
            if (value && value.GetZDO() != null && value.GetZDO().TempRemoveEarmark != b)
            {
                __instance.m_tempRemoved.Add(value);
            }
        }
        //Debug.Log("after 3 loop");
        for (int i = 0; i < __instance.m_tempRemoved.Count; i++)
        {
            ZNetView zNetView = __instance.m_tempRemoved[i];
            ZDO zDO = zNetView.GetZDO();
            zNetView.ResetZDO();
            UnityEngine.Object.Destroy(zNetView.gameObject);
            if (!zDO.Persistent && zDO.IsOwner())
            {
                ZDOMan.instance.DestroyZDO(zDO);
            }
            __instance.m_instances.Remove(zDO);
        }
        //Debug.Log("after 4 loop");

        return false;
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
        SendRecordingToBrainKey = Config.Bind<KeyboardShortcut>("Keybinds", "SendRecordingToBrainKey", new KeyboardShortcut(KeyCode.Y), "The key used to ");

        MicrophoneIndex = Config.Bind<int>("Integer", "MicrophoneIndex", 0, "Input device index in Windows Sound Settings.");
        CompanionVolume = Config.Bind<float>("Float", "CompanionVolume", 1f, "NPC dialogue volume (0-1)");*/
        DisableAutoSave = Config.Bind<bool>("Bool", "DisableAutoSave", false, "Disable auto saving the game world?");
    }

    private void OnDestroy() 
    {
        /*TestPanel.SetActive(false);
        Destroy(TestPanel);*/

        if (panelManager != null)
        {
            panelManager.DestroyAllPanels();
        }

        harmony.UnpatchSelf();
    }

    private void OnUnload()
    {
        if (panelManager != null)
        {
            panelManager.DestroyAllPanels();
        }
        // ... any other cleanup code
    }

    //private bool isTextFieldFocused = false;

    // PROCESS PLAYER INPUT
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), "Update")]
    private static void Player_Update_Postfix(Player __instance)
    {
        if (EventSystem.current.currentSelectedGameObject != null && EventSystem.current.currentSelectedGameObject.GetComponent<UnityEngine.UI.InputField>())
        {
            //Debug.Log("Ignoring input: a text field is in focus");
            return;
        }

        if (!ZNetScene.instance || !Player.m_localPlayer)
        {
            // Player is not in a world, allow input
            //Debug.Log("Ignoring input: player is not in a world");
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.Y))
        {
            //Debug.Log("Mod Menu Toggled, new visibility: " + instance.IsModMenuShowing);
            //instance.TogglePanel();
            instance.panelManager.TogglePanel("Settings");
            instance.panelManager.TogglePanel("Thrall Customization");

            return;
        }

        if (ZInput.GetKeyDown(KeyCode.E) && instance.PlayerNPC && instance.PlayerNPC.transform.position.DistanceTo(__instance.transform.position) < 5)
        {
            Debug.Log("Trying to access NPC inventory");
            instance.OnInventoryKeyPressed(__instance);
            return;
        }

        if (Menu.IsVisible() || Console.IsVisible() || Chat.instance.HasFocus() || instance.IsModMenuShowing)
        {
            //Debug.Log("Menu visible");
            //Debug.Log("Ignoring input: Menu, console, chat or mod menu is showing");
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Keybind: Spawn Companion");
            instance.SpawnCompanion();
            instance.StartFollowing(__instance);
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Keybind: Dismiss Companion");
            //Console.instance.TryRunCommand("despawn_all");
            if (!instance.PlayerNPC)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "There isn't an NPC in the world!");
                return;
            }

            SaveNPCData(instance.PlayerNPC);
            Destroy(instance.PlayerNPC);
            instance.PlayerNPC = null;

            return;
        }

        if (ZInput.GetKeyDown(KeyCode.F))
        {
            if (instance.eNPCMode == NPCCommand.CommandType.Idle || instance.eNPCMode == NPCCommand.CommandType.PatrolArea)
            {
                Debug.Log("Keybind: Follow Player");
                instance.Follow_Start(__instance.gameObject);
            }
            else if (instance.eNPCMode == NPCCommand.CommandType.FollowPlayer)
            {
                Debug.Log("Keybind: Patrol Area");
                instance.Patrol_Start();
            }
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.H))
        {
            if (instance.eNPCMode == NPCCommand.CommandType.HarvestResource)
                instance.Harvesting_Stop();
            else
                instance.Harvesting_Start("Beech_small");
            return;
        }

        /*if (ZInput.GetKeyDown(KeyCode.K))
        {
            if (instance.eNPCMode == NPCCommand.CommandType.CombatSneakAttack || instance.eNPCMode == NPCCommand.CommandType.CombatAttack)
                instance.Combat_StopAttacking();
            else
                instance.Combat_StartAttacking(null);
            return;
        }*/

        if (ZInput.GetKey(KeyCode.T) && !instance.IsRecording)
        {
            instance.StartRecording();
            return;
        }
        else if (!ZInput.GetKey(KeyCode.T) && instance.IsRecording)
        {
            if (Time.time - instance.recordingStartedTime > 1f)
            {
                instance.shortRecordingWarningShown = false;
                instance.StopRecording();
                instance.SendRecordingToBrain();
            }
            else if (!instance.shortRecordingWarningShown)
            {
                //Debug.Log("Recording was too short. Has to be atleast 1 second long");
                //MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Recording must be atleast 1 second long.");
                instance.shortRecordingWarningShown = true;
            }
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.L))
        {
            GameObject s = FindClosestItemDrop(Player.m_localPlayer.gameObject);
            //Debug.Log("FindClosestItemDrop: " + s.name);

            return;
        }

        //instance.PlayRecordedAudio("");
        //instance.LoadAndPlayAudioFromBase64(instance.npcDialogueAudioPath);
        //instance.PlayWavFile(instance.npcDialogueRawAudioPath);
    }


    float LastFindClosestItemDropTime = 0f;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    private static bool MonsterAI_CustomFixedUpdate_Prefix(MonsterAI __instance)
    {
        if (!__instance.name.Contains("HumanoidNPC")) return true;


        HumanoidNPC humanoidNPC = __instance.gameObject.GetComponent<HumanoidNPC>();
        GameObject newfollow = null;


        if (Time.time > instance.LastFindClosestItemDropTime + 3)
        {
            Debug.Log("trying to find item drop");

            newfollow = FindClosestItemDrop(__instance.gameObject);

            if (newfollow != null && newfollow != __instance.m_follow && newfollow.transform.position.DistanceTo(__instance.transform.position) < 7f)
            {
                Debug.Log($"Going to pickup nearby dropped item on the ground {newfollow.name}");
                __instance.SetFollowTarget(newfollow);
                return true;
            }
        }

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

            //Debug.Log("LastPositionDelta " + humanoidNPC.LastPositionDelta);
            if (humanoidNPC.LastPositionDelta > 2.5f && !humanoidNPC.InAttack() && humanoidNPC.GetTimeSinceLastAttack() > 1f)
            {
                humanoidNPC.StartAttack(humanoidNPC, false);
            }

            if (__instance.m_follow == null || __instance.m_follow.HasAnyComponent("Character", "Humanoid"))
            {
                newfollow = FindClosestResource(__instance.gameObject, instance.CurrentHarvestResourceName);

                if (newfollow != null)
                    __instance.SetFollowTarget(newfollow);
            }
        }

        else if (instance.eNPCMode == NPCCommand.CommandType.CombatAttack)
        {
            if (__instance.m_follow == null || !__instance.m_follow.HasAnyComponent("Character", "Humanoid", "BaseAI", "MonsterAI", "AnimalAI"))
            {
                newfollow = FindClosestEnemy(__instance.gameObject, instance.CurrentEnemyName);

                if (newfollow != null)
                {
                    __instance.SetFollowTarget(newfollow);
                    Debug.Log("New enemy target " + newfollow.name);
                }
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HumanoidNPC), "CustomFixedUpdate")]
    private static void HumanoidNPC_CustomFixedUpdate_Prefix(HumanoidNPC __instance)
    {
        Minimap.PinData tbd = null;
        foreach (Minimap.PinData pd in Minimap.instance.m_pins)
        {
            if (pd.m_author == "NPC")
                tbd = pd;
        }

        if (tbd != null)
        {
            Minimap.instance.RemovePin(tbd);
        }
    }



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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "GetHoverText")]
    private static bool Character_GetHoverText_Prefix(Character __instance, ref string __result)
    {
        if (__instance.name.Contains("HumanoidNPC"))
        {
            HumanoidNPC humanoidNPC_component = __instance.GetComponent<HumanoidNPC>();

            __result = __instance.m_name;
            __result += "\n<color=yellow><b>[E]</b></color> Inventory";
            __result += "\n<color=yellow><b>[T]</b></color> Push to Talk";
            __result += "\n<color=yellow><b>[Y]</b></color> Menu";

            return false; // Skip original method
        }

        return true; // Continue to original method
    }

    /*// OVERRIDE NPC OVERLAY HUD
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
    }*/

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

            if (instance.PlayerNPC)
            {
                MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
                HumanoidNPC humanoidComponent = instance.PlayerNPC.GetComponent<HumanoidNPC>();

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
            Debug.Log("Spawning more than one NPC is disabled");
            return;
        }
        Player localPlayer = Player.m_localPlayer;
        GameObject npcPrefab = ZNetScene.instance.GetPrefab("HumanoidNPC");

        

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

        VisEquipment npcInstanceVis = npcInstance.GetComponent<VisEquipment>();
        VisEquipment playerInstanceVis = localPlayer.GetComponent<VisEquipment>();

        npcInstanceVis.m_isPlayer = true;
        npcInstanceVis.m_emptyBodyTexture = playerInstanceVis.m_emptyBodyTexture;
        npcInstanceVis.m_emptyLegsTexture = playerInstanceVis.m_emptyLegsTexture;

        instance.PlayerNPC = npcInstance;

        /*if (npcInstance.HasAnyComponent("Tameable"))
        {
            Debug.Log("removing npc tameable comp");
            Destroy(npcInstance.GetComponent<Tameable>());
        }*/

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


            Character character2 = humanoidNpc_Component;
            character2.m_onDeath = (Action)Delegate.Combine(character2.m_onDeath, new Action(OnNPCDeath));


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
            humanoidNpc_Component.m_runSpeed = localPlayer.m_runSpeed;

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

    protected virtual void OnNPCDeath()
    {
        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Your NPC died!");

        HumanoidNPC humanoidNPC = instance.PlayerNPC.GetComponent<HumanoidNPC>();

        PrintInventoryItems(humanoidNPC.m_inventory);

        SaveNPCData(instance.PlayerNPC);
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

        instance.CurrentEnemyName = EnemyName;

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

        instance.CurrentWeaponName = ItemName;

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

        instance.CurrentHarvestResourceName = CleanKey(ResourceName);
        Debug.Log("trying to harvest resource: " + instance.CurrentHarvestResourceName);

        //ResourceName = "Beech";
        GameObject resource = FindClosestResource(instance.PlayerNPC, instance.CurrentHarvestResourceName);
        if (resource == null)
        {
            // inform API that resource was not found and wasn't processed
            Debug.Log($"couldn't find resource: {resource}");
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
    private void SendRecordingToBrain()
    {
        if (instance.IsRecording)
        {
            instance.StopRecording();
        }

        //GameObject[] allNpcs = FindPlayerNPCs();
        if (instance.PlayerNPC)
        {
            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidComponent = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            //Debug.Log("BrainSendInstruction");
            BrainSendInstruction(instance.PlayerNPC);
            instance.lastSentToBrainTime = Time.time;

            AddChatTalk(humanoidComponent, "NPC", "...");
        }
    }

    static bool IsInventoryShowing = false;
    private void OnInventoryKeyPressed(Player player)
    {
        if (instance.PlayerNPC)
        {
            if (IsInventoryShowing)
            {
                InventoryGui.instance.Hide();
                IsInventoryShowing = false;
            }
            else
            {
                HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                InventoryGui.instance.Show(humanoidNPC_component.inventoryContainer);
                PrintInventoryItems(humanoidNPC_component.m_inventory);
                IsInventoryShowing = true;
            }
        }
        else
        {
            Debug.Log("OnInventoryKeyPressed instance.PlayerNPC is null ");
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
                //humanoidNPC.m_inventory.RemoveOneItem(item);
                humanoidNPC.m_inventory.RemoveItem(item, item.m_stack);
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
        text = text.TrimStart('\n');

        UserInfo userInfo = new UserInfo();
        userInfo.Name = character.m_name;
        Vector3 headPoint = character.GetEyePoint() + (Vector3.up * -100f);
        Chat.instance.AddInworldText(character.gameObject, 0, headPoint, Talker.Type.Shout, userInfo, text);
        if (text != "...")
        {
            Chat.instance.AddString(character.m_name, text, Talker.Type.Normal);
            Chat.instance.m_hideTimer = 0f;
            Chat.instance.m_chatWindow.gameObject.SetActive(value: true);
        }
    }

    public void BrainSynthesizeAudio(string text, string voice)
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

            //AddChatTalk(npc, "NPC", text);
            DownloadAudioFile(audio_file_id);
        }
        catch (Exception ex)
        {
            Debug.Log($"Failed to parse JSON: {ex.Message}");
        }

        instance.previewVoiceButton.SetActive(true);
    }

    private void BrainSendPeriodicUpdate(GameObject npc)
    {
        string jsonData = GetJSONForBrain(npc, false);

        WebClient webClient = new WebClient();
        webClient.Headers.Add("Content-Type", "application/json");

        webClient.UploadStringAsync(new System.Uri($"{brainBaseURL}/instruct_agent"), jsonData);
        webClient.UploadStringCompleted += OnBrainSendPeriodicUpdateResponse;
    }

    private void OnBrainSendPeriodicUpdateResponse(object sender, UploadStringCompletedEventArgs e)
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
                for (int i = 0; i < agentCommands.Count; i++)
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

                    string[] parameters = { };
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

                    Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

                    AddItemToScrollBox(TaskListScrollBox, $"{action} {category} ({p})", defaultSprite);
                }
            }
            else
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                AddChatTalk(npc, "NPC", agent_text_response);
                Debug.Log("No agent commands found.");
            }

            Debug.Log("Brain periodic update response: " + responseJson);
        }
        else
        {
            Debug.LogError("Request failed: " + e.Error.Message);
        }
    }

    private void BrainSendInstruction(GameObject npc)
    {
        string jsonData = GetJSONForBrain(npc);

        //Debug.Log("jsonData\n " + jsonData);

        // Create a new WebClient
        WebClient webClient = new WebClient();
        webClient.Headers.Add("Content-Type", "application/json");

        // Send the POST request
        webClient.UploadStringAsync(new System.Uri($"{brainBaseURL}/instruct_agent"), jsonData);
        webClient.UploadStringCompleted += OnBrainSendInstructionResponse;
    }

    private void OnBrainSendInstructionResponse(object sender, UploadStringCompletedEventArgs e)
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
                        AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
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

                    Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

                    DeleteAllTasks();
                    AddItemToScrollBox(TaskListScrollBox, $"{action} {category} ({p})", defaultSprite);
                }
            }
            else
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
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
                .Where(go => go.HasAnyComponent("MonsterAI", "BaseAI", "AnimalAI"))
                .ToArray();
        AllEnemiesInstancesLastRefresh = Time.time;
        return instance.AllEnemiesInstances;
    }

    private static GameObject FindClosestEnemy(GameObject character, string EnemyName)
    {
        //return GameObject.FindObjectsOfType<GameObject>(true)
            return instance.FindEnemies()
                //.Where(go => go.name.Contains(EnemyName) && go.HasAnyComponent("Character", "Humanoid" , "BaseAI", "MonsterAI"))
                .Where(go => go.name.StartsWith(CleanKey(EnemyName)))
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

    static int AllGOInstancesRefreshRate = 30;
    private static bool CanAccessAllGameInstances()
    {
        if (Time.time > instance.AllGOInstancesLastRefresh + AllGOInstancesRefreshRate || instance.AllGOInstancesLastRefresh == 0)
        {
            RefreshAllGameObjectInstances();
        }

        if (instance.AllGOInstances.Length > 0)
        {
            return true;
        }

        return false;
    }

    private static void RefreshAllGameObjectInstances()
    {
        if (!instance.PlayerNPC)
        {
            return;
        }

        instance.AllGOInstances = GameObject.FindObjectsOfType<GameObject>(false)
                .Where(go => go != null && go.transform.position.DistanceTo(instance.PlayerNPC.transform.position) < 300 && go.HasAnyComponent("ItemDrop", "Pickable", "Character", "Destructible", "TreeBase", "MineRock"))
                .ToArray();
                //.ToList();
        instance.AllGOInstancesLastRefresh = Time.time;

        Debug.Log($"RefreshAllGameObjectInstances len {instance.AllGOInstances.Count()}");

        RefreshPickables();
    }

    private static void RefreshPickables()
    {
        instance.AllPickableInstances = instance.AllGOInstances.Where(go => go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")).ToList();
        /*GameObject[] pickables = GameObject.FindObjectsOfType<GameObject>(false)
                //.Where(go => go != null && !ZoneSystem.instance.IsBlocked(go.transform.position) && (go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")))
                .Where(go => go != null  && (go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")))
                .ToArray();
        instance.AllPickableInstances.Clear();
        foreach (GameObject pickable in pickables)
        {
            instance.AllPickableInstances.Add(pickable);
        }*/
        //instance.AllPickableInstancesLastRefresh = Time.time;
        //Debug.Log("pickables len " + instance.AllPickableInstances.Count());
    }

    private static GameObject FindClosestPickableResource(GameObject character, Vector3 p_position, float radius)
    {
        /*if (!(instance.AllPickableInstances.Count > 0 && Time.time - instance.AllPickableInstancesLastRefresh < 30f && instance.AllPickableInstancesLastRefresh != 0f))
        {
            //Debug.Log("Updated AllPickableInstances");
            RefreshPickables();
        }*/

        if (CanAccessAllGameInstances())
        {
            GameObject[] nearbyPickables = instance.AllPickableInstances
            .Where(t => t != null && Vector3.Distance(p_position, t.transform.position) <= radius)
            .OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
            .ToArray();

            if (nearbyPickables.Length > 0 && nearbyPickables[0] != null)
            {
                return nearbyPickables[0];
            }
        }

        /*IOrderedEnumerable<GameObject> results = instance.AllPickableInstances.ToArray()
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
              
        return null;*/

        Debug.Log("FindClosestResource returning null");
        return null;
    }

    private static GameObject FindClosestResource(GameObject character, string ResourceName)
    {
        //return GameObject.FindObjectsOfType<GameObject>(true)

        if (CanAccessAllGameInstances())
        {
            return instance.AllGOInstances
                //.Where(go => go.name.Contains(ResourceName) && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
                .Where(go => CleanKey(go.name) == ResourceName && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
                .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .FirstOrDefault();
        }

        Debug.Log("FindClosestResource returning null");
        return null;
    }

    private static GameObject FindClosestItemDrop(GameObject character)
    {
        //return GameObject.FindObjectsOfType<GameObject>(true)

        if (CanAccessAllGameInstances())
        {
            instance.LastFindClosestItemDropTime = Time.time;

            Debug.Log("instance.AllGOInstances len " + instance.AllGOInstances.Count());

            /*IOrderedEnumerable<GameObject> results = instance.AllGOInstances
            .Where(go => go.HasAnyComponent("ItemDrop"))
            .OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position));*/


            GameObject[] allItemDrops = instance.AllGOInstances.Where(go => go != null && go.HasAnyComponent("ItemDrop"))
                .OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .ToArray();
            Debug.Log("allItemDrops len " + allItemDrops.Count());
            if (allItemDrops.Length > 0)
            {
                Debug.Log($"nearby ItemDrop {allItemDrops[0].name}");
                return allItemDrops[0];
            }

            

            //GameObject result = GetFirstFromOrderedEnumerable(results);

            //Debug.Log("result " + result.name);


            //Debug.Log("result2 " + results.Count());
            /*if (results != null && results.Count() > 0)
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
                            Debug.Log("result == " +  result.name);
                            return null;
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
                Debug.Log("No more item drops");
            }*/

            return null;

            /*GameObject[] nearbyItemDrops = instance.AllGOInstances
                //.Where(go => go.name.Contains(ResourceName) && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
                .Where(go => go.HasAnyComponent("ItemDrop"))
                .OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .ToArray();
            //.FirstOrDefault();

            Debug.Log("nearbyItemDrops len " + nearbyItemDrops.Count());
            return null;*/
        }

        Debug.Log("FindClosestItemDrop returning null");
        return null;
    }

    private GameObject FindClosestTreeFor(GameObject go, string TreeType = "small")
    {
        if (CanAccessAllGameInstances())
        {
            return instance.AllGOInstances
                .OrderBy(t => Vector3.Distance(go.transform.position, t.transform.position))
                .FirstOrDefault();
        }

        Debug.Log("FindClosestTreeFor returning null");
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

    public static string CleanKey(string key)
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
        {
            if (character.name.Contains("Player") || character.name.Contains("HumanoidNPC"))
                continue;
            ProcessResource(character, character.name);
        }

        /*foreach (Humanoid humanoid in humanoids)
            ProcessResource(humanoid, humanoid.name);*/

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

    private int recordingLength = 10; // Maximum recording length in seconds
    private int sampleRate = 22050; // Reduced from 44100
    private int bitDepth = 8; // Reduced from 16

    private void StartRecording()
    {
        string micName = null;
        if (instance.MicrophoneIndex < 0 || instance.MicrophoneIndex >= Microphone.devices.Count())
            micName = Microphone.devices[instance.MicrophoneIndex];
        instance.recordedAudioClip = Microphone.Start(micName, false, recordingLength, sampleRate);
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
        audioSource.volume = instance.npcVolume;
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

    public static string GetJSONForBrain(GameObject character, bool includeRecordedAudio = true)
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

        /*var playerInventoryItems = new JsonArray();
        foreach (ItemDrop.ItemData item in Player.m_localPlayer.m_inventory.m_inventory)
        {
            var itemData = new JsonObject
            {
                ["name"] = item.m_shared.m_name,
                ["amount"] = item.m_stack,
            };
            playerInventoryItems.Add(itemData);
        }*/

        var gameState = new JsonObject
        {
            ["Name"] = humanoidNPC.m_name,
            ["Health"] = humanoidNPC.GetHealth(),
            ["Stamina"] = humanoidNPC.m_stamina,
            ["Inventory"] = npcInventoryItems,
            //["PlayerInventory"] = playerInventoryItems,
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
        string base64audio = includeRecordedAudio ? instance.GetBase64FileData(instance.playerDialogueAudioPath) : "";

        var jsonObject = new JsonObject
        {
            ["player_id"] = humanoidNPC.GetZDOID().ToString(),
            ["game_state"] = gameState,
            ["player_instruction_audio_file_base64"] = base64audio,
            ["timestamp"] = Time.time,
            ["personality"] = instance.npcPersonality,
            ["voice"] = npcVoices[instance.npcVoice].ToLower(),
            ["gender"] = instance.npcGender,
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
        data["personality"] = instance.npcPersonality;
        data["voice"] = instance.npcVoice;
        data["gender"] = instance.npcGender;
        data["MicrophoneIndex"] = instance.MicrophoneIndex;
        

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
        data["hairColor"] = hairColorArray;

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

            
            instance.npcName = data["name"].ToString();

            instance.npcPersonality = data["personality"].ToString();

            instance.npcVoice = int.Parse(data["voice"].ToString());
            

            instance.npcGender = int.Parse(data["gender"].ToString());

            instance.MicrophoneIndex = int.Parse(data["MicrophoneIndex"].ToString());

            // Load skin color
            JsonArray skinColorArray = data["skinColor"] as JsonArray;
            if (skinColorArray.Count == 3)
            {
                instance.skinColor = new Color(
                    float.Parse(skinColorArray[0].ToString()),
                    float.Parse(skinColorArray[1].ToString()),
                    float.Parse(skinColorArray[2].ToString())
                );

            }

            // Load skin color
            JsonArray hairColorArray = data["hairColor"] as JsonArray;
            if (hairColorArray.Count == 3)
            {
                instance.hairColor = new Color(
                    float.Parse(hairColorArray[0].ToString()),
                    float.Parse(hairColorArray[1].ToString()),
                    float.Parse(hairColorArray[2].ToString())
                );
            }

            ApplyNPCData(npc);


            // Load inventory
            JsonArray inventoryArray = data["inventory"] as JsonArray;
            npc.m_inventory.RemoveAll();
            npc.GetInventory().RemoveAll();
            npc.m_inventory.m_inventory.Clear();
            foreach (JsonObject itemData in inventoryArray)
            {
                string itemName = itemData["name"].ToString();
                int stack = int.Parse(itemData["stack"].ToString());


                string prefabRealName = TransformToPrefabName(LocalizationManager.Instance.TryTranslate(itemName));

                Debug.Log($"trying to add to inventory: {itemName} x{stack} {prefabRealName}");
                

                GameObject itemPrefab = ZNetScene.instance.GetPrefab(prefabRealName);
                if (itemPrefab != null)
                {
                    ItemDrop.ItemData itemdata = npc.PickupPrefab(itemPrefab, stack);
                    /*if (itemdata.IsEquipable())
                    {
                        npc.EquipItem(itemdata);
                        Debug.Log($"equipable: {itemName} x{stack}");
                    }
                    else
                    {
                        npc.GetInventory().AddItem(itemPrefab.gameObject, stack);
                        Debug.Log($"non equipable: {itemName} x{stack}");
                    }*/
                }
            }

            Debug.Log($"NPC data loaded for {npc.m_name}");
        }
        else
        {
            Debug.LogWarning("No saved NPC data found.");
        }
    }

    static string TransformToPrefabName(string localizedName)
    {
        // Split the name into words
        string[] words = localizedName.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Capitalize the first letter of each word and join them
        string prefabName = string.Join("", words.Select(word => char.ToUpper(word[0]) + word.Substring(1)));

        return prefabName;
    }

    public static void ApplyNPCData(HumanoidNPC npc)
    {
        npc.m_name = instance.npcName;
        instance.nameInputField.SetTextWithoutNotify(instance.npcName);
        instance.personalityInputField.SetTextWithoutNotify(instance.npcPersonality);
        instance.voiceDropdownComp.SetValueWithoutNotify(instance.npcVoice);
        instance.micDropdownComp.SetValueWithoutNotify(instance.MicrophoneIndex);
        if (instance.npcGender == 0)
        {
            instance.toggleMasculine.isOn = true;
            instance.toggleFeminine.isOn = false;
        }
        else
        {
            instance.toggleMasculine.isOn = false;
            instance.toggleFeminine.isOn = true;
        }

        npc.m_visEquipment.SetHairColor(new Vector3(
            instance.hairColor.r,
            instance.hairColor.g,
            instance.hairColor.b
        ));

        npc.m_visEquipment.SetSkinColor(new Vector3(
            instance.skinColor.r,
            instance.skinColor.g,
            instance.skinColor.b
        ));

        
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

    int MenuTitleFontSize = 36;
    int MenuSectionTitleFontSize = 24;
    Vector2 MenuSectionTitlePosition = new Vector2(10f, -5f);

    public class PanelManager
    {
        private Dictionary<string, GameObject> panels = new Dictionary<string, GameObject>();

        public GameObject CreatePanel(string panelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width, float height, bool draggable, Vector2 pivot = new Vector2())
        {
            if (panels.ContainsKey(panelName))
            {
                Debug.Log($"Panel {panelName} already exists.");
                return panels[panelName];
            }

            if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
            {
                Debug.Log("GUIManager instance or CustomGUI is null");
                return null;
            }

            GameObject panel = GUIManager.Instance.CreateWoodpanel(
                parent: GUIManager.CustomGUIFront.transform,
                anchorMin: anchorMin,
                anchorMax: anchorMax,
                position: position,
                width: width,
                height: height,
                draggable: draggable);

            RectTransform rectTransform = panel.GetComponent<RectTransform>();
            rectTransform.pivot = pivot;

            AddTitleText(panel, panelName);

            panel.SetActive(false);
            panels[panelName] = panel;

            return panel;
        }

        private void AddTitleText(GameObject panel, string title)
        {
            // Create a new GameObject for the text
            GameObject titleObject = new GameObject("PanelTitle");
            titleObject.transform.SetParent(panel.transform, false);

            // Add Text component
            Text titleText = titleObject.AddComponent<Text>();
            titleText.text = title.ToUpper();
            titleText.font = GUIManager.Instance.NorseBold;
            titleText.fontSize = instance.MenuTitleFontSize;
            titleText.color = GUIManager.Instance.ValheimOrange;
            titleText.alignment = TextAnchor.MiddleCenter;

            // Set up RectTransform for the text
            RectTransform rectTransform = titleText.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.anchoredPosition = new Vector2(0, -40);
            rectTransform.sizeDelta = new Vector2(0, 40);
            rectTransform.pivot = new Vector2(0, 1);
        }

        public void TogglePanel(string panelName)
        {
            if (!panels.ContainsKey(panelName))
            {
                Debug.Log($"Panel {panelName} does not exist.");
                return;
            }

            GameObject panel = panels[panelName];
            bool state = !panel.activeSelf;

            panel.SetActive(state);
            // Assuming instance is accessible, you might need to adjust this
            instance.IsModMenuShowing = state;

            GUIManager.BlockInput(state);
        }

        public void DestroyAllPanels()
        {
            foreach (var panel in panels.Values)
            {
                if (panel != null)
                {
                    GameObject.Destroy(panel);
                }
            }
            panels.Clear();
        }


        public GameObject CreateSubPanel(GameObject parentPanel, string subPanelName, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, float width, float height, Vector2 pivot = new Vector2())
        {
            GameObject subPanel = new GameObject(subPanelName);
            RectTransform rectTransform = subPanel.AddComponent<RectTransform>();
            Image image = subPanel.AddComponent<Image>();

            // Set up the RectTransform
            rectTransform.SetParent(parentPanel.transform, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = new Vector2(width, height);
            rectTransform.pivot = pivot;

            // Set up the Image component for the black background
            image.color = new Color(0, 0, 0, 0.5f); // Opaque black with slight transparency

            return subPanel;
        }
    }

    private PanelManager panelManager = new PanelManager();
    private GameObject settingsPanel;
    private GameObject thrallCustomizationPanel;


    private GameObject taskQueueSubPanel;
    private GameObject keybindsSubPanel;
    private GameObject micInputSubPanel;
    private GameObject egoBannerSubPanel;


    private GameObject npcNameSubPanel;
    private GameObject npcPersonalitySubPanel;
    private GameObject npcVoiceSubPanel;
    private GameObject npcBodyTypeSubPanel;
    private GameObject npcAppearanceSubPanel;


    
    public string npcName = "";
    public string npcPersonality = "";
    public int npcPersonalityIndex = 0;

    public int npcGender = 0;
    public int npcVoice = 0;
    public float npcVolume = 90f;
    public int MicrophoneIndex = 0;

    public Color skinColor;
    public Color hairColor;

    private void CreateModMenuUI()
    {
        float TopOffset = 375f;

        settingsPanel = panelManager.CreatePanel(
            "Settings",
            anchorMin: new Vector2(0f, .5f),
            anchorMax: new Vector2(0f, .5f),
            position: new Vector2(100, TopOffset),
            width: 480,
            height: 700,
            draggable: false,
            pivot: new Vector2(0, 1f)
        );

        thrallCustomizationPanel = panelManager.CreatePanel(
            "Thrall Customization",
            anchorMin: new Vector2(1f, .5f),
            anchorMax: new Vector2(1f, .5f),
            position: new Vector2(-100, TopOffset),
            width: 450,
            height: 880,
            draggable: false,
            pivot: new Vector2(1, 1f)
        );

        taskQueueSubPanel = panelManager.CreateSubPanel(settingsPanel, "Task Queue", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100f), 430, 180, pivot: new Vector2(0.5f, 1f));
        keybindsSubPanel = panelManager.CreateSubPanel(taskQueueSubPanel, "Keybinds", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -200f), 430, 170, pivot: new Vector2(0.5f, 1f));
        micInputSubPanel = panelManager.CreateSubPanel(keybindsSubPanel, "Mic Input", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -190f), 430, 80, pivot: new Vector2(0.5f, 1f));
        egoBannerSubPanel = panelManager.CreateSubPanel(micInputSubPanel, "Ego Banner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100f), 430, 30, pivot: new Vector2(0.5f, 1f));
        
        
        
        npcNameSubPanel = panelManager.CreateSubPanel(thrallCustomizationPanel, "Name", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100f), 400, 80, pivot: new Vector2(0.5f, 1f));
        npcPersonalitySubPanel = panelManager.CreateSubPanel(npcNameSubPanel, "Personality", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -90f), 400, 250, pivot: new Vector2(0.5f, 1f));
        npcVoiceSubPanel = panelManager.CreateSubPanel(npcPersonalitySubPanel, "Voice", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -260f), 400, 110, pivot: new Vector2(0.5f, 1f));
        npcBodyTypeSubPanel = panelManager.CreateSubPanel(npcVoiceSubPanel, "Body Type", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120f), 400, 100, pivot: new Vector2(0.5f, 1f));
        npcAppearanceSubPanel = panelManager.CreateSubPanel(npcBodyTypeSubPanel, "Appearance", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -110f), 400, 120, pivot: new Vector2(0.5f, 1f));
        


        CreateScrollableTaskQueue();

        CreateKeyBindings();

        CreateMicInput();

        CreateEgoBanner();

        CreateNameSection();

        CreatePersonalitySection();

        CreateVoiceAndVolumeControls();

        CreateBodyTypeToggle();

        CreateAppearanceSection();

        CreateSaveButton();
    }

    GameObject[] TasksList = {};
    GameObject TaskListScrollBox;

    private void CreateScrollableTaskQueue()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Task Queue",
            parent: taskQueueSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            //position: new Vector2(150f, -30f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        Debug.Log("Creating scrollable task queue");

        TaskListScrollBox = CreateScrollBox(taskQueueSubPanel, new Vector2(-10, -10), 400, 140);

        /*Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

        // Add some items to the scroll box
        for (int i = 0; i < 20; i++)
        {
            AddItemToScrollBox(scrollBox, $"Task {i + 1}", defaultSprite);
        }*/
    }

    public GameObject CreateScrollBox(GameObject parent, Vector2 position, float width, float height)
    {
        GameObject scrollViewObject = new GameObject("ScrollView");
        scrollViewObject.transform.SetParent(parent.transform, false);

        ScrollRect scrollRect = scrollViewObject.AddComponent<ScrollRect>();
        RectTransform scrollRectTransform = scrollViewObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRectTransform.anchoredPosition = position;
        scrollRectTransform.sizeDelta = new Vector2(width, height);

        GameObject viewportObject = new GameObject("Viewport");
        viewportObject.transform.SetParent(scrollViewObject.transform, false);
        RectTransform viewportRectTransform = viewportObject.AddComponent<RectTransform>();
        viewportRectTransform.anchorMin = Vector2.zero;
        viewportRectTransform.anchorMax = Vector2.one;
        viewportRectTransform.sizeDelta = new Vector2(-20, 0); // Make room for scrollbar
        viewportRectTransform.anchoredPosition = Vector2.zero;

        // Add mask to viewport
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = Color.white;
        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = new GameObject("Content");
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRectTransform = contentObject.AddComponent<RectTransform>();
        VerticalLayoutGroup verticalLayout = contentObject.AddComponent<VerticalLayoutGroup>();
        ContentSizeFitter contentSizeFitter = contentObject.AddComponent<ContentSizeFitter>();

        contentRectTransform.anchorMin = new Vector2(0, 1);
        contentRectTransform.anchorMax = new Vector2(1, 1);
        contentRectTransform.pivot = new Vector2(0f, 1f); // Set pivot to top center
        contentRectTransform.sizeDelta = new Vector2(0, 0);
        contentRectTransform.anchoredPosition = Vector2.zero;
        verticalLayout.padding = new RectOffset(10, 10, 10, 10);
        verticalLayout.spacing = 10;
        verticalLayout.childAlignment = TextAnchor.UpperLeft;
        verticalLayout.childForceExpandWidth = true;
        verticalLayout.childControlWidth = true;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRectTransform;
        scrollRect.viewport = viewportRectTransform;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        GameObject scrollbarObject = new GameObject("Scrollbar");
        scrollbarObject.transform.SetParent(scrollViewObject.transform, false);
        Scrollbar scrollbar = scrollbarObject.AddComponent<Scrollbar>();
        Image scrollbarImage = scrollbarObject.AddComponent<Image>();
        scrollbarImage.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Semi-transparent gray
        RectTransform scrollbarRectTransform = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRectTransform.anchorMin = new Vector2(1, 0);
        scrollbarRectTransform.anchorMax = Vector2.one;
        scrollbarRectTransform.sizeDelta = new Vector2(20, 0);
        scrollbarRectTransform.anchoredPosition = Vector2.zero;

        GameObject scrollbarHandleObject = new GameObject("Handle");
        scrollbarHandleObject.transform.SetParent(scrollbarObject.transform, false);
        Image handleImage = scrollbarHandleObject.AddComponent<Image>();
        handleImage.color = new Color(0.7f, 0.7f, 0.7f, 0.7f); // Semi-transparent light gray
        RectTransform handleRectTransform = scrollbarHandleObject.GetComponent<RectTransform>();
        handleRectTransform.sizeDelta = Vector2.zero;

        scrollbar.handleRect = handleRectTransform;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        scrollRect.verticalScrollbar = scrollbar;

        return scrollViewObject;
    }

    public void AddItemToScrollBox(GameObject scrollBox, string text, Sprite icon)
    {
        Transform contentTransform = scrollBox.transform.Find("Viewport/Content");
        if (contentTransform != null)
        {
            GameObject itemObject = new GameObject("Item");
            itemObject.transform.SetParent(contentTransform, false);

            HorizontalLayoutGroup horizontalLayout = itemObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(5, 5, 5, 5);
            horizontalLayout.spacing = 10;
            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.childControlWidth = false;

            LayoutElement itemLayout = itemObject.AddComponent<LayoutElement>();
            itemLayout.minHeight = 40;
            itemLayout.flexibleWidth = 1;

            // Image
            GameObject imageObject = new GameObject("Icon");
            imageObject.transform.SetParent(itemObject.transform, false);
            Image imageComponent = imageObject.AddComponent<Image>();
            imageComponent.sprite = icon;
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(30, 30);
            LayoutElement imageLayout = imageObject.AddComponent<LayoutElement>();
            imageLayout.minWidth = 30;
            imageLayout.minHeight = 30;
            imageLayout.flexibleWidth = 0;

            // Text
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(itemObject.transform, false);
            Text textComponent = textObject.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = GUIManager.Instance.AveriaSerif;
            textComponent.fontSize = 20;
            textComponent.color = Color.white;
            textComponent.alignment = TextAnchor.MiddleLeft;
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            LayoutElement textLayout = textObject.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;

            // Spacer (to push the delete button to the right)
            GameObject spacerObject = new GameObject("Spacer");
            spacerObject.transform.SetParent(itemObject.transform, false);
            LayoutElement spacerLayout = spacerObject.AddComponent<LayoutElement>();
            spacerLayout.flexibleWidth = 1;

            // Delete Button
            GameObject buttonObject = new GameObject("DeleteButton");
            buttonObject.transform.SetParent(itemObject.transform, false);
            Button buttonComponent = buttonObject.AddComponent<Button>();
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.clear;
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(25, 25);
            LayoutElement buttonLayout = buttonObject.AddComponent<LayoutElement>();
            buttonLayout.minWidth = 25;
            buttonLayout.minHeight = 25;
            buttonLayout.flexibleWidth = 0;

            // Button Text
            GameObject buttonTextObject = new GameObject("ButtonText");
            buttonTextObject.transform.SetParent(buttonObject.transform, false);
            Text buttonTextComponent = buttonTextObject.AddComponent<Text>();
            buttonTextComponent.text = "X";
            buttonTextComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            buttonTextComponent.fontSize = 18;
            buttonTextComponent.color = Color.white;
            buttonTextComponent.alignment = TextAnchor.MiddleCenter;
            RectTransform buttonTextRect = buttonTextObject.GetComponent<RectTransform>();
            buttonTextRect.anchorMin = Vector2.zero;
            buttonTextRect.anchorMax = Vector2.one;
            buttonTextRect.sizeDelta = Vector2.zero;

            // Delete functionality
            buttonComponent.onClick.AddListener(() => {
                GameObject.Destroy(itemObject);
            });

            TasksList.AddItem(itemObject);
        }
    }

    public void DeleteAllTasks()
    {
        foreach (GameObject itemObject in TasksList)
        {
            GameObject.Destroy(itemObject);
        }
    }



    private void CreateKeyBindings()
    {
        string[] bindings = {
            "[G] Spawn",
            "[X] Dismiss",
            "[H] Harvest",
            "[F] Follow/Patrol"
        };

        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Keybinds",
            parent: keybindsSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        for (int i = 0; i < bindings.Length; i++)
        {
            GameObject textObject2 = GUIManager.Instance.CreateText(
                text: bindings[i],
                parent: keybindsSubPanel.transform,
                anchorMin: new Vector2(0f, 1f),
                anchorMax: new Vector2(0f, 1f),
                position: new Vector2(10f, -40f) + new Vector2(0, (-i * 30)),
                font: GUIManager.Instance.AveriaSerif,
                fontSize: 20,
                color: Color.white,
                outline: true,
                outlineColor: Color.black,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            textObject2.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
        }
    }

    Dropdown micDropdownComp;
    private void CreateMicInput()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Mic Input",
            parent: micInputSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 26,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        var micDropdown = GUIManager.Instance.CreateDropDown(
            parent: micInputSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(0f, 0f),
            fontSize: 16,
            width: 280f,
            height: 30f);

        micDropdownComp = micDropdown.GetComponent<Dropdown>();
        List<string> truncatedOptions = Microphone.devices.ToList().Select(option => TruncateText(option, 27)).ToList();
        micDropdownComp.AddOptions(truncatedOptions);

        RectTransform dropdownRect = micDropdown.GetComponent<RectTransform>();

        dropdownRect.pivot = new Vector2(0f, 1f);
        dropdownRect.anchoredPosition = new Vector2(10f, -40f);


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
        instance.MicrophoneIndex = index;
        Debug.Log("new MicrophoneName " + Microphone.devices[index]);
    }

    private void CreateEgoBanner()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "egovalheimmod.ai",
            parent: egoBannerSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            /*anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(0f, 0f),*/
            position: new Vector2(10f, -2f),
            //position: startPosition + new Vector2(170, 0),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 22,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 1f);

    }


    
    private InputField nameInputField;

    private void CreateNameSection()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Name",
            parent: npcNameSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: instance.MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        CreateNameInputField(npcNameSubPanel.transform, "Bilbo");

        /*GameObject textFieldObject = GUIManager.Instance.CreateInputField(
           parent: npcNameSubPanel.transform,
           anchorMin: new Vector2(0f, 1f),
           anchorMax: new Vector2(0f, 1f),
           position: new Vector2(10f, -40f),
           contentType: InputField.ContentType.Standard,
           placeholderText: "Valkyrie",
           fontSize: 30,
           width: 350f,
           height: 30f);

        textFieldObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        nameInputField = textFieldObject.GetComponent<InputField>();
        nameInputField.onValueChanged.AddListener(OnNPCNameChanged);
        nameInputField.interactable = true;*/
    }

    public void CreateNameInputField(Transform parent, string placeholder, int fontSize = 18, int width = 380, int height = 30)
    {
        GameObject inputFieldObject = new GameObject("CustomInputField");
        inputFieldObject.transform.SetParent(parent, false);

        Image background = inputFieldObject.AddComponent<Image>();
        background.color = new Color(0.7f, 0.7f, 0.7f, 0.3f);

        nameInputField = inputFieldObject.AddComponent<InputField>();
        nameInputField.lineType = InputField.LineType.SingleLine;
        nameInputField.onValueChanged.AddListener(OnNPCNameChanged);

        RectTransform rectTransform = nameInputField.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(width, height);
        rectTransform.anchoredPosition = new Vector2(0, -15); // Move the field down

        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputFieldObject.transform, false);
        Text placeholderText = placeholderObj.AddComponent<Text>();
        placeholderText.text = placeholder;
        placeholderText.font = GUIManager.Instance.AveriaSerifBold;
        placeholderText.fontSize = fontSize;
        placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

        RectTransform placeholderTransform = placeholderText.GetComponent<RectTransform>();
        placeholderTransform.anchorMin = Vector2.zero;
        placeholderTransform.anchorMax = Vector2.one;
        placeholderTransform.offsetMin = new Vector2(10, 0);
        placeholderTransform.offsetMax = new Vector2(-10, 0);
        placeholderTransform.anchoredPosition = new Vector2(0, -4);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputFieldObject.transform, false);
        Text personalityInputText = textObj.AddComponent<Text>();
        personalityInputText.font = GUIManager.Instance.AveriaSerifBold;
        personalityInputText.fontSize = fontSize;
        personalityInputText.color = Color.white;

        RectTransform textTransform = personalityInputText.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.offsetMin = new Vector2(10, 0);
        textTransform.offsetMax = new Vector2(-10, 0);
        textTransform.anchoredPosition = new Vector2(0, -4);

        nameInputField.placeholder = placeholderText;
        nameInputField.textComponent = personalityInputText;
    }

    private void OnNPCNameChanged(string newValue)
    {
        Debug.Log("Input value changed to: " + newValue);

        if (!instance.PlayerNPC) return;

        instance.npcName = newValue;
        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
        npc.m_name = newValue;
        nameInputField.SetTextWithoutNotify(newValue);

    }

    Dropdown personalityDropdownComp;

    private void CreatePersonalitySection()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Personality",
            parent: npcPersonalitySubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: instance.MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 1);

        var personalityDropdown = GUIManager.Instance.CreateDropDown(
            parent: npcPersonalitySubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(10f, -40f),
            fontSize: 20,
            width: 200f,
            height: 30f);

        rectTransform = personalityDropdown.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0, 1);

        instance.personalityDropdownComp = personalityDropdown.GetComponent<Dropdown>();
        instance.personalityDropdownComp.AddOptions(npcPersonalities);

        /*// Load the saved value
        int savedIndex = PlayerPrefs.GetInt("SelectedVoiceIndex", 0);
        personalityDropdownComp.value = savedIndex;*/

        // Add listener for value change
        instance.personalityDropdownComp.onValueChanged.AddListener(OnNPCPersonalityDropdownChanged);

        CreateMultilineInputField(
            parent: npcPersonalitySubPanel.transform,
            placeholder: "She's strong, stoic, tomboyish, confident and serious...",
            fontSize: 14
        );
    }

    private void OnNPCPersonalityDropdownChanged(int index)
    {
        instance.npcPersonalityIndex = index;
        instance.npcPersonality = npcPersonalitiesMap[npcPersonalities[index]];
        instance.personalityInputField.SetTextWithoutNotify(npcPersonalitiesMap[npcPersonalities[index]]);
        Debug.Log("new NPCPersonality " + instance.npcPersonalityIndex);
    }

    private InputField personalityInputField;
    
    static public List<String> npcPersonalities = new List<string> {
        "Freiya",
        "Mean",
        "Bag Chaser",
        "Creditor"
    };

    static public Dictionary<String, String> npcPersonalitiesMap = new Dictionary<String, String>
    {
      {"Freiya", "She's strong, stoic, tomboyish, confident and serious. behind her cold exterior she is soft and caring, but she's not always good at showing it. She secretly wants a husband but is not good when it comes to romance and love, very oblivious to it." },
      {"Mean", "Mean and angry. Always responds rudely."},
      {"Bag Chaser", "Only cares about the money. Mentions money every time"},
      {"Creditor", "He gave me 10000 dollars which I haven't returned. He brings it up everytime we talk."},
    };



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

    public void CreateMultilineInputField(Transform parent, string placeholder, int fontSize = 16, int width = 380, int height = 150)
    {
        // Create main GameObject for the input field
        GameObject inputFieldObject = new GameObject("CustomInputField");
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
        rectTransform.position = rectTransform.position + new Vector3(0, -40, 0);

        // Create placeholder text
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(inputFieldObject.transform, false);
        Text placeholderText = placeholderObj.AddComponent<Text>();
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
        //placeholderTransform.pivot = new Vector2(0, 1);

        // Create input text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(inputFieldObject.transform, false);
        Text personalityInputText = textObj.AddComponent<Text>();
        personalityInputText.font = GUIManager.Instance.AveriaSerifBold;
        personalityInputText.fontSize = fontSize;
        personalityInputText.color = Color.white;

        // Set up input text RectTransform
        RectTransform textTransform = personalityInputText.GetComponent<RectTransform>();
        textTransform.anchorMin = new Vector2(0, 0);
        textTransform.anchorMax = new Vector2(1, 1);
        textTransform.offsetMin = new Vector2(10, 10);
        textTransform.offsetMax = new Vector2(-10, -10);
        //textTransform.pivot = new Vector2(0, 1);

        // Assign text components to InputField
        personalityInputField.placeholder = placeholderText;
        personalityInputField.textComponent = personalityInputText;
    }

    private void OnPersonalityTextChanged(string newText)
    {
        instance.npcPersonality = newText;
        Debug.Log("New personality " + instance.npcPersonality);
    }

    Dropdown voiceDropdownComp;
    Slider volumeSliderComp;

    private void CreateVoiceAndVolumeControls()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Voice",
            parent: npcVoiceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        var voiceDropdown = GUIManager.Instance.CreateDropDown(
            parent: npcVoiceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(110f, -50f),
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



        instance.previewVoiceButton = GUIManager.Instance.CreateButton(
            text: "Preview",
            parent: voiceDropdown.transform,
            anchorMin: new Vector2(0.5f, 0f),
            anchorMax: new Vector2(0.5f, 0f),
            position: new Vector2(190, 0f),
            width: 100f,
            height: 30f);

        instance.previewVoiceButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
        instance.previewVoiceButtonComp = instance.previewVoiceButton.GetComponent<Button>();
        instance.previewVoiceButtonComp.onClick.AddListener(() => OnPreviewVoiceButtonClick(instance.previewVoiceButtonComp));




        textObject = GUIManager.Instance.CreateText(
            text: "Volume",
            parent: npcVoiceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(10f, -75f),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 20,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        var volumeSlider = CreateSlider(
            parent: npcVoiceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(230f, -87.5f),
            width: 250f,
            height: 15f);

        instance.volumeSliderComp = volumeSlider.GetComponent<Slider>();
        instance.volumeSliderComp.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }


    GameObject previewVoiceButton;
    Button previewVoiceButtonComp;
    private void CreatePreviewVoiceButton()
    {
        
    }

    private void OnPreviewVoiceButtonClick(Button button)
    {
        instance.BrainSynthesizeAudio("Hello, I am your friend sent by the team at Ego", npcVoices[instance.npcVoice].ToLower());
        //Debug.Log("Hello, I am your friend sent by the team at Ego. voice: " + npcVoices[instance.npcVoice].ToLower());
        instance.previewVoiceButton.SetActive(false);
    }

    private void OnNPCVoiceDropdownChanged(int index)
    {
        instance.npcVoice = index;
        Debug.Log("new instance.npcVoice " + instance.npcVoice);
    }

    private void OnVolumeSliderValueChanged(float value)
    {
        instance.npcVolume = value;
        Debug.Log("new companion volume " + instance.npcVolume);
    }


    Toggle toggleMasculine;
    Toggle toggleFeminine;

    private void CreateBodyTypeToggle()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Body Type",
            parent: npcBodyTypeSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);


        GameObject toggleObj1 = CreateToggle(npcBodyTypeSubPanel.transform, "Masculine", "Masculine", -20);
        GameObject toggleObj2 = CreateToggle(npcBodyTypeSubPanel.transform, "Feminine", "Feminine", -50);

        instance.toggleMasculine = toggleObj1.GetComponent<Toggle>();
        instance.toggleFeminine = toggleObj2.GetComponent<Toggle>();

        instance.toggleMasculine.isOn = true;

        // Add listeners
        instance.toggleMasculine.onValueChanged.AddListener(isOn => OnBodyTypeToggleChanged(instance.toggleMasculine, instance.toggleFeminine, isOn));
        instance.toggleFeminine.onValueChanged.AddListener(isOn => OnBodyTypeToggleChanged(instance.toggleFeminine, instance.toggleMasculine, isOn));
    }

    void OnBodyTypeToggleChanged(Toggle changedToggle, Toggle otherToggle, bool isOn)
    {
        if (isOn && otherToggle.isOn)
        {
            otherToggle.isOn = false;
        }
        instance.npcGender = changedToggle.name == "Masculine" ? 0 : 1;

        if (instance.PlayerNPC)
        {
            VisEquipment npcVisEquipment = instance.PlayerNPC.GetComponent<VisEquipment>();
            npcVisEquipment.SetModel(instance.npcGender);
        }
        else
        {
            Debug.Log("OnBodyTypeToggleChanged instance.PlayerNPC is null");
        }

        Debug.Log("new npcGender " + instance.npcGender);
    }


    private void CreateAppearanceSection()
    {
        GameObject textObject = GUIManager.Instance.CreateText(
            text: "Appearance",
            parent: npcAppearanceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: MenuSectionTitlePosition,
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: MenuSectionTitleFontSize,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        textObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        var skinColorButtonObject = GUIManager.Instance.CreateButton(
            text: "",
            parent: npcAppearanceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(10, -40f),
            width: 50f,
            height: 30f);

        skinColorButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        GameObject skinColorTextObject = GUIManager.Instance.CreateText(
            text: "Skin Tone",
            parent: skinColorButtonObject.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(60,-3),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 20,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        skinColorTextObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        skinColorButtonObject.GetComponent<Button>().onClick.AddListener(CreateSkinColorPicker);





        var hairColorButtonObject = GUIManager.Instance.CreateButton(
            text: "",
            parent: npcAppearanceSubPanel.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(10, -80f),
            width: 50f,
            height: 30f);

        hairColorButtonObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        GameObject hairColorTextObject = GUIManager.Instance.CreateText(
            text: "Hair Color",
            parent: hairColorButtonObject.transform,
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(60, -3),
            font: GUIManager.Instance.AveriaSerifBold,
            fontSize: 20,
            color: Color.white,
            outline: true,
            outlineColor: Color.black,
            width: 350f,
            height: 40f,
            addContentSizeFitter: false);

        hairColorTextObject.GetComponent<RectTransform>().pivot = new Vector2(0, 1);

        hairColorButtonObject.GetComponent<Button>().onClick.AddListener(CreateHairColorPicker);
    }

    private void CreateSkinColorPicker()
    {
        GUIManager.Instance.CreateColorPicker(
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(500f, -500f),
            original: Color.yellow,
            message: "Skin Tone",
            OnSkinColorChanged,
            OnSkinColorSelected,
            false
        );
    }

    private void OnSkinColorChanged(Color changedColor)
    {
        if (!instance.PlayerNPC) return;

        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
        npc.m_visEquipment.SetSkinColor(new Vector3(
            instance.skinColor.r,
            instance.skinColor.g,
            instance.skinColor.b
        ));
        //Jotunn.Logger.LogInfo($"Color changing: {changedColor}");
    }

    private void OnSkinColorSelected(Color selectedColor)
    {
        if (!instance.PlayerNPC) return;

        instance.skinColor = selectedColor;
        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
        npc.m_visEquipment.SetSkinColor(new Vector3(
            instance.skinColor.r,
            instance.skinColor.g,
            instance.skinColor.b
        ));
        Jotunn.Logger.LogInfo($"Selected color: {instance.skinColor}");
        // You can save the color to a config file or use it in your mod here
    }

    private void CreateHairColorPicker()
    {
        GUIManager.Instance.CreateColorPicker(
            anchorMin: new Vector2(0f, 1f),
            anchorMax: new Vector2(0f, 1f),
            position: new Vector2(500f, -500f),
            original: Color.yellow,
            message: "Hair Color",
            OnHairColorChanged,
            OnHairColorSelected,
            false
        );
    }

    private void OnHairColorChanged(Color changedColor)
    {
        if (!instance.PlayerNPC) return;

        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
        npc.m_visEquipment.SetHairColor(new Vector3(
            instance.hairColor.r,
            instance.hairColor.g,
            instance.hairColor.b
        ));
        //Jotunn.Logger.LogInfo($"Color changing: {changedColor}");
    }

    private void OnHairColorSelected(Color selectedColor)
    {
        if (!instance.PlayerNPC) return;

        instance.hairColor = selectedColor;
        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
        npc.m_visEquipment.SetHairColor(new Vector3(
            instance.hairColor.r,
            instance.hairColor.g,
            instance.hairColor.b
        ));
        Jotunn.Logger.LogInfo($"Selected color: {instance.hairColor}");
        // You can save the color to a config file or use it in your mod here
    }

    private void CreateSaveButton()
    {
        GameObject saveButton = GUIManager.Instance.CreateButton(
            text: "SAVE",
            parent: thrallCustomizationPanel.transform,
            anchorMin: new Vector2(0.5f, 0f),
            anchorMax: new Vector2(0.5f, 0f),
            position: new Vector2(0, 25f),
            width: 250f,
            height: 40f);

        saveButton.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);

        Button saveButtonComp = saveButton.GetComponent<Button>();
        saveButtonComp.onClick.AddListener(() => OnSaveButtonClick(saveButtonComp));
    }

    private void OnSaveButtonClick(Button button)
    {
        instance.panelManager.TogglePanel("Settings");
        instance.panelManager.TogglePanel("Thrall Customization");
        instance.IsModMenuShowing = false;
        GUIManager.BlockInput(false);
        if (instance.PlayerNPC)
            SaveNPCData(instance.PlayerNPC);
    }

    // Make sure to include your existing CreateTask and CreateSlider methods here




    /*
     * 
     * UI GENERATOR FUNCTIONS
     * 
     */

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
        //fill.GetComponent<Image>().color = GUIManager.Instance.ValheimOrange;
        fill.GetComponent<Image>().color = new Color(0.7f, 0.7f, 0.7f);
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
        handle.GetComponent<Image>().color = GUIManager.Instance.ValheimOrange;
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

    GameObject CreateToggle(Transform parent, string name, string label, float positionY)
    {
        GameObject toggleObj = new GameObject(name, typeof(RectTransform), typeof(Toggle));
        toggleObj.transform.SetParent(parent, false);

        RectTransform toggleRect = toggleObj.GetComponent<RectTransform>();
        toggleRect.anchorMin = new Vector2(0f, 1f);
        toggleRect.anchorMax = new Vector2(0f, 1f);
        toggleRect.anchoredPosition = new Vector2(10, positionY);
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
}




/*
 * 
 * 
 * UI
 * 
 * 
 */
