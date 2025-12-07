using NicolasMassara.CustomUpdateManager;

namespace Plugins.NicolasMassara.CustomUpdateManager.Test
{
    public class UpdateMangerTest : ManagedBehavior, 
        UpdateManager.IUpdatable
    {
        public UpdateManager.UpdateGroup SelfUpdateGroup { get; } = 
            UpdateManager.UpdateGroup.Always;

        public UpdateManager.UpdatePriorityGroup SelfPriorityGroup { get; } =
            UpdateManager.UpdatePriorityGroup.Critical;

        public UpdateManager.TickGroup SelfTickGroup { get; } = 
            UpdateManager.TickGroup.EveryFrame;

        public void ExecuteUpdate(float deltaTime)
        {
            //Debug.Log($"Update from MonoBehavior class, At->{Time.realtimeSinceStartup}");
        }
        
        public void ExecuteFixedUpdate(float fixedDeltaTime)
        {
            //Debug.Log($"Fixed from MonoBehavior class, At->{Time.realtimeSinceStartup}");
        }

        
        public void ExecuteLateUpdate(float deltaTime)
        {
            //Debug.Log($"Late from MonoBehavior class, At->{Time.realtimeSinceStartup}");
        }
        
    }
}