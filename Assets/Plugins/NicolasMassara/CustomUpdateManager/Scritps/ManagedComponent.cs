using System;


namespace NicolasMassara.CustomUpdateManager
{
    /// <summary>
    /// This class allows to update a non MonoBehavior class
    /// </summary>
    public abstract class ManagedComponent : IManagedObject, IDisposable
    {
        private bool _isRegistered;
        private bool _disposed;
        
        protected void Initialize()
        {
            if (!_isRegistered)
            {
                this.RegisterInManager();
                _isRegistered = true;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing && _isRegistered)
            {
                this.UnregisterInManager();
            }
            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}