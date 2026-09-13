using IKDex.Models;

namespace IKDex.Services;

public sealed class AttendanceService(DatabaseService databaseService)
{
    public IReadOnlyList<AttendanceRecord> GetMonth(DateTime month, long? employeeId=null)
    {
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();
        cmd.CommandText="""
            SELECT a.Id,a.EmployeeId,e.FirstName||' '||e.LastName,a.WorkDate,a.CheckIn,a.CheckOut,a.Note
            FROM Attendance a JOIN Employees e ON e.Id=a.EmployeeId
            WHERE strftime('%Y-%m',a.WorkDate)=$month AND ($employeeId IS NULL OR a.EmployeeId=$employeeId)
            ORDER BY a.WorkDate DESC,e.FirstName,e.LastName;
            """;
        cmd.Parameters.AddWithValue("$month",month.ToString("yyyy-MM"));cmd.Parameters.AddWithValue("$employeeId",employeeId is null?DBNull.Value:employeeId.Value);
        using var r=cmd.ExecuteReader();var result=new List<AttendanceRecord>();while(r.Read())result.Add(new AttendanceRecord{Id=r.GetInt64(0),EmployeeId=r.GetInt64(1),EmployeeName=r.GetString(2),WorkDate=DateTime.Parse(r.GetString(3)),CheckIn=r.IsDBNull(4)?null:TimeSpan.Parse(r.GetString(4)),CheckOut=r.IsDBNull(5)?null:TimeSpan.Parse(r.GetString(5)),Note=r.GetString(6)});return result;
    }
    public void Save(long employeeId,DateTime date,TimeSpan? checkIn,TimeSpan? checkOut,string note)
    {
        if(checkIn is not null&&checkOut is not null&&checkOut<=checkIn)throw new InvalidOperationException("Çıkış saati giriş saatinden sonra olmalıdır.");
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();
        cmd.CommandText="""
            INSERT INTO Attendance(EmployeeId,WorkDate,CheckIn,CheckOut,Note,UpdatedAtUtc)
            VALUES($employeeId,$date,$in,$out,$note,$updated)
            ON CONFLICT(EmployeeId,WorkDate) DO UPDATE SET CheckIn=excluded.CheckIn,CheckOut=excluded.CheckOut,Note=excluded.Note,UpdatedAtUtc=excluded.UpdatedAtUtc;
            """;
        cmd.Parameters.AddWithValue("$employeeId",employeeId);cmd.Parameters.AddWithValue("$date",date.ToString("yyyy-MM-dd"));cmd.Parameters.AddWithValue("$in",checkIn is null?DBNull.Value:checkIn.Value.ToString(@"hh\:mm"));cmd.Parameters.AddWithValue("$out",checkOut is null?DBNull.Value:checkOut.Value.ToString(@"hh\:mm"));cmd.Parameters.AddWithValue("$note",note.Trim());cmd.Parameters.AddWithValue("$updated",DateTime.UtcNow.ToString("O"));cmd.ExecuteNonQuery();
    }
    public int GetTodayPresentCount(){using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT COUNT(*) FROM Attendance WHERE WorkDate=date('now','localtime') AND CheckIn IS NOT NULL;";return Convert.ToInt32(cmd.ExecuteScalar());}
}
