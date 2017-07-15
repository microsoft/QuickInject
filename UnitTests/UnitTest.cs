namespace UnitTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using QuickInject;

    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void SimpleDefaultConstructorTest()
        {
            var container = new QuickInjectContainer();
            var classA = container.Resolve<A>();
            Assert.AreEqual(classA.Value, 42);
        }

        [TestMethod]
        public void SimpleDefaultConstructorWithLifetimeManagerGetValueShortCircuit()
        {
            var container = new QuickInjectContainer();
            var lifetimeManager = new TestLifetimeManager();
            container.RegisterType<A>(lifetimeManager);

            var instance = new A { Value = 43 };

            lifetimeManager.SetValue(instance);

            var classA = container.Resolve<A>();
            Assert.AreEqual(classA.Value, 43);
        }

        [TestMethod]
        public void ValidateSetValueSimpleDefaultConstructor()
        {
            var container = new QuickInjectContainer();
            var lifetimeManager = new TestLifetimeManager();
            container.RegisterType<A>(lifetimeManager);

            container.Resolve<A>(); // side-effect is that lifetimeManager should have the right value

            Assert.AreEqual(42, (lifetimeManager.GetValue() as A).Value);
        }

        [TestMethod]
        public void ValidateSetValueSimpleConstructor()
        {
            var container = new QuickInjectContainer();

            var b = container.Resolve<B>();
            Assert.AreNotEqual(b.A1, b.A2);
        }
    }
}