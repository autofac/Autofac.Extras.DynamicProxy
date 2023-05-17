// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Reflection;
using Autofac.Builder;
using Autofac.Core;
using Autofac.Core.Resolving.Pipeline;
using Autofac.Features.Scanning;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy;

/// <summary>
/// Adds registration syntax to the <see cref="ContainerBuilder"/> type.
/// </summary>
public static class RegistrationExtensions
{
    private const string InterceptorsPropertyName = "Autofac.Extras.DynamicProxy.RegistrationExtensions.InterceptorsPropertyName";

    private const string AttributeInterceptorsPropertyName = "Autofac.Extras.DynamicProxy.RegistrationExtensions.AttributeInterceptorsPropertyName";

    private static readonly IEnumerable<Service> EmptyServices = Enumerable.Empty<Service>();

    private static readonly ProxyGenerator ProxyGenerator = new();

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration)
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default);
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default);
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <param name="options">Proxy generation options to apply.</param>
    /// <param name="additionalInterfaces">Additional interface types. Calls to their members will be proxied as well.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        params Type[] additionalInterfaces)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ActivatorData.ConfigurationActions.Add((t, rb) => rb.EnableClassInterceptors(options, additionalInterfaces));
        return registration;
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or added with InterceptedBy().
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <param name="options">Proxy generation options to apply.</param>
    /// <param name="additionalInterfaces">Additional interface types. Calls to their members will be proxied as well.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        params Type[] additionalInterfaces)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ActivatorData.ImplementationType =
            ProxyGenerator.ProxyBuilder.CreateClassProxyType(
                registration.ActivatorData.ImplementationType,
                additionalInterfaces ?? Type.EmptyTypes,
                options);

        var interceptorServices = GetInterceptorServicesFromAttributes(registration.ActivatorData.ImplementationType);
        AddInterceptorServicesToMetadata(registration, interceptorServices, AttributeInterceptorsPropertyName);

        registration.OnPreparing(e =>
        {
            var proxyParameters = new List<Parameter>();
            int index = 0;

            if (options.HasMixins)
            {
                foreach (var mixin in options.MixinData.Mixins)
                {
                    proxyParameters.Add(new PositionalParameter(index++, mixin));
                }
            }

            proxyParameters.Add(new PositionalParameter(index++, GetInterceptorServices(e.Component, registration.ActivatorData.ImplementationType)
                .Select(s => e.Context.ResolveService(s))
                .Cast<IInterceptor>()
                .ToArray()));

            if (options.Selector != null)
            {
                proxyParameters.Add(new PositionalParameter(index, options.Selector));
            }

            e.Parameters = proxyParameters.Concat(e.Parameters).ToArray();
        });

        return registration;
    }

    /// <summary>
    /// Enable interface interception on the target type. Interceptors will be determined
    /// via Intercept attributes on the class or interface, or added with InterceptedBy() calls.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">Registration style.</typeparam>
    /// <param name="registration">Registration to apply interception to.</param>
    /// <param name="options">Proxy generation options to apply.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> EnableInterfaceInterceptors<TLimit, TActivatorData, TSingleRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration, ProxyGenerationOptions? options = null)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ConfigurePipeline(p => p.Use(PipelinePhase.Activation, MiddlewareInsertionMode.StartOfPhase, (ctx, next) =>
        {
            next(ctx);

            EnsureInterfaceInterceptionApplies(ctx.Registration);

            if (ctx.Instance is null)
            {
                return;
            }

            var proxiedInterfaces = ctx.Instance
                .GetType()
                .GetInterfaces()
                .Where(ProxyUtil.IsAccessible)
                .ToArray();

            if (!proxiedInterfaces.Any())
            {
                return;
            }

            var theInterface = proxiedInterfaces.First();
            var interfaces = proxiedInterfaces.Skip(1).ToArray();

            var interceptors = GetInterceptorServices(ctx.Registration, ctx.Instance.GetType())
                .Select(s => ctx.ResolveService(s))
                .Cast<IInterceptor>()
                .ToArray();

            ctx.Instance = options == null
                ? ProxyGenerator.CreateInterfaceProxyWithTarget(theInterface, interfaces, ctx.Instance, interceptors)
                : ProxyGenerator.CreateInterfaceProxyWithTarget(theInterface, interfaces, ctx.Instance, options, interceptors);
        }));

        return registration;
    }

    /// <summary>
    /// Allows a list of interceptor services to be assigned to the registration.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Registration style.</typeparam>
    /// <param name="builder">Registration to apply interception to.</param>
    /// <param name="interceptorServices">The interceptor services.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    /// <exception cref="System.ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorServices"/>.</exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InterceptedBy<TLimit, TActivatorData, TStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        params Service[] interceptorServices)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (interceptorServices == null || interceptorServices.Any(s => s == null))
        {
            throw new ArgumentNullException(nameof(interceptorServices));
        }

        AddInterceptorServicesToMetadata(builder, interceptorServices, InterceptorsPropertyName);

        return builder;
    }

    /// <summary>
    /// Allows a list of interceptor services to be assigned to the registration.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Registration style.</typeparam>
    /// <param name="builder">Registration to apply interception to.</param>
    /// <param name="interceptorServiceNames">The names of the interceptor services.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorServiceNames"/>.</exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InterceptedBy<TLimit, TActivatorData, TStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        params string[] interceptorServiceNames)
    {
        if (interceptorServiceNames == null || interceptorServiceNames.Any(n => n == null))
        {
            throw new ArgumentNullException(nameof(interceptorServiceNames));
        }

        return InterceptedBy(builder, interceptorServiceNames.Select(n => new KeyedService(n, typeof(IInterceptor))).ToArray());
    }

    /// <summary>
    /// Allows a list of interceptor services to be assigned to the registration.
    /// </summary>
    /// <typeparam name="TLimit">Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">Activator data type.</typeparam>
    /// <typeparam name="TStyle">Registration style.</typeparam>
    /// <param name="builder">Registration to apply interception to.</param>
    /// <param name="interceptorServiceTypes">The types of the interceptor services.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="builder"/> or <paramref name="interceptorServiceTypes"/>.</exception>
    public static IRegistrationBuilder<TLimit, TActivatorData, TStyle> InterceptedBy<TLimit, TActivatorData, TStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        params Type[] interceptorServiceTypes)
    {
        if (interceptorServiceTypes == null || interceptorServiceTypes.Any(t => t == null))
        {
            throw new ArgumentNullException(nameof(interceptorServiceTypes));
        }

        return InterceptedBy(builder, interceptorServiceTypes.Select(t => new TypedService(t)).ToArray());
    }

    private static void EnsureInterfaceInterceptionApplies(IComponentRegistration componentRegistration)
    {
        if (componentRegistration.Services
            .OfType<IServiceWithType>()
            .Select(s => new Tuple<Type, TypeInfo>(s.ServiceType, s.ServiceType.GetTypeInfo()))
            .Any(s => !s.Item2.IsInterface || !ProxyUtil.IsAccessible(s.Item1)))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.CurrentCulture,
                    RegistrationExtensionsResources.InterfaceProxyingOnlySupportsInterfaceServices,
                    componentRegistration));
        }
    }

    private static void AddInterceptorServicesToMetadata<TLimit, TActivatorData, TStyle>(
        IRegistrationBuilder<TLimit, TActivatorData, TStyle> builder,
        IEnumerable<Service> interceptorServices,
        string metadataKey)
    {
        if (builder.RegistrationData.Metadata.TryGetValue(metadataKey, out object? existing) && existing is IEnumerable<Service> existingServices)
        {
            builder.RegistrationData.Metadata[metadataKey] =
                existingServices.Concat(interceptorServices).Distinct();
        }
        else
        {
            builder.RegistrationData.Metadata.Add(metadataKey, interceptorServices);
        }
    }

    private static IEnumerable<Service> GetInterceptorServices(IComponentRegistration registration, Type implType)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        if (implType == null)
        {
            throw new ArgumentNullException(nameof(implType));
        }

        var result = EmptyServices;

        if (registration.Metadata.TryGetValue(InterceptorsPropertyName, out object? services) && services is IEnumerable<Service> existingPropertyServices)
        {
            result = result.Concat(existingPropertyServices);
        }

        return (registration.Metadata.TryGetValue(AttributeInterceptorsPropertyName, out services) && services is IEnumerable<Service> existingAttributeServices)
            ? result.Concat(existingAttributeServices)
            : result.Concat(GetInterceptorServicesFromAttributes(implType));
    }

    private static IEnumerable<Service> GetInterceptorServicesFromAttributes(Type implType)
    {
        var implTypeInfo = implType.GetTypeInfo();
        if (!implTypeInfo.IsClass)
        {
            return Enumerable.Empty<Service>();
        }

        var classAttributeServices = implTypeInfo
            .GetCustomAttributes(typeof(InterceptAttribute), true)
            .Cast<InterceptAttribute>()
            .Select(att => att.InterceptorService);

        var interfaceAttributeServices = implType
            .GetInterfaces()
            .SelectMany(i => i.GetTypeInfo().GetCustomAttributes(typeof(InterceptAttribute), true))
            .Cast<InterceptAttribute>()
            .Select(att => att.InterceptorService);

        return classAttributeServices.Concat(interfaceAttributeServices);
    }
}
