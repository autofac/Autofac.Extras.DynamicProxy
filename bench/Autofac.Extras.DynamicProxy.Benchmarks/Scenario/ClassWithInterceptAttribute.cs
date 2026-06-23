// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.DynamicProxy.Benchmarks.Scenario;

[Intercept(typeof(StringMethodInterceptor))]
public class ClassWithInterceptAttribute : ITest
{
    public virtual string Test()
    {
        throw new NotImplementedException();
    }
}
