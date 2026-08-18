using BLL.DTOs;

namespace BLL.Services.Interfaces.AuthService;

public interface IAuthService
{
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<CurrentUserProfileDto> GetCurrentUserProfileAsync(Guid userId);
    Task<VerificationResponse> VerifyRegistrationAsync(VerifyRegistrationRequest request);
    Task ResendVerificationAsync(ResendVerificationRequest request);
    Task<ForgotPasswordResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ResetPasswordResponse> ResetPasswordAsync(ResetPasswordRequest request);
}
