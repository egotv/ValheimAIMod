using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;
using Jotunn.Managers;
using Jotunn.Entities;

namespace ValheimAIMod
{
    [BepInPlugin("egovalheimmod.ValheimAIModLoader", "Valheim AI NPC Mod Loader", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class ValheimAIModLoader : BaseUnityPlugin
    {
        private static ValheimAIModLoader instance;
        private readonly Harmony harmony = new Harmony("egovalheimmod.ValheimAIModLoader");

        private static GameObject HumanoidNPCPrefab;

        void Awake()
        {
            instance = this;

            RegisterConsoleCommands();

            var script_npc_assetBundle = GetAssetBundleFromResources("scriptnpc");
            HumanoidNPCPrefab = script_npc_assetBundle.LoadAsset<GameObject>("Assets/CustomAssets/HumanoidNPC.prefab");

            if (HumanoidNPCPrefab) Debug.Log("HumanoidNPCPrefab loaded"); 
            else Debug.Log("HumanoidNPCPrefab not loaded");

            script_npc_assetBundle.Unload(false);

            harmony.PatchAll();
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        public static AssetBundle GetAssetBundleFromResources(string fileName)
        {
            var execAssembly = Assembly.GetExecutingAssembly();

            var resourceName = execAssembly.GetManifestResourceNames()
                .Single(str => str.EndsWith(fileName)); 

            using (var  stream = execAssembly.GetManifestResourceStream(resourceName))
            {
                return AssetBundle.LoadFromStream(stream);
            }
        }



        /* CONSOLE COMMANDS */
        private void RegisterConsoleCommands()
        {
            CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new DespawnAllCommand());
        }

        public class DespawnAllCommand : ConsoleCommand
        {
            public override string Name => "despawn_all";

            public override string Help => "Despawn all __INPUT_TEXT__ game objects";

            public override void Run(string[] args)
            {
                if (args.Length == 0)
                {
                    instance.DespawnPrefabInstances("HumanoidNPC");
                    return;
                }
                instance.DespawnPrefabInstances(args[0]);
            }
        }

        public void DespawnPrefabInstances(string prefabName)
        {
            List<GameObject> instancesToRemove = new List<GameObject>();

            // Find all instances of the specified prefab
            foreach (ZNetView view in FindObjectsOfType<ZNetView>())
            {
                if (view.gameObject.name.Contains(prefabName))
                {
                    instancesToRemove.Add(view.gameObject);
                }
            }

            // Despawn the prefab instances
            foreach (GameObject instance in instancesToRemove)
            {
                ZNetView view = instance.GetComponent<ZNetView>();
                if (view != null)
                {
                    view.Destroy();
                }
            }

            Console.instance.Print($"Despawned {instancesToRemove.Count} instances of prefab '{prefabName}'");
        }



        // Add custom prefabs to game
        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            public static void Prefix(ZNetScene __instance)
            {
                if (__instance == null)
                {
                    return;
                }
                __instance.m_prefabs.Add(HumanoidNPCPrefab);
                //__instance.m_namedPrefabs.Add(HumanoidNPCPrefab.name.GetStableHashCode(), HumanoidNPCPrefab);
            }
        }
    }
}