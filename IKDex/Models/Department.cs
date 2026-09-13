namespace IKDex.Models;

public sealed class Department
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}
