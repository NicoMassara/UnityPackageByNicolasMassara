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
        HalfOfTarget,      // Executes every 1/2 frame
        ThirdTarget,     // Executes every 1/3 frame
        QuarterOfTarget,   // Executes every 1/4 frame
        EightOfTarget,     // Executes every 1/8 frame
        SixteenthOfTarget, // Executes every 1/16 frame
        EverySecond,    // Executes every 1 second
    }

    public class TimerData
    {
        public float Time { get; private set; }
        public UpdateFrequency Frequency { get; private set; }
        private event Action OnEndAction;
        private event Action OnStartAction;

        public TimerData(float time, UpdateFrequency frequency,
            Action onStartAction,
            Action onEndAction)
        {
            Time = time;
            Frequency = frequency;
            this.OnStartAction = onStartAction;
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

        #region Timer

        public class Timer
        {
            
            #region Tools

            private class TimerTools
            {
                public static float GetTickByFrequency(UpdateFrequency group, float frameTime, float targetFrameRate)
                {
                    float targetFPS = targetFrameRate > 0 ? targetFrameRate : (1f / frameTime);
                    float baseFrameTime = 1f / targetFPS;
                    float adaptiveFrameTime = Mathf.Lerp(baseFrameTime, frameTime, 0.2f);

                    return group switch
                    {
                        UpdateFrequency.EveryFrame => adaptiveFrameTime,
                        UpdateFrequency.HalfOfTarget => adaptiveFrameTime * 2f,
                        UpdateFrequency.QuarterOfTarget => adaptiveFrameTime * 4f,
                        UpdateFrequency.EightOfTarget => adaptiveFrameTime * 8f,
                        UpdateFrequency.SixteenthOfTarget => adaptiveFrameTime * 16f,
                        UpdateFrequency.EverySecond => 1f,
                        _ => adaptiveFrameTime
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

            public bool HasEnded { get; private set; }
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

                _elapsedSinceLastTick += deltaTime;
                float interval = TimerTools.GetTickByFrequency(_timerData.Frequency, frameTime, _targetFrameRate);
                
                if (!_hasStarted)
                {
                    _hasStarted = true;
                    _timerData?.TriggerOnStartAction();
                }
                
                if (_elapsedSinceLastTick < interval) 
                    return;
                
                _currentTime -= _elapsedSinceLastTick;
                _elapsedSinceLastTick = 0f;
                
                if (_currentTime <= 0)
                {
                    _timerData?.TriggerOnEndAction();
                    Reset();
                }
                
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
        
        #region ID Generator

        public class GeneratedId
        {
            public ulong Id { get; private set; }
            public bool IsActive => Id > 0;
            private event Action<GeneratedId> _onRelease;

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

        public class RandomIdGenerator
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

        private readonly List<ulong> _cancelAddIds = new List<ulong>();
        private readonly List<TimerManagerData> _running = new List<TimerManagerData>();
        private readonly List<TimerManagerData> _toAdd = new List<TimerManagerData>();
        private readonly List<TimerManagerData> _toRemove = new List<TimerManagerData>();
        private readonly Dictionary<ulong, TimerManagerData> _timerDic = new Dictionary<ulong, TimerManagerData>();

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
                data.Timer.TryRun(deltaTime, frameTime);

                if (data.Timer.HasEnded)
                    _toRemove.Add(data);
            }
        }

        private void ApplyPending()
        {
            if (_toAdd.Count > 0)
            {
                var cancelIds = new HashSet<ulong>(_cancelAddIds);

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
                    _timerFactory.ReturnTimer(data.Timer);
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

        private void ClearInternal()
        {
            foreach (var timer in _running)
                _toRemove.Add(timer);
        }

        #endregion
    }
}
