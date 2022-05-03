using Microsoft.ApplicationInsights.DataContracts;
using System.Text.Json;

namespace MauiInsights.CrashHandling
{
    internal class CrashLogger
    {
        private const string CrashLogExtension = ".crashlog";
        private readonly string _crashlogDirectory;
        public CrashLogger(string crashlogDirectory)
        {
            EnsureCanWrite(crashlogDirectory);
            _crashlogDirectory = crashlogDirectory;
        }

        private static void EnsureCanWrite(string crashlogDirectory)
        {
            var path = Path.Combine(crashlogDirectory, $"testfile{CrashLogExtension}");
            File.WriteAllText(path, "test");
            File.Delete(path);
        }

        public void LogToFileSystem(Exception? e)
        {
            var telemetry = new ExceptionTelemetry(e)
            {
                Timestamp = DateTimeOffset.UtcNow
            };

            var path = Path.Combine(_crashlogDirectory, $"{Guid.NewGuid()}{CrashLogExtension}");
            using var writer = new StreamWriter(File.OpenWrite(path));
            var jsonWriter = new JsonSerializationWriter(writer);
            jsonWriter.WriteStartObject();
            telemetry.SerializeData(jsonWriter);
            jsonWriter.WriteProperty(nameof(ExceptionInfo.timestamp), telemetry.Timestamp);
            jsonWriter.WriteEndObject();
        }

        private static IEnumerable<string> GetCrashLogFiles(string crashlogDirectory) => Directory.GetFiles(crashlogDirectory).Where(f => f.EndsWith(CrashLogExtension));
        public async IAsyncEnumerable<ExceptionTelemetry> GetCrashLog()
        {
            foreach(var file in GetCrashLogFiles(_crashlogDirectory))
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

        public void ClearCrashLog()
        {
            foreach(var file in GetCrashLogFiles(_crashlogDirectory))
            {
                File.Delete(file);
            }
        }
    }
}
