namespace IKDex.Models;

public sealed class PayrollRecord
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime Period { get; set; }
    public decimal GrossSalary { get; set; }
    public decimal Bonus { get; set; }
    public decimal OvertimePay { get; set; }
    public decimal SocialSecurityDeduction { get; set; }
    public decimal UnemploymentDeduction { get; set; }
    public decimal IncomeTax { get; set; }
    public decimal StampTax { get; set; }
    public decimal OtherDeduction { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "Taslak";
    public decimal TotalEarnings => GrossSalary + Bonus + OvertimePay;
    public decimal TotalDeductions => SocialSecurityDeduction + UnemploymentDeduction + IncomeTax + StampTax + OtherDeduction;
    public decimal NetSalary => TotalEarnings - TotalDeductions;
    public string PeriodText => Period.ToString("MMMM yyyy");
}
