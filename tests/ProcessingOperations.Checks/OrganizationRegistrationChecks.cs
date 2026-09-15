using BLL.DTOs;
using BLL.Services.Implements.AuthService;
using BLL.Services.Implements.ManagerAccounts;
using BLL.Services.Interfaces.AuthService;
using DAL;
using DAL.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Security.Authentication;

internal static class OrganizationRegistrationChecks
{
    public static async Task Run(Func<AppDbContext> createDb)
    {
        await using var db = createDb();
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Supabase:Url"] = "https://storage-check.supabase.co",
            ["Supabase:Bucket"] = "donation-images",
            ["Jwt:Key"] = "registration-check-only-key-with-at-least-32-characters",
            ["Jwt:Issuer"] = "checks", ["Jwt:Audience"] = "checks"
        }).Build();
        var sender = new CapturingEmailSender();
        var service = new AuthService(new UnitOfWork(db), db, config, sender);
        var sequence = 0;
        RegisterRequest Request(string role = "CharityOrganization") => new()
        {
            AccountType = role, FullName = "Registration check organization",
            UserName = "register_check_" + ++sequence, Email = $"register{sequence}@example.test",
            PhoneNumber = $"098765{sequence:0000}", Address = "123 Registration check address",
            Password = "Registration123!", RepresentativeName = "Test representative", TaxCode = "CHECK-123",
            CertificateImageUrl = "https://storage-check.supabase.co/storage/v1/object/public/donation-images/organization-certificates/123456-aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa.png"
        };
        void Check(bool condition, string label)
        {
            if (!condition) throw new Exception("FAIL: " + label);
            Console.WriteLine("PASS: " + label);
        }
        async Task Reject(RegisterRequest request, string label)
        {
            var before = await db.Users.CountAsync();
            try { await service.RegisterAsync(request); }
            catch (InvalidOperationException)
            {
                Check(await db.Users.CountAsync() == before, label);
                return;
            }
            throw new Exception("FAIL: expected rejection: " + label);
        }

        foreach (var role in new[] { "Manager", "WarehouseStaff", "Unknown", "" })
            await Reject(Request(role), "public registration rejects role " + role);
        var invalid = Request(); invalid.RepresentativeName = " ";
        await Reject(invalid, "organization requires representative");
        invalid = Request(); invalid.TaxCode = null;
        await Reject(invalid, "organization requires registration number");
        invalid = Request(); invalid.CertificateImageUrl = null;
        await Reject(invalid, "organization requires certificate");
        foreach (var url in new[] {
            "https://attacker.test/certificate.png", "javascript:alert(1)",
            "https://storage-check.supabase.co/storage/v1/object/public/donation-images/donations/certificate.png",
            "https://storage-check.supabase.co/storage/v1/object/public/donation-images/organization-certificates/certificate.svg" })
        {
            invalid = Request(); invalid.CertificateImageUrl = url;
            await Reject(invalid, "organization rejects untrusted certificate URL");
        }

        foreach (var role in new[] { "CharityOrganization", "RecyclingOrganization", "DisposalOrganization", "Donor" })
        {
            var request = Request(role);
            var registration = await service.RegisterAsync(request);
            var user = await db.Users.Include(x => x.Role).SingleAsync(x => x.Id == registration.UserId);
            Check(user.Role.RoleName == role && !user.EmailConfirmed && user.IsActive == false && user.UserStatus == "PendingVerification",
                role + " registration awaits email verification");
            if (role == "Donor") Check(user.CertificateImageUrl is null && user.RepresentativeName is null && user.TaxCode is null, "donor registration ignores organization fields");
            else Check(user.FullName == request.FullName && user.RepresentativeName == request.RepresentativeName && user.CertificateImageUrl == request.CertificateImageUrl, "organization identity and certificate persist");

            try { await service.LoginAsync(new() { UserName = request.UserName, Password = request.Password }); throw new Exception("Unverified login was accepted"); }
            catch (AuthenticationException) { Check(true, "unverified organization or donor cannot log in"); }
            var verified = await service.VerifyRegistrationAsync(new() { UserId = user.Id, Code = sender.Codes[request.Email] });
            Check(verified.AccountActivated && verified.EmailConfirmed, "existing OTP flow activates " + role);
            var login = await service.LoginAsync(new() { UserName = request.UserName, Password = request.Password });
            Check(login.Role == role && !string.IsNullOrEmpty(login.Token), "login retains correct organization role");
            var profile = await service.GetCurrentUserProfileAsync(user.Id);
            Check(profile.CertificateImageUrl == user.CertificateImageUrl && profile.RepresentativeName == user.RepresentativeName, "profile returns registration details");
            if (role != "Donor")
            {
                var accounts = await new ManagerAccountService(db).SearchAsync(null, role, request.FullName, 1, 100);
                Check(accounts.Items.Any(x => x.Id == user.Id && x.CertificateImageUrl == request.CertificateImageUrl), "manager can inspect organization certificate");
            }
            await Reject(request, "duplicate registration is rejected");
            user.IsActive = false;
            await db.SaveChangesAsync();
            try { await service.LoginAsync(new() { UserName = request.UserName, Password = request.Password }); throw new Exception("Inactive login was accepted"); }
            catch (AuthenticationException) { Check(true, "inactive account cannot log in"); }
        }
    }

    private sealed class CapturingEmailSender : IEmailVerificationSender
    {
        public Dictionary<string, string> Codes { get; } = new();
        public Task SendAsync(string email, string recipientName, string code) { Codes[email] = code; return Task.CompletedTask; }
        public Task SendPasswordResetAsync(string email, string recipientName, string code) => Task.CompletedTask;
    }
}
