using System;
using System.Collections.Generic;
using UnityEngine;

namespace NicolasMassara.TimedActionManager
{
    public class TimedActionQueue
    {
        
        #region Tools

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

        #endregion
        
        #region Timer

        private class Timer
        {
            private float _currentTime = -1;
            private bool _hasStarted = false;
            
            public event Action OnStart;
            public event Action OnEnd;

            public void Set(float targetTime)
            {
                _currentTime = Mathf.Clamp(targetTime, 0.001f, float.MaxValue);
                _hasStarted = true;
            }

            public void Run(float deltaTime)
            {
                if (_currentTime <= 0) return;
                
                if (_hasStarted)
                {
                    OnStart?.Invoke();
                    _hasStarted = false;
                }
                _currentTime -= deltaTime;

                if (_currentTime <= 0)
                {
                    OnEnd?.Invoke();
                    Reset();
                }
            }

            private void Reset()
            {
                _currentTime = -1;
            }
        }

        #endregion
        
        private readonly Timer _executeTimer = new Timer();
        private readonly Queue<ActionData> _actionQueue = new Queue<ActionData>();
        private ActionData _currentAction;
        private bool _isRunning;
        private float _elapsedSinceLastTick = 0;
        private PriorityTick _priority;
        
        private int _targetFrameRate;
        private int _actionsCompleted;
        public bool IsEmpty => _actionQueue.Count == 0 && !_isRunning; 
        

        public TimedActionQueue()
        {
            _executeTimer.OnStart += Timer_OnStartHandler;
            _executeTimer.OnEnd += Timer_OnEndHandler;
        }

        public TimedActionQueue AddAction(ActionData[] actionQueue)
        {
            foreach (var action in actionQueue)
            {
                AddAction(action);
            }
            
            Debug.Log($"Actions Added: {actionQueue.Length}");

            return this;
        }
        
        public TimedActionQueue AddAction(ActionData action)
        {
            _actionQueue.Enqueue(action);
            return this;
        }

        public TimedActionQueue SetPriority(PriorityTick priority)
        {
            _priority = priority;
            return this;
        }


        public TimedActionQueue SetTargetFrameRate(int targetFrameRate)
        {
            _targetFrameRate = targetFrameRate;
            return this;
        }

        public void Run(float deltaTime, float frameTime)
        {
            if (_isRunning)
            {
                _elapsedSinceLastTick += deltaTime;
                float interval = TimerTools.GetPriorityTick(_priority, frameTime, _targetFrameRate);
                
                if (_elapsedSinceLastTick < interval) 
                    return;
                
                _executeTimer.Run(_elapsedSinceLastTick);
                _elapsedSinceLastTick = 0f;
            }
            else if (_actionQueue.Count > 0)
            {
                _currentAction = _actionQueue.Dequeue();
                _executeTimer.Set(_currentAction.TimeToExecute);
                _isRunning = true;
            }
        }

        public void Reset()
        {
            _elapsedSinceLastTick = 0;
            _actionsCompleted = 0;
            _isRunning = false;
            _priority = PriorityTick.None;
            _targetFrameRate = 0;
            _actionQueue.Clear();
            _currentAction = null;
        }

        #region Timer Handlers

        private void Timer_OnStartHandler()
        {
            _currentAction.ExecuteStartAction();
        }
        
        private void Timer_OnEndHandler()
        {
            _currentAction.ExecuteEndAction();
            _isRunning = false;
            _currentAction = null;
            _actionsCompleted++;
        }

        #endregion
    }
}