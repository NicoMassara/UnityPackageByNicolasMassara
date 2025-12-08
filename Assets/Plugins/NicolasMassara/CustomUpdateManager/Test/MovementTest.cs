using NicolasMassara.CustomUpdateManager;
using UnityEngine;

namespace Plugins.NicolasMassara.CustomUpdateManager.Test
{
    [AddComponentMenu("NicolasMassara/CustomUpdateManager/Test/Movement")]
    public class MovementTest : ManagedBehavior, UpdateManager.IUpdatable ,UpdateManager.IUpdateConditional
    {
        [SerializeField] private SpeedController speedController;
        [Space]
        [SerializeField] private UpdateManager.UpdateGroup updateGroup;
        [SerializeField] private UpdateManager.TickGroup tickGroup;
        [SerializeField] private UpdateManager.UpdatePriorityGroup priorityGroup;
        [Space] 
        [SerializeField] private bool canUpdate = true;
        
        public UpdateManager.UpdateGroup SelfUpdateGroup => updateGroup;
        public UpdateManager.TickGroup SelfTickGroup => tickGroup;
        public UpdateManager.UpdatePriorityGroup SelfPriorityGroup => priorityGroup;
        
        public void ExecuteUpdate(float deltaTime)
        {
            transform.Rotate(0f, 0f, speedController.RotationSpeed * deltaTime);
        }

        public bool CanUpdate(UpdateManager.UpdateType type)
        {
            return canUpdate;
        }
    }
}