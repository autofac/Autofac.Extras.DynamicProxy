using System;
using Castle.DynamicProxy;
using Xunit;

namespace Autofac.Extras.DynamicProxy.Test
{
    public class InterfaceInterceptionWithPropertyInjectionFixture
    {
        [Fact]
        public void InterfaceInterceptorsSupportPropertyInjection()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<StringMethodInterceptor>();

            builder.RegisterType<OtherImpl>().As<IOtherService>();

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

        [Fact(Skip = "https://github.com/autofac/Autofac/issues/758")]
        public void InterfaceInterceptorsSupportCircularPropertyInjection()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<StringMethodInterceptor>();

            builder.RegisterType<OtherImpl>().As<IOtherService>();

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

        public interface IOtherService
        {
        }

        public class OtherImpl : IOtherService
        {
        }

        public interface IPublicInterface
        {
            string PublicMethod();

            IOtherService GetServiceProperty();
        }

        public class InterceptableWithProperty : IPublicInterface
        {
            public IOtherService ServiceProperty { get; set; }

            public IOtherService GetServiceProperty() => ServiceProperty;

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
}
