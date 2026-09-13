namespace IKDex.Models;

public sealed class EmployeeDocument
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string StoredPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsArchived { get; set; }
    public string FileSizeText => FileSize < 1024 * 1024 ? $"{FileSize / 1024d:0.0} KB" : $"{FileSize / 1024d / 1024d:0.0} MB";
    public string ExpiryText => ExpiryDate?.ToString("dd.MM.yyyy") ?? "—";
}
