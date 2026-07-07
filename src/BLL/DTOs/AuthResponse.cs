namespace BLL.DTOs
{
    public class AuthResponse
    {
        public string Token { get; set; }
        public DateTime ExpiredAt { get; set; }
        public Guid UserId { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string? AvatarUrl { get; set; }
        public string Role { get; set; }
    }
}
