using System;
using Autofac.Core;
using Castle.DynamicProxy;
using Xunit;

namespace Autofac.Extras.DynamicProxy.Test
{
    public class ClassInterceptorsFixture
    {
        [Fact]
        public void InterceptorCanBeWiredUsingInterceptedBy()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<D>()
                .EnableClassInterceptors()
                .InterceptedBy(typeof(AddOneInterceptor));
            builder.RegisterType<AddOneInterceptor>();
            var container = builder.Build();
            var i = 10;
            var c = container.Resolve<D>(TypedParameter.From(i));
            var got = c.GetI();
            Assert.Equal(i + 1, got);
        }

        [Fact]
        public void InterceptsReflectionBasedComponent()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<C>().EnableClassInterceptors();
            builder.RegisterType<AddOneInterceptor>();
            var container = builder.Build();
            var i = 10;
            var c = container.Resolve<C>(TypedParameter.From(i));
            var got = c.GetI();
            Assert.Equal(i + 1, got);
        }

        [Fact]
        public void ThrowsIfParametersAreNotMet()
        {
            // Issue #14: Resolving an intercepted type where dependencies aren't met should throw
            var builder = new ContainerBuilder();
            builder.RegisterType<C>().EnableClassInterceptors();
            builder.RegisterType<AddOneInterceptor>();
            var container = builder.Build();
            Assert.Throws<DependencyResolutionException>(() => container.Resolve<C>());
        }

        [Fact]
        public void ResolveFactoryWithInterceptors()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<DoNothingInterceptor>()
                .AsSelf()
                .PropertiesAutowired()
                .InstancePerLifetimeScope();
            builder.RegisterType<ClassWithDelegate>()
                .AsSelf()
                .PropertiesAutowired()
                .InstancePerDependency()
                .EnableClassInterceptors().InterceptedBy(typeof(DoNothingInterceptor));
            builder.RegisterType<ClassWithDelegateFactory>()
                .AsSelf()
                .PropertiesAutowired()
                .InstancePerDependency()
                .EnableClassInterceptors().InterceptedBy(typeof(DoNothingInterceptor));

            var container = builder.Build();

            const int i = 123;

            using (var scope = container.BeginLifetimeScope())
            {
                var mgr = scope.Resolve<ClassWithDelegateFactory>();
                var byFunc = mgr.CreateByFunc(i);
                var byDelegate = mgr.CreateByDelegate(i);

                Assert.Equal(byFunc.I, byDelegate.I);
            }
        }

        private class AddOneInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
                if (invocation.Method.Name == "GetI")
                {
                    invocation.ReturnValue = 1 + (int)invocation.ReturnValue;
                }
            }
        }

        [Intercept(typeof(AddOneInterceptor))]
        public class C
        {
            public C(int i)
            {
                this.I = i;
            }

            public int I { get; set; }

            public virtual int GetI()
            {
                return this.I;
            }
        }

        public class D
        {
            public D(int i)
            {
                this.I = i;
            }

            public int I { get; set; }

            public virtual int GetI()
            {
                return this.I;
            }
        }

        public class ClassWithDelegate
        {
            public delegate ClassWithDelegate Factory(int i);

            public int I { get; set; }

            public ClassWithDelegate(int i)
            {
                I = i;
            }
        }

        public class ClassWithDelegateFactory
        {
            public Func<int, ClassWithDelegate> ObjectFuncFactory { get; set; }

            public ClassWithDelegate.Factory ObjectDelegateFactory { get; set; }

            public virtual ClassWithDelegate CreateByFunc(int i)
            {
                return ObjectFuncFactory(i);
            }

            public virtual ClassWithDelegate CreateByDelegate(int i)
            {
                return ObjectDelegateFactory(i);
            }
        }

        private class DoNothingInterceptor : IInterceptor
        {
            public void Intercept(IInvocation invocation)
            {
                invocation.Proceed();
            }
        }
    }
}
