namespace UnitTests
{
    using QuickInject;

    internal sealed class TestLifetimeManager : LifetimeManager
    {
        private object instance;

        public override object GetValue()
        {
            return this.instance;
        }

        public override object GetValue(object resolutionContext)
        {
            return this.instance;
        }

        public override void SetValue(object newValue)
        {
            this.instance = newValue;
        }

        public override void SetValue(object resolutionContext, object newValue)
        {
            this.instance = newValue;
        }

        public override void RemoveValue()
        {
        }

        public override void RemoveValue(object resolutionContext)
        {
        }
    }
}