﻿// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Extras.DynamicProxy.Test.SatelliteAssembly;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class InterfaceInterceptorsFixture
{
    internal interface IInternalInterface
    {
        string InternalMethod();
    }

    public interface IPublicInterface
    {
        string PublicMethod();
    }

    private interface IPrivateInterface
    {
        string PrivateMethod();
    }

    [Fact]
    public void EnableInterfaceInterceptors_NullRegistration()
    {
        IRegistrationBuilder<Interceptable, ConcreteReflectionActivatorData, SingleRegistrationStyle> concrete = null;
        IRegistrationBuilder<Interceptable, Features.Scanning.ScanningActivatorData, SingleRegistrationStyle> scanning = null;
        var options = new ProxyGenerationOptions();
        Assert.Throws<ArgumentNullException>(() => concrete.EnableInterfaceInterceptors());
        Assert.Throws<ArgumentNullException>(() => concrete.EnableInterfaceInterceptors(options));
        Assert.Throws<ArgumentNullException>(() => scanning.EnableInterfaceInterceptors());
        Assert.Throws<ArgumentNullException>(() => scanning.EnableInterfaceInterceptors(options));
    }

    [Fact]
    public void InterceptsInternalInterfacesWithInternalsVisibleToDynamicProxyGenAssembly2()
    {
        var internalsAttribute = typeof(InterfaceInterceptorsFixture).GetTypeInfo().Assembly.GetCustomAttribute<InternalsVisibleToAttribute>();
        Assert.Contains("DynamicProxyGenAssembly2", internalsAttribute.AssemblyName);

        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<Interceptable>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IInternalInterface>();
        var container = builder.Build();
        var obj = container.Resolve<IInternalInterface>();
        Assert.Equal("intercepted-InternalMethod", obj.InternalMethod());
    }

    [Fact]
    public void InterceptsPublicInterfaces()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<Interceptable>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        var container = builder.Build();
        var obj = container.Resolve<IPublicInterface>();
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
    }

    [Fact]
    public void DoesNotInterceptPrivateInterfaces()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<Interceptable>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPrivateInterface>();
        var container = builder.Build();
        Assert.Throws<DependencyResolutionException>(() => container.Resolve<IPrivateInterface>());
    }

    [Fact]
    public void InterceptsPublicInterfacesSatelliteAssembly()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<InterceptablePublicSatellite>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterfaceSatellite>();
        var container = builder.Build();
        var obj = container.Resolve<IPublicInterfaceSatellite>();
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
    }

    [Fact]
    public void ThrowsIfParametersAreNotMet()
    {
        // Issue #14: Resolving an intercepted type where dependencies aren't met should throw
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<InterceptableWithParameter>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        var container = builder.Build();
        Assert.Throws<DependencyResolutionException>(() => container.Resolve<IPublicInterface>());
    }

    public class Interceptable : IPublicInterface, IInternalInterface, IPrivateInterface
    {
        public string InternalMethod()
        {
            throw new NotImplementedException();
        }

        public string PrivateMethod()
        {
            throw new NotImplementedException();
        }

        public string PublicMethod()
        {
            throw new NotImplementedException();
        }
    }

    public class InterceptableWithParameter : IPublicInterface, IInternalInterface
    {
        private readonly int _value;

        public InterceptableWithParameter(int value)
        {
            _value = value;
        }

        public string InternalMethod()
        {
            throw new NotImplementedException();
        }

        public string PublicMethod()
        {
            throw new NotImplementedException();
        }
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
