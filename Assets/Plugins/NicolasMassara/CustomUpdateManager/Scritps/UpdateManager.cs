using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace NicolasMassara.CustomUpdateManager
{
    public class UpdateManager : MonoBehaviour
    {
        // ----------------------- Custom Time ---------------------------

        #region Custom Time
        
        public static class CustomTime
        {
            private static readonly Dictionary<UpdateGroup, TimeChannel> Channels = new();

            public static float GlobalTimeScale = 1;
            public static float GlobalFixedTimeScale = 1;

            public static event Action<UpdateGroup> OnPause;
            public static event Action<UpdateGroup> OnResume;
            
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

            #region Pause / Resume
            
            public static void PauseChannel(UpdateGroup updateGroup)
            {
                if (updateGroup == UpdateGroup.Always)
                {
                    Debug.LogWarning("Update Group 'Always' cannot be paused");
                    return;
                }

                GetChannel(updateGroup).SetPaused(true);
                OnPause?.Invoke(updateGroup);
            }
            
            public static void PauseChannel(UpdateGroup[] updateGroup)
            {
                for (int i = 0; i < updateGroup.Length; i++)
                {
                    PauseChannel(updateGroup[i]);
                }
            }
            
            public static void ResumeChannel(UpdateGroup updateGroup)
            {
                if (updateGroup == UpdateGroup.Always)
                {
                    return;
                }

                GetChannel(updateGroup).SetPaused(false);
                OnResume?.Invoke(updateGroup);
            }
            
            public static void ResumeChannel(UpdateGroup[] updateGroup)
            {
                for (int i = 0; i < updateGroup.Length; i++) 
                    ResumeChannel(updateGroup[i]);
            }
            
            #endregion
            
            public static void SetChannelTimeScale(UpdateGroup updateGroup, float timeScale)
            {
                if (updateGroup == UpdateGroup.Always)
                {
                    Debug.LogWarning("Update Group 'Always' cannot be modified");
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
        
        #endregion
        
        // ----------------------- Base Interfaces ---------------------------
        
        #region Base Interfaces 
        
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
        
        #endregion
        
        
        // ----------------------- Conditional  Interfaces ---------------------------
        
        #region Conditional Interfaces

        public interface IUpdateConditional
        {
           public bool CanUpdate(UpdateType type);
        }
        
        
        
        #endregion 
        
        // ----------------------- Enums ---------------------------
        
        #region Enums
        
        public enum UpdateType
        {
            Update,
            Fixed,
            Late
        }
        
        // Add many groups as you like, but do not remove 'Always'
        public enum UpdateGroup
        {
            Always, // DO NOT REMOVE
            Gameplay,
            UI,
            Inputs,
        }

        // Add many groups as you like
        public enum UpdatePriorityGroup
        {
            Critical   = 0,
            High       = 100,
            Normal     = 200,
            Low        = 300,
            Background = 400,  
        }
        
        
        // Modify it as you please, but you'll need to modify 'GetTickInterval'
        public enum TickGroup
        {
            EveryFrame,
            EveryHalfSecond,
            EveryQuarterSecond,
            EveryEightSecond,
            EverySixteenthSecond,
            EveryThirtySecond,
            EverySixtyFourthSecond,
            EverySecond
        }
        
        #endregion
        
        // ----------------------- Tools ---------------------------
        
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
                TickGroup.EverySixtyFourthSecond => baseInterval * 64f,
                TickGroup.EverySecond      => 1f,
                _ => throw new ArgumentOutOfRangeException(nameof(group), group, null)
            };

            return tickValue;
        }
        
        
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
        
        // --------------------- Updatable Components -----------------------
        
        #region Updatable Component Controller
        
        private abstract class UpdateController<T> where T : IBaseUpdatable
        {
            private bool _isUpdating;
            
            private readonly List<T> _toAdd = new List<T>();
            private readonly List<T> _toRemove = new List<T>();
            private readonly List<T> _runningList = new List<T>();
            private readonly Dictionary<UpdatePriorityGroup, List<T>> _runningDic = new Dictionary<UpdatePriorityGroup, List<T>>();
            private readonly Dictionary<T, float> _accumulatedDeltaTimeDic = new Dictionary<T, float>();
            protected abstract UpdateType SelfUpdateType { get; }
            
            private bool _isDirty = false;

            protected int TargetFrameRate { get; private set; }

            public bool IsPaused;
            public int RunningCount => _runningList.Count;
            
            private event Action<IManagedObject> OnRegistered;
            private event Action<IManagedObject> OnUnregistered;
            
            

            protected UpdateController(int targetFrameRate, 
                Action<IManagedObject> onRegistered, Action<IManagedObject> onUnregistered)
            {
                TargetFrameRate = targetFrameRate;
                OnRegistered = onRegistered;
                OnUnregistered = onUnregistered;
            }
            
            public void UpdateComponents()
            {
                ApplyPending();
                
                _isUpdating = true;

                if (_isDirty) UpdateSortedPriorities();
                

                if (!IsPaused || RunningCount > 0)
                {
                    for (int i = 0; i < _runningList.Count; i++)
                    {
                        var element = _runningList[i];
                        
                        // Check for conditional

                        if (element is IUpdateConditional cond && !cond.CanUpdate(SelfUpdateType))
                            continue;
                        
                        UpdateElement(element);
                    }
                }
                
                _isUpdating = false;
                
                ApplyPending();
            }

            protected abstract void UpdateElement(T element);


            // ReSharper disable once MemberHidesStaticFromOuterClass
            public void SetTargetFrameRate(int targetFrameRate)
            {
                TargetFrameRate = targetFrameRate;
            }

            #region Add/Remove
            
            protected abstract void TryAddToRunningList(T element);

            private void RemoveFromRunningList(T element)
            {
                if (_runningList.Contains(element))
                {
                    _runningList.Remove(element);
                    OnUnregistered?.Invoke((IManagedObject)element);
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

            #region Debug Getters

#if UNITY_EDITOR
            
            public Dictionary<UpdatePriorityGroup, List<T>> GetPriorityRunningDic() => _runningDic;
            public Dictionary<T, float> GetAccumulatedDeltaTimeDic() => _accumulatedDeltaTimeDic;
            
#endif

            #endregion

            #region List / Dic
            protected void AddToRunningDic(UpdatePriorityGroup group, T value)
            {
                if (_runningDic.TryGetValue(group, out var runningList) == false)
                {
                    runningList = new List<T>();
                    _runningDic.Add(group, runningList);
                }

                _isDirty = true;
                
                OnRegistered?.Invoke((IManagedObject)value);
                
                runningList.Add(value);
                
                _accumulatedDeltaTimeDic.TryAdd(value, 0);
            }

            protected void UpdateSortedPriorities()
            {
                _runningList.Clear();
                
                // Ordenamos las prioridades solo si hay cambios
                foreach (var priority in _runningDic.Keys.OrderBy(p => (int)p))
                {
                    var bucket = _runningDic[priority];
                    if (bucket.Count > 0)
                        _runningList.AddRange(bucket);
                }
                
                _isDirty = false;
            }
            
            #endregion
        }

        private class UpdatableComponent : UpdateController<IUpdatable>
        {
            protected override UpdateType SelfUpdateType => UpdateType.Update;

            public UpdatableComponent(int targetFrameRate, Action<IManagedObject> onRegistered, Action<IManagedObject> onUnregistered) 
                : base(targetFrameRate, onRegistered, onUnregistered) { }

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
            protected override UpdateType SelfUpdateType => UpdateType.Fixed;
            
            public FixedUpdatableComponent(int targetFrameRate, Action<IManagedObject> onRegistered, Action<IManagedObject> onUnregistered)
                : base(targetFrameRate, onRegistered, onUnregistered) { }

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
            protected override UpdateType SelfUpdateType => UpdateType.Late;
            
            public LateUpdatableComponent(int targetFrameRate, Action<IManagedObject> onRegistered, Action<IManagedObject> onUnregistered)
                : base(targetFrameRate, onRegistered, onUnregistered) { }

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
        
        public static event Action<IManagedObject> OnRegistered;
        public static event Action<IManagedObject> OnUnregistered;
        //
        public static event Action<IManagedObject> OnFixedRegistered;
        public static event Action<IManagedObject> OnFixedUnregistered;
        //
        public static event Action<IManagedObject> OnLateRegistered;
        public static event Action<IManagedObject> OnLateUnregistered;
        
        private void Awake()
        {
            // Handle if is placed in a GameObject
            if (_instance == null)
            {
                _instance = this;
            }

            Application.targetFrameRate = TargetFrameRate;
            
            _updatableComponent = new UpdatableComponent(TargetFrameRate, OnRegistered, OnUnregistered);
            _fixedUpdatableComponent = new FixedUpdatableComponent(TargetFrameRate, OnFixedRegistered, OnFixedUnregistered);
            _lateUpdatableComponent = new LateUpdatableComponent(TargetFrameRate, OnLateRegistered, OnLateUnregistered);
            
        }
        
            
        #region Count Getters
        
#if UNITY_EDITOR
        public static int UpdateCount => _instance._updatableComponent.RunningCount;
        public static Dictionary<UpdatePriorityGroup, List<IUpdatable>> UpdatePriorityGroups =>
            _instance._updatableComponent.GetPriorityRunningDic();
        public static Dictionary<IUpdatable, float> AccumulatedDeltaTime =>
            _instance._updatableComponent.GetAccumulatedDeltaTimeDic();
        
        public static int FixedCount => _instance._fixedUpdatableComponent.RunningCount;
        public static Dictionary<UpdatePriorityGroup, List<IFixedUpdatable>> FixedPriorityGroups =>
            _instance._fixedUpdatableComponent.GetPriorityRunningDic();
        public static Dictionary<IFixedUpdatable, float> FixedAccumulatedDeltaTime =>
            _instance._fixedUpdatableComponent.GetAccumulatedDeltaTimeDic();
        
        public static int LateCount => _instance._lateUpdatableComponent.RunningCount;
        public static Dictionary<UpdatePriorityGroup, List<ILateUpdatable>> LatePriorityGroups =>
            _instance._lateUpdatableComponent.GetPriorityRunningDic();
        public static Dictionary<ILateUpdatable, float> LateAccumulatedDeltaTime =>
            _instance._lateUpdatableComponent.GetAccumulatedDeltaTimeDic();
        
#endif
        
        #endregion

        #region Frame Rate Setter

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