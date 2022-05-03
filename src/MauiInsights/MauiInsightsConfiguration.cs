using Microsoft.ApplicationInsights.Extensibility;

namespace MauiInsights;

public class MauiInsightsConfiguration
{
    public string? ApplicationInsightsConnectionString { get; set; }
    public IDictionary<string, string> AdditionalTelemetryProperties { get; set; } = new Dictionary<string, string>();
    public IEnumerable<ITelemetryInitializer> TelemetryInitializers { get; set; } = Enumerable.Empty<ITelemetryInitializer>();
}
