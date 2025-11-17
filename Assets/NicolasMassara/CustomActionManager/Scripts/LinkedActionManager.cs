using UnityEngine;

namespace NicolasMassara.CustomActionManager
{
    public class LinkedActionManager : MonoBehaviour
    {
        //====================================================
        //                       SINGLETON
        //====================================================
        
        public static LinkedActionManager Instance =>  _instance != null ? _instance : (_instance = CreateInstance());
        private static LinkedActionManager _instance;
        
        private static LinkedActionManager CreateInstance()
        {
            var gameObject = new GameObject(nameof(TimedActionManager))
            {
                hideFlags = HideFlags.DontSave,
            };
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<LinkedActionManager>();
        }
    }
}