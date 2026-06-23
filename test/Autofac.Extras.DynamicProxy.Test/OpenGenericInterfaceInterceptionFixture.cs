// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Castle.DynamicProxy;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace Autofac.Extras.DynamicProxy.Test;

public class OpenGenericInterfaceInterceptionFixture
{
    public interface ICommandHandler<TCommand, TResult>
    {
        TResult Handle(TCommand command);
    }

    [Fact]
    public void InterceptsClosedTypesOfScannedOpenGenericInterface()
    {
        // Issue #27: When scanning an assembly and registering closed types of an
        // open generic interface, interface interception should still work even
        // though the scan also registers the concrete type as a service.
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterAssemblyTypes(typeof(OpenGenericInterfaceInterceptionFixture).Assembly)
            .AsClosedTypesOf(typeof(ICommandHandler<,>))
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        var handler = container.Resolve<ICommandHandler<string, string>>();

        Assert.Equal("intercepted-Handle", handler.Handle("input"));
    }

    [Fact]
    public void InterceptsInterfaceServiceWhenRegistrationAlsoExposesConcreteType()
    {
        // A registration can expose both the concrete type and an interface. When
        // resolved by the interface, interception should apply even though the
        // concrete type is not an interface.
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<StringCommandHandler>()
            .AsSelf()
            .As<ICommandHandler<string, string>>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        var handler = container.Resolve<ICommandHandler<string, string>>();

        Assert.Equal("intercepted-Handle", handler.Handle("input"));
    }

    [Fact]
    public void ThrowsWhenResolvingConcreteServiceWithInterfaceInterception()
    {
        // Resolving the non-interface service directly cannot be proxied via
        // interface interception and must still throw, even when the registration
        // also exposes an interface service.
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<StringCommandHandler>()
            .AsSelf()
            .As<ICommandHandler<string, string>>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        var dx = Assert.Throws<DependencyResolutionException>(() => container.Resolve<StringCommandHandler>());
        Assert.IsType<InvalidOperationException>(dx.InnerException);
    }

    public class StringCommandHandler : ICommandHandler<string, string>
    {
        public string Handle(string command) => command;
    }

    private class StringMethodInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            if (invocation.Method.ReturnType == typeof(string))
            {
                invocation.ReturnValue = "intercepted-" + invocation.Method.Name;
            }
            else
            {
                invocation.Proceed();
            }
        }
    }
}
