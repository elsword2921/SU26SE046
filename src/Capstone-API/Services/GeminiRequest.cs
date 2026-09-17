using System.Net;
using System.Text;

namespace Capstone_API.Services;

internal sealed class GeminiUnavailableException(string message, string code) : Exception(message)
{
    public string Code { get; } = code;
}

internal static class GeminiRequest
{
    internal const string OverloadedMessage = "Gemini đang quá tải, vui lòng thử lại sau. Bạn vẫn có thể tiếp tục phân loại thủ công.";

    internal static async Task<HttpResponseMessage> SendAsync(HttpClient client, string path,
        string apiKey, string payload, CancellationToken cancellationToken, TimeSpan? timeBudget = null)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeBudget ?? TimeSpan.FromSeconds(90));
        var overloaded = false;
        try
        {
            for (var attempt = 0; attempt < 3; attempt++)
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                message.Headers.Add("x-goog-api-key", apiKey);
                var response = await client.SendAsync(message, deadline.Token);
                if (response.StatusCode is not (HttpStatusCode.TooManyRequests or HttpStatusCode.InternalServerError
                    or HttpStatusCode.BadGateway or HttpStatusCode.ServiceUnavailable or HttpStatusCode.GatewayTimeout))
                    return response;

                overloaded = response.StatusCode is HttpStatusCode.ServiceUnavailable or HttpStatusCode.TooManyRequests;
                var retryAfter = response.Headers.RetryAfter;
                var requestedDelay = retryAfter?.Delta
                    ?? (retryAfter?.Date is { } date ? date - DateTimeOffset.UtcNow : TimeSpan.Zero);
                response.Dispose();
                if (attempt == 2) break;
                var delay = TimeSpan.FromMilliseconds(1000 * Math.Pow(2, attempt) + Random.Shared.Next(0, 501));
                if (requestedDelay > delay) delay = requestedDelay;
                // The shared deadline also bounds Retry-After and all retry attempts.
                await Task.Delay(delay, deadline.Token);
            }
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            if (overloaded) throw new GeminiUnavailableException(OverloadedMessage, "GEMINI_OVERLOADED");
            throw new GeminiUnavailableException("Gemini phản hồi quá lâu, vui lòng thử lại sau.", "GEMINI_TIMEOUT");
        }
        catch (HttpRequestException)
        {
            throw new GeminiUnavailableException("Không thể kết nối Gemini, vui lòng thử lại sau.", "GEMINI_UNAVAILABLE");
        }
        throw new GeminiUnavailableException(overloaded ? OverloadedMessage
            : "Gemini tạm thời không khả dụng, vui lòng thử lại sau.",
            overloaded ? "GEMINI_OVERLOADED" : "GEMINI_UNAVAILABLE");
    }
}
