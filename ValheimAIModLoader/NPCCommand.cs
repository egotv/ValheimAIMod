﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ValheimAIModLoader
{
    /*public abstract class NPCCommand
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


        public NPCCommand() {
            Type = CommandType.Idle;
            priority = 1;
            expiresAt = -1;
        }

        public NPCCommand(CommandType IType, int Ipriority, int expiresInSeconds, GameObject INPC) { 
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
            *//*if (IParameters != null)
            {
                Parameters = IParameters;
            }*//*
        }

        public abstract bool IsTaskComplete();
        public abstract void Execute();

        *//*public void SetParameter(string key, object value)
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
        }*//*
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
        public int RequiredAmount { get; set; }

        public override bool IsTaskComplete()
        {
            // Check if harvesting condition is met, e.g., resource is within range
            //return Vector3.Distance(npc.transform.position, Resource.transform.position) <= 5f;
            if (humanoidNPC)
            {
                if (humanoidNPC.m_inventory.)
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

        public override bool IsTaskComplete()
        {
            // Check if harvesting condition is met, e.g., resource is within range
            if (Target == null) return true;
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
    }*/
}
