using BLL.DTOs;

namespace BLL.Services.Interfaces.AuthService
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
    }
}
