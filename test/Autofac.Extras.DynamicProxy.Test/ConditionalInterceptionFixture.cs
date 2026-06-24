// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Builder;
using Autofac.Features.Scanning;
using Castle.DynamicProxy;
using IInvocation = Castle.DynamicProxy.IInvocation;

namespace Autofac.Extras.DynamicProxy.Test;

public class ConditionalInterceptionFixture
{
    [Fact]
    public void NullRegistration_Throws()
    {
        IRegistrationBuilder<Intercepted, ConcreteReflectionActivatorData, SingleRegistrationStyle> concrete = null!;
        IRegistrationBuilder<Intercepted, ScanningActivatorData, SingleRegistrationStyle> scanning = null!;
        Func<Type, bool> predicate = _ => true;

        Assert.Throws<ArgumentNullException>(() => concrete.EnableInterfaceInterceptors(predicate));
        Assert.Throws<ArgumentNullException>(() => concrete.EnableClassInterceptors(predicate));
        Assert.Throws<ArgumentNullException>(() => scanning.EnableClassInterceptors(predicate));
    }

    [AttributeUsage(AttributeTargets.Class)]
    private sealed class InterceptMeAttribute : Attribute
    {
    }

    public interface IPublicInterface
    {
        string PublicMethod();
    }

    [Fact]
    public void InterfaceInterception_AppliesProxyWhenPredicateMatches()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<Intercepted>()
            .EnableInterfaceInterceptors(t => t.IsDefined(typeof(InterceptMeAttribute), false))
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        var container = builder.Build();

        var obj = container.Resolve<IPublicInterface>();

        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
    }

    [Fact]
    public void InterfaceInterception_SkipsProxyWhenPredicateDoesNotMatch()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<NotIntercepted>()
            .EnableInterfaceInterceptors(t => t.IsDefined(typeof(InterceptMeAttribute), false))
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        var container = builder.Build();

        var obj = container.Resolve<IPublicInterface>();

        // Predicate returns false, so the raw instance is returned without a proxy.
        Assert.Equal("PublicMethod", obj.PublicMethod());
        Assert.IsType<NotIntercepted>(obj);
    }

    [Fact]
    public void InterfaceInterception_PredicateAppliesPerScannedType()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterAssemblyTypes(typeof(ConditionalInterceptionFixture).Assembly)
            .Where(t => t == typeof(Intercepted) || t == typeof(NotIntercepted))
            .As<IPublicInterface>()
            .EnableInterfaceInterceptors(t => t.IsDefined(typeof(InterceptMeAttribute), false))
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        var all = container.Resolve<IEnumerable<IPublicInterface>>().ToList();

        // The attributed type is proxied; the other is returned untouched.
        Assert.Contains(all, o => o.PublicMethod() == "intercepted-PublicMethod");
        Assert.Contains(all, o => o.PublicMethod() == "PublicMethod");
    }

    [Fact]
    public void ClassInterception_AppliesProxyWhenPredicateMatches()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<InterceptedClass>()
            .EnableClassInterceptors(t => t.IsDefined(typeof(InterceptMeAttribute), false))
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        var obj = container.Resolve<InterceptedClass>();

        Assert.Equal("intercepted-VirtualMethod", obj.VirtualMethod());
    }

    [Fact]
    public void ClassInterception_SkipsProxyWhenPredicateDoesNotMatch()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterType<NotInterceptedClass>()
            .EnableClassInterceptors(t => t.IsDefined(typeof(InterceptMeAttribute), false))
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        var obj = container.Resolve<NotInterceptedClass>();

        // Predicate returns false, so the type is registered without a proxy.
        Assert.Equal("VirtualMethod", obj.VirtualMethod());
        Assert.Same(typeof(NotInterceptedClass), obj.GetType());
    }

    [Fact]
    public void ClassInterception_PredicateAppliesPerScannedType()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();
        builder
            .RegisterAssemblyTypes(typeof(ConditionalInterceptionFixture).Assembly)
            .Where(t => t == typeof(InterceptedClass) || t == typeof(NotInterceptedClass))
            .EnableClassInterceptors(t => t.IsDefined(typeof(InterceptMeAttribute), false))
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();

        Assert.Equal("intercepted-VirtualMethod", container.Resolve<InterceptedClass>().VirtualMethod());
        Assert.Equal("VirtualMethod", container.Resolve<NotInterceptedClass>().VirtualMethod());
    }

    [InterceptMe]
    public class Intercepted : IPublicInterface
    {
        public string PublicMethod() => "PublicMethod";
    }

    public class NotIntercepted : IPublicInterface
    {
        public string PublicMethod() => "PublicMethod";
    }

    [InterceptMe]
    public class InterceptedClass
    {
        public virtual string VirtualMethod() => "VirtualMethod";
    }

    public class NotInterceptedClass
    {
        public virtual string VirtualMethod() => "VirtualMethod";
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
