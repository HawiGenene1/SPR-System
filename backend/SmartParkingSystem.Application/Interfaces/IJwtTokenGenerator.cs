using System.Security.Claims;
using SmartParkingSystem.Domain.Entities;

namespace SmartParkingSystem.Application.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
