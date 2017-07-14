namespace QuickInject
{
    public abstract class ContextInvariantLifetimeManager : LifetimeManager
    {
        public override object GetValue(object resolutionContext)
        {
            return this.GetValue();
        }

        public override void SetValue(object resolutionContext, object newValue)
        {
            this.SetValue(newValue);
        }

        public override void RemoveValue(object resolutionContext)
        {
            this.RemoveValue();
        }
    }
}