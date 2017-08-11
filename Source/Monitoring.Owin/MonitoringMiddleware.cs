using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Affecto.Middleware.Monitoring.Owin
{
    public class MonitoringMiddleware : OwinMiddleware
    {
        private readonly OwinMiddleware next;
        private readonly Func<Task> healthCheck;

        private readonly string monitorPath;
        private readonly string monitorShallowPath;
        private readonly string monitorDeepPath;

        public MonitoringMiddleware(OwinMiddleware next, string routePrefix = null, Func<Task> healthCheck = null) : base(next)
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
            this.healthCheck = healthCheck;
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
            if (context.Request.Path.ToString().Contains(monitorShallowPath))
            {
                return ShallowEndpoint(context);
            }
            if (context.Request.Path.ToString().Contains(monitorDeepPath))
            {
                return DeepEndpoint(context);
            }

            return Task.CompletedTask;
        }

        private async Task DeepEndpoint(IOwinContext context)
        {
            if (healthCheck == null)
            {
                context.Response.StatusCode = (int) HttpStatusCode.NoContent;
            }
            else
            {
                try
                {
                    await healthCheck().ConfigureAwait(false);
                    context.Response.StatusCode = (int) HttpStatusCode.NoContent;
                }
                catch (Exception e)
                {
                    string message;

                    if (e is AggregateException && e.InnerException != null)
                    {
                        message = e.InnerException.Message;
                    }
                    else
                    {
                        message = e.Message;
                    }

                    context.Response.StatusCode = (int) HttpStatusCode.ServiceUnavailable;
                    context.Response.ReasonPhrase = message;
                }
            }
        }

        private static Task ShallowEndpoint(IOwinContext context)
        {
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        }
    }
}