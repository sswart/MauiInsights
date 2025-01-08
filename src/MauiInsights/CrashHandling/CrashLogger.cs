using Microsoft.ApplicationInsights.DataContracts;
using System.Text.Json;
using Microsoft.ApplicationInsights;

namespace MauiInsights.CrashHandling
{
    internal record CrashLogSettings(string Directory);
    
    internal class CrashLogger
    {
        private readonly SessionId? _sessionId;
        private const string CrashLogExtension = ".crashlog";
        private readonly string _crashlogDirectory;
        public CrashLogger(CrashLogSettings settings, SessionId? sessionId)
        {
            _sessionId = sessionId;
            EnsureCanWrite(settings.Directory);
            _crashlogDirectory = settings.Directory;
        }

        private static void EnsureCanWrite(string crashlogDirectory)
        {
            var path = Path.Combine(crashlogDirectory, $"testfile{CrashLogExtension}");
            File.WriteAllText(path, "test");
            File.Delete(path);
        }

        public void LogToFileSystem(Exception? e)
        {
            try
            {
                var telemetry = new ExceptionTelemetry(e)
                {
                    Timestamp = DateTimeOffset.UtcNow,
                };
                telemetry.Context.Session.Id = _sessionId?.Value;
                
                var path = Path.Combine(_crashlogDirectory, $"{Guid.NewGuid()}{CrashLogExtension}");
                using var writer = new StreamWriter(File.OpenWrite(path));
                var jsonWriter = new JsonSerializationWriter(writer);
                jsonWriter.WriteStartObject();
                telemetry.SerializeData(jsonWriter);
                jsonWriter.WriteProperty(nameof(ExceptionInfo.timestamp), telemetry.Timestamp);
                jsonWriter.WriteProperty(nameof(ExceptionInfo.sessionId), telemetry.Context.Session.Id);
                jsonWriter.WriteEndObject();
            }
            catch
            {
                // Swallow any exceptions.
                // We are already in the context of handling an exception so just do nothing instead.
            }
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
                var telemetry = new ExceptionTelemetry(exceptionInfo.exceptions.Select(e => e.Map()), SeverityLevel.Error, "", new Dictionary<string, string>(), new Dictionary<string, double>())
                {
                    Timestamp = exceptionInfo.timestamp
                };
                telemetry.Context.Session.Id = exceptionInfo.sessionId;
                yield return telemetry;
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
