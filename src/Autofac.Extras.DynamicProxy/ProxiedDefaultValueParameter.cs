// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Autofac.Core;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// Supplies optional constructor argument values that are lost when a class proxy
/// is generated.
/// </summary>
/// <remarks>
/// <para>
/// Class interception replaces the registered implementation type with a generated
/// proxy subclass. The generated constructors mirror the parameters of the type
/// being proxied, but they don't carry the default values of those parameters, so
/// <see cref="Autofac.Core.Activators.Reflection.DefaultValueParameter"/> can't see
/// them and optional arguments fail to bind. This parameter reads the default values
/// from the type that was proxied and supplies them on the proxy's behalf.
/// </para>
/// <para>
/// This is a last resort. Values passed to the resolve operation, values configured
/// on the registration, and services available from the container all take
/// precedence, which keeps binding behavior the same as it would be without a proxy.
/// </para>
/// </remarks>
internal sealed class ProxiedDefaultValueParameter : Parameter
{
    private readonly Type _proxiedType;

    private readonly IEnumerable<Parameter> _configuredParameters;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProxiedDefaultValueParameter"/> class.
    /// </summary>
    /// <param name="proxiedType">
    /// The type that was proxied; the source of the default values.
    /// </param>
    /// <param name="configuredParameters">
    /// The parameters configured on the registration. These take precedence over
    /// default values, so they're checked before one is supplied.
    /// </param>
    public ProxiedDefaultValueParameter(Type proxiedType, IEnumerable<Parameter> configuredParameters)
    {
        _proxiedType = proxiedType;
        _configuredParameters = configuredParameters;
    }

    /// <inheritdoc/>
    public override bool CanSupplyValue(ParameterInfo pi, IComponentContext context, [NotNullWhen(returnValue: true)] out Func<object?>? valueProvider)
    {
        if (pi == null)
        {
            throw new ArgumentNullException(nameof(pi));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        valueProvider = null;

        // Only generated proxy constructors are missing default values. Anything
        // else already binds correctly on its own.
        if (pi.Member is not ConstructorInfo || !_proxiedType.IsAssignableFrom(pi.Member.DeclaringType))
        {
            return false;
        }

        // Defer to the container when the service is genuinely available; autowiring
        // wins over a default value on an unproxied type too.
        if (context.ComponentRegistry.TryGetServiceRegistration(new TypedService(pi.ParameterType), out _))
        {
            return false;
        }

        // Defer to anything explicitly configured on the registration.
        foreach (var configured in _configuredParameters)
        {
            if (configured.CanSupplyValue(pi, context, out _))
            {
                return false;
            }
        }

        var proxied = FindProxiedParameter(pi);

        if (proxied is null || !proxied.HasDefaultValue)
        {
            return false;
        }

        var defaultValue = proxied.DefaultValue;

        // Workaround for https://github.com/dotnet/corefx/issues/11797, mirroring
        // the handling in Autofac's DefaultValueParameter.
        if (defaultValue is null && pi.ParameterType.IsValueType)
        {
            defaultValue = Activator.CreateInstance(pi.ParameterType);
        }

        valueProvider = () => defaultValue;
        return true;
    }

    /// <summary>
    /// Locates the parameter on the proxied type that corresponds to a parameter on
    /// the generated proxy constructor.
    /// </summary>
    /// <param name="pi">The proxy constructor parameter.</param>
    /// <returns>
    /// The matching parameter on the proxied type, or <see langword="null" /> if
    /// there isn't one.
    /// </returns>
    private ParameterInfo? FindProxiedParameter(ParameterInfo pi)
    {
        foreach (var constructor in _proxiedType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            foreach (var parameter in constructor.GetParameters())
            {
                if (string.Equals(parameter.Name, pi.Name, StringComparison.Ordinal) &&
                    parameter.ParameterType == pi.ParameterType)
                {
                    return parameter;
                }
            }
        }

        return null;
    }
}
