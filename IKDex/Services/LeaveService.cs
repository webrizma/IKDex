using IKDex.Models;

namespace IKDex.Services;

public sealed class LeaveService(DatabaseService databaseService)
{
    public IReadOnlyList<LeaveRequest> GetAll(string? status = null, long? employeeId = null)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT l.Id, l.EmployeeId, e.FirstName || ' ' || e.LastName, l.LeaveType, l.StartDate,
                   l.EndDate, l.DayCount, l.Reason, l.Status, l.RequestedAtUtc
            FROM LeaveRequests l JOIN Employees e ON e.Id=l.EmployeeId
            WHERE ($status='' OR l.Status=$status) AND ($employeeId IS NULL OR l.EmployeeId=$employeeId)
            ORDER BY CASE l.Status WHEN 'Bekliyor' THEN 0 ELSE 1 END, l.RequestedAtUtc DESC;
            """;
        command.Parameters.AddWithValue("$status", status ?? string.Empty);
        command.Parameters.AddWithValue("$employeeId", employeeId is null ? DBNull.Value : employeeId.Value);
        using var reader = command.ExecuteReader(); var result = new List<LeaveRequest>();
        while (reader.Read()) result.Add(new LeaveRequest {
            Id=reader.GetInt64(0), EmployeeId=reader.GetInt64(1), EmployeeName=reader.GetString(2), LeaveType=reader.GetString(3),
            StartDate=DateTime.Parse(reader.GetString(4)), EndDate=DateTime.Parse(reader.GetString(5)), DayCount=reader.GetInt32(6),
            Reason=reader.GetString(7), Status=reader.GetString(8), RequestedAt=DateTime.Parse(reader.GetString(9)) });
        return result;
    }

    public void Create(long employeeId, string type, DateTime start, DateTime end, string reason)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var overlap = connection.CreateCommand();
        overlap.CommandText = """
            SELECT COUNT(*) FROM LeaveRequests WHERE EmployeeId=$employeeId AND Status IN ('Bekliyor','Onaylandı')
            AND StartDate <= $end AND EndDate >= $start;
            """;
        overlap.Parameters.AddWithValue("$employeeId",employeeId); overlap.Parameters.AddWithValue("$start",start.ToString("yyyy-MM-dd")); overlap.Parameters.AddWithValue("$end",end.ToString("yyyy-MM-dd"));
        if (Convert.ToInt32(overlap.ExecuteScalar()) > 0) throw new InvalidOperationException("Bu personelin seçilen tarihlerle çakışan bir izin talebi bulunuyor.");
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO LeaveRequests(EmployeeId, LeaveType, StartDate, EndDate, DayCount, Reason, Status, RequestedAtUtc)
            VALUES($employeeId,$type,$start,$end,$days,$reason,'Bekliyor',$requestedAt);
            """;
        command.Parameters.AddWithValue("$employeeId",employeeId); command.Parameters.AddWithValue("$type",type);
        command.Parameters.AddWithValue("$start",start.ToString("yyyy-MM-dd")); command.Parameters.AddWithValue("$end",end.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$days",CountWorkingDays(start,end)); command.Parameters.AddWithValue("$reason",reason.Trim());
        command.Parameters.AddWithValue("$requestedAt",DateTime.UtcNow.ToString("O")); command.ExecuteNonQuery();
    }

    public void Review(long id, string status, long reviewerUserId)
    {
        if (status is not ("Onaylandı" or "Reddedildi")) throw new ArgumentOutOfRangeException(nameof(status));
        using var connection = databaseService.CreateConnection(); connection.Open(); using var command = connection.CreateCommand();
        command.CommandText = "UPDATE LeaveRequests SET Status=$status, ReviewedAtUtc=$reviewedAt, ReviewedByUserId=$reviewer WHERE Id=$id AND Status='Bekliyor';";
        command.Parameters.AddWithValue("$status",status); command.Parameters.AddWithValue("$reviewedAt",DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$reviewer",reviewerUserId); command.Parameters.AddWithValue("$id",id); command.ExecuteNonQuery();
    }

    public int GetPendingCount() => GetCount("SELECT COUNT(*) FROM LeaveRequests WHERE Status='Bekliyor';");
    public int GetTodayApprovedCount() => GetCount("SELECT COUNT(*) FROM LeaveRequests WHERE Status='Onaylandı' AND StartDate <= date('now','localtime') AND EndDate >= date('now','localtime');");
    private int GetCount(string sql) { using var connection=databaseService.CreateConnection(); connection.Open(); using var command=connection.CreateCommand(); command.CommandText=sql; return Convert.ToInt32(command.ExecuteScalar()); }

    public static int CountWorkingDays(DateTime start, DateTime end)
    {
        var count=0; for(var day=start.Date;day<=end.Date;day=day.AddDays(1)) if(day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) count++; return count;
    }
}
