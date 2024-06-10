using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    public class NPCCommandQueue
    {
        // All commands for NPC
        public List<NPCCommand> commandList = new List<NPCCommand>();


        public Dictionary<string, NPCCommand> commandDict = new Dictionary<string, NPCCommand>();
        // New commands will override what old commands? new Move order will override/delete any previous Follow order. 
        public Dictionary<string, string[]> NewCommandDeletes = new Dictionary<string, string[]>
        {
            { "move", new string[] { "follow" } },
            { "follow", new string[] { "move"} },
        };

        /*public NPCCommand GetHighestPriorityCommand()
        {
            NPCCommand highest = new NPCCommand();
            foreach (NPCCommand npcCommand in commandList)
            {
                if (npcCommand.expiresAt > Time.time)
                {
                    Debug.Log("deleting expired npcCommand: " + npcCommand.ToString());
                    commandList.Remove(npcCommand);
                    continue;
                }
                if (highest.priority < npcCommand.priority)
                {
                    highest = npcCommand;
                }
            }
            return highest;
        }*/

        public NPCCommand GetBestAction()
        {
            foreach (NPCCommand command in commandList)
            {
                if (command == null) continue;

                if (command.IsConditionMet())
                {
                    continue;
                }
                return command;
            }
            return null;
        }

        public void AddCommand(NPCCommand command)
        {
            RemoveCommandsWithPriority(5);
            commandList.Add(command);
            commandList.Sort((a1, a2) => a2.priority.CompareTo(a1.priority));
            /*if (NewCommandDeletes.ContainsKey(commandname))
            {
                string[] toberemoved_commands = NewCommandDeletes[commandname];
                foreach (string tbr in toberemoved_commands)
                {
                    RemoveCommand(tbr);
                }
            }

            commandDict.Add(commandname, command);*/
        }

        public void RemoveCommandsWithType(NPCCommand.CommandType CommandType)
        {
            List<int> removingIndexes = new List<int>();
            for (int i = 0; i < commandList.Count(); i++)
            {
                if (commandList[i] == null || (commandList[i].Type == CommandType))
                {
                    removingIndexes.Add(i);
                }
            }

            foreach (int r in removingIndexes)
            {
                commandList.RemoveAt(r);
            }
        }

        public void RemoveCommandsWithPriority(int priority)
        {
            List<int> removingIndexes = new List<int>();
            for (int i = 0; i < commandList.Count(); i++)
            {
                if (commandList[i] == null || (commandList[i].priority == priority))
                {
                    removingIndexes.Add(i);
                }
            }

            foreach (int r in removingIndexes)
            {
                commandList.RemoveAt(r);
            }
        }

        public void RemoveCommandsWithTypeAndPriority(NPCCommand.CommandType CommandType, int priority)
        {
            List<int> removingIndexes = new List<int>();
            for (int i = 0; i < commandList.Count(); i++)
            {
                if (commandList[i] == null || (commandList[i].Type == CommandType && commandList[i].priority == priority))
                {
                    removingIndexes.Add(i);
                }
            }

            foreach (int r in removingIndexes)
            {
                commandList.RemoveAt(r);
            }
        }
    }
}
