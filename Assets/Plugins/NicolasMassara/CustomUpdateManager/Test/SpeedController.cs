using NicolasMassara.CustomUpdateManager;
using UnityEngine;

namespace Plugins.NicolasMassara.CustomUpdateManager.Test
{
#if UNITY_EDITOR
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

        public void OnValidate()
        {
            if(_hasStarted == false) return;

            if (_lastFrameRate != frameRate)
            {
                UpdateManager.Instance.SetTargetFrameRate(frameRate);
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
                UpdateManager.CustomTime.SetChannelPaused(updateGroup, isPaused);
                _wasPaused = isPaused;
            }
            
            if (_wasUpdatePaused != isUpdatePaused)
            {
                UpdateManager.Instance.IsGlobalPaused = isUpdatePaused;
                _wasUpdatePaused = isUpdatePaused;
            }
        }
        
        private void Start()
        {
            _hasStarted = true;
        }
    }
    
#endif
}