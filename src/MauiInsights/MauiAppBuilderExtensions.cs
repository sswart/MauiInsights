using MauiInsights.CrashHandling;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;
using Microsoft.Maui.LifecycleEvents;

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
        SetupTelemetryLifecycleEvents(appBuilder);
        return appBuilder;
    }

    private static void SetupTelemetryLifecycleEvents(MauiAppBuilder appBuilder)
    {
        appBuilder.ConfigureLifecycleEvents(builder =>
         {
#if ANDROID
				builder.AddAndroid(androidBuilder =>
				{
					androidBuilder.OnDestroy(activity => _client?.Flush());
				});
#elif IOS
                builder.AddiOS(ios => ios
                        .WillTerminate(app => _client?.Flush()));

#elif WINDOWS
                builder.AddWindows(windows => windows
                          .OnClosed((window, args) => _client?.Flush()));
#endif
         });
    }

    public static MauiAppBuilder AddCrashLogging(this MauiAppBuilder appBuilder, IConnectivity connectivity, string crashlogDirectory)
    {
        var crashHandler = new CrashLogger(crashlogDirectory);
        appBuilder.Services.AddSingleton(crashHandler);

        SetupCrashlogLifecycleEvents(appBuilder, connectivity, crashlogDirectory);
        return appBuilder;
    }

    private static void SetupCrashlogLifecycleEvents(MauiAppBuilder appBuilder, IConnectivity connectivity, string crashlogDirectory)
    {
        appBuilder.ConfigureLifecycleEvents(builder =>
        {
#if ANDROID
				builder.AddAndroid(androidBuilder =>
				{
					androidBuilder.OnStart(activity => SendCrashes(connectivity, crashlogDirectory));
				});
#elif IOS
                builder.AddiOS(ios => ios
                        .WillFinishLaunching((app, dict) => SendCrashes(connectivity, crashlogDirectory)));

#elif WINDOWS
                builder.AddWindows(windows => windows
                          .OnLaunched((window, args) => SendCrashes(connectivity, crashlogDirectory)));
#endif
        });
    }

    private static void SendCrashes(IConnectivity connectivity, string crashlogDirectory)
    {
        Task.Run(async () =>
        {
            if (await connectivity.HasInternetConnection() && _client != null)
            {
                var crashes = CrashLogger.GetCrashLog(crashlogDirectory);
                await foreach(var crash in crashes)
                {
                    _client.TrackException(crash);
                }
                CrashLogger.ClearCrashLog(crashlogDirectory);
            }
        });
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