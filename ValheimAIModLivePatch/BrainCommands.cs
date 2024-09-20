using BepInEx;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimAIModLoader
{
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

    public partial class ValheimAIModLivePatch : BaseUnityPlugin
    {
        private void Follow_Start(GameObject target, string NPCDialogueMessage = "Right behind ya!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Follow_Start failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            SetMonsterAIAggravated(thrallAIcomp, false);
            thrallAIcomp.SetFollowTarget(target);

            if (NPCDialogueMessage != "")
                AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.FollowPlayer;
            LogMessage("Follow_Start activated!");
        }

        private void Follow_Stop(string NPCDialogueMessage = "I'm gonna wander off on my own now!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Follow_Stop failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            SetMonsterAIAggravated(thrallAIcomp, false);
            thrallAIcomp.SetFollowTarget(null);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage("Follow_Stop activated!");
        }

        private void Combat_StartAttacking(string EnemyName, string NPCDialogueMessage = "Watch out, here I come!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Combat_StartAttacking failed, PlayerNPC == null");
                return;
            }

            if (NPCCurrentMode == NPCMode.Passive)
                NPCCurrentMode = NPCMode.Defensive;

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            /*GameObject[] allEnemies = FindEnemies();
            GameObject nearestUntamedEnemy = allEnemies
            .Where(enemy => enemy.GetComponent<ThrallAI>() != null && !enemy.GetComponent<ThrallAI>().m_character.m_tamed)
            .OrderBy(enemy => Vector3.Distance(PlayerNPC.transform.position, enemy.transform.position))
            .FirstOrDefault();*/

            CurrentEnemyName = EnemyName;

            GameObject closestEnemy = null;

            if (EnemyName != "")
            {
                LogInfo($"Trying to find enemy {EnemyName}");
                Character character = FindClosestEnemy(humanoidnpc_component.gameObject);
                if (character)
                {
                    thrallAIcomp.SetFollowTarget(null);
                    //thrallAIcomp.SetTarget(character);
                    //thrallAIcomp.m_targetCreature = character;

                    thrallAIcomp.m_targetCreature = null;
                    thrallAIcomp.SetTarget(character);
                    thrallAIcomp.m_updateTargetTimer = 1000000f;
                }
                else
                {
                    LogError("Combat_StartAttacking, findclosestenemy returned null");
                    return;
                }
            }
            else
            {
                LogError("EnemyName was null");
            }



            /*if (closestEnemy != null)
            {
                Character character = closestEnemy.GetComponent<Character>();
                if (character)
                {
                    thrallAIcomp.SetFollowTarget(null);
                    thrallAIcomp.SetTarget(character);
                }
                else
                {
                    thrallAIcomp.SetFollowTarget(closestEnemy);
                }
                
                LogInfo($"Combat_StartAttacking closestEnemy found! " + closestEnemy.name);
            }
            else
            {
                thrallAIcomp.SetFollowTarget(null);
                LogError("Combat_StartAttacking closestEnemy not found!");
            }*/


            //thrallAIcomp.m_alerted = false;
            //thrallAIcomp.m_aggravatable = true;
            //thrallAIcomp.SetHuntPlayer(true);

            if (NPCDialogueMessage != "")
                AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.CombatAttack;
            LogMessage("Combat_StartAttacking activated!");
        }

        private void Combat_StartSneakAttacking(GameObject target, string NPCDialogueMessage = "Sneaking up on em!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Combat_StartSneakAttacking failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            // disregard nearby enemies
            thrallAIcomp.SetFollowTarget(null);
            thrallAIcomp.m_alerted = false;
            thrallAIcomp.m_aggravatable = true;
            thrallAIcomp.SetHuntPlayer(true);
            humanoidnpc_component.SetCrouch(true);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.CombatSneakAttack;
            LogMessage("Combat_StartSneakAttacking activated!");
        }

        private void Combat_StartDefending(GameObject target, string NPCDialogueMessage = "Don't worry, I'm here with you!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Combat_StartDefending failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            // disregard nearby enemies
            thrallAIcomp.SetFollowTarget(null);
            SetMonsterAIAggravated(thrallAIcomp, false);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.CombatDefend;
            LogMessage("Combat_StartDefending activated!");
        }

        private void Combat_StopAttacking(string NPCDialogueMessage = "I'll give em a break!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Combat_StopAttacking failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            // disregard nearby enemies
            thrallAIcomp.SetFollowTarget(null);
            SetMonsterAIAggravated(thrallAIcomp, false);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage("Combat_StopAttacking activated!");
        }

        private void Inventory_DropAll(string NPCDialogueMessage = "Here is all I got!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Inventory_DropAll failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            DropAllItems(humanoidnpc_component);

            //AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            //NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage("Inventory_DropAll activated!");
        }

        private void Inventory_DropItem(string ItemName, string NPCDialogueMessage = "Here is what you asked for!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Inventory_DropItem failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            DropItem(ItemName, humanoidnpc_component);

            //AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            //NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage("Inventory_DropItem activated!");
        }

        private void Inventory_EquipItem(string ItemName, string NPCDialogueMessage = "On it boss!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Inventory_EquipItem failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();


            if (!ItemName.StartsWith("$"))
            {
                ItemName = "$" + ItemName;
            }



            CurrentWeaponName = ItemName;

            EquipItem(ItemName, humanoidnpc_component);

            //AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            //NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage($"Inventory_EquipItem activated! ItemName : {ItemName}");
        }

        private void Harvesting_Start(string ResourceName, string NPCDialogueMessage = "On it boss!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Harvesting_Start failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            CurrentHarvestResourceName = CleanKey(ResourceName);
            LogInfo("trying to harvest resource: " + CurrentHarvestResourceName);

            ResourceNodes = QueryResourceComplete(CurrentHarvestResourceName, false);
            ResourceNodesNamesOnly = ConvertResourcesToNames(ResourceNodes.Values.ToList());
            ResourceNodesOneArray = FlattenListOfLists(ResourceNodesNamesOnly);

            //ResourceName = "Beech";

            if (NPCDialogueMessage != "")
                AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.HarvestResource;
            LogMessage("Harvesting_Start activated!");
        }

        private void Harvesting_Stop(string NPCDialogueMessage = "No more labor!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Harvesting_Stop failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            thrallAIcomp.SetFollowTarget(null);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage("Harvesting_Stop activated!");
        }

        private void Patrol_Start(string NPCDialogueMessage = "I'm keeping guard around here! They know not to try!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Patrol_Start failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            patrol_position = Player.m_localPlayer.transform.position;
            instance.patrol_harvest = true;

            //Vector3 randLocation = GetRandomReachableLocationInRadius(humanoidNPC_component.patrol_position, patrol_radius);

            thrallAIcomp.SetFollowTarget(null);
            SetMonsterAIAggravated(thrallAIcomp, false);
            SetMonsterAIAggravated(thrallAIcomp, true);

            if (NPCDialogueMessage != "")
                AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.PatrolArea;
            LogMessage("Patrol_Start activated!");
        }

        private void Patrol_Stop(string NPCDialogueMessage = "My job is done here!")
        {
            if (PlayerNPC == null)
            {
                LogError("NPC command Patrol_Stop failed, PlayerNPC == null");
                return;
            }

            ThrallAI thrallAIcomp = PlayerNPC.GetComponent<ThrallAI>();
            HumanoidNPC humanoidnpc_component = PlayerNPC.GetComponent<HumanoidNPC>();

            thrallAIcomp.SetFollowTarget(null);

            AddChatTalk(humanoidnpc_component, "NPC", NPCDialogueMessage);

            NPCCurrentCommandType = NPCCommand.CommandType.Idle;
            LogMessage("Patrol_Stop activated!");
        }
    }
}
