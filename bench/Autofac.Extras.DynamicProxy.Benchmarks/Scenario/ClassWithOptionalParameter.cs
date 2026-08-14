// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.DynamicProxy.Benchmarks.Scenario;

public class ClassWithOptionalParameter : ITest
{
    private readonly int _count;

    public ClassWithOptionalParameter(int count = 42)
    {
        _count = count;
    }

    public virtual string Test()
    {
        return _count.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
