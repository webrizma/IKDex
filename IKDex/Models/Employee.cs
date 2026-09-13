namespace IKDex.Models;

public sealed class Employee
{
    public long Id { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string DisplayName => FullName.ToUpperInvariant();
    public string DisplayDepartment => Department.ToUpperInvariant();
    public string DisplayPosition => Position.ToUpperInvariant();
    public string Initials
    {
        get
        {
            var first = string.IsNullOrWhiteSpace(FirstName) ? string.Empty : FirstName.Trim()[0].ToString();
            var last = string.IsNullOrWhiteSpace(LastName) ? string.Empty : LastName.Trim()[0].ToString();
            return (first + last).ToUpperInvariant();
        }
    }
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string MaritalStatus { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public string MotherName { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string EmergencyContactName { get; set; } = string.Empty;
    public string EmergencyContactPhone { get; set; } = string.Empty;
    public string EducationLevel { get; set; } = string.Empty;
    public string BloodType { get; set; } = string.Empty;
    public string EmploymentType { get; set; } = string.Empty;
    public decimal GrossSalary { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;
    public string StatusText => IsActive ? "Aktif" : "Pasif";
}
