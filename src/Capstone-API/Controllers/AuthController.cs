using BLL.DTOs;
using BLL.Services.Interfaces.AuthService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Capstone_API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            return Ok(await _authService.RegisterAsync(request));
        }

        [HttpPost("verify-registration")]
        public async Task<IActionResult> VerifyRegistration(VerifyRegistrationRequest request) =>
            Ok(await _authService.VerifyRegistrationAsync(request));

        [HttpPost("resend-verification")]
        public async Task<IActionResult> ResendVerification(ResendVerificationRequest request)
        {
            await _authService.ResendVerificationAsync(request);
            return Ok(new { message = "A new verification code was sent." });
        }

        [HttpPost("login")]
        public async Task<AuthResponse> Login(LoginRequest request)
        {
            return await _authService.LoginAsync(request);
        }

        [HttpPost("forgot-password")]
        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request) =>
            await _authService.ForgotPasswordAsync(request);

        [HttpPost("reset-password")]
        public async Task<ResetPasswordResponse> ResetPassword(ResetPasswordRequest request) =>
            await _authService.ResetPasswordAsync(request);

        [Authorize]
        [HttpGet("me")]
        public async Task<CurrentUserProfileDto> Me()
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            return await _authService.GetCurrentUserProfileAsync(userId);
        }
    }
}
