namespace QuickInject
{
    using System;
    using System.Threading;

    public class SynchronizedTestLifetimeManager : ContextInvariantLifetimeManager, IRequiresRecovery
    {
        private readonly object lockObj = new object();

        private object value;

        public bool RecoverCalled { get; private set; }

        public override object GetValue()
        {
            Monitor.Enter(this.lockObj);
            var result = this.SynchronizedGetValue();

            if (result != null)
            {
                Monitor.Exit(this.lockObj);
            }

            return result;
        }

        public override void SetValue(object newValue)
        {
            this.SynchronizedSetValue(newValue);
            this.TryExit();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        public override void RemoveValue()
        {
            this.Dispose();
        }

        public void Recover()
        {
            this.TryExit();
            this.RecoverCalled = true;
        }

        protected object SynchronizedGetValue()
        {
            return this.value;
        }

        protected void SynchronizedSetValue(object newValue)
        {
            this.value = newValue;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.value != null)
            {
                var disposable = this.value as IDisposable;
                disposable?.Dispose();

                this.value = null;
            }
        }

        private void TryExit()
        {
            // Prevent first chance exception when abandoning a lock that has not been entered
            if (Monitor.IsEntered(this.lockObj))
            {
                try
                {
                    Monitor.Exit(this.lockObj);
                }
                catch (SynchronizationLockException)
                {
                    // Noop here - we don't hold the lock and that's ok.
                }
            }
        }
    }
}