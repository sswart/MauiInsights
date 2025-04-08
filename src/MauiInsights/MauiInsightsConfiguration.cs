using Microsoft.ApplicationInsights.Extensibility;

namespace MauiInsights;

public class MauiInsightsConfiguration
{
    public string? ApplicationInsightsConnectionString { get; set; }
    public bool EnableW3CCorrelation { get; set; } = true;
    public IDictionary<string, string> AdditionalTelemetryProperties { get; set; } = new Dictionary<string, string>();
    [Obsolete("Use the AddApplicationInsights with the configureTelemetry parameter instead to add ITelemetryInitializers")]
    public IEnumerable<ITelemetryInitializer> TelemetryInitializers { get; set; } = Enumerable.Empty<ITelemetryInitializer>();
}
