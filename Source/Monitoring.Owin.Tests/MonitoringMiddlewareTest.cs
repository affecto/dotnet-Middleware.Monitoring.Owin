using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Affecto.Middleware.Monitoring.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Testing;
using Xunit;
using NSubstitute;

namespace Monitoring.Owin.Tests
{
    public class MonitoringMiddlewareTest
    {
        [Fact]
        public async void ShallowPathReturns204()
        {
            using (var server = TestServer.Create(app =>
            {
                app.Use(typeof(MockMonitoringMiddleware), null, null);
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
                app.Use(typeof(MockMonitoringMiddleware), "", GetHealthCheckServiceFactoryFor(SuccessHealthCheckAsync));
            }))
            {
                HttpResponseMessage response = await server.HttpClient.GetAsync("/_monitor/deep");
                var msg = await response.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Equal(string.Empty, msg);
            }
        }

        [Fact]
        public async void DeepPathReturns503()
        {
            using (var server = TestServer.Create(app =>
            {
                app.Use(typeof(MockMonitoringMiddleware), "", GetHealthCheckServiceFactoryFor(FailureHealthCheckAsync));
            }))
            {
                HttpResponseMessage response = await server.HttpClient.GetAsync("/_monitor/deep");
                var msg = await response.Content.ReadAsStringAsync();
                Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
                Assert.NotEqual(string.Empty, msg);
            }
        }

        private static Func<IHealthCheckService> GetHealthCheckServiceFactoryFor(Func<Task> checkHealthAsync)
        {
            var mockHealthCheckService = Substitute.For<IHealthCheckService>();
            mockHealthCheckService.CheckHealthAsync().Returns(checkHealthAsync());
            Func<IHealthCheckService> HealthCheckServiceFactory = () => mockHealthCheckService;
            return HealthCheckServiceFactory;
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
        public MockMonitoringMiddleware(OwinMiddleware next, string routePrefix = null, Func<IHealthCheckService> healthCheckServiceFactory = null)
            : base(next, routePrefix, healthCheckServiceFactory)
        {
        }
    }
}
