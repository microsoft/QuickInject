// Copyright Notice:
// Some of the unit test code is 
// Copyright (c) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// http://unity.codeplex.com/SourceControl/latest#LICENSE.txt

namespace QuickInjectUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using QuickInject;
    using QuickInject.BuildPlanVisitors;

    public static class Extensions
    {
        public static IQuickInjectContainer RegisterType<T>(this IQuickInjectContainer container)
        {
            return container.RegisterType(null, typeof(T), new TransientLifetimeManager());
        }

        public static IQuickInjectContainer RegisterType<TFrom, TTo>(this IQuickInjectContainer container)
        {
            return container.RegisterType(typeof(TFrom), typeof(TTo), null);
        }

        public static IQuickInjectContainer RegisterType<TFrom, TTo>(this IQuickInjectContainer container, LifetimeManager lifetime)
        {
            return container.RegisterType(typeof(TFrom), typeof(TTo), lifetime);
        }

        public static IQuickInjectContainer RegisterType<T>(this IQuickInjectContainer container, LifetimeManager lifetime)
        {
            return container.RegisterType(null, typeof(T), lifetime);
        }

        public static IQuickInjectContainer RegisterInstance<T>(this IQuickInjectContainer container, T instance)
        {
            return container.RegisterInstance(typeof(T), instance, new ContainerControlledLifetimeManager());
        }

        public static T Resolve<T>(this IQuickInjectContainer container)
        {
            return (T)container.Resolve(typeof(T));
        }
    }

    public class MyDependency
    {
        private object myDepObj;

        public MyDependency(object obj)
        {
            this.myDepObj = obj;
        }
    }

    public class GenericD { }

    public class GenericC { }

    public class GenericB { }

    public class GenericA { }

    public class HaveManyGenericTypesClosed : IHaveManyGenericTypesClosed
    {
        public HaveManyGenericTypesClosed()
        { }

        public HaveManyGenericTypesClosed(GenericA t1Value)
        {
            PropT1 = t1Value;
        }

        public HaveManyGenericTypesClosed(GenericB t2Value)
        {
            PropT2 = t2Value;
        }

        public HaveManyGenericTypesClosed(GenericB t2Value, GenericA t1Value)
        {
            PropT2 = t2Value;
            PropT1 = t1Value;
        }

        private GenericA propT1;

        public GenericA PropT1
        {
            get { return propT1; }
            set { propT1 = value; }
        }

        private GenericB propT2;

        public GenericB PropT2
        {
            get { return propT2; }
            set { propT2 = value; }
        }

        private GenericC propT3;

        public GenericC PropT3
        {
            get { return propT3; }
            set { propT3 = value; }
        }

        private GenericD propT4;

        public GenericD PropT4
        {
            get { return propT4; }
            set { propT4 = value; }
        }

        public void Set(GenericA t1Value)
        {
            PropT1 = t1Value;
        }

        public void Set(GenericB t2Value)
        {
            PropT2 = t2Value;
        }

        public void Set(GenericC t3Value)
        {
            PropT3 = t3Value;
        }

        public void Set(GenericD t4Value)
        {
            PropT4 = t4Value;
        }

        public void SetMultiple(GenericD t4Value, GenericC t3Value)
        {
            PropT4 = t4Value;
            PropT3 = t3Value;
        }
    }

    public interface IHaveManyGenericTypesClosed
    {
        GenericA PropT1 { get; set; }
        GenericB PropT2 { get; set; }
        GenericC PropT3 { get; set; }
        GenericD PropT4 { get; set; }

        void Set(GenericA value);
        void Set(GenericB value);
        void Set(GenericC value);
        void Set(GenericD value);

        void SetMultiple(GenericD t4Value, GenericC t3Value);
    }

    public class MyDisposableObject : IDisposable
    {
        private bool wasDisposed = false;

        public bool WasDisposed
        {
            get { return wasDisposed; }
            set { wasDisposed = value; }
        }

        public void Dispose()
        {
            wasDisposed = true;
        }
    }

    public class UnityTestClass
    {
        private string name = "Hello";

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
    }

    public interface IA
    {
    }

    public interface IB
    {
    }

    public interface IC
    {
    }

    public class A : IA
    {
        public A()
        {
        }
    }

    public class B : IB
    {
        public B(IA a)
        {
        }
    }

    public class C
    {
        public C(IA a, IB b)
        {
            
        }
    }

    public interface ITest
    { }

    public class ATest : ITest
    {
        public string Strtest = "Hello";
    }

    public class BTest : ATest
    { }

    public class CTest : BTest
    { }

    public interface ITestColl
    { }

    public class ATestColl : ITestColl
    {
        public string Strtest = "Hello";

        public ATestColl()
        {
        }
    }

    public class PremitiveParameter : ITestColl
    {
        public PremitiveParameter(int i)
        {
        }
    }

    public class ListOfClassParameter : ITestColl
    {
        public ListOfClassParameter(List<ATest> lst)
        {
        }
    }

    public class IntParameter : ITestColl
    {
        public IntParameter(Int32 i32)
        {
        }
    }

    public class ArrParameter : ITestColl
    {
        public ArrParameter(ATest[] i)
        {
        }
    }

    public class BTestColl : ATestColl
    {
        public BTestColl(ATestColl[] acoll)
        {
        }
    }

    public class CTestColl : ITestColl
    {
        public CTestColl(char acoll)
        {
        }
    }

    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void DuplicateRegInParentAndChild()
        {
            IA a = new A();
            IB b = new B(a);

            var parent = new QuickInjectContainer();
            parent.RegisterInstance(a).RegisterInstance(b);

            IQuickInjectContainer child = parent.CreateChildContainer();

            var childA = child.Resolve<IA>();
            var parentA = parent.Resolve<IA>();

            var childB = child.Resolve<IB>();
            var parentB = parent.Resolve<IB>();

            Assert.IsTrue(childA == parentA);
            Assert.IsTrue(childB == parentB);
        }

        [TestMethod]
        public void ChildRegistrationIsChosenWhenResolvedFromChild()
        {
            IA aParent = new A();
            IA aChild = new A();

            var parent = new QuickInjectContainer();
            parent.RegisterInstance(aParent);

            IQuickInjectContainer child = parent.CreateChildContainer();
            child.RegisterInstance(aChild);


            Assert.IsTrue(aChild == child.Resolve<IA>());
            Assert.IsTrue(aParent == parent.Resolve<IA>());
        }

        [TestMethod]
        public void WhenInstanceIsRegisteredAsSingletonEnsureItIsNotGarbageCollected()
        {
            ITest iTest;
            BTest objB = new BTest();

            var uc1 = new QuickInjectContainer();

            uc1.RegisterType<ITest, ATest>();
            iTest = objB;

            uc1.RegisterInstance<ITest>(iTest);

            iTest = (ITest)uc1.Resolve(typeof(ITest));
            Assert.IsNotNull(iTest);

            iTest = null;

            GC.Collect();

            iTest = (ITest)uc1.Resolve(typeof(ITest));

            Assert.IsNotNull(iTest);

            iTest = (ITest)uc1.Resolve(typeof(ITest));

            Assert.IsNotNull(iTest);
        }

        [TestMethod]
        public void SetLifetimeGetTwice()
        {
            IQuickInjectContainer uc = new QuickInjectContainer();

            uc.RegisterType<A>(new ContainerControlledLifetimeManager());
            A obj = uc.Resolve<A>();
            A obj1 = uc.Resolve<A>();

            Assert.AreSame(obj, obj1);
        }

        [TestMethod]
        public void SetSingletonRegisterInstanceTwice()
        {
            IQuickInjectContainer uc = new QuickInjectContainer();

            A aInstance = new A();
            uc.RegisterInstance<A>(aInstance).RegisterInstance<A>(aInstance);
            A obj = uc.Resolve<A>();
            A obj1 = uc.Resolve<A>();

            Assert.AreSame(obj, obj1);
        }

        [TestMethod]
        public void RegisterWithParentAndChild()
        {
            //create unity container
            var parentuc = new QuickInjectContainer();

            //register type UnityTestClass
            parentuc.RegisterType<UnityTestClass>(new ContainerControlledLifetimeManager());

            UnityTestClass mytestparent = parentuc.Resolve<UnityTestClass>();
            mytestparent.Name = "Hello World";
            IQuickInjectContainer childuc = parentuc.CreateChildContainer();
            childuc.RegisterType<UnityTestClass>(new ContainerControlledLifetimeManager());

            UnityTestClass mytestchild = childuc.Resolve<UnityTestClass>();

            Assert.AreNotSame(mytestparent.Name, mytestchild.Name);
        }

        [TestMethod]
        public void UseExternallyControlledLifetime()
        {
            IQuickInjectContainer parentuc = new QuickInjectContainer();

            parentuc.RegisterType<UnityTestClass>(new ExternallyControlledLifetimeManager());

            UnityTestClass parentinstance = parentuc.Resolve<UnityTestClass>();
            parentinstance.Name = "Hello World Ob1";
            parentinstance = null;
            GC.Collect();
            UnityTestClass parentinstance1 = parentuc.Resolve<UnityTestClass>();

            Assert.AreSame("Hello", parentinstance1.Name);
        }

        [TestMethod]
        public void UseExternallyControlledLifetimeResolve()
        {
            IQuickInjectContainer parentuc = new QuickInjectContainer();
            parentuc.RegisterType<UnityTestClass>(new ExternallyControlledLifetimeManager());

            UnityTestClass parentinstance = parentuc.Resolve<UnityTestClass>();
            parentinstance.Name = "Hello World Ob1";

            UnityTestClass parentinstance1 = parentuc.Resolve<UnityTestClass>();

            Assert.AreSame(parentinstance.Name, parentinstance1.Name);
        }

        [TestMethod]
        public void UseContainerControlledLifetime()
        {
            UnityTestClass obj1 = new UnityTestClass();

            obj1.Name = "InstanceObj";

            var parentuc = new QuickInjectContainer();
            parentuc.RegisterType<UnityTestClass>(new ContainerControlledLifetimeManager());

            UnityTestClass parentinstance = parentuc.Resolve<UnityTestClass>();
            parentinstance.Name = "Hello World Ob1";
            parentinstance = null;
            GC.Collect();

            UnityTestClass parentinstance1 = parentuc.Resolve<UnityTestClass>();

            Assert.AreSame("Hello World Ob1", parentinstance1.Name);
        }

        [TestMethod]
        public void GetObject()
        {
            var uc = new QuickInjectContainer();
            object obj = uc.Resolve<object>();

            Assert.IsNotNull(obj);
        }

        [TestMethod]
        public void RecursiveDependencies()
        {
            IQuickInjectContainer uc = new QuickInjectContainer();
            object obj1 = uc.Resolve<MyDependency>();

            Assert.IsNotNull(obj1);
            Assert.IsInstanceOfType(obj1, typeof(MyDependency));
        }

        [TestMethod]
        public void TwoInstancesAreNotSame()
        {
            var uc = new QuickInjectContainer();
            object obj1 = uc.Resolve<object>();
            object obj2 = uc.Resolve<object>();

            Assert.AreNotSame(obj1, obj2);
        }

        [TestMethod]
        public void SingletonsAreSame()
        {
            IQuickInjectContainer uc = new QuickInjectContainer()
                .RegisterType<object>(new ContainerControlledLifetimeManager());
            object obj1 = uc.Resolve<object>();
            object obj2 = uc.Resolve<object>();

            Assert.AreSame(obj1, obj2);
            Assert.IsInstanceOfType(obj1.GetType(), typeof(object));
        }
        
        [TestMethod]
        public void ContainerReturnsTheSameInstanceOnTheSameThread()
        {
            IQuickInjectContainer container = new QuickInjectContainer();

            container.RegisterType<IHaveManyGenericTypesClosed, HaveManyGenericTypesClosed>(new PerThreadLifetimeManager());

            IHaveManyGenericTypesClosed a = container.Resolve<IHaveManyGenericTypesClosed>();
            IHaveManyGenericTypesClosed b = container.Resolve<IHaveManyGenericTypesClosed>();

            Assert.AreSame(a, b);
        }

        [TestMethod]
        public void ContainerReturnsDifferentInstancesOnDifferentThreads()
        {
            IQuickInjectContainer container = new QuickInjectContainer();

            container.RegisterType<IHaveManyGenericTypesClosed, HaveManyGenericTypesClosed>(new PerThreadLifetimeManager());

            Thread t1 = new Thread(new ParameterizedThreadStart(ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadProcedure));
            Thread t2 = new Thread(new ParameterizedThreadStart(ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadProcedure));

            ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadInformation info =
                new ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadInformation(container);

            t1.Start(info);
            t2.Start(info);
            t1.Join();
            t2.Join();

            IHaveManyGenericTypesClosed a = new List<IHaveManyGenericTypesClosed>(info.ThreadResults.Values)[0];
            IHaveManyGenericTypesClosed b = new List<IHaveManyGenericTypesClosed>(info.ThreadResults.Values)[1];

            Assert.AreNotSame(a, b);
        }

        [TestMethod]
        public void TwoInterfacesMappedToSameConcreteTypeGetSameInstance()
        {
            IQuickInjectContainer container = new QuickInjectContainer();
            container.RegisterType<IFoo, Foo>(new ContainerControlledLifetimeManager());
            container.RegisterType<IBar, Foo>();
            var foo = container.Resolve<IFoo>();
            var bar = container.Resolve<IBar>();

            Assert.AreSame(foo, bar);
        }

        [TestMethod]
        public void ComplicatedRegistrationsWithChildContainerLifetimes1()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new TransientLifetimeRemovalBuildPlanVisitor());
            var child = container.CreateChildContainer();

            var correctInstanceForIFooResolutionFromChild = new Foo();
            var correctInstanceForFooResolutionFromChild = new SuperFoo();

            var preSetFooOnLifetime = new Foo();
            SuperFoo fooResolvedFromMainContainer = new SuperFoo();

            var lifetime = new ContainerControlledLifetimeManager();
            lifetime.SetValue(preSetFooOnLifetime);

            container.RegisterType<IFoo, Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => new Foo()));
            container.RegisterType<IBar, Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => correctInstanceForIFooResolutionFromChild));
            container.RegisterType<Foo, SuperFoo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => fooResolvedFromMainContainer));

            child.RegisterType<Foo, SuperFoo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => correctInstanceForFooResolutionFromChild));

            var f = container.Resolve<Foo>();
            var g = container.Resolve<Foo>();

            Assert.AreSame(child.Resolve<IBar>(), correctInstanceForIFooResolutionFromChild);
            Assert.AreSame(child.Resolve<IFoo>(), correctInstanceForIFooResolutionFromChild);
            Assert.AreSame(child.Resolve<Foo>(), correctInstanceForFooResolutionFromChild);
        }

        [TestMethod]
        public void ComplicatedRegistrationsWithChildContainerLifetimes2()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new TransientLifetimeRemovalBuildPlanVisitor());
            var child = container.CreateChildContainer();

            var correctInstanceForIFooResolutionFromChild = new Foo();
            var correctInstanceForFooResolutionFromChild = new SuperFoo();

            var preSetFooOnLifetime = new Foo();
            SuperFoo fooResolvedFromMainContainer = new SuperFoo();

            var lifetime = new ContainerControlledLifetimeManager();
            lifetime.SetValue(fooResolvedFromMainContainer);

            container.RegisterType<IFoo, Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => new Foo()));
            container.RegisterType<IBar, Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => correctInstanceForIFooResolutionFromChild));
            container.RegisterType<Foo, SuperFoo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => fooResolvedFromMainContainer));
            container.RegisterType<SuperFoo>(lifetime);
            child.RegisterType<Foo, SuperFoo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => correctInstanceForFooResolutionFromChild));

            var f = container.Resolve<Foo>();
            var g = container.Resolve<Foo>();

            Assert.AreSame(child.Resolve<IBar>(), correctInstanceForIFooResolutionFromChild);
            Assert.AreSame(child.Resolve<IFoo>(), correctInstanceForIFooResolutionFromChild);
            Assert.AreSame(child.Resolve<Foo>(), correctInstanceForFooResolutionFromChild);
            Assert.AreSame(container.Resolve<Foo>(), fooResolvedFromMainContainer);
            Assert.AreSame(container.Resolve<SuperFoo>(), fooResolvedFromMainContainer);
        }

        [TestMethod]
        public void SetValueCallsArePreservedWhenTransientLifetimeRemovalRuns()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new TransientLifetimeRemovalBuildPlanVisitor());
            
            container.RegisterType<IFoo, Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => new Foo()));
            container.RegisterType<IBar, Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => new Foo()));

            var a = container.Resolve<IFoo>();
            var b = container.Resolve<IBar>();

            Assert.AreSame(a, b);
        }

        [TestMethod]
        public void LifetimeManagerWillProvideValueForAnInterfaceType()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new TransientLifetimeRemovalBuildPlanVisitor());

            var lifetime = new ContainerControlledLifetimeManager();
            var foo = new Foo();
            lifetime.SetValue(foo);

            container.RegisterType<IFoo>(lifetime);

            var a = container.Resolve<IFoo>();

            Assert.AreSame(a, foo);
        }

        [TestMethod]
        public void SynchronizedLifetimeManagerRecoveryTestWithExceptionReThrownAndRecoverCalled()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new LifetimeManagerRequiresRecoveryBuildPlanVisitor());

            var lifetime = new ThrowRecordingSynchronizedLifetimeManager();
            container.RegisterType<Foo>(lifetime, new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => ThrowException()));

            bool exceptionWasReThrown = false;

            try
            {
                container.Resolve<Foo>();
            }
            catch (ArgumentNullException)
            {
                exceptionWasReThrown = true;
            }

            Assert.IsTrue(exceptionWasReThrown);
            Assert.IsTrue(lifetime.RecoverWasCalled);
        }

        private static Foo ThrowException()
        {
            throw new ArgumentNullException("Foobar");
        }

        [TestMethod]
        public void ReregistrationOfATypeUpdatesBuildCache()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new LifetimeManagerRequiresRecoveryBuildPlanVisitor());

            var foo = new Foo();

            container.RegisterType<Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => foo));
            var cfoo = container.Resolve<ConsumesFoo>();
            container.RegisterType<Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => new Foo()));

            var cfoo2 = container.Resolve<ConsumesFoo>();

            Assert.AreNotSame(cfoo.Foo, cfoo2.Foo);
        }

        [TestMethod]
        public void ReregistrationOfATypeUpdatesBuildCacheInChildContainer()
        {
            var container = new QuickInjectContainer();
            container.AddBuildPlanVisitor(new LifetimeManagerRequiresRecoveryBuildPlanVisitor());

            var child = container.CreateChildContainer();

            var foo = new Foo();

            container.RegisterType<Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => foo));
            var cfoo = child.Resolve<ConsumesFoo>();
            container.RegisterType<Foo>(new ContainerControlledLifetimeManager(), new ParameterizedLambdaExpressionInjectionFactory<Foo>(() => new Foo()));

            var cfoo2 = child.Resolve<ConsumesFoo>();

            Assert.AreNotSame(cfoo.Foo, cfoo2.Foo);
        }

        [TestMethod]
        public void RegisterTypeAfterRegisterInstanceDoesNotReusePreviousInstance()
        {
            var container = new QuickInjectContainer();

            var foo = new Foo();
            var foo2 = new Foo();
            container.RegisterInstance<IFoo>(foo);

            var returnedInstance = container.Resolve<IFoo>();

            var lifetime = new ContainerControlledLifetimeManager();
            lifetime.SetValue(foo2);

            container.RegisterType<IFoo>(lifetime);
            var returnedInstance2 = container.Resolve<IFoo>();

            Assert.AreNotSame(returnedInstance, returnedInstance2);
        }

        [TestMethod]
        public void ResolutionParameterTest()
        {
            var container = new QuickInjectContainer();
            container.RegisterType<HttpContextBase>()
                     .RegisterType<HttpRequestBase>(new ParameterizedLambdaExpressionInjectionFactory<HttpContextBase, HttpRequestBase>(httpContext => httpContext.Request))
                     .RegisterType<HttpCookieCollection>(new ParameterizedLambdaExpressionInjectionFactory<HttpRequestBase, HttpCookieCollection>(httpRequest => httpRequest.Cookies));

            container.RegisterResolutionContextType<HttpContextBase>();

            var ctx = new HttpContextBase();
            var request = new HttpRequestBase();
            request.Cookies = new HttpCookieCollection();
            ctx.Request = request;

            container.AddBuildPlanVisitor(new TransientLifetimeRemovalBuildPlanVisitor());
            var cookie = container.Resolve<HttpCookieCollectionUser>(ctx);
            Assert.AreSame(cookie.Cookiecollection, ctx.Request.Cookies);
        }

        [TestMethod]
        public void RegisterTypeOfMultiLevelParameterizedInjectionFactory()
        {
            var container = new QuickInjectContainer();
            container.RegisterType<HttpContextBase>()
                     .RegisterType<HttpRequestBase>(new ParameterizedLambdaExpressionInjectionFactory<HttpContextBase, HttpRequestBase>(httpContext => httpContext.Request))
                     .RegisterType<HttpCookieCollection>(new ParameterizedLambdaExpressionInjectionFactory<HttpRequestBase, HttpCookieCollection>(httpRequest => httpRequest.Cookies));
            
            var cookie = container.Resolve<HttpCookieCollectionUser>();
            Assert.AreNotEqual(cookie, null);
        }

        public class HttpContextBase
        {
            public HttpContextBase()
            {
                this.Request = new HttpRequestBase();
            }

            public HttpRequestBase Request { get; set; }
        }

        public class HttpRequestBase
        {
            public HttpRequestBase()
            {
                this.Cookies = new HttpCookieCollection();
            }

            public HttpCookieCollection Cookies { get; set; }
        }

        public class HttpCookieCollection
        {
            public string Cookie { get; set; }
        }

        public class HttpCookieCollectionUser
        {
            public HttpCookieCollection Cookiecollection { get; set; }

            public HttpCookieCollectionUser(HttpCookieCollection cookieCollection, HttpRequestBase request)
            {
                this.Cookiecollection = cookieCollection;
            }
        }

        public class ThrowRecordingSynchronizedLifetimeManager : LifetimeManager, IRequiresRecovery
        {
            private object store;

            public bool RecoverWasCalled { get; private set; }

            public override object GetValue()
            {
                return this.store;
            }

            public void Recover()
            {
                this.RecoverWasCalled = true;
            }

            public override void RemoveValue()
            {
                throw new NotImplementedException();
            }

            public override void SetValue(object newValue)
            {
                this.store = newValue;
            }
        }

        public class SuperFoo : Foo
        {

        }

        public class ConsumesFoo
        {
            public ConsumesFoo(Foo foo)
            {
                this.Foo = foo;
            }

            public Foo Foo { get; private set; }
        }

        public class Foo : IFoo, IBar
        {

        }

        public interface IFoo
        {

        }

        public interface IBar
        {

        }

        public class ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadInformation
        {
            private readonly IQuickInjectContainer _container;
            private readonly Dictionary<Thread, IHaveManyGenericTypesClosed> _threadResults;
            private readonly object dictLock = new object();

            public ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadInformation(IQuickInjectContainer container)
            {
                _container = container;
                _threadResults = new Dictionary<Thread, IHaveManyGenericTypesClosed>();
            }

            public IQuickInjectContainer Container
            {
                get { return _container; }
            }

            public Dictionary<Thread, IHaveManyGenericTypesClosed> ThreadResults
            {
                get { return _threadResults; }
            }

            public void SetThreadResult(Thread t, IHaveManyGenericTypesClosed result)
            {
                lock (dictLock)
                {
                    _threadResults.Add(t, result);
                }
            }
        }

        private void ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadProcedure(object o)
        {
            ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadInformation info = o as ContainerReturnsDifferentInstancesOnDifferentThreads_ThreadInformation;

            IHaveManyGenericTypesClosed resolve1 = info.Container.Resolve<IHaveManyGenericTypesClosed>();
            IHaveManyGenericTypesClosed resolve2 = info.Container.Resolve<IHaveManyGenericTypesClosed>();

            Assert.AreSame(resolve1, resolve2);

            info.SetThreadResult(Thread.CurrentThread, resolve1);
        }
    }
}