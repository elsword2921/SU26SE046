using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

// This handler never opens a network connection or creates a real shipment.
sealed class FakeGhnHandler : HttpMessageHandler
{
    public ConcurrentDictionary<string, string> Orders { get; } = new();
    public string Status { get; set; } = "ready_to_pick";
    public bool RejectNext { get; set; }
    public bool TimeoutAfterCreate { get; set; }
    public int CreateAttempts;
    public JsonElement LastPayload { get; private set; }
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.GetValues("Token").Single() != "test-token" || request.Headers.GetValues("ShopId").Single() != "123")
            throw new Exception("Unexpected GHN credentials in fixture.");
        using var document = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
        if (request.RequestUri!.AbsolutePath.EndsWith("/create"))
        {
            Interlocked.Increment(ref CreateAttempts);
            LastPayload = document.RootElement.Clone();
            if (RejectNext)
            {
                RejectNext = false;
                return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { code = 400, message = "Invalid fixture shipment", data = (object?)null }) };
            }
            var key = LastPayload.GetProperty("client_order_code").GetString()!;
            var code = Orders.GetOrAdd(key, x => "GHN" + x[^12..]);
            if (TimeoutAfterCreate)
            {
                TimeoutAfterCreate = false;
                throw new TaskCanceledException("Simulated timeout after GHN accepted the request.");
            }
            return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { code = 200, data = new { order_code = code } }) };
        }
        return new(HttpStatusCode.OK) { Content = JsonContent.Create(new { code = 200, data = new { status = Status } }) };
    }
}
