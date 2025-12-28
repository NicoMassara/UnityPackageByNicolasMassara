using System;
using System.Collections.Generic;
using NicolasMassara.CustomUpdateManager;
using UnityEditor;
using UnityEngine;

namespace Plugins.NicolasMassara.CustomUpdateManager.Test
{
    [AddComponentMenu("NicolasMassara/CustomUpdateManager/Test/Update Manager Debug")]
    public class UpdateManagerDebug : MonoBehaviour
    {
#if UNITY_EDITOR

        public int UpdateCount { get; private set; }
        public int FixedCount { get; private set; }
        public int LateCount { get; private set; }
        
        private void Update()
        {
            UpdateCount = UpdateManager.UpdateCount;
            FixedCount = UpdateManager.FixedCount;
            LateCount = UpdateManager.LateCount;
        }
        
#endif
    }
    
    
#if UNITY_EDITOR

    [CustomEditor(typeof(UpdateManagerDebug))]
    public class MyComponentEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            UpdateManagerDebug comp = (UpdateManagerDebug)target;
            
            EditorGUILayout.LabelField("Update Count", comp.UpdateCount.ToString());
            EditorGUILayout.LabelField("Fixed Count", comp.FixedCount.ToString());
            EditorGUILayout.LabelField("Late Count", comp.LateCount.ToString());
        }
    }
    
#endif
}
