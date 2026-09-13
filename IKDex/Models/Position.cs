namespace IKDex.Models;

public sealed class Position
{
    public long Id { get; set; }
    public long DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}
