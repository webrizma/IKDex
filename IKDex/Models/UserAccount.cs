namespace IKDex.Models;

public sealed class UserAccount
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public long? EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}
