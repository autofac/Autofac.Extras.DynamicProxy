using System;
using System.Linq;
using System.Reflection;
using Castle.DynamicProxy;
using Xunit;

namespace Autofac.Extras.DynamicProxy.Test
{
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
            builder.RegisterType<C>().EnableClassInterceptors(options);
            builder.RegisterType<AddOneInterceptor>();
            builder.RegisterType<AddTenInterceptor>();
            var container = builder.Build();
            int i = 10;
            var cpt = container.Resolve<C>(TypedParameter.From(i));

            var loaded = cpt as ILazyLoadMixin;
            Assert.NotNull(loaded);
            Assert.True(loaded.IsLoaded);
        }

        [Fact]
        public void CanInterceptMethodsWithSpecificInterceptors()
        {
            var options = new ProxyGenerationOptions { Selector = new MyInterceptorSelector() };

            var builder = new ContainerBuilder();
            builder.RegisterType<C>().EnableClassInterceptors(options);
            builder.RegisterType<AddOneInterceptor>();
            builder.RegisterType<AddTenInterceptor>();
            var container = builder.Build();
            int i = 10;
            var cpt = container.Resolve<C>(TypedParameter.From(i));

            Assert.Equal(i + 1, cpt.GetI());
            Assert.Equal(i + 10, cpt.GetJ());
        }

        [Fact]
        public void CanInterceptOnlySpecificMethods()
        {
            var options = new ProxyGenerationOptions(new InterceptOnlyJ());

            var builder = new ContainerBuilder();
            builder.RegisterType<C>().EnableClassInterceptors(options);
            builder.RegisterType<AddOneInterceptor>();
            builder.RegisterType<AddTenInterceptor>();
            var container = builder.Build();
            int i = 10;
            var cpt = container.Resolve<C>(TypedParameter.From(i));

            Assert.Equal(i, cpt.GetI());
            Assert.Equal(i + 11, cpt.GetJ());
        }

        private class AddOneInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
                if (invocation.Method.Name == "GetI" || invocation.Method.Name == "GetJ")
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
                if (invocation.Method.Name == "GetJ")
                {
                    invocation.ReturnValue = 10 + (int)invocation.ReturnValue;
                }
            }
        }

        [Intercept(typeof(AddOneInterceptor))]
        [Intercept(typeof(AddTenInterceptor))]
        public class C
        {
            public C(int i)
            {
                this.I = this.J = i;
            }

            public int I { get; set; }

            public int J { get; set; }

            public virtual int GetI()
            {
                return this.I;
            }

            public virtual int GetJ()
            {
                return this.J;
            }
        }

        public class D
        {
            public D(int i)
            {
                this.I = this.J = i;
            }

            public int I { get; set; }

            public int J { get; set; }

            public virtual int GetI()
            {
                return this.I;
            }

            public virtual int GetJ()
            {
                return this.J;
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
                return methodInfo.Name.Equals("GetJ");
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
                var result = method.Name == "GetI"
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
}
