namespace NicolasMassara.CustomUpdateManager
{
    public interface IBaseUpdatable
    {
        public UpdateGroup SelfUpdateGroup { get; }
        public TickGroup SelfTickGroup { get; }
    }

    public interface IUpdatable : IBaseUpdatable
    {
        void ExecuteUpdate(float deltaTime);
    }

    public interface IFixedUpdatable : IBaseUpdatable
    {
        void ExecuteFixedUpdate(float fixedDeltaTime);
    }

    public interface ILateUpdatable : IBaseUpdatable
    {
        void ExecuteLateUpdate(float deltaTime);
    }
}