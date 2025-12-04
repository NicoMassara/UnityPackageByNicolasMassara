using UnityEngine;

namespace NicolasMassara.CustomUpdateManager
{
    
    public abstract class ManagedBehavior : MonoBehaviour, IManagedObject
    {
        protected virtual void OnEnable()
        {
            this.RegisterInManager();
        }

        protected virtual void OnDisable()
        {
            this.UnregisterInManager();
        }
    }
}