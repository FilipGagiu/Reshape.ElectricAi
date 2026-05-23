namespace Reshape.ElectricAi.Presentation.Middleware;

internal static class ErrorEnvelope
{
    public static object Simple(string code, string message) =>
        new { error = new { code, message } };

    public static object WithDetails(string code, string message, object details) =>
        new { error = new { code, message, details } };
}
