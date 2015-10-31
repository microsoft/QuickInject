namespace QuickInject
{
    using System;
    using System.Collections.Generic;

    public class PerThreadLifetimeManager : LifetimeManager
    {
        [ThreadStatic]
        private static Dictionary<Guid, object> values;
        private readonly Guid key;
        
        public PerThreadLifetimeManager()
        {
            this.key = Guid.NewGuid();
        }
        
        public override object GetValue()
        {
            EnsureValues();

            object result;
            values.TryGetValue(this.key, out result);
            return result;
        }
        
        public override void SetValue(object newValue)
        {
            EnsureValues();

            values[this.key] = newValue;
        }
        
        public override void RemoveValue()
        {
        }

        private static void EnsureValues()
        {
            // no need for locking, values is TLS
            if (values == null)
            {
                values = new Dictionary<Guid, object>();
            }
        }
    }
}