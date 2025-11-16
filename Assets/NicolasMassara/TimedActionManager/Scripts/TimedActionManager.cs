using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NicolasMassara.TimedActionManager
{
    #region External Tools
    public enum PriorityTick
    {
        High,
        MediumHigh,
        Medium,
        MediumLow,
        Low,
        None
    }
    
    public class ActionData
    {
        public float TimeToExecute;
        public Action OnStartAction;
        public Action OnEndAction;
        
        public void ExecuteStartAction()
        {
            OnStartAction?.Invoke();
        }

        public void ExecuteEndAction()
        {
            OnEndAction?.Invoke();
        }
    }
    

    #endregion
    
    public class TimedActionManager : MonoBehaviour
    {
        //====================================================
        //                       SINGLETON
        //====================================================
        
        public static TimedActionManager Instance =>  _instance != null ? _instance : (_instance = CreateInstance());
        private static TimedActionManager _instance;
        
        private static TimedActionManager CreateInstance()
        {
            var gameObject = new GameObject(nameof(TimedActionManager))
            {
                hideFlags = HideFlags.DontSave,
            };
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<TimedActionManager>();
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
            private readonly List<TimedActionQueue> _availableQueue = new List<TimedActionQueue>();
            private int AvailableCount => _availableQueue.Count;

            public ActionFactory(int initializeCount = 10)
            {
                for (int i = 0; i < initializeCount; i++)
                    _availableQueue.Add(new TimedActionQueue());
            }

            public TimedActionQueue GetActionQueue()
            {
                if (AvailableCount == 0) return new TimedActionQueue();

                var timer = _availableQueue[0];
                _availableQueue.RemoveAt(0);
                return timer;
            }

            public void ReturnActionQueue(TimedActionQueue actionQueue)
            {
                actionQueue.Reset();
                _availableQueue.Add(actionQueue);
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
            public TimedActionQueue ActionQueue;
            public GeneratedId ExternalId;
        }

        #region Action Logic

        private void Update()
        {
            ApplyPending();
            
            if (_running.Count == 0) return;
            
            foreach (var data in _running.ToList())
            {
                data.ActionQueue.Run(Time.deltaTime, Time.unscaledDeltaTime);

                if (data.ActionQueue.IsEmpty)
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
                
                foreach (var data in _toAdd.ToList())
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
                foreach (var data in _toRemove.ToList())
                {
                    _running.Remove(data);
                    _idsDic.Remove(data.ExternalId.Id);
                    _actionFactory.ReturnActionQueue(data.ActionQueue);
                    data.ExternalId.Release();
                }
            
                _toRemove.Clear();
            }
        }

        #endregion
        
        #region Public API

        public static GeneratedId Add(ActionData[] queueData, PriorityTick priority = PriorityTick.High) => Instance.AddInternal(queueData,priority);
        public static GeneratedId Add(List<ActionData> queueData, PriorityTick priority = PriorityTick.High) => Instance.AddInternal(queueData,priority);
        public static bool Remove(GeneratedId id) => Instance.RemoveInternal(id);
        public static void Clear() => Instance.ClearInternal();

        #endregion

        #region Internal

        private GeneratedId AddInternal(ActionData[] queueData, PriorityTick priority = PriorityTick.High)
        {
            var generatedId = _idStorage.Generate();
            var action = _actionFactory.GetActionQueue();
            
            action.AddAction(queueData).SetTargetFrameRate(Application.targetFrameRate).SetPriority(priority);
            
            _toAdd.Add(new ActionQueueData {ActionQueue = action, ExternalId = generatedId});
            return generatedId;
        }
        
        private GeneratedId AddInternal(List<ActionData> queueData, PriorityTick priority = PriorityTick.High)
        {
            return AddInternal(queueData.ToArray(), priority);
        }
        
        private bool RemoveInternal(GeneratedId id)
        {
            if (_idsDic.TryGetValue(id.Id, out var value))
            {
                _toRemove.Add(value);
                return true;
            }
            else if (!_cancelIds.Contains(id.Id))
            {
                _cancelIds.Add(id.Id);
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