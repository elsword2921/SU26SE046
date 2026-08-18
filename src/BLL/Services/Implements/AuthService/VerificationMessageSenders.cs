using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using BLL.Services.Interfaces.AuthService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Implements.AuthService;

public class EmailVerificationSender(
    IConfiguration configuration,
    ILogger<EmailVerificationSender> logger) : IEmailVerificationSender
{
    public async Task SendPasswordResetAsync(string email, string recipientName, string code)
    {
        if (!bool.TryParse(configuration["Notifications:Email:Enabled"], out var enabled) || !enabled)
        {
            logger.LogWarning("DEV PASSWORD RESET OTP for {Email}: {Code}", email, code);
            return;
        }

        using var client = CreateClient();
        var safeName = WebUtility.HtmlEncode(recipientName);
        var safeCode = WebUtility.HtmlEncode(code);
        using var message = new MailMessage
        {
            From = new MailAddress(configuration["Notifications:Email:From"]!, "ReThreads", Encoding.UTF8),
            Subject = "ReThreads - Đặt lại mật khẩu",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true,
            Body = $"""
                <div style="font-family:Arial,sans-serif;max-width:560px;margin:auto;padding:32px;color:#10231d">
                  <h1 style="color:#087d5e">Đặt lại mật khẩu ReThreads</h1>
                  <p>Xin chào {safeName},</p>
                  <p>Dùng mã dưới đây để đặt lại mật khẩu. Mã có hiệu lực trong 5 phút.</p>
                  <div style="margin:24px 0;padding:20px;text-align:center;background:#f0fcf8;border:1px dashed #13b987;border-radius:14px;font-size:36px;font-weight:800;letter-spacing:8px;color:#087d5e">{safeCode}</div>
                  <p>Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này và không chia sẻ mã cho bất kỳ ai.</p>
                </div>
                """
        };
        message.To.Add(email);
        await client.SendMailAsync(message);
    }

    private SmtpClient CreateClient() => new(
        configuration["Notifications:Email:Host"],
        int.TryParse(configuration["Notifications:Email:Port"], out var port) ? port : 587)
    {
        EnableSsl = bool.TryParse(configuration["Notifications:Email:UseSsl"], out var ssl) && ssl,
        Credentials = new NetworkCredential(
            configuration["Notifications:Email:Username"],
            configuration["Notifications:Email:Password"])
    };

    public async Task SendAsync(string email, string recipientName, string code)
    {
        if (!bool.TryParse(configuration["Notifications:Email:Enabled"], out var enabled) || !enabled)
        {
            logger.LogWarning("DEV EMAIL OTP for {Email}: {Code}", email, code);
            return;
        }

        using var client = new SmtpClient(
            configuration["Notifications:Email:Host"],
            int.TryParse(configuration["Notifications:Email:Port"], out var port) ? port : 587)
        {
            EnableSsl = bool.TryParse(configuration["Notifications:Email:UseSsl"], out var ssl) && ssl,
            Credentials = new NetworkCredential(
                configuration["Notifications:Email:Username"],
                configuration["Notifications:Email:Password"])
        };

        var safeRecipientName = WebUtility.HtmlEncode(recipientName);
        var safeCode = WebUtility.HtmlEncode(code);
        var htmlBody = $$"""
            <!doctype html>
            <html lang="vi">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Xác nhận email ReThreads</title></head>
            <body style="margin:0;padding:0;background:#f3f7f5;font-family:Arial,'Helvetica Neue',sans-serif;color:#10231d;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:#f3f7f5;">
                <tr><td align="center" style="padding:36px 16px;">
                  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="max-width:600px;background:#fff;border:1px solid #dce8e3;border-radius:20px;overflow:hidden;box-shadow:0 12px 36px rgba(16,35,29,.08);">
                    <tr><td style="padding:28px 36px;background:#073d31;">
                      <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
                        <td width="50" height="50" align="center" valign="middle" style="width:50px;height:50px;border-radius:14px;background:#13c995;color:#fff;font-size:28px;font-weight:bold;line-height:50px;">&#9752;</td>
                        <td style="padding-left:14px;"><div style="font-size:25px;font-weight:800;color:#fff;">Re<span style="color:#1ee0a7;">Threads</span></div><div style="margin-top:3px;font-size:12px;letter-spacing:1.4px;text-transform:uppercase;color:#b7d8cd;">Trao đi yêu thương</div></td>
                      </tr></table>
                    </td></tr>
                    <tr><td style="padding:38px 36px 30px;">
                      <div style="display:inline-block;padding:7px 12px;border-radius:999px;background:#e6faf4;color:#087d5e;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.8px;">Xác nhận tài khoản</div>
                      <h1 style="margin:18px 0 12px;font-size:28px;line-height:1.25;color:#10231d;">Xin chào {{safeRecipientName}},</h1>
                      <p style="margin:0;color:#52675f;font-size:16px;line-height:1.7;">Cảm ơn bạn đã đăng ký tài khoản ReThreads. Sử dụng mã dưới đây để xác nhận địa chỉ email của bạn:</p>
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:28px 0;"><tr><td align="center" style="padding:24px;border:1px dashed #13b987;border-radius:16px;background:#f0fcf8;">
                        <div style="font-size:12px;font-weight:700;letter-spacing:1.5px;text-transform:uppercase;color:#598078;">Mã xác nhận</div>
                        <div style="margin-top:10px;font-family:'Courier New',monospace;font-size:38px;line-height:1;font-weight:800;letter-spacing:9px;color:#087d5e;">{{safeCode}}</div>
                        <div style="margin-top:14px;font-size:13px;color:#6b7f78;">Mã có hiệu lực trong <strong>5 phút</strong></div>
                      </td></tr></table>
                      <p style="margin:0;color:#6b7f78;font-size:14px;line-height:1.65;">Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email. Không chia sẻ mã xác nhận với bất kỳ ai.</p>
                    </td></tr>
                    <tr><td style="padding:22px 36px;border-top:1px solid #e5eeea;background:#fbfdfc;color:#789087;font-size:12px;line-height:1.6;">Email này được gửi tự động bởi ReThreads.<br>Cùng nhau kéo dài vòng đời quần áo và lan tỏa những điều tử tế.</td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;

        using var message = new MailMessage
        {
            From = new MailAddress(configuration["Notifications:Email:From"]!, "ReThreads", Encoding.UTF8),
            Subject = "ReThreads - Xác nhận địa chỉ email",
            SubjectEncoding = Encoding.UTF8,
            BodyEncoding = Encoding.UTF8,
            IsBodyHtml = true
        };
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            $"Xin chào {recipientName},\n\nMã xác nhận email ReThreads của bạn là: {code}\nMã có hiệu lực trong 5 phút.\n\nKhông chia sẻ mã này với bất kỳ ai.\n\nReThreads",
            Encoding.UTF8, MediaTypeNames.Text.Plain));
        message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
            htmlBody, Encoding.UTF8, MediaTypeNames.Text.Html));
        message.To.Add(email);
        await client.SendMailAsync(message);
    }
}
