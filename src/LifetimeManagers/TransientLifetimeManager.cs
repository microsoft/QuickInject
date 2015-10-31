namespace QuickInject
{
    public sealed class TransientLifetimeManager : LifetimeManager
    {
        public override object GetValue()
        {
            return null;
        }

        public override void SetValue(object newValue)
        {
        }

        public override void RemoveValue()
        {
        }
    }
}