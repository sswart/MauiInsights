using MauiInsights.CrashHandling;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.AspNetCore;
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
    private static SessionId? _sessionId;
    public static MauiAppBuilder AddApplicationInsights(this MauiAppBuilder appBuilder, string appInsightsConnectionString, Action<TelemetryConfiguration>? configureTelemetry = null) => AddApplicationInsights(appBuilder, new MauiInsightsConfiguration { ApplicationInsightsConnectionString = appInsightsConnectionString }, configureTelemetry);

    public static MauiAppBuilder AddApplicationInsights(this MauiAppBuilder appBuilder, MauiInsightsConfiguration configuration, Action<TelemetryConfiguration>? configureTelemetry = null)
    {
        if (string.IsNullOrEmpty(configuration.ApplicationInsightsConnectionString))
        {
            throw new ArgumentException("Configuration must have a valid Application Insights Connection string", nameof(configuration));
        }
        configureTelemetry ??= _ => { };
        _sessionId = new SessionId();
        appBuilder.Services.AddSingleton(_sessionId);
        SetupTelemetryClient(appBuilder, configuration, configureTelemetry);
        SetupHttpDependecyTracking(appBuilder);
        SetupPageViewTelemetry();
        SetupLogger(appBuilder, configuration);
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

    public static MauiAppBuilder AddCrashLogging(this MauiAppBuilder appBuilder) => AddCrashLogging(appBuilder, new EssentialsConnectivity(), FileSystem.CacheDirectory);

    public static MauiAppBuilder AddCrashLogging(this MauiAppBuilder appBuilder, string crashlogDirectory) => AddCrashLogging(appBuilder, new EssentialsConnectivity(), crashlogDirectory);

    public static MauiAppBuilder AddCrashLogging(this MauiAppBuilder appBuilder, IConnectivity connectivity) => AddCrashLogging(appBuilder, connectivity, FileSystem.CacheDirectory);

    public static MauiAppBuilder AddCrashLogging(this MauiAppBuilder appBuilder, IConnectivity connectivity, string crashlogDirectory)
    {
        var crashLogger = new CrashLogger(new CrashLogSettings(crashlogDirectory), _client);
        appBuilder.Services.AddSingleton(crashLogger);
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => crashLogger.LogToFileSystem(e.ExceptionObject as Exception);
        TaskScheduler.UnobservedTaskException += (sender, e) => crashLogger.LogToFileSystem(e.Exception);
        
        SetupCrashlogLifecycleEvents(appBuilder, connectivity, crashLogger);
        return appBuilder;
    }

    private static void SetupCrashlogLifecycleEvents(MauiAppBuilder appBuilder, IConnectivity connectivity, CrashLogger crashLogger)
    {
        appBuilder.ConfigureLifecycleEvents(builder =>
        {
#if ANDROID
				builder.AddAndroid(androidBuilder =>
				{
					androidBuilder.OnStart(activity => SendCrashes(connectivity, crashLogger));
				});
#elif IOS
                builder.AddiOS(ios => ios
                        .WillFinishLaunching((app, dict) =>
                        {
                            SendCrashes(connectivity, crashLogger);
                            return true;
                        }));

#elif WINDOWS
                builder.AddWindows(windows => windows
                          .OnLaunched((window, args) => SendCrashes(connectivity, crashLogger)));
#endif
        });
    }

    private static void SendCrashes(IConnectivity connectivity, CrashLogger crashLogger)
    {
        Task.Run(async () =>
        {
            if (await connectivity.HasInternetConnection() && _client != null)
            {
                var crashes = crashLogger.GetCrashLog();
                await foreach(var crash in crashes)
                {
                    _client.TrackException(crash);
                }
                crashLogger.ClearCrashLog();
            }
        });
    }

    private static void SetupTelemetryClient(MauiAppBuilder appBuilder, MauiInsightsConfiguration configuration, Action<TelemetryConfiguration> configureTelemetry)
    {
        var telemetryConfiguration = GetTelemetryConfiguration(appBuilder, configuration);
        configureTelemetry(telemetryConfiguration);
        _client = new TelemetryClient(telemetryConfiguration);
        _client.Context.Session.Id = _sessionId?.Value;
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
                _client?.TrackPageView(view.GetType().Name);
            }
        });
    }

    private static void SetupLogger(MauiAppBuilder appBuilder, MauiInsightsConfiguration configuration)
    {
        var telemetryConfig = GetTelemetryConfiguration(appBuilder, configuration);
        var logConfig = new ApplicationInsightsLoggerOptions()
        {
            FlushOnDispose = true,
            TrackExceptionsAsExceptionTelemetry = true,
        };
        
        appBuilder.Logging.AddProvider(
            new ApplicationInsightsLoggerProvider(
                Options.Create(telemetryConfig), Options.Create(logConfig)));
    }

    private static TelemetryConfiguration GetTelemetryConfiguration(MauiAppBuilder builder, MauiInsightsConfiguration configuration)
    {
        var telemetryConfig = new TelemetryConfiguration()
        {
            ConnectionString = configuration.ApplicationInsightsConnectionString,
        };
        telemetryConfig.TelemetryInitializers.Add(new AdditionalPropertiesInitializer(configuration.AdditionalTelemetryProperties));
        telemetryConfig.TelemetryInitializers.Add(new ApplicationInfoInitializer());
        telemetryConfig.TelemetryInitializers.Add(new SessionInfoInitializer(_sessionId));
        
        foreach(var initializer in configuration.TelemetryInitializers)
        {
            telemetryConfig.TelemetryInitializers.Add(initializer);
        }

        var telemetryInitializers = builder.Services
            .Where(s => s.ServiceType == typeof(ITelemetryInitializer) && (s.ImplementationInstance ?? s.KeyedImplementationInstance) is ITelemetryInitializer)
            .Select(s => s.ImplementationInstance ?? s.KeyedImplementationInstance)
            .Cast<ITelemetryInitializer>()
            .ToList();
        foreach(var initializer in telemetryInitializers)
        {
            telemetryConfig.TelemetryInitializers.Add(initializer);
        }

        var telemetryProcessors =
            builder.Services.Where(s => s.ServiceType == typeof(ITelemetryProcessorFactory)).ToList();
        
        var serviceProvider = builder.Services.BuildServiceProvider();
        
        foreach (var processorDescriptor in telemetryProcessors)
        {
            if (processorDescriptor.ImplementationFactory is not Func<IServiceProvider, ITelemetryProcessorFactory>
                factoryMethod) continue;
            var factory = factoryMethod(serviceProvider);
            telemetryConfig.TelemetryProcessorChainBuilder.Use(factory.Create);
        }
        return telemetryConfig;
    }
}

public record SessionId
{
    public string Value { get; } = Guid.NewGuid().ToString();
};