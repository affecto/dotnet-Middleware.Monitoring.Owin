using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Affecto.Middleware.Monitoring.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Xunit;

namespace Monitoring.Owin.Tests
{
    public class MonitoringMiddlewareTest
    {
        [Fact]
        public async void ShallowPathReturns204()
        {
            using (var server = TestServer.Create(app =>
            {
                app.Use(typeof(MockMonitoringMiddleware), "", (Func<Task>) SuccessHealthCheckAsync);
            }))
            {
                HttpResponseMessage response = await server.HttpClient.GetAsync("/_monitor/shallow");
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        [Fact]
        public async void DeepPathReturns204()
        {
            using (var server = TestServer.Create(app =>
            {
                app.Use(typeof(MockMonitoringMiddleware), "", (Func<Task>) SuccessHealthCheckAsync);
            }))
            {
                HttpResponseMessage response = await server.HttpClient.GetAsync("/_monitor/deep");
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            }
        }

        [Fact]
        public async void DeepPathReturns503()
        {
            using (var server = TestServer.Create(app =>
            {
                app.Use(typeof(MockMonitoringMiddleware), "", (Func<Task>) FailureHealthCheckAsync);
            }))
            {
                HttpResponseMessage response = await server.HttpClient.GetAsync("/_monitor/deep");
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            }
        }

        private static Task SuccessHealthCheckAsync()
        {
            return Task.CompletedTask;
        }

        private static Task FailureHealthCheckAsync()
        {
            return Task.FromException(new Exception());
        }
    }

    internal class MockMonitoringMiddleware : MonitoringMiddleware
    {
        public MockMonitoringMiddleware(OwinMiddleware next, string routePrefix, Func<Task> checkHealthAsync)
            : base(next, routePrefix, checkHealthAsync)
        {
        }
    }
}
