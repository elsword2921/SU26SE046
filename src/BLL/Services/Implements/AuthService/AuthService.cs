using BLL.DTOs;
using BLL.Services.Interfaces.AuthService;
using DAL.Models;
using DAL.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BLL.Services.Implements.AuthService
{
    public class AuthService : IAuthService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IConfiguration _configuration;
        public AuthService(IUnitOfWork unitOfWork, IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _configuration = configuration;
        }
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _unitOfWork.UserRepository.GetWithConditionAsync(
                x => x.UserName == request.UserName,
                false,
                x => x.Role);

            if (user == null)
            {
                throw new Exception("Invalid username or password");
            }

            bool validPassword = BCrypt.Net.BCrypt.Verify(
                request.Password,
                user.PasswordHash);

            if (!validPassword)
            {
                throw new Exception("Invalid username or password");
            }

            string token = GenerateToken(user);

            return new AuthResponse
            {
                Token = token,
                ExpiredAt = DateTime.UtcNow.AddHours(2),
                UserId = user.Id,
                FullName = user.FullName,
                UserName = user.UserName,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role.RoleName
            };
        }

        public async Task RegisterAsync(RegisterRequest request)
        {
            var existedUser =
           await _unitOfWork.UserRepository.GetWithConditionAsync(
               x => x.UserName == request.UserName);

            if (existedUser != null)
            {
                throw new Exception("Username already exists");
            }

            var donorRole =
                await _unitOfWork.RoleRepository.GetWithConditionAsync(
                    x => x.RoleName == "Donor");

            var user = new User
            {
                FullName = request.FullName,
                UserName = request.UserName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                RoleId = donorRole.Id,
                Address = request.Address,
                PhoneNumber = request.PhoneNumber,
                CreateAt = DateTime.UtcNow,
            };

            await _unitOfWork.UserRepository.AddAsync(user);
            await _unitOfWork.SaveChangeAsync();
        }

        private string GenerateToken(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role.RoleName),
                new Claim("username", user.UserName)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));

            var credentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
