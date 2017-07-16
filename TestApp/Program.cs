namespace TestApp
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using QuickInject;

    internal sealed class Program
    {
        public static void Main(string[] args)
        {
            var container = new QuickInjectContainer();

            container.Resolve<B>();

            Foo(container);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Foo(QuickInjectContainer container)
        {
            var sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 100000000; ++i)
            {
                container.Resolve<B>();
            }

            sw.Stop();
            Console.WriteLine($"Millis: {sw.ElapsedMilliseconds}");
            Console.ReadKey();
        }
    }
}