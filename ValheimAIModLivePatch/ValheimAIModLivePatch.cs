using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;

/*using Jotunn.Entities;
using Jotunn.Managers;*/
using UnityEngine;

[BepInPlugin("sahejhundal.ValheimAIModLivePatch", "Valheim AI NPC Mod Live Patch", "1.0.0")]
[BepInProcess("valheim.exe")]
public class ValheimAIModLivePatch : BaseUnityPlugin
{
    /*public class SpawnNPCCommand : ConsoleCommand
    {
        public override string Name => "ego_spawnNPC";

        public override string Help => "Spawns a friend in the world";

        public override void Run(string[] args)
        {
            instance.SpawnCompanion();
        }
    }*/

    private static ValheimAIModLivePatch instance;
    private readonly Harmony harmony = new Harmony("sahejhundal.ValheimAIModLivePatch");

    private ConfigEntry<KeyboardShortcut> spawnCompanionKey;
    private ConfigEntry<KeyboardShortcut> ToggleFollowKey;
    private ConfigEntry<KeyboardShortcut> ToggleHarvestKey;
    private ConfigEntry<KeyboardShortcut> ToggleAttackKey;
    private ConfigEntry<KeyboardShortcut> InventoryKey;
    private ConfigEntry<bool> DisableAutoSave;

    private GameObject[] AllPlayerNPCInstances;
    private float AllPlayerNPCInstancesLastRefresh = 0f;
    private GameObject[] SmallTrees;

    // NPC VARS
    private ValheimAIModLoader.NPCMode eNPCMode;
    /*private bool bFollowPlayer; // TODO: CONVERT CURRENT MODE TO ENUM
    private bool bHarvest;*/
    private float FollowUntilDistance = 1f;
    private float RunUntilDistance = 3f;

    private float StaminaExhaustedMinimumBreakTime = 3f;
    private float MinimumStaminaToRun = 5f;




    private void Awake()
    {
        Debug.Log("ValheimAIModLivePatch Awake");
        instance = this;

        ConfigBindings();
        RegisterConsoleCommands();
        
        harmony.PatchAll(typeof(ValheimAIModLivePatch));
    }

    private void ConfigBindings()
    {
        spawnCompanionKey = Config.Bind<KeyboardShortcut>("Keybinds", "SpawnCompanionKey", new KeyboardShortcut(KeyCode.G), "The key used to spawn an NPC.");
        ToggleFollowKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleFollowKey", new KeyboardShortcut(KeyCode.F), "The key used to command all NPCs to follow you.");
        ToggleHarvestKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleHarvestKey", new KeyboardShortcut(KeyCode.H), "The key used to command all NPCs to go harvest.");
        ToggleAttackKey = Config.Bind<KeyboardShortcut>("Keybinds", "ToggleAttackKey", new KeyboardShortcut(KeyCode.K), "The key used to command all NPCs to attack enemies.");
        InventoryKey = Config.Bind<KeyboardShortcut>("Keybinds", "InventoryKey", new KeyboardShortcut(KeyCode.Y), "The key used to command all NPCs to -");
        DisableAutoSave = Config.Bind<bool>("Bool", "DisableAutoSave", false, "Disable auto saving the game world?");
    }

    private void RegisterConsoleCommands()
    {
        //CommandManager.Instance.AddConsoleCommand((ConsoleCommand)new SpawnNPCCommand());
    }

    private void OnDestroy()
    {
        harmony.UnpatchSelf();
    }

    private void PrintAllTags()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        HashSet<string> uniqueTags = new HashSet<string>();

        foreach (GameObject obj in allObjects)
        {
            string tag = obj.tag;
            if (!string.IsNullOrEmpty(tag))
            {
                uniqueTags.Add(tag);
            }
        }

        Debug.Log("All tags defined in Valheim:");
        foreach (string tag in uniqueTags)
        {
            Debug.Log(tag);
        }
    }

    private static ValheimAIModLoader.NPCScript IsScriptNPC(Character __instance)
    {
        if (__instance.gameObject.name.Contains("ScriptNPC"))
        {
            ValheimAIModLoader.NPCScript npcscript_component = __instance.GetComponent<ValheimAIModLoader.NPCScript>();
            if (npcscript_component)
            {
                return npcscript_component;
            }
        }
        return null;
    }

    /*[HarmonyPrefix]
    [HarmonyPatch(typeof(BaseAI), "MoveTo")]
    public static void BaseAI_MoveTo_Prefix(BaseAI __instance, ref float dt, ref Vector3 point, ref float dist, ref bool run)
    {
        ValheimAIModLoader.NPCScript npcscript_component = IsScriptNPC(__instance.m_character);
        if (npcscript_component)
        {
            if (npcscript_component.m_stamina < 10)
            {
                run = false;
                Debug.Log("MoveTo told ai bot to stop running");
            }
        }
    }*/

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "CheckRun")]
    public static bool Character_CheckRun_Prefix(Character __instance, Vector3 moveDir, float dt)
    {
        // 
        ValheimAIModLoader.NPCScript npcscript_component = IsScriptNPC(__instance);
        if (npcscript_component)
        {
            if (Time.time - npcscript_component.m_staminaLastBreakTime < instance.StaminaExhaustedMinimumBreakTime)
            {
                return false;
            }
            else if (npcscript_component.m_stamina < instance.MinimumStaminaToRun)
            {
                npcscript_component.m_staminaLastBreakTime = Time.time;
                return false;
            }
        }
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BaseAI), "Follow")]
    private static bool BaseAI_Follow_Prefix(BaseAI __instance, GameObject go, float dt)
    {
        // EXECUTES ON BASEAI::TICK
        if (!__instance.name.Contains("ScriptNPC")) return true;

        float num = Vector3.Distance(go.transform.position, __instance.transform.position);
        //Debug.Log("distance " + num);
        bool run = num > instance.RunUntilDistance;
        if (num < instance.FollowUntilDistance)
        {
            __instance.StopMoving();
        }
        else
        {
            __instance.MoveTo(dt, go.transform.position, 0f, run);
        }

        return false;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Tameable), "Interact")]
    private static void Tameable_Interact_Postfix(Tameable __instance, Humanoid user, bool hold, bool alt, ref bool __result)
    {
        // ON TAME/ON PRESS TAME (E) ON FRIEND
        Debug.Log("tameable interact");
        //instance.PrintAllTags();
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Character), "GetHoverText")]
    private static bool Character_GetHoverText_Prefix(Character __instance, ref string __result)
    {
        // Display stamina on Character hover widget
        Tameable component = __instance.GetComponent<Tameable>();
        if ((bool)component)
        {
            __result = component.GetHoverText();

            ValheimAIModLoader.NPCScript npcscript_component = IsScriptNPC(__instance);
            if (npcscript_component)
            {
                __result += "\nStamina: " + npcscript_component.m_stamina;
            }
            
        }
        return false;
    }

    /*[HarmonyPostfix]
    [HarmonyPatch(typeof(Attack), "Start")]
    private static void Attack_Start_Postfix(Attack __instance)
    {
        // TO FIND OUT ANIMATIONS NAMES
        Debug.Log("Attack anim " + __instance.m_attackAnimation);
    }*/

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Player), "Update")]
    private static void Player_Update_Postfix(Player __instance)
    {
        KeyboardShortcut value = instance.spawnCompanionKey.Value;
        if (value.IsDown())
        {
            instance.SpawnCompanion();
            return;
        }
        value = instance.ToggleFollowKey.Value;
        if (value.IsDown())
        {
            instance.OnToggleFollowKeyPressed(__instance);
            return;
        }
        value = instance.ToggleHarvestKey.Value;
        if (value.IsDown())
        {
            instance.OnToggleHarvestKeyPressed(__instance);
            return;
        }
        value = instance.ToggleAttackKey.Value;
        if (value.IsDown())
        {
            instance.OnToggleAttackKeyPressed(__instance);
            return;
        }
        value = instance.InventoryKey.Value;
        if (value.IsDown())
        {
            instance.OnInventoryKeyPressed(__instance);
            return;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Humanoid), "CustomFixedUpdate")]
    private static void Humanoid_CustomFixedUpdate_Postfix(Humanoid __instance, float fixedDeltaTime)
    {
        if (__instance && __instance.name.Contains("ScriptNPC"))
        {
            MonsterAI monsterAIcomponent = __instance.GetComponent<MonsterAI>();
            Humanoid humanoidcomponent = __instance.GetComponent<Humanoid>();

            if (monsterAIcomponent && monsterAIcomponent.m_follow && monsterAIcomponent.m_follow != Player.m_localPlayer.gameObject)
            {
                if (monsterAIcomponent.m_follow.transform.position.DistanceTo(__instance.transform.position) < instance.FollowUntilDistance + .5f && !humanoidcomponent.InAttack())
                {
                    //Debug.Log("Close enough");
                    monsterAIcomponent.LookAt(monsterAIcomponent.m_follow.transform.position);
                    humanoidcomponent.StartAttack(humanoidcomponent, false);
                }
            }
        }
    }

    private static void SetMonsterAIAggravated(MonsterAI monsterAIcomponent, bool Aggravated)
    {
        if (Aggravated)
        {

        }
        else
        {
            monsterAIcomponent.m_aggravated = false;
            monsterAIcomponent.m_aggravatable = false;
            monsterAIcomponent.m_alerted = false;

            monsterAIcomponent.m_eventCreature = false;
            monsterAIcomponent.m_targetCreature = null;
            //monsterAIcomponent.m_viewRange = 0f;
            monsterAIcomponent.SetHuntPlayer(false);
        }
    }

    private void OnToggleFollowKeyPressed(Player player)
    {
        eNPCMode = eNPCMode == ValheimAIModLoader.NPCMode.Follow ? ValheimAIModLoader.NPCMode.Idle : ValheimAIModLoader.NPCMode.Follow;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            Humanoid humanoidComponent = npc.GetComponent<Humanoid>();
            if (monsterAIcomponent != null && humanoidComponent != null)
            {
                //Debug.Log(("updating npc " + humanoidComponent.GetHoverName()));
                if (eNPCMode == ValheimAIModLoader.NPCMode.Follow)
                {
                    SetMonsterAIAggravated(monsterAIcomponent, false);
                    monsterAIcomponent.SetFollowTarget(player.gameObject);
                    Debug.Log("Everyone now following player!");
                }
                else
                {
                    monsterAIcomponent.SetFollowTarget(null);
                    Debug.Log("Everyone doing their own thing");
                }
            }
            else
            {
                Debug.Log("monsterAI was null for npc in instance.AllPlayerNPCInstances");
            }
        }
    }

    private void OnToggleHarvestKeyPressed(Player player)
    {
        eNPCMode = ValheimAIModLoader.NPCMode.Harvest;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            Humanoid humanoidComponent = npc.GetComponent<Humanoid>();
            if (monsterAIcomponent != null && humanoidComponent != null)
            {
                GameObject ClosestTree = FindClosestTreeFor(npc);

                // disregard nearby enemies
                monsterAIcomponent.m_eventCreature = false;
                monsterAIcomponent.m_targetCreature = null;
                monsterAIcomponent.m_viewRange = 0f;
                monsterAIcomponent.m_alerted = false;
                monsterAIcomponent.m_aggravatable = false;
                monsterAIcomponent.SetHuntPlayer(false);
                monsterAIcomponent.m_aggravated = false;
                monsterAIcomponent.SetFollowTarget(ClosestTree);

                Debug.Log("Everyone harvesting!");

                //TODO: AVOID MULTIPLE NPCS GOING TO CHOP THE SAME TREE
                //TODO: LOOP FUNCTION TO KEEP HARVESTING RESOURCES UNTIL A CONDITION IS MET
            }
        }
    }

    private void OnToggleAttackKeyPressed(Player player)
    {
        eNPCMode = ValheimAIModLoader.NPCMode.Attack;

        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            Humanoid humanoidComponent = npc.GetComponent<Humanoid>();
            if (monsterAIcomponent != null && humanoidComponent != null)
            {
                // disregard nearby enemies
                monsterAIcomponent.SetFollowTarget(null);
                monsterAIcomponent.m_viewRange = 50f;
                monsterAIcomponent.m_alerted = false;
                monsterAIcomponent.m_aggravatable = true;
                monsterAIcomponent.SetHuntPlayer(true);

                Debug.Log("Everyone attacking!");
            }
        }
    }

    private void OnInventoryKeyPressed(Player player)
    {
        GameObject[] allNpcs = FindPlayerNPCs();
        foreach (GameObject npc in allNpcs)
        {
            MonsterAI monsterAIcomponent = npc.GetComponent<MonsterAI>();
            Humanoid humanoidComponent = npc.GetComponent<Humanoid>();
            if (monsterAIcomponent != null && humanoidComponent != null)
            {
                /*GameObject itemPrefab = ZNetScene.instance.GetPrefab("Bread");
                if (itemPrefab != null)
                {
                    humanoidComponent.GetInventory().AddItem(itemPrefab.gameObject, 5);
                    player.GetInventory().AddItem(itemPrefab.gameObject, 5);
                }
                else
                {
                    Debug.LogError($"itemprefab was null");
                }*/

                /*ItemDrop.ItemData bread_itemdata = humanoidComponent.GetInventory().GetItem("$item_bread");
                if (bread_itemdata != null)
                {
                    humanoidComponent.UseItem(humanoidComponent.GetInventory(), bread_itemdata, true);
                }
                else
                {
                    Debug.LogError("bread_itemdata was null");
                }*/

                humanoidComponent.m_zanim.SetTrigger("eat");

                Debug.Log("Print out inventory!");
                PrintInventoryItems(humanoidComponent.m_inventory);
            }
        }
    }

    private static void PrintInventoryItems(Inventory inventory)
    {
        Debug.Log("Character Inventory Items:");

        List<ItemDrop.ItemData> items = inventory.GetAllItems();
        foreach (ItemDrop.ItemData item in items)
        {
            Debug.Log($"- {item.m_shared.m_name} (Quantity: {item.m_stack})");
        }
    }

    private void SpawnCompanion()
    {
        Player localPlayer = Player.m_localPlayer;
        GameObject npcPrefab = ZNetScene.instance.GetPrefab("ScriptNPC");
        if (npcPrefab == null)
        {
            Logger.LogError("ScriptNPC prefab not found!");
        }

        // spawn NPC
        Vector3 spawnPosition = localPlayer.transform.position + localPlayer.transform.forward * 2f;
        Quaternion spawnRotation = localPlayer.transform.rotation;
        GameObject npcInstance = Instantiate<GameObject>(npcPrefab, spawnPosition, spawnRotation);
        npcInstance.SetActive(true);

        // make the monster tame
        MonsterAI monsterAIcomp = npcInstance.GetComponent<MonsterAI>();
        if (monsterAIcomp != null)
        {
            //monsterAIcomp.SetFollowTarget(localPlayer.gameObject);
            monsterAIcomp.MakeTame();
            //monsterAIcomp.SetHuntPlayer(true);
        }
        else
        {
            Logger.LogError("MonsterAI component not found on the instantiated ScriptNPC prefab!");
        }

        // add item to inventory
        Humanoid humanoidComponent = npcInstance.GetComponent<Humanoid>();
        if (humanoidComponent != null)
        {
            GameObject itemPrefab = ZNetScene.instance.GetPrefab("Bread");
            if (itemPrefab != null)
            {
                humanoidComponent.GetInventory().AddItem(itemPrefab.gameObject, 15);
                HitData hitData = new HitData(100);
                humanoidComponent.Damage(hitData);
            }
            else
            {
                Debug.LogError("bread prefab was null");
            }
        }
        else
        {
            Logger.LogError("humanoidComponent component not found on the instantiated ScriptNPC prefab!");
        }
    }

    private IEnumerator FollowPlayer(MonsterAI monsterAI)
    {
        while (monsterAI != null)
        {
            GameObject playerObject = Player.m_localPlayer.gameObject;
            AllPlayerNPCInstancesLastRefresh = Time.deltaTime;

            monsterAI.SetFollowTarget(playerObject);
            monsterAI.MakeTame();

            yield return null;
        }
    }

    private GameObject[] FindEnemies()
    {
        if (Time.time - AllPlayerNPCInstancesLastRefresh < 1f)
        {
            return instance.AllPlayerNPCInstances;
        }
        instance.AllPlayerNPCInstances = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.StartsWith("PlayerNPC"))
                .ToArray();
        AllPlayerNPCInstancesLastRefresh = Time.time;
        return instance.AllPlayerNPCInstances;
    }

    private GameObject[] FindPlayerNPCs()
    {
        if (Time.time - AllPlayerNPCInstancesLastRefresh < 1f)
        {
            return instance.AllPlayerNPCInstances;
        }
        instance.AllPlayerNPCInstances = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.StartsWith("ScriptNPC"))
                .ToArray();
        AllPlayerNPCInstancesLastRefresh = Time.time;
        return instance.AllPlayerNPCInstances;
    }

    private GameObject FindClosestTreeFor(GameObject go, string TreeType = "small")
    {
        if (TreeType == "small")
            return FindSmallTrees().Where(t => t.gameObject.name.StartsWith("Beech_small"))// || t.gameObject.name.StartsWith("Pine"))
                .OrderBy(t => Vector3.Distance(go.transform.position, t.transform.position))
                .FirstOrDefault();
        return null;
    }

    private static GameObject[] FindSmallTrees()
    {
        instance.SmallTrees = GameObject.FindObjectsOfType<GameObject>(true)
                .Where(go => go.name.StartsWith("Beech_small"))
                .ToArray();
        return instance.SmallTrees;
    }

    // Disable auto save
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Game), "UpdateSaving")]
    private static bool Game_UpdateSaving_Prefix()
    {
        return !instance.DisableAutoSave.Value;
    }
}
