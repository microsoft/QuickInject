namespace QuickInject
{
    using System;

    public class ExternallyControlledLifetimeManager : LifetimeManager
    {
        private WeakReference value = new WeakReference(null);
        
        public override object GetValue()
        {
            return this.value.Target;
        }
        
        public override void SetValue(object newValue)
        {
            this.value = new WeakReference(newValue);
        }
        
        public override void RemoveValue()
        {
            // Deliberate Noop - we don't own this instance after all.
        }
    }
}