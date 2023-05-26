// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class AttributedInterfaceInterceptionFixture
{
    [Intercept(typeof(AddOneInterceptor))]
    public interface IHasValue
    {
        int GetValueByMethod();
    }

    [Fact]
    public void DetectsNonInterfaceServices()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<Service>().EnableInterfaceInterceptors();
        builder.RegisterType<AddOneInterceptor>();
        var c = builder.Build();
        var dx = Assert.Throws<DependencyResolutionException>(() => c.Resolve<Service>());
        Assert.IsType<InvalidOperationException>(dx.InnerException);
    }

    [Fact]
    public void FindsInterceptionAttributeOnExpressionComponent()
    {
        var builder = new ContainerBuilder();
        builder.Register(c => new Service()).As<IHasValue>().EnableInterfaceInterceptors();
        builder.RegisterType<AddOneInterceptor>();
        var cpt = builder.Build().Resolve<IHasValue>();

        Assert.Equal(11, cpt.GetValueByMethod()); // proxied
    }

    [Fact]
    public void FindsInterceptionAttributeOnReflectionComponent()
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<Service>().As<IHasValue>()
            .EnableInterfaceInterceptors();

        builder.RegisterType<AddOneInterceptor>();

        var container = builder.Build();
        var cpt = container.Resolve<IHasValue>();

        Assert.Equal(11, cpt.GetValueByMethod()); // proxied
    }

    public class Service : IHasValue
    {
        public Service()
        {
            Value = 10;
        }

        public int Value { get; private set; }

        public int GetValueByMethod()
        {
            return Value;
        }
    }

    private class AddOneInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.Method.Name == "GetValueByMethod")
            {
                invocation.ReturnValue = 1 + (int)invocation.ReturnValue;
            }
        }
    }
}
