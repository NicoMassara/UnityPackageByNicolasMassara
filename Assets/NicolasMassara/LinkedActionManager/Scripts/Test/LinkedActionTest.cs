using System;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.LinkedActionManager.Scripts.Test
{
#if UNITY_EDITOR
    public class LinkedActionTest : MonoBehaviour
    {

        private void Start()
        {

        }

        public void RunQueue()
        {

        }

        private void Update()
        {

        }

        public void RemoveQueue()
        {

        }
    }
    
    public class WaitForKeyAction : IQueueAction
    {
        KeyCode key;

        public WaitForKeyAction(KeyCode key)
        {
            this.key = key;
        }

        public void OnStart()
        {
            Debug.Log("Waiting For: " + key);
        }

        public bool OnUpdate()
        {
            return Input.GetKeyDown(key);
        }
    }
    
#endif
    
    
#if UNITY_EDITOR
    
    [CustomEditor(typeof(LinkedActionTest))]
    public class ComponentsNameChangeEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Dibuja el inspector normal
            DrawDefaultInspector();

            // Agrega el botón
            LinkedActionTest script = (LinkedActionTest)target;
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