using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Http;

namespace MauiInsights
{
    public class DependencyTrackingHandler : DelegatingHandler
    {
        private readonly TelemetryClient _client;
        private readonly MauiInsightsConfiguration _configuration;

        public DependencyTrackingHandler(TelemetryClient client, MauiInsightsConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var telemetry = GetTelemetry(request);
            SetCorrelationHeaders(request, telemetry);
            telemetry.Start();
            HttpResponseMessage? response = null;
            try
            {
                response = await base.SendAsync(request, cancellationToken);
                return response;
            }
            finally
            {
                telemetry.Stop();
                Enrich(telemetry, response);
                _client.TrackDependency(telemetry);
            }
        }

        private DependencyTelemetry GetTelemetry(HttpRequestMessage request)
        {
            var host = request.RequestUri?.Host ?? "Unknown url";
            var call = request.RequestUri?.AbsolutePath ?? "Unknown url";
            var operationId = Guid.NewGuid().ToString().Replace("-", "");
            var telemetry = new DependencyTelemetry("Http", host, call, "");
            telemetry.Context.Operation.Id = operationId;
            telemetry.Context.Session.Id = _client.Context.Session.Id;
            return telemetry;
        }

        private void Enrich(DependencyTelemetry telemetry, HttpResponseMessage? response)
        {
            int statusCode = Convert.ToInt32(response?.StatusCode);
            telemetry.Success = (statusCode > 0) && (statusCode < 400);
            telemetry.ResultCode = statusCode > 0 ? statusCode.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private void SetCorrelationHeaders(HttpRequestMessage request, DependencyTelemetry telemetry)
        {
            var parentId = telemetry.Context.Operation.Id;
            var traceId = telemetry.Id;
            
            if (_configuration.UseOpenTelemetryHeaders)
            {
                var version = "00";
                var flags = _client.Context.Flags;
                request.Headers.Add("traceparent", $"{version}-{parentId}-{traceId}-{flags}");
            }
            else
            {
                request.Headers.Add("Request-Id", $"|{parentId}.{traceId}.");
            }
        }
    }

    internal class DependencyTrackingHandlerFilter : IHttpMessageHandlerBuilderFilter
    {
        private readonly IServiceProvider _serviceProvider;
        public DependencyTrackingHandlerFilter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
        {
            return (builder) =>
            {
                next(builder);

                if (_serviceProvider.GetService(typeof(DependencyTrackingHandler)) is DependencyTrackingHandler handler)
                {
                    builder.AdditionalHandlers.Add(handler);
                }
            };
        }
    }
}
