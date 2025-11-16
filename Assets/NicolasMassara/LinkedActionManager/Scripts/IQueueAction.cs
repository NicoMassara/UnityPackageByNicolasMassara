namespace NicolasMassara.LinkedActionManager
{
    public interface IQueueAction
    {
        public void OnStart();
        public bool OnUpdate();
    }
    
    public interface IQueueAction<TResult> : IQueueAction
    {
        TResult Result { get; }
    }
}