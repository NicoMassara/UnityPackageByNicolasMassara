using System;
using UnityEngine;
using UnityEditor;

namespace NicolasMassara.CustomTimerManager.Tests
{
#if UNITY_EDITOR
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
            // Dibuja el inspector normal
            DrawDefaultInspector();

            // Agrega el botón
            TimerManagerTester script = (TimerManagerTester)target;
            if (GUILayout.Button("Add Timer"))
            {
                // Llama al método normalmente
                script.AddTimer();
            }
            
            if (GUILayout.Button("Remove Time"))
            {
                // Llama al método normalmente
                script.RemoveTimer();
            }
            
            if (GUILayout.Button("Pause Timer"))
            {
                // Llama al método normalmente
                script.PauseTimer();
            }
            
            if (GUILayout.Button("Resume Time"))
            {
                // Llama al método normalmente
                script.ResumeTimer();
            }
        }
    }
    
#endif

}