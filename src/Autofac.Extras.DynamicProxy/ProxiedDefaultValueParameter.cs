// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Autofac.Core;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// Supplies optional constructor argument values that are lost when a class
/// proxy is generated.
/// </summary>
/// <remarks>
/// <para>
/// Class interception replaces the registered implementation type with a
/// generated proxy subclass. The generated constructors mirror the parameters
/// of the type being proxied, but they don't carry the default values of those
/// parameters, so
/// <see cref="Autofac.Core.Activators.Reflection.DefaultValueParameter"/> can't
/// see them and optional arguments fail to bind. This parameter reads the
/// default values from the type that was proxied and supplies them on the
/// proxy's behalf.
/// </para>
/// <para>
/// The values are read once, when this parameter is created, so resolving costs
/// a dictionary lookup rather than a walk over the constructors.
/// </para>
/// <para>
/// This is a last resort. Values passed to the resolve operation, values
/// configured on the registration, and services available from the container
/// all take precedence, which keeps binding behavior the same as it would be
/// without a proxy.
/// </para>
/// </remarks>
internal sealed class ProxiedDefaultValueParameter : Parameter
{
    private readonly IEnumerable<Parameter> _configuredParameters;

    private readonly Dictionary<ParameterInfo, Func<object?>> _defaultValues;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="ProxiedDefaultValueParameter"/> class.
    /// </summary>
    /// <param name="proxyType">
    /// The generated proxy type, whose constructor parameters are the ones being
    /// supplied.
    /// </param>
    /// <param name="proxiedType">
    /// The type that was proxied; the source of the default values.
    /// </param>
    /// <param name="configuredParameters">
    /// The parameters configured on the registration. These take precedence
    /// over default values, so they're checked before one is supplied.
    /// </param>
    /// <param name="proxyArgumentCount">
    /// The number of leading arguments the generated constructors take for the
    /// proxy itself - the mixins, the interceptor array, and the selector. The
    /// parameters mirrored from the proxied type start after these.
    /// </param>
    public ProxiedDefaultValueParameter(Type proxyType, Type proxiedType, IEnumerable<Parameter> configuredParameters, int proxyArgumentCount)
    {
        _configuredParameters = configuredParameters;
        _defaultValues = FindDefaultValues(proxyType, proxiedType, proxyArgumentCount);
    }

    /// <inheritdoc/>
    public override bool CanSupplyValue(ParameterInfo pi, IComponentContext context, [NotNullWhen(returnValue: true)] out Func<object?>? valueProvider)
    {
        valueProvider = null;

        if (!_defaultValues.TryGetValue(pi, out var defaultValueProvider))
        {
            return false;
        }

        // Defer to the container when the service is genuinely available;
        // autowiring wins over a default value on an unproxied type too.
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

        valueProvider = defaultValueProvider;
        return true;
    }

    /// <summary>
    /// Reads the default values the generated constructors dropped, keyed by the
    /// proxy constructor parameter each one belongs to.
    /// </summary>
    /// <param name="proxyType">The generated proxy type.</param>
    /// <param name="proxiedType">The type that was proxied.</param>
    /// <param name="proxyArgumentCount">
    /// The number of leading arguments the generated constructors take for the
    /// proxy itself.
    /// </param>
    /// <returns>
    /// The default value providers for the parameters that have one.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A generated constructor mirrors, in order, the parameters of the one
    /// constructor it chains to. The whole mirrored signature has to be matched
    /// to find that constructor: overloads can share a parameter name and type
    /// while declaring different default values, so matching a single parameter
    /// across all of them picks up the wrong default.
    /// </para>
    /// </remarks>
    private static Dictionary<ParameterInfo, Func<object?>> FindDefaultValues(Type proxyType, Type proxiedType, int proxyArgumentCount)
    {
        var defaultValues = new Dictionary<ParameterInfo, Func<object?>>();

        // Non-public constructors are included because a protected constructor
        // is mirrored by a public one on the proxy, which the container can then
        // select.
        var proxiedConstructors = proxiedType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (var proxyConstructor in proxyType.GetConstructors())
        {
            var mirrored = proxyConstructor.GetParameters();

            foreach (var proxiedConstructor in proxiedConstructors)
            {
                var proxied = proxiedConstructor.GetParameters();

                if (proxied.Length != mirrored.Length - proxyArgumentCount ||
                    !IsMirroredBy(proxied, mirrored, proxyArgumentCount))
                {
                    continue;
                }

                AddDefaultValues(defaultValues, proxied, mirrored, proxyArgumentCount);
                break;
            }
        }

        return defaultValues;
    }

    /// <summary>
    /// Determines whether the parameters of a constructor on the proxied type
    /// are the ones a generated constructor mirrors.
    /// </summary>
    /// <param name="proxied">
    /// The parameters of a constructor on the proxied type.
    /// </param>
    /// <param name="mirrored">
    /// The parameters of the generated proxy constructor.
    /// </param>
    /// <param name="proxyArgumentCount">
    /// The number of leading arguments the generated constructor takes for the
    /// proxy itself.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the generated constructor mirrors
    /// <paramref name="proxied" />; otherwise, <see langword="false" />.
    /// </returns>
    private static bool IsMirroredBy(ParameterInfo[] proxied, ParameterInfo[] mirrored, int proxyArgumentCount)
    {
        for (var i = 0; i < proxied.Length; i++)
        {
            var proxyParameter = mirrored[i + proxyArgumentCount];

            if (!string.Equals(proxied[i].Name, proxyParameter.Name, StringComparison.Ordinal) ||
                proxied[i].ParameterType != proxyParameter.ParameterType)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Records the default values declared on a constructor of the proxied type
    /// against the parameters of the generated constructor mirroring it.
    /// </summary>
    /// <param name="defaultValues">The set of default values being built.</param>
    /// <param name="proxied">
    /// The parameters of the constructor on the proxied type.
    /// </param>
    /// <param name="mirrored">
    /// The parameters of the generated proxy constructor.
    /// </param>
    /// <param name="proxyArgumentCount">
    /// The number of leading arguments the generated constructor takes for the
    /// proxy itself.
    /// </param>
    private static void AddDefaultValues(Dictionary<ParameterInfo, Func<object?>> defaultValues, ParameterInfo[] proxied, ParameterInfo[] mirrored, int proxyArgumentCount)
    {
        for (var i = 0; i < proxied.Length; i++)
        {
            if (TryGetDefaultValue(proxied[i], out var defaultValue))
            {
                defaultValues.Add(mirrored[i + proxyArgumentCount], () => defaultValue);
            }
        }
    }

    /// <summary>
    /// Reads the default value declared on a parameter of the proxied type.
    /// </summary>
    /// <param name="proxied">The parameter on the proxied type.</param>
    /// <param name="defaultValue">
    /// The default value, if the parameter declares one.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if the parameter declares a default value;
    /// otherwise, <see langword="false" />.
    /// </returns>
    private static bool TryGetDefaultValue(ParameterInfo proxied, out object? defaultValue)
    {
        defaultValue = null;

        if (!proxied.HasDefaultValue)
        {
            return false;
        }

        defaultValue = proxied.DefaultValue;

        // Workaround for https://github.com/dotnet/corefx/issues/11797,
        // mirroring the handling in Autofac's DefaultValueParameter.
        if (defaultValue is null && proxied.ParameterType.IsValueType)
        {
            defaultValue = Activator.CreateInstance(proxied.ParameterType);
        }

        return true;
    }
}
