using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace MauiInsights;

internal class SessionInfoInitializer : ITelemetryInitializer
{
    private readonly string? _sessionId;

    public SessionInfoInitializer(SessionId? sessionId)
    {
        _sessionId = sessionId?.Value;
    }
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ExceptionTelemetry) return;
        if (string.IsNullOrEmpty(telemetry.Context.Session.Id))
        {
            telemetry.Context.Session.Id = _sessionId;
        }

    }
}