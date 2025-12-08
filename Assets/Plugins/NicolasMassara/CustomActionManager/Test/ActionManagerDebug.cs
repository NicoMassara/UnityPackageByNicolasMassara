using System;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.CustomActionManager.Test
{
#if UNITY_EDITOR
    [AddComponentMenu("NicolasMassara/Action Manager/Test/Action Manager Debug")]
    public class ActionManagerDebug : MonoBehaviour
    {
        public int RunningCount { get; private set; }
        public int FixedRunningCount { get; private set; }
        public int LateRunningCount { get; private set; }

        private void Update()
        {
            RunningCount = ActionManager.RunningCount;
            FixedRunningCount = ActionManager.FixedRunningCount;
            LateRunningCount = ActionManager.LateRunningCount;
        }
    }
    
    [CustomEditor(typeof(ActionManagerDebug))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ActionManagerDebug comp = (ActionManagerDebug)target;
            
            EditorGUILayout.LabelField("Update Count", comp.RunningCount.ToString());
            EditorGUILayout.LabelField("Fixed Count", comp.FixedRunningCount.ToString());
            EditorGUILayout.LabelField("Late Count", comp.LateRunningCount.ToString());
        }
    }
#endif
}