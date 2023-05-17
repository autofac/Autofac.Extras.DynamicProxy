// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class AttributedInterfaceInterceptionWithOptionsFixture
{
    [Intercept(typeof(AddOneInterceptor))]
    [Intercept(typeof(AddTenInterceptor))]
    public interface IHasTwoValues
    {
        int GetFirstValueByMethod();

        int GetSecondValueByMethod();
    }

    [Fact]
    public void CanCreateMixinWithAttributeInterceptors()
    {
        var options = new ProxyGenerationOptions();
        options.AddMixinInstance(new Dictionary<int, int>());

        var builder = new ContainerBuilder();
        builder.RegisterType<Service>().As<IHasTwoValues>().EnableInterfaceInterceptors(options);
        builder.RegisterType<AddOneInterceptor>();
        builder.RegisterType<AddTenInterceptor>();
        var cpt = builder.Build().Resolve<IHasTwoValues>();

        var dict = cpt as IDictionary<int, int>;

        Assert.NotNull(dict);

        dict.Add(1, 2);

        Assert.Equal(2, dict[1]);

        dict.Clear();

        Assert.Empty(dict);
    }

    [Fact]
    public void CanInterceptMethodsWithSpecificInterceptors()
    {
        var options = new ProxyGenerationOptions { Selector = new MyInterceptorSelector() };

        var builder = new ContainerBuilder();
        builder.RegisterType<Service>().As<IHasTwoValues>().EnableInterfaceInterceptors(options);
        builder.RegisterType<AddOneInterceptor>();
        builder.RegisterType<AddTenInterceptor>();
        var cpt = builder.Build().Resolve<IHasTwoValues>();

        Assert.Equal(11, cpt.GetFirstValueByMethod());
        Assert.Equal(20, cpt.GetSecondValueByMethod());
    }

    [Fact]
    public void CanInterceptOnlySpecificMethods()
    {
        var options = new ProxyGenerationOptions(new InterceptOnlySecondValue());

        var builder = new ContainerBuilder();
        builder.RegisterType<Service>().As<IHasTwoValues>().EnableInterfaceInterceptors(options);
        builder.RegisterType<AddOneInterceptor>();
        builder.RegisterType<AddTenInterceptor>();
        var cpt = builder.Build().Resolve<IHasTwoValues>();

        Assert.Equal(10, cpt.GetFirstValueByMethod());
        Assert.Equal(21, cpt.GetSecondValueByMethod());
    }

    public class Service : IHasTwoValues
    {
        public Service()
        {
            FirstValue = SecondValue = 10;
        }

        public int FirstValue { get; private set; }

        public int SecondValue { get; private set; }

        public int GetFirstValueByMethod()
        {
            return FirstValue;
        }

        public int GetSecondValueByMethod()
        {
            return SecondValue;
        }
    }

    private class AddOneInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.Method.Name == "GetFirstValueByMethod" || invocation.Method.Name == "GetSecondValueByMethod")
            {
                invocation.ReturnValue = 1 + (int)invocation.ReturnValue;
            }
        }
    }

    private class AddTenInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.Method.Name == "GetSecondValueByMethod")
            {
                invocation.ReturnValue = 10 + (int)invocation.ReturnValue;
            }
        }
    }

    private class InterceptOnlySecondValue : IProxyGenerationHook
    {
        public void MethodsInspected()
        {
        }

        public void NonProxyableMemberNotification(Type type, MemberInfo memberInfo)
        {
        }

        public bool ShouldInterceptMethod(Type type, MethodInfo methodInfo)
        {
            return methodInfo.Name.Equals("GetSecondValueByMethod", StringComparison.Ordinal);
        }
    }

    private class MyInterceptorSelector : IInterceptorSelector
    {
        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            return method.Name == "GetFirstValueByMethod"
                ? interceptors.OfType<AddOneInterceptor>().ToArray<IInterceptor>()
                : interceptors.OfType<AddTenInterceptor>().ToArray<IInterceptor>();
        }
    }
}
