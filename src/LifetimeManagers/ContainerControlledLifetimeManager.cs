namespace QuickInject
{
    using System;

    public class ContainerControlledLifetimeManager : SynchronizedLifetimeManager, IDisposable
    {
        private object value;

        protected override object SynchronizedGetValue()
        {
            return this.value;
        }
        
        protected override void SynchronizedSetValue(object newValue)
        {
            this.value = newValue;
        }
        
        public override void RemoveValue()
        {
            this.Dispose();
        }
        
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this); // shut FxCop up
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (this.value != null)
            {
                if (this.value is IDisposable)
                {
                    ((IDisposable)this.value).Dispose();
                }
                this.value = null;
            }
        }
    }
}