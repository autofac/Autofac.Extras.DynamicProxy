// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class ClassInterceptorsWithOptionsFixture
{
    public interface ILazyLoadMixin
    {
        bool IsLoaded { get; }
    }

    [Fact]
    public void CanCreateMixinWithClassInterceptors()
    {
        var options = new ProxyGenerationOptions();
        options.AddMixinInstance(new LazyLoadMixin());

        var builder = new ContainerBuilder();
        builder.RegisterType<HasAttributeInterceptors>().EnableClassInterceptors(options);
        builder.RegisterType<AddOneInterceptor>();
        builder.RegisterType<AddTenInterceptor>();
        var container = builder.Build();
        int i = 10;
        var cpt = container.Resolve<HasAttributeInterceptors>(TypedParameter.From(i));

        var loaded = cpt as ILazyLoadMixin;
        Assert.NotNull(loaded);
        Assert.True(loaded.IsLoaded);
    }

    [Fact]
    public void CanInterceptMethodsWithSpecificInterceptors()
    {
        var options = new ProxyGenerationOptions { Selector = new MyInterceptorSelector() };

        var builder = new ContainerBuilder();
        builder.RegisterType<HasAttributeInterceptors>().EnableClassInterceptors(options);
        builder.RegisterType<AddOneInterceptor>();
        builder.RegisterType<AddTenInterceptor>();
        var container = builder.Build();
        int i = 10;
        var cpt = container.Resolve<HasAttributeInterceptors>(TypedParameter.From(i));

        Assert.Equal(i + 1, cpt.GetFirstValueByMethod());
        Assert.Equal(i + 10, cpt.GetSecondValueByMethod());
    }

    [Fact]
    public void CanInterceptOnlySpecificMethods()
    {
        var options = new ProxyGenerationOptions(new InterceptOnlyJ());

        var builder = new ContainerBuilder();
        builder.RegisterType<HasAttributeInterceptors>().EnableClassInterceptors(options);
        builder.RegisterType<AddOneInterceptor>();
        builder.RegisterType<AddTenInterceptor>();
        var container = builder.Build();
        int i = 10;
        var cpt = container.Resolve<HasAttributeInterceptors>(TypedParameter.From(i));

        Assert.Equal(i, cpt.GetFirstValueByMethod());
        Assert.Equal(i + 11, cpt.GetSecondValueByMethod());
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

    [Intercept(typeof(AddOneInterceptor))]
    [Intercept(typeof(AddTenInterceptor))]
    public class HasAttributeInterceptors
    {
        public HasAttributeInterceptors(int i)
        {
            FirstValue = SecondValue = i;
        }

        public int FirstValue { get; set; }

        public int SecondValue { get; set; }

        public virtual int GetFirstValueByMethod()
        {
            return FirstValue;
        }

        public virtual int GetSecondValueByMethod()
        {
            return SecondValue;
        }
    }

    public class ServiceWithTwoValues
    {
        public ServiceWithTwoValues(int i)
        {
            FirstValue = SecondValue = i;
        }

        public int FirstValue { get; set; }

        public int SecondValue { get; set; }

        public virtual int GetFirstValueByMethod()
        {
            return FirstValue;
        }

        public virtual int GetSecondValueByMethod()
        {
            return SecondValue;
        }
    }

    private class InterceptOnlyJ : IProxyGenerationHook
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

    public class LazyLoadMixin : ILazyLoadMixin
    {
        public bool IsLoaded
        {
            get
            {
                return true;
            }
        }
    }

    private class MyInterceptorSelector : IInterceptorSelector
    {
        public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
        {
            var result = method.Name == "GetFirstValueByMethod"
                ? interceptors.OfType<AddOneInterceptor>().ToArray<IInterceptor>()
                : interceptors.OfType<AddTenInterceptor>().ToArray<IInterceptor>();

            if (result.Length == 0)
            {
                throw new InvalidOperationException("No interceptors for method " + method.Name);
            }

            return result;
        }
    }
}
