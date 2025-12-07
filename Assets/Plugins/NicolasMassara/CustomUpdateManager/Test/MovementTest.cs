using NicolasMassara.CustomUpdateManager;
using UnityEngine;

namespace Plugins.NicolasMassara.CustomUpdateManager.Test
{
    public class MovementTest : ManagedBehavior, UpdateManager.IUpdatable
    {
        [SerializeField] private SpeedController speedController;
        [SerializeField] private UpdateManager.UpdateGroup updateGroup;
        [SerializeField] private UpdateManager.TickGroup tickGroup;
        [SerializeField] private UpdateManager.UpdatePriorityGroup priorityGroup;
        public UpdateManager.UpdateGroup SelfUpdateGroup => updateGroup;
        public UpdateManager.TickGroup SelfTickGroup => tickGroup;
        public UpdateManager.UpdatePriorityGroup SelfPriorityGroup => priorityGroup;
        
        public void ExecuteUpdate(float deltaTime)
        {
            transform.Rotate(0f, 0f, speedController.RotationSpeed * deltaTime);
        }
    }
}