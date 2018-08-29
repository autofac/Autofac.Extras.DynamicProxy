using System;

namespace Autofac.Extras.DynamicProxy.Benchmarks.Scenario
{
    public class ClassWithoutInterceptAttribute : ITest
    {
        public virtual string Test()
        {
            throw new NotImplementedException();
        }
    }
}
