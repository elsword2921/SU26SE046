using System.Security.Authentication;
using System.Text;
using BLL.Services.Implements.OperatingFund;
using DAL;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

internal static class StatementChecks
{
    public static async Task Run(Action<bool,string> check)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database=ReThreadsStatementChecks_{Guid.NewGuid():N};Integrated Security=true;TrustServerCertificate=true").Options;
        var config = new ConfigurationBuilder().Build(); var clock = new TestClock();
        User User(string role) => new() { Id = Guid.NewGuid(), UserName = Guid.NewGuid().ToString("N"), Email = Guid.NewGuid()+"@example.test", FullName=role, EmailConfirmed=true, IsActive=true, UserStatus="Active", Role=new Role{Id=Guid.NewGuid(),RoleName=role} };
        var manager = User("Manager"); var second = User("Manager"); var inactive = User("Manager"); inactive.IsActive=false;
        var donor = User("Donor"); var organization = User("CharityOrganization"); var staff = User("WarehouseStaff");
        async Task<T> Run<T>(Func<FundStatementService,Task<T>> action) { await using var db=new AppDbContext(options); return await action(new(db,clock,config)); }
        async Task Do(Func<FundStatementService,Task> action) => await Run(async s=>{await action(s);return true;});
        async Task Denied(Func<FundStatementService,Task> action,string label) {
            try { await Do(action); } catch(Exception e) when(e is AuthenticationException or InvalidOperationException) {check(true,label);return;} throw new Exception("Expected rejection: "+label);
        }
        async Task<int> Notices() {await using var db=new AppDbContext(options);return await db.Notifications.CountAsync();}
        var pdf=Encoding.ASCII.GetBytes("%PDF-1.7\nTest statement\n%%EOF");
        var input=new CreateFundStatement(Guid.NewGuid(),"2026-09",200000,"Month-end balance");
        try
        {
            await using(var db=new AppDbContext(options)){await db.Database.EnsureCreatedAsync();db.AddRange(manager,second,inactive,donor,organization,staff);await db.SaveChangesAsync();}
            clock.Now=DateTimeOffset.Parse("2026-09-30T16:59:59Z");
            await Do(s=>s.RemindManagers());check(await Notices()==0,"no statement reminder before month closes in Vietnam");
            await Denied(s=>s.Publish(manager.Id,input,pdf),"open month statement rejected");
            clock.Now=DateTimeOffset.Parse("2026-09-30T17:00:00Z");
            await Task.WhenAll(Do(s=>s.RemindManagers()),Do(s=>s.RemindManagers()));
            check(await Notices()==2,"Vietnam month boundary reminds each active manager exactly once across concurrent workers");
            var status=await Run(s=>s.Status(manager.Id));
            check(status.MissingMonths.SequenceEqual(new[]{"2026-09"}) && status.LatestClosedMonth=="2026-09","monthly due status uses previous Vietnam month");
            await using(var db=new AppDbContext(options)){check(await db.Set<FundStatementReminder>().CountAsync()==2,"reminder delivery markers persisted");await db.Notifications.ExecuteDeleteAsync();}
            await Do(s=>s.RemindManagers());check(await Notices()==0,"clearing notifications and restarting worker cannot resend same reminder");
            await Denied(s=>s.Publish(donor.Id,input,pdf),"donor cannot publish statement");
            await Denied(s=>s.Status(donor.Id),"due manager status restricted");
            await Denied(s=>s.Publish(inactive.Id,input,pdf),"inactive manager cannot publish");
            await Denied(s=>s.Publish(manager.Id,input with{Month="2026-10"},pdf),"current month rejected after boundary");
            await Denied(s=>s.Publish(manager.Id,input with{Month="2026-08"},pdf),"pre-launch period rejected");
            await Denied(s=>s.Publish(manager.Id,input with{ClosingBalance=-1},pdf),"negative reported balance rejected");
            await Denied(s=>s.Publish(manager.Id,input with{ClosingBalance=1.5m},pdf),"fractional VND statement rejected");
            await Denied(s=>s.Publish(manager.Id,input,Encoding.ASCII.GetBytes("<html>fake</html>")),"statement file validation enforced");
            await Denied(s=>s.Publish(manager.Id,input,new byte[5*1024*1024+1]),"oversized statement rejected");
            var ids=await Task.WhenAll(Run(s=>s.Publish(manager.Id,input,pdf)),Run(s=>s.Publish(manager.Id,input,pdf)));
            check(ids[0]==ids[1],"concurrent retry creates one monthly statement");
            await Denied(s=>s.Publish(second.Id,input with{RequestKey=Guid.NewGuid()},pdf),"another manager cannot duplicate a month");
            await Denied(s=>s.Publish(manager.Id,input with{ClosingBalance=10},pdf),"same request key cannot silently change a statement");
            check((await Run(s=>s.Status(manager.Id))).MissingMonths.Count==0,"published month removes reminder banner");
            check((await Run(s=>s.List(donor.Id,1))).Items.Single().ClosingBalance==200000,"donor sees published closing balance");
            check((await Run(s=>s.Document(organization.Id,ids[0]))).Data.SequenceEqual(pdf),"authenticated organization downloads original statement");
            await Denied(s=>s.Document(staff.Id,ids[0]),"staff cannot download statements");
            await Denied(s=>s.Document(Guid.NewGuid(),ids[0]),"unknown user cannot download statements");
            await Do(s=>s.RemindManagers());check(await Notices()==0,"published month does not emit new reminders");
            await using(var db=new AppDbContext(options)){check(await db.Set<FundContribution>().CountAsync()==0 && await db.Set<FundExpense>().CountAsync()==0,"bank statement does not alter operating ledger");}
            clock.Now=DateTimeOffset.Parse("2026-11-01T00:00:00+07:00");await Do(s=>s.RemindManagers());
            check(await Notices()==2,"next month gets fresh reminder");
            var october=await Run(s=>s.Publish(manager.Id,input with{RequestKey=Guid.NewGuid(),Month="2026-10",ClosingBalance=0},pdf));
            check(october!=Guid.Empty,"zero closing balance accepted");
            clock.Now=DateTimeOffset.Parse("2027-02-01T00:00:00+07:00");await Do(s=>s.RemindManagers());
            check((await Run(s=>s.Status(manager.Id))).MissingMonths.SequenceEqual(new[]{"2026-11","2026-12","2027-01"}),"missed months caught up across year boundary");
            check(await Notices()==4,"catch-up combines missing periods into one notification per manager");
        }
        finally {await using var db=new AppDbContext(options);await db.Database.EnsureDeletedAsync();}
    }
    private sealed class TestClock : TimeProvider {public DateTimeOffset Now; public override DateTimeOffset GetUtcNow()=>Now;}
}
