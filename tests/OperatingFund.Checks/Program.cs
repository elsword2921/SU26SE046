using BLL.Services.Implements.OperatingFund;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Never reads application configuration or connects to production.
var database = "ReThreadsFundChecks_" + Guid.NewGuid().ToString("N");
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true").Options;
AppDbContext Db() => new(options);
var gateway = new FakeGateway();
async Task<T> Run<T>(Func<OperatingFundService, Task<T>> action) { await using var db = Db(); return await action(new(db, gateway)); }
async Task Do(Func<OperatingFundService, Task> action) => await Run(async s => { await action(s); return true; });
int checks = 0;
void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); checks++; Console.WriteLine("PASS " + label); }
async Task Denied(Func<OperatingFundService, Task> action, string label) {
    try { await Do(action); } catch(Exception e) when (e is AuthenticationException or InvalidOperationException) { Check(true, label); return; }
    throw new Exception("FAIL expected rejection: " + label);
}
User User(string role) => new() { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString("N"), Email = Guid.NewGuid()+"@example.test", FullName = role, UserStatus = "Active", EmailConfirmed = true, IsActive = true, Role = new Role { Id = Guid.NewGuid(), RoleName = role } };
var donor = User("Donor"); var org = User("CharityOrganization"); var manager = User("Manager"); var staff = User("WarehouseStaff"); var pending = User("RecyclingOrganization"); pending.UserStatus = "PendingApproval";
var proof = Encoding.ASCII.GetBytes("%PDF-1.7\nTest receipt only\n%%EOF");
CreateFundExpense Expense(decimal amount) => new(Guid.NewGuid(), amount, "Transport", "Clothes pickup transport", DateOnly.FromDateTime(DateTime.UtcNow));
try {
    await GatewayContractChecks.Run(Check);
    await StatementChecks.Run(Check);
    await using (var db = Db()) { await db.Database.EnsureCreatedAsync(); db.AddRange(donor, org, manager, staff, pending); await db.SaveChangesAsync(); }
    Check((await Run(s=>s.Summary(donor.Id))).Balance == 0, "empty fund");
    await Denied(s=>s.Summary(staff.Id), "staff denied");
    await Denied(s=>s.Summary(pending.Id), "unapproved organization denied");
    await Denied(s=>s.Checkout(manager.Id,new(Guid.NewGuid(),50000)), "manager cannot contribute");
    await Denied(s=>s.Checkout(donor.Id,new(Guid.NewGuid(),999)), "minimum amount");
    await Denied(s=>s.Checkout(donor.Id,new(Guid.NewGuid(),1000.5m)), "fractional VND denied");
    gateway.Enabled = false; await Denied(s=>s.Checkout(donor.Id,new(Guid.NewGuid(),50000)), "unconfigured payment denied"); gateway.Enabled = true;
    var input = new CreateContribution(Guid.NewGuid(),100000);
    var c = await Run(s=>s.Checkout(donor.Id,input));
    var again = await Run(s=>s.Checkout(donor.Id,input));
    Check(c.Id == again.Id && gateway.Creates == 1, "checkout idempotency");
    Check(c.OrderCode > 0 && c.OrderCode < 9007199254740991, "safe unique order code");
    await Denied(s=>s.Checkout(donor.Id,input with {Amount=50000}), "idempotency amount mismatch denied");
    Check((await Run(s=>s.Summary(org.Id))).Balance == 0, "pending payment never credits fund");
    Check((await Run(s=>s.Mine(org.Id,1))).Total == 0, "private contribution history");
    await Denied(s=>s.AllContributions(org.Id,1), "organization cannot read all contributions");
    Check((await Run(s=>s.AllContributions(manager.Id,1))).Total==1,"manager sees fund receipts");
    await Denied(s=>s.Refresh(org.Id,c.Id), "cross-account reconciliation denied");
    JsonElement Hook(string sig = "valid", string currency = "VND") => JsonSerializer.SerializeToElement(new { data = new { orderCode=c.OrderCode, currency, paymentLinkId=gateway.Links[c.OrderCode].Id }, signature=sig });
    await Denied(s=>s.Webhook(Hook("invalid")), "forged webhook rejected");
    await Denied(s=>s.Webhook(Hook(currency:"USD")), "wrong webhook currency rejected");
    gateway.Links[c.OrderCode] = gateway.Links[c.OrderCode] with {Paid=50000};
    await Do(s=>s.Webhook(Hook())); Check((await Run(s=>s.Summary(donor.Id))).Balance == 0, "partial transfer not booked");
    gateway.Links[c.OrderCode] = gateway.Links[c.OrderCode] with {Status="PAID"};
    await Denied(s=>s.Refresh(donor.Id,c.Id), "paid amount mismatch rejected");
    gateway.Links[c.OrderCode] = gateway.Links[c.OrderCode] with {Paid=100000};
    await Task.WhenAll(Do(s=>s.Webhook(Hook())), Do(s=>s.Webhook(Hook())), Run(s=>s.Refresh(donor.Id,c.Id)));
    var summary = await Run(s=>s.Summary(manager.Id)); Check(summary.Balance == 100000 && summary.Contributions == 1, "concurrent callback credits exactly once");
    await Do(s=>s.Webhook(JsonSerializer.SerializeToElement(new {data=new{orderCode=123},signature="valid"})));
    Check((await Run(s=>s.Summary(donor.Id))).Balance==100000,"signed registration sample ignored");
    await Denied(s=>s.Publish(donor.Id,Expense(1000),proof), "only manager publishes");
    await Denied(s=>s.Publish(manager.Id,Expense(100001),proof), "overspending rejected");
    await Denied(s=>s.Publish(manager.Id,Expense(1000),Encoding.UTF8.GetBytes("<script>evil</script>")), "unsafe evidence rejected");
    var spend = Expense(10000); var expenseId = await Run(s=>s.Publish(manager.Id,spend,proof));
    Check(await Run(s=>s.Publish(manager.Id,spend,proof)) == expenseId,"expense idempotency");
    await Denied(s=>s.Publish(manager.Id,spend with {Amount=11000},proof),"expense key conflict rejected");
    var results = await Task.WhenAll(Enumerable.Range(0,2).Select(async _ => { try { await Run(s=>s.Publish(manager.Id,Expense(60000),proof)); return true; } catch(InvalidOperationException) {return false;} }));
    Check(results.Count(x=>x)==1 && (await Run(s=>s.Summary(donor.Id))).Balance==30000,"concurrent spending cannot overdraw");
    Check((await Run(s=>s.Receipt(org.Id,expenseId))).Data.SequenceEqual(proof),"authenticated organization evidence download");
    await Denied(s=>s.Receipt(staff.Id,expenseId),"staff cannot download evidence");
    await Denied(s=>s.Void(manager.Id,expenseId,""),"void requires reason");
    await Do(s=>s.Void(manager.Id,expenseId,"Duplicate entry")); await Do(s=>s.Void(manager.Id,expenseId,"Second attempt"));
    Check((await Run(s=>s.Summary(org.Id))).Balance==40000,"void restores balance once");
    var audit=(await Run(s=>s.ExpenseList(org.Id,1))).Items.Single(e=>e.Id==expenseId);
    Check(audit.VoidedAt!=null && audit.VoidReason=="Duplicate entry" && audit.Amount==10000,"void retains public audit trail");
    gateway.FailAfterCreate=true;
    var recovered=await Run(s=>s.Checkout(org.Id,new(Guid.NewGuid(),5000)));
    Check(recovered.CheckoutUrl!=null,"lost create response recovered by order code");
    var expiredKey=Guid.NewGuid();
    await using(var db=Db()) {db.Add(new FundContribution{Id=Guid.NewGuid(),UserId=org.Id,RequestKey=expiredKey,Amount=5000,CreatedAt=DateTimeOffset.UtcNow.AddHours(-1),ExpiresAt=DateTimeOffset.UtcNow.AddMinutes(-1)});await db.SaveChangesAsync();}
    Check((await Run(s=>s.Checkout(org.Id,new(expiredKey,5000)))).Status=="Expired","expired failed checkout allows client to start a new attempt");
    var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["PayOS:Enabled"]="true", ["PayOS:ClientId"]="test", ["PayOS:ApiKey"]="test", ["PayOS:ChecksumKey"]="secret", ["PayOS:ReturnUrl"]="https://example.test/fund" }).Build();
    var real=new PayOsGateway(new HttpClient(),config);
    var data=JsonSerializer.SerializeToElement(new {orderCode=123,description="RETHREADS",amount=1000,cancelUrl="https://example.test/fund",returnUrl="https://example.test/fund"});
    var canonical="amount=1000&cancelUrl=https://example.test/fund&description=RETHREADS&orderCode=123&returnUrl=https://example.test/fund";
    var signature=Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"),Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    Check(real.VerifyWebhook(data,signature),"PayOS alphabetic HMAC contract");
    Check(!real.VerifyWebhook(data,new string('0',64)),"HMAC tamper detection");
    Check(!real.VerifyWebhook(JsonDocument.Parse("{\"amount\":1,\"amount\":2}").RootElement,signature),"duplicate signed keys rejected");
    Console.WriteLine($"ALL {checks} FUND CHECKS PASSED");
} finally { await using var cleanup=Db(); await cleanup.Database.EnsureDeletedAsync(); }

sealed class FakeGateway : IPayOsGateway {
    public bool Enabled {get;set;}=true;
    public int Creates; public bool FailAfterCreate;
    public System.Collections.Concurrent.ConcurrentDictionary<long,PayOsLink> Links=new();
    public Task<PayOsLink> CreateAsync(FundContribution c) {
        Creates++; var link=new PayOsLink(Guid.NewGuid().ToString("N"),c.OrderCode,c.Amount,"PENDING",0,"https://pay.payos.vn/web/test"); Links[c.OrderCode]=link;
        if(FailAfterCreate) {FailAfterCreate=false;throw new HttpRequestException("Simulated lost response");}
        return Task.FromResult(link);
    }
    public Task<PayOsLink?> GetAsync(long code)=>Task.FromResult(Links.TryGetValue(code,out var link)?link:null);
    public bool VerifyWebhook(JsonElement data,string signature)=>signature=="valid";
}
