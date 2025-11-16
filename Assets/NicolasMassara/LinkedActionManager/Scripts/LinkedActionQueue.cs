using System.Collections.Generic;
using UnityEngine;

namespace NicolasMassara.LinkedActionManager
{
    public class LinkedActionQueue
    {
        private Queue<IQueueAction> _actionQueue = new Queue<IQueueAction>();
        private IQueueAction _currentAction = null;

        public LinkedActionQueue()
        {
        }

        public LinkedActionQueue(IQueueAction action)
        {
            AddAction(action);
        }
        
        public LinkedActionQueue(IQueueAction[] actions)
        {
            AddAction(actions);
        }

        public void AddAction(IQueueAction action)
        {
            _actionQueue.Enqueue(action);
        }

        public void AddAction(IQueueAction[] actions)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                AddAction(actions[i]);
            }
        }

        public void Execute()
        {
            if (_currentAction == null)
            {
                if (_actionQueue.Count > 0)
                {
                    _currentAction = _actionQueue.Dequeue();
                    _currentAction.OnStart();
                }
                else
                {
                    //Debug.Log("Queue is empty");
                }
            }
            else
            {
                if (_currentAction.OnUpdate())
                {
                    _currentAction = null;
                }
            }
        }
    }
    
}