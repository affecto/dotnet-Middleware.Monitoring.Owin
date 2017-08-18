# Monitoring.Owin
* **Affecto.Middleware.Monitoring.Owin**
  * Monitoring middleware implementation based on OWIN interface defined in Microsoft.Owin NuGet.
  * NuGet: https://www.nuget.org/packages/Affecto.Middleware.Monitoring.Owin

### Build status

[![Build status](https://ci.appveyor.com/api/projects/status/qtta5ad35bo6bu8a?svg=true)](https://ci.appveyor.com/project/affecto/dotnet-middleware-monitoring-owin)

## Monitoring middleware code examples (using Autofac)

Middleware is registered on per-route basis using shallow and deep endpoints (`route/_monitor/shallow` and `route/_monitor/deep` respectively). Shallow endpoint performs a quick check, while deep endpoint uses a `IHealthCheckService` which implements a `CheckHealthAsync` method. This method is used for deeper monitoring, e.g. database status checking. Both shallow and deep routes return HTTP 204. In case of exception during deep check, HTTP 503 is returned.

#### Extending monitoring middleware

```csharp
using Affecto.Middleware.Monitoring.Owin;
using Microsoft.Owin;
// ...other dependencies

internal class SearchMonitoringMiddleware : MonitoringMiddleware
{
    public SearchMonitoringMiddleware(OwinMiddleware next, IComponentContext container)
        : base (next, "/my-route", container.Resolve<Func<IHealthCheckService>>())
        {
        }
}
```

#### Implementing health check service for deep endpoint

```csharp
using Affecto.Middleware.Monitoring.Owin;
// ...other dependencies

internal class SearchHealthCheckService : IHealthCheckService
{
    public Task CheckHealthAsync()
    {
        // perform deep check to e.g. database
    }
}
```

#### Registering middleware

```csharp
// ...dependencies

public class Startup
{
    public void Configuration(IAppBuilder app)
    {
        var builder = new ContainerBuilder();

        builder.RegisterType<SearchHealthCheckService>().As<IHealthCheckService>();
        builder.RegisterType<SearchMonitoringMiddleware>();

        // if you need multiple middleware with different health check service, use named registering, like:
        // builder.RegisterType<SearchHealthCheckService>().Named<IHealthCheckService>(SearchHealthCheckService.ServiceKey);

        IContainer container = builder.Build();
        app.UseAutofacMiddleware(container);
    }
}
```