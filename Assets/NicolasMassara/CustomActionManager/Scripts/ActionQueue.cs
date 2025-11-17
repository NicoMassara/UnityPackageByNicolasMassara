using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NicolasMassara.TimedActionManager;
using UnityEngine;

namespace NicolasMassara.CustomActionManager
{
    public class ActionQueue
    {
        private class TimerTools
        {
            public static float GetPriorityTick(PriorityTick group, float frameTime, float targetFrameRate)
            {
                float targetFPS = targetFrameRate > 0 ? targetFrameRate : (1f / frameTime);
                float baseFrameTime = 1f / targetFPS;
                float adaptiveFrameTime = Mathf.Lerp(baseFrameTime, frameTime, 0.2f);

                return group switch
                {
                    PriorityTick.High => adaptiveFrameTime,
                    PriorityTick.MediumHigh => adaptiveFrameTime * 2,
                    PriorityTick.Medium => adaptiveFrameTime * 4,
                    PriorityTick.MediumLow => adaptiveFrameTime * 8,
                    PriorityTick.Low => adaptiveFrameTime * 16,
                    PriorityTick.None => 1f,
                    _ => adaptiveFrameTime
                };
            }
        }
        
        private Queue<IQueueAction> _actions = new Queue<IQueueAction>();
        private Queue<IQueueAction> _urgentActions = new Queue<IQueueAction>();
        private IQueueAction _current = null;
        private bool _isPaused = false;
        private float _elapsedSinceLastTick = 0;
        private int _targetFrameRate = -1;
        private PriorityTick _priority = PriorityTick.High;
        public bool IsRunning => (_actions.Count > 0 || _urgentActions.Count > 0) || _current != null;
        
        #region Add

        public ActionQueue AddAction(IQueueAction data)
        {
            _actions ??= new Queue<IQueueAction>();
            
            _actions.Enqueue(data);

            return this;
        }

        public ActionQueue AddAction(IEnumerable<IQueueAction> actions)
        {
            _actions = ActionQueueTools.CreateQueue(actions);
            
            return this;
        }

        #endregion

        #region Setters

        public ActionQueue SetTargetFrameRate(int targetFrameRate)
        {
            _targetFrameRate = targetFrameRate;
            return this;
        }

        public ActionQueue SetPriority(PriorityTick priority)
        {
            _priority = priority;
            return this;
        }

        #endregion

        #region Actions

        public void Pause() => _isPaused = true;
        public void Resume() => _isPaused = false;
        public void Clear()
        {
            _current?.OnInterrupt();
            _actions.Clear();
            _current = null;
            ResetValues();
        }
        
        public void InterruptAction()
        {
            _current?.OnInterrupt();
            _current = null;
        }
        
        #endregion

        #region Urgent
        
        public void AddUrgentAndInterrupt(IQueueAction action)
        {
            if (IsRunning == false)
            {
                Debug.Log("Queue not running");
                return;
            }

            _current?.OnInterrupt();

            _current = action;

            _current.OnStart();
        }

        public void AddUrgentNext(IEnumerable<IQueueAction> actions)
        {
            if (IsRunning == false)
            {
                Debug.Log("Queue not running");
                return;
            }
            
            _urgentActions = ActionQueueTools.CreateQueue(actions);
        }

        public void AddUrgentNext(IQueueAction action)
        {
            if (IsRunning == false)
            {
                Debug.Log("Queue not running");
                return;
            }
            
            _urgentActions.Enqueue(action);
        }

        #endregion

        public void Execute(float deltaTime, float frameTime)
        {
            if(_isPaused) return;
            
            if (_current == null)
            {
                if (_urgentActions.Count > 0)
                {
                    _current = _urgentActions.Dequeue();
                    _current.OnStart();
                }
                else if (_actions.Count > 0)
                {
                    _current = _actions.Dequeue();
                    _current.OnStart();
                }
                else
                {
                    Debug.Log("Queue is empty");
                }
            }
            else
            {
                _elapsedSinceLastTick += deltaTime;
                float interval = TimerTools.GetPriorityTick(_priority, frameTime, _targetFrameRate);

                while (_elapsedSinceLastTick >= interval)
                {
                    if (_current.OnUpdate(interval) == ActionStatus.Success)
                    {
                        _current = null;
                        _elapsedSinceLastTick = 0;
                        break;
                    }
                    
                    _elapsedSinceLastTick -= interval;
                }
            }
        }

        private void ResetValues()
        {
            _actions.Clear();
            _urgentActions.Clear();
            _isPaused = false;
            _elapsedSinceLastTick = 0;
        }
    }
}