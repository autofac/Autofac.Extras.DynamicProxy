using System;

namespace Autofac.Extras.DynamicProxy.Benchmarks.Scenario
{
    [Intercept(typeof(StringMethodInterceptor))]
    public class ClassWithInterceptAttribute : ITest
    {
        public virtual string Test()
        {
            throw new NotImplementedException();
        }
    }
}
