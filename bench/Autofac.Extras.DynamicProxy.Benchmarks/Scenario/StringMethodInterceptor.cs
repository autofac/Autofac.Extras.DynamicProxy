// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Benchmarks.Scenario;

internal class StringMethodInterceptor : IInterceptor
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
