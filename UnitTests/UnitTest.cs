using System;

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
        public void ValidateUniqueInstancesOfSameType()
        {
            var container = new QuickInjectContainer();

            var b = container.Resolve<B>();

            Assert.AreNotEqual(b.A1, b.A2);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Cannot construct type: UnitTests.IA")]
        public void ThrowOnUnconstructableTypes()
        {
            var container = new QuickInjectContainer();
            container.Resolve<IA>();
        }

        [TestMethod]
        public void DoNotThrowOnUnconstructableTypeIfLifetimeManagerIsSet()
        {
            var container = new QuickInjectContainer();
            var lifetimeManager = new TestLifetimeManager();
            container.RegisterType<IA>(lifetimeManager);

            var a = new A();

            lifetimeManager.SetValue(a);

            Assert.AreSame(a, container.Resolve<IA>());
        }

        [TestMethod]
        public void ParameterizedCodeProviderReturnsInstanceThroughItsFactory()
        {
            var container = new QuickInjectContainer();
            var lifetimeManager = new TestLifetimeManager();

            container.RegisterType<C>(lifetimeManager);
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetACodeProvider()));

            var ia = container.Resolve<IA>();

            Assert.AreSame((lifetimeManager.GetValue() as C).PropToVerify, ia);
        }

        [TestMethod]
        public void ParameterizedCodeProviderReturnsInstanceThroughItsFactoryInstance()
        {
            var container = new QuickInjectContainer();

            var c = new C(new B(new A(), new A()), new A());

            var lifetimeManager = new TestLifetimeManager();

            container.RegisterInstance(c);
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetACodeProvider()));

            var ia = container.Resolve<IA>();

            Assert.AreSame(c.PropToVerify, ia);
        }

        [TestMethod]
        public void ParameterizedCodeProviderReturnsInstanceThroughItsFactoryInstanceRecursive()
        {
            var container = new QuickInjectContainer();

            var lifetimeManager = new TestLifetimeManager();

            container.RegisterType<C>(lifetimeManager);

            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetACodeProvider()));
            container.RegisterType<D>(new ParameterizedLambdaExpressionInjectionFactory<C, D>(new GetDCodeProvider()));


            var e = container.Resolve<E>();

            Assert.AreSame((lifetimeManager.GetValue() as C).PropToVerify2, e.D);
        }
    }
}