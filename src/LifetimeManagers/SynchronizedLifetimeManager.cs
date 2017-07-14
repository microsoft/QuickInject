namespace QuickInject
{
    using System.Threading;

    public abstract class SynchronizedLifetimeManager : ContextInvariantLifetimeManager, IRequiresRecovery
    {
        private readonly object lockObj = new object();

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

        public override void RemoveValue()
        {
        }

        public void Recover()
        {
            this.TryExit();
        }

        protected abstract object SynchronizedGetValue();

        protected abstract void SynchronizedSetValue(object newValue);

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