using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;



namespace ValheimAIModLoader
{
    public enum ActionType
    {
        Follow,
        Harvest,
        Kill,
        Idle
        // Add more action types as needed
    }

    public enum ActionPriority
    {
        Low,
        Medium,
        High
    }

    public class Action
    {
        public ActionType Type { get; }
        public GameObject TargetObject { get; }
        public ActionPriority Priority { get; }
        public float Timestamp { get; }

        public Action(ActionType type, GameObject targetObject, ActionPriority priority)
        {
            Type = type;
            TargetObject = targetObject;
            Priority = priority;
            Timestamp = Time.time;
        }
    }

    public class ActionQueue
    {
        private Stack<(ActionPriority, Action)> actionStack = new Stack<(ActionPriority, Action)>();
        public void Enqueue(ActionPriority priority, Action action)
        {
            actionStack.Push((priority, action));
        }

        public void ExecuteNext()
        {
            if (actionStack.Count > 0)
            {
                (ActionPriority priority, Action action) = GetHighestPriorityAction();
                action();
                actionStack = new Stack<(ActionPriority, Action)>(actionStack);
            }
        }

        public bool HasActions()
        {
            return actionStack.Count > 0;
        }

        public void Clear()
        {
            actionStack.Clear();
        }

        private (ActionPriority, Action) GetHighestPriorityAction()
        {
            (ActionPriority priority, Action action) highestPriorityAction = (ActionPriority.Low, null);

            foreach ((ActionPriority priority, Action action) in actionStack)
            {
                if (priority > highestPriorityAction.priority)
                {
                    highestPriorityAction = (priority, action);
                }
            }

            return highestPriorityAction;
        }
    }
}
