using System;
using System.Collections.Generic;
using NicolasMassara.TimedActionManager;
using UnityEngine;

namespace NicolasMassara.CustomActionManager
{
    public class LinkedActionManager : MonoBehaviour
    {
        //====================================================
        //                       SINGLETON
        //====================================================
        
        public static LinkedActionManager Instance =>  _instance != null ? _instance : (_instance = CreateInstance());
        private static LinkedActionManager _instance;
        
        private static LinkedActionManager CreateInstance()
        {
            var gameObject = new GameObject(nameof(LinkedActionManager))
            {
                hideFlags = HideFlags.DontSave,
            };
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<LinkedActionManager>();
        }
        
        #region ID Generator

        public class GeneratedId
        {
            public ulong Id { get; private set; }
            public bool IsActive => Id > 0;
            private event Action<GeneratedId> _onRelease;

            public GeneratedId()
            {
            }

            public GeneratedId(ulong id, Action<GeneratedId> onRelease)
            {
                Id = id;
                _onRelease = onRelease;
            }

            public void Release()
            {
                _onRelease?.Invoke(this);
            }

            public void Reset()
            {
                Id = 0;
            }
        }

        private class RandomIdGenerator
        {
            private const ulong NullId = 0; // Default ID used as a null
            
            private readonly HashSet<ulong> _inUseId = new HashSet<ulong>(); // In Used ID List 
            private readonly System.Random _random = new System.Random();
            
            /// <summary>
            /// Generates a random GeneratedId
            /// That contains an ulong used as the ID
            /// </summary>
            /// <returns></returns>
            public GeneratedId Generate()
            {
                ulong value;
                int attempts = 0;

                do
                {
                    value = NextUlong();
                    attempts++;

                    if (attempts > 100)
                    {
                        break;
                    }

                } while (_inUseId.Contains(value));

                _inUseId.Add(value);
                
                var generatedId = new GeneratedId(value,Release);
                
                return generatedId;
            }
            
            private void Release(GeneratedId idData)
            {
                _inUseId.Remove(idData.Id);
                idData.Reset();
            }
            
            private ulong NextUlong()
            {
                ulong value;

                do
                {
                    byte[] bytes = new byte[8];
                    _random.NextBytes(bytes);
                    value = BitConverter.ToUInt64(bytes, 0);

                } while (value == NullId);

                return value;
            }
        }

        #endregion
        
        #region Action Factory

        public class ActionFactory
        {
            private readonly List<ActionQueueRunner> _availableQueue = new List<ActionQueueRunner>();
            private int AvailableCount => _availableQueue.Count;

            public ActionFactory(int initializeCount = 10)
            {
                for (int i = 0; i < initializeCount; i++)
                    _availableQueue.Add(new ActionQueueRunner());
            }

            public ActionQueueRunner GetActionQueue()
            {
                if (AvailableCount == 0) return new ActionQueueRunner();

                var timer = _availableQueue[0];
                _availableQueue.RemoveAt(0);
                return timer;
            }

            public void ReturnActionQueue(ActionQueueRunner actionQueue)
            {
                actionQueue.ResetValues();
                _availableQueue.Add(actionQueue);
            }
        }

        #endregion

        #region QueueRunner

        public class ActionQueueRunner
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
            
            private IQueueAction _current = null;
            private bool _isPaused = false;
            private float _elapsedSinceLastTick = 0;
            private int _targetFrameRate = -1;
            private PriorityTick _priority = PriorityTick.High;
            private ActionStatus _status = ActionStatus.Idle;
            public bool IsRunning => _status == ActionStatus.Running;
            public bool HasFinished => _status == ActionStatus.Success || _status == ActionStatus.Failure;
            
            #region Setters

            public ActionQueueRunner AddAction(IQueueAction data)
            {
                _current = data;
                
                return this;
            }
            
            public ActionQueueRunner SetTargetFrameRate(int targetFrameRate)
            {
                _targetFrameRate = targetFrameRate;
                return this;
            }

            public ActionQueueRunner SetPriority(PriorityTick priority)
            {
                _priority = priority;
                return this;
            }

            #endregion

            #region Actions

            public void Pause() => _isPaused = true;
            public void Resume() => _isPaused = false;

            public void Interrupt()
            {
                _current?.OnInterrupt();
                _current = null;
                _status = ActionStatus.Success;
            }

            #endregion
            
            public void Execute(float deltaTime, float frameTime)
            {
                if(_isPaused) return;
                
                if (_current != null)
                {
                    _status = ActionStatus.Running;
                    
                    _elapsedSinceLastTick += deltaTime;
                    float interval = TimerTools.GetPriorityTick(_priority, frameTime, _targetFrameRate);

                    while (_elapsedSinceLastTick >= interval)
                    {
                        
                        if (_current.OnUpdate(interval) == ActionStatus.Success)
                        {
                            _elapsedSinceLastTick = 0;
                            _status = ActionStatus.Idle;
                            break;
                        }
                        
                        _elapsedSinceLastTick -= interval;
                    }
                }
                else
                {
                    _status = ActionStatus.Success;
                }
            }

            public void ResetValues()
            {
                _current = null;
                _isPaused = false;
                _status = ActionStatus.Idle;
                _elapsedSinceLastTick = 0;
            }
        }

        #endregion
        
        private readonly ActionFactory _actionFactory = new ActionFactory();
        private readonly RandomIdGenerator _idStorage = new RandomIdGenerator();

        private readonly List<ulong> _cancelIds = new List<ulong>();
        private readonly List<ActionQueueData> _running = new List<ActionQueueData>();
        private readonly List<ActionQueueData> _toAdd = new List<ActionQueueData>();
        private readonly List<ActionQueueData> _toRemove = new List<ActionQueueData>();
        private readonly Dictionary<ulong, ActionQueueData> _idsDic = new Dictionary<ulong, ActionQueueData>();
        private int RunningCount => _running.Count;
        
        private class ActionQueueData
        {
            public ActionQueueRunner ActionQueue;
            public GeneratedId ExternalId;
        }

        #region Action Logic

        private void Update()
        {
            ApplyPending();
            
            if (_running.Count == 0) return;
            
            foreach (var data in _running)
            {
                data.ActionQueue.Execute(Time.deltaTime, Time.unscaledDeltaTime);

                if (data.ActionQueue.HasFinished)
                {
                    _toRemove.Add(data);
                }
            }
            
            ApplyPending();
        }
        

        private void ApplyPending()
        {
            if (_toAdd.Count > 0)
            {
                var cancelIds = new HashSet<ulong>(_cancelIds);
                
                foreach (var data in _toAdd)
                {
                    if (cancelIds.Contains(data.ExternalId.Id))
                    {
                        data.ExternalId.Release();
                        continue;
                    }
                    
                    _running.Add(data);
                    _idsDic.Add(data.ExternalId.Id, data);
                }
            
                _toAdd.Clear();
            }
            
            if (_toRemove.Count > 0)
            {
                foreach (var data in _toRemove)
                {
                    _running.Remove(data);
                    _idsDic.Remove(data.ExternalId.Id); ;
                    _actionFactory.ReturnActionQueue(data.ActionQueue);
                    data.ExternalId.Release();
                }
            
                _toRemove.Clear();
            }
        }

        #endregion
        
        #region Public API

        #region ActionQueue Settings
        public static bool Pause(GeneratedId id) => Instance.PauseInternal(id);
        public static bool Resume(GeneratedId id) => Instance.ResumeInternal(id);

        #endregion

        public static ActionQueueRunner GetActionQueue(GeneratedId id) => Instance.GetActionQueueInternal(id);
        public static GeneratedId Add(IQueueAction queueData, PriorityTick priority = PriorityTick.High) => Instance.AddInternal(queueData,priority);
        public static bool Remove(GeneratedId id) => Instance.RemoveInternal(id);
        public static void Clear() => Instance.ClearInternal();

        #endregion

        #region Internal API

        #region ActionQueue Settings
        

        private bool PauseInternal(GeneratedId id)
        {
            if(id == null) return false;
            
            if (_idsDic.TryGetValue(id.Id, out var value))
            {
                value.ActionQueue.Pause();
                return true;
            }
            
            return false;
        }
        
        private bool ResumeInternal(GeneratedId id)
        {
            if(id == null) return false;
            
            if (_idsDic.TryGetValue(id.Id, out var value))
            {
                value.ActionQueue.Resume();
                return true;
            }
            
            return false;
        }

        #endregion


        private ActionQueueRunner GetActionQueueInternal(GeneratedId id)
        {
            if(id == null) return null;
            
            return _idsDic.TryGetValue(id.Id, out var value) ? value.ActionQueue : null;
        }
        
        private GeneratedId AddInternal(IQueueAction queueData, PriorityTick priority = PriorityTick.High)
        {
            var generatedId = _idStorage.Generate();
            var action = _actionFactory.GetActionQueue();
            
            action.AddAction(queueData).SetTargetFrameRate(Application.targetFrameRate).SetPriority(priority);
            
            _toAdd.Add(new ActionQueueData {ActionQueue = action, ExternalId = generatedId});
            return generatedId;
        }
        
        private bool RemoveInternal(GeneratedId id)
        {
            if(id == null) return false;
            
            if (_idsDic.TryGetValue(id.Id, out var value))
            {
                value.ActionQueue.Interrupt();
                return true;
            }

            return false;
        }

        private void ClearInternal()
        {
            foreach (var value in _running)
            {
                _toRemove.Add(value);
            }
        }

        #endregion
    }
}