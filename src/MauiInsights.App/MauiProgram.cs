using MauiInsights;
using Refit;

namespace MauiInsights.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<TestApp>()
			.AddApplicationInsights("InstrumentationKey=65926b9d-34fa-4f47-9474-9f6186ac623e;IngestionEndpoint=https://westeurope-5.in.applicationinsights.azure.com/;LiveEndpoint=https://westeurope.livediagnostics.monitor.azure.com/;ApplicationId=55c38abb-35b9-4952-9d8d-800b4f32dc3b")
			.AddCrashLogging()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
			.Services
			.AddSingleton<MainPage>()
			.AddRefitClient<IRestClient>()
			.ConfigureHttpClient(c => c.BaseAddress = new Uri("https://jsonplaceholder.typicode.com"));

		return builder.Build();
	}
}
