using BepInEx;
using HarmonyLib;
using Jotunn;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace ValheimAIModLoader
{
    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MonsterAI), "UpdateAI")]
        private static bool MonsterAI_UpdateAI_Prefix(MonsterAI __instance)
        {
            if (!Player.m_localPlayer || !__instance) return true;

            if (!__instance.name.Contains("HumanoidNPC")) return true;


            HumanoidNPC humanoidNPC = __instance.gameObject.GetComponent<HumanoidNPC>();


            //LogError($"following {(__instance.m_follow ? __instance.m_follow.name : "null")} {(__instance.m_follow ? __instance.m_follow.transform.position.DistanceTo(__instance.transform.position).ToString() : "0")} target {(__instance.m_targetCreature ? __instance.m_targetCreature.name : "null")} {(__instance.m_targetCreature ? __instance.m_targetCreature.transform.position.DistanceTo(__instance.transform.position).ToString() : "0")}");


            if (NPCCurrentMode != NPCMode.Passive)
            {
                //EnemyListNullCheck();

                if (enemyList.Count > 0)
                {
                    __instance.m_viewRange = 80;
                    if (__instance.m_targetCreature == null || !enemyList.Contains(__instance.m_targetCreature))
                    {
                        if (__instance.m_targetCreature == null)
                        {
                            LogError("__instance.m_targetCreature == null");
                        }
                        else if (!enemyList.Contains(__instance.m_targetCreature))
                        {
                            LogError($"{enemyList.Count} !enemyList.Contains(__instance.m_targetCreature.gameObject)");
                        }

                        enemyList.Where(go => go != null).OrderBy(go => go.transform.position.DistanceTo(__instance.transform.position));

                        //Character character = enemyList[0].GetComponent<Character>();

                        foreach (var go in enemyList.ToArray())
                        {
                            if (!go) continue;

                            //Character character = GetCharacterFromGameObject(go);
                            Character character = go;

                            if (character != null)
                            {
                                //__instance.SetFollowTarget(null);
                                //__instance.m_targetCreature = character;

                                __instance.m_targetCreature = null;
                                __instance.SetTarget(character);
                                __instance.m_updateTargetTimer = 1000000f;
                                LogError($"New enemy in defensive mode: {character.name}");

                                __instance.m_alerted = false;
                                __instance.m_aggravatable = true;
                                __instance.SetHuntPlayer(true);

                                //break;
                                return true;
                            }
                            else
                            {
                                LogError("Defensive mode enemy from enemyList wasn't a character");
                            }
                        }

                        if (!__instance.m_targetCreature && enemyList.Count > 0)
                        {
                            enemyList.Clear();
                            LogError("enemy list cleared");
                        }
                    }
                }
            }



            NPCCommand command = instance.commandManager.GetNextCommand();

            //if (command != null)
            if (NPCCurrentCommand == null || NPCCurrentCommand != command && command != null)
            {
                NPCCurrentCommand = command;
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

            /* auto pickup
             * if (Time.time > instance.LastFindClosestItemDropTime + 1.5 && 
                !(NPCCurrentMode == NPCMode.Defensive && enemyList.Count > 0) && 
                NPCCurrentCommandType != NPCCommand.CommandType.CombatAttack)
                //&& NPCCurrentCommandType != NPCCommand.CommandType.HarvestResource)
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
            }*/

            if (NPCCurrentCommandType == NPCCommand.CommandType.PatrolArea && patrol_position != Vector3.zero)
            {
                float dist = __instance.transform.position.DistanceTo(patrol_position);

                if (dist > chaseUntilPatrolRadiusDistance && !MovementLock)
                {
                    SetMonsterAIAggravated(__instance, false);
                    MovementLock = true;
                    LogInfo($"{humanoidNPC.m_name} went too far ({chaseUntilPatrolRadiusDistance}m away) from patrol position, heading back now!");
                }
                else if (dist < instance.patrol_radius - 3f)
                {
                    MovementLock = false;
                    __instance.m_lastKnownTargetPos = patrol_position;
                }
                else if (dist < instance.patrol_radius)
                {
                    __instance.m_aggravatable = true;
                }

                if (MovementLock)
                {
                    __instance.MoveTo(Time.deltaTime, patrol_position, 0f, false);
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
                    if (followtarget == null || followtarget.transform.position.DistanceTo(patrol_position) > chaseUntilPatrolRadiusDistance ||
                                    (!followtarget.HasAnyComponent("Pickable") && !followtarget.HasAnyComponent("ItemDrop")))
                    {
                        List<Pickable> closestPickables = SphereSearchForGameObjectsWithComponent<Pickable>(patrol_position, chaseUntilPatrolRadiusDistance - 2);

                        if (closestPickables.Count == 0)
                        {
                            LogMessage($"{humanoidNPC.m_name} has picked up all dropped items around the patrolling area. Only keeping guard now!");
                            instance.patrol_harvest = false;
                            return true;
                        }

                        foreach (Pickable closestPickable in closestPickables)
                        {
                            if (closestPickable == null)
                            {
                                LogMessage($"{humanoidNPC.m_name} has picked up all dropped items around the patrolling area. Only keeping guard now!");
                                instance.patrol_harvest = false;
                                return true;
                            }

                            else if (closestPickable.transform.position.DistanceTo(patrol_position) < chaseUntilPatrolRadiusDistance)
                            {
                                LogMessage($"{humanoidNPC.m_name} is going to pickup {closestPickable.name} in patrol area, distance: {closestPickable.transform.position.DistanceTo(__instance.transform.position)}");
                                __instance.SetFollowTarget(closestPickable.gameObject);
                                return true;
                            }
                            else
                            {
                                LogInfo("Closest pickable's distance is too far!");
                            }
                        }
                    }
                }
                else
                {
                    //LogError("chilling in patrol area");
                    __instance.RandomMovementArroundPoint(Time.deltaTime, patrol_position, 7f, false);
                    return false;
                }

                return true;
            }

            else if (NPCCurrentCommandType == NPCCommand.CommandType.HarvestResource && (enemyList.Count == 0))
            {
                if ((__instance.m_follow == null && __instance.m_targetCreature == null) ||
                    !ResourceNodesOneArray.Contains(CleanKey(__instance.m_follow.name)) ||
                    __instance.m_follow == Player.m_localPlayer)
                {
                    //comehere


                    if (Time.time - closestItemDropsLastRefresh > 3 || closestItemDrops.Count < 1)
                    {
                        closestItemDrops = SphereSearchForGameObjectsWithComponent<ItemDrop>(__instance.transform.position, 7);
                        foreach (ItemDrop closestItemDrop in closestItemDrops)
                        {
                            if (closestItemDrop != null && closestItemDrop.gameObject != __instance.m_follow && IsStringEqual(closestItemDrop.name, CurrentHarvestResourceName, true))
                            {
                                if (humanoidNPC.m_inventory.CanAddItem(closestItemDrop.m_itemData))
                                {
                                    LogMessage($"{humanoidNPC.m_name} is going to pickup nearby dropped item on the ground {closestItemDrop.name} before harvesting");
                                    __instance.SetFollowTarget(closestItemDrop.gameObject);
                                    return true;
                                }
                            }
                        }
                    }


                    ItemDrop.ItemData currentWeaponData = humanoidNPC.GetCurrentWeapon();
                    var sources = instance.FindResourceSourcesRecursive(CurrentHarvestResourceName, currentWeaponData);
                    sources = sources.OrderByDescending(s => s.Efficiency).ToList();
                    GameObject resource;
                    bool success = false;

                    foreach (var source in sources)
                    {
                        if (success) continue;

                        resource = FindClosestResource(PlayerNPC, source.Name, false);

                        if (resource != null)
                        {
                            if (source.Category == "CharacterDrop")
                            {
                                Character targetCharacter = GetCharacterFromGameObject(resource);
                                if (targetCharacter != null)
                                {
                                    //__instance.SetTarget(targetCharacter);
                                    //__instance.m_targetCreature = targetCharacter;

                                    __instance.m_targetCreature = null;
                                    __instance.SetTarget(targetCharacter);
                                    __instance.m_updateTargetTimer = 1000000f;
                                    LogMessage($"{humanoidNPC.m_name} is going to harvest {resource.name} by killing {targetCharacter.name}");
                                    success = true;
                                }
                                else
                                {
                                    LogError("Harvesting failed! Closest resource was a CharacterDrop but has no Character component");
                                }

                            }
                            else
                            {
                                __instance.SetFollowTarget(resource);
                                LogMessage($"{humanoidNPC.m_name} is going to harvest {resource.name}");
                                success = true;
                            }
                        }
                    }

                    if (!success)
                    {
                        LogMessage($"Couldnt find any resources to harvest for {CurrentHarvestResourceName}");
                        LogInfo($"Removing harvest {CurrentHarvestResourceName} command");
                        CurrentHarvestResourceName = "Wood";
                        instance.commandManager.RemoveCommand(0);
                    }
                }
            }

            else if (NPCCurrentCommandType == NPCCommand.CommandType.CombatAttack)
            {
                //if (__instance.m_follow == null || !__instance.m_follow.HasAnyComponent("Character", "Humanoid", "BaseAI", "MonsterAI", "AnimalAI"))
                if (__instance.m_targetCreature == null || (!IsStringStartingWith(CurrentEnemyName, __instance.m_targetCreature.name, true) && !enemyList.Contains(__instance.m_targetCreature)))
                {
                    Character character = FindClosestEnemy(__instance.gameObject, CurrentEnemyName);
                    if (character)
                    {
                        //__instance.SetFollowTarget(null);
                        //__instance.SetTarget(character);
                        __instance.m_targetCreature = null;
                        __instance.SetTarget(character);
                        __instance.m_updateTargetTimer = 1000000f;
                        //__instance.m_timeSinceAttacking = 0;
                        LogMessage($"CombatAttack new target set: {CleanKey(character.name)}");
                    }
                    else
                    {
                        instance.commandManager.RemoveCommand(0);
                        LogError("CommandType.CombatAttack, findclosestenemy returned null. Probably no more targets in the area. Removing combat command");
                    }
                }
            }

            else if (NPCCurrentCommandType == NPCCommand.CommandType.FollowPlayer)
            {
                if (__instance.m_follow && __instance.m_follow != Player.m_localPlayer.gameObject && !__instance.m_follow.HasAnyComponent("ItemDrop", "Pickable") && (enemyList.Count == 0))
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



            if (NPCCurrentMode == NPCMode.Passive)
            {
                //LogError("Passive");

                //SetMonsterAIAggravated(__instance, false);
                __instance.m_aggravatable = false;
                __instance.m_alerted = false;
                __instance.m_aggravated = false;
                __instance.m_targetCreature = null;
                __instance.SetHuntPlayer(false);
                __instance.m_viewRange = 0;

            }
            else if (NPCCurrentMode == NPCMode.Defensive)
            {
                if (NPCCurrentCommandType == NPCCommand.CommandType.CombatAttack)
                {
                    //SetMonsterAIAggravated(__instance, true);
                    //__instance.m_aggravatable = true;
                    //__instance.m_alerted = true;
                    //__instance.m_aggravated = true;
                    //__instance.SetHuntPlayer(true);

                    __instance.m_alerted = false;
                    __instance.m_aggravatable = true;
                    __instance.SetHuntPlayer(true);


                    __instance.m_viewRange = 80;
                }
                /*else
                {
                    //SetMonsterAIAggravated(__instance, false);
                    __instance.m_aggravatable = false;
                    __instance.m_alerted = false;
                    __instance.m_aggravated = false;
                    __instance.m_targetCreature = null;
                    __instance.SetHuntPlayer(false);
                    __instance.m_viewRange = 0;
                }*/
            }
            else if (NPCCurrentMode == NPCMode.Aggressive && !__instance.m_aggravated)
            {
                SetMonsterAIAggravated(__instance, true);
                __instance.SetAggravated(true, BaseAI.AggravatedReason.Theif);
                __instance.SetAlerted(true);
                __instance.SetHuntPlayer(true);
                __instance.m_viewRange = 80;
            }



            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(HumanoidNPC), "CustomFixedUpdate")]
        private static void HumanoidNPC_CustomFixedUpdate_Postfix(HumanoidNPC __instance)
        {
            MonsterAI monsterAIcomponent = __instance.GetComponent<MonsterAI>();

            if (!monsterAIcomponent)
            {
                LogError("HumanoidNPC_CustomFixedUpdate_Postfix failed! monsterAIcomponent null");
                return;
            }

            if (NPCCurrentCommandType == NPCCommand.CommandType.FollowPlayer && (Player.m_localPlayer == null || monsterAIcomponent.m_follow == null))
            {
                monsterAIcomponent.SetFollowTarget(null);
                return;
            }


            if (__instance.LastPosition.DistanceTo(__instance.transform.position) > 1.5f || __instance.m_lastHit != NPCLastHitData)
            {
                NPCLastHitData = __instance.m_lastHit;
                __instance.LastPosition = __instance.transform.position;
                __instance.LastMovedAtTime = Time.time;
            }
            else
            {
                /*if (NPCCurrentCommandType != NPCCommand.CommandType.FollowPlayer)
                    __instance.LastPositionDelta += Time.deltaTime;*/
            }

            GameObject followTarget = monsterAIcomponent.m_follow;
            Character followTargetCharacter = null;
            if (followTarget)
                followTargetCharacter = followTarget.gameObject.GetComponent<Character>();


            if (monsterAIcomponent.m_targetCreature && IsRangedWeapon(__instance.GetCurrentWeapon()) && CheckArrows(__instance.m_inventory) > 0)
                return;



            if (followTarget != null && followTarget != Player.m_localPlayer.gameObject)
            {
                if (useWeapon != null && !__instance.IsItemEquiped(useWeapon))
                {
                    __instance.EquipItem(useWeapon);
                }

                float distanceBetweenTargetAndSelf = followTarget.transform.position.DistanceTo(__instance.transform.position);
                //float distanceBetweenTargetAndSelf = DistanceBetween(monsterAIcomponent.gameObject, __instance.gameObject);

                if (NPCCurrentCommandType == NPCCommand.CommandType.HarvestResource &&
                    followTarget.HasAnyComponent("Destructible", "TreeBase", "TreeLog", "MineRock", "MineRock5") &&
                            (Time.time - NewFollowTargetLastRefresh > MaxChaseTimeForOneFollowTarget && NewFollowTargetLastRefresh != 0) ||
                            (Time.time - __instance.LastMovedAtTime > MaxChaseTimeForOneFollowTarget && distanceBetweenTargetAndSelf < 8))
                {
                    // time for new follow target
                    LogMessage($"NPC seems to be stuck for >20s while trying to harvest {followTarget.gameObject.name}, evading task!");
                    blacklistedItems.Add(followTarget.gameObject);
                    __instance.LastMovedAtTime = Time.time;
                    RefreshAllGameObjectInstances();
                    monsterAIcomponent.SetFollowTarget(null);
                    return;
                }

                if (!__instance.InAttack())
                {
                    float MinDistanceAllowed = 0.8f;
                    if (followTarget.HasAnyComponent("ItemDrop", "Pickable"))
                        MinDistanceAllowed = 2f;
                    else if (followTarget.HasAnyComponent("TreeLog"))
                        MinDistanceAllowed = 1f;
                    else if (followTarget.HasAnyComponent("TreeBase"))
                        MinDistanceAllowed = 1.25f;

                    if (distanceBetweenTargetAndSelf < FollowUntilDistance + MinDistanceAllowed + 6)
                    {
                        if (NewFollowTargetLastRefresh == 0)
                        {
                            NewFollowTargetLastRefresh = Time.time;
                            //LogError($"NewFollowTargetLastRefresh set to {Time.time}");
                        }
                    }
                    if (distanceBetweenTargetAndSelf < FollowUntilDistance + MinDistanceAllowed)
                    //if (PerformRaycast(__instance) == followTarget.gameObject)
                    {
                        if (followTarget.HasAnyComponent("ItemDrop"))
                        {
                            instance.PickupItemDrop(__instance, monsterAIcomponent);

                            if (!followTarget)
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
                        else if (followTarget.HasAnyComponent("Pickable"))
                        {
                            __instance.DoInteractAnimation(followTarget.transform.position);

                            Pickable pick = followTarget.GetComponent<Pickable>();
                            pick.Interact(Player.m_localPlayer, false, false);

                            Destroy(followTarget);
                            //instance.AllPickableInstances.Remove(followTarget);

                            /*if (!followTarget)
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
                        else if (followTarget != Player.m_localPlayer.gameObject && !HasAnyChildComponent(followTarget, new List<Type>() { typeof(Character) }))
                        {
                            bool secondaryAnimation = false;
                            string targetname = followTarget.name.ToLower();
                            if ((targetname.Contains("log") && targetname.Contains("half")) || (targetname.Contains("log") && __instance.transform.position.z - followTarget.transform.position.z > -5))
                                secondaryAnimation = true;
                            else if (followTarget.name.ToLower().Contains("log"))
                                secondaryAnimation = UnityEngine.Random.value > 0.25f;
                            monsterAIcomponent.LookAt(followTarget.transform.position);
                            //__instance.StartAttack(followTargetCharacter ? followTargetCharacter : __instance, UnityEngine.Random.value > 0.5f);
                            __instance.StartAttack(followTargetCharacter ? followTargetCharacter : __instance, secondaryAnimation);

                            LogError($"Attacking resource {targetname}");
                        }
                    }
                    else if ((__instance.GetVelocity().magnitude < .2f && Time.time - __instance.LastMovedAtTime > 3f && !monsterAIcomponent.CanMove(__instance.transform.position - followTarget.transform.position, 1f, 1f)))// || CalculateXYDistance(followTarget.transform.position, __instance.transform.position) < 4)
                    {
                        monsterAIcomponent.LookAt(followTarget.transform.position);
                        __instance.StartAttack(followTargetCharacter ? followTargetCharacter : __instance, UnityEngine.Random.value > 0.5f);

                        // add random movement
                    }
                    /*else if (monsterAIcomponent.m_follow.HasAnyComponent("Character", "Humanoid") && monsterAIcomponent.m_follow != Player.m_localPlayer.gameObject && IsRangedWeapon(__instance.GetCurrentWeapon())
                        && distanceBetweenTargetAndSelf < 25)
                    {
                        Character character = monsterAIcomponent.m_follow.GetComponent<Character>();
                        if (character && monsterAIcomponent.CanSeeTarget(character))
                        {
                            LogError("Can see target");
                            if (CheckArrows(__instance.GetInventory()) > 0)
                            {
                                monsterAIcomponent.LookAt(character.transform.position);

                                if (monsterAIcomponent.IsLookingAt(character.transform.position, __instance.GetCurrentWeapon().m_shared.m_aiAttackMaxAngle, __instance.GetCurrentWeapon().m_shared.m_aiInvertAngleCheck))
                                {
                                    LogError("Doing attack");
                                    //monsterAIcomponent.DoAttack(character, isFriend: false);
                                    __instance.StartAttack(character, false);
                                }
                                //__instance.StartAttack(character, false);
                            }
                        }
                        
                    }*/
                    /*else
                        Debug.Log("velocity " + __instance.GetVelocity());*/
                }

            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MonsterAI), "SetFollowTarget")]
        private static void MonsterAI_SetFollowTarget_Postfix(MonsterAI __instance)
        {
            if (!IsStringEqual(__instance.name, NPCPrefabName, true)) { return; }

            NewFollowTargetLastRefresh = 0;
            useWeapon = null;
            //LogError("NewFollowTargetLastRefresh set to 0");

            if (__instance.m_follow == null)
            {
                targetDamageModifiers = new HitData.DamageModifiers();
                return;
            }

            GameObject followTarget = __instance.m_follow.gameObject;

            if (followTarget.HasAnyComponent("TreeLog"))
            {
                TreeLog comp = followTarget.GetComponent<TreeLog>();
                targetDamageModifiers = comp.m_damages;
            }
            else if (followTarget.HasAnyComponent("TreeBase"))
            {
                TreeBase comp = followTarget.GetComponent<TreeBase>();
                targetDamageModifiers = comp.m_damageModifiers;
            }
            else if (followTarget.HasAnyComponent("Character", "Humanoid"))
            {
                Character comp = followTarget.GetComponent<Character>();
                if (comp)
                    targetDamageModifiers = comp.m_damageModifiers;
                else
                {
                    Humanoid comp2 = followTarget.GetComponent<Humanoid>();
                    if (comp2)
                        targetDamageModifiers = comp2.m_damageModifiers;
                }
            }
            else if (followTarget.HasAnyComponent("MineRock"))
            {
                MineRock comp = followTarget.GetComponent<MineRock>();
                targetDamageModifiers = comp.m_damageModifiers;
            }
            else if (followTarget.HasAnyComponent("MineRock5"))
            {
                MineRock5 comp = followTarget.GetComponent<MineRock5>();
                targetDamageModifiers = comp.m_damageModifiers;
            }
            else if (followTarget.HasAnyComponent("Destructible"))
            {
                Destructible comp = followTarget.GetComponent<Destructible>();
                targetDamageModifiers = comp.m_damages;
            }
            else
            {
                targetDamageModifiers = new HitData.DamageModifiers();
                return;
            }
            //LogError($"targetDamageModifiers for {followTarget.name}");
            //targetDamageModifiers.Print();

            if (humanoid_PlayerNPC)
            {
                ItemDrop.ItemData bestWeapon = GetBestHarvestingTool(humanoid_PlayerNPC.m_inventory.m_inventory, targetDamageModifiers);
                if (bestWeapon != null)
                {
                    LogInfo($"Equipping best weapon: {bestWeapon.m_shared.m_name} for {CleanKey(followTarget.name)}");
                    useWeapon = bestWeapon;
                    humanoid_PlayerNPC.EquipItem(bestWeapon);
                }
            }
            else
            {
                //LogError($"no humanoid_PlayerNPC");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), "StartAttack")]
        private static bool Humanoid_StartAttack_Prefix(Humanoid __instance, Character target, bool secondaryAttack, bool __result)
        {
            if ((__instance.InAttack() && !__instance.HaveQueuedChain()) || __instance.InDodge() || !__instance.CanMove() || __instance.IsKnockedBack() || __instance.IsStaggering() || __instance.InMinorAction())
            {
                __result = false;
                return false;
            }

            ItemDrop.ItemData currentWeapon = __instance.GetCurrentWeapon();
            if (currentWeapon == null)
            {
                __result = false;
                return false;
            }

            if (secondaryAttack && !currentWeapon.HaveSecondaryAttack())
            {
                __result = false;
                return false;
            }

            if (!secondaryAttack && !currentWeapon.HavePrimaryAttack())
            {
                __result = false;
                return false;
            }

            if (__instance.m_currentAttack != null)
            {
                __instance.m_currentAttack.Stop();
                __instance.m_previousAttack = __instance.m_currentAttack;
                __instance.m_currentAttack = null;
            }

            Attack attack = ((!secondaryAttack) ? (attack = currentWeapon.m_shared.m_attack.Clone()) : (attack = currentWeapon.m_shared.m_secondaryAttack.Clone()));
            if (attack.Start(__instance, __instance.m_body, __instance.m_zanim, __instance.m_animEvent, __instance.m_visEquipment, currentWeapon, __instance.m_previousAttack, __instance.m_timeSinceLastAttack, 1f))
            {
                __instance.ClearActionQueue();
                __instance.StartAttackGroundCheck();
                __instance.m_currentAttack = attack;
                __instance.m_currentAttackIsSecondary = secondaryAttack;
                __instance.m_lastCombatTimer = 0f;
                __result = true;
                //return true;
            }

            __result = false;
            return false;
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
                RunDistance = RunUntilDistance;
                FollowDistance = FollowUntilDistance;
            }


            float num = Vector3.Distance(go.transform.position, __instance.transform.position);
            bool run = num > RunDistance;
            if (NPCCurrentCommandType == NPCCommand.CommandType.FollowPlayer)
                run = run && !Player.m_localPlayer.IsCrouching() && !Player.m_localPlayer.m_walk;
            if (NPCCurrentCommandType == NPCCommand.CommandType.CombatSneakAttack)
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
                __result += $"\n<color=yellow><b>[{instance.inventoryKey.Value.ToString()}]</b></color> Inventory";
                __result += $"\n<color=yellow><b>[{instance.talkKey.Value.ToString()}]</b></color> Push to Talk";
                __result += $"\n<color=yellow><b>[{instance.thrallMenuKey.Value.ToString()}]</b></color> Menu";
                __result += $"\n<color=yellow><b>[{instance.combatModeKey.Value.ToString()}]</b></color> Combat Mode: <color=yellow>{NPCCurrentMode}</color>";

                return false; // Skip original method
            }

            return true; // Continue to original method
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
                //__instance.Pickup(component.gameObject);

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
                    if (NPCCurrentCommandType == NPCCommand.CommandType.HarvestResource &&
                        IsStringEqual(CurrentHarvestResourceName, PickupItemDropName, true))
                    {
                        if (NPCCurrentCommand != null && NPCCurrentCommand is HarvestAction)
                        {
                            HarvestAction action = (HarvestAction)NPCCurrentCommand;
                            action.RequiredAmount = Math.Max(action.RequiredAmount - stack, 0);
                            //LogInfo($"[Harvest Task] : {action.RequiredAmount} {action.ResourceName} remaining");
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

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), "EquipBestWeapon")]
        private static bool Humanoid_EquipBestWeapon_Prefix(Humanoid __instance, Character targetCreature, StaticTarget targetStatic, Character hurtFriend, Character friend)
        {
            List<ItemDrop.ItemData> allItems = __instance.m_inventory.GetAllItems();
            if (allItems.Count == 0 || __instance.InAttack())
            {
                return false;
            }
            float num = 0f;
            if ((bool)targetCreature)
            {
                float radius = targetCreature.GetRadius();
                num = Vector3.Distance(targetCreature.transform.position, __instance.transform.position) - radius;
            }
            else if ((bool)targetStatic)
            {
                num = Vector3.Distance(targetStatic.transform.position, __instance.transform.position);
            }
            float time = Time.time;
            __instance.IsFlying();
            __instance.IsSwimming();
            Humanoid.optimalWeapons.Clear();
            Humanoid.outofRangeWeapons.Clear();
            Humanoid.allWeapons.Clear();
            foreach (ItemDrop.ItemData item in allItems)
            {
                if (!item.IsWeapon() || !__instance.m_baseAI.CanUseAttack(item) || (IsRangedWeapon(item) && CheckArrows(__instance.m_inventory) == 0))
                {
                    continue;
                }
                if (item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
                {
                    if (num < item.m_shared.m_aiAttackRangeMin)
                    {
                        continue;
                    }
                    Humanoid.allWeapons.Add(item);
                    if ((targetCreature == null && targetStatic == null) || time - item.m_lastAttackTime < item.m_shared.m_aiAttackInterval)
                    {
                        continue;
                    }
                    if (num > item.m_shared.m_aiAttackRange)
                    {
                        Humanoid.outofRangeWeapons.Add(item);
                        continue;
                    }
                    if (item.m_shared.m_aiPrioritized)
                    {
                        __instance.EquipItem(item);
                        return false;
                    }
                    Humanoid.optimalWeapons.Add(item);
                }
                else if (item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt)
                {
                    if (!(hurtFriend == null) && !(time - item.m_lastAttackTime < item.m_shared.m_aiAttackInterval))
                    {
                        if (item.m_shared.m_aiPrioritized)
                        {
                            __instance.EquipItem(item);
                            return false;
                        }
                        Humanoid.optimalWeapons.Add(item);
                    }
                }
                else if (item.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Friend && !(friend == null) && !(time - item.m_lastAttackTime < item.m_shared.m_aiAttackInterval))
                {
                    if (item.m_shared.m_aiPrioritized)
                    {
                        __instance.EquipItem(item);
                        return false;
                    }
                    Humanoid.optimalWeapons.Add(item);
                }
            }
            if (Humanoid.optimalWeapons.Count > 0)
            {
                foreach (ItemDrop.ItemData optimalWeapon in Humanoid.optimalWeapons)
                {
                    if (optimalWeapon.m_shared.m_aiPrioritized)
                    {
                        __instance.EquipItem(optimalWeapon);
                        return false;
                    }
                }
                __instance.EquipItem(Humanoid.optimalWeapons[UnityEngine.Random.Range(0, Humanoid.optimalWeapons.Count)]);
            }
            else if (Humanoid.outofRangeWeapons.Count > 0)
            {
                foreach (ItemDrop.ItemData outofRangeWeapon in Humanoid.outofRangeWeapons)
                {
                    if (outofRangeWeapon.m_shared.m_aiPrioritized)
                    {
                        __instance.EquipItem(outofRangeWeapon);
                        return false;
                    }
                }
                __instance.EquipItem(Humanoid.outofRangeWeapons[UnityEngine.Random.Range(0, Humanoid.outofRangeWeapons.Count)]);
            }
            else if (Humanoid.allWeapons.Count > 0)
            {
                foreach (ItemDrop.ItemData allWeapon in Humanoid.allWeapons)
                {
                    if (allWeapon.m_shared.m_aiPrioritized)
                    {
                        __instance.EquipItem(allWeapon);
                        return false;
                    }
                }
                __instance.EquipItem(Humanoid.allWeapons[UnityEngine.Random.Range(0, Humanoid.allWeapons.Count)]);
            }
            else
            {
                ItemDrop.ItemData currentWeapon = __instance.GetCurrentWeapon();
                __instance.UnequipItem(currentWeapon, triggerEquipEffects: false);
            }


            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Humanoid), "EquipItem")]
        private static void HumanoidNPC_EquipItem_Postfix(Humanoid __instance, ItemDrop.ItemData item, bool triggerEquipEffects = true)
        {
            if (!IsStringEqual(__instance.name, NPCPrefabName, true)) return;

            if (item.m_shared.m_skillType == Skills.SkillType.Bows && item.m_shared.m_aiAttackRange < 10)
            {
                item.m_shared.m_aiAttackRange = 20;
            }
        }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(Humanoid), "OnDamaged")]
        public static void Character_OnDamaged_Prefix(Humanoid __instance, HitData hit)
        {
            //Debug.Log("Character_OnDamaged_Postfix");
            if (NPCCurrentMode != NPCMode.Passive && PlayerNPC)
            {
                //Debug.Log("NPCCurrentMode == NPCMode.Defensive");
                if (__instance == Player.m_localPlayer || __instance is HumanoidNPC)
                {

                    Character attacker = hit.GetAttacker();
                    if (attacker != null && !enemyList.Contains(attacker))
                    {
                        enemyList.Add(attacker);
                    }
                    else
                    {
                    }
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "OnDeath")]
        private static void Character_OnDeath_Prefix(Character __instance)
        {
            if (!PlayerNPC || (NPCCurrentCommandType != NPCCommand.CommandType.CombatAttack && NPCCurrentCommandType != NPCCommand.CommandType.CombatSneakAttack)) return;

            Debug.LogError("Character_OnDeath_Prefix");

            if (NPCCurrentCommand != null && NPCCurrentCommand is AttackAction)
            {
                Debug.LogError("is AttackAction");
                AttackAction action = (AttackAction)NPCCurrentCommand;
                if (IsStringEqual(__instance.gameObject.name, action.TargetName, true) && __instance.m_lastHit != null)
                {
                    Debug.LogError("target killed" + __instance.m_lastHit.ToString());
                    Character attacker = __instance.m_lastHit.GetAttacker();
                    if (attacker != null && attacker.gameObject != null && attacker.gameObject == PlayerNPC)
                    {
                        action.TargetQuantity = Math.Max(action.TargetQuantity - 1, 0);
                        LogInfo($"{action.TargetQuantity} {action.TargetName} remaining to kill");
                    }
                }
            }
        }

        // Damage NPC armor
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Character), "RPC_Damage")]
        private static bool Character_RPC_Damage_Prefix(Character __instance, long sender, HitData hit)
        {
            if (__instance.IsDebugFlying())
            {
                return false;
            }
            if (hit.GetAttacker() == Player.m_localPlayer)
            {
                Game.instance.IncrementPlayerStat(__instance.IsPlayer() ? PlayerStatType.PlayerHits : PlayerStatType.EnemyHits);
                __instance.m_localPlayerHasHit = true;
            }
            if (!__instance.m_nview.IsOwner() || __instance.GetHealth() <= 0f || __instance.IsDead() || __instance.IsTeleporting() || __instance.InCutscene() || (hit.m_dodgeable && __instance.IsDodgeInvincible()))
            {
                return false;
            }
            Character attacker = hit.GetAttacker();
            if ((hit.HaveAttacker() && attacker == null) || (__instance.IsPlayer() && !__instance.IsPVPEnabled() && attacker != null && attacker.IsPlayer() && !hit.m_ignorePVP))
            {
                return false;
            }
            if (attacker != null && !attacker.IsPlayer())
            {
                float difficultyDamageScalePlayer = Game.instance.GetDifficultyDamageScalePlayer(__instance.transform.position);
                hit.ApplyModifier(difficultyDamageScalePlayer);
                hit.ApplyModifier(Game.m_enemyDamageRate);
            }
            __instance.m_seman.OnDamaged(hit, attacker);
            if (__instance.m_baseAI != null && __instance.m_baseAI.IsAggravatable() && !__instance.m_baseAI.IsAggravated() && (bool)attacker && attacker.IsPlayer() && hit.GetTotalDamage() > 0f)
            {
                BaseAI.AggravateAllInArea(__instance.transform.position, 20f, BaseAI.AggravatedReason.Damage);
            }
            if (__instance.m_baseAI != null && !__instance.m_baseAI.IsAlerted() && hit.m_backstabBonus > 1f && Time.time - __instance.m_backstabTime > 300f && (!ZoneSystem.instance.GetGlobalKey(GlobalKeys.PassiveMobs) || !__instance.m_baseAI.CanSeeTarget(attacker)))
            {
                __instance.m_backstabTime = Time.time;
                hit.ApplyModifier(hit.m_backstabBonus);
                __instance.m_backstabHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
            }
            if (__instance.IsStaggering() && !__instance.IsPlayer())
            {
                hit.ApplyModifier(2f);
                __instance.m_critHitEffects.Create(hit.m_point, Quaternion.identity, __instance.transform);
            }
            if (hit.m_blockable && __instance.IsBlocking())
            {
                __instance.BlockAttack(hit, attacker);
            }
            __instance.ApplyPushback(hit);
            if (hit.m_statusEffectHash != 0)
            {
                StatusEffect statusEffect = __instance.m_seman.GetStatusEffect(hit.m_statusEffectHash);
                if (statusEffect == null)
                {
                    statusEffect = __instance.m_seman.AddStatusEffect(hit.m_statusEffectHash, resetTime: false, hit.m_itemLevel, hit.m_skillLevel);
                }
                else
                {
                    statusEffect.ResetTime();
                    statusEffect.SetLevel(hit.m_itemLevel, hit.m_skillLevel);
                }
                if (statusEffect != null && attacker != null)
                {
                    statusEffect.SetAttacker(attacker);
                }
            }
            WeakSpot weakSpot = __instance.GetWeakSpot(hit.m_weakSpot);
            if (weakSpot != null)
            {
                ZLog.Log("HIT Weakspot:" + weakSpot.gameObject.name);
            }
            HitData.DamageModifiers damageModifiers = __instance.GetDamageModifiers(weakSpot);
            hit.ApplyResistance(damageModifiers, out var significantModifier);
            if (__instance.IsPlayer() || __instance is HumanoidNPC)
            {
                float bodyArmor = __instance.GetBodyArmor();
                hit.ApplyArmor(bodyArmor);
                __instance.DamageArmorDurability(hit);
            }
            else if (Game.m_worldLevel > 0)
            {
                hit.ApplyArmor(Game.m_worldLevel * Game.instance.m_worldLevelEnemyBaseAC);
            }
            float poison = hit.m_damage.m_poison;
            float fire = hit.m_damage.m_fire;
            float spirit = hit.m_damage.m_spirit;
            hit.m_damage.m_poison = 0f;
            hit.m_damage.m_fire = 0f;
            hit.m_damage.m_spirit = 0f;
            __instance.ApplyDamage(hit, showDamageText: true, triggerEffects: true, significantModifier);
            __instance.AddFireDamage(fire);
            __instance.AddSpiritDamage(spirit);
            __instance.AddPoisonDamage(poison);
            __instance.AddFrostDamage(hit.m_damage.m_frost);
            __instance.AddLightningDamage(hit.m_damage.m_lightning);

            return false;
        }



        /*
         * 
         * PLAYER STATS MIRRORING
         * 
         */


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




                if (humanoid_PlayerNPC != null && (Math.Abs(humanoid_PlayerNPC.GetMaxHealth() - hp) > 1.5f || Math.Abs(humanoid_PlayerNPC.m_maxStamina - stamina) > 1.5f))
                {
                    humanoid_PlayerNPC.SetMaxHealth(hp);
                    humanoid_PlayerNPC.m_maxStamina = stamina;

                    //instance.AddChatTalk(humanoid_PlayerNPC, humanoid_PlayerNPC.m_name, $"Max Health: {hp.ToString("F1")}\nMax Stamina: {stamina.ToString("F1")}\n\n\n", false);
                    MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, $"{humanoid_PlayerNPC.m_name} stats updated:\n Max Health: {hp.ToString("F1")}\nMax Stamina: {stamina.ToString("F1")}\n\n\n");
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

                if (PlayerNPC)
                {
                    HumanoidNPC humanoidComponent = PlayerNPC.GetComponent<HumanoidNPC>();

                    if (humanoidComponent != null)
                    {
                        humanoidComponent.Heal(num);
                    }
                }
            }

            return false;
        }

        // NPC CROUCHES WHEN PLAYER CROUCHES
        [HarmonyPostfix]
        [HarmonyPatch(typeof(Player), "SetCrouch")]
        private static void Player_SetCrouch_Postfix(Player __instance, bool crouch)
        {
            if (PlayerNPC && humanoid_PlayerNPC && NPCCurrentCommandType == NPCCommand.CommandType.FollowPlayer)
            {
                humanoid_PlayerNPC.SetCrouch(crouch);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Character), "SetWalk")]
        private static void Character_SetWalk_Postfix(Character __instance, bool walk)
        {
            if (__instance is Player)
            {
                if (PlayerNPC && humanoid_PlayerNPC && NPCCurrentCommandType == NPCCommand.CommandType.FollowPlayer)
                {
                    humanoid_PlayerNPC.SetWalk(walk);
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
                FindPlayerNPC();
                if (PlayerNPC)
                {
                    humanoid_PlayerNPC.HideHandItems();
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
                FindPlayerNPC();
                if (PlayerNPC)
                {
                    humanoid_PlayerNPC.ShowHandItems();
                }
            }
        }




        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseAI), "FindRandomStaticTarget")]
        private static bool BaseAI_FindRandomStaticTarget_Prefix(BaseAI __instance, float maxDistance, StaticTarget __result)
        {
            if (!IsStringEqual(__instance.name, NPCPrefabName, true)) return true;

            __result = null;
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(BaseAI), "FindClosestStaticPriorityTarget")]
        private static bool BaseAI_FindClosestStaticPriorityTarget_Prefix(BaseAI __instance, StaticTarget __result)
        {
            if (!IsStringEqual(__instance.name, NPCPrefabName, true)) return true;

            __result = null;
            return false;
        }



        // Inventory transfer hotfix
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
        private static bool InventoryGui_OnSelectedItem_Prefix(InventoryGui __instance, InventoryGrid grid, ItemDrop.ItemData item, Vector2i pos, InventoryGrid.Modifier mod)
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
                if (humanoid_PlayerNPC && __instance.m_currentContainer == humanoid_PlayerNPC.inventoryContainer)
                {
                    //Debug.Log($"interacting w npc inventory");

                    if (__instance.m_dragInventory != null)
                    {
                        if (__instance.m_dragInventory == humanoid_PlayerNPC.m_inventory && __instance.m_containerGrid != grid)
                        {
                            //Debug.Log($"came from NPC inventory");
                            //Debug.Log($"dropped into player inventory");

                            localPlayer.GetInventory().MoveItemToThis(grid.GetInventory(), __instance.m_dragItem);

                            //if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable() && humanoid_PlayerNPC.IsItemEquiped(__instance.m_dragItem))
                            if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable())
                            {
                                humanoid_PlayerNPC.UnequipItem(__instance.m_dragItem);
                                if (item != null && item.IsEquipable())
                                    humanoid_PlayerNPC.UnequipItem(item);
                                //Debug.Log($"NPC unequipping item {__instance.m_dragItem.m_shared.m_name}");
                            }

                            /*if (PlayerNPC)
                                SaveNPCData(PlayerNPC);*/
                        }
                        else if (__instance.m_dragInventory == localPlayer.m_inventory && __instance.m_containerGrid == grid)
                        {
                            //Debug.Log($"came from player inventory");
                            //Debug.Log($"dropped into npc inventory");

                            __instance.m_currentContainer.GetInventory().MoveItemToThis(localPlayer.GetInventory(), __instance.m_dragItem);

                            //if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable() && !humanoid_PlayerNPC.IsItemEquiped(__instance.m_dragItem))
                            if (__instance.m_dragItem != null && __instance.m_dragItem.IsEquipable())
                            {
                                if (Player.m_localPlayer.IsItemEquiped(__instance.m_dragItem) || Player.m_localPlayer.GetCurrentWeapon() == __instance.m_dragItem)
                                {
                                    //Debug.Log($"uneqipping {__instance.m_dragItem.m_shared.m_name} from player");
                                    Player.m_localPlayer.UnequipItem(__instance.m_dragItem);
                                }


                                humanoid_PlayerNPC.EquipItem(__instance.m_dragItem);
                                if (item != null && item.IsEquipable())
                                    humanoid_PlayerNPC.EquipItem(item);
                                //Debug.Log($"NPC equipping item {__instance.m_dragItem.m_shared.m_name}");
                            }

                            /*if (PlayerNPC)
                                SaveNPCData(PlayerNPC);*/
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

                                //LogInfo("Moving from npc/container to player");
                                if (PlayerNPC)
                                {
                                    HumanoidNPC humanoidNPC_component = PlayerNPC.GetComponent<HumanoidNPC>();
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

                                //LogInfo("Moving from player to npc/container");
                                if (PlayerNPC)
                                {
                                    HumanoidNPC humanoidNPC_component = PlayerNPC.GetComponent<HumanoidNPC>();
                                    if (__instance.m_currentContainer.GetInventory() == humanoidNPC_component.m_inventory)
                                    {
                                        if (item.IsEquipable())
                                        {
                                            humanoidNPC_component.EquipItem(item, true);
                                            //humanoidNPC_component.m_inventory.RemoveItem(item);
                                        }
                                    }

                                    if (item != null && localPlayer.IsItemEquiped(item))
                                        humanoid_PlayerNPC.UnequipItem(item);
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
            if (IsLocalSingleplayer() && PlayerNPC)
            {
                /*string text = GetChatInputText();
                LogError($"Just typed {text}");*/
                instance.BrainSendInstruction(PlayerNPC, false);
                if (Player.m_localPlayer)
                    instance.AddChatTalk(Player.m_localPlayer, "NPC", GetChatInputText(), true);
                instance.AddChatTalk(humanoid_PlayerNPC, "NPC", "...", false);
            }

            return false;
        }



        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemDrop), "RPC_RequestOwn")]
        private static bool ItemDrop_RPC_RequestOwn_Prefix(ItemDrop __instance, long uid)
        {
            ZLog.Log("Player " + uid + " wants to pickup " + __instance.gameObject.name + "   im: " + ZDOMan.GetSessionID());
            if (__instance.m_nview.IsOwner())
            {
                __instance.m_nview.GetZDO().SetOwner(uid);
            }
            else if (__instance.m_nview.GetZDO().GetOwner() == uid)
            {
                ZLog.Log("  but they are already the owner");
            }
            else
            {
                ZLog.Log("  but neither I nor the requesting player are the owners");
                //LogError($"input uid: {uid.ToString()}, itemdrop uid {__instance.m_nview.GetZDO().m_uid.ToString()}, player uid {Player.m_localPlayer.GetZDOID().ToString()}");
                __instance.m_nview.GetZDO().SetOwner(uid);
            }

            return false;
        }

    }
}
