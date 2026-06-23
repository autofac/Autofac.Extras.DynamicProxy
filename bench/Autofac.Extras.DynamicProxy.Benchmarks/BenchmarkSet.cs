// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Autofac.Extras.DynamicProxy.Benchmarks;

/// <summary>
/// The set of benchmark types that can be run from the command line.
/// </summary>
public static class BenchmarkSet
{
    /// <summary>
    /// All benchmark types in this assembly.
    /// </summary>
    public static readonly Type[] All =
    {
        typeof(ClassInterceptionBenchmark),
        typeof(InterfaceInterceptionBenchmark),
    };
}
