using System;
using UnityEditor;
using UnityEngine;

namespace NicolasMassara.CustomTimerManager.Tests
{
#if UNITY_EDITOR
    
    [AddComponentMenu("NicolasMassara/Custom Timer Manager/Tests/Timer Manager Debug")]
    public class TimerDebug : MonoBehaviour
    {
        public int RunningCount { get; private set; }
        public int CancelCount { get; private set; }
        public int ToAddCount { get; private set; }
        public int ToRemoveCount { get; private set; }

        private void Update()
        {
            RunningCount = TimerManager.RunningCount;
            CancelCount = TimerManager.CancelCount;
            ToAddCount = TimerManager.ToAddCount;
            ToRemoveCount = TimerManager.ToRemoveCount;
        }
    }
    
    [CustomEditor(typeof(TimerDebug))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TimerDebug comp = (TimerDebug)target;
            
            EditorGUILayout.LabelField("Update Count", comp.RunningCount.ToString());
            EditorGUILayout.LabelField("Cancel Count", comp.CancelCount.ToString());
            EditorGUILayout.LabelField("To Add Count", comp.ToAddCount.ToString());
            EditorGUILayout.LabelField("To Remove Count", comp.ToRemoveCount.ToString());
        }
    }
    
#endif
}