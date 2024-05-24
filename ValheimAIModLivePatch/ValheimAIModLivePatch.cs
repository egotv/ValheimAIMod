using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace ValheimAIMod
{
    [BepInPlugin("sahejhundal.ValheimAIMod", "Valheim AI NPC Mod", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class ValheimAIMod : BaseUnityPlugin
    {
        private static ValheimAIMod instance;
        private readonly Harmony harmony = new Harmony("sahejhundal.ValheimAIMod");

        private static GameObject PlayerNPCPrefab;


        // MOD Awake (loadup code)
        void Awake()
        {
            instance = this;

            var assetBundle = GetAssetBundleFromResources("player_npc");
            PlayerNPCPrefab = assetBundle.LoadAsset<GameObject>("Assets/CustomAssets/PlayerNPC.prefab");

            var playerNpcScript = PlayerNPCPrefab.GetComponent<Player>();

            if (playerNpcScript != null)
            {
                //GameObject.DestroyImmediate(playerNpcScript);
                Object.Destroy(playerNpcScript);
            }

            PlayerNPC playerNPC_comp = PlayerNPCPrefab.AddComponent<PlayerNPC>();
            MonsterAI monsterAI_comp = PlayerNPCPrefab.AddComponent<MonsterAI>();

            StartCoroutine(FollowPlayer(monsterAI_comp));


            if (PlayerNPCPrefab)
            {
                Debug.Log("PlayerNPC loaded");
            }
            else
            {
                Debug.Log("PlayerNPC not loaded");
            }

            assetBundle.Unload(false);

            //Debug.Log("Initialized config and debugging");
            harmony.PatchAll();
            //Harmony.CreateAndPatchAll(typeof(ValheimAIMod));
        }

        // MOD OnDestroy (destructor code/unpatch)
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
            }
        }

        private IEnumerator FollowPlayer(MonsterAI monsterAI_comp)
        {
            while (monsterAI_comp != null)
            {
                GameObject playerObject = Player.m_localPlayer.gameObject;

                float deltaTime = Time.deltaTime;

                monsterAI_comp.SetFollowTarget(playerObject);
                monsterAI_comp.MakeTame();

                yield return null;
            }
        }

        [HarmonyPatch(typeof(PlayerController), "FixedUpdate")]
        static class PlayerController_FixedUpdate_Patch
        {
            /*
             A PlayerController is spawned whenever a Player is spawned. PlayerController takes input from local player. 
            Since we are using Player class for our NPCs, we need to destroy the extra spawned PlayerController.
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

        /*[HarmonyPatch(typeof(PlayerNPC), "FixedUpdate")]
        static class Player_FixedUpdate_Patch
        {
            static bool Prefix(PlayerNPC __instance)
            {
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
                    __instance.PlayerAttackInput(fixedDeltaTime);
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
        }*/
    }
}