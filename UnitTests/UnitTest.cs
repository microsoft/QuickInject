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
            var classA = container.Resolve<ClassA>();
            Assert.AreEqual(classA.Value, 42);
        }

        [TestMethod]
        public void SimpleDefaultConstructorWithLifetimeManagerGetValueShortCircuit()
        {
            var container = new QuickInjectContainer();
            var lifetimeManager = new TestLifetimeManager();
            container.RegisterType<ClassA>(lifetimeManager);

            var instance = new ClassA { Value = 43 };

            lifetimeManager.SetValue(instance);

            var classA = container.Resolve<ClassA>();
            Assert.AreEqual(classA.Value, 43);
        }

        [TestMethod]
        public void ValidateSetValueSimpleDefaultConstructor()
        {
            var container = new QuickInjectContainer();
            var lifetimeManager = new TestLifetimeManager();
            container.RegisterType<ClassA>(lifetimeManager);

            container.Resolve<ClassA>(); // side-effect is that lifetimeManager should have the right value

            Assert.AreEqual(42, (lifetimeManager.GetValue() as ClassA).Value);
        }
    }
}