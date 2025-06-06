﻿using System;
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
using System.Collections;
using ValheimAIMod;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine.Windows;
using System.Diagnostics.Eventing.Reader;
using System.Reflection;
using UnityEngine.InputSystem;

namespace ValheimAIModLoader
{
    [BepInPlugin("egoai.thrallmodlivepatch", "ego.ai Thrall Mod Live Patch", "0.0.1")]
    [BepInProcess("valheim.exe")]
    [BepInDependency("egoai.thrallmodloader", BepInDependency.DependencyFlags.HardDependency)]
    public class ValheimAIModLivePatch : BaseUnityPlugin
    {
        public enum NPCMode
        {
            Passive,
            Defensive,
            Aggressive
        }




        public static ValheimAIModLivePatch instance;
        private readonly Harmony harmony = new Harmony("egoai.thrallmodlivepatch");
    
        //private const string brainBaseURL = "http://localhost:5000";
        //private const string brainBaseURL = "https://valheim-agent-brain.fly.dev";
        private const string encryptedBrainBaseURL = "Ju1M0vL+9PbHCPO8Tr0Leb92JpHZZcYqtuCSwhjbizen4omyPMmvjXjfSZ9MBoCv";

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

        public class Resource
        {
            public string Name { get; set; }
            public int MinAmount { get; set; }
            public int MaxAmount { get; set; }
            public float Health { get; set; }

            public Resource(string name, int minAmount, int maxAmount, float health)
            {
                Name = name;
                MinAmount = minAmount;
                MaxAmount = maxAmount;
                Health = health;
            }

            public float CalculateEaseScore(float distance, bool HasWeapon)
            {
                // Constants for weighting different factors
                float AMOUNT_WEIGHT = HasWeapon ? 0.0f : 0.4f;
                float HEALTH_WEIGHT = HasWeapon ? 0.6f : 0.0f;
                float DISTANCE_WEIGHT = HasWeapon ? 0.4f : 0.6f;

                // Calculate sub-scores
                float amountScore = ((MinAmount + MaxAmount) / 2.0f) * 10; // Assuming max possible amount is 10
                float healthScore = 100 / Health; // Inverse relationship: lower health is better
                float distanceScore = 100 / (1 + distance); // Inverse relationship: closer is better

                // Combine sub-scores with weights
                float totalScore = (amountScore * AMOUNT_WEIGHT) +
                                   (healthScore * HEALTH_WEIGHT) +
                                   (distanceScore * DISTANCE_WEIGHT);

                return totalScore;
            }
        }

        private static Dictionary<string, Dictionary<string, List<Resource>>> resourceDatabase = new Dictionary<string, Dictionary<string, List<Resource>>>();
        private static Dictionary<string, float> resourceHealthMap = new Dictionary<string, float>();
        private static Dictionary<string, float> resourceQuantityMap = new Dictionary<string, float>();


        private static readonly byte[] Key = new byte[32]
        {
            23, 124, 67, 88, 190, 12, 45, 91,
            255, 7, 89, 45, 168, 42, 109, 187,
            23, 100, 76, 217, 154, 200, 43, 79,
            19, 176, 62, 9, 201, 33, 95, 128
        };
        private static readonly byte[] IV = new byte[16]
        {
        88, 145, 23, 200, 56, 178, 12, 90,
        167, 34, 78, 191, 78, 23, 12, 78
        };

        public static string Decrypt(string cipherText)
        {
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                using (var msDecrypt = new System.IO.MemoryStream(Convert.FromBase64String(cipherText)))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var srDecrypt = new System.IO.StreamReader(csDecrypt))
                {
                    return srDecrypt.ReadToEnd();
                }
            }
        }

        private static string GetBrainAPIAddress()
        {
            return Decrypt(encryptedBrainBaseURL);
        }

        public static bool IsInAWorld()
        {
            return ZNetScene.instance != null && Player.m_localPlayer != null;
        }

        public static bool IsLocalSingleplayer()
        {
            // Check if ZNet instance exists
            if (ZNet.instance == null)
            {
                Debug.LogWarning("ZNet instance is null. Unable to determine world type.");
                return false;
            }

            // Check if it's a dedicated server
            if (ZNet.instance.IsDedicated())
            {
                return false; // Dedicated server is always multiplayer
            }

            // Check if it's a local server (which could be singleplayer or non-dedicated multiplayer)
            if (ZNet.instance.IsServer())
            {
                // If it's a server and there's only one peer (the host), it's singleplayer
                return ZNet.instance.GetPeers().Count <= 1;
            }

            // If we're not the server, it's multiplayer
            return false;
        }

        private static bool ModInitComplete = false;

        private void DoModInit()
        {
            LogWarning("Initializing Thrall Mod!");
            Chat.instance.SendText(Talker.Type.Normal, "EGO.AI THRALL MOD LOADED!");

            CreateModMenuUI();

            instance.NPCCurrentMode = NPCMode.Defensive;

            

            instance.FindPlayerNPCs();
            if (instance.PlayerNPC)
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                LoadNPCData(npc);
            }


            PopulateDatabase();
            //PopulateAllItems();

            ModInitComplete = true;

            LogMessage("Thrall mod initialization complete");
            //Debug.Log("Thrall mod initialization complete");
        }

        private static void LogInfo(string s)
        {
            logger.LogInfo(s);
            logEntries.Add($"[Info] [{DateTime.Now.ToString()}] {s}");
        }

        private static void LogMessage(string s)
        {
            logger.LogMessage(s);
            logEntries.Add($"[Message] [{DateTime.Now.ToString()}] {s}");
        }

        private static void LogWarning(string s)
        {
            logger.LogWarning(s);
            logEntries.Add($"[Warning] [{DateTime.Now.ToString()}] {s}");
        }

        private static void LogError(string s)
        {
            logger.LogError(s);
            logEntries.Add($"[Error] [{DateTime.Now.ToString()}] {s}");
        }

        

        private static ManualLogSource logger;
        private static List<string> logEntries = new List<string>();

        private void Awake()
        {
            logger = Logger;
            
            instance = this;

            LogMessage("ego.ai Thrall ValheimAIModLivePatch Loaded! :)");
            LogWarning("This mod is designed for single-player gameplay and may not function correctly when used alongside other mods.");

            ConfigBindings();

            /*PopulateCraftingRequirements();
            PopulateBuildingRequirements();
            PopulateMonsterPrefabs();
            PopulateAllItems();*/

            playerDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "playerdialogue.wav");
            npcDialogueAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue.wav");
            npcDialogueRawAudioPath = Path.Combine(UnityEngine.Application.persistentDataPath, "npcdialogue_raw.wav");

            
            LogInfo("Setting up logging for Thrall");
            //Application.logMessageReceived += CaptureLog;
            instance.RestartSendLogTimer();


            if (IsInAWorld())
            {
                //LogWarning("Thrall mod loaded at runtime, trying to initialize");

                instance.DoModInit();
            }
            else
            {
                LogWarning("Thrall mod loaded at startup. Mod not initalized yet! Waiting for player to join a world...");
            }


            harmony.PatchAll(typeof(ValheimAIModLivePatch));
        }

        private void RestartSendLogTimer()
        {
            instance.SetTimer(30, () =>
            {
                instance.SendLogToBrain();
                instance.RestartSendLogTimer();
            });
        }

        private void CaptureLog(string logString, string stackTrace, LogType type)
        {
            string entry = $"[{Time.time}] [{type}] {logString}";
            if (type == LogType.Exception)
            {
                entry += $"\n{stackTrace}";
            }
            logEntries.Add(entry);

            // Optionally, you can set a max number of entries to keep in memory
            if (logEntries.Count > 10000)  // For example, keep last 10000 entries
            {
                logEntries.RemoveAt(0);
            }
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(ZNet), "Shutdown")] 
        private static bool ZNet_Shutdown_Prefix()
        {
            SaveLogs();
            return true;
        }

        private static bool SaveLogs()
        {
            LogInfo("Game is shutting down, sending accumulated logs");
            string FilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "lastlog.json");

            string res = "";
            foreach (string s in logEntries)
            {
                res += s + "\n";
            }

            File.WriteAllText(FilePath, res);
            return true;
        }*/



        [HarmonyPostfix]
        [HarmonyPriority(Priority.Low)]
        [HarmonyPatch(typeof(Player), "Awake")]
        public static void Player_Awake_Postfix(Player __instance)
        {
            //Debug.Log("Player_Awake");

            if (__instance == null)
                return;

            if (ZNetScene.instance == null)
            {
                //LogWarning("Player spawned but not in a game world!");
                return;
            }

            if (!ModInitComplete)
            {
                //LogWarning("Local player spawned, trying to initialize Thrall mod.");
                instance.DoModInit();
            }
            else
            {
                LogInfo("Skipping Thrall mod init because it is already initialized.");
            }

            instance.FindPlayerNPCs();

            if (!instance.PlayerNPC)
            {
                //LogWarning("Local player spawned, but there is no NPC in the world. Trying to spawn an NPC in 1 second...");

                instance.SetTimer(1f, () =>
                {
                    if (!instance.PlayerNPC) {
                        LogWarning("Spawning a Thrall into the world!");
                        instance.SpawnCompanion();
                        if (instance.PlayerNPC_humanoid)
                        {
                            string text = $"Hey there, I'm {(instance.PlayerNPC_humanoid.m_name != "" ? instance.PlayerNPC_humanoid.m_name : "a Thrall")}. Press and hold T to talk with me.";
                            instance.AddChatTalk(instance.PlayerNPC_humanoid, "NPC", text);
                            instance.BrainSynthesizeAudio(text, npcVoices[instance.npcVoice].ToLower());
                        } 
                    }
                });
            }
        }

        public void SetTimer(float duration, Action onComplete)
        {
            StartCoroutine(TimerCoroutine(duration, onComplete));
        }

        private System.Collections.IEnumerator TimerCoroutine(float duration, Action onComplete)
        {
            yield return new WaitForSeconds(duration);
            onComplete?.Invoke();
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

                    foreach (var sourceName in sourcePair.Value)
                    {
                        JsonObject source = new JsonObject();
                        source["Name"] = sourceName.Name;
                        source["MinAmount"] = sourceName.MinAmount;
                        source["MaxAmount"] = sourceName.MaxAmount;
                        source["Health"] = sourceName.Health;
                        sourceArray.Add(source);
                    }

                    resourceObject[sourcePair.Key] = sourceArray;
                }

                jsonObject[resourcePair.Key] = resourceObject;
            }

            string jsonFilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "resource_database.json");
            File.WriteAllText(jsonFilePath, IndentJson(jsonObject.ToString())); // '2' is for indentation
            LogInfo($"Saved resource database to {jsonFilePath}");
        }

        public static string IndentJson(string json)
        {
            int indentLevel = 0;
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < json.Length; i++)
            {
                char ch = json[i];
                switch (ch)
                {
                    case '{':
                    case '[':
                        sb.Append(ch);
                        if (!inQuotes)
                        {
                            sb.AppendLine();
                            Indent(++indentLevel, sb);
                        }
                        break;
                    case '}':
                    case ']':
                        if (!inQuotes)
                        {
                            sb.AppendLine();
                            Indent(--indentLevel, sb);
                        }
                        sb.Append(ch);
                        break;
                    case ',':
                        sb.Append(ch);
                        if (!inQuotes)
                        {
                            sb.AppendLine();
                            Indent(indentLevel, sb);
                        }
                        break;
                    case ':':
                        sb.Append(ch);
                        if (!inQuotes)
                            sb.Append(" ");
                        break;
                    case '"':
                        sb.Append(ch);
                        bool escaped = false;
                        var index = i;
                        while (index > 0 && json[--index] == '\\')
                            escaped = !escaped;
                        if (!escaped)
                            inQuotes = !inQuotes;
                        break;
                    default:
                        sb.Append(ch);
                        break;
                }
            }

            return sb.ToString();
        }

        private static void Indent(int count, StringBuilder sb)
        {
            for (int i = 0; i < count; i++)
                sb.Append("    ");
        }

        private void PopulateDatabase()
        {
            foreach (GameObject prefab in ZNetScene.instance.m_prefabs)
            {
                if (prefab.HasAnyComponent("TreeBase"))
                    CheckTreeBase(prefab);
                if (prefab.HasAnyComponent("TreeLog"))
                    CheckTreeLog(prefab);
                
                if (prefab.HasAnyComponent("Pickable"))
                    CheckPickables(prefab);
                if (prefab.HasAnyComponent("ItemDrop"))
                    CheckItemDrop(prefab);
                if (prefab.HasAnyComponent("MineRock"))
                    CheckMineRock(prefab);
                if (prefab.HasAnyComponent("MineRock5"))
                    CheckMineRock5(prefab);
                if (prefab.HasAnyComponent("Destructible"))
                    CheckDestructibles(prefab);

                if (prefab.HasAnyComponent("CharacterDrop"))
                    CheckCharacterDrop(prefab);
                if (prefab.HasAnyComponent("DropOnDestroyed"))
                    CheckDropOnDestroyed(prefab);
            }

            /*AddTreeRelationships();

            SortDatabase();*/


            SaveDatabaseToJson();
        }

        /*private void SortDatabase()
        {
            foreach (var resource in resourceDatabase.Keys)
            {
                Dictionary<string, List<string>> resources = resourceDatabase[resource];

                foreach (var key in resources.Keys.ToList())
                {
                    if (resources[key] != null && resources[key] is IEnumerable<string> stringList)
                    {
                        resourceDatabase[resource][key] = stringList.OrderBy(s => s, new CustomComparer()).ToList();

                        *//*if (resource == "Wood")
                            foreach (string s in resourceDatabase[resource][key])
                                Debug.Log($"{resource} {key} {s}");*//*
                    }
                }
            }
        }*/

        /*private void AddTreeRelationships()
        {
            foreach (var kvp in resourceDatabase)
            {
                Dictionary<string, List<string>> resources = kvp.Value;

                // Add logs that drop sub logs that drop this resource
                List<string> newLogs = new List<string>();
                newLogs.AddRange(resources["TreeLog"]);

                foreach (string r in resources["TreeLog"])
                {
                    if (logToLogMap.ContainsKey(r))
                    {
                        newLogs.AddRange(logToLogMap[r]);
                        //Debug.Log($"adding {logToLogMap[r].Count} tree logs to {kvp.Key}");
                    }
                }

                resourceDatabase[kvp.Key]["TreeLog"] = newLogs;

                // add trees that drop logs that drop this resource
                foreach (string r in resources["TreeLog"])
                {
                    if (logToTreeMap.ContainsKey(r))
                    {
                        resourceDatabase[kvp.Key]["TreeBase"].AddRange(logToTreeMap[r]);
                        //Debug.Log($"adding {logToTreeMap[r].Count} treebases to {kvp.Key}");

                        foreach (string x in logToTreeMap[r])
                        {
                            if (logToTreeMap.ContainsKey(x))
                            {
                                resourceDatabase[kvp.Key]["TreeBase"].AddRange(logToTreeMap[x]);
                                //Debug.Log($"adding {logToTreeMap[x].Count} treebases to {kvp.Key}");
                            }
                        }
                    }
                }
            }
        }*/


        private Dictionary<string, List<string>> logToTreeMap = new Dictionary<string, List<string>>();
        private Dictionary<string, List<string>> logToLogMap = new Dictionary<string, List<string>>();

        private void CheckTreeBase(GameObject prefab)
        {
            TreeBase treeBase = prefab.GetComponent<TreeBase>();
            if (treeBase != null && treeBase.m_dropWhenDestroyed != null && treeBase.m_dropWhenDestroyed.m_drops != null)
            {
                foreach (DropTable.DropData drop in treeBase.m_dropWhenDestroyed.m_drops)
                {
                    float health = -1;
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(treeBase.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = treeBase.m_health;
                    }

                    resourceQuantityMap[prefab.name] = treeBase.m_dropWhenDestroyed.m_dropMax;

                    Resource sourceResource = new Resource(prefab.name, treeBase.m_dropWhenDestroyed.m_dropMin, treeBase.m_dropWhenDestroyed.m_dropMax, resourceHealthMap[prefab.name]);
                    AddToDatabase(drop.m_item.name, "TreeBase", sourceResource);
                }

                // Store the relationship between the tree and its log prefab
                if (treeBase.m_logPrefab != null)
                {
                    /*if (!logToTreeMap.ContainsKey(treeBase.m_logPrefab.name))
                        logToTreeMap[treeBase.m_logPrefab.name] = new List<string>();
                    logToTreeMap[treeBase.m_logPrefab.name].Add(prefab.name);*/
                    Resource sourceResource = new Resource(prefab.name, 1, 1, treeBase.m_health);
                    AddToDatabase(treeBase.m_logPrefab.name, "TreeBase", sourceResource);
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
                    float health = -1;
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(treeBase.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = treeBase.m_health;
                    }

                    resourceQuantityMap[prefab.name] = treeBase.m_dropWhenDestroyed.m_dropMax;
                    Resource sourceResource = new Resource(prefab.name, treeBase.m_dropWhenDestroyed.m_dropMin, treeBase.m_dropWhenDestroyed.m_dropMax, resourceHealthMap[prefab.name]);

                    AddToDatabase(drop.m_item.name, "TreeLog", sourceResource);
                }

                if (treeBase.m_subLogPrefab != null)
                {
                    /*if (!logToLogMap.ContainsKey(treeBase.m_subLogPrefab.name))
                        logToLogMap[treeBase.m_subLogPrefab.name] = new List<string>();
                    logToLogMap[treeBase.m_subLogPrefab.name].Add(prefab.name);*/
                    Resource sourceResource = new Resource(prefab.name, 1, 1, treeBase.m_health);
                    AddToDatabase(treeBase.m_subLogPrefab.name, "TreeLog", sourceResource);
                }
            }
        }

        private void CheckCharacterDrop(GameObject prefab)
        {
            CharacterDrop characterDrop = prefab.GetComponent<CharacterDrop>();
            if (characterDrop != null && characterDrop.m_drops != null)
            {
                float health = -1;
                
                if (prefab.HasAnyComponent("Humanoid"))
                {
                    Humanoid humanoid = prefab.GetComponent<Humanoid>();
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(humanoid.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = humanoid.m_health;
                    }
                }
                else if (prefab.HasAnyComponent("Character"))
                {
                    Character humanoid = prefab.GetComponent<Character>();
                    if (resourceHealthMap.TryGetValue(prefab.name, out health))
                    {
                        resourceHealthMap[prefab.name] = Math.Max(humanoid.m_health, health);
                    }
                    else
                    {
                        resourceHealthMap[prefab.name] = humanoid.m_health;
                    }
                }
                foreach (CharacterDrop.Drop drop in characterDrop.m_drops)
                {
                    resourceQuantityMap[prefab.name] = characterDrop.m_drops.Count;
                    Resource sourceResource = new Resource(prefab.name, drop.m_amountMin, drop.m_amountMax, resourceHealthMap[prefab.name]);
                    AddToDatabase(drop.m_prefab.name, "CharacterDrop", sourceResource);
                }
            }
        }

        private void CheckDropOnDestroyed(GameObject prefab)
        {
            DropOnDestroyed dropOnDestroyed = prefab.GetComponent<DropOnDestroyed>();
            if (dropOnDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed != null && dropOnDestroyed.m_dropWhenDestroyed.m_drops != null)
            {
                float health = -1;
                if (resourceHealthMap.TryGetValue(prefab.name, out health))
                {
                    resourceHealthMap[prefab.name] = health;
                }
                else
                {
                    resourceHealthMap[prefab.name] = health;
                    if (resourceHealthMap[prefab.name] <= 0 && prefab.HasAnyComponent("WearNTear"))
                    {
                        WearNTear wnt = prefab.GetComponent<WearNTear>();
                        if (wnt != null)
                        {
                            resourceHealthMap[prefab.name] = wnt.m_health;
                        }
                    }
                }
                    
                    
                    
                foreach (DropTable.DropData drop in dropOnDestroyed.m_dropWhenDestroyed.m_drops)
                {
                    resourceQuantityMap[prefab.name] = dropOnDestroyed.m_dropWhenDestroyed.m_dropMax;
                    Resource sourceResource = new Resource(prefab.name, dropOnDestroyed.m_dropWhenDestroyed.m_dropMin, dropOnDestroyed.m_dropWhenDestroyed.m_dropMax, resourceHealthMap[prefab.name]);
                    if (drop.m_item)
                        AddToDatabase(drop.m_item.name, "DropOnDestroyed", sourceResource);
                }
            }
        }

        private void CheckItemDrop(GameObject prefab)
        {
            ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
            //if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
            //if (itemDrop != null && itemDrop.m_itemData != null && itemDrop.m_itemData.m_dropPrefab != null)
            if (itemDrop != null && itemDrop.m_itemData != null)
            {
                resourceQuantityMap[prefab.name] = itemDrop.m_itemData.m_stack;
                //AddToDatabase(itemDrop.m_itemData.m_shared.m_name, "ItemDrop", prefab.name);
                Resource sourceResource = new Resource(prefab.name, 1, itemDrop.m_itemData.m_stack, 2);
                AddToDatabase(prefab.name, "ItemDrop", sourceResource);
            }
        }

        private void CheckDestructibles(GameObject prefab)
        {
            Destructible destructible = prefab.GetComponent<Destructible>();
        
            if (destructible != null)
            {
                float health = -1;
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
                    Resource sourceResource = new Resource(prefab.name, 1, 1, resourceHealthMap[prefab.name]);
                    AddToDatabase(destructible.m_spawnWhenDestroyed.name, "Destructible", sourceResource);
                }
            }
        }

        private void CheckPickables(GameObject prefab)
        {
            Pickable pickable = prefab.GetComponent<Pickable>();
            if (pickable != null && pickable.m_itemPrefab != null)
            {
                resourceQuantityMap[prefab.name] = pickable.m_amount;
                Resource sourceResource = new Resource(prefab.name, pickable.m_amount, pickable.m_amount, 5);
                AddToDatabase(pickable.m_itemPrefab.name, "Pickable", sourceResource);
            }
        }

        private void CheckMineRock(GameObject prefab)
        {
            MineRock minerock = prefab.GetComponent<MineRock>();
            if (minerock != null && minerock.m_dropItems != null && minerock.m_dropItems.m_drops != null)
            {
                float health = -1;
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
                    Resource sourceResource = new Resource(prefab.name, drop.m_stackMin, drop.m_stackMax, resourceHealthMap[prefab.name]);
                    AddToDatabase(drop.m_item.name, "MineRock", sourceResource);
                }
            }
        }

        private void CheckMineRock5(GameObject prefab)
        {
            MineRock5 minerock = prefab.GetComponent<MineRock5>();
            if (minerock != null && minerock.m_dropItems != null && minerock.m_dropItems.m_drops != null)
            {
                float health = -1;
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
                    Resource sourceResource = new Resource(prefab.name, drop.m_stackMin, drop.m_stackMax, resourceHealthMap[prefab.name]);
                    AddToDatabase(drop.m_item.name, "MineRock5", sourceResource);
                }
            }
        }

        private static List<string> priorityOrderUnarmed = new List<string>
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

        private static List<string> priorityOrder = new List<string>
        {
            "TreeLog",
            "TreeBase",
            "MineRock",
            "MineRock5",

            "DropOnDestroyed",
            "Destructible",

            "ItemDrop",
            "Pickable",

            "CharacterDrop",
        };

        private void AddToDatabase(string resourceName, string sourceType, Resource sourceResource)
        {
            if (!resourceDatabase.ContainsKey(resourceName))
            {
                resourceDatabase[resourceName] = new Dictionary<string, List<Resource>>
                    {
                        { "TreeLog", new List<Resource>() },
                        { "TreeBase", new List<Resource>() },
                        { "MineRock", new List<Resource>() },
                        { "MineRock5", new List<Resource>() },

                        { "DropOnDestroyed", new List<Resource>() },
                        { "Destructible", new List<Resource>() },

                        { "ItemDrop", new List<Resource>() },
                        { "Pickable", new List<Resource>() },

                        { "CharacterDrop", new List<Resource>() },
                    };
            }

            resourceDatabase[resourceName][sourceType].Add(sourceResource);
        }

        public static Dictionary<string, List<Resource>> QueryResourceComplete(string resourceName, bool HasWeapon = true)
        {
            if (!resourceDatabase.ContainsKey(resourceName))
            {
                return new Dictionary<string, List<Resource>>(); // Return an empty array if resource is not found
            }

            var results = resourceDatabase[resourceName];
            var resourceList = new Dictionary<string, List<Resource>>();

            // Add all resources to the set without labels
            foreach (var sourceType in HasWeapon ? priorityOrder : priorityOrderUnarmed)
            {
                if (results.ContainsKey(sourceType))
                {
                    resourceList[sourceType] = results[sourceType];
                }
            }

            return resourceList;
        }

        private static List<List<string>> ConvertResourcesToNames(List<List<Resource>> resourceLists)
        {
            return resourceLists.Select(innerList =>
                innerList.Select(resource => resource.Name).ToList()
            ).ToList();
        }

        private static List<string> FlattenListOfLists(List<List<string>> nestedList)
        {
            return nestedList.SelectMany(innerList => innerList).ToList();
        }

        public static string[] QueryResource(string resourceName, bool HasWeapon = true)
        {
            if (!resourceDatabase.ContainsKey(resourceName))
            {
                return new string[0]; // Return an empty array if resource is not found
            }

            var results = resourceDatabase[resourceName];
            var resourceList = new List<Resource>();

            // Add all resources to the set without labels
            foreach (var sourceType in HasWeapon ? priorityOrder : priorityOrderUnarmed)
            {
                if (results.ContainsKey(sourceType))
                {
                    //LogInfo(sourceType);
                    resourceList.AddRange(results[sourceType]);
                }
            }

            // Print the array before returning
            /*Debug.Log($"Resources for '{resourceName}':");
            Debug.Log(string.Join(", ", resourceList.ToArray()));*/

            //return resourceList.ToArray();
            return resourceList.Select(r => r.Name).ToArray();
        }

        public static List<Resource> FindCommonResources(List<Resource> resources, List<string> names)
        {
            return resources.Where(resource => names.Contains(resource.Name, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        private static List<GameObject> SortResourcesByEase(Dictionary<Resource, GameObject> resourceObjects, Vector3 playerPosition, bool HasWeapon = false)
        {
            return resourceObjects
                .OrderByDescending(kvp =>
                {
                    float distance = Vector3.Distance(playerPosition, kvp.Value.transform.position);
                    return kvp.Key.CalculateEaseScore(distance, HasWeapon);
                })
                .Select(kvp => kvp.Value)
                .ToList();
        }

        public static List<string> FindCommonElements(string[] array1, string[] array2)
        {
            HashSet<string> set = new HashSet<string>(array2);
            //return array1.Where(item => set.Contains(item)).ToList();
            List<string> output = array1.Where(item => set.Contains(item)).ToList();

            /*Debug.Log($"array1:");
            Debug.Log(string.Join(", ", array1.ToArray()));

            Debug.Log($"array2:");
            Debug.Log(string.Join(", ", array2.ToArray()));

            Debug.Log($"Common Elements:");
            Debug.Log(string.Join(", ", output.ToArray()));*/

            /*output.Sort((a, b) =>
            {
                float healthA = resourceQuantityMap.TryGetValue(CleanKey(a), out float valueA) ? valueA : float.MinValue;
                float healthB = resourceQuantityMap.TryGetValue(CleanKey(b), out float valueB) ? valueB : float.MinValue;
                return healthB.CompareTo(healthA); // Sort in descending order
            });*/

            return output;
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
            //BrainAPIAddress = Config.Bind<string>("String", "BrainAPIAddress", GetBrainAPIAddress(), "URL address of the brain API");
            DisableAutoSave = Config.Bind<bool>("Bool", "DisableAutoSave", false, "Disable auto saving the game world?");

            spawnKey = Config.Bind("Keybinds", "Spawn", KeyCode.G, "Key for spawning a Thrall");
            harvestKey = Config.Bind("Keybinds", "Harvest", KeyCode.H, "Key for spawning a Thrall");
            followKey = Config.Bind("Keybinds", "Follow", KeyCode.F, "Key for spawning a Thrall");
            talkKey = Config.Bind("Keybinds", "Talk", KeyCode.T, "Key for spawning a Thrall");
            inventoryKey = Config.Bind("Keybinds", "Inventory", KeyCode.E, "Key for spawning a Thrall");
            thrallMenuKey = Config.Bind("Keybinds", "Menu", KeyCode.Y, "Key for spawning a Thrall");
            combatModeKey = Config.Bind("Keybinds", "CombatMode", KeyCode.J, "Key for spawning a Thrall");

            allKeybinds = new List<ConfigEntry<KeyCode>> { instance.spawnKey, instance.harvestKey, instance.followKey };
        }

        private void OnDestroy() 
        {
            /*TestPanel.SetActive(false);
            Destroy(TestPanel);*/

            if (panelManager != null)
            {
                panelManager.DestroyAllPanels();
            }

            if (!ZInput.GetKey(KeyCode.F6))
                instance.SendLogToBrain();

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

            if (instance.IsModMenuShowing && ZInput.GetKeyDown(KeyCode.Escape))
            {
                instance.ToggleModMenu();

                return;
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

                LogInfo("Keybind: Thrall Menu");

                instance.ToggleModMenu();

                return;
            }

        

            if (ZInput.GetKeyDown(KeyCode.E) && instance.PlayerNPC && instance.PlayerNPC.transform.position.DistanceTo(__instance.transform.position) < 5)
            {
                LogInfo("Keybind: Inventory");
                instance.OnInventoryKeyPressed(__instance);
                return;
            }

        

            if (ZInput.GetKeyDown(instance.spawnKey.Value))
            {
                GameObject[] allNpcs = instance.FindPlayerNPCs();
                if (allNpcs.Length > 0)
                {
                    LogInfo("Keybind: Dismiss Companion");

                    foreach (GameObject aNpc in allNpcs)
                    {
                        if (aNpc != null)
                        {
                            SaveNPCData(aNpc);
                            HumanoidNPC humanoidNPC = aNpc.GetComponent<HumanoidNPC>();
                            humanoidNPC.AddPoisonDamage(100000);
                            /*humanoidNPC.AddFireDamage(100000);
                            humanoidNPC.AddFrostDamage(100000);
                            humanoidNPC.AddLightningDamage(100000);
                            humanoidNPC.AddSpiritDamage(100000);*/
                        }
                    }

                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Thrall left the world!");
                    /*instance.PlayerNPC = null;
                    instance.PlayerNPC_humanoid = null;*/
                }
                else
                {
                    LogInfo("Keybind: Spawn Companion");
                    instance.SpawnCompanion();
                }

            
                return;
            }

            if (ZInput.GetKeyDown(instance.followKey.Value) && instance.PlayerNPC)
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

                if (instance.NPCCurrentCommand != NPCCommand.CommandType.FollowPlayer)
                {
                    LogInfo("Keybind: Follow Player");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} now following you!");
                    instance.Follow_Start(__instance.gameObject);
                }
                else
                {
                    LogInfo("Keybind: Patrol Area");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} now patrolling this area!");
                    instance.Patrol_Start();
                }
                return;
            }

            if (ZInput.GetKeyDown(instance.harvestKey.Value) && instance.PlayerNPC)
            {
                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

                if (instance.NPCCurrentCommand == NPCCommand.CommandType.HarvestResource)
                {
                    LogInfo("Keybind: Stop All Harvesting");

                    instance.commandManager.RemoveCommandsOfType<HarvestAction>();
                    //instance.Harvesting_Stop();
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} stopped harvesting!");
                }
                else
                {
                    LogInfo("Keybind: Harvest");

                    HarvestAction action = new HarvestAction();
                    action.humanoidNPC = npc;
                    action.ResourceName = "Stone";
                    action.RequiredAmount = 20;
                    action.OriginalRequiredAmount = 20;
                    instance.commandManager.AddCommand(action);

                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} harvesting {action.ResourceName.ToLower()}!");
                }
                
                return;
            }

            if (ZInput.GetKeyDown(KeyCode.J) && instance.PlayerNPC)
            {
                LogInfo("Keybind: Change Combat Mode");

                HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

                ToggleNPCCurrentCommand();

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} is now {instance.NPCCurrentMode.ToString()}");
            }

            if (ZInput.GetKey(KeyCode.T) && !instance.IsRecording)
            {
                LogInfo("Keybind: Start Recording");

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

            if (ZInput.GetKeyDown(KeyCode.P))
            {
                //Debug.LogError($"IsUnderwater {IsUnderwater(__instance.transform.position)}");
                //instance.SendLogToBrain();
                //SaveLogs();

                /*GameObject prefab = null;

                prefab = ZNetScene.instance.GetPrefab("Rock_3");
                if (prefab)
                {
                    Debug.Log("Rock_3");
                    Destructible destructible = prefab.GetComponent<Destructible>();
                    destructible.m_damages.Print();
                }

                prefab = ZNetScene.instance.GetPrefab("Beech1");
                if (prefab)
                {
                    Debug.Log("Beech1");
                    TreeBase treeBase = prefab.GetComponent<TreeBase>();
                    treeBase.m_damageModifiers.Print();
                }

                prefab = ZNetScene.instance.GetPrefab("AxeBronze");
                if (prefab)
                {
                    Debug.Log("AxeBronze");
                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    Debug.Log(itemDrop.m_itemData.m_shared.m_damages.ToString());
                }

                prefab = ZNetScene.instance.GetPrefab("PickaxeBronze");
                if (prefab)
                {
                    Debug.Log("PickaxeBronze");
                    ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
                    Debug.Log(itemDrop.m_itemData.m_shared.m_damages.ToString());
                }*/



                //PerformRaycast(__instance);

                /*Vector3 p = Player.m_localPlayer.transform.position;
                float radius = 30f;

                SphereSearchForGameObjects(p, radius);*/

                /*foreach (string element in QueryResource("Wood"))
                {
                    Debug.Log("query " + element);
                }

                foreach (string element in FindCommonElements(QueryResource("Wood"), GetNearbyResources(Player.m_localPlayer.gameObject).Keys.ToArray()))
                {
                    Debug.Log("common " + element);
                }*/



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

        private static GameObject textObject;

        static int CountItemsInInventory(Inventory inventory, string itemName)
        {
            if (inventory == null)
            {
                return 0;
            }

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
            if (!Player.m_localPlayer || !__instance) return true;

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


            if (Time.time > instance.LastFindClosestItemDropTime + 1.5 && 
                !(instance.NPCCurrentMode == NPCMode.Defensive && instance.enemyList.Count > 0) && 
                instance.NPCCurrentCommand != NPCCommand.CommandType.CombatAttack)
                //&& instance.NPCCurrentCommand != NPCCommand.CommandType.HarvestResource)
            {
                //Debug.Log("trying to find item drop");

                ItemDrop closestItemDrop = SphereSearchForGameObjectWithComponent<ItemDrop>(__instance.transform.position, 5);
                if (closestItemDrop != null && closestItemDrop.gameObject != __instance.m_follow && closestItemDrop.transform.position.DistanceTo(__instance.transform.position) < 7f)
                {
                    if (humanoidNPC.m_inventory.CanAddItem(closestItemDrop.m_itemData) && closestItemDrop.m_itemData.GetWeight() + humanoidNPC.m_inventory.GetTotalWeight() < humanoidNPC.GetMaxCarryWeight())
                    {
                        LogInfo($"{humanoidNPC.m_name} is going to pickup nearby dropped item on the ground {closestItemDrop.name} in free time");
                        __instance.SetFollowTarget(closestItemDrop.gameObject);
                        return true;
                    }
                }

                /*newfollow = FindClosestItemDrop(__instance.gameObject);

                if (newfollow != null && newfollow != __instance.m_follow && newfollow.transform.position.DistanceTo(__instance.transform.position) < 7f)
                {
                    ItemDrop itemDrop = newfollow.GetComponent<ItemDrop>();
                    if (humanoidNPC.m_inventory.CanAddItem(itemDrop.m_itemData) && itemDrop.m_itemData.GetWeight() + humanoidNPC.m_inventory.GetTotalWeight() < humanoidNPC.GetMaxCarryWeight())
                    {
                        Debug.Log($"Going to pickup nearby dropped item on the ground {newfollow.name}");
                        __instance.SetFollowTarget(newfollow);
                        return true;
                    }
                }*/
            }

            if (instance.NPCCurrentCommand == NPCCommand.CommandType.PatrolArea && instance.patrol_position != Vector3.zero)
            {
                float dist = __instance.transform.position.DistanceTo(instance.patrol_position);

                if (dist > instance.chaseUntilPatrolRadiusDistance && !instance.MovementLock)
                {
                    SetMonsterAIAggravated(__instance, false);
                    instance.MovementLock = true;
                    LogInfo($"{humanoidNPC.m_name} went too far ({instance.chaseUntilPatrolRadiusDistance}m away) from patrol position, heading back now!");
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
                    // probably trying to kill an enemy
                    return true;
                }

                if (instance.patrol_harvest)
                {
                    if (followtarget == null || followtarget.transform.position.DistanceTo(instance.patrol_position) > instance.chaseUntilPatrolRadiusDistance ||
                                    (!followtarget.HasAnyComponent("Pickable") && !followtarget.HasAnyComponent("ItemDrop")))
                    {
                        List<Pickable> closestPickables = SphereSearchForGameObjectsWithComponent<Pickable>(instance.patrol_position, instance.chaseUntilPatrolRadiusDistance - 2);
                        foreach (Pickable closestPickable in closestPickables)
                        {
                            if (closestPickable == null)
                            {
                                LogMessage($"{humanoidNPC.m_name} has picked up all dropped items around the patrolling area. Only keeping guard now!");
                                instance.patrol_harvest = false;
                                return true;
                            }

                            else if (closestPickable.transform.position.DistanceTo(instance.patrol_position) < instance.chaseUntilPatrolRadiusDistance)
                            {
                                LogMessage($"{humanoidNPC.m_name} is going to pickup {closestPickable.name} in patrol area, distance: {closestPickable.transform.position.DistanceTo(__instance.transform.position)}");
                                __instance.SetFollowTarget(closestPickable.gameObject);
                                return true;
                            }
                            /*else
                            {
                                LogInfo("Closest pickable's distance is too far!");
                            }*/
                        }
                        //Pickable closestPickable = SphereSearchForGameObjectWithComponent<Pickable>(instance.patrol_position, instance.chaseUntilPatrolRadiusDistance);
                        //newfollow = FindClosestPickableResource(__instance.gameObject, instance.patrol_position, instance.chaseUntilPatrolRadiusDistance);
                    }
                }

                return true;
            }

            else if (instance.NPCCurrentCommand == NPCCommand.CommandType.HarvestResource && (instance.enemyList.Count == 0))
            {
                //Debug.Log("LastPositionDelta " + humanoidNPC.LastPositionDelta);
                if (humanoidNPC.LastPositionDelta > 2.5f && !humanoidNPC.InAttack() && humanoidNPC.GetTimeSinceLastAttack() > 1f)
                {
                    if (__instance.m_follow)
                        __instance.LookAt(__instance.m_follow.transform.position);

                    humanoidNPC.StartAttack(humanoidNPC, false);
                }

                ItemDrop.ItemData currentWeaponData = humanoidNPC.GetCurrentWeapon();
                bool HasCurrentWeapon = (currentWeaponData != null && currentWeaponData.m_shared.m_name != "Unarmed");
                Dictionary<string, List<Resource>> queryresources = QueryResourceComplete(instance.CurrentHarvestResourceName, HasCurrentWeapon);
                List<List<string>> queryresourcesstr = ConvertResourcesToNames(queryresources.Values.ToList());
                List<string> queryresourcesfstr = FlattenListOfLists(queryresourcesstr);

                /*foreach (string s in queryresourcesfstr)
                {
                    Debug.Log($"queryresourcesfstr: {s}");
                }*/


                if (__instance.m_follow == null || 
                    //__instance.m_follow.HasAnyComponent("Character", "Humanoid") || 
                    (__instance.m_follow.HasAnyComponent("Character", "Humanoid") && !queryresourcesfstr.Contains(CleanKey(__instance.m_follow.name))) || 
                    __instance == Player.m_localPlayer || 
                    (!queryresourcesfstr.Contains(CleanKey(__instance.m_follow.name)) && !__instance.m_follow.HasAnyComponent("Pickable", "ItemDrop")))
                {
                    //comehere
                    ItemDrop closestItemDrop = SphereSearchForGameObjectWithComponent<ItemDrop>(__instance.transform.position, 7);
                    if (closestItemDrop != null && closestItemDrop.gameObject != __instance.m_follow)
                    {
                        if (humanoidNPC.m_inventory.CanAddItem(closestItemDrop.m_itemData))
                        {
                            LogMessage($"{humanoidNPC.m_name} is going to pickup nearby dropped item on the ground {closestItemDrop.name} before harvesting");
                            __instance.SetFollowTarget(closestItemDrop.gameObject);
                            return true;
                        }
                    }

                    LogInfo($"Querying for resource: {instance.CurrentHarvestResourceName} weapon: {HasCurrentWeapon}");

                    bool success = false;
                    /*foreach(List<Resource> queriesOfType in queryresources.Values)
                    {*/
                    List<Resource> commonElements = FindCommonResources(queryresources.Values.SelectMany(innerList => innerList).ToList(), GetNearbyResources(__instance.gameObject).Keys.ToList());

                    Dictionary<Resource, GameObject> ResourcesToGameObjects = new Dictionary<Resource, GameObject>();
                    for (int r = 0; r < commonElements.Count; r++)
                    {
                        GameObject resource = FindClosestResource(instance.PlayerNPC, commonElements[r].Name);
                        //if (resource == null || resource.transform.position.DistanceTo(__instance.transform.position) < 50)
                        if (resource == null && !blacklistedItems.Contains(resource))
                        {
                            LogInfo($"Couldn't find resource {commonElements[r].Name} nearby!");

                            LogInfo($"Trying to find how to get {commonElements[r].Name}");

                            Dictionary<string, List<Resource>> queryresources2 = QueryResourceComplete(commonElements[r].Name, (currentWeaponData != null && currentWeaponData.m_shared.m_name != "Unarmed"));
                            List<Resource> commonElements2 = FindCommonResources(queryresources2.Values.SelectMany(innerList => innerList).ToList(), GetNearbyResources(__instance.gameObject).Keys.ToList());

                            for (int r2 = 0; r2 < commonElements2.Count; r2++)
                            {
                                GameObject resource2 = FindClosestResource(instance.PlayerNPC, commonElements2[r2].Name);

                                if (resource2 == null && !blacklistedItems.Contains(resource2))
                                {
                                    LogInfo($"Couldn't find resource {commonElements2[r2].Name} nearby either!");
                                    continue;
                                }
                                ResourcesToGameObjects[commonElements[r]] = resource2;
                            }

                            continue;
                        }
                        ResourcesToGameObjects[commonElements[r]] = resource;
                    }
                    NewFollowTargetLastRefresh = Time.time;

                    List<GameObject> sortedResources = SortResourcesByEase(ResourcesToGameObjects, instance.PlayerNPC.transform.position, HasCurrentWeapon);
                    /*foreach (GameObject s in sortedResources)
                    {
                        Debug.Log($"sorted harvesting options: {s.name}");
                    }*/

                    if (sortedResources.Count > 0)
                    {
                        //GameObject go = GetClosestFromArray(resources, instance.PlayerNPC.transform.position);
                        GameObject go = sortedResources[0];
                        __instance.SetFollowTarget(go);
                        LogMessage($"{humanoidNPC.m_name} is going to harvest {go.name}");

                        Destructible destructible = go.GetComponent<Destructible>();
                        bool isTree = false;

                        //humanoidNPC.GetCurrentWeapon().m_shared.m_damages.

                        if (destructible != null)
                        {
                            isTree = destructible.m_destructibleType == DestructibleType.Tree || destructible.m_damages.m_chop != HitData.DamageModifier.Immune;
                        }

                        if (go.HasAnyComponent("TreeBase", "TreeLog") || go.name.ToLower().Contains("log") || isTree)
                        {
                            //equip axe
                            EquipItemType(humanoidNPC, ItemDrop.ItemData.ItemType.OneHandedWeapon);

                            LogInfo($"{humanoidNPC.m_name} is equipping OneHandedWeapon");
                        }
                        else if (go.HasAnyComponent("MineRock", "MineRock5", "Destructible", "DropOnDestroyed"))
                        {
                            //equip pickaxe
                            ItemDrop.ItemData currentWeapon = humanoidNPC.GetCurrentWeapon();
                            EquipItemType(humanoidNPC, ItemDrop.ItemData.ItemType.TwoHandedWeapon);
                            if (currentWeapon == humanoidNPC.GetCurrentWeapon())
                                EquipItemType(humanoidNPC, ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft);

                            LogInfo($"{humanoidNPC.m_name} is equipping TwoHandedWeapon");
                        }

                        success = true;
                        //break;
                    }
                    //}

                    if (!success)
                    {
                        LogMessage($"Couldnt find any resources to harvest for {instance.CurrentHarvestResourceName}");
                        LogInfo($"Removing harvest {instance.CurrentHarvestResourceName} command");
                        instance.CurrentHarvestResourceName = "Wood";
                        instance.commandManager.RemoveCommand(0);
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
                        LogMessage("New enemy target " + newfollow.name);
                    }
                }
            }

            else if (instance.NPCCurrentCommand == NPCCommand.CommandType.FollowPlayer)
            {
                if (__instance.m_follow && __instance.m_follow != Player.m_localPlayer.gameObject && !__instance.m_follow.HasAnyComponent("ItemDrop", "Pickable") && !instance.enemyList.Contains(__instance.m_follow))
                {
                    __instance.SetFollowTarget(Player.m_localPlayer.gameObject);
                    LogMessage("Following player again ");
                }
                else if (!__instance.m_follow)
                {
                    __instance.SetFollowTarget(Player.m_localPlayer.gameObject);
                    LogMessage("Following player again ");
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
                    LogMessage($"New enemy in defensive mode: {__instance.m_follow.name}");
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

        private static HashSet<GameObject> nearbyItemDrops = new HashSet<GameObject>();
        private static HashSet<GameObject> blacklistedItems = new HashSet<GameObject>(); // list of unreachable items
        private static float NewFollowTargetLastRefresh = 0f;
        private static float MaxChaseTimeForOneFollowTarget = 20f;

        private static void EquipItemType(HumanoidNPC npc, ItemDrop.ItemData.ItemType itemType)
        {
            if (!npc) return;

            int bestQuality = -1;
            ItemDrop.ItemData res = null;

            foreach (var i in npc.m_inventory.GetAllItems())
            {
                if (i.m_shared.m_itemType == itemType && i.m_quality > bestQuality)
                {
                    res = i;
                    bestQuality = i.m_quality;
                }
            }

            if (res != null)
            {
                npc.EquipItem(res);
            }
        }

        private static GameObject GetClosestFromArray(GameObject[] gos, Vector3 position)
        {
            return gos.OrderBy(go => Vector3.Distance(position, go.transform.position)).FirstOrDefault();
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

        private static HitData NPCLastHitData = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HumanoidNPC), "CustomFixedUpdate")]
        private static void HumanoidNPC_CustomFixedUpdate_Postfix(HumanoidNPC __instance)
        {
            /*Minimap.instance.UpdatePins();
            Minimap.instance.SetMapPin(__instance.name, __instance.transform.position);*/


            MonsterAI monsterAIcomponent = __instance.GetComponent<MonsterAI>();

            if (instance.NPCCurrentCommand == NPCCommand.CommandType.FollowPlayer && (Player.m_localPlayer == null || monsterAIcomponent.m_follow == null))
            {
                monsterAIcomponent.SetFollowTarget(null);
                return;
            }


            //Debug.Log(__instance.LastPositionDelta);
            if (__instance.LastPosition.DistanceTo(__instance.transform.position) > 1.5f || __instance.m_lastHit != NPCLastHitData)
            {
                NPCLastHitData = __instance.m_lastHit;
                __instance.LastPosition = __instance.transform.position;
                __instance.LastPositionDelta = 0;
            }
            else
            {
                __instance.LastPositionDelta += Time.deltaTime;
            }

        

            if (monsterAIcomponent && monsterAIcomponent.m_follow != null && monsterAIcomponent.m_follow != Player.m_localPlayer.gameObject)
            {
                float distanceBetweenTargetAndSelf = monsterAIcomponent.m_follow.transform.position.DistanceTo(__instance.transform.position);

                if (instance.NPCCurrentCommand == NPCCommand.CommandType.HarvestResource &&
                            (Time.time - NewFollowTargetLastRefresh > MaxChaseTimeForOneFollowTarget && NewFollowTargetLastRefresh != 0) || 
                            ((__instance.LastPositionDelta > MaxChaseTimeForOneFollowTarget && distanceBetweenTargetAndSelf < 5) &&
                            monsterAIcomponent.m_follow.HasAnyComponent("Destructible", "TreeBase", "TreeLog", "MineRock", "MineRock5")))
                {
                    // time for new follow target
                    LogMessage($"NPC seems to be stuck for >20s while trying to harvest {monsterAIcomponent.m_follow.gameObject.name}, evading task!");
                    blacklistedItems.Add(monsterAIcomponent.m_follow.gameObject);
                    __instance.LastPositionDelta = 0;
                    RefreshAllGameObjectInstances();
                    monsterAIcomponent.SetFollowTarget(null);
                    return;
                }

                if (!__instance.InAttack())
                {
                    float MinDistanceAllowed = 1f;
                    if (monsterAIcomponent.m_follow.HasAnyComponent("ItemDrop", "Pickable"))
                        MinDistanceAllowed = 2f;
                    else if (monsterAIcomponent.m_follow.HasAnyComponent("TreeLog"))
                        MinDistanceAllowed = 0.7f;

                    if (distanceBetweenTargetAndSelf < instance.FollowUntilDistance + MinDistanceAllowed)
                    //if (PerformRaycast(__instance) == monsterAIcomponent.m_follow.gameObject)
                    {
                        if (monsterAIcomponent.m_follow.HasAnyComponent("ItemDrop"))
                        {
                            instance.PickupItemDrop(__instance, monsterAIcomponent);

                            if (!monsterAIcomponent.m_follow)
                            {
                                ItemDrop closestItemDrop = SphereSearchForGameObjectWithComponent<ItemDrop>(monsterAIcomponent.transform.position, 8);
                                if (closestItemDrop != null)
                                {
                                    LogInfo($"Found another nearby item drop {closestItemDrop.name}");
                                    monsterAIcomponent.SetFollowTarget(closestItemDrop.gameObject);
                                }
                                else
                                {
                                    monsterAIcomponent.SetFollowTarget(null);
                                    //LogMessage($"follow target set to null after picking up item drop");
                                }
                            }
                        
                        }
                        else if (monsterAIcomponent.m_follow.HasAnyComponent("Pickable"))
                        {
                            __instance.DoInteractAnimation(monsterAIcomponent.m_follow.transform.position);

                            Pickable pick = monsterAIcomponent.m_follow.GetComponent<Pickable>();
                            pick.Interact(Player.m_localPlayer, false, false);

                            Destroy(monsterAIcomponent.m_follow);
                            instance.AllPickableInstances.Remove(monsterAIcomponent.m_follow);

                            /*if (!monsterAIcomponent.m_follow)
                            {*/

                            ItemDrop closestItemDrop = SphereSearchForGameObjectWithComponent<ItemDrop>(monsterAIcomponent.transform.position, 8);
                            if (closestItemDrop != null)
                            {
                                monsterAIcomponent.SetFollowTarget(closestItemDrop.gameObject);
                                LogInfo($"Found nearby item drop {closestItemDrop.name} after interacting with pickable");
                            }
                            else
                            {
                                monsterAIcomponent.SetFollowTarget(null);
                                LogInfo($"follow target set to null after interacting with pickable");
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

        private static GameObject FindTopLevelObject(GameObject obj)
        {
            while (obj.transform.parent != null)
            {
                obj = obj.transform.parent.gameObject;
            }
            return obj;
        }

        private static GameObject PerformRaycast(Character player)
        {
            Vector3 rayStart = player.GetEyePoint();
            Vector3 rayDirection = player.GetLookDir();
            float rayDistance = 50f; // Adjust this value to change the raycast distance

            RaycastHit hit;
            if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance))
            {
                GameObject go = FindTopLevelObject(hit.collider.gameObject);
                Debug.Log($"raycast hit {go.name}");
                return go;
            }
            else
            {
                player.Message(MessageHud.MessageType.TopLeft, "Raycast didn't hit anything", 0, null);
            }

            return null;
        }

        private static T SphereSearchForGameObjectWithComponent<T>(Vector3 p, float radius) where T : Component
        {
            int layerMask = ~0; // This will check all layers
            Collider[] colliders = Physics.OverlapSphere(p, radius, layerMask, QueryTriggerInteraction.Collide);
            List<T> res = new List<T>();

            foreach (Collider collider in colliders)
            {
                T character = GetComponentInParentOrSelf<T>(collider.gameObject);

                if (character != null)
                {
                    res.Add(character);
                }
            }

            if (res.Count > 0)
                return res.OrderBy(go => go.transform.position.DistanceTo(p)).First();

            return null;
        }

        private static List<T> SphereSearchForGameObjectsWithComponent<T>(Vector3 p, float radius) where T : Component
        {
            int layerMask = ~0; // This will check all layers
            Collider[] colliders = Physics.OverlapSphere(p, radius, layerMask, QueryTriggerInteraction.Collide);
            List<T> res = new List<T>();

            foreach (Collider collider in colliders)
            {
                T character = GetComponentInParentOrSelf<T>(collider.gameObject);

                if (character != null)
                {
                    res.Add(character);
                }
            }

            return res;
        }


        private static void SphereSearchForGameObjects(Vector3 p, float radius)
        {
            int layerMask = ~0; // This will check all layers

            Collider[] colliders = Physics.OverlapSphere(p, radius, layerMask, QueryTriggerInteraction.Collide);

            foreach (Collider collider in colliders)
            {
                GameObject obj = collider.gameObject;

                // Check for various component types
                Character character = GetComponentInParentOrSelf<Character>(obj);
                ItemDrop itemDrop = GetComponentInParentOrSelf<ItemDrop>(obj);
                TreeBase tree = GetComponentInParentOrSelf<TreeBase>(obj);
                Pickable pickable = GetComponentInParentOrSelf<Pickable>(obj);

                if (character != null)
                {
                    if (character.IsPlayer())
                    {
                        //Debug.Log($"Player detected: {obj.name}");
                    }
                    else
                    {
                        Debug.Log($"Character detected: {character.name} (Type: {character.GetHoverName()})");
                    }
                }
                else if (itemDrop != null)
                {
                    Debug.Log($"Item drop detected: {itemDrop.name} (Item: {itemDrop.m_itemData.m_dropPrefab.name})");
                }
                else if (tree != null)
                {
                    Debug.Log($"Tree detected: {tree.name}");
                }
                else if (pickable != null)
                {
                    Debug.Log($"Pickable object detected: {pickable.name}");
                }
                else
                {
                    //Debug.Log($"Other object detected: {obj.name}");
                }
            }
        }

        // Helper method to get component in parent or self
        static T GetComponentInParentOrSelf<T>(GameObject obj) where T : Component
        {
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                component = obj.GetComponentInParent<T>();
            }
            return component;
        }


        /*List<GameObject> PickedPickables = new List<GameObject>();

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
        }*/

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
                //Debug.Log("comp null or ");
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
                    //Debug.Log("InTar");
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

                string PickupItemDropName = component.name;
                LogMessage($"{__instance.m_name} is picking up {component.name}");
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
                    LogInfo($"Thrall cannot pickup item {component.GetHoverName()} {component.name}");
                    return;
                }
                int stack = component.m_itemData.m_stack;
                bool flag = __instance.m_inventory.AddItem(component.m_itemData);
                if (__instance.m_nview.GetZDO() == null)
                {
                    //Debug.Log($"__instance.m_nview.GetZDO() == null");
                    UnityEngine.Object.Destroy(component.gameObject);
                    return;
                }
                if (!flag)
                {
                    LogInfo($"Thrall can't pickup item {component.GetHoverName()} {component.name} because no room");
                    //Message(MessageHud.MessageType.Center, "$msg_noroom");
                    return;
                }
                else
                {
                    //Debug.Log($"NPC can pickup item {component.GetHoverName()} {component.name}");
                    if (instance.NPCCurrentCommand == NPCCommand.CommandType.HarvestResource &&
                        IsStringEqual(instance.CurrentHarvestResourceName, PickupItemDropName, true))
                    {
                        if (instance.currentcommand != null && instance.currentcommand is HarvestAction)
                        {
                            HarvestAction action = (HarvestAction)instance.currentcommand;
                            action.RequiredAmount = Math.Max(action.RequiredAmount - stack, 0);
                            LogInfo($"[Harvest Task] : {action.RequiredAmount} {action.ResourceName} remaining");
                        }
                        else
                        {
                            LogInfo($"NPC picked up CurrentHarvestResource {PickupItemDropName} but currentcommand is null or not a HarvestAction");
                        }
                    }
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


        public static bool IsStringEqual(string a, string b, bool bCleanKey)
        {
            if (bCleanKey)
                return CleanKey(a).ToLower().Equals(CleanKey(b).ToLower());

            return a.ToLower().Equals(b.ToLower());
        }

        public static bool IsStringStartingWith(string a, string b, bool bCleanKey)
        {
            if (bCleanKey)
                return CleanKey(a).ToLower().StartsWith(CleanKey(b).ToLower());

            return a.ToLower().StartsWith(b.ToLower());
        }

        public static bool DoesStringContains(string a, string b, bool bCleanKey)
        {
            if (bCleanKey)
                return CleanKey(a).ToLower().Contains(CleanKey(b).ToLower());

            return a.ToLower().StartsWith(b.ToLower());
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "OnDeath")]
        private static void Character_OnDeath_Prefix(Character __instance)
        {
            if (!instance.PlayerNPC || (instance.NPCCurrentCommand != NPCCommand.CommandType.CombatAttack && instance.NPCCurrentCommand != NPCCommand.CommandType.CombatSneakAttack)) return;

            if (instance.currentcommand != null && instance.currentcommand is AttackAction)
            {
                AttackAction action = (AttackAction)instance.currentcommand;
                if (IsStringEqual(__instance.gameObject.name, action.TargetName, true) && __instance.m_lastHit != null)
                {
                    Character attacker = __instance.m_lastHit.GetAttacker();
                    if (attacker != null && __instance.m_lastHit.GetAttacker() != null && __instance.m_lastHit.GetAttacker().gameObject != null && attacker.gameObject == instance.PlayerNPC)
                    {
                        action.TargetQuantity = Math.Max(action.TargetQuantity - 1, 0);
                        LogInfo($"{action.TargetQuantity} {action.TargetName} remaining to kill");
                    }
                }
            }
        }





        // OVERRIDE AI "RUN OR WALK?" LOGIC WHEN FOLLOWING A CHARACTER
        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseAI), "Follow")]
        private static bool BaseAI_Follow_Prefix(BaseAI __instance, GameObject go, float dt)
        {
            if (!Player.m_localPlayer) return true;

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

                    //instance.AddChatTalk(instance.PlayerNPC_humanoid, instance.PlayerNPC_humanoid.m_name, $"Max Health: {hp.ToString("F1")}\nMax Stamina: {stamina.ToString("F1")}\n\n\n", false);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"{instance.PlayerNPC_humanoid.m_name} stats updated:\n Max Health: {hp.ToString("F1")}\nMax Stamina: {stamina.ToString("F1")}\n\n\n");

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

                    //instance.AddChatTalk(instance.PlayerNPC_humanoid, instance.PlayerNPC_humanoid.m_name, $"Health: {num}\n\n\n", false);

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
            if (instance.PlayerNPC && instance.PlayerNPC_humanoid && instance.NPCCurrentCommand == NPCCommand.CommandType.FollowPlayer)
            {
                instance.PlayerNPC_humanoid.SetCrouch(crouch);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "SetWalk")]
        private static void Character_SetWalk_Postfix(Character __instance, bool walk)
        {
            if (__instance is Player)
            {
                if (instance.PlayerNPC && instance.PlayerNPC_humanoid && instance.NPCCurrentCommand == NPCCommand.CommandType.FollowPlayer)
                {
                    instance.PlayerNPC_humanoid.SetWalk(walk);
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
                            //Debug.Log($"came from NPC inventory");
                            //Debug.Log($"dropped into player inventory");

                            localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), __instance.m_dragItem);

                            //if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable() && instance.PlayerNPC_humanoid.IsItemEquiped(__instance.m_dragItem))
                            if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable())
                            {
                                instance.PlayerNPC_humanoid.UnequipItem(__instance.m_dragItem);
                                if (item != null && item.IsEquipable())
                                    instance.PlayerNPC_humanoid.UnequipItem(item);
                                //Debug.Log($"NPC unequipping item {__instance.m_dragItem.m_shared.m_name}");
                            }

                            /*if (instance.PlayerNPC)
                                SaveNPCData(instance.PlayerNPC);*/
                        }
                        else if (__instance.m_dragInventory == localPlayer.m_inventory && __instance.m_containerGrid == grid)
                        {
                            //Debug.Log($"came from player inventory");
                            //Debug.Log($"dropped into npc inventory");

                            __instance.m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), __instance.m_dragItem);

                            //if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable() && !instance.PlayerNPC_humanoid.IsItemEquiped(__instance.m_dragItem))
                            if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable())
                            {
                                if (Player.m_localPlayer.IsItemEquiped(__instance.m_dragItem) || Player.m_localPlayer.GetCurrentWeapon() == __instance.m_dragItem)
                                {
                                    //Debug.Log($"uneqipping {__instance.m_dragItem.m_shared.m_name} from player");
                                    Player.m_localPlayer.UnequipItem(__instance.m_dragItem);
                                }
                                

                                instance.PlayerNPC_humanoid.EquipItem(__instance.m_dragItem);
                                if (item != null && item.IsEquipable())
                                    instance.PlayerNPC_humanoid.EquipItem(item);
                                //Debug.Log($"NPC equipping item {__instance.m_dragItem.m_shared.m_name}");
                            }

                            /*if (instance.PlayerNPC)
                                SaveNPCData(instance.PlayerNPC);*/
                        }
                    }
                    else
                    {
                        //Debug.Log($"InventoryGUI OnSelectedItem failed, __instance.m_dragInventory was null");
                    }
                }
                else
                {
                    //Debug.Log($"not interacting w npc inventory");
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
                    //Debug.LogError("item == null");
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
                LogError("Spawning more than one NPC is disabled");
                return;
            }
            Player localPlayer = Player.m_localPlayer;
            GameObject npcPrefab = ZNetScene.instance.GetPrefab("HumanoidNPC");

            instance.commandManager.RemoveAllCommands();
            instance.enemyList.Clear();
        

            if (npcPrefab == null)
            {
                LogError("ScriptNPC prefab not found!");
            }

            // spawn NPC
            Vector3 spawnPosition = localPlayer.transform.position + localPlayer.transform.forward * 2f;
            //Vector3 spawnPosition = GetRandomSpawnPosition(10f);
            Quaternion spawnRotation = localPlayer.transform.rotation;

            GameObject npcInstance = Instantiate<GameObject>(npcPrefab, spawnPosition, spawnRotation);
            npcInstance.SetActive(true);

            UnityEngine.CapsuleCollider capsuleCollider = npcInstance.GetComponent<CapsuleCollider>();
            capsuleCollider.radius = 0.7f;

            VisEquipment npcInstanceVis = npcInstance.GetComponent<VisEquipment>();
            VisEquipment playerInstanceVis = localPlayer.GetComponent<VisEquipment>();

            npcInstanceVis.m_isPlayer = true;
            npcInstanceVis.m_emptyBodyTexture = playerInstanceVis.m_emptyBodyTexture;
            npcInstanceVis.m_emptyLegsTexture = playerInstanceVis.m_emptyLegsTexture;

            instance.PlayerNPC = npcInstance;

            if (npcInstance.HasAnyComponent("Tameable"))
            {
                //Debug.Log("removing npc tameable comp");
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

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{humanoidNpc_Component.m_name} has entered the world!");


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

                    LogInfo("Thrall's inventory size was 0, giving default rags");

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
                LogError("humanoidNpc_Component component not found on the instantiated ScriptNPC prefab!");
            }
        }

        protected virtual void OnNPCDeath()
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Your Thrall died! Press [G] to respawn");

            HumanoidNPC humanoidNPC = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            //PrintInventoryItems(humanoidNPC.m_inventory);

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
                LogError("NPC command Follow_Start failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(target);

            if (NPCDialogueMessage != "")
            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.FollowPlayer;
            LogMessage("Follow_Start activated!");
        }

        private void Follow_Stop(string NPCDialogueMessage = "I'm gonna wander off on my own now!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Follow_Stop failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            SetMonsterAIAggravated(monsterAIcomponent, false);
            monsterAIcomponent.SetFollowTarget(null);  

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage("Follow_Stop activated!");
        }

        private void Combat_StartAttacking(string EnemyName, string NPCDialogueMessage = "Watch out, here I come!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Combat_StartAttacking failed, instance.PlayerNPC == null");
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
                LogInfo($"Trying to find enemy {EnemyName}");
                closestEnemy = FindClosestEnemy(instance.PlayerNPC, EnemyName);
            }
            else
            {
                LogError("EnemyName was null");
            }

            

            if (closestEnemy != null)
            {
                monsterAIcomponent.SetFollowTarget(closestEnemy);
                LogInfo($"Combat_StartAttacking closestEnemy found! " + closestEnemy.name);
            }
            else
            {
                monsterAIcomponent.SetFollowTarget(null);
                LogError("Combat_StartAttacking closestEnemy not found!");
            }
            

            monsterAIcomponent.m_alerted = false;
            monsterAIcomponent.m_aggravatable = true;
            monsterAIcomponent.SetHuntPlayer(true);

            if (NPCDialogueMessage != "")
            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.CombatAttack;
            LogMessage("Combat_StartAttacking activated!");
        }

        private void Combat_StartSneakAttacking(GameObject target, string NPCDialogueMessage = "Sneaking up on em!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Combat_StartSneakAttacking failed, instance.PlayerNPC == null");
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
            LogMessage("Combat_StartSneakAttacking activated!");
        }

        private void Combat_StartDefending(GameObject target, string NPCDialogueMessage = "Don't worry, I'm here with you!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Combat_StartDefending failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            // disregard nearby enemies
            monsterAIcomponent.SetFollowTarget(null);
            SetMonsterAIAggravated(monsterAIcomponent, false);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.CombatDefend;
            LogMessage("Combat_StartDefending activated!");
        }

        private void Combat_StopAttacking(string NPCDialogueMessage = "I'll give em a break!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Combat_StopAttacking failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            // disregard nearby enemies
            monsterAIcomponent.SetFollowTarget(null);
            SetMonsterAIAggravated(monsterAIcomponent, false);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage("Combat_StopAttacking activated!");
        }

        private void Inventory_DropAll(string NPCDialogueMessage = "Here is all I got!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Inventory_DropAll failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            DropAllItems(humanoidnpc_component);

            //AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            //instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage("Inventory_DropAll activated!");
        }
        private void Inventory_DropItem(string ItemName, string NPCDialogueMessage = "Here is what you asked for!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Inventory_DropItem failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            DropItem(ItemName, humanoidnpc_component);

            //AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            //instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage("Inventory_DropItem activated!");
        }

        private void Inventory_EquipItem(string ItemName, string NPCDialogueMessage = "On it boss!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Inventory_EquipItem failed, instance.PlayerNPC == null");
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

            //AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            //instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage($"Inventory_EquipItem activated! ItemName : {ItemName}");
        }

        private void Harvesting_Start(string ResourceName, string NPCDialogueMessage = "On it boss!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Harvesting_Start failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            instance.CurrentHarvestResourceName = CleanKey(ResourceName);
            LogInfo("trying to harvest resource: " + instance.CurrentHarvestResourceName);

            //ResourceName = "Beech";

            if (NPCDialogueMessage != "")
                AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.HarvestResource;
            LogMessage("Harvesting_Start activated!");
        }

        private void Harvesting_Stop(string NPCDialogueMessage = "No more labor!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Harvesting_Stop failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            monsterAIcomponent.SetFollowTarget(null);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage("Harvesting_Stop activated!");
        }

        private void Patrol_Start(string NPCDialogueMessage = "I'm keeping guard around here! They know not to try!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Patrol_Start failed, instance.PlayerNPC == null");
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
            LogMessage("Patrol_Start activated!");
        }

        private void Patrol_Stop(string NPCDialogueMessage = "My job is done here!")
        {
            if (instance.PlayerNPC == null)
            {
                LogError("NPC command Patrol_Stop failed, instance.PlayerNPC == null");
                return;
            }

            MonsterAI monsterAIcomponent = instance.PlayerNPC.GetComponent<MonsterAI>();
            HumanoidNPC humanoidnpc_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();

            monsterAIcomponent.SetFollowTarget(null);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            instance.NPCCurrentCommand = NPCCommand.CommandType.Idle;
            LogMessage("Patrol_Stop activated!");
        }

        private static string GetChatInputText()
        {
            if (Chat.instance != null)
                return ((TMP_InputField)(object)Chat.instance.m_input).text;
            return "";
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Chat), "InputText")]
        private static bool Chat_InputText_Prefix(Chat __instance)
        {
            if (IsLocalSingleplayer() && instance.PlayerNPC)
            {
                /*string text = GetChatInputText();
                LogError($"Just typed {text}");*/
                instance.BrainSendInstruction(instance.PlayerNPC, false);
                if (Player.m_localPlayer)
                    instance.AddChatTalk(Player.m_localPlayer, "NPC", GetChatInputText(), true);
                instance.AddChatTalk(instance.PlayerNPC_humanoid, "NPC", "...", false);
            }

            return false;
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

        private async Task SendLogToBrain()
        {
            if (logEntries.Count <= 0) return;

            StringBuilder res = new StringBuilder();
            foreach (string entry in logEntries)
            {
                res.AppendLine(entry);
            }

            var jObject = new JsonObject
            {
                ["player_id"] = GetPlayerSteamID(),
                ["timestamp"] = DateTime.Now.ToString(),
                ["log_string"] = res.ToString(),
            };

            // Create a new WebClient
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/json");
            webClient.UploadStringCompleted += OnSendLogToBrainCompleted;


            try
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var uploadTask = webClient.UploadStringTaskAsync(new Uri($"{GetBrainAPIAddress()}/log_valheim"), IndentJson(jObject.ToString()));

                var completedTask = await Task.WhenAny(uploadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    webClient.CancelAsync();
                    throw new TimeoutException("Request timed out after 10 seconds");
                }

                await uploadTask; // Ensure any exceptions are thrown
                LogInfo("Successfully logged to brain!");
            }
            catch (WebException ex)
            {
                LogError($"Error connecting to server/log: {ex.Message}");
            }
            catch (TimeoutException ex)
            {
                LogError($"Request timed out /log: {ex.Message}");
            }
            catch (Exception ex)
            {
                LogError($"An error occurred /log: {ex.Message}");
            }



            // Send the POST request
            /*webClient.UploadStringAsync(new System.Uri($"{GetBrainAPIAddress()}/log_valheim"), jObject.ToString());
            webClient.UploadStringCompleted += OnSendLogToBrainCompleted;*/


            /*string FilePath = Path.Combine(UnityEngine.Application.persistentDataPath, "lastlog.json");
            LogInfo($"Saving temp log to {FilePath}");

            File.WriteAllText(FilePath, jObject.ToString());*/

            logEntries.Clear();
        }

        private void OnSendLogToBrainCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                LogInfo("Logged to brain completed!");

            }
            else
            {
                LogError("Sending log to brain failed: " + e.Error.Message);
            }
        }


        static bool IsInventoryShowing = false;
        private void OnInventoryKeyPressed(Player player)
        {
            if (instance.PlayerNPC)
            {
                SaveNPCData(instance.PlayerNPC);
                if (IsInventoryShowing)
                {
                    InventoryGui.instance.Hide();
                    IsInventoryShowing = false;
                }
                else
                {
                    HumanoidNPC humanoidNPC_component = instance.PlayerNPC.GetComponent<HumanoidNPC>();
                    InventoryGui.instance.Show(humanoidNPC_component.inventoryContainer);
                    //PrintInventoryItems(humanoidNPC_component.m_inventory);
                    IsInventoryShowing = true;
                }
            }
            else
            {
                LogError("OnInventoryKeyPressed instance.PlayerNPC is null ");
            }
        }

        private void DropAllItems(HumanoidNPC humanoidNPC)
        {
            List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
            int num = 1;
            LogInfo($"{humanoidNPC.m_name} dropping {humanoidNPC.m_inventory.GetAllItems().Count} items: ");
            foreach (ItemDrop.ItemData item in allItems)
            {
                LogInfo(item.m_shared.m_name);
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
                if ((item.m_dropPrefab != null && ItemName == item.m_dropPrefab.name) || ItemName == item.m_shared.m_name)
                {
                    LogInfo($"{humanoidNPC.m_name} dropping item: " + item.m_shared.m_name);
                    //Vector3 position = humanoidNPC.transform.position + Vector3.up * 0.5f + UnityEngine.Random.insideUnitSphere * 0.3f;
                    Vector3 position = humanoidNPC.transform.position + Vector3.up * 2f + UnityEngine.Random.insideUnitSphere * 0.3f + humanoidNPC.transform.forward * 5.5f;
                    Quaternion rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0, 360), 0f);
                    ItemDrop.DropItem(item, item.m_stack, position, rotation);
                    num++;
                    //humanoidNPC.m_inventory.RemoveOneItem(item);
                    humanoidNPC.m_inventory.RemoveItem(item, item.m_stack);
                    return;
                }
            }
            LogInfo($"{humanoidNPC.m_name} couldn't drop item: {ItemName}");
        }

        private void EquipItem(string ItemName, HumanoidNPC humanoidNPC)
        {
            List<ItemDrop.ItemData> allItems = humanoidNPC.m_inventory.GetAllItems();
            foreach (ItemDrop.ItemData item in allItems)
            {
                if (ItemName == item.m_shared.m_name)
                {
                    LogInfo($"{humanoidNPC.m_name} equipping  " + item.m_shared.m_name);
                    humanoidNPC.EquipItem(item);
                    return;
                }
            }

        }




        public void AddChatTalk(Character character, string name, string text, bool addToChat = true)
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
            long senderID = character is Player ? 99991 : 99992;
            Chat.WorldTextInstance oldtext = Chat.instance.FindExistingWorldText(senderID);
            if (oldtext != null && oldtext.m_gui)
            {
                UnityEngine.Object.Destroy(oldtext.m_gui);
                Chat.instance.m_worldTexts.Remove(oldtext);
            }
            Chat.instance.AddInworldText(character.gameObject, senderID, headPoint, Talker.Type.Shout, userInfo, text + "\n\n\n\n\n");
            if (text != "..." && addToChat)
            {
                Chat.instance.AddString(character is Player ? Player.m_localPlayer.GetPlayerName() : character.m_name, text, Talker.Type.Normal);
                Chat.instance.m_hideTimer = 0f;
                Chat.instance.m_chatWindow.gameObject.SetActive(value: true);
            }
        }

        public void BrainSynthesizeAudio(string text, string voice)
        {
            using (WebClient client = new WebClient())
            {
                // Construct the URL with query parameters
                string url = $"{GetBrainAPIAddress()}/synthesize_audio?text={Uri.EscapeDataString(text)}&voice={Uri.EscapeDataString(voice)}&player_id={GetPlayerSteamID()}";

                client.DownloadStringCompleted += OnBrainSynthesizeAudioResponse;
                client.DownloadStringAsync(new Uri(url));
            }
        }

        private void OnBrainSynthesizeAudioResponse(object sender, DownloadStringCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                LogError($"Synthesize Audio Download failed: {e.Error.Message}");
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
                LogError($"Failed to parse Synthesize Audio Download JSON: {ex.Message}");
            }

            instance.previewVoiceButton.SetActive(true);
            SetPreviewVoiceButtonState(instance.previewVoiceButtonComp, true, 1);
        }

        private void BrainSendPeriodicUpdate(GameObject npc)
        {
            string jsonData = GetJSONForBrain(npc, false);

            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/json");

            webClient.UploadStringAsync(new System.Uri($"{GetBrainAPIAddress()}/instruct_agent"), jsonData);
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

                            LogError("Agent command response from brain was incomplete. Command's Action or Category is missing!");
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
                            LogError($"param {pa}");
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

        private async Task BrainSendInstruction(GameObject npc, bool Voice = true)
        {
            string jsonData = GetJSONForBrain(npc, Voice);

            //Debug.Log("jsonData\n " + jsonData);

            // Create a new WebClient
            WebClient webClient = new WebClient();
            webClient.Headers.Add("Content-Type", "application/json");
            webClient.UploadStringCompleted += OnBrainSendInstructionResponse;

            try
            {
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(10));
                var uploadTask = webClient.UploadStringTaskAsync(new Uri($"{GetBrainAPIAddress()}/instruct_agent"), jsonData);

                var completedTask = await Task.WhenAny(uploadTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    webClient.CancelAsync();
                    throw new TimeoutException("Request timed out after 10 seconds");

                }

                await uploadTask; // Ensure any exceptions are thrown
            }
            catch (WebException ex)
            {
                LogError($"Brain Send Instruction | Error connecting to server: {ex.Message}");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Error connecting to Thrall server!");
            }
            catch (TimeoutException ex)
            {
                LogError($"Brain Send Instruction | Request timed out: {ex.Message}");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Timeout error while connecting to Thrall server!");
            }
            catch (Exception ex)
            {
                LogError($"Brain Send Instruction | An error occurred: {ex.Message}");
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "An error occurred while trying to connect to Thrall server!");
            }



            // Send the POST request
            /*webClient.UploadStringAsync(new System.Uri($"{BrainAPIAddress.Value}/instruct_agent"), jsonData);
            webClient.UploadStringCompleted += OnBrainSendInstructionResponse;*/
        }

        private void OnBrainSendInstructionResponse(object sender, UploadStringCompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string responseJson = IndentJson(e.Result);

                // Parse the response JSON using SimpleJSON's DeserializeObject
                JsonObject responseObject = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(responseJson);
                string audioFileId = responseObject["agent_text_response_audio_file_id"].ToString();
                string agent_text_response = responseObject["agent_text_response"].ToString().TrimStart('\n');
                string player_instruction_transcription = responseObject["player_instruction_transcription"].ToString();

                //Debug.Log("Response from brain");
                
                LogInfo("Full response from brain: " + responseJson);
                LogMessage("You said: " + player_instruction_transcription);
                LogMessage("NPC said: " + agent_text_response);

                // Get the agent_commands array
                JsonArray agentCommands = responseObject["agent_commands"] as JsonArray;

                // Check if agent_commands array exists and has at least one element
                if (instance.PlayerNPC && agentCommands != null && agentCommands.Count > 0)
                {
                    for (int i=0; i<agentCommands.Count; i++)
                    {
                        JsonObject commandObject = agentCommands[i] as JsonObject;
                        HumanoidNPC npc = instance.PlayerNPC.GetComponent<HumanoidNPC>();

                        AddChatTalk(Player.m_localPlayer, "Player", player_instruction_transcription);
                        AddChatTalk(npc, "NPC", agent_text_response);

                        if (!(commandObject.ContainsKey("action") && commandObject.ContainsKey("category")))
                        {
                            LogError("Agent command response from brain was incomplete. Command's Action or Category is missing!");
                            continue;
                        }

                        string action = commandObject["action"].ToString();
                        string category = commandObject["category"].ToString();

                        string[] parameters = {};

                        string parametersString = "";

                        if (commandObject.ContainsKey("parameters"))
                        {
                            JsonArray jsonparams = commandObject["parameters"] as JsonArray;
                            parameters = jsonparams.Select(x => x.ToString()).ToArray();
                        }

                        foreach (string s in parameters)
                        {
                            parametersString += $"{s}, ";
                        }

                        LogInfo($"NEW COMMAND: {category} {action} {parametersString}");
                        if (category == "Inventory")
                            ProcessNPCCommand(category, action, parameters.Length > 0 ? parameters[0] : "", agent_text_response);

                        Sprite defaultSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);

                        if (category == "Harvesting")
                        {
                            string ResourceName = null;
                            int ResourceQuantity = 0;
                            string ResourceNode = null;

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
                                harvestAction.RequiredAmount = ResourceQuantity;
                                harvestAction.OriginalRequiredAmount = ResourceQuantity;
                                //harvestAction.RequiredAmount = ResourceQuantity + CountItemsInInventory(npc.m_inventory, ResourceName);
                                instance.commandManager.AddCommand(harvestAction);
                            }
                            else
                            {
                                LogError("Brain gave Harvesting command but didn't give 3 parameters");
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
                            string TargetName = null;
                            string WeaponName = null;
                            int TargetQty = 1;

                            if (parameters.Length > 0)
                            {
                                /*if (parameters.Length >= 1)
                                    TargetName = parameters[0];
                                if (parameters.Length >= 2)
                                    TargetQty = int.Parse(parameters[1]);
                                if (parameters.Length >= 3)
                                    WeaponName = parameters[2];*/

                                for (int x = 0; i < parameters.Length; x++)
                                {
                                    if (x > 2) break;
                                    //Debug.Log($"x {x} {parameters[x]}");
                                    
                                    if (x == 0)
                                    {
                                        TargetName = parameters[x];
                                        //Debug.Log($"target name {x} {parameters[x]}");
                                    }
                                    else if (int.TryParse(parameters[x], out int quantity))
                                    {
                                        TargetQty = quantity;
                                        //Debug.Log($"target qty {x} {parameters[x]}");
                                    }
                                    else
                                    {
                                        WeaponName = parameters[x];
                                        //Debug.Log($"weapon name {x} {parameters[x]}");
                                    }
                                }

                                if (TargetQty < 1)
                                {
                                    TargetQty = 1;
                                }

                                AttackAction attackAction = new AttackAction();
                                attackAction.humanoidNPC = npc;
                                attackAction.TargetName = TargetName;
                                attackAction.Target = FindClosestEnemy(instance.PlayerNPC, TargetName);
                                attackAction.TargetQuantity = TargetQty;
                                attackAction.OriginalTargetQuantity = TargetQty;
                                instance.commandManager.AddCommand(attackAction);
                            }
                            else
                            {
                                LogError("Brain gave Combat command but didn't give a parameters");
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
                    LogInfo("No agent commands sent by brain.");
                }

                // Download the audio file asynchronously
                DownloadAudioFile(audioFileId);
            }
            else
            {
                LogError("Request failed: " + e.Error.Message);
            }
        }

        private void DownloadAudioFile(string audioFileId)
        {
            // Create a new WebClient for downloading the audio file
            WebClient webClient = new WebClient();

            // Download the audio file asynchronously
            webClient.DownloadDataAsync(new System.Uri($"{GetBrainAPIAddress()}/get_audio_file?audio_file_id={audioFileId}&player_id={GetPlayerSteamID()}"));
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
                    LogInfo("Brain response time: " + (Time.time - instance.lastSentToBrainTime));

                PlayWavFile(npcDialogueRawAudioPath);
            
            }
            else if (e.Error is WebException webException && webException.Status == WebExceptionStatus.ProtocolError && ((HttpWebResponse)webException.Response).StatusCode == HttpStatusCode.NotFound)
            {
                LogError("Audio file does not exist.");
            }
            else
            {
                LogError("Download failed: " + e.Error.Message);
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
                .Where(go => go != null && IsStringStartingWith(go.name, EnemyName, true))
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
            if (instance.AllPlayerNPCInstances.Length > 1)
            {
                for (int i = 0; i < instance.AllPlayerNPCInstances.Length; i++)
                {
                    UnityEngine.Object.Destroy(instance.AllPlayerNPCInstances[i]);
                }
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
            if (!instance.PlayerNPC && !Player.m_localPlayer)
            {
                LogError("RefreshAllGameObjectInstances failed! Local player and PlayerNPC was null");
                return;
            }

            Vector3 p = instance.PlayerNPC != null ? instance.PlayerNPC.transform.position : Player.m_localPlayer.transform.position;

            instance.AllGOInstances = GameObject.FindObjectsOfType<GameObject>(false)
                    .Where(go => go != null && 
                    go.transform.position.DistanceTo(p) < 300 && 
                    !blacklistedItems.Contains(go) &&
                    go.HasAnyComponent("ItemDrop", "CharacterDrop", "DropOnDestroyed", "Pickable", "Character", "Destructible", "TreeBase", "TreeLog", "MineRock", "MineRock5"))
                    .ToArray();
                    //.ToList();
            instance.AllGOInstancesLastRefresh = Time.time;

            LogInfo($"RefreshAllGameObjectInstances len {instance.AllGOInstances.Count()}");

            RefreshPickables();
        }

        private static void RefreshPickables()
        {
            instance.AllPickableInstances = instance.AllGOInstances.Where(go => go.HasAnyComponent("Pickable") || go.HasAnyComponent("ItemDrop")).ToList();
        }

        private static GameObject FindClosestPickableResource(GameObject character, Vector3 p_position, float radius)
        {
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

            LogInfo("FindClosestResource returning null");
            return null;
        }

        private static GameObject FindClosestResource(GameObject character, string ResourceName, bool UnderwaterAllowed = true)
        {
            //return GameObject.FindObjectsOfType<GameObject>(true)

            if (CanAccessAllGameInstances())
            {
                return instance.AllGOInstances
                    //.Where(go => go.name.Contains(ResourceName) && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
                    //.Where(go => go != null && CleanKey(go.name.ToLower()) == ResourceName.ToLower() && go.HasAnyComponent("Pickable", "Destructible", "TreeBase", "ItemDrop"))
                    .Where(go => go != null && IsStringEqual(go.name, ResourceName, true) && (UnderwaterAllowed || !IsUnderwater(go.transform.position)))
                    .ToArray().OrderBy(t => Vector3.Distance(character.transform.position, t.transform.position))
                    .FirstOrDefault();
            }

            LogError($"FindClosestResource returning null for {ResourceName}");
            return null;
        }

        private static bool IsUnderwater(Vector3 position)
        {
            GameObject go = ZNetScene.instance.GetPrefab("Fish1");
            if (go)
            {
                Fish fish = go.GetComponent<Fish>();

                if (fish)
                {
                    //Debug.LogError("valid fish");
                    return position.y < fish.GetWaterLevel(position);
                }
            }
            


            return false;
        }

        private static GameObject FindClosestItemDrop(GameObject character)
        {
            if (CanAccessAllGameInstances())
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

            //LogError("FindClosestItemDrop returning null");
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

            //Debug.Log("FindClosestTreeFor returning null");
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

        private static float nearbyResourcesLastRefresh = 0f;
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

            /*Remove any trailing numbers
            while (key.Length > 0 && char.IsDigit(key[key.Length - 1]))
            {
                key = key.Substring(0, key.Length - 1);
            }

            // Trim again in case there was whitespace before the numbers
            return key.Trim();*/

            return key;
        }

        private static Dictionary<string, int> GetNearbyResources(GameObject source)
        {
            if (instance.nearbyResources.Count > 0 && Time.time - nearbyResourcesLastRefresh < 10) return instance.nearbyResources;

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
            }

            if (instance.AllGOInstances.Length == 0)
            {
                RefreshAllGameObjectInstances();
            }

            foreach (GameObject co in instance.AllGOInstances)
                if (co != null)
                    ProcessResource(co, co.name);

            //Debug.Log($"Populated nearbyResources {instance.nearbyResources.Count} {instance.nearbyResourcesDistance.Count}");

            return instance.nearbyResources;
        }

        private string GetNearbyResourcesJSON(GameObject source)
        {
            GetNearbyResources(source);

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
            

            string json = SimpleJson.SimpleJson.SerializeObject(jarray);
            //LogInfo(json);
            LogInfo($"Total nearby resources count: {totalResources}");
            return IndentJson(json);
        }

        Dictionary<string, int> nearbyEnemies = new Dictionary<string, int>();
        Dictionary<string, float> nearbyEnemiesDistance = new Dictionary<string, float>();
        private string GetNearbyEnemies(GameObject source)
        {
            Character[] characters = GameObject.FindObjectsOfType<Character>(true);
            Humanoid[] humanoids = GameObject.FindObjectsOfType<Humanoid>(true);

            /*Debug.Log("characters len " + characters.Length);
            Debug.Log("humanoids len " + humanoids.Length);*/

        

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
            

            //string json = jarray.ToString();
            string json = SimpleJson.SimpleJson.SerializeObject(jarray);
            //Debug.Log(json);
            LogInfo($"Total nearby enemies: {totalEnemies}");
            return IndentJson(json);
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
            if (Microphone.devices.Length == 0)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "No microphone detected! Please connect a microphone and restart the game.");
                return;
            }

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

            Chat.WorldTextInstance oldtext = Chat.instance.FindExistingWorldText(99991);
            if (oldtext != null && oldtext.m_gui)
            {
                UnityEngine.Object.Destroy(oldtext.m_gui);
                Chat.instance.m_worldTexts.Remove(oldtext);
            }
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
                LogError("Error saving recording: " + e.Message);
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
                LogError("Audio file not found: " + audioPath);
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

            LogInfo("Playing last recorded clip audio");
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
                LogError($"File not found: {filePath}");
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
                LogError($"Error playing WAV file: {e.Message}");
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
                LogError("Audio file not found: " + audioPath);
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
            //LogInfo("Thrall's inventory items:");
            foreach (ItemDrop.ItemData item in humanoidNPC.m_inventory.m_inventory)
            {
                var itemData = new JsonObject
                {
                    ["name"] = item.m_dropPrefab ? item.m_dropPrefab.name : item.m_shared.m_name,
                    ["amount"] = item.m_stack,
                };

                //LogInfo($"{item.m_shared.m_name} x{item.m_stack}");
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


            var jsonObject = new JsonObject
            {
                //["player_id"] = humanoidNPC.GetZDOID().ToString(),
                ["player_id"] = GetPlayerSteamID(),
                ["agent_name"] = humanoidNPC.m_name,
                ["game_state"] = gameState,
                ["timestamp"] = Time.time,
                ["personality"] = instance.npcPersonality,
                ["voice"] = npcVoices[instance.npcVoice].ToLower(),
                ["gender"] = instance.npcGender,
            };

            if (includeRecordedAudio)
            {
                jsonObject["player_instruction_audio_file_base64"] = instance.GetBase64FileData(instance.playerDialogueAudioPath);
            }
            else
            {
                jsonObject["player_instruction_text"] = GetChatInputText();
            }
            jsonObject["voice_or_text"] = includeRecordedAudio ? "voice" : "text";

            string jsonString = SimpleJson.SimpleJson.SerializeObject(jsonObject);
            jsonString = IndentJson(jsonString);

            jsonObject["player_instruction_audio_file_base64"] = "";
            string jsonString2 = SimpleJson.SimpleJson.SerializeObject(jsonObject);
            jsonString2 = IndentJson(jsonString2);
            LogInfo("Sending to brain: " + jsonString2);

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
            json = IndentJson(json);

            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "thrallmod.json");

            File.WriteAllText(filePath, json);
            LogInfo("Saved NPC data to " + filePath);
        }

        public static void LoadNPCData(HumanoidNPC npc)
        {
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string filePath = Path.Combine(UnityEngine.Application.persistentDataPath, "thrallmod.json");
            LogInfo("Loading NPC data from " + filePath);

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
                LogMessage($"Loading {inventoryArray.Count} items to {npc.m_name}'s inventory");
                foreach (JsonObject itemData in inventoryArray)
                {
                    string itemName = itemData["name"].ToString();
                    int stack = int.Parse(itemData["stack"].ToString());
                    int equipped = 0;
                    if (itemData.ContainsKey("equipped"))
                        equipped = int.Parse(itemData["equipped"].ToString());


                    LogInfo($"{itemName} x{stack} {(equipped == 1 ? "(equipping)" : "")}");
                

                    GameObject itemPrefab = ZNetScene.instance.GetPrefab(itemName);
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
                        LogError($"itemPrefab {itemName} was null");
                    }
                }

                npc.EquipBestWeapon(Player.m_localPlayer, null, Player.m_localPlayer, Player.m_localPlayer);

                LogMessage($"{npc.m_name} data loaded successfully!");
            }
            else
            {
                LogWarning("No saved NPC data found.");
                LogMessage("Applying default NPC personality");

                /*instance.npcName = "The Truth";
                instance.npcPersonality = "He always lies and brags about stuff he doesn't have or has never seen. His lies are extremely obvious. Always brings up random stuff out of nowhere";
                instance.personalityDropdownComp.SetValueWithoutNotify(npcPersonalities.Count - 1);*/

                instance.OnNPCPersonalityDropdownChanged(0);

                ApplyNPCData(npc);
            }
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
            int personalityIndex = FindKeyIndexForValue(instance.npcPersonality);
            instance.personalityDropdownComp.SetValueWithoutNotify(personalityIndex == -1 ? npcPersonalities.Count - 1 : personalityIndex);
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

            string json = IndentJson(jsonObject.ToString());

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "crafting_requirements.json");

            File.WriteAllText(filePath, json);
            LogError($"Crafting requirements exported to {filePath}");
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

            string json = IndentJson(jsonObject.ToString());

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "building_requirements.json");

            File.WriteAllText(filePath, json);
            LogError($"Building requirements exported to {filePath}");
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
            json = IndentJson(json);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "monsters.json");

            File.WriteAllText(filePath, json);
            LogError($"Monster prefab list exported to {filePath}");
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
            json = IndentJson(json);

            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string filePath = Path.Combine(desktopPath, "all_items_list.json");

            File.WriteAllText(filePath, json);
            LogError($"Crafting requirements exported to {filePath}");
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
            if (instance.DisableAutoSave == null)
                return false;
            return !instance.DisableAutoSave.Value;
        }

        void ToggleModMenu()
        {
            if (!instance.PlayerNPC)
            {
                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Cannot open Thrall Menu without a Thrall in the world!");
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
            LogMessage("Character Inventory Items:");

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            foreach (ItemDrop.ItemData item in items)
            {
                LogMessage($"- {item.m_shared.m_name} (Quantity: {item.m_stack} | {item.m_dropPrefab.name})");
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
                    LogWarning($"Panel {panelName} already exists.");
                    return panels[panelName];
                }

                if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
                {
                    LogError("GUIManager instance or CustomGUI is null");
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
                    LogError($"TogglePanel failed! Panel {panelName} does not exist.");
                    return;
                }

                GameObject panel = panels[panelName];
                bool state = !panel.activeSelf;

                if (state)
                {
                    instance.RefreshTaskList();
                    instance.RefreshKeyBindings();
                }

                if (panel != null)
                {
                    panel.SetActive(state);
                }
                else
                {
                    LogError($"TogglePanel failed! Panel {panelName} was null!");
                    return;
                }
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
            keybindsSubPanel = panelManager.CreateSubPanel(settingsPanel, "Keybinds", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -300f), 430, 170, pivot: new Vector2(0.5f, 1f));
            micInputSubPanel = panelManager.CreateSubPanel(settingsPanel, "Mic Input", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -490f), 430, 80, pivot: new Vector2(0.5f, 1f));
            egoBannerSubPanel = panelManager.CreateSubPanel(settingsPanel, "Ego Banner", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -590f), 430, 30, pivot: new Vector2(0.5f, 1f));
        
        
        
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

            //Debug.Log("Creating scrollable task queue");

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
                    LogMessage($"Removing npc command [{index}]");
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
                    /*int RequiredAmount = action.RequiredAmount;
                    if (instance.PlayerNPC_humanoid)
                        RequiredAmount -= CountItemsInInventory(instance.PlayerNPC_humanoid.m_inventory, action.ResourceName);*/
                    AddItemToScrollBox(TaskListScrollBox, $"Gathering {action.ResourceName} ({action.RequiredAmount})", defaultSprite, i);
                }
                if (task is PatrolAction)
                {
                    PatrolAction action = (PatrolAction)task;
                    AddItemToScrollBox(TaskListScrollBox, $"Patrolling area: {action.patrol_position.ToString()}", defaultSprite, i);
                }
                if (task is AttackAction)
                {
                    AttackAction action = (AttackAction)task;
                    AddItemToScrollBox(TaskListScrollBox, $"Attacking: {action.TargetName} ({action.TargetQuantity})", defaultSprite, i);
                }
                if (task is FollowAction)
                {
                    AddItemToScrollBox(TaskListScrollBox, "Following Player", defaultSprite, i);
                }
                i++;
            }
        }


        bool IsEditingKeybind = false;
        private ConfigEntry<KeyCode> spawnKey;
        private ConfigEntry<KeyCode> harvestKey;
        private ConfigEntry<KeyCode> followKey;
        private ConfigEntry<KeyCode> talkKey;
        private ConfigEntry<KeyCode> inventoryKey;
        private ConfigEntry<KeyCode> thrallMenuKey;
        private ConfigEntry<KeyCode> combatModeKey;
        private static List<ConfigEntry<KeyCode>> allKeybinds;// = new List<ConfigEntry<KeyCode>>{ instance.spawnKey, instance.harvestKey, instance.followKey };

        private IEnumerator ListenForNewKeybind(int keybindIndex)
        {
            yield return new WaitForSeconds(0.1f); // Short delay to prevent immediate capture

            while (true)
            {
                foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
                {
                    if (ZInput.GetKeyDown(keyCode, false))
                    {
                        bool flag = false;
                        foreach (ConfigEntry<KeyCode> entry in allKeybinds)
                        {
                            if (entry.Value == keyCode)
                            {
                                LogError($"{keyCode.ToString()} is already used for {entry.Definition}!");
                                //yield break;
                                flag = true;
                                yield return null;
                            }
                        }
                        if (!flag)
                        {
                            allKeybinds[keybindIndex].Value = keyCode;
                            LogWarning($"Keybind for {allKeybinds[keybindIndex].Definition} set to Key: {keyCode.ToString()}");
                            
                        }

                        RefreshKeyBindings();

                        yield break;
                    }
                }
                yield return null;
            }
        }

        private List<Button> editButtons = new List<Button>();
        private void RefreshKeyBindings()
        {
            foreach (Transform child in keybindsSubPanel.transform)
            {
                Destroy(child.gameObject);
            }
            editButtons.Clear();

            CreateKeyBindings();
        }
        private void CreateKeyBindings()
        {
            string[] bindings = {
                $"[{spawnKey.Value.ToString()}] Spawn/Dismiss",
                $"[{harvestKey.Value.ToString()}] Harvest",
                $"[{followKey.Value.ToString()}] Follow/Patrol"
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
                /*GameObject textObject2 = GUIManager.Instance.CreateText(
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

                textObject2.GetComponent<RectTransform>().pivot = new Vector2(0, 1);*/

                // Create a container for each row
                GameObject rowContainer = new GameObject($"KeybindRow_{i}");
                RectTransform rowRectTransform = rowContainer.AddComponent<RectTransform>();
                rowRectTransform.SetParent(keybindsSubPanel.transform, false);
                rowRectTransform.anchorMin = new Vector2(0f, 1f);
                rowRectTransform.anchorMax = new Vector2(1f, 1f);
                rowRectTransform.anchoredPosition = new Vector2(10f, -60f - (i * 30));
                rowRectTransform.sizeDelta = new Vector2(0, 30);

                // Create text object for keybind
                GameObject textObject2 = GUIManager.Instance.CreateText(
                    text: bindings[i],
                    parent: rowContainer.transform,
                    anchorMin: new Vector2(0f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    position: Vector2.zero,
                    font: GUIManager.Instance.AveriaSerif,
                    fontSize: 20,
                    color: Color.white,
                    outline: true,
                    outlineColor: Color.black,
                    width: 300f,
                    height: 30f,
                    addContentSizeFitter: false);

                RectTransform textRectTransform = textObject2.GetComponent<RectTransform>();
                textRectTransform.pivot = new Vector2(0, 1f);
                textRectTransform.anchoredPosition = Vector2.zero;

                // Create edit button
                GameObject editButton = GUIManager.Instance.CreateButton(
                    text: "Edit",
                    parent: rowContainer.transform,
                    anchorMin: new Vector2(1f, 0f),
                    anchorMax: new Vector2(1f, 1f),
                    position: new Vector2(-5f, 0f),
                    width: 60f,
                    height: 25f);

                RectTransform buttonRectTransform = editButton.GetComponent<RectTransform>();
                buttonRectTransform.pivot = new Vector2(1f, 1f);
                buttonRectTransform.anchoredPosition = new Vector2(-20f, 0f);

                // Add click event to the button
                Button buttonComponent = editButton.GetComponent<Button>();
                int index = i; // Capture the current index for the lambda
                buttonComponent.onClick.AddListener(() => OnEditKeybind(index));

                editButtons.Add(buttonComponent);
            }
        }

        private void OnEditKeybind(int index)
        {
            foreach (Button button in editButtons)
            {
                button.interactable = false;
            }

            // You can open a dialog or start listening for a new key press here
            if (allKeybinds.Count >= 0 && allKeybinds.Count > index)
            {
                LogInfo($"Waiting for new keybind...");
                StartCoroutine(ListenForNewKeybind(index));
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
            LogWarning("New microphone picked: " + Microphone.devices[index]);
        }

        private void CreateEgoBanner()
        {
            GameObject textObject = GUIManager.Instance.CreateText(
                text: "ego.ai's Discord Server",
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
                outlineColor: Color.blue,
                width: 350f,
                height: 40f,
                addContentSizeFitter: false);

            RectTransform rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.pivot = new Vector2(0, 1f);

            // Add EventTrigger component
            EventTrigger eventTrigger = textObject.AddComponent<EventTrigger>();

            // Create a new entry for the click event
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;

            // Add the OnClick function to the entry
            entry.callback.AddListener((eventData) => { OnClickEgoBanner(); });

            // Add the entry to the EventTrigger
            eventTrigger.triggers.Add(entry);
        }

        private void OnClickEgoBanner()
        {
            string url = "https://discord.gg/egoai";
            Application.OpenURL(url);
            LogInfo("Ego discord url clicked!");
            // Add your custom logic here
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
            //logger.L("Input value changed to: " + newValue);

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

            LogInfo($"New NPCPersonality picked from dropdown: {npcPersonalities[instance.npcPersonalityIndex]}");
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
            //Debug.Log("New personality " + instance.npcPersonality);
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
            LogInfo("New NPCVoice picked: " + npcVoices[instance.npcVoice]);
        }

        private void OnVolumeSliderValueChanged(float value)
        {
            instance.npcVolume = value;
            //Debug.Log("new companion volume " + instance.npcVolume);
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
                //Debug.Log("OnBodyTypeToggleChanged instance.PlayerNPC is null");
            }

            LogInfo("New NPCGender picked: " + changedToggle.name);
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
            //Jotunn.LogInfo($"Color changing: {changedColor}");
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
            LogInfo($"Selected color: {instance.skinColor}");
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
            //Jotunn.LogInfo($"Color changing: {changedColor}");
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
            LogInfo($"Selected color: {instance.hairColor}");
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
                if (command.humanoidNPC != null)
                {
                    string commandTypeText = null;
                    if (command is HarvestAction)
                    {
                        HarvestAction action = (HarvestAction)command;
                        commandTypeText = $"gathering {action.OriginalRequiredAmount} {action.ResourceName}";
                    }
                    else if (command is AttackAction)
                    {
                        AttackAction action = (AttackAction)command;
                        commandTypeText = $"attacking {action.OriginalTargetQuantity} {action.TargetName}";
                    }
                    else if (command is PatrolAction)
                    {
                        PatrolAction action = (PatrolAction)command;
                        //commandTypeText = $"patrolling around area: {action.patrol_position.ToString()}";
                        commandTypeText = $"patrolling area";
                    }
                    else if (command is FollowAction)
                    {
                        FollowAction action = (FollowAction)command;
                        commandTypeText = $"following {Player.m_localPlayer.GetPlayerName()}";
                    }
                    string text = $"Done {commandTypeText}";
                    ValheimAIModLivePatch.instance.AddChatTalk(command.humanoidNPC, "NPC", text);
                    ValheimAIModLivePatch.instance.BrainSynthesizeAudio(text, ValheimAIModLivePatch.npcVoices[ValheimAIModLivePatch.instance.npcVoice].ToLower());
                }
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
        public int OriginalRequiredAmount { get; set; }
        public int RequiredAmount { get; set; }

        public override bool IsTaskComplete()
        {
            // Check if harvesting condition is met, e.g., resource is within range
            //return Vector3.Distance(npc.transform.position, Resource.transform.position) <= 5f;
            if (humanoidNPC)
            {
                if (RequiredAmount <= 0)
                    return true;
                /*if (humanoidNPC.HasEnoughResource(ResourceName, RequiredAmount))
                {
                    Debug.Log("HarvestAction task complete");
                    return true;
                }
                else
                {
                    //Debug.Log("HarvestAction doesnt have enough resources");
                }*/
            }
            else
            {
                Debug.LogError("HarvestAction no humanoidNPC");
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
        public int TargetQuantity { get; set; }
        public int OriginalTargetQuantity { get; set; }

        public override bool IsTaskComplete()
        {
            // Check if harvesting condition is met, e.g., resource is within range
            if (TargetQuantity == 0) return true;
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



    public class CustomComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            // If one is a "_half" version of the other, place "_half" first
            if (x.EndsWith("_half") && y == x.Substring(0, x.Length - 5))
                return -1;
            if (y.EndsWith("_half") && x == y.Substring(0, y.Length - 5))
                return 1;

            // Otherwise, use standard string comparison
            return string.Compare(x, y, StringComparison.Ordinal);
        }
    }

}