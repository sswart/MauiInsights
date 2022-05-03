# Maui Insights
Maui Insights gives you a simple way to add Application Insights telemetry to your .NET Maui app.

To use it, simply call .AddApplicationInsights() with your AI connection string, and optionally call .AddCrashLogging() to also log any uncaught exceptions to Application Insights. See the example below:
```c#
using MauiInsights;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.AddApplicationInsights("<connection string>")
			.AddCrashLogging();

		return builder.Build();
	}
}
```

## What does it do?
Maui Insights provides the following:
### Application Insights sink for ILogger
Similar to default implementations in .NET Web applications.
### Automatic page view tracking
Whenever a page is visited, a pageView event is sent to Application Insights
### Automatic http dependency tracking
When using HttpClientFactory, any Http calls are automatically tracked as dependency calls. In addition, OpenTelemetry headers are added to the request so calls can be correlated across your applications.
This includes libraries like Refit.

When manually constructing HttpClient or HttpMessageHandler instances, the DependencyTrackingHandler class can be used to manually add this functionality.
### Crash logging
When using .AddCrashLogging(), any uncaught exceptions are written to the platforms default cache directory. On the next app start, if the device has an internet connection, these crash logs are then sent to Application Insights as exceptions.
### Application Insights TelemetryClient in DI Container
An singleton instance of TelemetryClient is registered and available for any manual telemetry


## Advanced scenarios
If you want to extend your telemetry, use the .AddApplicationInsights() overload that accepts a MauiInsightsConfiguration. You can either add custom keyvalue pairs which will be added to the additional properties of the telemetry, or you can implement your own instances of ITelemetryInitializer to modify any telemetry that is sent.