namespace IKDex.Models;

public sealed class AttendanceRecord
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public TimeSpan? CheckIn { get; set; }
    public TimeSpan? CheckOut { get; set; }
    public string Note { get; set; } = string.Empty;
    public string CheckInText => CheckIn?.ToString(@"hh\:mm") ?? "—";
    public string CheckOutText => CheckOut?.ToString(@"hh\:mm") ?? "—";
    public string WorkedText => CheckIn is not null && CheckOut is not null ? (CheckOut.Value - CheckIn.Value).ToString(@"hh\:mm") : "—";
}
