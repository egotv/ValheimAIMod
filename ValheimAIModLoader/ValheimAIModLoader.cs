using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace ValheimAIModLoader
{
    [BepInPlugin("sahejhundal.ValheimAIModLoader", "Valheim AI NPC Mod Loader", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class ValheimAIModLoader : BaseUnityPlugin
    {
        private static ValheimAIModLoader instance;
        private readonly Harmony harmony = new Harmony("sahejhundal.ValheimAIModLoader");

        private static GameObject PlayerNPCPrefab;
        private static GameObject PlayerClassNPCPrefab;
        private static GameObject SkeletonNPCPrefab;


        // MOD Awake (loadup code)
        void Awake()
        {
            instance = this;

            var player_npc_assetBundle = GetAssetBundleFromResources("player_npc");
            PlayerNPCPrefab = player_npc_assetBundle.LoadAsset<GameObject>("Assets/CustomAssets/PlayerNPC.prefab");
            
            var playerclass_npc_assetBundle = GetAssetBundleFromResources("playerclass_npc");
            PlayerClassNPCPrefab = playerclass_npc_assetBundle.LoadAsset<GameObject>("Assets/CustomAssets/PlayerClassNPC.prefab");
            
            var skeleton_npc_assetBundle = GetAssetBundleFromResources("skeleton_npc");
            SkeletonNPCPrefab = skeleton_npc_assetBundle.LoadAsset<GameObject>("Assets/CustomAssets/SkeletonNPC.prefab");

            /*var playerNpcScript = PlayerNPCPrefab.GetComponent<Player>();

            if (playerNpcScript != null)
            {
                //GameObject.DestroyImmediate(playerNpcScript);
                Object.Destroy(playerNpcScript);
            }*/

            //PlayerNPC playerNPC_comp = PlayerNPCPrefab.AddComponent<PlayerNPC>();
            //MonsterAI monsterAI_comp = PlayerNPCPrefab.AddComponent<MonsterAI>();


            if (PlayerNPCPrefab) Debug.Log("PlayerNPCPrefab loaded"); 
            else Debug.Log("PlayerNPCPrefab not loaded");
            if (SkeletonNPCPrefab) Debug.Log("SkeletonNPCPrefab loaded");
            else Debug.Log("SkeletonNPCPrefab not loaded");

            player_npc_assetBundle.Unload(false);
            skeleton_npc_assetBundle.Unload(false);

            //Debug.Log("Initialized config and debugging");
            harmony.PatchAll();
            //harmony.PatchAll(typeof(ValheimAIModLoader));
        }

        // MOD OnDestroy (destructor code/unpatch)
        void OnDestroy()
        {
            harmony.UnpatchSelf();
            //Harmony.UnpatchID(harmony.Id);
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


        [HarmonyPatch(typeof(ZNetScene), "Awake")]
        static class ZNetScene_Awake_Patch
        {
            public static void Prefix(ZNetScene __instance)
            {
                if (__instance == null)
                {
                    return;
                }
                __instance.m_prefabs.Add(PlayerNPCPrefab);
                __instance.m_prefabs.Add(PlayerClassNPCPrefab);
                __instance.m_prefabs.Add(SkeletonNPCPrefab);
            }
        }


        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        static class PlayerController_FixedUpdate_Patch
        {
            /*
             Destroy PlayerController for non locally controlled players
             */
            static bool Prefix(PlayerController __instance)
            {
                if (Player.m_localPlayer != __instance.m_character)
                {
                    //ZNetScene.instance.Destroy(__instance.gameObject);
                    Debug.Log("DESTROYING PC");
                    Object.Destroy(__instance);
                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(Player), "FixedUpdate")]
        static class Player_FixedUpdate_Patch
        {
            static bool Prefix(Player __instance)
            {
                /*Debug.Log("Player_FixedUpdate_Patch");*/
                float fixedDeltaTime = Time.fixedDeltaTime;
                __instance.UpdateAwake(fixedDeltaTime);
                if (__instance.m_nview.GetZDO() == null)
                {
                    return false;
                }
                __instance.UpdateTargeted(fixedDeltaTime);
                if (!__instance.m_nview.IsOwner())
                {
                    return false;
                }
                if (!__instance.IsDead())
                {
                    __instance.UpdateActionQueue(fixedDeltaTime);
                    if (Player.m_localPlayer == __instance)
                    {
                        __instance.PlayerAttackInput(fixedDeltaTime);
                    }
                    __instance.UpdateAttach();
                    __instance.UpdateDoodadControls(fixedDeltaTime);
                    __instance.UpdateCrouch(fixedDeltaTime);
                    __instance.UpdateDodge(fixedDeltaTime);
                    __instance.UpdateCover(fixedDeltaTime);
                    __instance.UpdateStations(fixedDeltaTime);
                    __instance.UpdateGuardianPower(fixedDeltaTime);
                    __instance.UpdateBaseValue(fixedDeltaTime);
                    __instance.UpdateStats(fixedDeltaTime);
                    __instance.UpdateTeleport(fixedDeltaTime);
                    __instance.AutoPickup(fixedDeltaTime);
                    __instance.EdgeOfWorldKill(fixedDeltaTime);
                    __instance.UpdateBiome(fixedDeltaTime);
                    __instance.UpdateStealth(fixedDeltaTime);


                    if (GameCamera.instance != null && __instance.m_attachPointCamera == null && Vector3.Distance(GameCamera.instance.transform.position, __instance.transform.position) < 2f)
                    {
                        __instance.SetVisible(visible: false);
                    }
                    AudioMan.instance.SetIndoor(__instance.InShelter() || ShieldGenerator.IsInsideShield(__instance.transform.position));
                }

                //Debug.Log("Player FixedUpdate override");
                return false;
            }
        }
    }
}