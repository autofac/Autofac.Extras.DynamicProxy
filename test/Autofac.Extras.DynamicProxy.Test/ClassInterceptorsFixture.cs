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

        [Fact(Skip = "Issue #14")]
        public void ThrowsIfParametersAreNotMet()
        {
            // Issue #14: Resolving an intercepted type where dependencies aren't met should throw
            var builder = new ContainerBuilder();
            builder.RegisterType<C>().EnableClassInterceptors();
            builder.RegisterType<AddOneInterceptor>();
            var container = builder.Build();
            Assert.Throws<DependencyResolutionException>(() => container.Resolve<C>());
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
    }
}
