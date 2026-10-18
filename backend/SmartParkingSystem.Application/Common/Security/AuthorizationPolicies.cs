namespace SmartParkingSystem.Application.Common.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string ParkingAttendant = "ParkingAttendant";
    public const string Customer = "Customer";
}

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireAttendant = "RequireAttendant";
    public const string RequireCustomer = "RequireCustomer";
    public const string RequireStaff = "RequireStaff"; // Admin or ParkingAttendant
}
