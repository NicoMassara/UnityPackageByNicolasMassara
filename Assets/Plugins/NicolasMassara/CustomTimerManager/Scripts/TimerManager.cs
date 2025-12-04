using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NicolasMassara.CustomTimerManager
{
    #region External Tools

    public enum UpdateFrequency
    {
        EveryFrame,     // Executes every frame
        HalfOfTarget,      // Executes every 1/2 of target frame
        ThirdTarget,     // Executes every 1/3 of target frame
        QuarterOfTarget,   // Executes every 1/4 of target frame
        EightOfTarget,     // Executes every 1/8 of target frame
        SixteenthOfTarget, // Executes every 1/16 of target frame
        EverySecond,    // Executes every 1 second
    }

    public class TimerData
    {
        public float Time { get; private set; }
        public UpdateFrequency Frequency { get; private set; }
        private event Action OnEndAction;
        private event Action OnStartAction;

        public TimerData(float time,
            Action onStartAction,
            Action onEndAction, 
            UpdateFrequency frequency = UpdateFrequency.EveryFrame)
        {
            Time = time;
            Frequency = frequency;
            this.OnStartAction = onStartAction;
            this.OnEndAction = onEndAction;
        }
        
        public TimerData(float time,
            Action onEndAction, 
            UpdateFrequency frequency = UpdateFrequency.EveryFrame)
        {
            Time = time;
            Frequency = frequency;
            this.OnEndAction = onEndAction;
        }

        public void TriggerOnEndAction() => OnEndAction?.Invoke();
        public void TriggerOnStartAction() => OnStartAction?.Invoke();
    }

    #endregion

    public class TimerManager : MonoBehaviour
    {
        //====================================================
        //                       SINGLETON
        //====================================================
        public static TimerManager Instance => _instance != null ? _instance : (_instance = CreateInstance());
        protected static TimerManager _instance;

        private static TimerManager CreateInstance()
        {
            var gameObject = new GameObject(nameof(TimerManager))
            {
                hideFlags = HideFlags.DontSave,
            };
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<TimerManager>();
        }

        private void MakeSingleton()
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        //====================================================
        //                       TOOLS
        //====================================================

        #region ID Generator
        public class GeneratedId
        {
            public ushort Id { get; private set; }
            public bool IsActive => Id > 0;
            private event Action<GeneratedId> _onRelease;

            public GeneratedId(ushort id, Action<GeneratedId> onRelease)
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
            private const ushort NullId = 0; // Default ID used as null

            private readonly HashSet<ushort> _inUseId;// IDs currently in use
            private ushort _nextId = 1; // Start from 1 (0 = NullId)

            public RandomIdGenerator(int initialSize = 10)
            {
                initialSize = Mathf.Clamp(initialSize, 0, ushort.MaxValue);
                _inUseId = new HashSet<ushort>(initialSize);
            }

            /// <summary>
            /// Generates an incremental GeneratedId
            /// </summary>
            public GeneratedId Generate()
            {
                if (_inUseId.Count >= ushort.MaxValue - 1)
                    throw new InvalidOperationException("All available IDs are in use.");

                // Find the next free ID
                while (_inUseId.Contains(_nextId) || _nextId == NullId)
                {
                    _nextId++;

                    if (_nextId == ushort.MaxValue)
                        _nextId = 1; // Wrap around if overflow
                }

                ushort value = _nextId;
                _inUseId.Add(value);
                _nextId++;

                return new GeneratedId(value, Release);
            }

            private void Release(GeneratedId idData)
            {
                _inUseId.Remove(idData.Id);
                idData.Reset();
            }
        }
        #endregion
        
        #region Timer

        public class Timer
        {
            #region Tools

            private class TimerTools
            {
                public static float GetTickByFrequency(UpdateFrequency group, float frameTime, float targetFrameRate)
                {
                    float baseFrameTime = targetFrameRate > 0 ? 1f / targetFrameRate : frameTime;

                    return group switch
                    {
                        UpdateFrequency.EveryFrame => frameTime,
                        UpdateFrequency.HalfOfTarget => baseFrameTime * 2f,
                        UpdateFrequency.ThirdTarget => baseFrameTime * 3f,
                        UpdateFrequency.QuarterOfTarget => baseFrameTime * 4f,
                        UpdateFrequency.EightOfTarget => baseFrameTime * 8f,
                        UpdateFrequency.SixteenthOfTarget => baseFrameTime * 16f,
                        UpdateFrequency.EverySecond => 1f,
                        _ => baseFrameTime
                    };
                }
            }

            #endregion

            private TimerData _timerData;
            private bool _canRun;
            private bool _hasStarted = false;
            private float _currentTime = -1;
            private float _targetTime;
            private float _targetFrameRate;
            private float _elapsedSinceLastTick;
            private bool _isPaused;

            public bool HasEnded { get; private set; }
            public bool IsPaused => _isPaused;
            public bool HasStarted => _hasStarted;
            public float CurrentTime => _currentTime;
            public float TargetTime => _targetTime;
            public float CurrentRatio => _currentTime / _targetTime;

            public Timer() { }

            public void SetData(TimerData timerData, float targetFrameRate)
            {
                _timerData = timerData;
                _targetFrameRate = targetFrameRate;

                if (_timerData != null)
                {
                    Set(_timerData.Time);
                    _canRun = true;
                }
                else
                {
                    Debug.LogWarning("TimerData is null. Timer will not run.");
                }
            }

            private void Set(float time)
            {
                _currentTime = Mathf.Clamp(time, 0.001f, float.MaxValue);
                _targetTime = _currentTime;
                HasEnded = false;
            }

            /// <summary>
            /// Updates the timer according to its frequency.
            /// </summary>
            public void TryRun(float deltaTime, float frameTime)
            {
                if (!_canRun) return;

                if (_timerData == null)
                {
                    Debug.LogWarning("TimerData is null. Timer will not run.");
                    return;
                }

                if (!_hasStarted)
                {
                    _hasStarted = true;
                    _timerData?.TriggerOnStartAction();
                }
                //

                if (_timerData?.Frequency == UpdateFrequency.EveryFrame)
                {
                    _currentTime -= deltaTime;
                }
                else
                {
                    _elapsedSinceLastTick += deltaTime;
                    
                    float interval = TimerTools.GetTickByFrequency(_timerData.Frequency, frameTime, _targetFrameRate);
                    
                    while (_elapsedSinceLastTick >= interval)
                    {
                        _elapsedSinceLastTick -= interval;
                        _currentTime -= interval;
                    }
                }

                
                if (_currentTime <= 0)
                {
                    _timerData?.TriggerOnEndAction();
                    Reset();
                }
                
            }

            public void Pause()
            {
                _isPaused = true;
            }

            public void Resume()
            {
                _isPaused = false;
            }

            /// <summary>
            /// Resets all internal timer values.
            /// </summary>
            private void Reset()
            {
                _timerData = null;
                HasEnded = true;
                _canRun = false;
                _elapsedSinceLastTick = 0;
                _targetTime = -1;
                _currentTime = -1;
                _targetFrameRate = 0;
            }
        }

        #endregion

        #region TimerManagerData

        private class TimerManagerData
        {
            public Timer Timer;
            public GeneratedId ExternalId;
        }

        #endregion

        #region TimerFactory

        public class TimerFactory
        {
            private readonly List<Timer> _availableTimers = new List<Timer>();
            private int AvailableCount => _availableTimers.Count;

            public TimerFactory(int initializeCount = 10)
            {
                for (int i = 0; i < initializeCount; i++)
                    _availableTimers.Add(new Timer());
            }

            public Timer GetTimer()
            {
                if (AvailableCount == 0) return new Timer();

                var timer = _availableTimers[0];
                _availableTimers.RemoveAt(0);
                return timer;
            }

            public void ReturnTimer(Timer timer) => _availableTimers.Add(timer);
        }

        #endregion

        private TimerFactory _timerFactory;
        private const int InitialTimerCount = 15;
        private readonly RandomIdGenerator _idStorage = new RandomIdGenerator();
        private float _applicationTargetFrameRate;

        private readonly List<ushort> _cancelAddIds = new List<ushort>();
        private readonly List<TimerManagerData> _running = new List<TimerManagerData>();
        private readonly List<TimerManagerData> _toAdd = new List<TimerManagerData>();
        private readonly List<TimerManagerData> _toRemove = new List<TimerManagerData>();
        private readonly Dictionary<ushort, TimerManagerData> _timerDic = new Dictionary<ushort, TimerManagerData>();

        //====================================================
        //                       COUNTERS
        //====================================================
        public int RunningCount => _running.Count;
        public int CancelCount => _cancelAddIds.Count;
        public int ToAddCount => _toAdd.Count;
        public int ToRemoveCount => _toRemove.Count;

        //====================================================
        //                     INITIALIZE
        //====================================================
        private void Awake()
        {
            if (_instance == null) MakeSingleton();

            _applicationTargetFrameRate = Application.targetFrameRate;
            InitializeTimer();
        }

        private void InitializeTimer() => _timerFactory = new TimerFactory(InitialTimerCount);

        //====================================================
        //                     UPDATE CYCLE
        //====================================================
        private void Update()
        {
            if (_running.Count == 0) return;

            RunTimers();
        }

        private void LateUpdate() => ApplyPending();

        #region Timer Logic

        private void RunTimers()
        {
            float deltaTime = Time.deltaTime;
            float frameTime = Time.unscaledDeltaTime;

            for (int i = 0; i < _running.Count; i++)
            {
                var data = _running[i];
                
                if(data.Timer.IsPaused)
                    continue;
                
                data.Timer.TryRun(deltaTime, frameTime);

                if (data.Timer.HasEnded)
                    _toRemove.Add(data);
            }
        }

        private void ApplyPending()
        {
            if (_toAdd.Count > 0)
            {
                var cancelIds = new HashSet<ushort>(_cancelAddIds);

                foreach (var data in _toAdd)
                {
                    if (cancelIds.Contains(data.ExternalId.Id))
                    {
                        data.ExternalId.Release();
                        continue;
                    }

                    _running.Add(data);
                    _timerDic.Add(data.ExternalId.Id, data);
                }

                _toAdd.Clear();
            }

            if (_toRemove.Count > 0)
            {
                foreach (var data in _toRemove.ToList())
                {
                    _running.Remove(data);
                    _timerDic.Remove(data.ExternalId.Id);
                    data.ExternalId.Reset();
                }

                _toRemove.Clear();
            }
        }

        #endregion

        //====================================================
        //                       PUBLIC API
        //====================================================
        #region Public

        public static GeneratedId Add(TimerData timerData) => Instance.AddInternal(timerData);
        public static bool Remove(GeneratedId generatedId) => Instance.RemoveInternal(generatedId);
        public static bool Pause(GeneratedId generatedId) => Instance.PauseInternal(generatedId);
        public static bool Resume(GeneratedId generatedId) => Instance.ResumeInternal(generatedId);
        public static void Clear() => Instance.ClearInternal();

        #endregion

        //====================================================
        //                    INTERNAL METHODS
        //====================================================
        #region Internal

        private GeneratedId AddInternal(TimerData timerData)
        {
            var generatedId = _idStorage.Generate();
            var timer = _timerFactory.GetTimer();
            timer.SetData(timerData, _applicationTargetFrameRate);

            _toAdd.Add(new TimerManagerData
            {
                Timer = timer,
                ExternalId = generatedId
            });

            return generatedId;
        }

        private bool RemoveInternal(GeneratedId generatedId)
        {
            if(generatedId == null) return false;
            
            if (_timerDic.TryGetValue(generatedId.Id, out var value))
            {
                _toRemove.Add(value);
                return true;
            }

            if (_cancelAddIds.Contains(generatedId.Id))
            {
                _cancelAddIds.Add(generatedId.Id);
                return true;
            }

            return false;
        }
        
        private bool PauseInternal(GeneratedId generatedId)
        {
            if(generatedId == null) return false;
            
            if (_timerDic.TryGetValue(generatedId.Id, out var value))
            {
                value.Timer.Pause();
                return true;
            }

            return false;
        }
        
        private bool ResumeInternal(GeneratedId generatedId)
        {
            if(generatedId == null) return false;
            
            if (_timerDic.TryGetValue(generatedId.Id, out var value))
            {
                value.Timer.Resume();
                return true;
            }

            return false;
        }

        private void ClearInternal()
        {
            foreach (var timer in _running)
                _toRemove.Add(timer);
        }

        #endregion
    }
}
