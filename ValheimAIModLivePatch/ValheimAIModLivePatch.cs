using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn;
using SimpleJson;



using UnityEngine;
using System.IO;
using System.Net;
using Jotunn.Managers;
using UnityEngine.UI;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.EventSystems;
using System.Collections;
using BepInEx.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using System.Runtime.InteropServices.WindowsRuntime;


namespace ValheimAIModLoader
{
    [BepInPlugin("egoai.thrallmodlivepatch", "ego.ai Thrall Mod Live Patch", "0.0.1")]
    [BepInProcess("valheim.exe")]
    [BepInDependency("egoai.thrallmodloader", BepInDependency.DependencyFlags.HardDependency)]
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private void DoModInit()
        {
            LogWarning("Initializing Thrall Mod!");
            Chat.instance.SendText(Talker.Type.Normal, "EGO.AI THRALL MOD LOADED!");

            CreateModMenuUI();

            NPCCurrentMode = NPCMode.Defensive;

            FindPlayerNPC();
            if (PlayerNPC)
            {
                HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();
                LoadNPCData(npc);
            }


            /*PopulateCraftingRequirements();
            PopulateBuildingRequirements();
            PopulateMonsterPrefabs();
            PopulateAllItems();*/

            PopulateDatabase();
            //PopulateAllWeapons();
            //PopulateAllItems();

            ModInitComplete = true;

            LogMessage("Thrall mod initialization complete");
            //Debug.Log("Thrall mod initialization complete");
        }

        private void Awake()
        {
            logger = Logger;
            
            instance = this;

            LogMessage("ego.ai Thrall ValheimAIModLivePatch Loaded! :)");
            LogWarning("This mod is designed for single-player gameplay and may not function correctly when used alongside other mods.");

            ConfigBindings();

            

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

        private void ConfigBindings()
        {
            //BrainAPIAddress = Config.Bind<string>("String", "BrainAPIAddress", GetBrainAPIAddress(), "URL address of the brain API");
            LogToBrain = Config.Bind<bool>("Bool", "LogToBrain", true, "Log To Brain?");
            DisableAutoSave = Config.Bind<bool>("Bool", "DisableAutoSave", false, "Disable auto saving the game world?");

            spawnKey = Config.Bind("Keybinds", "Spawn", KeyCode.G, "Key for spawning a Thrall");
            harvestKey = Config.Bind("Keybinds", "Harvest", KeyCode.H, "Key for spawning a Thrall");
            followKey = Config.Bind("Keybinds", "Follow", KeyCode.F, "Key for spawning a Thrall");
            talkKey = Config.Bind("Keybinds", "Talk", KeyCode.T, "Key for spawning a Thrall");
            inventoryKey = Config.Bind("Keybinds", "Inventory", KeyCode.E, "Key for spawning a Thrall");
            thrallMenuKey = Config.Bind("Keybinds", "Menu", KeyCode.Y, "Key for spawning a Thrall");
            combatModeKey = Config.Bind("Keybinds", "CombatMode", KeyCode.J, "Key for spawning a Thrall");

            allKeybinds = new List<ConfigEntry<KeyCode>> { instance.spawnKey, instance.harvestKey, instance.followKey, instance.inventoryKey, instance.talkKey, instance.thrallMenuKey, instance.combatModeKey };
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
    }
}