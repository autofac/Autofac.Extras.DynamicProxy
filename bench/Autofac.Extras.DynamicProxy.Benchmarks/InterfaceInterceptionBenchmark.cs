using Autofac.Extras.DynamicProxy.Benchmarks.Scenario;
using BenchmarkDotNet.Attributes;

namespace Autofac.Extras.DynamicProxy.Benchmarks
{
    /// <summary>
    /// Tests the performance of retrieving a (reasonably) deeply-nested object graph.
    /// </summary>
    public class InterfaceInterceptionBenchmark
    {
        private IContainer _container;

        [GlobalSetup]
        public void Setup()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<StringMethodInterceptor>();
            builder.RegisterType<ClassWithInterceptAttribute>()
                .EnableInterfaceInterceptors()
                .As<ITest>();
            builder.RegisterType<ClassWithoutInterceptAttribute>()
                .EnableInterfaceInterceptors()
                .InterceptedBy(typeof(StringMethodInterceptor))
                .As<ITest>();
            _container = builder.Build();
        }

        [Benchmark]
        public string WiredUsingInterceptAttribute()
        {
            var instance = _container.Resolve<ITest>();
            return instance.Test();
        }

        [Benchmark]
        public string WiredUsingInterceptedBy()
        {
            var d = _container.Resolve<ITest>();
            return d.Test();
        }
    }
}
