// Copyright (c) Autofac Project. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Autofac.Features.AttributeFilters;
using Castle.DynamicProxy;

namespace Autofac.Extras.DynamicProxy.Test;

/// <summary>
/// Documents the interaction between <c>WithAttributeFiltering</c> and class
/// interception, including a known limitation tracked by
/// <see href="https://github.com/autofac/Autofac.Extras.DynamicProxy/issues/56">issue #56</see>.
/// </summary>
/// <remarks>
/// <para>
/// <c>EnableClassInterceptors</c> rewrites the implementation type to a Castle
/// DynamicProxy subclass, replicating the original constructor parameter
/// attributes onto the proxy constructor. Castle reproduces an <b>enum</b>
/// argument passed to an attribute parameter that is typed <see cref="object"/>
/// (as <c>KeyFilterAttribute(object key)</c> is) using the enum's <b>underlying
/// integer</b> type rather than the original enum type. As a result the key on
/// the replicated <see cref="KeyFilterAttribute"/> no longer matches the
/// registered keyed service, and filtering silently fails.
/// </para>
/// <para>
/// This is an upstream Castle DynamicProxy bug tracked at
/// <see href="https://github.com/castleproject/Core/issues/748">castleproject/Core#748</see>.
/// It only affects non-string keys under class interception; string keys and
/// interface interception are unaffected. The tests below pin the current
/// behavior so we get a signal if/when the upstream behavior changes.
/// </para>
/// </remarks>
public class ClassInterceptorsWithAttributeFilteringFixture
{
    public enum LoggerKey
    {
        First,
        Second,
    }

    [Fact]
    public void EnumKeyFilteringWorksWithoutInterception()
    {
        // Baseline: WithAttributeFiltering honors an enum [KeyFilter] when
        // interception is NOT enabled.
        var builder = new ContainerBuilder();
        builder.Register(c => "first").Keyed<string>(LoggerKey.First);
        builder.Register(c => "second").Keyed<string>(LoggerKey.Second);
        builder.RegisterType<HasEnumKeyedParameter>().WithAttributeFiltering();
        var container = builder.Build();

        var resolved = container.Resolve<HasEnumKeyedParameter>();

        Assert.Equal("first", resolved.Value);
    }

    [Fact]
    public void EnumKeyFilteringIsNotHonoredWithClassInterception()
    {
        // KNOWN LIMITATION (issue #56 / castleproject/Core#748): with class
        // interception the replicated [KeyFilter] loses its enum key type, so the
        // filter no longer matches the keyed registration and the unkeyed
        // fallback is injected instead of the "First" keyed value.
        //
        // This test documents the *current, incorrect* behavior. If it starts
        // failing because "first" is resolved, Castle has likely fixed the
        // underlying bug and this limitation (and the docs/comments) can be
        // removed.
        var builder = new ContainerBuilder();
        builder.RegisterType<NullInterceptor>();
        builder.Register(c => "first").Keyed<string>(LoggerKey.First);
        builder.Register(c => "fallback");
        builder
            .RegisterType<HasEnumKeyedParameter>()
            .WithAttributeFiltering()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(NullInterceptor));
        var container = builder.Build();

        var resolved = container.Resolve<HasEnumKeyedParameter>();

        Assert.Equal("fallback", resolved.Value);
    }

    [Fact]
    public void StringKeyFilteringWorksWithClassInterception()
    {
        // WORKAROUND: a string key round-trips through the object-typed attribute
        // parameter losslessly, so filtering works with class interception.
        var builder = new ContainerBuilder();
        builder.RegisterType<NullInterceptor>();
        builder.Register(c => "first").Keyed<string>("first-key");
        builder.Register(c => "fallback");
        builder
            .RegisterType<HasStringKeyedParameter>()
            .WithAttributeFiltering()
            .EnableClassInterceptors()
            .InterceptedBy(typeof(NullInterceptor));
        var container = builder.Build();

        var resolved = container.Resolve<HasStringKeyedParameter>();

        Assert.Equal("first", resolved.Value);
    }

    [Fact]
    public void EnumKeyFilteringWorksWithInterfaceInterception()
    {
        // WORKAROUND: interface interception does not rewrite the constructor, so
        // the original [KeyFilter] is read intact and the enum key matches.
        var builder = new ContainerBuilder();
        builder.RegisterType<NullInterceptor>();
        builder.Register(c => "first").Keyed<string>(LoggerKey.First);
        builder.Register(c => "fallback");
        builder
            .RegisterType<HasEnumKeyedParameter>()
            .WithAttributeFiltering()
            .As<IHaveValue>()
            .EnableInterfaceInterceptors()
            .InterceptedBy(typeof(NullInterceptor));
        var container = builder.Build();

        var resolved = container.Resolve<IHaveValue>();

        Assert.Equal("first", resolved.Value);
    }

    public interface IHaveValue
    {
        string Value
        {
            get;
        }
    }

    public class HasEnumKeyedParameter : IHaveValue
    {
        public HasEnumKeyedParameter([KeyFilter(LoggerKey.First)] string value)
        {
            Value = value;
        }

        public virtual string Value
        {
            get;
        }
    }

    public class HasStringKeyedParameter
    {
        public HasStringKeyedParameter([KeyFilter("first-key")] string value)
        {
            Value = value;
        }

        public virtual string Value
        {
            get;
        }
    }

    private class NullInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            invocation.Proceed();
        }
    }
}
