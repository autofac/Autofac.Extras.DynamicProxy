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

    private readonly int _proxyArgumentCount;

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
    /// <param name="proxyArgumentCount">
    /// The number of leading arguments the generated constructors take for the proxy
    /// itself - the mixins, the interceptor array, and the selector. The parameters
    /// mirrored from the proxied type start after these.
    /// </param>
    public ProxiedDefaultValueParameter(Type proxiedType, IEnumerable<Parameter> configuredParameters, int proxyArgumentCount)
    {
        _proxiedType = proxiedType;
        _configuredParameters = configuredParameters;
        _proxyArgumentCount = proxyArgumentCount;
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

        if (proxied is null)
        {
            return false;
        }

        bool hasDefaultValue;

        try
        {
            hasDefaultValue = proxied.HasDefaultValue;
        }
        catch (FormatException) when (proxied.ParameterType == typeof(DateTime))
        {
            // Workaround for https://github.com/dotnet/corefx/issues/12338, mirroring
            // the handling in Autofac's DefaultValueParameter. Reading the default
            // value of a DateTime parameter can throw, in which case the parameter is
            // known to have one.
            valueProvider = () => default(DateTime);
            return true;
        }

        if (!hasDefaultValue)
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
    /// <remarks>
    /// <para>
    /// A generated constructor takes the arguments the proxy itself needs and then
    /// mirrors, in order, the parameters of the one constructor it chains to. The
    /// whole mirrored signature has to be matched to find that constructor:
    /// overloads can share a parameter name and type while declaring different
    /// default values, so matching a single parameter across all of them picks up
    /// the wrong default.
    /// </para>
    /// </remarks>
    private ParameterInfo? FindProxiedParameter(ParameterInfo pi)
    {
        var mirroredPosition = pi.Position - _proxyArgumentCount;

        if (mirroredPosition < 0)
        {
            // An argument belonging to the proxy rather than to the proxied type.
            return null;
        }

        var mirrored = ((ConstructorInfo)pi.Member).GetParameters();

        // Non-public constructors are included because a protected constructor is
        // mirrored by a public one on the proxy, which the container can then select.
        foreach (var constructor in _proxiedType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            var candidates = constructor.GetParameters();

            if (candidates.Length != mirrored.Length - _proxyArgumentCount)
            {
                continue;
            }

            if (IsMirroredBy(candidates, mirrored))
            {
                return candidates[mirroredPosition];
            }
        }

        return null;
    }

    /// <summary>
    /// Determines whether the parameters of a constructor on the proxied type are the
    /// ones a generated constructor mirrors.
    /// </summary>
    /// <param name="candidates">The parameters of a constructor on the proxied type.</param>
    /// <param name="mirrored">The parameters of the generated proxy constructor.</param>
    /// <returns>
    /// <see langword="true" /> if the generated constructor mirrors
    /// <paramref name="candidates" />; otherwise, <see langword="false" />.
    /// </returns>
    private bool IsMirroredBy(ParameterInfo[] candidates, ParameterInfo[] mirrored)
    {
        for (var i = 0; i < candidates.Length; i++)
        {
            var proxyParameter = mirrored[i + _proxyArgumentCount];

            if (!string.Equals(candidates[i].Name, proxyParameter.Name, StringComparison.Ordinal) ||
                candidates[i].ParameterType != proxyParameter.ParameterType)
            {
                return false;
            }
        }

        return true;
    }
}
