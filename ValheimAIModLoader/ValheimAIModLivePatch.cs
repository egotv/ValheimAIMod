using BepInEx;
using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections;

namespace ValheimAIModLivePatch
{
    [BepInPlugin("sahejhundal.ValheimAIModLivePatch", "Valheim AI NPC Mod Live Patch", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private static ValheimAIModLivePatch instance;
        private readonly Harmony harmony = new Harmony("sahejhundal.ValheimAIModLivePatch");
        private ConfigEntry<KeyboardShortcut> spawnCompanionKey;
        private static GameObject PlayerNPCPrefab;


        // MOD Awake (loadup code)
        void Awake()
        {
            instance = this;
            spawnCompanionKey = Config.Bind("Keybinds", "SpawnCompanionKey", new KeyboardShortcut(KeyCode.F));


            Debug.Log("ValheimAIModLivePatch Awake");
            //harmony.PatchAll();
            harmony.PatchAll(typeof(ValheimAIModLivePatch));
        }

        // MOD OnDestroy (destructor code/unpatch)
        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Update")]
        private static void PlayerUpdate_Postfix(Player __instance)
        {
            //Debug.Log("playerupdate postfix");
            // Check if the spawn companion key is pressed
            if (instance.spawnCompanionKey.Value.IsDown())
            {
                instance.SpawnCompanion(__instance);
            }
        }

        /*[HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "UpdateEyeRotation")]
        private static bool Character_UpdateEyeRotation_Postfix(Character __instance)
        {
            if (!__instance)
            {
                Debug.Log("Character_UpdateEyeRotation_Postfix instance invalid");
                return false;
            }
            if (!__instance.m_eye)
            {
                Debug.Log("Character_UpdateEyeRotation_Postfix eye invalid");
                __instance.m_eye.localPosition = new Vector3();
                return false;
            }
            //Debug.Log("Character_UpdateEyeRotation_Postfix both valid m_lookDir " + __instance.m_lookDir.ToString());
            return true;
        }*/

        

        private void SpawnCompanion(Player player)
        {
            if (player != null)
            {

                //GameObject skeletonPrefab = ZNetScene.instance.GetPrefab("PlayerNPC");
                GameObject skeletonPrefab = ZNetScene.instance.GetPrefab("PlayerClassNPC");
                //GameObject skeletonPrefab = ZNetScene.instance.GetPrefab("SkeletonNPC");

                if (skeletonPrefab == null)
                {
                    Logger.LogError("PlayerNPC prefab not found!");
                }

                // Get the player's position and rotation
                Vector3 spawnPosition = player.transform.position + player.transform.forward * 2f;
                Quaternion spawnRotation = player.transform.rotation;

                // Instantiate the companion prefab at the spawn position and rotation
                GameObject skeletonInstance = Instantiate(skeletonPrefab, spawnPosition, spawnRotation);

                skeletonInstance.SetActive(true);

                MonsterAI monsterAI = skeletonInstance.GetComponent<MonsterAI>();

                /*if (monsterAI != null)
                {
                    //GameObject.DestroyImmediate(playerNpcScript);
                    Object.Destroy(monsterAI);
                }

                monsterAI = skeletonInstance.AddComponent<MonsterAI>();*/
                

                if (monsterAI != null)
                {
                    monsterAI.SetFollowTarget(player.gameObject);
                    monsterAI.MakeTame();

                    StartCoroutine(FollowPlayer(monsterAI));
                }
                else
                {
                    Logger.LogError("MonsterAI component not found on the instantiated PlayerNPC prefab!");
                }

            }
        }

        private IEnumerator FollowPlayer(MonsterAI monsterAI)
        {
            while (monsterAI != null)
            {
                GameObject playerObject = Player.m_localPlayer.gameObject;

                float deltaTime = Time.deltaTime;

                monsterAI.SetFollowTarget(playerObject);
                monsterAI.MakeTame();

                yield return null;
            }
        }
    }
}