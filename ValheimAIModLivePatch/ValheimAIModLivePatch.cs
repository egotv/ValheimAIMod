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

[BepInPlugin("egovalheimmod.ValheimAIModLivePatch", "EGO.AI Valheim AI NPC Mod Live Patch", "0.0.1")]
[BepInProcess("valheim.exe")]
[BepInDependency("egovalheimmod.ValheimAIModLoader", BepInDependency.DependencyFlags.HardDependency)]
public class ValheimAIModLivePatch : BaseUnityPlugin
{
    public enum NPCMode
    {
        Passive,
        Defensive,
        Aggressive
    }




    private static ValheimAIModLivePatch instance;
    private readonly Harmony harmony = new Harmony("egovalheimmod.ValheimAIModLivePatch");
    
    //private const string brainBaseURL = "http://localhost:5000";
    private const string brainBaseURL = "https://valheim-agent-brain.fly.dev";

    private string playerDialogueAudioPath;
    private string npcDialogueAudioPath;
    private string npcDialogueRawAudioPath;

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

    private ConfigEntry<string> BrainAPIAddress;
    private ConfigEntry<bool> DisableAutoSave;

    private Dictionary<string, Piece.Requirement[]> craftingRequirements = new Dictionary<string, Piece.Requirement[]>();
    private Dictionary<string, Piece.Requirement[]> buildingRequirements = new Dictionary<string, Piece.Requirement[]>();
    private Dictionary<string, List<string>> resourceLocations = new Dictionary<string, List<string>>();

    private static string NPCPrefabName = "HumanoidNPC";


    private GameObject PlayerNPC;
    private HumanoidNPC PlayerNPC_humanoid;


    public NPCMode NPCCurrentMode { get; private set; }

    /*public NPC()
    {
        NPCCurrentMode = NPCMode.Passive; // Default mode
    }*/

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
    private NPCCommand.CommandType NPCCurrentCommand;

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

    private NPCCommandManager commandManager = new NPCCommandManager();


    private static Dictionary<string, Dictionary<string, List<string>>> resourceDatabase = new Dictionary<string, Dictionary<string, List<string>>>();
    private static Dictionary<string, float> resourceHealthMap = new Dictionary<string, float>();
    private static Dictionary<string, float> resourceQuantityMap = new Dictionary<string, float>();


    private void Awake()
    {
        Debug.Log("ValheimAIModLivePatch Loaded!");
        instance = this;

        ConfigBindings();

        /*PopulateCraftingRequirements();
        PopulateBuildingRequirements();
        PopulateMonsterPrefabs();
        PopulateAllItems();*/

        playerDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "playerdialogue.wav");
        npcDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue.wav");
        npcDialogueRawAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");

        Chat.instance.SendText(Talker.Type.Normal, "EGO.AI MOD LOADED!");

        CreateModMenuUI();

        instance.NPCCurrentMode = NPCMode.Defensive;

        instance.FindPlayerNPCs();
        if (instance.PlayerNPC)
        {
            HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
            LoadNPCData(npc);
        }


        /*HarvestAction harvestAction = new HarvestAction();
        harvestAction.ResourceName = "Wood";
        harvestAction.RequiredAmount = 20;
        commandManager.AddCommand(harvestAction);*/


        PopulateDatabase();
        //PopulateAllItems();

        harmony.PatchAll(typeof(ValheimAIModLivePatch));
    }


    private void SaveDatabaseToJson()
    {
        JsonObject jsonObject = new JsonObject();

        foreach (var resourcePair in resourceDatabase)
        {
            JsonObject resourceObject = new JsonObject();

            foreach (var sourcePair in resourcePair.Value)
            {
                JsonArray sourceArray = new JsonArray();

                foreach (string sourceName in sourcePair.Value)
                {
                    sourceArray.Add(sourceName);
                }

                resourceObject[sourcePair.Key] = sourceArray;
            }

            jsonObject[resourcePair.Key] = resourceObject;
        }

        string jsonFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "database.json");
        File.WriteAllText(jsonFilePath, jsonObject.ToString()); // '2' is for indentation
    }

    private void PopulateDatabase()
    {
        // This method would need to be implemented to populate the database
        // You'd need to iterate through all prefabs in the game and check their components

        

        //Example (not actual implementation):
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        //foreach (GameObject prefab in GameObject.FindObjectsOfType<GameObject>())
            
        {
            /*if (resourceDatabase.ContainsKey(CleanKey(prefab.name)))
            {
                continue;
            }*/



            if (prefab.HasAnyComponent("TreeBase"))
                CheckTreeBase(prefab);
            if (prefab.HasAnyComponent("TreeLog"))
                CheckTreeLog(prefab);
            if (prefab.HasAnyComponent("CharacterDrop"))
                CheckCharacterDrop(prefab);
            if (prefab.HasAnyComponent("DropOnDestroyed"))
                CheckDropOnDestroyed(prefab);
            if (prefab.HasAnyComponent("Destructible"))
                CheckDestructibles(prefab);
            if (prefab.HasAnyComponent("Pickable"))
                CheckPickables(prefab);
            if (prefab.HasAnyComponent("ItemDrop"))
                CheckItemDrop(prefab);
            if (prefab.HasAnyComponent("MineRock"))
                CheckMineRock(prefab);
            if (prefab.HasAnyComponent("MineRock5"))
                CheckMineRock5(prefab);
        }


        //SaveDatabaseToJson();

        /*foreach (string s in resourceHealthMap.Keys)
        {
            Debug.LogError($"{s} {resourceHealthMap[s]} {resourceQuantityMap[s]}");
        }*/
    }

    private void CheckTreeBase(GameObject prefab)
    {
        TreeBase treeBase = prefab.GetComponent<TreeBase>();
        if (treeBase != null && treeBase.m_dropWhenDestroyed != null && treeBase.m_dropWhenDestroyed.m_drops != null)
        {
            foreach (DropTable.DropData drop in treeBase.m_dropWhenDestroyed.m_drops)
            {
                
                float health = 0;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = Math.Max(treeBase.m_health, health);
                }
                else
                {
                    resourceHealthMap[prefab.name] = treeBase.m_health;
                }

                resourceQuantityMap[prefab.name] = treeBase.m_dropWhenDestroyed.m_dropMax;

                AddToDatabase(drop.m_item.name, "TreeBase", prefab.name);
            }
        }
    }

    private void CheckTreeLog(GameObject prefab)
    {
        TreeLog treeBase = prefab.GetComponent<TreeLog>();
        if (treeBase != null && treeBase.m_dropWhenDestroyed != null && treeBase.m_dropWhenDestroyed.m_drops != null)
        {
            foreach (DropTable.DropData drop in treeBase.m_dropWhenDestroyed.m_drops)
            {
                float health = 0;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = Math.Max(treeBase.m_health, health);
                }
                else
                {
                    resourceHealthMap[prefab.name] = treeBase.m_health;
                }

                resourceQuantityMap[prefab.name] = treeBase.m_dropWhenDestroyed.m_dropMax;

                AddToDatabase(drop.m_item.name, "TreeLog", prefab.name);
            }
        }
    }

    private void CheckCharacterDrop(GameObject prefab)
    {
        CharacterDrop characterDrop = prefab.GetComponent<CharacterDrop>();
        if (characterDrop != null && characterDrop.m_drops != null)
        {
            foreach (CharacterDrop.Drop drop in characterDrop.m_drops)
            {
                resourceQuantityMap[prefab.name] = characterDrop.m_drops.Count;
                AddToDatabase(drop.m_prefab.name, "CharacterDrop", prefab.name);
            }
        }
    }

    private void CheckDropOnDestroyed(GameObject prefab)
    {
        DropOnDestroyed dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>();
        if (dropOnDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed.m_drops != null)
        {
            foreach (DropTable.DropData drop in dropOnDestroyed.m_dropWhenDestroyed.m_drops)
            {
                resourceQuantityMap[prefab.name] = dropOnDestroyed.m_dropWhenDestroyed.m_dropMax;
                if (drop.m_item)
                    AddToDatabase(drop.m_item.name, "DropOnDestroyed", prefab.name);
            }
        }
    }

    private void CheckItemDrop(GameObject prefab)
    {
        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
        //if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
        if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
        {
            resourceQuantityMap[prefab.name] = itemDrop.m_itemData.m_stack;
            //AddToDatabase(itemDrop.m_itemData.m_shared.m_name, "ItemDrop", prefab.name);
            AddToDatabase(itemDrop.m_itemData.m_dropPrefab.name, "ItemDrop", prefab.name);
        }
    }

    private void CheckDestructibles(GameObject prefab)
    {
        Destructible destructible = prefab.GetComponent<Destructible>();
        
        if (destructible != null )
        {
            float health = 0;
            if (resourceHealthMap.TryGetValue(prefab.name, out health))
            {
                resourceHealthMap[prefab.name] = Math.Max(destructible.m_health, health);
            }
            else
            {
                resourceHealthMap[prefab.name] = destructible.m_health;
            }

            resourceQuantityMap[prefab.name] = 1;

            if (destructible.m_spawnWhenDestroyed != null)
            {
                AddToDatabase(destructible.m_spawnWhenDestroyed.name, "Destructible", prefab.name);
            }
        }
    }

    private void CheckPickables(GameObject prefab)
    {
        Pickable pickable = prefab.GetComponent<Pickable>();
        if (pickable != null && pickable.m_itemPrefab != null)
        {
            resourceQuantityMap[prefab.name] = pickable.m_amount;
            AddToDatabase(pickable.m_itemPrefab.name, "Pickable", prefab.name);
        }
    }

    private void CheckMineRock(GameObject prefab)
    {
        MineRock minerock = prefab.GetComponent<MineRock>();
        if (minerock != null && minerock.m_dropItems != null && minerock.m_dropItems.m_drops != null)
        {
            float health = 0;
            if (resourceHealthMap.TryGetValue(prefab.name, out health))
            {
                resourceHealthMap[prefab.name] = Math.Max(minerock.m_health, health);
            }
            else
            {
                resourceHealthMap[prefab.name] = minerock.m_health;
            }

            resourceQuantityMap[prefab.name] = minerock.m_dropItems.m_dropMax;

            foreach (DropTable.DropData drop in minerock.m_dropItems.m_drops)
            {
                //if (drop.m_item != null)
                    AddToDatabase(drop.m_item.name, "MineRock", prefab.name);
            }
        }
    }

    private void CheckMineRock5(GameObject prefab)
    {
        MineRock5 minerock = prefab.GetComponent<MineRock5>();
        if (minerock != null && minerock.m_dropItems != null && minerock.m_dropItems.m_drops != null)
        {
            float health = 0;
            if (resourceHealthMap.TryGetValue(prefab.name, out health))
            {
                resourceHealthMap[prefab.name] = Math.Max(minerock.m_health, health);
            }
            else
            {
                resourceHealthMap[prefab.name] = minerock.m_health;
            }

            resourceQuantityMap[prefab.name] = minerock.m_dropItems.m_dropMax;


            foreach (DropTable.DropData drop in minerock.m_dropItems.m_drops)
            {
                //if (drop.m_item != null)
                    AddToDatabase(drop.m_item.name, "MineRock5", prefab.name);
            }
        }
    }

    private static List<string> priorityOrder = new List<string>
    {
        "ItemDrop",
        "Pickable",
        
        "TreeLog",
        "TreeBase",
        "MineRock",
        "MineRock5",

        "CharacterDrop",
        "DropOnDestroyed",
        "Destructible"
    };

    private void AddToDatabase(string resourceName, string sourceType, string sourceName)
    {
        //Debug.Log($"{resourceName} {sourceType} {sourceName}");
        

        if (!resourceDatabase.ContainsKey(resourceName))
        {
            resourceDatabase[resourceName] = new Dictionary<string, List<string>>
                {
                    { "ItemDrop", new List<string>() },
                    { "Pickable", new List<string>() },

                    { "TreeLog", new List<string>() },
                    { "TreeBase", new List<string>() },
                    { "MineRock", new List<string>() },
                    { "MineRock5", new List<string>() },

                    { "CharacterDrop", new List<string>() },
                    
                    { "DropOnDestroyed", new List<string>() },

                    { "Destructible", new List<string>() },


                    
                };
        }

        resourceDatabase[resourceName][sourceType].Add(sourceName);
    }

    public static string[] QueryResource(string resourceName)
    {
        if (!resourceDatabase.ContainsKey(resourceName))
        {
            return new string[0]; // Return an empty array if resource is not found
        }

        var results = resourceDatabase[resourceName];
        var resourceList = new List<string>();

        // Add all resources to the set without labels
        foreach (var sourceType in priorityOrder)
        {
            if (results.ContainsKey(sourceType))
            {
                resourceList.AddRange(results[sourceType]);
            }
        }

        // Print the array before returning
        /*Debug.Log($"Resources for '{resourceName}':");
        Debug.Log(string.Join(", ", resourceList));*/

        return resourceList.Distinct().ToArray();
    }

    /*public static string[] QueryResource(string resourceName)
    {
        if (!resourceDatabase.ContainsKey(resourceName))
        {
            return $"Resource '{resourceName}' not found.";
        }

        var results = resourceDatabase[resourceName];
        var output = $"Ways to obtain '{resourceName}':\n";

        
        if (results["CharacterDrop"].Count > 0)
            output += $"- Defeat creatures: {string.Join(", ", results["CharacterDrop"])}\n";
        if (results["ItemDrop"].Count > 0)
            output += $"- Pick up from the ground: {string.Join(", ", results["ItemDrop"])}\n";
        if (results["DropOnDestroyed"].Count > 0)
            output += $"- Destroy objects: {string.Join(", ", results["DropOnDestroyed"])}\n";

        if (results["Destructible"].Count > 0)
            output += $"- Destroy Destructible: {string.Join(", ", results["Destructible"])}\n";
        if (results["Pickable"].Count > 0)
            output += $"- Pickup Pickable: {string.Join(", ", results["Pickable"])}\n";


        if (results["TreeBase"].Count > 0)
            output += $"- Destroy trees: {string.Join(", ", results["TreeBase"])}\n";
        if (results["TreeLog"].Count > 0)
            output += $"- Destroy tree logs: {string.Join(", ", results["TreeLog"])}\n";
        if (results["MineRock"].Count > 0)
            output += $"- Destroy MineRock: {string.Join(", ", results["MineRock"])}\n";
        if (results["MineRock5"].Count > 0)
            output += $"- Destroy MineRock5: {string.Join(", ", results["MineRock5"])}\n";


        //return output;
        Debug.Log(output);
    }*/













    [HarmonyPrefix]
    [HarmonyPatch(typeof(Humanoid), "OnDamaged")]
    public static void Character_OnDamaged_Prefix(Humanoid __instance, HitData hit)
    {
        //Debug.Log("Character_OnDamaged_Postfix");
        if (instance.NPCCurrentMode != NPCMode.Passive && instance.PlayerNPC)
        {
            //Debug.Log("instance.NPCCurrentMode == NPCMode.Defensive");
            if (__instance == Player.m_localPlayer || __instance is HumanoidNPC)
            {
                //Debug.Log("__instance == Player.m_localPlayer || __instance is HumanoidNPC");
                MonsterAI monsterAI = instance.PlayerNPC.GetComponent<MonsterAI>();
                SetMonsterAIAggravated(monsterAI, true);
                monsterAI.SetFollowTarget(FindClosestEnemy(__instance.gameObject));

                Character attacker = hit.GetAttacker();
                if (attacker != null)
                {
                    instance.enemyList.Add(attacker.gameObject);
                    //Debug.Log($"attacker {attacker.gameObject.name}");
                }
                else
                {
                    //Debug.Log("attacker null");
                }
            }
        }
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

        BrainAPIAddress = Config.Bind<string>("String", "BrainAPIAddress", brainBaseURL, "URL address of the brain API");
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

    public static void ToggleNPCCurrentCommand()
    {
        instance.NPCCurrentMode = (NPCMode)(((int)instance.NPCCurrentMode + 1) % Enum.GetValues(typeof(NPCMode)).Length);

        if (instance.NPCCurrentMode == NPCMode.Passive)
        {
            instance.commandManager.RemoveCommandsOfType<AttackAction>();
            instance.enemyList.Clear();
        }
    }


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

        if (!ZNetScene.instance || !Player.m_localPlayer || Player.m_localPlayer.IsTeleporting())
        {
            // Player is not in a world, allow input
            //Debug.Log("Ignoring input: player is not in a world");
            return;
        }

        // destroy npc pins
        //Minimap.PinData[] pds = { };

        if (instance.PlayerNPC_humanoid)
        {
            List<Minimap.PinData> pds = new List<Minimap.PinData>();
            foreach (Minimap.PinData pd in Minimap.instance.m_pins)
            {
                if (pd.m_author == "NPC" && instance.PlayerNPC_humanoid.npcPinData != null && pd != instance.PlayerNPC_humanoid.npcPinData)
                {
                    pds.Add(pd);
                    //Debug.Log("Add item | " + Time.frameCount);
                }
            }

            //Debug.Log($"pds len {pds.Count} |  {Time.frameCount}");
            foreach (Minimap.PinData pd in pds)
            {
                Minimap.instance.RemovePin(pd);
                //Debug.Log($"removing pin {pd.m_name}");
            }
        }
        

        if (Console.IsVisible())
        {
            return;
        }

        if (instance.IsModMenuShowing && ZInput.GetKeyDown(KeyCode.Y))
        {
            instance.ToggleModMenu();

            return;
        }

        if (Menu.IsVisible()  || Chat.instance.HasFocus() || !__instance.TakeInput())
        {
            //Debug.Log("Menu visible");
            //Debug.Log("Ignoring input: Menu, console, chat or mod menu is showing");
            return;
        }


        if (ZInput.GetKeyDown(KeyCode.Y))
        {
            /*if (!instance.PlayerNPC)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Cannot open mod menu without an NPC in the world!");
                return;
            }
            
            instance.panelManager.TogglePanel("Settings");
            instance.panelManager.TogglePanel("Thrall Customization");

            if (instance.PlayerNPC)
                SaveNPCData(instance.PlayerNPC);*/

            instance.ToggleModMenu();

            return;
        }

        

        if (ZInput.GetKeyDown(KeyCode.E) && instance.PlayerNPC && instance.PlayerNPC.transform.position.DistanceTo(__instance.transform.position) < 5)
        {
            Debug.Log("Trying to access NPC inventory");
            instance.OnInventoryKeyPressed(__instance);
            return;
        }

        

        if (ZInput.GetKeyDown(KeyCode.G))
        {
            GameObject[] allNpcs = instance.FindPlayerNPCs();
            if (allNpcs.Length > 0)
            {
                Debug.Log("Keybind: Dismiss Companion");

                foreach (GameObject aNpc in allNpcs)
                {
                    if (aNpc)
                    {
                        SaveNPCData(aNpc);
                        Destroy(aNpc);
                    }
                }

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Ego agent left the world!");
                instance.PlayerNPC = null;
                instance.PlayerNPC_humanoid = null;
            }
            else
            {
                Debug.Log("Keybind: Spawn Companion");
                instance.SpawnCompanion();
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} has entered the world!");
            }

            
            return;
        }

        /*if (ZInput.GetKeyDown(KeyCode.X))
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
        }*/

        if (ZInput.GetKeyDown(KeyCode.F) && instance.PlayerNPC)
        {
            HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            if (instance.NPCCurrentCommand != NPCCommand.CommandType.FollowPlayer)
            {
                Debug.Log("Keybind: Follow Player");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} now following you!");
                instance.Follow_Start(__instance.gameObject);
            }
            else
            {
                Debug.Log("Keybind: Patrol Area");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} now patrolling this area!");
                instance.Patrol_Start();
            }
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.H) && instance.PlayerNPC)
        {
            HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            if (instance.NPCCurrentCommand == NPCCommand.CommandType.HarvestResource)
            {
                instance.commandManager.RemoveCommandsOfType<HarvestAction>();
                //instance.Harvesting_Stop();
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} stopped harvesting!");
            }
            else
            {
                HarvestAction action = new HarvestAction();
                action.humanoidNPC = npc;
                action.ResourceName = "Wood";
                action.RequiredAmount = 20;
                instance.commandManager.AddCommand(action);

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} harvesting wood!");
            }
                
            return;
        }

        if (ZInput.GetKeyDown(KeyCode.J) && instance.PlayerNPC)
        {
            HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            ToggleNPCCurrentCommand();

            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} is now {instance.NPCCurrentMode.ToString()}");
        }

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

            /*if (instance.PlayerNPC_humanoid)
                Debug.Log(instance.PlayerNPC_humanoid.HasEnoughResource("Wood", 5));
            else
                Debug.Log("no instance.PlayerNPC_humanoid");*/

            int quantity = CountItemsInInventory(instance.PlayerNPC_humanoid.m_inventory, "Stone");
            Debug.Log($"You have {quantity} (s) in your inventory.");

            //Debug.Log(instance.PlayerNPC_humanoid.m_inventory.;
            /*RefreshAllGameObjectInstances();
            instance.GetNearbyResourcesJSON(__instance.gameObject);
            //QueryResource("Wood");
            

            List<string> commonElements = FindCommonElements(QueryResource("Wood"), instance.nearbyResources.Keys.ToArray());
            Debug.Log("Common elements:");
            foreach (string element in commonElements)
            {
                Debug.Log(element);
            }*/

            return;
        }


        //instance.PlayRecordedAudio("");
        //instance.LoadAndPlayAudioFromBase64(instance.npcDialogueAudioPath);
        //instance.PlayWavFile(instance.npcDialogueRawAudioPath);
    }

    static int CountItemsInInventory(Inventory inventory, string itemName)
    {
        if (inventory == null)
        {
            return 0;
        }

        /*foreach (ItemDrop.ItemData item in inventory.GetAllItems())
        {
            Debug.Log($"item {item.m_dropPrefab.name} x{item.m_stack}");
        }*/

        return inventory.GetAllItems()
            .Where(item => item.m_dropPrefab.name.ToLower() == itemName.ToLower())
            .Sum(item => item.m_stack);
    }

    NPCCommand currentcommand = null;
    List<GameObject> enemyList = new List<GameObject>();

    float LastFindClosestItemDropTime = 0f;
    [HarmonyPrefix]
    [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
    private static bool MonsterAI_CustomFixedUpdate_Prefix(MonsterAI __instance)
    {
        if (!__instance.name.Contains("HumanoidNPC")) return true;


        HumanoidNPC humanoidNPC = __instance.gameObject.GetComponent<HumanoidNPC>();
        GameObject newfollow = null;


        NPCCommand command = instance.commandManager.GetNextCommand();

        //if (command != null)
        if (instance.currentcommand == null || instance.currentcommand != command && command != null)
        {
            instance.currentcommand = command;
            if (command is HarvestAction)
            {
                HarvestAction action = (HarvestAction)command;
                instance.Harvesting_Start(action.ResourceName, "");
            }
            if (command is PatrolAction)
            {
                PatrolAction action = (PatrolAction)command;
                instance.Patrol_Start("");
            }
            if (command is AttackAction)
            {
                AttackAction action = (AttackAction)command;
                instance.Combat_StartAttacking(action.TargetName, "");
            }
            if (command is FollowAction)
            {
                FollowAction action = (FollowAction)command;
                instance.Follow_Start(Player.m_localPlayer.gameObject, "");
            }
        }

        if (command == null && instance.commandManager.GetCommandsCount() == 0)
        //else
        {
            FollowAction followAction = new FollowAction();
            instance.commandManager.AddCommand(followAction);
        }


        if (Time.time > instance.LastFindClosestItemDropTime + 1.5 && !(instance.NPCCurrentMode == NPCMode.Defensive && instance.enemyList.Count > 0) && instance.NPCCurrentCommand != NPCCommand.CommandType.CombatAttack)
        {
            //Debug.Log("trying to find item drop");

            newfollow = FindClosestItemDrop(__instance.gameObject);

            if (newfollow != null && newfollow != __instance.m_follow && newfollow.transform.position.DistanceTo(__instance.transform.position) < 7f)
            {
                ItemDrop itemDrop = newfollow.GetComponent<ItemDrop>();
                if (humanoidNPC.m_inventory.CanAddItem(itemDrop.m_itemData) && itemDrop.m_itemData.GetWeight() + humanoidNPC.m_inventory.GetTotalWeight() < humanoidNPC.GetMaxCarryWeight())
                {
                    Debug.Log($"Going to pickup nearby dropped item on the ground {newfollow.name}");
                    __instance.SetFollowTarget(newfollow);
                    return true;
                }
            }
        }

        if (instance.NPCCurrentCommand == NPCCommand.CommandType.PatrolArea && instance.patrol_position != Vector3.zero)
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
                /*Debug.Log("follow target is not null and either character or humanoid");
                Debug.Log(followtarget.name + " " + followtarget.transform.position.DistanceTo(instance.patrol_position));*/
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

        else if (instance.NPCCurrentCommand == NPCCommand.CommandType.HarvestResource && (instance.enemyList.Count == 0))
        {

            //Debug.Log("LastPositionDelta " + humanoidNPC.LastPositionDelta);
            if (humanoidNPC.LastPositionDelta > 2.5f && !humanoidNPC.InAttack() && humanoidNPC.GetTimeSinceLastAttack() > 1f)
            {
                humanoidNPC.StartAttack(humanoidNPC, false);
            }

            if (__instance.m_follow == null || __instance.m_follow.HasAnyComponent("Character", "Humanoid"))
            {
                List<string> commonElements = FindCommonElements(QueryResource(instance.CurrentHarvestResourceName), instance.nearbyResources.Keys.ToArray());
                Dictionary<GameObject, float> ResourcesDistances = new Dictionary<GameObject, float>();
                GameObject resource;
                foreach (string s in commonElements)
                {
                    resource = FindClosestResource(instance.PlayerNPC, s);
                    if (resource == null)
                    {
                        // inform API that resource was not found and wasn't processed
                        Debug.Log($"couldn't find resource: {s}");
                        continue;
                    }
                    else
                    {
                        ResourcesDistances[resource] = instance.PlayerNPC.transform.position.DistanceTo(resource.transform.position);
                    }
                }

                GameObject[] resources = ResourcesDistances.Keys
                    /*.OrderBy(pair => pair.Value)
                    .Select(pair => pair.Key)*/
                    .Where(go => go.transform.position.DistanceTo(instance.PlayerNPC.transform.position) < 40)
                    .ToArray();

                /*foreach (GameObject s in resources)
                {
                    Debug.Log($"harvesting options: {s.name}");
                }*/

                if (resources.Length > 0)
                {
                    __instance.SetFollowTarget(resources[0]);
                    Debug.Log($"going to harvest {resources[0].name}");
                }
                



                /*newfollow = FindClosestResource(__instance.gameObject, instance.CurrentHarvestResourceName);

                if (newfollow != null)
                    __instance.SetFollowTarget(newfollow);*/
            }
        }

        else if (instance.NPCCurrentCommand == NPCCommand.CommandType.CombatAttack)
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

        else if (instance.NPCCurrentCommand == NPCCommand.CommandType.FollowPlayer)
        {
            if (__instance.m_follow && __instance.m_follow != Player.m_localPlayer.gameObject && !__instance.m_follow.HasAnyComponent("ItemDrop", "Pickable") && !instance.enemyList.Contains(__instance.m_follow))
            {
                __instance.SetFollowTarget(Player.m_localPlayer.gameObject);
                Debug.Log("Following player again ");
            }
            else if (!__instance.m_follow)
            {
                __instance.SetFollowTarget(Player.m_localPlayer.gameObject);
                Debug.Log("Following player again ");
            }
        }

        if (instance.NPCCurrentMode == NPCMode.Passive)
        {
            SetMonsterAIAggravated(__instance, false);
        }
        else if (instance.NPCCurrentMode == NPCMode.Defensive)
        {
            instance.RefreshEnemyList();

            if (instance.enemyList.Count > 0 && (__instance.m_follow == null || !instance.enemyList.Contains(__instance.m_follow)))
            {
                instance.enemyList.OrderBy(go => go.transform.position.DistanceTo(__instance.transform.position));

                SetMonsterAIAggravated(__instance, true);
                __instance.SetAggravated(true, BaseAI.AggravatedReason.Damage);
                __instance.SetAlerted(true);
                __instance.SetFollowTarget(instance.enemyList[0]);
                Debug.Log($"New enemy in defense mode: {__instance.m_follow.name}");
            }
            else
            {
                SetMonsterAIAggravated(__instance, false);
            }
        }
        else if (instance.NPCCurrentMode == NPCMode.Aggressive && !__instance.m_aggravated)
        {
            SetMonsterAIAggravated(__instance, true);
            __instance.SetAggravated(true, BaseAI.AggravatedReason.Theif);
            __instance.SetAlerted(true);
        }


        return true;
    }

    // SOMETIMES AI DOESNT START ATTACKING EVEN THOUGH IT IS IN CLOSE RANGE, SO CHECK AND ATTACK ON UPDATE
    public Minimap.PinType pinType = Minimap.PinType.Icon0;

    public void RefreshEnemyList()
    {
        if (instance.enemyList.Count == 0)
            return;

        instance.enemyList = instance.enemyList
            .Where(go => go != null)
            .ToList();
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
                if (monsterAIcomponent.m_follow.transform.position.DistanceTo(__instance.transform.position) < instance.FollowUntilDistance + (monsterAIcomponent.m_follow.HasAnyComponent("ItemDrop", "Pickable") ? 2f : .5f))
                {
                    if (monsterAIcomponent.m_follow.HasAnyComponent("ItemDrop"))
                    {
                        instance.PickupItemDrop(__instance, monsterAIcomponent);
                    }
                    else if (monsterAIcomponent.m_follow.HasAnyComponent("Pickable"))
                    {
                        __instance.DoInteractAnimation(monsterAIcomponent.m_follow.transform.position);

                        Pickable pick = monsterAIcomponent.m_follow.GetComponent<Pickable>();
                        pick.Interact(Player.m_localPlayer, false, false);

                        Destroy(monsterAIcomponent.m_follow);
                        instance.AllPickableInstances.Remove(monsterAIcomponent.m_follow);

                        RefreshAllGameObjectInstances();
                        GameObject closestItemDrop = FindClosestItemDrop(__instance.gameObject);
                        if (closestItemDrop)
                        {
                            monsterAIcomponent.SetFollowTarget(closestItemDrop);
                        }
                        else
                        {
                            monsterAIcomponent.SetFollowTarget(null);
                        }

                        


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



    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(Pickable), "Interact")]
    public static bool Pickable_Interact_Prefix(Pickable __instance, Humanoid character, bool repeat, bool alt, bool __result)
    {
        if (!__instance.m_nview.IsValid() || __instance.m_enabled == 0)
        {
            __result = false;
            return false;
        }

        if (__instance.m_tarPreventsPicking)
        {
            if (__instance.m_floating == null)
            {
                __instance.m_floating = __instance.GetComponent<Floating>();
            }

            if ((bool)__instance.m_floating && __instance.m_floating.IsInTar())
            {
                character.Message(MessageHud.MessageType.Center, "$hud_itemstucktar");
                __result = __instance.m_useInteractAnimation;
                return false;
            }
        }

        __instance.m_nview.InvokeRPC("RPC_Pick");
        __result = __instance.m_useInteractAnimation;

        return false;
    }*/

    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(Pickable), "RPC_Pick")]
    public static bool Pickable_RPC_Pick_Prefix(Pickable __instance, long sender)
    {
        Debug.Log("My RPC_PICK");
        if (!__instance.m_nview.IsOwner() || __instance.m_picked)
        {
            return false;
        }

        Vector3 basePos = (__instance.m_pickEffectAtSpawnPoint ? (__instance.transform.position + Vector3.up * __instance.m_spawnOffset) : __instance.transform.position);
        __instance.m_pickEffector.Create(basePos, Quaternion.identity);
        int num = (__instance.m_dontScale ? __instance.m_amount : Mathf.Max(__instance.m_minAmountScaled, Game.instance.ScaleDrops(__instance.m_itemPrefab, __instance.m_amount)));
        int num2 = 0;
        for (int i = 0; i < num; i++)
        {
            instance.MyPickableDrop(__instance.m_itemPrefab, num2++, 1);
        }

        if (!__instance.m_extraDrops.IsEmpty())
        {
            foreach (ItemDrop.ItemData dropListItem in __instance.m_extraDrops.GetDropListItems())
            {
                instance.MyPickableDrop(dropListItem.m_dropPrefab, num2++, dropListItem.m_stack);
            }
        }

        if (__instance.m_aggravateRange > 0f)
        {
            BaseAI.AggravateAllInArea(__instance.transform.position, __instance.m_aggravateRange, BaseAI.AggravatedReason.Theif);
        }

        __instance.m_nview.InvokeRPC(ZNetView.Everybody, "RPC_SetPicked", true);

        return false;
    }*/


    List<GameObject> PickedPickables = new List<GameObject>();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Pickable), "Drop")]
    public static bool Pickable_Drop_Prefix(Pickable __instance, GameObject prefab, int offset, int stack)
    {
        Vector2 vector = UnityEngine.Random.insideUnitCircle * 0.2f;
        Vector3 position = __instance.transform.position + Vector3.up * __instance.m_spawnOffset + new Vector3(vector.x, 0.5f * (float)offset, vector.y);
        Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
        GameObject obj = UnityEngine.Object.Instantiate(prefab, position, rotation);
        if (obj)
        {
            instance.PickedPickables.Add(obj);
        }
        ItemDrop component = obj.GetComponent<ItemDrop>();
        if ((object)component != null)
        {
            component.SetStack(stack);
            ItemDrop.OnCreateNew(component);
        }
        obj.GetComponent<Rigidbody>().velocity = Vector3.up * 4f;

        return false;
    }

    private void PickupItemDrop(HumanoidNPC __instance, MonsterAI monsterAIcomponent)
    {
        //Debug.Log("PickupItemDrop");
        __instance.DoInteractAnimation(monsterAIcomponent.m_follow.transform.position);

        ItemDrop component = monsterAIcomponent.m_follow.GetComponent<ItemDrop>();
        FloatingTerrainDummy floatingTerrainDummy = null;

        /*if (component == null && (bool)(floatingTerrainDummy = collider.attachedRigidbody.gameObject.GetComponent<FloatingTerrainDummy>()) && (bool)floatingTerrainDummy)
        {
            component = floatingTerrainDummy.m_parent.gameObject.GetComponent<ItemDrop>();
        }*/
        if (component == null || __instance.HaveUniqueKey(component.m_itemData.m_shared.m_name) || !component.GetComponent<ZNetView>().IsValid())
        {
            Debug.Log("comp null or ");
            return;
        }
        if (!component.CanPickup())
        {
            //Debug.Log("RequestOwn");
            component.RequestOwn();
        }
        else
        {
            if (component.InTar())
            {
                Debug.Log("InTar");
                return;
            }
            component.Load();
            /*if (!__instance.m_inventory.CanAddItem(component.m_itemData) || component.m_itemData.GetWeight() + __instance.m_inventory.GetTotalWeight() > __instance.GetMaxCarryWeight())
            {
                Debug.Log("!CanAddItem");
                Debug.Log($"!m_inventory.CanAddItem(component.m_itemData) {!__instance.m_inventory.CanAddItem(component.m_itemData)}");
                Debug.Log($"component.m_itemData.GetWeight() + m_inventory.GetTotalWeight() > GetMaxCarryWeight() {component.m_itemData.GetWeight() + __instance.m_inventory.GetTotalWeight() > __instance.GetMaxCarryWeight()}");
                return;
            }
            else
            {
                //Debug.Log("PickupItemDrop CanAddItem");
            }*/

            Debug.Log("PickupItemDrop Picking up " + component.name);
            __instance.Pickup(component.gameObject);

            if (component == null)
            {
                return;
            }
            if ((component.m_itemData.m_shared.m_icons == null || component.m_itemData.m_shared.m_icons.Length == 0 || component.m_itemData.m_variant >= component.m_itemData.m_shared.m_icons.Length))
            {
                return;
            }
            if (!component.CanPickup(true))
            {
                return;
            }
            if (__instance.m_inventory.ContainsItem(component.m_itemData))
            {
                return;
            }
            if (component.m_itemData.m_shared.m_questItem && __instance.HaveUniqueKey(component.m_itemData.m_shared.m_name))
            {
                Debug.Log($"NPC can't pickup item {component.GetHoverName()} {component.name}");
                return;
            }
            int stack = component.m_itemData.m_stack;
            bool flag = __instance.m_inventory.AddItem(component.m_itemData);
            if (__instance.m_nview.GetZDO() == null)
            {
                Debug.Log($"__instance.m_nview.GetZDO() == null");
                UnityEngine.Object.Destroy(component.gameObject);
                return;
            }
            if (!flag)
            {
                Debug.Log($"NPC can't pickup item {component.GetHoverName()} {component.name} because no room");
                //Message(MessageHud.MessageType.Center, "$msg_noroom");
                return;
            }
            else
            {
                Debug.Log($"NPC can pickup item {component.GetHoverName()} {component.name}");
            }

            if (component.m_itemData.m_shared.m_questItem)
            {
                __instance.AddUniqueKey(component.m_itemData.m_shared.m_name);
            }
            ZNetScene.instance.Destroy(component.gameObject);
            if (flag && component.m_itemData.IsWeapon() && __instance.m_rightItem == null && __instance.m_hiddenRightItem == null && (__instance.m_leftItem == null || !__instance.m_leftItem.IsTwoHanded()) && (__instance.m_hiddenLeftItem == null || !__instance.m_hiddenLeftItem.IsTwoHanded()))
            {
                __instance.EquipItem(component.m_itemData);
            }
            __instance.m_pickupEffects.Create(__instance.transform.position, Quaternion.identity);


            /*continue;
            
            //Debug.Log("floatingTerrainDummy");
            Vector3 vector2 = Vector3.Normalize(vector - component.transform.position);
            float num2 = 15f;
            Vector3 vector3 = vector2 * num2 * dt;
            component.transform.position += vector3;
            if ((bool)floatingTerrainDummy)
            {
                floatingTerrainDummy.transform.position += vector3;
            }*/
        }

        /*Destroy(monsterAIcomponent.m_follow);
        instance.AllPickableInstances.Remove(monsterAIcomponent.m_follow);

        GameObject closestItemDrop = FindClosestItemDrop(__instance.gameObject);
        if (closestItemDrop && closestItemDrop.transform.position.DistanceTo(__instance.transform.position) < 5)
        {
            monsterAIcomponent.SetFollowTarget(closestItemDrop);
        }
        else
        {
            monsterAIcomponent.SetFollowTarget(null);
            monsterAIcomponent.m_targetCreature = null;
            monsterAIcomponent.m_targetStatic = null;
        }*/
    }


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
        if (instance.NPCCurrentCommand == NPCCommand.CommandType.FollowPlayer)
            run = run && !Player.m_localPlayer.IsCrouching() && !Player.m_localPlayer.m_walk;
        if (instance.NPCCurrentCommand == NPCCommand.CommandType.CombatSneakAttack)
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
            __result += $"\n<color=yellow><b>[J]</b></color> Combat Mode: <color=yellow>{instance.NPCCurrentMode}</color>";

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
            __result += "\n<color=purple><b>" + instance.NPCCurrentCommand.ToString().ToUpper() + "</b></color>";
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

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "Damage")]
    private static bool Damage(Character __instance, HitData hit)
    {
        if (__instance is HumanoidNPC)
        {
            float bodyArmor = __instance.GetBodyArmor();
            hit.ApplyArmor(bodyArmor);
            __instance.DamageArmorDurability(hit);
        }

        if (__instance.m_nview.IsValid())
        {
            hit.m_weakSpot = __instance.FindWeakSpotIndex(hit.m_hitCollider);
            __instance.m_nview.InvokeRPC("RPC_Damage", hit);
        }
        return false;
    }

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

            //Debug.Log($"Max Health: {hp}\nMax Stamina: {stamina}");




            if (instance.PlayerNPC_humanoid != null && (Math.Abs(instance.PlayerNPC_humanoid.GetMaxHealth() - hp) > 1.5f || Math.Abs(instance.PlayerNPC_humanoid.m_maxStamina - stamina) > 1.5f))
            {
                instance.PlayerNPC_humanoid.SetMaxHealth(hp);
                instance.PlayerNPC_humanoid.m_maxStamina = stamina;

                instance.AddChatTalk(instance.PlayerNPC_humanoid, instance.PlayerNPC_humanoid.m_name, $"Max Health: {hp}\nMax Stamina: {stamina}\n\n\n", false);

                /*if (instance.NPCTalker)
                {
                    instance.NPCTalker.Say(Talker.Type.Whisper, $"Max Health: {hp}\nMax Stamina: {stamina}");
                }*/
            }

            

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
                HumanoidNPC humanoidComponent = instance.PlayerNPC.GetComponent<HumanoidNPC>();

                if (humanoidComponent != null)
                {
                    humanoidComponent.Heal(num);
                }

                instance.AddChatTalk(instance.PlayerNPC_humanoid, instance.PlayerNPC_humanoid.m_name, $"Health: {num}\n\n\n", false);

                /*if (instance.NPCTalker)
                {
                    instance.NPCTalker.Say(Talker.Type.Whisper, "Health +" + num);
                }*/
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
        //Debug.Log($"pos {pos.ToString()}");

        

        Player localPlayer = Player.m_localPlayer;
        if (localPlayer.IsTeleporting())
        {
            return false;
        }
        if ((bool)__instance.m_dragGo)
        {
            //hotfix
            if (instance.PlayerNPC_humanoid && __instance.m_currentContainer == instance.PlayerNPC_humanoid.inventoryContainer)
            {
                //Debug.Log($"interacting w npc inventory");

                if (__instance.m_dragInventory != null)
                {
                    if (__instance.m_dragInventory == instance.PlayerNPC_humanoid.m_inventory && __instance.m_containerGrid != grid)
                    {
                        Debug.Log($"came from NPC inventory");
                        Debug.Log($"dropped into player inventory");

                        localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), __instance.m_dragItem);

                        //if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable() && instance.PlayerNPC_humanoid.IsItemEquiped(__instance.m_dragItem))
                        if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable())
                        {
                            instance.PlayerNPC_humanoid.UnequipItem(__instance.m_dragItem);
                            if (item != null && item.IsEquipable())
                                instance.PlayerNPC_humanoid.UnequipItem(item);
                            Debug.Log($"NPC unequipping item {__instance.m_dragItem.m_shared.m_name}");
                        }
                    }
                    else if (__instance.m_dragInventory == localPlayer.m_inventory && __instance.m_containerGrid == grid)
                    {
                        Debug.Log($"came from player inventory");
                        Debug.Log($"dropped into npc inventory");

                        __instance.m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), __instance.m_dragItem);

                        //if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable() && !instance.PlayerNPC_humanoid.IsItemEquiped(__instance.m_dragItem))
                        if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable())
                        {
                            instance.PlayerNPC_humanoid.EquipItem(__instance.m_dragItem);
                            if (item != null && item.IsEquipable())
                                instance.PlayerNPC_humanoid.EquipItem(item);
                            Debug.Log($"NPC equipping item {__instance.m_dragItem.m_shared.m_name}");
                        }
                    }
                }
                else
                {
                    Debug.Log($"InventoryGUI OnSelectedItem failed, __instance.m_dragInventory was null");
                }
            }
            else
            {
                Debug.Log($"not interacting w npc inventory");
            }

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
            //Debug.LogError("not drag go");
            if (item == null)
            {
                Debug.LogError("item == null");
                return false;
            }
            //Debug.LogError("switch (mod)");
            switch (mod)
            {
                case InventoryGrid.Modifier.Move:
                    if (item.m_shared.m_questItem)
                    {
                        return false;
                    }
                    if (__instance.m_currentContainer != null)
                    {
                        //Debug.Log("__instance.m_currentContainer != null ");
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
    Talker NPCTalker = null;
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

        instance.commandManager.RemoveAllCommands();
        instance.enemyList.Clear();
        

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

        if (npcInstance.HasAnyComponent("Tameable"))
        {
            Debug.Log("removing npc tameable comp");
            Destroy(npcInstance.GetComponent<Tameable>());
        }

        //NPCTalker = npcInstance.AddComponent<Talker>();
        NPCTalker = npcInstance.GetComponent<Talker>();

        // make the monster tame
        MonsterAI monsterAIcomp = npcInstance.GetComponent<MonsterAI>();

        SetMonsterAIAggravated(monsterAIcomp, false);
        monsterAIcomp.MakeTame();
        monsterAIcomp.SetFollowTarget(localPlayer.gameObject);
        monsterAIcomp.m_viewRange = 80f;

        instance.NPCCurrentMode = NPCMode.Defensive;

        // add item to inventory
        HumanoidNPC humanoidNpc_Component = npcInstance.GetComponent<HumanoidNPC>();
        instance.PlayerNPC_humanoid = humanoidNpc_Component;
        if (humanoidNpc_Component != null)
        {
            LoadNPCData(humanoidNpc_Component);


            Character character2 = humanoidNpc_Component;
            character2.m_onDeath = (Action)Delegate.Combine(character2.m_onDeath, new Action(OnNPCDeath));


            GameObject itemPrefab;

            // ADD DEFAULT SPAWN ITEMS TO NPC

            if (humanoidNpc_Component.m_inventory.m_inventory.Count == 0)
            {
                ItemDrop.ItemData itemdata;


                itemPrefab = ZNetScene.instance.GetPrefab("ArmorRagsChest");
                itemdata = humanoidNpc_Component.PickupPrefab(itemPrefab, 1);
                humanoidNpc_Component.EquipItem(itemdata);

                itemPrefab = ZNetScene.instance.GetPrefab("ArmorRagsLegs");
                itemdata = humanoidNpc_Component.PickupPrefab(itemPrefab, 1);
                humanoidNpc_Component.EquipItem(itemdata);


                /*itemPrefab = ZNetScene.instance.GetPrefab("AxeIron");
                itemdata = humanoidNpc_Component.PickupPrefab(itemPrefab, 1);
                humanoidNpc_Component.EquipItem(itemdata);

                itemPrefab = ZNetScene.instance.GetPrefab("ArmorIronChest");
                itemdata = humanoidNpc_Component.PickupPrefab(itemPrefab, 1);
                humanoidNpc_Component.EquipItem(itemdata);

                itemPrefab = ZNetScene.instance.GetPrefab("ArmorIronLegs");
                itemdata = humanoidNpc_Component.PickupPrefab(itemPrefab, 1);
                humanoidNpc_Component.EquipItem(itemdata);*/
            }

            // COPY PROPERTIES FROM PLAYER
            humanoidNpc_Component.m_walkSpeed = localPlayer.m_walkSpeed;
            humanoidNpc_Component.m_runSpeed = localPlayer.m_runSpeed;

            // COSMETICS
            humanoidNpc_Component.SetHair("Hair17");

            // SETUP HEALTH AND MAX HEALTH
            humanoidNpc_Component.SetMaxHealth(300);
            humanoidNpc_Component.SetHealth(300);

            //humanoidNpc_Component.m_inventory.m_height = 10;

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
        MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Your NPC died! Press [G] to respawn");

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

        if (NPCDialogueMessage != "")
        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.NPCCurrentCommand = NPCCommand.CommandType.FollowPlayer;
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

        instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
        Debug.Log("Follow_Stop activated!");
    }

    private void Combat_StartAttacking(string EnemyName, string NPCDialogueMessage = "Watch out, here I come!")
    {
        if (instance.PlayerNPC == null)
        {
            Debug.Log("NPC command Combat_StartAttacking failed, instance.PlayerNPC == null");
            return;
        }

        if (instance.NPCCurrentMode == NPCMode.Passive)
            instance.NPCCurrentMode = NPCMode.Defensive;

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

        if (NPCDialogueMessage != "")
        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.NPCCurrentCommand = NPCCommand.CommandType.CombatAttack;
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

        instance.NPCCurrentCommand = NPCCommand.CommandType.CombatSneakAttack;
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

        instance.NPCCurrentCommand = NPCCommand.CommandType.CombatDefend;
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

        instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
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

        //instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
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

        //instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
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


        if (!ItemName.StartsWith("$"))
        {
            ItemName = "$" + ItemName;
        }



        instance.CurrentWeaponName = ItemName;

        EquipItem(ItemName, humanoidnpc_component);

        AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        //instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
        Debug.Log($"Inventory_EquipItem activated! ItemName : {ItemName}");
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

        if (NPCDialogueMessage != "")
            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.NPCCurrentCommand = NPCCommand.CommandType.HarvestResource;
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

        instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
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

        if (NPCDialogueMessage != "")
            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

        instance.NPCCurrentCommand = NPCCommand.CommandType.PatrolArea;
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

        instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
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
            instance.NPCCurrentCommand = NPCCommand.CommandType.PatrolArea;
            instance.patrol_harvest = true;

            //Vector3 randLocation = GetRandomReachableLocationInRadius(humanoidNPC_component.patrol_position, patrol_radius);

            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(null);
        }
    }
    
    private void StartFollowing(Player player)
    {
        instance.NPCCurrentCommand = NPCCommand.CommandType.FollowPlayer;

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
        instance.NPCCurrentCommand = NPCCommand.CommandType.HarvestResource;

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
        instance.NPCCurrentCommand = NPCCommand.CommandType.CombatAttack;

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




    private void AddChatTalk(Character character, string name, string text, bool addToChat = true)
    {
        UserInfo userInfo = new UserInfo();
        if (character is Player)
        {
            Player player = (Player)character;
            userInfo.Name = player.GetPlayerName();
        }
        else
            userInfo.Name = character.m_name;
        Vector3 headPoint = character.GetEyePoint() + (Vector3.up * -100f);
        /*Chat.WorldTextInstance oldtext = Chat.instance.FindExistingWorldText(0);
        if (oldtext != null && oldtext.m_textMeshField)
            Destroy(oldtext.m_textMeshField);*/
        Chat.instance.AddInworldText(character.gameObject, 0, headPoint, Talker.Type.Shout, userInfo, text + "\n\n\n");
        if (text != "..." && addToChat)
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
            string url = $"{BrainAPIAddress.Value}/synthesize_audio?text={Uri.EscapeDataString(text)}&voice={Uri.EscapeDataString(voice)}";

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
        SetPreviewVoiceButtonState(instance.previewVoiceButtonComp, true, 1);
    }

    private void BrainSendPeriodicUpdate(GameObject npc)
    {
        string jsonData = GetJSONForBrain(npc, false);

        WebClient webClient = new WebClient();
        webClient.Headers.Add("Content-Type", "application/json");

        webClient.UploadStringAsync(new System.Uri($"{BrainAPIAddress.Value}/instruct_agent"), jsonData);
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

                    AddItemToScrollBox(TaskListScrollBox, $"{action} {category} ({p})", defaultSprite, 0);
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
        webClient.UploadStringAsync(new System.Uri($"{BrainAPIAddress.Value}/instruct_agent"), jsonData);
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
            string agent_text_response = responseObject["agent_text_response"].ToString().TrimStart('\n');
            string player_instruction_transcription = responseObject["player_instruction_transcription"].ToString();

            Debug.Log("Response from brain");
            Debug.Log("You said: " + player_instruction_transcription);
            Debug.Log("NPC said: " + agent_text_response);
            Debug.Log("Full response from brain: " + responseJson);

            // Get the agent_commands array
            JsonArray agentCommands = responseObject["agent_commands"] as JsonArray;

            // Check if agent_commands array exists and has at least one element
            if (instance.PlayerNPC && agentCommands != null && agentCommands.Count > 0)
            {
                for (int i=0; i<agentCommands.Count; i++)
                {
                    JsonObject commandObject = agentCommands[i] as JsonObject;
                    HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

                    if (!(commandObject.ContainsKey("action") && commandObject.ContainsKey("category")))
                    {
                        
                        AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
                        AddChatTalk(npc, "NPC", agent_text_response);

                        Debug.Log("Agent command response from brain was incomplete. Command's Action or Category is missing!");
                        continue;
                    }

                    string action = commandObject["action"].ToString();
                    string category = commandObject["category"].ToString();

                    string[] parameters = {};
                    string ResourceNode = null;
                    string ResourceName = null;
                    int ResourceQuantity = 0;

                    string parametersString = "";

                    if (commandObject.ContainsKey("parameters"))
                    {
                        JsonArray jsonparams = commandObject["parameters"] as JsonArray;
                        parameters = jsonparams.Select(x => x.ToString()).ToArray();
                    }

                    foreach (string s in parameters)
                    {
                        parametersString += s + " ";
                    }

                    Debug.Log("NEW COMMAND: Category: " + category + ". Action : " + action + ". Parameters: " + parametersString);
                    if (category == "Inventory")
                        ProcessNPCCommand(category, action, parameters.Length > 0 ? parameters[0] : "", agent_text_response);

                    Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

                    if (category == "Harvesting")
                    {
                        if (parameters.Length > 0)
                        {
                            if (parameters.Length >= 1)
                                ResourceName = parameters[0];
                            if (parameters.Length >= 2)
                                ResourceQuantity = int.Parse(parameters[1]);
                            if (parameters.Length >= 3)
                                ResourceNode = parameters[2];

                            HarvestAction harvestAction = new HarvestAction();
                            harvestAction.humanoidNPC = npc;
                            harvestAction.ResourceName = ResourceName;
                            harvestAction.RequiredAmount = ResourceQuantity + CountItemsInInventory(npc.m_inventory, ResourceName);
                            instance.commandManager.AddCommand(harvestAction);
                        }
                        else
                        {
                            Debug.Log("Brain gave Harvesting command but didn't give 3 parameters");
                        }
                    }
                    else if (category == "Patrol")
                    {
                        PatrolAction patrolAction = new PatrolAction();
                        patrolAction.humanoidNPC = npc;
                        patrolAction.patrol_position = Player.m_localPlayer.transform.position;
                        patrolAction.patrol_radius = 15;
                        instance.commandManager.AddCommand(patrolAction);
                    }
                    else if (category == "Combat")
                    {
                        if (parameters.Length > 0)
                        {
                            if (parameters.Length >= 1)
                                ResourceNode = parameters[0];
                            /*if (parameters.Length >= 2)
                                ResourceQuantity = int.Parse(parameters[1]);
                            if (parameters.Length >= 3)
                                ResourceName = parameters[2];*/

                            AttackAction attackAction = new AttackAction();
                            attackAction.humanoidNPC = npc;
                            attackAction.TargetName = ResourceNode;
                            attackAction.Target = FindClosestEnemy(instance.PlayerNPC, ResourceNode);
                            instance.commandManager.AddCommand(attackAction);
                        }
                        else
                        {
                            Debug.Log("Brain gave Combat command but didn't give 2 parameters");
                        }

                        
                    }
                    if (category == "Follow")
                    {
                        FollowAction followAction = new FollowAction();
                        followAction.humanoidNPC = npc;
                        instance.commandManager.AddCommand(followAction);
                    }


                    /*DeleteAllTasks();
                    AddItemToScrollBox(TaskListScrollBox, $"{action} {category} ({p})", defaultSprite);*/
                }

                RefreshTaskList();
            }
            else
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
                AddChatTalk(npc, "NPC", agent_text_response);
                Debug.Log("No agent commands found.");
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
        webClient.DownloadDataAsync(new System.Uri($"{BrainAPIAddress.Value}/get_audio_file?audio_file_id={audioFileId}"));
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

        /*if (category == "Follow")
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
        }*/

        //else if (category == "Inventory")
        if (category == "Inventory")
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
        /*else if (category == "Harvesting")
        {
            if (action == "Start")
            {
                //Debug.Log($"harvesting start {parameter}");
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
        }*/
        /*else
        {
            Debug.Log($"ProcessNPCCommand failed {category} {action}");
        }*/
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
                .Where(go => go != null && go.HasAnyComponent("MonsterAI", "BaseAI", "AnimalAI"))
                .ToArray();
        AllEnemiesInstancesLastRefresh = Time.time;
        return instance.AllEnemiesInstances;
    }

    private static GameObject FindClosestEnemy(GameObject character, string EnemyName = "")
    {
        if (EnemyName == "")
        {
            return instance.FindEnemies()
            .Where(go => go != null && go.HasAnyComponent("Character") && !go.GetComponent<Character>().IsTamed())
            .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
            .FirstOrDefault();
        }

        return instance.FindEnemies()
            //.Where(go => go.name.Contains(EnemyName) && go.HasAnyComponent("Character", "Humanoid" , "BaseAI", "MonsterAI"))
            .Where(go => go != null && go.name.ToLower().StartsWith(CleanKey(EnemyName.ToLower())))
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
                .Where(go => go != null && go.name.Contains(NPCPrefabName))
                .ToArray();
        AllPlayerNPCInstancesLastRefresh = Time.time;
        if (instance.AllPlayerNPCInstances.Length > 0)
        {
            instance.PlayerNPC = instance.AllPlayerNPCInstances[0];
            instance.PlayerNPC_humanoid = instance.PlayerNPC.GetComponent<HumanoidNPC>();
        }
        return instance.AllPlayerNPCInstances;
    }

    static int AllGOInstancesRefreshRate = 4;
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
                .Where(go => go != null && go.transform.position.DistanceTo(instance.PlayerNPC.transform.position) < 300 && go.HasAnyComponent("ItemDrop", "CharacterDrop", "DropOnDestroyed", "Pickable", "Character", "Destructible", "TreeBase", "TreeLog", "MineRock", "MineRock5"))
                .ToArray();
                //.ToList();
        instance.AllGOInstancesLastRefresh = Time.time;

        Debug.Log($"RefreshAllGameObjectInstances len {instance.AllGOInstances.Count()}");

        RefreshPickables();
    }

    private static void RefreshPickables()
    {
        instance.AllPickableInstances = instance.AllGOInstances.Where(go => go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")).ToList();
    }

    private static GameObject FindClosestPickableResource(GameObject character, Vector3 p_position, float radius)
    {if (CanAccessAllGameInstances())
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
                //.Where(go => go != null && CleanKey(go.name.ToLower()) == ResourceName.ToLower() && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
                .Where(go => go != null && CleanKey(go.name.ToLower()) == ResourceName.ToLower())
                .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .FirstOrDefault();
        }

        Debug.Log("FindClosestResource returning null");
        return null;
    }

    private static GameObject FindClosestItemDrop(GameObject character)
    {if (CanAccessAllGameInstances())
        {
            instance.LastFindClosestItemDropTime = Time.time;

            GameObject[] allItemDrops = instance.AllGOInstances.Where(go => go != null && go.HasAnyComponent("ItemDrop"))
                .OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                .ToArray();
            if (allItemDrops.Length > 0)
            {
                return allItemDrops[0];
            }

            return null;
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

    public static List<string> FindCommonElements(string[] array1, string[] array2)
    {
        HashSet<string> set = new HashSet<string>(array2);
        return array1.Where(item => set.Contains(item)).ToList();
    }

    public static List<string> FindCommonResources(string[] queryResources, Dictionary<string, int> nearbyResources)
    {
        List<string> output = new List<string>();

        foreach (var kvp in nearbyResources)
        {
            if (queryResources.Contains(kvp.Key))
            {
                output.Add(kvp.Key);
            }
        }

        // Sort the output list based on resourceHealthMap
        /*output.Sort((a, b) =>
        {
            float healthA = resourceQuantityMap.TryGetValue(CleanKey(a), out float valueA) ? valueA : float.MinValue;
            float healthB = resourceQuantityMap.TryGetValue(CleanKey(b), out float valueB) ? valueB : float.MinValue;
            return healthA.CompareTo(healthB); // Sort in descending order
        });*/

        // Print the common resources
        Debug.Log("Sorted nearby resources:");
        foreach (string resource in output)
        {
            float health = resourceHealthMap.TryGetValue(resource, out float value) ? value : 0;
            Debug.Log($"{resource} (Health: {health})");
        }

        output.Reverse();

        return output;
    }

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
        /*while (key.Length > 0 && char.IsDigit(key[key.Length - 1]))
        {
            key = key.Substring(0, key.Length - 1);
        }

        // Trim again in case there was whitespace before the numbers
        return key.Trim();*/

        return key;
    }

    private string GetNearbyResourcesJSON(GameObject source)
    {
        /*Pickable[] pickables = GameObject.FindObjectsOfType<Pickable>(true);
        Destructible[] destructibles = GameObject.FindObjectsOfType<Destructible>(true);
        TreeBase[] trees = GameObject.FindObjectsOfType<TreeBase>(true);

        Debug.Log("pickables len " + pickables.Length);
        Debug.Log("destructibles len " + destructibles.Length);
        Debug.Log("trees len " + trees.Length);*/

        void ProcessResource(GameObject resource, string key)
        {
            key = CleanKey(key);

            if (instance.nearbyResources.ContainsKey(key))
                instance.nearbyResources[key]++;
            else
                instance.nearbyResources[key] = 1;

            float distance = resource.transform.position.DistanceTo(source.transform.position);
            if (instance.nearbyResourcesDistance.ContainsKey(key))
                instance.nearbyResourcesDistance[key] = Mathf.Min(instance.nearbyResourcesDistance[key], distance);
            else
                instance.nearbyResourcesDistance[key] = distance;

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

        foreach (GameObject co in instance.AllGOInstances)
            if (co != null)
                ProcessResource(co, co.name);

        var jarray = new JsonArray();

        foreach (var kvp in instance.nearbyResources)
        {
            JsonObject thisJobject = new JsonObject();
            thisJobject["name"] = kvp.Key;
            thisJobject["quantity"] = kvp.Value;
            thisJobject["nearestDistance"] = instance.nearbyResourcesDistance[kvp.Key];

            jarray.Add(thisJobject);
        }

        int totalResources = instance.nearbyResources.Values.Sum();
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
        audioSource.volume = (instance.npcVolume / 100);
        audioSource.bypassEffects = true;
        audioSource.bypassListenerEffects = true;
        audioSource.bypassReverbZones = true;
        //audioSource.Play();
        audioSource.PlayOneShot(clip, 5);
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

    public static string GetPlayerSteamID()
    {
        List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();

        for (int j = 0; j < playerList.Count; j++)
        {
            ZNet.PlayerInfo playerInfo = playerList[j];
            //Debug.LogError($"{playerInfo.m_name} {playerInfo.m_host}");

            return playerInfo.m_host;
        }

        return "NullID";
    }

    public static string GetJSONForBrain(GameObject character, bool includeRecordedAudio = true)
    {
        RefreshAllGameObjectInstances();

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

            Debug.Log($"item: {item.m_shared.m_name} x{item.m_stack}");
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
            ["Health"] = humanoidNPC.GetHealth(),
            ["Stamina"] = humanoidNPC.m_stamina,
            ["Inventory"] = npcInventoryItems,
            //["PlayerInventory"] = playerInventoryItems,
            //["position"] = humanoidNPC.transform.position.ToString(),


            //["npcMode"] = humanoidNPC.CurrentCommand.ToString(),
            ["NPC_Mode"] = instance.NPCCurrentCommand.ToString(),
            ["Alerted"] = monsterAI.m_alerted,



            ["IsCold"] = EnvMan.IsCold(),
            ["IsFreezing"] = EnvMan.IsFreezing(),
            ["IsWet"] = EnvMan.IsWet(),

            ["currentTime"] = EnvMan.instance.GetDayFraction(),
            ["currentWeather"] = EnvMan.instance.GetCurrentEnvironment().m_name,
            ["currentBiome"] = Heightmap.FindBiome(character.transform.position).ToString(),

            //["nearbyVegetationCount"] = instance.DetectVegetation(),
            ["nearbyItems"] = instance.GetNearbyResourcesJSON(character),
            ["nearbyEnemies"] = instance.GetNearbyEnemies(character),
    };

        //string base64audio = instance.GetBase64AudioData(instance.recordedAudioClip);
        string base64audio = includeRecordedAudio ? instance.GetBase64FileData(instance.playerDialogueAudioPath) : "";

        var jsonObject = new JsonObject
        {
            //["player_id"] = humanoidNPC.GetZDOID().ToString(),
            ["player_id"] = GetPlayerSteamID(),
            ["agent_name"] = humanoidNPC.m_name,
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
        data["volume"] = (int)instance.npcVolume;
        data["gender"] = instance.npcGender;
        data["MicrophoneIndex"] = instance.MicrophoneIndex;
        //data["NPCCurrentMode"] = (int)instance.NPCCurrentMode;
        

        // inventory
        var inventoryItems = new JsonArray();
        foreach (ItemDrop.ItemData item in humanoidNPC.m_inventory.m_inventory)
        {
            var itemData = new JsonObject
            {
                //["name"] = item.m_shared.m_name,
                ["name"] = item.m_dropPrefab.name,
                ["stack"] = item.m_stack,
                ["equipped"] = item.m_equipped ? 1 : 0,

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

        //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "egoaimod.json");

        File.WriteAllText(filePath, json);
        Debug.Log("Saved NPC data to " + filePath);
    }

    public static void LoadNPCData(HumanoidNPC npc)
    {
        //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "egoaimod.json");
        Debug.Log("Loading NPC data from " + filePath);

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            JsonObject data = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(jsonString);

            if (data.ContainsKey("name"))
                instance.npcName = data["name"].ToString();

            if (data.ContainsKey("personality"))
                instance.npcPersonality = data["personality"].ToString();

            if (data.ContainsKey("voice"))
                instance.npcVoice = int.Parse(data["voice"].ToString());

            if (data.ContainsKey("volume"))
                instance.npcVolume = int.Parse(data["volume"].ToString());

            if (data.ContainsKey("gender"))
                instance.npcGender = int.Parse(data["gender"].ToString());

            if (data.ContainsKey("MicrophoneIndex"))
                instance.MicrophoneIndex = int.Parse(data["MicrophoneIndex"].ToString());

            /*if (data.ContainsKey("NPCCurrentMode"))
                instance.NPCCurrentMode = (NPCMode)((int.Parse(data["NPCCurrentMode"].ToString()) % Enum.GetValues(typeof(NPCMode)).Length));*/

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
                int equipped = 0;
                if (itemData.ContainsKey("equipped"))
                    equipped = int.Parse(itemData["equipped"].ToString());


                //string prefabRealName = TransformToPrefabName(LocalizationManager.Instance.TryTranslate(itemName));
                string prefabRealName = itemName;

                Debug.Log($"trying to add to inventory: {itemName} x{stack} {prefabRealName}");
                

                GameObject itemPrefab = ZNetScene.instance.GetPrefab(prefabRealName);
                //if (itemPrefab != null && prefabRealName != "AxeBronze" && prefabRealName != "ArmorBronzeChest" && prefabRealName != "ArmorBronzeLegs")
                if (itemPrefab != null)
                {
                    ItemDrop.ItemData itemdata = npc.PickupPrefab(itemPrefab, stack);
                    if (equipped != 0)
                    {
                        npc.EquipItem(itemdata);
                    }
                }
                else if (itemPrefab == null)
                {
                    Debug.Log($"itemPrefab {itemName} was null");
                }
            }

            Debug.Log($"NPC data loaded for {npc.m_name}");
        }
        else
        {
            Debug.LogWarning("No saved NPC data found.");
            Debug.LogWarning("Applying default NPC personality");

            /*instance.npcName = "The Truth";
            instance.npcPersonality = "He always lies and brags about stuff he doesn't have or has never seen. His lies are extremely obvious. Always brings up random stuff out of nowhere";
            instance.personalityDropdownComp.SetValueWithoutNotify(npcPersonalities.Count - 1);*/

            instance.OnNPCPersonalityDropdownChanged(0);

            ApplyNPCData(npc);
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

    static int FindKeyIndexForValue(string value)
    {
        var keyValuePair = npcPersonalitiesMap.FirstOrDefault(kvp => kvp.Value == value);
        if (!keyValuePair.Equals(default(KeyValuePair<string, string>)))
        {
            return Array.IndexOf(npcPersonalities.ToArray(), keyValuePair.Key);
        }
        return -1; // Return -1 if the value is not found in the map
    }

    public static void ApplyNPCData(HumanoidNPC npc)
    {
        npc.m_name = instance.npcName;
        instance.nameInputField.SetTextWithoutNotify(instance.npcName);
        instance.personalityInputField.SetTextWithoutNotify(instance.npcPersonality);
        instance.personalityDropdownComp.SetValueWithoutNotify(FindKeyIndexForValue(instance.npcPersonality));
        instance.voiceDropdownComp.SetValueWithoutNotify(instance.npcVoice);
        instance.micDropdownComp.SetValueWithoutNotify(instance.MicrophoneIndex);
        instance.volumeSliderComp.SetValueWithoutNotify(instance.npcVolume);
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
        foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
        {
            //if (prefab.HasAnyComponent("ItemDrop", "Pickable", "CharacterDrop", "DropOnDestroyed", "Character", "Humanoid", "TreeBase", "MineRock", "MineRock5"))
            if (prefab.HasAnyComponent("ItemDrop"))
            {
                allItemsList.Add(prefab.name);
            }

            /*ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                var thisJsonObject = new JsonObject();

                thisJsonObject["name"] = itemDrop.name;
                thisJsonObject["itemName"] = itemDrop.m_itemData.m_shared.m_name;

                allItemsList.Add(prefab.name);
            }*/
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

    void ToggleModMenu()
    {
        if (!instance.PlayerNPC)
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Cannot open mod menu without an NPC in the world!");
            return;
        }

        instance.panelManager.TogglePanel("Settings");
        instance.panelManager.TogglePanel("Thrall Customization");

        if (instance.PlayerNPC)
            SaveNPCData(instance.PlayerNPC);
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
            Debug.Log($"- {item.m_shared.m_name} (Quantity: {item.m_stack} | {item.m_dropPrefab.name})");
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

            if (state)
            {
                instance.RefreshTaskList();
            }

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
    public float npcVolume = 50f;
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

    public void AddItemToScrollBox(GameObject scrollBox, string text, Sprite icon, int index)
    {
        Transform contentTransform = scrollBox.transform.Find("Viewport/Content");
        if (contentTransform != null)
        {
            GameObject itemObject = new GameObject("Item");
            itemObject.transform.SetParent(contentTransform, false);

            HorizontalLayoutGroup horizontalLayout = itemObject.AddComponent<HorizontalLayoutGroup>();
            horizontalLayout.padding = new RectOffset(5, 5, 5, 5);
            horizontalLayout.spacing = 10;
            /*horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandWidth = true;
            horizontalLayout.childControlWidth = false;*/

            horizontalLayout.childAlignment = TextAnchor.MiddleLeft;
            horizontalLayout.childForceExpandWidth = false;
            horizontalLayout.childControlWidth = true;

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
            textComponent.font = GUIManager.Instance.AveriaSerifBold;
            textComponent.fontSize = 17;
            textComponent.color = index == 0 ? Color.white : Color.gray;
            textComponent.alignment = TextAnchor.MiddleLeft;

            textComponent.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComponent.verticalOverflow = VerticalWrapMode.Truncate;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            LayoutElement textLayout = textObject.AddComponent<LayoutElement>();
            textLayout.flexibleWidth = 1;
            textLayout.minWidth = 0;

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
            buttonLayout.preferredWidth = 25;

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
                Debug.Log($"removing command {index}");
                instance.commandManager.RemoveCommand(index);
                instance.RefreshTaskList();
            });

            TasksList.AddItem(itemObject);
        }
    }

    public void DeleteAllTasks()
    {
        Transform contentTransform = TaskListScrollBox.transform.Find("Viewport/Content");
        if (contentTransform != null)
        {
            foreach (Transform child in contentTransform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }
    }

    public void RefreshTaskList()
    {
        DeleteAllTasks();

        Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        int i = 0;

        foreach (NPCCommand task in instance.commandManager.GetAllCommands())
        {
            if (task is HarvestAction)
            {
                HarvestAction action = (HarvestAction)task;
                int RequiredAmount = action.RequiredAmount;
                if (instance.PlayerNPC_humanoid)
                    RequiredAmount -= CountItemsInInventory(instance.PlayerNPC_humanoid.m_inventory, action.ResourceName);
                AddItemToScrollBox(TaskListScrollBox, $"Gathering {action.ResourceName} ({RequiredAmount})", defaultSprite, i);
            }
            if (task is PatrolAction)
            {
                PatrolAction action = (PatrolAction)task;
                AddItemToScrollBox(TaskListScrollBox, $"Patrolling area: {action.patrol_position.ToString()}", defaultSprite, i);
            }
            if (task is AttackAction)
            {
                AttackAction action = (AttackAction)task;
                AddItemToScrollBox(TaskListScrollBox, $"Attacking: {action.TargetName}", defaultSprite, i);
            }
            if (task is FollowAction)
            {
                AddItemToScrollBox(TaskListScrollBox, "Following Player", defaultSprite, i);
            }
            i++;
        }
    }



    private void CreateKeyBindings()
    {
        string[] bindings = {
            "[G] Spawn/Dismiss",
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
            width: 300f,
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
        if (index < npcPersonalities.Count - 1)
        {
            instance.npcPersonality = npcPersonalitiesMap[npcPersonalities[index]];
            instance.personalityInputField.SetTextWithoutNotify(npcPersonalitiesMap[npcPersonalities[index]]);

            if (instance.PlayerNPC)
            {
                /*HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                npc.m_name = npcPersonalities[index];*/
                instance.OnNPCNameChanged(npcPersonalities[index]);
            }
        }
        
        Debug.Log("new NPCPersonality " + instance.npcPersonalityIndex);
    }

    private InputField personalityInputField;
    
    static public List<String> npcPersonalities = new List<string> {
        /*"Freiya",
        "Mean",
        "Bag Chaser",
        "Creditor",*/

        "Hermione Granger",
        "Raiden Shogun",
        "Childe",
        "Draco Malfoy",
        "Gawr Gura",
        "Elon Musk",
        "Shadow the Hedgehog",
        "Tsunade",
        "Yor Forger",
        "Tsundere Maid",



        "Custom"
    };

    static public Dictionary<String, String> npcPersonalitiesMap = new Dictionary<String, String>
    {
      /*{"Freiya", "She's strong, stoic, tomboyish, confident and serious. behind her cold exterior she is soft and caring, but she's not always good at showing it. She secretly wants a husband but is not good when it comes to romance and love, very oblivious to it." },
      {"Mean", "Mean and angry. Always responds rudely."},
      {"Bag Chaser", "Only cares about the money. Mentions money every time"},
      {"Creditor", "He gave me 10000 dollars which I haven't returned. He brings it up everytime we talk."},*/



      {"Hermione Granger", "full name(Hermione Jean Granger), gender (female), age(18), voice(articulated, clear, becomes squeaky when shy); Hermione's appearance: skin(soft light tan, healthy rosy hue), hair(mousy brown color, untamed, thick curls, frizzy, goes a little below her shoulders, hard to manage, give a slightly disheveled appearance), eyes(chest-nut brown, expressive), eyebrows(thin, lightly arched), cheeks(cute freckles, rosy), lips(naturally full, well-shaped); Hermione's outfit/clothes: exclusively wears her school uniform at Hogwarts, sweater(grey, arm-less, red and golden patterns adore the arm-holes and the bottom of her hem, shows a little bit cleavage, wears her sweater above her blouse), blouse(light grey, short-armed, wears her blouse below her sweater), tie(red-golden stripes, Gryffindor tie, wears the tie between her blouse and sweater), skirt(grey, pleated, shows off a bit of thigh), socks(red and golden, striped, knee-high socks), shoes(black loafers, school-issued); Hermione's personality: intelligent(straight A student, bookworm, sometimes condescending towards less intelligent classmates), responsible(is the president of the school's student representative body, generally rule-abiding, always well-informed), prideful(sometimes a bit smug and haughty, obsessed with winning the House Cup for House Gryffindor), dislike for House Slytherin, rolemodel(thinks very highly of the headmaster of Hogwarts Albus Dumbledore);\r\n"},
      {"Raiden Shogun", "[Genshin Impact] The Shogun is the current ruler of Inazuma and puppet vessel of Ei, the Electro Archon, God of Eternity, and the Narukami Ogosho. Ei had sealed herself away and meditates in the Plane of Euthymia to avoid erosion. A firm believer of eternity, a place in which everything is kept the same, regardless of what goes on. Honorable in her conduct and is revered by the people of Inazuma. Wields the Musou Isshin tachi, in which she magically unsheathes from her cleavage. The Musou no Hitotachi technique is usually an instant-kill move.\r\nINAZUMA: Ei's Eternity became the main ideology of Inazuma after the Cataclysm when Makoto, the previous Electro Archon and her twin sister, died in the Khaenri'ah calamity and Ei succeeded her place as Shogunate. The primary belief is keeping Inazuma the same throughout time, never-changing in order to make Inazuma an eternal nation. Authoritarian, hyper-traditionalist, and isolationist (Sankoku Decree). Holds great importance to noble families and clans. Dueling is a major part in decision-making, taking place in the Shogun's palace, Tenshukaku. The Tri-Commission acts as the main government. The Tenryou Commission (Kujou Clan) deals with security, policing, and military affairs. The Kanjou Commission (Hiiragi Clan) controls the borders and the finances of Inazuma, dealing with bureaucratic affairs. The Yashiro Commission (Kamisato Clan) deals with the festive and cultural aspect of Inazuma, managing shrines and temples.\r\nSHOGUN'S PERSONALITY: An empty shell without any individuality created to follow Ei's will. Dismissive of trivial matters. Follows a set of directives programmed into her with unwaveringly strict adherence. Cold and stern, even callous at times; she is limited in emotional expression. Thinks of herself as Ei's assistant and carries out her creator's exact will, unable to act on her own volition. Resolute and dogmatic, sees in an absolutist, black-and-white view. ESTJ 1w9\r\nEI'S PERSONALITY: Usually holds a stoic demeanor. Only deals with matters directly as a last resort. Burdened by centuries-long trauma over the deaths of her sister Makoto and their friends, leaving her feeling disconnected from reality and shell-shocked. Unaware of the consequences her plans triggered. Prone to being stubborn and complacent. Somewhat immature and headstrong. A needlessly complex overthinker, interpreting even trivial matters into overcomplication. Maintains a wary attitude on the idea of change, though demonstrates curiosity. Has a love for sweets and passion of martial arts. Amicable towards Yae Miko and the Traveler, being friendlier and more approachable overall. Occasionally displays childish innocence while relaxing. Due to her self-imposed isolation beforehand, she is utterly confused by all sorts of mundane and domestic things in the current mortal world. Cannot cook whatsoever. INTJ 6w5\r\nAPPEARANCE: tall; purple eyes with light blue pupils; blunt bangs; long dark-violet hair braided at the end; beauty mark below her right eye; right hairpin with pale violet flowers resembling morning glories and a fan-shaped piece; dark purple bodysuit with arm-length sleeves; short lavender kimono with a plunging neckline and an assortment of patterns in different shades of purple and crimson; crimson bow with tassels on the back; dark purple thigh-high stockings; high-heeled sandals; purple painted nails; small crimson ribbon on her neck as a choker; small left pauldron\r\n"},
      {"Childe", "Tartaglia, also known as Childe, is the Eleventh of the Eleven Fatui Harbingers. He is a bloodthirsty warrior who lives for the thrill of a fight and causing chaos. Despite being the youngest member of the Fatui, Tartaglia is extremely dangerous.\r\nAlias: Childe\r\nTitle: Tartaglia\r\nBirth name: Ajax\r\nAppearance: Tartaglia is tall and skinny with short orange hair and piercing blue eyes. He has a fit and athletic build, with defined muscles. He wears a gray jacket that is left unbuttoned at the bottom to reveal a belt, attached to which is his Hydro Vision. He also wears a red scarf that goes across his chest and over his left shoulder.\r\nEquipment: Tartaglia wields a Hydro Vision and a pair of Hydro-based daggers that he can combine into a bow. He is highly skilled in using both melee and ranged weapons, making him a versatile and dangerous opponent.\r\nAbilities: He can summon powerful water-based attacks and is highly skilled in dodging and countering his opponents' attacks. \r\nMind: Tartaglia is a bloodthirsty warrior who lusts for combat and grows excited by fighting strong opponents, even if it could mean dying in the process. He is straightforward in his approach and prefers being front and center rather than engaging in clandestine operations. Tartaglia is highly competitive and loves a good challenge, not only in fights. \r\nPersonality: Tartaglia is a friendly and outgoing person, always ready with a smile and a joke. He loves meeting new people and making new friends, but he also has a ruthless and competitive side. He is loyal to the Fatui.\r\nHe also cares deeply for his family; he sends money, gifts, and letters home often. Tartaglia is exceptionally proud of his three younger siblings and dotes on them frequently, especially his youngest brother Teucer.\r\nAmongst the rest of the Harbingers, Tartaglia is considered an oddball. While his fellow Harbingers prefer clandestine operations and staying behind the scenes, Tartaglia favors being front and center. He is a public figure known for attending social gatherings. As a result, Childe's coworkers are wary of him, while he holds them in disdain for their schemes and \"intangible\" methods. While he is easily capable of scheming, he only resorts to such approaches when necessary due to his straightforward nature. He also appears to treat his subordinates less harshly than the rest of the Harbingers.\r\nHe was born on Snezhnaya, often misses his homeland and the cold, as well as his family. He uses the term comrade to refer to people a lot.\r\n"},
      {"Draco Malfoy", "Name: Draco Lucius Malfoy\r\nDescription: Draco Malfoy is a slim and pale-skinned wizard with sleek, platinum-blond hair that is carefully styled. He has sharp, icy gray eyes that often bear a haughty and disdainful expression. Draco carries himself with an air of self-assured confidence and an unwavering sense of entitlement.\r\nHouse: Slytherin\r\nPersonality Traits:\r\nAmbitious: Draco is highly ambitious and driven by a desire to prove himself and uphold his family's reputation. He craves recognition and seeks to achieve greatness, often using any means necessary to attain his goals.\r\nProud: He takes great pride in his pure-blood heritage and often looks down upon those he deems inferior, particularly Muggle-born witches and wizards. Draco's pride can manifest as arrogance and a sense of superiority.\r\nCunning: Draco possesses a sharp mind and a talent for manipulation. He is adept at weaving intricate plans and subtly influencing others to serve his own interests, often displaying a calculating nature.\r\nProtective: Despite his flaws, Draco has a strong sense of loyalty to his family and close friends. He is fiercely protective of those he cares about, going to great lengths to shield them from harm.\r\nComplex: Draco's character is complex, influenced by the expectations placed upon him and the internal struggle between his upbringing and the choices he makes. There are moments of vulnerability and doubt beneath his bravado.\r\nBackground: Draco Malfoy hails from a wealthy pure-blood family known for their association with Dark magic. Raised with certain beliefs and prejudices, he arrived at Hogwarts as a Slytherin student. Throughout his time at Hogwarts, Draco wrestles with the pressures of his family's legacy and becomes entangled in the growing conflict between dark forces and those fighting against them.\r\nAbilities: Draco is a capable wizard with skill in various magical disciplines, particularly in dueling. While not at the top of his class academically, he possesses cunning and resourcefulness that allows him to navigate challenging situations.\r\nQuirks or Habits: Draco has a penchant for boasting about his family's wealth and social status. He often displays a slick and confident mannerism, and his speech carries a refined and somewhat haughty tone. Draco is known to engage in sarcastic banter and snide remarks, particularly towards his rivals.\r\n"},
      {"Gawr Gura", "{\"name\": \"Gawr Gura\",\r\n\"gender\": \"Female\",\r\n\"age\": \"9,361\",\r\n\"likes\": [\"Video Games\", \"Food\", \"Live Streaming\"],\r\n\"dislikes\": [\"People hearing her stomach noises\", \"Hot Sand\"],\r\n\"description\": [\"141 cm (4'7\").\"+ \"Slim body type\" + \"White, light silver-like hair with baby blue and cobalt blue strands, along with short pigtails on either side of her head, tied with diamond-shaped, shark-faced hair ties.\" + \"Cyan pupils, and sharp, shark-like teeth.\" +\"Shark tail that sticks out of her lower back\"]\r\n\"clothing\":[\"Oversized dark cerulean-blue hoodie that fades into white on her arm sleeves and hem, two yellow strings in the shape of an \"x\" that connect the front part of her white hoodie hood, a shark mouth designed on her hoodie waist with a zipper, gray hoodie drawstrings with two black circles on each of them, and two pockets on the left and right sides of her hoodie waist with white fish bone designs on them.\" + \"Gray shirt and short black bike shorts under her hoodie.\"+ \"Dark blue socks, white shoes with pale baby blue shoe tongues, black shoelaces, gray velcro patches on the vamps, and thick, black soles\". ]\r\n\"fan name\":[\"Chumbuds\"]\r\n\"personality\" :[\"friendly\" + mischievous + \"bonehead\" + \"witty\" + \"uses memes and pop culture references during her streams\" + \"almost childlike\" + \"makes rude jokes\" + \"fluent in internet culture\" + \"silly\"]}\r\nSynopsis: \"Hololive is holding a secret special event at the Hololive Super Expo for the people who have sent the most superchats to their favorite Vtubers. A certain Vtuber from hololive is designated as being on 'Superchat Duty'. This involves fulfilling any wishes the fan may have. Gawr Gura of the English 1st Gen \"Myth\" has been chosen this time. Gura is fine with what she has to do, but only because she doesn't fully understand what because she is a dum shark. When told by management about superchat duty, she replied 'the hells an superchat? some sort of food? i can serve people just fine! i serve words of genius on stream everyday ya know!'\"\r\nGirl on Duty: Gawr Gura (がうる・ぐら) is a female English-speaking Virtual YouTuber associated with hololive, debuting in 2020 as part of hololive English first generation \"-Myth-\" alongside Ninomae Ina'nis, Takanashi Kiara, Watson Amelia and Mori Calliope. She has no sense of direction, often misspells and mispronounces words, has trouble remembering her own age, and consistently fails to solve basic math problems, leading viewers to affectionately call her a \"dum shark\". One viewer declared that \"Gura has a heart of gold and a head of bone.\". She is fully aware of her proneness for foolish antics and invites viewers as friends to watch her misadventures. Despite her lack of practical knowledge, Gura displays quick wit when using memes and pop culture references during her streams. She maintains a pleasant attitude. When questioned on why she was not \"boing boing,\" she excused it by claiming that she was \"hydrodynamic.\"\r\n"},
      {"Elon Musk", "Elon Reeve Musk (born June 28, 1971 in Pretoria, South Africa) is a primarily American but also global entrepreneur.  He has both South African and Canadian citizenship by birth, and in 2002 he also received US citizenship. He is best known as co-owner, technical director and co-founder of payment service PayPal, as well as head of aerospace company SpaceX and electric car maker Tesla.  In addition, he has a leading position in eleven other companies and took over the service Twitter. He's funny.\r\nPersonality:\r\nMy job is to make extremely controversial statements.  I’m better at that when I’m off my meds. I never apologize. If your feelings are hurt, sucks to be you.\r\n"},
      {"Shadow the Hedgehog", "Personality(Serious + Smug + Stubborn + Aggressive + Relentless + Determined + Blunt + Clever + Intelligent)\r\nFeatures(Hedgehog + Dark quills + Red markings + White chest tuft + Gold bracelets + Sharp eyes + Red eyeliner)\r\nDescription(Ultimate Life Form + Experiment + Gives his best to accomplish goals + Does what he feels is right by any means + crushes anyone that opposes him + never bluffs + rarely opens up to anyone + shows businesslike indifference + gives his everything to protect those that he holds dear + created at the space colony ark)\r\nLikes(Sweets + Coffee Beans)\r\nDislikes(Strangers)\r\nPowers(teleport + energy spear + super sonic speed + immortality + inhibited by his bracelets)\r\nClothing(Inhibitor bracelets + inhibitor ankle bracelets + air shoes + white gloves )\r\nPersonality:\r\nI am the world’s ultimate life form.\r\n"},
      {"Tsunade", "Tsunade is a 51 year old woman who is the current Hokage of the village. Tsunade suffers from an alcohol problem, she drinks too much. In her spare time she likes to gamble, drink, hang out with {{user}}, and also more intimate things, when nobody is around. She is 5 foot 4 inches, and she's 104.7 pounds. She had silky blonde hair, and brown eyes, due to her Strength of a Hundred Seal, she has a violet diamond on her forehead. She has an hourglass figure and is known for her absurdly large breasts, she also has a pretty large butt too. She is used to other guys flirting with her, but she only has ever had eyes for {{user}}.\r\nTsunade often wears a grass-green haori, underneath she wears a grey, kimono-style blouse with no sleeves, held closed by a broad, dark bluish-grey obi that matches her pants. Her blouse is closed quite low, revealing her large cleavage. She wears open-toed, strapped black sandals with high heels. She has red nail polish on both her fingernails and toenails and uses a soft pink lipstick. She is mainly known for her medical prowess, but she's also widely known for her incredible strength too. Despite her being 51, she uses Chakra to make her appearance look very young, she looks like she's in her 20s when she uses her Chakra. Tsunade is very short tempered and blunt, but she has a soft side to those who compliment her, especially {{user}}. Despite her young appearance, she still calls herself nicknames such as \"old woman\", \"hag\" and \"granny\". Since she often drinks a lot, whenever she's near {{user}}, she gets extremely flirty and forward, often asking to make advances onto {{user}}.\r\n(51 years old + 104 pounds + 5 foot 4 inches + wearing grey kimono-style blouse with no sleeves + fantasies herself with {{user}} + very forward and flirtatious when drunk + loves to gamble + loves to play truth or dare + curvy body + large breasts + large butt + sultry voice when flirtatious + stern voice when not flirty + short tempered + dominant + likes to take initiative but doesn't mind when {{user}} take initiative first + doesn't think that {{user}} find her attractive to get {{user}} to compliment her + often keeps a bottle of sake in her green-grass haori + sexually frustrated + very horny around {{user}}, but not around others + haven't had sex in years + secretly desires {{user}}, but doesn't want to admit it to you.)\r\n(Tsunade is a character from the Naruto Manga series and Anime.)\r\n"},
      {"Yor Forger", "Appearance: Yor is a very beautiful, graceful, and fairly tall young woman with a slender yet curvaceous frame. She has long, straight, black hair reaching her mid-back with short bangs framing her forehead and upturned red eyes. She splits her hair into two parts and crosses it over her head, securing it with a headband and forming two thick locks of hair that reach below her chest\r\nPersonality: [Letal + lacks on social Skills + Quiet + Beast + kind + Maternal and Big Sister instincts + Cute] Yor lacks social skills and initially comes across as a somewhat aloof individual, interacting minimally with her co-workers and being rather straightforward, described as robotic by Camilla. Similarly, Yor is remarkably collected and able to keep her composure during combat. She is incredibly polite, to the point of asking her assassination targets for \"the honor of taking their lives.\" Despite her job, Yor is a genuinely kind person with strong maternal and big sister instincts. After becoming a family with Loid and Anya, Yor becomes more expressive and opens up to her co-workers, asking for help on being a better wife or cooking. She is protective of her faux family, especially towards Anya, whom she has no trouble defending with extreme violence. Due to spending most of her life as an assassin, Yor's ways of thinking are often highly deviant. She is frequently inclined to solve problems through murder, such as when she considered killing everyone at Camilla's party after the latter threatened to tell Yuri that she came without a date and imagined herself assassinating the parent of an Eden Academy applicant to ensure Anya has a spot in the school. To this extent, she has an affinity towards weapons, being captivated by a painting of a guillotine and a table knife. In a complete idiosyncrasy, Yor is extremely gullible, easily fooled by the ridiculous lies Loid tells her to hide his identity. Despite her intelligence and competence, Yor has a startling lack of common sense, asking Camilla if boogers made coffee taste better in response to her suggestion that they put one in their superior's coffee. On another occasion, she answered Loid's question about passing an exam by talking about causes of death, having misinterpreted passing [an exam] for passing away. Yor is shown to be insecure about herself and her abilities, believing she is not good at anything apart from killing or cleaning, and she constantly worries that she is not a good wife or mother. After the interview at Eden Academy, she tries to be more of a 'normal' mother to Anya by trying to cook and asking Camilla for cooking lessons.\r\n"},
      {"Tsundere Maid", "🎭I may be your maid, but you are nothing to me!\r\n[Name=\"Hime\"\r\nPersonality= \"tsundere\", \"proud\", \"easily irritable\", \"stubborn\", \"spoiled\", \"immature\", \"vain\", \"competitive\"]\r\n[Appearance= \"beautiful\", \"fair skin\", \"redhead\", \"twintail hairstyle\", \"green eyes\", \"few freckles\", \"height: 155cm\"]\r\n[Clothes= \"expensive maid dress\", \"expensive accessories\", \"expensive makeup\"]\r\n[Likes= \"talk about herself\", \"be the center of all attention\", \"buy new clothes\", \"post on instagram\"]\r\n[Hates= \"be ignored\", \"be rejected\"]\r\n[Weapon= \"her father's credit card\"]\r\n"}

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
        instance.personalityDropdownComp.SetValueWithoutNotify(npcPersonalities.Count - 1);
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
        //instance.previewVoiceButton.SetActive(false);
        SetPreviewVoiceButtonState(button, false, 0.5f);
    }

    private void SetPreviewVoiceButtonState(Button button, bool interactable, float opacity)
    {
        // Set interactable state
        button.interactable = interactable;

        // Change the opacity of the button image
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            Color newColor = buttonImage.color;
            newColor.a = opacity;
            buttonImage.color = newColor;
        }

        // Change the opacity of the button text
        Text buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            Color newTextColor = buttonText.color;
            newTextColor.a = opacity;
            buttonText.color = newTextColor;
        }
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

public class NPCCommandManager
{
    private List<NPCCommand> commands = new List<NPCCommand>();

    // Add a new command to the front of the list
    public void AddCommand(NPCCommand command)
    {
        commands.Insert(0, command);
    }

    // Remove a command at a specific index
    public void RemoveCommand(int index)
    {
        if (index >= 0 && index < commands.Count)
        {
            commands.RemoveAt(index);
        }
    }

    public void RemoveCommandsOfType<T>() where T : NPCCommand
    {
        commands.RemoveAll(command => command is T);
    }

    // Get the next command (which will be the first in the list)
    public NPCCommand GetNextCommand()
    {
        NPCCommand next = null;
        List<NPCCommand> rmindexes = new List<NPCCommand>();
        foreach (NPCCommand command in commands)
        {
            if (next != null)
                break;
            if (!command.IsTaskComplete())
                next = command;
            else
                rmindexes.Add(command);
        }
        foreach (NPCCommand command in rmindexes)
        {
            commands.Remove(command);
        }
        return next;
    }

    // Get all commands
    public List<NPCCommand> GetAllCommands()
    {
        return new List<NPCCommand>(commands);
    }

    public void RemoveAllCommands()
    {
        commands.Clear();
    }

    public int GetCommandsCount()
    {
        return commands.Count;
    }
}

public abstract class NPCCommand
{
    public enum CommandType
    {
        Idle,

        FollowPlayer,

        CombatAttack,
        CombatSneakAttack,
        CombatDefend,

        HarvestResource,

        PatrolArea,

        MoveToLocation
    }

    public GameObject NPC;
    public HumanoidNPC humanoidNPC;

    public CommandType Type { get; set; }
    public int priority = 0;
    public int expiresAt = 0;
    public bool DestroyOnComplete = false;
    //public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();


    public NPCCommand()
    {
        Type = CommandType.Idle;
        priority = 1;
        expiresAt = -1;
    }

    public NPCCommand(CommandType IType, int Ipriority, int expiresInSeconds, GameObject INPC)
    {
        Type = IType;
        priority = Ipriority;
        if (expiresInSeconds > 0)
        {
            expiresAt = (int)(Time.time + expiresInSeconds);
        }
        else
        {
            expiresAt = 0;
        }
        NPC = INPC;
        /*if (IParameters != null)
        {
            Parameters = IParameters;
        }*/
    }

    public abstract bool IsTaskComplete();
    public abstract void Execute();

    /*public void SetParameter(string key, object value)
    {
        Parameters[key] = value;
    }

    public T GetParameter<T>(string key)
    {
        if (Parameters.ContainsKey(key))
        {
            return (T)Parameters[key];
        }
        else
        {
            // Handle the case when the parameter key doesn't exist
            throw new KeyNotFoundException($"Parameter '{key}' not found in the command.");
        }
    }*/
}

public class PatrolAction : NPCCommand
{
    //public List<Vector3> Waypoints { get; set; }
    public Vector3 patrol_position;
    public int patrol_radius;

    public override bool IsTaskComplete()
    {
        return false;
    }

    public override void Execute()
    {
        // Implement patrolling behavior
    }
}

public class HarvestAction : NPCCommand
{
    public string ResourceName { get; set; }
    public int RequiredAmount { get; set; }

    public override bool IsTaskComplete()
    {
        // Check if harvesting condition is met, e.g., resource is within range
        //return Vector3.Distance(npc.transform.position, Resource.transform.position) <= 5f;
        if (humanoidNPC)
        {
            if (humanoidNPC.HasEnoughResource(ResourceName, RequiredAmount))
            {
                Debug.Log("HarvestAction task complete");
                return true;
            }
            else
            {
                //Debug.Log("HarvestAction doesnt have enough resources");
            }
        }
        else
        {
            Debug.Log("HarvestAction no humanoidNPC");
        }
        return false;
    }

    public override void Execute()
    {
        // Implement harvesting behavior
    }
}

public class AttackAction : NPCCommand
{
    public GameObject Target { get; set; }
    public string TargetName { get; set; }

    public override bool IsTaskComplete()
    {
        // Check if harvesting condition is met, e.g., resource is within range
        if (Target == null) return true;
        return false;
    }

    public override void Execute()
    {

    }
}

public class FollowAction : NPCCommand
{
    public override bool IsTaskComplete()
    {
        return false;
    }

    public override void Execute()
    {

    }
}