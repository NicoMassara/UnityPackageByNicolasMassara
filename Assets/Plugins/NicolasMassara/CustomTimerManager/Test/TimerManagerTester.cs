using System;
using UnityEngine;
using UnityEditor;

namespace NicolasMassara.CustomTimerManager.Tests
{
#if UNITY_EDITOR
    [AddComponentMenu("NicolasMassara/Custom Timer Manager/Tests/Timer Manager Tester")]
    public class TimerManagerTester : MonoBehaviour
    {
        [SerializeField] private TimerManagerTestData testMessage;
        [Space] 
        [Tooltip("Change will not apply during runtime")]
        [Range(24, 165)] 
        [SerializeField]
        private int targetFrameRate = 60;
        private TimerManager.GeneratedId _id;
        private bool _canExecute;

        private void Awake()
        {
            _canExecute = true;
            Application.targetFrameRate = targetFrameRate;

            TimerManager.OnAdded += () =>
            {
                Debug.LogFormat("Timer Added");
            };
            
            TimerManager.OnRemoved += () =>
            {
                Debug.LogFormat("Timer Removed");
            };
        }

        public void AddTimer()
        {
            if (_canExecute == false) return;
            
            var timerData = new TimerData(testMessage.TargetTime,
                testMessage.StartAction, testMessage.EndAction,testMessage.Frequency);
            _id = TimerManager.Add(timerData);
        }

        public void RemoveTimer()
        {
            if (_canExecute == false) return;
            
            TimerManager.Remove(_id);
        }

        public void PauseTimer()
        {
            if (_canExecute == false) return;
            
            TimerManager.Pause(_id);
        }

        public void ResumeTimer()
        {
            if (_canExecute == false) return;
            
            TimerManager.Resume(_id);
        }

        public void RestartTimer()
        {
            if (_canExecute == false) return;
            
            TimerManager.Restart(_id);
        }
    }

    [Serializable]
    public class TimerManagerTestData
    {
        [TextArea] [SerializeField]
        private string messageToPrint = "Hello World!";
        [Space]
        [Range(0.1f, 60f)]
        public float TargetTime = 5;
        [Space]
        public UpdateFrequency Frequency;

        public void StartAction()
        {
            if (string.IsNullOrEmpty(messageToPrint))
            {
                messageToPrint = "Hello World!";
            }

            Debug.Log($"Printing Message in {TargetTime} seconds, Frequency set to: {Frequency}");
        }

        public void EndAction()
        {
            Debug.Log($"Message: {messageToPrint} printed in {TargetTime} seconds");
        }
    }
    
#endif
#if UNITY_EDITOR
    
    [CustomEditor(typeof(TimerManagerTester))]
    public class ComponentsNameChangeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            TimerManagerTester script = (TimerManagerTester)target;
            
            if (GUILayout.Button("Add Timer")) script.AddTimer();
            if (GUILayout.Button("Remove Time")) script.RemoveTimer();
            if (GUILayout.Button("Pause Timer")) script.PauseTimer();
            if (GUILayout.Button("Resume Timer")) script.ResumeTimer();
            if (GUILayout.Button("Restart Timer")) script.RestartTimer();
        }
    }
    
#endif

}