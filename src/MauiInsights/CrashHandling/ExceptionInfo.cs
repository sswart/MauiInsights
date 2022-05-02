using Microsoft.ApplicationInsights.DataContracts;

namespace MauiInsights.CrashHandling
{
    public record ExceptionInfo(int ver, List<ExceptionData> exceptions, DateTimeOffset timestamp);

    public record ExceptionData(int id, int outerId, string typeName, string message, bool hasFullStack, List<StackTracePart>? parsedStack)
    {
        public ExceptionDetailsInfo Map()
        {
            return new ExceptionDetailsInfo(id, outerId, typeName, message, hasFullStack, "", parsedStack?.Select(s => s.Map()).ToList());
        }
    }
    public record StackTracePart(string assembly, string fileName, int level, int line, string method)
    {
        public StackFrame Map()
        {
            return new StackFrame(assembly, fileName, level, line, method);
        }
    }
}
