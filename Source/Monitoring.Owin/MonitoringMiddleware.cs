using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Affecto.Middleware.Monitoring.Owin
{
    public class MonitoringMiddleware : OwinMiddleware
    {
        private readonly OwinMiddleware next;
        private readonly Func<IHealthCheckService> healthCheckServiceFactory;

        private readonly string monitorPath;
        private readonly string monitorShallowPath;
        private readonly string monitorDeepPath;

        public MonitoringMiddleware(OwinMiddleware next, string routePrefix = null, Func<IHealthCheckService> healthCheckServiceFactory = null) : base(next)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (string.IsNullOrWhiteSpace(routePrefix))
            {
                routePrefix = string.Empty;
            }
            else if (!routePrefix.StartsWith("/"))
            {
                routePrefix = "/" + routePrefix;
            }

            monitorPath = routePrefix + "/_monitor";
            monitorShallowPath = routePrefix + "/_monitor/shallow";
            monitorDeepPath = routePrefix + "/_monitor/deep";

            this.next = next;
            this.healthCheckServiceFactory = healthCheckServiceFactory;
        }

        public override Task Invoke(IOwinContext context)
        {
            if (context.Request.Path.ToString().Contains(monitorPath))
            {
                return HandleMonitorEndpoint(context);
            }

            return next.Invoke(context);
        }

        private Task HandleMonitorEndpoint(IOwinContext context)
        {
            if (context.Request.Path.ToString().Contains(monitorDeepPath))
            {
                return DeepEndpoint(context);
            }

            return ShallowEndpoint(context);
        }

        private async Task DeepEndpoint(IOwinContext context)
        {
            if (healthCheckServiceFactory == null)
            {
                context.Response.StatusCode = (int) HttpStatusCode.NoContent;
            }
            else
            {
                try
                {
                    await healthCheckServiceFactory().CheckHealthAsync().ConfigureAwait(false);
                    context.Response.StatusCode = (int) HttpStatusCode.NoContent;
                }
                catch (Exception e)
                {
                    string message;

                    if (e is AggregateException && e.InnerException != null)
                    {
                        message = e.InnerException.ToString();
                    }
                    else
                    {
                        message = e.ToString();
                    }

                    context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
                    context.Response.ContentType = "text/plain";
                    await context.Response.Body.WriteAsync(Encoding.UTF8.GetBytes(message), 0, message.Length);
                }
            }
        }

        private static Task ShallowEndpoint(IOwinContext context)
        {
            context.Response.StatusCode = (int) HttpStatusCode.NoContent;
            return Task.CompletedTask;
        }
    }
}