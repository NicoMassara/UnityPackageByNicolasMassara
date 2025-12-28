using System;
using NicolasMassara.CustomUpdateManager;
using UnityEngine;

namespace Plugins.NicolasMassara.CustomUpdateManager.Test
{
#if UNITY_EDITOR
    [AddComponentMenu("NicolasMassara/CustomUpdateManager/Test/Speed Controller")]
    public class SpeedController : MonoBehaviour
    {
        [Header("Speed")]
        [Range(1,500)]
        [SerializeField] private float rotationSpeed;
        [SerializeField] private bool isUpdatePaused;

        [Header("Frame Rate")]
        [Range(1, 240)] 
        [SerializeField] private int frameRate = 60;
        
        
        [Header("Update Group")]
        [SerializeField] private UpdateManager.UpdateGroup updateGroup;
        [Range(0, 1)]
        [SerializeField] 
        private float timeScale = 1;
        [SerializeField] private bool isPaused = false;
        
        private bool _hasStarted;
        private int _lastFrameRate;
        private bool _wasPaused;
        private bool _wasUpdatePaused;
        private UpdateManager.UpdateGroup _lastUpdateGroup;
        private float _lastTimeScale;
        public float RotationSpeed => rotationSpeed;

        [Obsolete("Obsolete")]
        public void OnValidate()
        {
            if(_hasStarted == false) return;

            if (_lastFrameRate != frameRate)
            {
                if (frameRate > Screen.currentResolution.refreshRate)
                {
                    frameRate = Screen.currentResolution.refreshRate;
                }

                UpdateManager.SetTargetFrameRate(frameRate);
                _lastFrameRate = frameRate;
            }

            if (_lastUpdateGroup != updateGroup)
            {
                UpdateManager.CustomTime.SetChannelTimeScale(updateGroup, timeScale);
                _lastUpdateGroup = updateGroup;
            }

            if (!Mathf.Approximately(_lastTimeScale, timeScale))
            {
                UpdateManager.CustomTime.SetChannelTimeScale(updateGroup, timeScale);
                _lastTimeScale = timeScale;
            }

            if (_wasPaused != isPaused)
            {
                if (isPaused)
                {
                    UpdateManager.CustomTime.PauseChannel(updateGroup);
                }
                else
                {
                    UpdateManager.CustomTime.ResumeChannel(updateGroup);
                }
                
                _wasPaused = isPaused;
            }
            
            if (_wasUpdatePaused != isUpdatePaused)
            {
                UpdateManager.Instance.IsGlobalPaused = isUpdatePaused;
                _wasUpdatePaused = isUpdatePaused;
            }
        }

        private void Awake()
        {
            UpdateManager.OnRegistered += managedObject =>
            {
                Debug.Log($"Registered UpdateManager: {managedObject}");
            };
        }

        private void Start()
        {
            _lastUpdateGroup = updateGroup;
            _hasStarted = true;
        }
    }
    
#endif
}