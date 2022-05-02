using Microsoft.ApplicationInsights.DataContracts;
using System.Text.Json;

namespace MauiInsights.CrashHandling
{
    internal class CrashLogger
    {
        public CrashLogger(string crashlogDirectory)
        {
            EnsureCanWrite(crashlogDirectory);

            AppDomain.CurrentDomain.UnhandledException += (sender, e) => LogToFileSystem(e.ExceptionObject as Exception, crashlogDirectory);
            TaskScheduler.UnobservedTaskException += (sender, e) => LogToFileSystem(e.Exception, crashlogDirectory);
        }

        private static void EnsureCanWrite(string crashlogDirectory)
        {
            var path = Path.Combine(crashlogDirectory, $"testfile{CrashLogExtension}");
            File.WriteAllText(path, "test");
            File.Delete(path);
        }

        private const string CrashLogExtension = ".crashlog";

        private static void LogToFileSystem(Exception? e, string crashlogDirectory)
        {
            var telemetry = new ExceptionTelemetry(e)
            {
                Timestamp = DateTimeOffset.UtcNow
            };

            var path = Path.Combine(crashlogDirectory, $"{Guid.NewGuid()}{CrashLogExtension}");
            using var writer = new StreamWriter(File.OpenWrite(path));
            var jsonWriter = new JsonSerializationWriter(writer);
            jsonWriter.WriteStartObject();
            telemetry.SerializeData(jsonWriter);
            jsonWriter.WriteProperty(nameof(ExceptionInfo.timestamp), telemetry.Timestamp);
            jsonWriter.WriteEndObject();
        }

        private static IEnumerable<string> GetCrashLogFiles(string crashlogDirectory) => Directory.GetFiles(crashlogDirectory).Where(f => f.EndsWith(CrashLogExtension));
        public static async IAsyncEnumerable<ExceptionTelemetry> GetCrashLog(string crashlogDirectory)
        {
            foreach(var file in GetCrashLogFiles(crashlogDirectory))
            {
                using var stream = File.OpenRead(file);
                var exceptionInfo = await JsonSerializer.DeserializeAsync<ExceptionInfo>(stream);
                if (exceptionInfo == null)
                {
                    continue;
                }
                yield return new ExceptionTelemetry(exceptionInfo.exceptions.Select(e => e.Map()), SeverityLevel.Error, "", new Dictionary<string, string>(), new Dictionary<string, double>())
                {
                    Timestamp = exceptionInfo.timestamp
                };
            }
        }

        public static void ClearCrashLog(string crashlogDirectory)
        {
            foreach(var file in GetCrashLogFiles(crashlogDirectory))
            {
                File.Delete(file);
            }
        }
    }
}
