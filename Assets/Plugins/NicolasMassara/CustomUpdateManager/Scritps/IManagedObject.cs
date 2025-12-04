namespace NicolasMassara.CustomUpdateManager
{
    public interface IManagedObject
    {
        
    }

    public static class ManagedObjectExtensions
    {
        public static void RegisterInManager(this IManagedObject element)
        {
            UpdateManager.Instance.Register(element);
        }

        public static void UnregisterInManager(this IManagedObject element)
        {
            UpdateManager.Instance.Unregister(element);
        }
    }
}