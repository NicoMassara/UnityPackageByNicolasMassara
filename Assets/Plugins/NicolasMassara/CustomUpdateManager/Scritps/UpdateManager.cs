using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NicolasMassara.CustomUpdateManager
{
    
    public class UpdateManager : MonoBehaviour
    {
        // ----------------------- Custom Time ---------------------------
        public static class CustomTime
        {
            private static readonly Dictionary<UpdateGroup, TimeChannel> Channels = new();

            public static float GlobalTimeScale = 1;
            public static float GlobalFixedTimeScale = 1;
            
            public static TimeChannel GetChannel(UpdateGroup key)
            {
                if (!Channels.ContainsKey(key))
                    Channels[key] = new TimeChannel();
                return Channels[key];
            }

            public static bool GetIsPausedByChannel(UpdateGroup key)
            {
                return GetChannel(key).IsPaused;
            }

            public static float GetDeltaTimeByChannel(UpdateGroup key)
            {
                return GetChannel(key).DeltaTime;
            }
            
            public static float GetUnscaledDeltaTimeByChannel(UpdateGroup key)
            {
                return GetChannel(key).UnscaledDeltaTime;
            }
            
            public static float GetFixedDeltaTimeByChannel(UpdateGroup key)
            {
                return GetChannel(key).FixedDeltaTime;
            }
            
            public static float GetUnscaledFixedDeltaTimeByChannel(UpdateGroup key)
            {
                return GetChannel(key).UnscaledFixedDeltaTime;
            }

            internal static void UpdateAll(float unscaledDeltaTime)
            {
                foreach (var kv in Channels)
                    kv.Value.Update(unscaledDeltaTime * GlobalTimeScale);
            }
            
            internal static void FixedUpdateAll(float unscaledDeltaTime)
            {
                foreach (var kv in Channels)
                    kv.Value.UpdateFixed(unscaledDeltaTime * GlobalFixedTimeScale);
            }

            public static void SetChannelPaused(UpdateGroup updateGroup, bool isPaused)
            {
                if (updateGroup == UpdateGroup.Always)
                {
                    Debug.LogWarning("Update Group: Always cannot be paused");
                    return;
                }

                GetChannel(updateGroup).SetPaused(isPaused);
            }
            
            public static void SetChannelPaused(UpdateGroup[] updateGroup, bool isPaused)
            {
                for (int i = 0; i < updateGroup.Length; i++)
                {
                    GetChannel(updateGroup[i]).SetPaused(isPaused);
                }
            }
            
            public static void SetChannelTimeScale(UpdateGroup updateGroup, float timeScale)
            {
                if (updateGroup == UpdateGroup.Always)
                {
                    Debug.LogWarning("Update Group: Always cannot be modified");
                    return;
                }
                
                GetChannel(updateGroup).SetTimeScale(timeScale);
            }

            public static void SetChannelTimeScale(UpdateGroup[] updateGroup, float timeScale)
            {
                for (int i = 0; i < updateGroup.Length; i++)
                {
                    GetChannel(updateGroup[i]).SetTimeScale(timeScale);
                }
            }
        }
        public class TimeChannel
        {
            public float DeltaTime { get; private set; }
            public float UnscaledDeltaTime { get; private set; }
            public float FixedDeltaTime { get; private set; }
            public float UnscaledFixedDeltaTime { get; private set; }
            public float TimeScale = 1f;
            public bool IsPaused = false;
            
            public void SetTimeScale(float scale) => TimeScale = Mathf.Max(0f, scale);
            public void SetPaused(bool value) => IsPaused = value;

            public void Update(float unscaledDeltaTime)
            {
                UnscaledDeltaTime =  IsPaused ? 0f : unscaledDeltaTime;
                DeltaTime = IsPaused ? 0f : unscaledDeltaTime * TimeScale;
            }
            
            public void UpdateFixed(float fixedUnscaledDeltaTime)
            {
                UnscaledFixedDeltaTime = IsPaused ? 0f :  fixedUnscaledDeltaTime;
                FixedDeltaTime = IsPaused ? 0f : fixedUnscaledDeltaTime * TimeScale;
            }
        }
        
        // -------------------------------------------------------------
        
        // ----------------------- Interfaces ---------------------------
        
        public interface IBaseUpdatable { }

        public interface IUpdatable : IBaseUpdatable
        {
            public UpdateGroup SelfUpdateGroup { get; }
            public UpdatePriorityGroup SelfPriorityGroup { get; }
            public TickGroup SelfTickGroup { get; }
            
            void ExecuteUpdate(float deltaTime);
        }

        public interface IFixedUpdatable : IBaseUpdatable
        {
            public UpdateGroup FixedSelfUpdateGroup { get; }
            public UpdatePriorityGroup FixedSelfPriorityGroup { get; }
            public TickGroup FixedSelfTickGroup { get; }
            void ExecuteFixedUpdate(float fixedDeltaTime);
        }

        public interface ILateUpdatable : IBaseUpdatable
        {
            public UpdateGroup LateSelfUpdateGroup { get; }
            public UpdatePriorityGroup LateSelfPriorityGroup { get; }
            public TickGroup LateSelfTickGroup { get; }
            void ExecuteLateUpdate(float deltaTime);
        }
        // -------------------------------------------------------------
        
        // ----------------------- Enums ---------------------------
        
        public enum UpdateGroup
        {
            Always,
            Gameplay,
            UI,
            Inputs,
            Camera
        }

        public enum UpdatePriorityGroup
        {
            Critical   = 0,
            High       = 100,
            Normal     = 200,
            Low        = 300,
            Background = 400,  
        }

        public enum TickGroup
        {
            EveryFrame,
            EveryHalfSecond,
            EveryQuarterSecond,
            EveryEightSecond,
            EverySixteenthSecond,
            EveryThirtySecond,
            EverySixtyFourthTarget,
            EverySecond
        }
        // -------------------------------------------------------------
        
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
                TickGroup.EveryHalfSecond       => baseInterval * 2f,
                TickGroup.EveryQuarterSecond    => baseInterval * 4f,
                TickGroup.EveryEightSecond      => baseInterval * 8f,
                TickGroup.EverySixteenthSecond  => baseInterval * 16f,
                TickGroup.EveryThirtySecond => baseInterval * 32f,
                TickGroup.EverySixtyFourthTarget => baseInterval * 64f,
                TickGroup.EverySecond      => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
            };

            return tickValue;
        }

        #endregion
        
        private abstract class UpdateController<T> where T : IBaseUpdatable
        {
            private bool _isUpdating;
            
            private readonly List<T> _toAdd = new List<T>();
            private readonly List<T> _toRemove = new List<T>();
            private readonly List<T> _runningList = new List<T>();
            private readonly Dictionary<UpdatePriorityGroup, List<T>> _runningDic = new Dictionary<UpdatePriorityGroup, List<T>>();
            private readonly Dictionary<T, float> _accumulatedDeltaTimeDic = new Dictionary<T, float>();
            
            // Marcar si la lista ordenada está desactualizada
            private bool _isDirty = true;

            protected int TargetFrameRate { get; private set; }

            public bool IsPaused;
            public int RunningCount => _runningList.Count;

            protected UpdateController(int targetFrameRate)
            {
                TargetFrameRate = targetFrameRate;
            }
            
            public void UpdateComponents()
            {
                ApplyPending();
                
                _isUpdating = true;
                
                if (_isDirty) 
                    UpdateSortedPriorities();

                if (!IsPaused || RunningCount > 0)
                {
                    for (int i = 0; i < _runningList.Count; i++)
                    {
                        UpdateElement(_runningList[i]);
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
            
            protected bool GetDosContainInRunningDic(UpdatePriorityGroup group, out List<T> runningList)
            {
                return _runningDic.TryGetValue(group, out runningList);
            }

            protected void AddToRunningDic(UpdatePriorityGroup group, T value)
            {
                if (GetDosContainInRunningDic(group, out List<T> runningList))
                {
                    runningList = new List<T>();
                    _runningDic.Add(group, runningList);
                }
                
                runningList.Add(value);
                
                _accumulatedDeltaTimeDic.TryAdd(value, 0);
            }

            protected abstract void TryAddToRunningList(T element);

            private void RemoveFromRunningList(T element)
            {
                if (_runningList.Contains(element))
                {
                    _runningList.Remove(element);
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
                else if (!_runningList.Contains(updatable))
                {
                    TryAddToRunningList(updatable);
                    _isDirty = false;
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
                        if (!_runningList.Contains(a))
                        {
                            TryAddToRunningList(a);
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

            protected void UpdateSortedPriorities()
            {
                _runningList.Clear();
                foreach (var priority in _runningDic.Keys.OrderBy(p => (int)p))
                {
                    _runningList.AddRange(_runningDic[priority]);
                }

                _isDirty = false;
            }
        }

        private class UpdatableComponent : UpdateController<IUpdatable>
        {
            public UpdatableComponent(int targetFrameRate) : 
                base(targetFrameRate) { }

            protected override void TryAddToRunningList(IUpdatable element)
            {
                AddToRunningDic(element.SelfPriorityGroup, element);
            }

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
                    var interval = GetTickInterval(element.SelfTickGroup, TargetFrameRate,scaledDeltaTime);
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
            
            protected override void TryAddToRunningList(IFixedUpdatable element)
            {
                AddToRunningDic(element.FixedSelfPriorityGroup, element);
            }

            protected override void UpdateElement(IFixedUpdatable element)
            {
                if(CustomTime.GetChannel(element.FixedSelfUpdateGroup).IsPaused)
                    return;
                
                float scaledDeltaTime = 0;
                
                if (element.FixedSelfTickGroup == TickGroup.EveryFrame)
                {
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.FixedSelfUpdateGroup);
                    element.ExecuteFixedUpdate(scaledDeltaTime);
                }
                else
                {
                    scaledDeltaTime = CustomTime.GetFixedDeltaTimeByChannel(element.FixedSelfUpdateGroup);
                    float interval = 1f / TargetFrameRate;
                    
                    float accumulatedTime = scaledDeltaTime;
                    while (accumulatedTime >= interval)
                    {
                        element.ExecuteFixedUpdate(interval);
                        accumulatedTime -= interval;
                    }
                    
                    if (accumulatedTime > 0f)
                        element.ExecuteFixedUpdate(accumulatedTime);
                }
            }
        }
        private class LateUpdatableComponent : UpdateController<ILateUpdatable>
        {
            public LateUpdatableComponent(int targetFrameRate) : 
                base(targetFrameRate) { }
            
            protected override void TryAddToRunningList(ILateUpdatable element)
            {
                AddToRunningDic(element.LateSelfPriorityGroup, element);
            }

            protected override void UpdateElement(ILateUpdatable element)
            {
                if (CustomTime.GetChannel(element.LateSelfUpdateGroup).IsPaused)
                    return;
                
                float scaledDeltaTime = 0;

                if (element.LateSelfTickGroup == TickGroup.EveryFrame)
                {
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.LateSelfUpdateGroup);
                    element.ExecuteLateUpdate(scaledDeltaTime);
                }
                else
                {
                    float deltaTime = GetAccumulatedDeltaTime(element);
                    scaledDeltaTime = CustomTime.GetDeltaTimeByChannel(element.LateSelfUpdateGroup);
                    var interval = GetTickInterval(element.LateSelfTickGroup, TargetFrameRate,scaledDeltaTime);
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

        public int TargetFrameRate { get;  private set; } = 120;
        public bool IsGlobalPaused { get; set; }
        
        private void Awake()
        {
            // Handle if is placed in a GameObject
            if (_instance == null)
            {
                _instance = this;
            }

            Application.targetFrameRate = TargetFrameRate;
            
            _updatableComponent = new UpdatableComponent(TargetFrameRate);
            _fixedUpdatableComponent = new FixedUpdatableComponent(TargetFrameRate);
            _lateUpdatableComponent = new LateUpdatableComponent(TargetFrameRate);
        }

        #region Frame Rate

        public static void SetTargetFrameRate(int targetFrameRate) =>
            _instance.Internal_SetTargetFrameRate(targetFrameRate);
        
        private void Internal_SetTargetFrameRate(int targetFrameRate)
        {
            Application.targetFrameRate = targetFrameRate;
            _updatableComponent.SetTargetFrameRate(targetFrameRate);
            _fixedUpdatableComponent.SetTargetFrameRate(targetFrameRate);
            _lateUpdatableComponent.SetTargetFrameRate(targetFrameRate);
        }
        
        #endregion



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