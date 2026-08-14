// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class ClassInterceptorsWithOptionalParametersFixture
{
    [Fact]
    public void OptionalReferenceParameterUsesDefaultWhenNotRegistered()
    {
        var container = BuildContainer<HasOptionalDependency>();

        var instance = container.Resolve<HasOptionalDependency>();

        Assert.Null(instance.Dependency);
    }

    [Fact]
    public void OptionalValueParameterUsesDefaultWhenNotRegistered()
    {
        var container = BuildContainer<HasOptionalValue>();

        var instance = container.Resolve<HasOptionalValue>();

        Assert.Equal(42, instance.Count);
    }

    [Fact]
    public void InterceptionStillAppliesWhenOptionalParameterIsDefaulted()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<HasOptionalValue>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(AddOneInterceptor));
        builder.RegisterType<AddOneInterceptor>();
        var container = builder.Build();

        var instance = container.Resolve<HasOptionalValue>();

        Assert.Equal(43, instance.GetCountByMethod());
    }

    [Fact]
    public void RegisteredServiceTakesPrecedenceOverDefault()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<HasOptionalDependency>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(DoNothingInterceptor));
        builder.RegisterType<DoNothingInterceptor>();
        builder.RegisterType<Dependency>().As<IDependency>();
        var container = builder.Build();

        var instance = container.Resolve<HasOptionalDependency>();

        Assert.IsType<Dependency>(instance.Dependency);
    }

    [Fact]
    public void ConfiguredParameterTakesPrecedenceOverDefault()
    {
        var expected = new Dependency();
        var builder = new ContainerBuilder();
        builder.RegisterType<HasOptionalDependency>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(DoNothingInterceptor))
            .WithParameter(TypedParameter.From<IDependency>(expected));
        builder.RegisterType<DoNothingInterceptor>();
        var container = builder.Build();

        var instance = container.Resolve<HasOptionalDependency>();

        Assert.Same(expected, instance.Dependency);
    }

    [Fact]
    public void ResolveParameterTakesPrecedenceOverDefault()
    {
        var container = BuildContainer<HasOptionalDependency>();
        var expected = new Dependency();

        var instance = container.Resolve<HasOptionalDependency>(TypedParameter.From<IDependency>(expected));

        Assert.Same(expected, instance.Dependency);
    }

    [Fact]
    public void RequiredParameterStillThrowsWhenMissing()
    {
        var container = BuildContainer<HasRequiredDependency>();

        Assert.Throws<DependencyResolutionException>(() => container.Resolve<HasRequiredDependency>());
    }

    [Fact]
    public void DefaultComesFromTheSelectedConstructorOverload()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<HasOverloadedConstructors>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(DoNothingInterceptor))
            .WithParameter(TypedParameter.From<IDependency>(new Dependency()));
        builder.RegisterType<DoNothingInterceptor>();
        var container = builder.Build();

        var instance = container.Resolve<HasOverloadedConstructors>();

        Assert.Equal(99, instance.Count);
    }

    [Fact]
    public void DefaultComesFromTheShorterConstructorWhenItIsTheOneSelected()
    {
        var container = BuildContainer<HasOverloadedConstructors>();

        var instance = container.Resolve<HasOverloadedConstructors>();

        Assert.Equal(1, instance.Count);
    }

    [Fact]
    public void ProtectedConstructorDefaultIsUsed()
    {
        // A protected constructor is mirrored by a public one on the proxy, so the
        // container can select it where it couldn't on the unproxied type.
        var builder = new ContainerBuilder();
        builder.RegisterType<HasProtectedConstructor>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(DoNothingInterceptor))
            .WithParameter(TypedParameter.From("named"));
        builder.RegisterType<DoNothingInterceptor>();
        var container = builder.Build();

        var instance = container.Resolve<HasProtectedConstructor>();

        Assert.Equal(7, instance.Count);
    }

    [Fact]
    public void OptionalDateTimeParameterUsesDefault()
    {
        var container = BuildContainer<HasOptionalDateTime>();

        var instance = container.Resolve<HasOptionalDateTime>();

        Assert.Equal(default, instance.When);
    }

    private static IContainer BuildContainer<TService>()
        where TService : class
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<TService>()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(DoNothingInterceptor));
        builder.RegisterType<DoNothingInterceptor>();
        return builder.Build();
    }

    public interface IDependency
    {
    }

    public class Dependency : IDependency
    {
    }

    public class HasOptionalDependency
    {
        public HasOptionalDependency(IDependency? dependency = null)
        {
            Dependency = dependency;
        }

        public IDependency? Dependency
        {
            get;
        }
    }

    public class HasOptionalValue
    {
        public HasOptionalValue(int count = 42)
        {
            Count = count;
        }

        public int Count
        {
            get;
        }

        public virtual int GetCountByMethod()
        {
            return Count;
        }
    }

    public class HasOptionalDateTime
    {
        public HasOptionalDateTime(DateTime when = default)
        {
            When = when;
        }

        public DateTime When
        {
            get;
        }
    }

    public class HasOverloadedConstructors
    {
        public HasOverloadedConstructors(int count = 1)
        {
            Count = count;
        }

        public HasOverloadedConstructors(IDependency dependency, int count = 99)
        {
            Dependency = dependency;
            Count = count;
        }

        public int Count
        {
            get;
        }

        public IDependency? Dependency
        {
            get;
        }
    }

    public class HasProtectedConstructor
    {
        protected HasProtectedConstructor(string name, int count = 7)
        {
            Name = name;
            Count = count;
        }

        public string Name
        {
            get;
        }

        public int Count
        {
            get;
        }
    }

    public class HasRequiredDependency
    {
        public HasRequiredDependency(IDependency dependency)
        {
            Dependency = dependency;
        }

        public IDependency Dependency
        {
            get;
        }
    }

    private class DoNothingInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }

    private class AddOneInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();

            if (invocation.Method.ReturnType == typeof(int))
            {
                invocation.ReturnValue = ((int)invocation.ReturnValue!) + 1;
            }
        }
    }
}
