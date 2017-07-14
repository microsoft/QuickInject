namespace QuickInject
{
    using System;

    public class ContainerControlledLifetimeManager : SynchronizedLifetimeManager, IDisposable
    {
        private object value;

        public override void RemoveValue()
        {
            this.Dispose();
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected override object SynchronizedGetValue()
        {
            return this.value;
        }

        protected override void SynchronizedSetValue(object newValue)
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
    }
}