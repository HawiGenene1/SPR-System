using SmartParkingSystem.Application.DTOs.Auth;
using SmartParkingSystem.Application.DTOs.Users;

namespace SmartParkingSystem.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshTokenAsync(RefreshTokenRequest request);
    Task<bool> LogoutAsync(Guid userId);
    Task<UserDto> GetCurrentUserAsync(Guid userId);
}
