// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

public class InterceptorsChosenByMetadataFixture
{
    public interface ICustomerService
    {
        int GetVisitCount();
    }

    [Fact]
    public void InterceptsWhenUsingExtendedPropertyAndType()
    {
        var builder = new ContainerBuilder();
        builder.RegisterType<CustomerService>()
            .As<ICustomerService>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(AddOneInterceptor));
        builder.RegisterType<AddOneInterceptor>();
        var container = builder.Build();
        var cs = container.Resolve<ICustomerService>();
        Assert.Equal(11, cs.GetVisitCount());
    }

    public class CustomerService : ICustomerService
    {
        public CustomerService()
        {
            VisitCount = 10;
        }

        private int VisitCount { get; set; }

        public int GetVisitCount()
        {
            return VisitCount;
        }
    }

    private class AddOneInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
            if (invocation.Method.Name.StartsWith("Get"))
            {
                invocation.ReturnValue = 1 + (int)invocation.ReturnValue;
            }
        }
    }
}
