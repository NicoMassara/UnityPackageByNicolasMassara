using System;
using System.Collections.Generic;
using UnityEngine;

namespace NicolasMassara.CustomUpdateManager
{
    
    public class UpdateManager : MonoBehaviour
    {

        // ----------------------- Singleton ---------------------------
        public static UpdateManager Instance =>  _instance != null ? _instance : (_instance = CreateInstance());
        protected static UpdateManager _instance;
        
        private static UpdateManager CreateInstance()
        {
            var gameObject = new GameObject(nameof(UpdateManager))
            {
                hideFlags = HideFlags.DontSave,
            };
            //Debug.Log($"Singleton Created: {typeof(T)}");
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<UpdateManager>();
        }
        
        // -------------------------------------------------------------
        
        // --------------------- Updatable Components -----------------------
        
        #region Updatable Component Controller

        #region Tools

        private static float GetTickInterval(TickGroup group, int targetFrameRate, float scaledDeltaTime)
        {
            float baseInterval = targetFrameRate > 0
                ? 1f / targetFrameRate
                : scaledDeltaTime;

            float tickValue = group switch
            {
                TickGroup.EveryFrame       => baseInterval,
                TickGroup.HalfTarget       => baseInterval * 2f,
                TickGroup.QuarterTarget    => baseInterval * 4f,
                TickGroup.EightTarget      => baseInterval * 8f,
                TickGroup.SixteenthTarget  => baseInterval * 16f,
                TickGroup.ThirtySecondTarget => baseInterval * 32f,
                TickGroup.SixtyFourthTarget => baseInterval * 64f,
                TickGroup.EverySecond      => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
            };

            return tickValue;
        }

        #endregion
        
        private abstract class UpdateController<T> where T : IBaseUpdatable
        {
            private bool _isUpdating;
            
            private readonly List<T> _running = new List<T>();
            private readonly List<T> _toAdd = new List<T>();
            private readonly List<T> _toRemove = new List<T>();
            private readonly Dictionary<T, float> _accumulatedDeltaTimeDic = new Dictionary<T, float>();

            protected int TargetFrameRate { get; private set; }

            public bool IsPaused;
            public int RunningCount => _running.Count;

            protected UpdateController(int targetFrameRate)
            {
                TargetFrameRate = targetFrameRate;
            }
            
            public void UpdateComponents()
            {
                ApplyPending();
                
                _isUpdating = true;

                if (!IsPaused || RunningCount > 0)
                {
                    for (int i = 0; i < _running.Count; i++)
                    {
                        UpdateElement(_running[i]);
                    }
                }
                
                _isUpdating = false;
                
                ApplyPending();
            }

            protected abstract void UpdateElement(T element);


            public void SetTargetFrameRate(int targetFrameRate)
            {
                TargetFrameRate = targetFrameRate;
            }

            #region Add/Remove

            private void AddToRunningList(T element)
            {
                _accumulatedDeltaTimeDic.TryAdd(element, 0);

                if (_running.Contains(element) == false)
                {
                    _running.Add(element);
                }
            }

            private void RemoveFromRunningList(T element)
            {
                if (_running.Contains(element))
                {
                    _running.Remove(element);
                }

                if (_accumulatedDeltaTimeDic.ContainsKey(element))
                {
                    _accumulatedDeltaTimeDic.Remove(element);
                }
            }

            public void Add(T updatable)
            {
                if (_isUpdating)
                {
                    if (!_toAdd.Contains(updatable))
                    {
                        _toAdd.Add(updatable);
                    }
                }
                else if (!_running.Contains(updatable))
                {
                    AddToRunningList(updatable);
                }
            }
            
            public void Remove(T updatable)
            {
                if (_isUpdating)
                {
                    if (!_toRemove.Contains(updatable))
                    {
                        _toRemove.Add(updatable);
                    }
                }
                else
                { 
                    RemoveFromRunningList(updatable);
                }
            }
            
            private void ApplyPending()
            {
                if (_toAdd.Count > 0)
                {
                    foreach (var a in _toAdd)
                    {
                        if (!_running.Contains(a))
                        {
                            AddToRunningList(a);
                        }
                    }
                    _toAdd.Clear();
                }

                if (_toRemove.Count > 0)
                {
                    foreach (var r in _toRemove)
                    {
                        RemoveFromRunningList(r);
                    }
                    
                    _toRemove.Clear();
                }
            }

            #endregion
            
            #region Delta Time

            protected void SetAccumulatedDeltaTime(T updatable, float deltaTime)
            {
                _accumulatedDeltaTimeDic[updatable] = deltaTime;
            }

            protected float GetAccumulatedDeltaTime(T updatable)
            {
                if (_accumulatedDeltaTimeDic.ContainsKey(updatable) == false)
                {
                    return 0f;
                }

                return _accumulatedDeltaTimeDic[updatable];
            }

            #endregion
        }

        private class UpdatableComponent : UpdateController<IUpdatable>
        {
            public UpdatableComponent(int targetFrameRate) : 
                base(targetFrameRate) { }

            protected override void UpdateElement(IUpdatable element)
            {
                if (CustomTime.GetChannel(element.SelfUpdateGroup).IsPaused)
                    return;
                
                float scaledDeltaTime = 0;

                if (element.SelfTickGroup == TickGroup.EveryFrame)
                {
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.SelfUpdateGroup);
                    element.ExecuteUpdate(scaledDeltaTime);
                }
                else
                {
                    float deltaTime = GetAccumulatedDeltaTime(element);
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.SelfUpdateGroup);
                    var interval = GetTickInterval(element.SelfTickGroup, UpdateManager.TargetFrameRate,scaledDeltaTime);
                    deltaTime += scaledDeltaTime;

                    while (deltaTime >= interval)
                    {
                        element.ExecuteUpdate(interval);
                        deltaTime -= interval;
                    
                        if (deltaTime > interval * 10f)
                            break;
                    }

                    SetAccumulatedDeltaTime(element, deltaTime);
                }
            }
        }
        private class FixedUpdatableComponent : UpdateController<IFixedUpdatable>
        {
            public FixedUpdatableComponent(int targetFrameRate) :
                base(targetFrameRate) { }

            protected override void UpdateElement(IFixedUpdatable element)
            {
                if(CustomTime.GetChannel(element.SelfUpdateGroup).IsPaused)
                    return;
                
                float scaledDeltaTime = CustomTime.GetFixedDeltaTimeByChannel(element.SelfUpdateGroup);
                float interval = 1f / TargetFrameRate;
                float stepTime = Mathf.Min(scaledDeltaTime, interval);

                element.ExecuteFixedUpdate(stepTime);
            }
        }
        private class LateUpdatableComponent : UpdateController<ILateUpdatable>
        {
            public LateUpdatableComponent(int targetFrameRate) : 
                base(targetFrameRate) { }

            protected override void UpdateElement(ILateUpdatable element)
            {
                if (CustomTime.GetChannel(element.SelfUpdateGroup).IsPaused)
                    return;
                
                float scaledDeltaTime = 0;

                if (element.SelfTickGroup == TickGroup.EveryFrame)
                {
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.SelfUpdateGroup);
                    element.ExecuteLateUpdate(scaledDeltaTime);
                }
                else
                {
                    float deltaTime = GetAccumulatedDeltaTime(element);
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.SelfUpdateGroup);
                    var interval = GetTickInterval(element.SelfTickGroup, UpdateManager.TargetFrameRate,scaledDeltaTime);
                    deltaTime += scaledDeltaTime;

                    while (deltaTime >= interval)
                    {
                        element.ExecuteLateUpdate(interval);
                        deltaTime -= interval;
                    
                        if (deltaTime > interval * 10f)
                            break;
                    }

                    SetAccumulatedDeltaTime(element, deltaTime);
                }
            }
        }

        #endregion
        
        private UpdatableComponent _updatableComponent;
        private FixedUpdatableComponent _fixedUpdatableComponent;
        private LateUpdatableComponent _lateUpdatableComponent;
        
        // -------------------------------------------------------------
        
        public const int TargetFrameRate = 120;
        
        public bool IsGlobalPaused { get; set; }
        
        private void Awake()
        {
            Application.targetFrameRate = TargetFrameRate;
            
            _updatableComponent = new UpdatableComponent(TargetFrameRate);
            _fixedUpdatableComponent = new FixedUpdatableComponent(TargetFrameRate);
            _lateUpdatableComponent = new LateUpdatableComponent(TargetFrameRate);
        }

        public void SetTargetFrameRate(int targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
            _updatableComponent.SetTargetFrameRate(targetFrameRate);
            _fixedUpdatableComponent.SetTargetFrameRate(targetFrameRate);
            _lateUpdatableComponent.SetTargetFrameRate(targetFrameRate);
        }

        #region Update Actions

        private void Update()
        {
            CustomTime.UpdateAll(IsGlobalPaused ? 0 : Time.unscaledDeltaTime);
            _updatableComponent.IsPaused = IsGlobalPaused;
            
            if(IsGlobalPaused) return;
            
            _updatableComponent.UpdateComponents();
        }

        private void FixedUpdate()
        {
            CustomTime.FixedUpdateAll(IsGlobalPaused ? 0 : Time.fixedUnscaledDeltaTime);
            _fixedUpdatableComponent.IsPaused = IsGlobalPaused;
            
            if(IsGlobalPaused) return;
            
            _fixedUpdatableComponent.UpdateComponents();
        }
        
        
        private void LateUpdate()
        {
            _lateUpdatableComponent.IsPaused = IsGlobalPaused;
            
            if(IsGlobalPaused) return;
            
            _lateUpdatableComponent.UpdateComponents();
        }

        #endregion

        #region Register/Unregister
        
        public void Register(IManagedObject element)
        {
            if(element == null) return;
            
            if (element is IUpdatable updatable)
            {
                _updatableComponent.Add(updatable);
            }
            if (element is IFixedUpdatable fixedUpdatable)
            {
                _fixedUpdatableComponent.Add(fixedUpdatable);
            }
            if (element is ILateUpdatable lateUpdatable)
            {
                _lateUpdatableComponent.Add(lateUpdatable);
            }
        }

        public void Unregister(IManagedObject element)
        {
            if(element == null) return;
            
            if (element is IUpdatable updatable)
            {
                _updatableComponent.Remove(updatable);
            }
            if (element is IFixedUpdatable fixedUpdatable)
            {
                _fixedUpdatableComponent.Remove(fixedUpdatable);
            }
            if (element is ILateUpdatable lateUpdatable)
            {
                _lateUpdatableComponent.Remove(lateUpdatable);
            }  
        }
        
        #endregion
    }
}