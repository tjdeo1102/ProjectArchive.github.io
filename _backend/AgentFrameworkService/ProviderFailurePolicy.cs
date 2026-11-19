using System.Net;

namespace MafiaSimulation.AgentFrameworkService;

internal static class ProviderFailurePolicy
{
    public static bool IsTransient(Exception exception)
    {
        if (exception is TimeoutException or OperationCanceledException or System.Net.Sockets.SocketException) return true;
        if (exception is HttpRequestException http)
            return http.StatusCode == null || http.StatusCode == HttpStatusCode.TooManyRequests || (int)http.StatusCode >= 500;
        if (exception is AggregateException aggregate) return aggregate.InnerExceptions.Any(IsTransient);
        return exception.InnerException != null && IsTransient(exception.InnerException);
    }
}
