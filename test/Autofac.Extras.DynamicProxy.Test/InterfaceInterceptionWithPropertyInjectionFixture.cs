// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class InterfaceInterceptionWithPropertyInjectionFixture
{
    [Fact]
    public void InterfaceInterceptorsSupportPropertyInjection()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();

        builder.RegisterType<OtherService>();

        builder
            .RegisterType<InterceptableWithProperty>()
            .PropertiesAutowired()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor))
            .As<IPublicInterface>();
        var container = builder.Build();
        var obj = container.Resolve<IPublicInterface>();

        Assert.NotNull(obj.GetServiceProperty());
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
    }

    [Fact]
    public void InterfaceInterceptorsSupportCircularPropertyInjection()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<StringMethodInterceptor>();

        builder.RegisterType<OtherService>();

        builder
            .RegisterType<InterceptableWithProperty>()
            .As<IPublicInterface>()
            .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(StringMethodInterceptor));
        var container = builder.Build();
        var obj = container.Resolve<IPublicInterface>();

        Assert.NotNull(obj.GetServiceProperty());
        Assert.Equal("intercepted-PublicMethod", obj.PublicMethod());
    }

    public class OtherService
    {
    }

    public interface IPublicInterface
    {
        string PublicMethod();

        OtherService GetServiceProperty();
    }

    public class InterceptableWithProperty : IPublicInterface
    {
        public OtherService Service { get; set; }

        public OtherService GetServiceProperty() => Service;

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
