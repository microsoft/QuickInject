namespace QuickInject
{
    public abstract class LifetimeManager
    {
        public abstract object GetValue();
        
        public abstract void SetValue(object newValue);
        
        public abstract void RemoveValue();
    }
}