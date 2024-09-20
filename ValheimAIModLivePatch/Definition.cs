using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        public enum NPCMode
        {
            Passive,
            Defensive,
            Aggressive
        }

        public static ValheimAIModLivePatch instance;
        private readonly Harmony harmony = new Harmony("egoai.thrallmodlivepatch");


        private static string NPCPrefabName = "HumanoidNPC";
        private static GameObject PlayerNPC;
        private static HumanoidNPC humanoid_PlayerNPC;
        private static ThrallAI thrallAI_PlayerNPC;


        private static GameObject[] AllGOInstances = { };
        private static float AllGOInstancesLastRefresh = 0f;
        private GameObject[] AllEnemiesInstances = { };
        private float AllEnemiesInstancesLastRefresh = 0f;
        private static HashSet<Character> enemyList = new HashSet<Character>();


        private NPCCommandManager commandManager = new NPCCommandManager();
        public static NPCMode NPCCurrentMode { get; private set; }
        private static NPCCommand NPCCurrentCommand = null;
        private static NPCCommand.CommandType NPCCurrentCommandType;

        

        private static HitData NPCLastHitData = null;



        

        public static bool MovementLock = false;
        public static float chaseUntilPatrolRadiusDistance = 20f;
        public static Vector3 patrol_position = Vector3.zero;
        public float patrol_radius = 10f;
        public bool patrol_harvest = false;

        public static string CurrentEnemyName = "Greyling";
        public static string CurrentHarvestResourceName = "Wood";
        public static string CurrentWeaponName = "";
        private static ItemDrop.ItemData useWeapon = null;

        private static string lastAttackedObjectZDOID = "";
        private static HitData.DamageModifiers targetDamageModifiers = new HitData.DamageModifiers();

        private static List<ItemDrop> closestItemDrops = new List<ItemDrop>();
        private static float closestItemDropsLastRefresh = 0;

        private static HashSet<GameObject> blacklistedItems = new HashSet<GameObject>(); // list of unreachable items
        private static float NewFollowTargetLastRefresh = 0f;
        private static float MaxChaseTimeForOneFollowTarget = 20f;

        private static Dictionary<string, List<Resource>> ResourceNodes = new Dictionary<string, List<Resource>>();
        private static List<List<string>> ResourceNodesNamesOnly = new List<List<string>>();
        private static List<string> ResourceNodesOneArray = new List<string>();

        
        public static bool IsModMenuShowing = false;
        private static bool ModInitComplete = false;


        private static float FollowUntilDistance = .5f;
        private static float RunUntilDistance = 3f;


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
        private ConfigEntry<bool> LogToBrain;
        private ConfigEntry<bool> DisableAutoSave;

        private Dictionary<string, Piece.Requirement[]> craftingRequirements = new Dictionary<string, Piece.Requirement[]>();
        private Dictionary<string, Piece.Requirement[]> buildingRequirements = new Dictionary<string, Piece.Requirement[]>();
        private Dictionary<string, List<string>> resourceLocations = new Dictionary<string, List<string>>();

    }
}
