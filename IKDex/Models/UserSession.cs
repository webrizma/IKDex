namespace IKDex.Models;

public sealed record UserSession(long Id, string UserName, string FullName, string Role, long? EmployeeId);
