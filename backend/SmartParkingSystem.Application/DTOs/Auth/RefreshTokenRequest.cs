using System.ComponentModel.DataAnnotations;

namespace SmartParkingSystem.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "Token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
