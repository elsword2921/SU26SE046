using System.Net;
using System.Text;
using System.Text.Json;
using BLL.Services.Implements.OperatingFund;
using DAL.Models;
using Microsoft.Extensions.Configuration;

internal static class GatewayContractChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> {
            ["PayOS:Enabled"]="true", ["PayOS:ClientId"]="client-test", ["PayOS:ApiKey"]="api-test",
            ["PayOS:ChecksumKey"]="secret", ["PayOS:ReturnUrl"]="https://example.test/fund"
        }).Build();
        var handler = new Handler(); var gateway = new PayOsGateway(new HttpClient(handler),config);
        var row = new FundContribution {Id=Guid.NewGuid(),OrderCode=20260922000001,Amount=12345,ExpiresAt=DateTimeOffset.UtcNow.AddMinutes(30)};
        var link = await gateway.CreateAsync(row);
        check(link.OrderCode==row.OrderCode && link.Amount==row.Amount,"signed create response accepted");
        var sent=handler.Body!.Value;
        var fields=new[]{"amount","cancelUrl","description","orderCode","returnUrl"}.ToDictionary(k=>k,k=>sent.GetProperty(k));
        check(PayOsGateway.Signature(JsonSerializer.SerializeToElement(fields),"secret")==sent.GetProperty("signature").GetString(),"only five checkout fields signed");
        check(sent.GetProperty("amount").GetRawText()=="12345" && sent.GetProperty("returnUrl").GetString()!.EndsWith(row.Id.ToString()),"whole VND and internal return identifier");
        check(handler.HeadersValid,"server credentials only to fixed PayOS HTTPS endpoint");
        var info=await gateway.GetAsync(row.OrderCode);
        check(info?.Paid==12345 && info.Status=="PAID","signed GET uses amountPaid");
        handler.Tamper=true;
        try { await gateway.GetAsync(row.OrderCode);throw new Exception("tampered response accepted"); } catch(InvalidOperationException) {check(true,"tampered provider response rejected");}
        handler.Tamper=false;handler.Currency="USD";
        try { await gateway.CreateAsync(row);throw new Exception("foreign currency accepted"); } catch(InvalidOperationException) {check(true,"wrong provider currency rejected");}
    }
    private sealed class Handler : HttpMessageHandler
    {
        public JsonElement? Body; public bool HeadersValid; public bool Tamper; public string Currency="VND";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            HeadersValid=request.RequestUri!.Host=="api-merchant.payos.vn" && request.RequestUri.Scheme=="https" && request.Headers.GetValues("x-client-id").Single()=="client-test" && request.Headers.GetValues("x-api-key").Single()=="api-test";
            JsonElement data;
            if(request.Method==HttpMethod.Post) {
                Body=JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token)).RootElement.Clone();
                data=JsonSerializer.SerializeToElement(new {paymentLinkId="link",orderCode=20260922000001,amount=12345,status="PENDING",currency=Currency,checkoutUrl="https://pay.payos.vn/web/link"});
            } else data=JsonSerializer.SerializeToElement(new {id="link",orderCode=20260922000001,amount=12345,amountPaid=12345,status="PAID",transactions=new[]{new{amount=12345,reference="test"}}});
            return new HttpResponseMessage(HttpStatusCode.OK) {Content=new StringContent(JsonSerializer.Serialize(new{code="00",data,signature=Tamper?new string('0',64):PayOsGateway.Signature(data,"secret")}),Encoding.UTF8,"application/json")};
        }
    }
}
