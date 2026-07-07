using BLL.DTOs;
using BLL.Services.Interfaces.AuthService;
using Microsoft.AspNetCore.Mvc;

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
            await _authService.RegisterAsync(request);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<AuthResponse> Login(LoginRequest request)
        {
            return await _authService.LoginAsync(request);
        }
    }
}
