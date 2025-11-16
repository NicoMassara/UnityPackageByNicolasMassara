using System;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.TimedActionManager.Test
{
#if UNITY_EDITOR
    public class ActionManagerTest : MonoBehaviour
    {
        [Range(1, 1000)] 
        [SerializeField] private int actionCount = 5;
        [Range(0,60)]
        [SerializeField] private float actionExecuteTime = 1.5f;
        [SerializeField] private PriorityTick priority;
        
        private ActionManager.GeneratedId _generatedId = new ActionManager.GeneratedId();
        private bool _hasStarted;
        
        private void Start()
        {
            _hasStarted = true;
        }

        public void RunQueue()
        {
            if(_hasStarted == false) return;

            var actionData = new ActionData[actionCount];
            
            for (int i = 0; i < actionCount; i++)
            {
                var count = i+1;
                actionData[i] = new ActionData
                {
                    TimeToExecute = actionExecuteTime,
                    OnStartAction = () =>
                    {
                        Debug.Log($"Action {count} Started At-> {Time.realtimeSinceStartup}");
                    },
                    OnEndAction = () =>
                    {
                        Debug.Log($"Action {count} Finished At-> {Time.realtimeSinceStartup}");
                    }
                };
            }
            
            _generatedId = ActionManager.Add(actionData, priority);
        }

        public void RemoveQueue()
        {
            if(_hasStarted == false) return;
            
            if (_generatedId.IsActive)
            {
                ActionManager.Remove(_generatedId);
            }
            else
            {
                Debug.Log("No action found");
            }
        }
    }
    
#endif
#if UNITY_EDITOR
    
    [CustomEditor(typeof(ActionManagerTest))]
    public class ComponentsNameChangeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Dibuja el inspector normal
            DrawDefaultInspector();

            // Agrega el botón
            ActionManagerTest script = (ActionManagerTest)target;
            if (GUILayout.Button("Run Queue"))
            {
                // Llama al método normalmente
                script.RunQueue();
            }
            
            if (GUILayout.Button("Remove Queue"))
            {
                // Llama al método normalmente
                script.RemoveQueue();
            }
        }
    }
    
#endif
}