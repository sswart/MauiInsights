using MauiInsights.CrashHandling;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;

namespace MauiInsights;

public static class MauiAppBuilderExtensions
{
    private static TelemetryClient? _client;
    public static MauiAppBuilder AddApplicationInsights(this MauiAppBuilder appBuilder, string appInsightsConnectionString)
    {
        SetupTelemetryClient(appBuilder, appInsightsConnectionString);
        SetupHttpDependecyTracking(appBuilder);
        SetupPageViewTelemetry();
        SetupLogger(appBuilder, appInsightsConnectionString);
        return appBuilder;
    }

    public static MauiAppBuilder AddCrashLogging(this MauiAppBuilder appBuilder, string crashLogDirectory)
    {
        var crashHandler = new CrashLogger(crashLogDirectory);
        appBuilder.Services.AddSingleton(crashHandler);
        return appBuilder;
    }

    private static void SetupTelemetryClient(MauiAppBuilder appBuilder, string appInsightsConnectionString)
    {
        var configuration = new TelemetryConfiguration()
        {
            ConnectionString = appInsightsConnectionString,
        };
        _client = new TelemetryClient(configuration);
        appBuilder.Services.AddSingleton(_client);
    }

    private static void SetupHttpDependecyTracking(MauiAppBuilder appBuilder)
    {
        appBuilder.Services.AddTransient<DependencyTrackingHandler>();
        appBuilder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IHttpMessageHandlerBuilderFilter, DependencyTrackingHandlerFilter>());
    }

    private static void SetupPageViewTelemetry()
    {
        Microsoft.Maui.Handlers.PageHandler.Mapper.AppendToMapping(nameof(IContentView.Content), (handler, view) =>
        {
            if (view is Page)
            {
                _client.TrackPageView(view.GetType().Name);
            }
        });
    }

    private static void SetupLogger(MauiAppBuilder appBuilder, string appInsightsConnectionString)
    {
        var telemetryConfig = new TelemetryConfiguration()
        {
            ConnectionString = appInsightsConnectionString
        };
        var logConfig = new ApplicationInsightsLoggerOptions()
        {
            FlushOnDispose = true,
            TrackExceptionsAsExceptionTelemetry = true
        };
        appBuilder.Logging.AddProvider(
            new ApplicationInsightsLoggerProvider(
                Options.Create(telemetryConfig), Options.Create(logConfig)));
    }
}