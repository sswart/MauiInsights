using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Http;

namespace MauiInsights
{
    public class DependencyTrackingHandler : DelegatingHandler
    {
        private readonly TelemetryClient _client;

        public DependencyTrackingHandler(TelemetryClient client)
        {
            _client = client;
        }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var telemetry = GetTelemetry(request);
            SetOpenTelemetryHeaders(request, telemetry);
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

        private static DependencyTelemetry GetTelemetry(HttpRequestMessage request)
        {
            var host = request.RequestUri?.Host ?? "Unknown url";
            var call = request.RequestUri?.AbsolutePath ?? "Unknown url";
            var operationId = Guid.NewGuid().ToString().Replace("-", "");
            var telemetry = new DependencyTelemetry("Http", host, call, "");
            telemetry.Context.Operation.Id = operationId;
            return telemetry;
        }

        private void Enrich(DependencyTelemetry telemetry, HttpResponseMessage? response)
        {
            telemetry.Success = response != null;
            telemetry.ResultCode = response?.StatusCode.ToString();
        }

        private void SetOpenTelemetryHeaders(HttpRequestMessage request, DependencyTelemetry telemetry)
        {
            var parentId = telemetry.Context.Operation.Id;
            var traceId = telemetry.Id;
            var version = "00";
            var flags = _client.Context.Flags;

            var headerValue = $"{version}-{parentId}-{traceId}-{flags}";
            request.Headers.Add("traceparent", headerValue);
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
