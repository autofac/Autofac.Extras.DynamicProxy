# Autofac.Extras.DynamicProxy

Interceptor and decorator support for [Autofac](https://autofac.org) via Castle DynamicProxy.

[![Build status](https://github.com/autofac/Autofac.Extras.DynamicProxy/actions/workflows/main.yml/badge.svg)](https://github.com/autofac/Autofac.Extras.DynamicProxy/actions/workflows/ci.yml) [![codecov](https://codecov.io/gh/Autofac/Autofac.Extras.DynamicProxy/branch/develop/graph/badge.svg)](https://codecov.io/gh/Autofac/Autofac.Extras.DynamicProxy) [![NuGet](https://img.shields.io/nuget/v/Autofac.Extras.DynamicProxy.svg)](https://nuget.org/packages/Autofac.Extras.DynamicProxy)

Please file issues and pull requests for this package in this repository rather than in the Autofac core repo.

- [Documentation](https://autofac.readthedocs.io/en/latest/advanced/interceptors.html)
- [NuGet](https://www.nuget.org/packages/Autofac.Extras.DynamicProxy/)
- [Contributing](https://autofac.readthedocs.io/en/latest/contributors.html)
- [Open in Visual Studio Code](https://open.vscode.dev/autofac/Autofac.Extras.DynamicProxy)

## Quick Start

First, create your interceptor:

```csharp
public class CallLogger : IInterceptor
{
  TextWriter _output;

  public CallLogger(TextWriter output)
  {
    _output = output;
  }

  public void Intercept(IInvocation invocation)
  {
    _output.Write("Calling method {0}.", invocation.Method.Name);
    invocation.Proceed();
    _output.WriteLine("Done: result was {0}.", invocation.ReturnValue);
  }
}
```

Then register your type to be intercepted:

```csharp
var builder = new ContainerBuilder();
builder.RegisterType<SomeType>()
       .As<ISomeInterface>()
       .EnableInterfaceInterceptors();
builder.Register(c => new CallLogger(Console.Out));
var container = builder.Build();
var willBeIntercepted = container.Resolve<ISomeInterface>();
```

[You can read more details in the documentation.](https://autofac.readthedocs.io/en/latest/advanced/interceptors.html)

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).
