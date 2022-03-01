# Autofac.Extras.DynamicProxy

Interceptor and decorator support for [Autofac](https://autofac.org) via Castle DynamicProxy.

[![Build status](https://ci.appveyor.com/api/projects/status/nx0urssttgc840eo?svg=true)](https://ci.appveyor.com/project/Autofac/autofac-extras-dynamicproxy) [![codecov](https://codecov.io/gh/Autofac/Autofac.Extras.DynamicProxy/branch/develop/graph/badge.svg)](https://codecov.io/gh/Autofac/Autofac.Extras.DynamicProxy)

[![Autofac on Stack Overflow](https://img.shields.io/badge/stack%20overflow-autofac-orange.svg)](https://stackoverflow.com/questions/tagged/autofac)

## Get Packages

You can get Autofac.Extras.DynamicProxy by [grabbing the latest NuGet packages](https://www.nuget.org/packages/Autofac.Extras.DynamicProxy/). If you're feeling adventurous, [continuous integration builds are on MyGet](https://www.myget.org/gallery/autofac).

[Release notes are available on GitHub](https://github.com/autofac/Autofac.Extras.DynamicProxy/releases).

## Get Help

**Need help with Autofac?** We have [a documentation site](https://autofac.readthedocs.io/) as well as [API documentation](https://autofac.org/apidoc/). We're ready to answer your questions on [Stack Overflow](https://stackoverflow.com/questions/tagged/autofac) or check out the [discussion forum](https://groups.google.com/forum/#forum/autofac).

**If you find a bug with Autofac.Extras.DynamicProxy** please [file it in that repo](https://github.com/autofac/Autofac.Extras.DynamicProxy/issues).

## Get Started

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

## Contributing / Pull Requests

Refer to the [Contributor Guide](https://github.com/autofac/.github/blob/master/CONTRIBUTING.md)
for setting up and building Autofac source.

You can also open this repository right now [in VS Code](https://open.vscode.dev/autofac/Autofac.Extras.DynamicProxy).
