namespace IKDex.Models;

public sealed class CompanySettings
{
    public string CompanyName { get; set; } = "IKDex Şirketi";
    public string TaxNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string UpdateRepositoryUrl { get; set; } = string.Empty;
}
