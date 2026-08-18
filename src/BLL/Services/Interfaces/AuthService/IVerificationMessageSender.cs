namespace BLL.Services.Interfaces.AuthService;

public interface IEmailVerificationSender
{
    Task SendAsync(string email, string recipientName, string code);
    Task SendPasswordResetAsync(string email, string recipientName, string code);
}
