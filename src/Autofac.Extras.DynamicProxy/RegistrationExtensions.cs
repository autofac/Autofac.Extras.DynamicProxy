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

    private static readonly IEnumerable<Service> _emptyServices = Enumerable.Empty<Service>();

    private static readonly ProxyGenerator _proxyGenerator = new();

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be
    /// determined via <see cref="InterceptAttribute"/> on the class or added
    /// with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration)
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default);
    }

    /// <summary>
    /// Enable class interception on the target type, conditionally based on the
    /// implementation type. Interceptors will be determined via
    /// <see cref="InterceptAttribute"/> on the class or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="shouldIntercept">
    /// A predicate, evaluated against each candidate implementation type, that
    /// determines whether interception is applied. Types for which the
    /// predicate returns <see langword="false" /> are registered without
    /// interception.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration,
        Func<Type, bool> shouldIntercept)
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default, shouldIntercept);
    }

    /// <summary>
    /// Enable class interception on the target type. Interceptors will be
    /// determined via <see cref="InterceptAttribute"/> on the class or added
    /// with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default);
    }

    /// <summary>
    /// Enable class interception on the target type, conditionally based on the
    /// implementation type. Interceptors will be determined via
    /// <see cref="InterceptAttribute"/> on the class or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="shouldIntercept">
    /// A predicate, evaluated against the implementation type, that determines
    /// whether interception is applied. When the predicate returns
    /// <see langword="false" /> the type is registered without interception.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration,
        Func<Type, bool> shouldIntercept)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        return EnableClassInterceptors(registration, ProxyGenerationOptions.Default, shouldIntercept);
    }

    /// <summary>
    /// Enable class interception on the target type with specific options and
    /// additional interfaces. Interceptors will be determined via
    /// <see cref="InterceptAttribute"/> on the class or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="options">
    /// Proxy generation options to apply.
    /// </param>
    /// <param name="additionalInterfaces">
    /// Additional interface types. Calls to their members will be proxied as well.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        params Type[] additionalInterfaces)
    {
        return EnableClassInterceptors(registration, options, null, additionalInterfaces);
    }

    /// <summary>
    /// Enable class interception on the target type, conditionally based on the
    /// implementation type, with specific options and additional interfaces.
    /// Interceptors will be determined via <see cref="InterceptAttribute"/> on
    /// the class or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="options">
    /// Proxy generation options to apply.
    /// </param>
    /// <param name="shouldIntercept">
    /// An optional predicate, evaluated against each candidate implementation type,
    /// that determines whether interception is applied. Types for which the predicate
    /// returns <see langword="false" /> are registered without interception. When
    /// <see langword="null" /> all types are intercepted.
    /// </param>
    /// <param name="additionalInterfaces">Additional interface types. Calls to their members will be proxied as well.</param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, ScanningActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        Func<Type, bool>? shouldIntercept,
        params Type[] additionalInterfaces)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ActivatorData.ConfigurationActions.Add((t, rb) => rb.EnableClassInterceptors(options, shouldIntercept, additionalInterfaces));
        return registration;
    }

    /// <summary>
    /// Enable class interception on the target type with specific options and
    /// additional interfaces. Interceptors will be determined via
    /// <see cref="InterceptAttribute"/> on the class or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="options">
    /// Proxy generation options to apply.
    /// </param>
    /// <param name="additionalInterfaces">
    /// Additional interface types. Calls to their members will be proxied as well.
    /// </param>
    /// <returns>Registration builder allowing the registration to be configured.</returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        params Type[] additionalInterfaces)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        return EnableClassInterceptors(registration, options, null, additionalInterfaces);
    }

    /// <summary>
    /// Enable class interception on the target type, conditionally based on the
    /// implementation type, with specific options and additional interfaces.
    /// Interceptors will be determined via <see cref="InterceptAttribute"/> on
    /// the class or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// Only virtual methods can be intercepted this way.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TConcreteReflectionActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="options">
    /// Proxy generation options to apply.
    /// </param>
    /// <param name="shouldIntercept">
    /// An optional predicate, evaluated against the implementation type, that
    /// determines whether interception is applied. When the predicate returns
    /// <see langword="false" /> the type is registered without interception. When
    /// <see langword="null" /> the type is intercepted.
    /// </param>
    /// <param name="additionalInterfaces">
    /// Additional interface types. Calls to their members will be proxied as well.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> EnableClassInterceptors<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration,
        ProxyGenerationOptions options,
        Func<Type, bool>? shouldIntercept,
        params Type[] additionalInterfaces)
        where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        // Class interception rewrites the implementation type to a proxy
        // subclass at registration time, so the decision to intercept is made
        // here, per type. When the predicate rejects the type the registration
        // is left untouched.
        if (shouldIntercept != null && !shouldIntercept(registration.ActivatorData.ImplementationType))
        {
            return registration;
        }

        var proxiedType = registration.ActivatorData.ImplementationType;

        registration.ActivatorData.ImplementationType =
            _proxyGenerator.ProxyBuilder.CreateClassProxyType(
                proxiedType,
                additionalInterfaces ?? Type.EmptyTypes,
                options);

        var interceptorServices = GetInterceptorServicesFromAttributes(registration.ActivatorData.ImplementationType);
        AddInterceptorServicesToMetadata(registration, interceptorServices, AttributeInterceptorsPropertyName);

        // The generated proxy constructors don't carry the default values of the
        // parameters they mirror. Those are read from the type being proxied,
        // once, the first time something is resolved - the number of arguments
        // the proxy takes for itself isn't known until the parameters supplying
        // them have been built.
        ProxiedDefaultValueParameter? proxiedDefaultValues = null;

        registration.OnPreparing(e =>
        {
            var proxyParameters = new List<Parameter>();
            var index = 0;

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

            proxiedDefaultValues ??= new ProxiedDefaultValueParameter(
                registration.ActivatorData.ImplementationType,
                proxiedType,
                registration.ActivatorData.ConfiguredParameters,
                proxyParameters.Count);

            e.Parameters = proxyParameters
                .Concat(e.Parameters)
                .Append(proxiedDefaultValues)
                .ToArray();
        });

        return registration;
    }

    /// <summary>
    /// Enable interface interception on the target type. Interceptors will be
    /// determined via <see cref="InterceptAttribute"/> on the class or
    /// interface, or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="options">
    /// Proxy generation options to apply.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> EnableInterfaceInterceptors<TLimit, TActivatorData, TSingleRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration, ProxyGenerationOptions? options = null)
    {
        return EnableInterfaceInterceptors(registration, options, null);
    }

    /// <summary>
    /// Enable interface interception on the target type, conditionally based on
    /// the resolved implementation type. Interceptors will be determined via
    /// <see cref="InterceptAttribute"/> on the class or interface, or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.</typeparam>
    /// <typeparam name="TActivatorData">
    /// Activator data type.</typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// Registration style.</typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="shouldIntercept">
    /// A predicate, evaluated against the resolved implementation type, that
    /// determines whether interception is applied. When the predicate returns
    /// <see langword="false" /> the resolved instance is returned without a proxy.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> EnableInterfaceInterceptors<TLimit, TActivatorData, TSingleRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
        Func<Type, bool> shouldIntercept)
    {
        return EnableInterfaceInterceptors(registration, null, shouldIntercept);
    }

    /// <summary>
    /// Enable interface interception on the target type, conditionally based on
    /// the resolved implementation type, with specific options. Interceptors
    /// will be determined via <see cref="InterceptAttribute"/> on the class or
    /// interface, or added with
    /// <see cref="InterceptedBy{TLimit, TActivatorData, TStyle}(IRegistrationBuilder{TLimit, TActivatorData, TStyle},string[])"/>.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TSingleRegistrationStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="registration">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="options">
    /// Proxy generation options to apply.
    /// </param>
    /// <param name="shouldIntercept">
    /// A predicate, evaluated against the resolved implementation type, that
    /// determines whether interception is applied. When the predicate returns
    /// <see langword="false" /> the resolved instance is returned without a proxy.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    public static IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> EnableInterfaceInterceptors<TLimit, TActivatorData, TSingleRegistrationStyle>(
        this IRegistrationBuilder<TLimit, TActivatorData, TSingleRegistrationStyle> registration,
        ProxyGenerationOptions? options,
        Func<Type, bool>? shouldIntercept)
    {
        if (registration == null)
        {
            throw new ArgumentNullException(nameof(registration));
        }

        registration.ConfigurePipeline(p => p.Use(PipelinePhase.Activation, MiddlewareInsertionMode.StartOfPhase, (ctx, next) =>
        {
            next(ctx);

            // The instance won't ever _practically_ be null by the time it gets here.
            var implementationType = ctx.Instance!.GetType();

            // Interface interception happens at resolve time, so the predicate is
            // evaluated against the actual implementation type being returned. When
            // it rejects the type the instance is returned unproxied; the
            // interface-only guard is also skipped because no proxy is created.
            if (shouldIntercept != null && !shouldIntercept(implementationType))
            {
                return;
            }

            EnsureInterfaceInterceptionApplies(ctx.Service, ctx.Registration);

            var proxiedInterfaces = implementationType
                .GetInterfaces()
                .Where(ProxyUtil.IsAccessible)
                .ToArray();

            if (proxiedInterfaces.Length == 0)
            {
                return;
            }

            var theInterface = proxiedInterfaces[0];
            var interfaces = proxiedInterfaces.Skip(1).ToArray();

            var interceptors = GetInterceptorServices(ctx.Registration, implementationType)
                .Select(s => ctx.ResolveService(s))
                .Cast<IInterceptor>()
                .ToArray();

            ctx.Instance = options == null
                ? _proxyGenerator.CreateInterfaceProxyWithTarget(theInterface, interfaces, ctx.Instance, interceptors)
                : _proxyGenerator.CreateInterfaceProxyWithTarget(theInterface, interfaces, ctx.Instance, options, interceptors);
        }));

        return registration;
    }

    /// <summary>
    /// Assigns a list of interceptor services to a registration by service.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="builder">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="interceptorServices">
    /// The interceptor services.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="interceptorServices"/> is <see langword="null"/>.
    /// </exception>
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
    /// Assigns a list of interceptor services to a registration by name.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="builder">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="interceptorServiceNames">
    /// The names of the interceptor services.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="interceptorServiceNames"/> is <see langword="null"/>.
    /// </exception>
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
    /// Assigns a list of interceptor services to a registration by interceptor
    /// type.
    /// </summary>
    /// <typeparam name="TLimit">
    /// Registration limit type.
    /// </typeparam>
    /// <typeparam name="TActivatorData">
    /// Activator data type.
    /// </typeparam>
    /// <typeparam name="TStyle">
    /// Registration style.
    /// </typeparam>
    /// <param name="builder">
    /// Registration to apply interception to.
    /// </param>
    /// <param name="interceptorServiceTypes">
    /// The types of the interceptor services.
    /// </param>
    /// <returns>
    /// Registration builder allowing the registration to be configured.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder"/> or <paramref name="interceptorServiceTypes"/> is <see langword="null"/>.
    /// </exception>
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

    private static void EnsureInterfaceInterceptionApplies(Service service, IComponentRegistration componentRegistration)
    {
        // Only the service actually being resolved needs to be a public interface.
        // A registration may expose additional services (for example, the concrete
        // type alongside an interface when scanning with AsClosedTypesOf) that are
        // not interfaces; those are irrelevant when interception is applied to an
        // interface service. See issue #27.
        if (service is IServiceWithType serviceWithType &&
            (!serviceWithType.ServiceType.GetTypeInfo().IsInterface || !ProxyUtil.IsAccessible(serviceWithType.ServiceType)))
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
        if (builder.RegistrationData.Metadata.TryGetValue(metadataKey, out var existing) && existing is IEnumerable<Service> existingServices)
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
        var result = _emptyServices;

        if (registration.Metadata.TryGetValue(InterceptorsPropertyName, out var services) && services is IEnumerable<Service> existingPropertyServices)
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
