// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Extras.DynamicProxy.Benchmarks.Scenario;

namespace Autofac.Extras.DynamicProxy.Benchmarks;

/// <summary>
/// Tests the performance of retrieving a (reasonably) deeply-nested object graph.
/// </summary>
public class ClassInterceptionBenchmark
{
    private IContainer _container = null!;

    [GlobalSetup]
    public void Setup()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<ClassWithInterceptAttribute>()
            .EnableClassInterceptors();
        builder.RegisterType<ClassWithoutInterceptAttribute>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        builder.RegisterType<ClassWithOptionalParameter>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        builder.RegisterType<StringMethodInterceptor>();
        _container = builder.Build();
    }

    [Benchmark]
    public string WiredUsingInterceptAttribute()
    {
        var instance = _container.Resolve<ClassWithInterceptAttribute>();
        return instance.Test();
    }

    [Benchmark]
    public string WiredUsingInterceptedBy()
    {
        var instance = _container.Resolve<ClassWithoutInterceptAttribute>();
        return instance.Test();
    }

    [Benchmark]
    public string WithOptionalConstructorParameter()
    {
        var instance = _container.Resolve<ClassWithOptionalParameter>();
        return instance.Test();
    }
}
