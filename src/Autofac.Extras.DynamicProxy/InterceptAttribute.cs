// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Core;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// Indicates that a type should be intercepted.
/// </summary>
[ExcludeFromCodeCoverage]
[SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
[SuppressMessage("Microsoft.Design", "CA1019:DefineAccessorsForAttributeArguments")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
public class InterceptAttribute : Attribute
{
    /// <summary>
    /// Gets the interceptor service.
    /// </summary>
    public Service InterceptorService { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptAttribute"/> class.
    /// </summary>
    /// <param name="interceptorService">The interceptor service.</param>
    /// <exception cref="System.ArgumentNullException">
    /// Thrown if <paramref name="interceptorService" /> is <see langword="null" />.
    /// </exception>
    public InterceptAttribute(Service interceptorService)
    {
        InterceptorService = interceptorService ?? throw new ArgumentNullException(nameof(interceptorService));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptAttribute"/> class.
    /// </summary>
    /// <param name="interceptorServiceName">Name of the interceptor service.</param>
    public InterceptAttribute(string interceptorServiceName)
        : this(new KeyedService(interceptorServiceName, typeof(IInterceptor)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InterceptAttribute"/> class.
    /// </summary>
    /// <param name="interceptorServiceType">The typed interceptor service.</param>
    public InterceptAttribute(Type interceptorServiceType)
        : this(new TypedService(interceptorServiceType))
    {
    }
}
