// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace UnitTests
{
    using System;
    using Microsoft.QuickInject;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            container.SealContainer();

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

            container.SealContainer();

            container.Resolve<A>(); // side-effect is that lifetimeManager should have the right value

            Assert.AreEqual(42, (lifetimeManager.GetValue() as A).Value);
        }

        [TestMethod]
        public void ValidateUniqueInstancesOfSameType()
        {
            var container = new QuickInjectContainer();
            container.SealContainer();

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

            container.SealContainer();

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

            container.SealContainer();

            var ia = container.Resolve<IA>();

            Assert.AreSame((lifetimeManager.GetValue() as C).PropToVerify, ia);
        }

        [TestMethod]
        public void ParameterizedCodeProviderReturnsInstanceThroughItsFactoryInstance()
        {
            var container = new QuickInjectContainer();

            var c = new C(new B(new A(), new A()), new A());

            container.RegisterInstance(c);
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetACodeProvider()));

            container.SealContainer();

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

            container.SealContainer();

            var e = container.Resolve<E>();

            Assert.AreSame((lifetimeManager.GetValue() as C).PropToVerify2, e.D);
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "This should not hang the unit test")]
        public void ExceptionThrowingFactoryMethodDoesNotDeadLock()
        {
            var container = new QuickInjectContainer();

            var lifetimeManager = new SynchronizedTestLifetimeManager();
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new ExceptionThrowingCodeProvider()));
            container.RegisterType<F>(lifetimeManager);

            container.SealContainer();

            try
            {
                container.Resolve<F>();
            }
            finally
            {
                Assert.AreEqual(true, lifetimeManager.RecoverCalled);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(Exception), "Container is sealed and cannot accept new registrations")]
        public void TestSealContainer()
        {
            var container = new QuickInjectContainer();

            var lifetimeManager = new SynchronizedTestLifetimeManager();
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new ExceptionThrowingCodeProvider()));

            container.SealContainer();

            container.RegisterType<F>(lifetimeManager);

            container.Resolve<F>();
        }

        [TestMethod]
        public void TestResolutionContext()
        {
            var container = new QuickInjectContainer();
            container.RegisterTypeAsResolutionContext<A>();

            var context = new A();

            var b = container.Resolve<B>(context);

            Assert.AreSame(b.A1, b.A2);
            Assert.AreSame(b.A1, context);
        }

        [TestMethod]
        public void ChildContainerOverridingARegistrationGetsADifferentPlan()
        {
            var container = new QuickInjectContainer();
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetACodeProvider()));

            var childContainer = container.CreateChildContainer();

            childContainer.RegisterType<A, A2>();
            var b = container.Resolve<B>();

            var b2 = childContainer.Resolve<B>();

            Assert.AreEqual(42, b.A1.Value);
            Assert.AreEqual(44, b2.A1.Value);
        }

        [TestMethod]
        public void ChildContainerOverridingARegistrationGetsADifferentPlan2()
        {
            var container = new QuickInjectContainer();
            container.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetACodeProvider()));

            var childContainer = container.CreateChildContainer();

            childContainer.RegisterType<IA>(new ParameterizedLambdaExpressionInjectionFactory<C, IA>(new GetA2CodeProvider()));
            var f = container.Resolve<F>();

            var f2 = childContainer.Resolve<F>();

            Assert.AreEqual(42, f.Value.Value);
            Assert.AreEqual(44, f2.Value.Value);

            container.SealContainer();

            GC.Collect();

            f = container.Resolve<F>();
            f2 = childContainer.Resolve<F>();
            container.SealContainer();

            GC.Collect();

            container.Resolve<F>();
            childContainer.Resolve<F>();

            Assert.AreEqual(42, f.Value.Value);
            Assert.AreEqual(44, f2.Value.Value);
        }

        [TestMethod]
        public void ResolutionMonitorIsCalledForContainer()
        {
            var resolutionMonitor = new AssertingResolutionMonitor(typeof(IA));
            var container = new QuickInjectContainer(new NoOpCompilationMonitor(), resolutionMonitor);
            container.RegisterType<IA, A>();
            container.Resolve<IA>();

            Assert.IsTrue(resolutionMonitor.BeginWasCalled);
            Assert.IsTrue(resolutionMonitor.EndWasCalled);
        }
    }
}