using IKDex.Models;

namespace IKDex.Services;

public sealed class AuthorizationService(UserSession session)
{
    public bool IsAdministrator => session.Role == "İK Yöneticisi";
    public bool CanManageEmployees => session.Role is "İK Yöneticisi" or "İK Uzmanı";
    public bool CanManageOrganization => session.Role == "İK Yöneticisi";
    public bool CanReviewLeave => session.Role is "İK Yöneticisi" or "İK Uzmanı";
    public bool CanManageUsers => session.Role == "İK Yöneticisi";
    public bool CanManagePayroll => session.Role == "İK Yöneticisi";
}
