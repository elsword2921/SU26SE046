namespace BLL.DTOs;

public record RegisterResponse(Guid UserId, string Message);

public class VerifyRegistrationRequest
{
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    public Guid UserId { get; set; }
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public record ForgotPasswordResponse(string Message);

public record ResetPasswordResponse(string Message);

public record VerificationResponse(
    bool EmailConfirmed,
    bool AccountActivated,
    string Message);
