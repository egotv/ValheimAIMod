using BepInEx;
using HarmonyLib;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
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

            FindPlayerNPC();

            if (!PlayerNPC)
            {
                //LogWarning("Local player spawned, but there is no NPC in the world. Trying to spawn an NPC in 1 second...");

                instance.SetTimer(1f, () =>
                {
                    if (!PlayerNPC)
                    {
                        LogWarning("Spawning a Thrall into the world!");
                        instance.SpawnCompanion();
                        if (humanoid_PlayerNPC)
                        {
                            string text = $"Hey there, I'm {(humanoid_PlayerNPC.m_name != "" ? humanoid_PlayerNPC.m_name : "your Thrall")}. Press and hold T to talk with me.";
                            instance.AddChatTalk(humanoid_PlayerNPC, "NPC", text);
                            instance.BrainSynthesizeAudio(text, npcVoices[instance.npcVoice].ToLower());
                        }
                    }
                });
            }
        }

        private static bool FindPlayerNPCTimer = false;

        // PROCESS PLAYER INPUT
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "Update")]
        private static void Player_Update_Postfix(Player __instance)
        {
            //LogError($"__instance.m_autoPickupMask: {__instance.m_autoPickupMask}");

            if (!FindPlayerNPCTimer)
            {
                instance.SetTimer(0.5f, () =>
                {
                    FindPlayerNPC();
                    FindPlayerNPCTimer = false;
                });

                FindPlayerNPCTimer = true;
            }

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

            if (humanoid_PlayerNPC)
            {
                List<Minimap.PinData> pds = new List<Minimap.PinData>();
                foreach (Minimap.PinData pd in Minimap.instance.m_pins)
                {
                    if (pd.m_author == "NPC" && humanoid_PlayerNPC.npcPinData != null && pd != humanoid_PlayerNPC.npcPinData)
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

            if (IsInventoryShowing && (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetKeyDown(instance.inventoryKey.Value)))
            {
                SaveNPCData(PlayerNPC);
                IsInventoryShowing = false;

                return;
            }

            if (IsModMenuShowing && ZInput.GetKeyDown(KeyCode.Escape))
            {
                instance.ToggleModMenu();

                return;
            }

            if (ZInput.GetKeyDown(instance.inventoryKey.Value) && __instance.m_hovering == PlayerNPC && PlayerNPC != null)
            {
                IsInventoryShowing = InventoryGui.instance.m_animator.GetBool("visible");
                //LogError($"IsInventoryShowing {IsInventoryShowing}");
                SaveNPCData(PlayerNPC);
                return;
            }

            if (Console.IsVisible())
            {
                return;
            }

            if (IsModMenuShowing && ZInput.GetKeyDown(instance.thrallMenuKey.Value))
            {
                instance.ToggleModMenu();

                return;
            }

            if (Menu.IsVisible() || Chat.instance.HasFocus() || !__instance.TakeInput())
            {
                //Debug.Log("Menu visible");
                //Debug.Log("Ignoring input: Menu, console, chat or mod menu is showing");
                return;
            }

            /*if (!__instance.m_hovering || CleanKey(__instance.m_hovering.name) != "HumanoidNPC")
            {
                return;
            }*/


            if (ZInput.GetKeyDown(instance.thrallMenuKey.Value))
            {
                //LogInfo("Keybind: Thrall Menu");

                instance.ToggleModMenu();

                return;
            }



            /*if (ZInput.GetKeyDown(instance.inventoryKey.Value) && !IsInventoryShowing && PlayerNPC && PlayerNPC.transform.position.DistanceTo(__instance.transform.position) < 5 && __instance.m_hovering && CleanKey(__instance.m_hovering.name) == "HumanoidNPC")
            {
                LogInfo("Keybind: Inventory");
                LogError("opening invenotry");
                instance.OnInventoryKeyPressed(__instance, true);
                return;
            }*/



            if (ZInput.GetKeyDown(instance.spawnKey.Value))
            {
                FindPlayerNPC();
                if (PlayerNPC)
                {

                    SaveNPCData(PlayerNPC);
                    humanoid_PlayerNPC.AddPoisonDamage(100000);
                    humanoid_PlayerNPC.m_health = 0;
                    /*humanoidNPC.AddFireDamage(100000);
                    humanoidNPC.AddFrostDamage(100000);
                    humanoidNPC.AddLightningDamage(100000);
                    humanoidNPC.AddSpiritDamage(100000);*/

                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"Thrall left the world!");
                    /*PlayerNPC = null;
                    humanoid_PlayerNPC = null;*/
                }
                else
                {
                    //LogInfo("Keybind: Spawn Companion");
                    instance.SpawnCompanion();
                }


                return;
            }

            if (ZInput.GetKeyDown(instance.followKey.Value) && PlayerNPC)
            {
                HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();

                if (NPCCurrentCommandType != NPCCommand.CommandType.FollowPlayer)
                {
                    //LogInfo("Keybind: Follow Player");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} now following you!");
                    instance.Follow_Start(__instance.gameObject);
                }
                else
                {
                    //LogInfo("Keybind: Patrol Area");
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} now patrolling this area!");
                    instance.Patrol_Start();
                }
                return;
            }

            if (ZInput.GetKeyDown(instance.harvestKey.Value) && PlayerNPC)
            {
                HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();

                if (NPCCurrentCommandType == NPCCommand.CommandType.HarvestResource)
                {
                    //LogInfo("Keybind: Stop All Harvesting");

                    instance.commandManager.RemoveCommandsOfType<HarvestAction>();
                    //instance.Harvesting_Stop();
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} stopped harvesting!");
                }
                else
                {
                    //LogInfo("Keybind: Harvest");

                    HarvestAction action = new HarvestAction();
                    action.humanoidNPC = npc;
                    action.ResourceName = "Wood";
                    action.RequiredAmount = 20;
                    action.OriginalRequiredAmount = 20;
                    instance.commandManager.AddCommand(action);

                    MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} harvesting {action.ResourceName.ToLower()}!");
                }

                return;
            }

            if (ZInput.GetKeyDown(instance.combatModeKey.Value) && PlayerNPC)
            {
                //LogInfo("Keybind: Change Combat Mode");

                HumanoidNPC npc = PlayerNPC.GetComponent<HumanoidNPC>();

                ToggleNPCCurrentCommandType();

                MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, $"{npc.m_name} is now {NPCCurrentMode.ToString()}");
            }

            if (ZInput.GetKey(instance.talkKey.Value) && !IsRecording)
            {
                //LogInfo("Keybind: Start Recording");

                instance.StartRecording();
                return;
            }
            else if (!ZInput.GetKey(instance.talkKey.Value) && IsRecording)
            {
                if (Time.time - recordingStartedTime > 1f)
                {
                    shortRecordingWarningShown = false;
                    instance.StopRecording();
                    instance.SendRecordingToBrain();
                }
                else if (!shortRecordingWarningShown)
                {
                    //Debug.Log("Recording was too short. Has to be atleast 1 second long");
                    //MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Recording must be atleast 1 second long.");
                    shortRecordingWarningShown = true;
                }
                return;
            }

            if (ZInput.GetKeyDown(KeyCode.P))
            {

                var resourceName = CurrentHarvestResourceName;
                var sources = instance.FindResourceSourcesRecursive(resourceName, __instance.GetCurrentWeapon());

                sources = sources.OrderByDescending(s => s.Efficiency).ToList();

                if (sources.Count == 0)
                {
                    Debug.Log($"No sources found for {resourceName} in nearby resources.");
                }
                else
                {
                    Debug.Log($"Sources for {resourceName}, sorted by efficiency:");
                    for (int i = 0; i < sources.Count; i++)
                    {
                        var (category, name, efficiency, distance, depth) = sources[i];
                        if (efficiency > 0)
                        {
                            Debug.Log($"{i + 1}. {category} - {name} (Efficiency: {efficiency:F2}, Distance: {distance:F2}, Depth: {depth})");
                        }
                        else
                        {
                            Debug.Log($"{i + 1}. {category} - {name} (Not interactable with current weapon, Distance: {distance:F2}, Depth: {depth})");
                        }
                    }
                }

                return;
            }
        }

        static bool IsInventoryShowing = false;
        private void OnInventoryKeyPressed(Player player, bool Show)
        {
            //LogError("OnInventoryKeyPressed pressed ");
            if (PlayerNPC)
            {
                //LogError("e pressed ");
                SaveNPCData(PlayerNPC);
                if (!Show)
                {
                    InventoryGui.instance.Hide();
                    IsInventoryShowing = false;
                }
                else
                {
                    HumanoidNPC humanoidNPC_component = PlayerNPC.GetComponent<HumanoidNPC>();
                    InventoryGui.instance.Show(humanoidNPC_component.inventoryContainer);
                    //PrintInventoryItems(humanoidNPC_component.m_inventory);
                    IsInventoryShowing = true;
                }
            }
            else
            {
                LogError("OnInventoryKeyPressed PlayerNPC is null ");
            }
        }

        private void SpawnCompanion()
        {
            FindPlayerNPC();
            if (PlayerNPC)
            {
                LogError("Spawning more than one NPC is disabled");
                return;
            }
            Player localPlayer = Player.m_localPlayer;
            GameObject npcPrefab = ZNetScene.instance.GetPrefab("HumanoidNPC");

            instance.commandManager.RemoveAllCommands();
            enemyList.Clear();


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

            PlayerNPC = npcInstance;

            if (npcInstance.HasAnyComponent("Tameable"))
            {
                //Debug.Log("removing npc tameable comp");
                Destroy(npcInstance.GetComponent<Tameable>());
            }

            // make the monster tame
            MonsterAI monsterAIcomp = npcInstance.GetComponent<MonsterAI>();

            SetMonsterAIAggravated(monsterAIcomp, false);
            monsterAIcomp.MakeTame();
            monsterAIcomp.SetFollowTarget(localPlayer.gameObject);
            monsterAIcomp.m_viewRange = 80f;
            monsterAIcomp.m_alertRange = 800f;
            //monsterAIcomp.m_updateTargetTimer = 1000000f;
            monsterAIcomp.m_maxChaseDistance = 500f;
            monsterAIcomp.m_fleeIfLowHealth = 0;
            monsterAIcomp.m_fleeIfNotAlerted = false;
            monsterAIcomp.m_fleeIfHurtWhenTargetCantBeReached = false;


            // passive stuf
            monsterAIcomp.m_aggravatable = false;
            monsterAIcomp.m_alerted = false;
            monsterAIcomp.m_aggravated = false;
            monsterAIcomp.m_targetCreature = null;
            monsterAIcomp.SetHuntPlayer(false);
            monsterAIcomp.m_viewRange = 0;

            NPCCurrentMode = NPCMode.Defensive;

            // add item to inventory
            HumanoidNPC humanoidNpc_Component = npcInstance.GetComponent<HumanoidNPC>();
            humanoid_PlayerNPC = humanoidNpc_Component;
            if (humanoidNpc_Component != null)
            {
                LoadNPCData(humanoidNpc_Component);
                EquipBestClothes(humanoidNpc_Component);

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

            //StartBrainPeriodicUpdateTimer();
        }

        protected virtual void OnNPCDeath()
        {
            MessageHud.instance.ShowMessage(MessageHud.MessageType.Center, "Your Thrall died! Press [G] to respawn");

            HumanoidNPC humanoidNPC = PlayerNPC.GetComponent<HumanoidNPC>();

            //PrintInventoryItems(humanoidNPC.m_inventory);

            SaveNPCData(PlayerNPC);
        }





        [HarmonyPrefix]
        [HarmonyPatch(typeof(ZNetScene), "RemoveObjects")]
        private static bool ZNetScene_RemoveObjects_Prefix(ZNetScene __instance, List<ZDO> currentNearObjects, List<ZDO> currentDistantObjects)
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

        // Disable auto save
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Game), "UpdateSaving")]
        private static bool Game_UpdateSaving_Prefix()
        {
            if (instance.DisableAutoSave == null)
                return false;
            return !instance.DisableAutoSave.Value;
        }
    }
}
