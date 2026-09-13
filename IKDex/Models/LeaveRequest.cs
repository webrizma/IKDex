namespace IKDex.Models;

public sealed class LeaveRequest
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string LeaveType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int DayCount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Bekliyor";
    public DateTime RequestedAt { get; set; }
}
