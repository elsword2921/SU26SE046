using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using DAL.Models;
using Microsoft.Extensions.Configuration;

namespace BLL.Services.Implements.OperatingFund;

public record PayOsLink(
    string Id,
    long OrderCode,
    decimal Amount,
    string Status,
    decimal Paid,
    string? CheckoutUrl
);

public interface IPayOsGateway
{
    bool Enabled { get; }
    Task<PayOsLink> CreateAsync(FundContribution contribution);
    Task<PayOsLink?> GetAsync(long orderCode);
    bool VerifyWebhook(JsonElement data, string signature);
}

public sealed class PayOsGateway(HttpClient http, IConfiguration config) : IPayOsGateway
{
    public bool Enabled =>
        bool.TryParse(config["PayOS:Enabled"], out var enabled)
        && enabled
        && new[] { "ClientId", "ApiKey", "ChecksumKey", "ReturnUrl" }.All(k =>
            !string.IsNullOrWhiteSpace(config[$"PayOS:{k}"])
        );

    public static string Signature(JsonElement data, string secret)
    {
        static object? Normalize(JsonElement value) =>
            value.ValueKind switch
            {
                JsonValueKind.Object => value
                    .EnumerateObject()
                    .OrderBy(p => p.Name, StringComparer.Ordinal)
                    .ToDictionary(p => p.Name, p => Normalize(p.Value)),
                JsonValueKind.Array => value.EnumerateArray().Select(Normalize).ToArray(),
                _ => value.Clone(),
            };
        static string Value(JsonElement value) =>
            value.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => "",
                JsonValueKind.String => value.GetString() is null or "null" or "undefined"
                    ? ""
                    : value.GetString()!,
                JsonValueKind.Array or JsonValueKind.Object => JsonSerializer.Serialize(
                    Normalize(value),
                    new JsonSerializerOptions
                    {
                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                    }
                ),
                _ => value.GetRawText(),
            };
        var input = string.Join(
            "&",
            data.EnumerateObject()
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .Select(p => $"{p.Name}={Value(p.Value)}")
        );
        return Convert
            .ToHexString(
                HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(input))
            )
            .ToLowerInvariant();
    }

    public bool VerifyWebhook(JsonElement data, string signature)
    {
        if (!Enabled || data.ValueKind != JsonValueKind.Object || signature.Length != 64)
            return false;
        if (data.EnumerateObject().GroupBy(p => p.Name).Any(g => g.Count() != 1))
            return false;
        try
        {
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(Signature(data, config["PayOS:ChecksumKey"]!)),
                Convert.FromHexString(signature)
            );
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task<JsonElement?> SendAsync(HttpMethod method, string path, object? body = null)
    {
        if (!Enabled)
            throw new InvalidOperationException(
                "Chưa cấu hình kênh thanh toán PayOS. Vui lòng quay lại sau."
            );
        using var request = new HttpRequestMessage(method, "https://api-merchant.payos.vn/" + path);
        request.Headers.Add("x-client-id", config["PayOS:ClientId"]);
        request.Headers.Add("x-api-key", config["PayOS:ApiKey"]);
        if (body != null)
            request.Content = JsonContent.Create(body);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                "Chưa kết nối được PayOS. Vui lòng thử lại; hệ thống chưa ghi nhận tiền."
            );
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        if (root.GetProperty("code").GetString() != "00")
            throw new InvalidOperationException(
                "PayOS chưa xử lý được yêu cầu. Vui lòng thử lại với cùng giao dịch."
            );
        var data = root.GetProperty("data");
        if (data.TryGetProperty("currency", out var currency) && currency.GetString() != "VND")
            throw new InvalidOperationException("PayOS trả về loại tiền tệ không hợp lệ.");
        if (
            !root.TryGetProperty("signature", out var sig)
            || !VerifyWebhook(data, sig.GetString() ?? "")
        )
            throw new InvalidOperationException(
                "Không xác minh được phản hồi PayOS. Chưa ghi nhận thanh toán."
            );
        return data.Clone();
    }

    private static PayOsLink Parse(JsonElement data) =>
        new(
            data.TryGetProperty("paymentLinkId", out var id)
                ? id.GetString()!
                : data.GetProperty("id").GetString()!,
            data.GetProperty("orderCode").GetInt64(),
            data.GetProperty("amount").GetDecimal(),
            data.GetProperty("status").GetString()!,
            data.TryGetProperty("amountPaid", out var paid) ? paid.GetDecimal() : 0,
            data.TryGetProperty("checkoutUrl", out var url) ? url.GetString() : null
        );

    public async Task<PayOsLink?> GetAsync(long orderCode)
    {
        var data = await SendAsync(HttpMethod.Get, $"v2/payment-requests/{orderCode}");
        return data.HasValue ? Parse(data.Value) : null;
    }

    public async Task<PayOsLink> CreateAsync(FundContribution contribution)
    {
        var returnUrl = config["PayOS:ReturnUrl"]!;
        if (
            !Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri)
            || uri.Scheme != "https"
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
        )
            throw new InvalidOperationException(
                "Cấu hình địa chỉ quay lại PayOS phải là HTTPS, không có query/fragment."
            );
        returnUrl += "?contribution=" + contribution.Id;
        var payload = new Dictionary<string, object>
        {
            ["amount"] = (long)contribution.Amount,
            ["cancelUrl"] = returnUrl,
            ["description"] = "RETHREADS",
            ["orderCode"] = contribution.OrderCode,
            ["returnUrl"] = returnUrl,
        };
        payload["signature"] = Signature(
            JsonSerializer.SerializeToElement(payload),
            config["PayOS:ChecksumKey"]!
        );
        payload["expiredAt"] = contribution.ExpiresAt.ToUnixTimeSeconds();
        return Parse((await SendAsync(HttpMethod.Post, "v2/payment-requests", payload))!.Value);
    }
}
