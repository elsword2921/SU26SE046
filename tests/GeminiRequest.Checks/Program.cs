using System.Net;
using System.Net.Http.Headers;
using Capstone_API.Services;

static void Check(bool value, string message)
{
    if (!value) throw new Exception(message);
    Console.WriteLine($"PASS: {message}");
}

var calls = 0;
using (var client = Client(async (request, token) =>
{
    Check(await request.Content!.ReadAsStringAsync(token) == "payload", "retry preserves request body");
    Check(request.Headers.GetValues("x-goog-api-key").Single() == "test-key", "retry preserves authentication");
    return new HttpResponseMessage(++calls < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
}))
{
    using var response = await Send(client);
    Check(response.IsSuccessStatusCode && calls == 3, "503 retries twice then recovers");
}

foreach (var status in new[] { HttpStatusCode.ServiceUnavailable, HttpStatusCode.TooManyRequests, HttpStatusCode.BadGateway })
{
    calls = 0;
    using var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(status)); });
    try { using var response = await Send(client); throw new Exception("Expected failure"); }
    catch (GeminiUnavailableException error)
    {
        Check(calls == 3, $"{status} stops after three attempts");
        Check(error.Code == (status == HttpStatusCode.BadGateway ? "GEMINI_UNAVAILABLE" : "GEMINI_OVERLOADED"), "error identifies overload separately");
        if (status == HttpStatusCode.ServiceUnavailable) Check(error.Message.Contains("Gemini đang quá tải"), "overload has a Vietnamese message");
    }
}

foreach (var status in new[] { HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden })
{
    calls = 0;
    using var client = Client((_, _) => { calls++; return Task.FromResult(new HttpResponseMessage(status)); });
    using var response = await Send(client);
    Check(calls == 1 && response.StatusCode == status, $"{status} is not retried");
}

calls = 0;
using (var client = Client((_, _) =>
{
    calls++;
    var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
    return Task.FromResult(response);
}))
{
    try { using var response = await Send(client, budget: TimeSpan.FromMilliseconds(100)); throw new Exception("Expected deadline"); }
    catch (GeminiUnavailableException error) { Check(calls == 1 && error.Code == "GEMINI_OVERLOADED", "deadline bounds Retry-After without early retry"); }
}

using (var client = Client(async (_, token) => { await Task.Delay(10000, token); return new HttpResponseMessage(HttpStatusCode.OK); }))
{
    try { using var response = await Send(client, budget: TimeSpan.FromMilliseconds(100)); throw new Exception("Expected timeout"); }
    catch (GeminiUnavailableException error) { Check(error.Code == "GEMINI_TIMEOUT", "timeout is not mislabeled overload"); }
    using var cancel = new CancellationTokenSource(100);
    try { using var response = await Send(client, cancel.Token); throw new Exception("Expected cancellation"); }
    catch (OperationCanceledException) { Check(cancel.IsCancellationRequested, "caller cancellation propagates"); }
}
Console.WriteLine("All Gemini request checks passed.");

static HttpClient Client(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send)
    => new(new Handler(send)) { BaseAddress = new Uri("https://gemini.test/") };
static Task<HttpResponseMessage> Send(HttpClient client, CancellationToken token = default, TimeSpan? budget = null)
    => GeminiRequest.SendAsync(client, "generate", "test-key", "payload", token, budget);
sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => send(request, cancellationToken);
}
