using IKDex.Models;

namespace IKDex.Services;

public sealed class PayrollService(DatabaseService databaseService)
{
    public IReadOnlyList<PayrollRecord> GetMonth(DateTime period)
    {
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();
        cmd.CommandText="""
            SELECT p.Id,p.EmployeeId,e.FirstName||' '||e.LastName,p.Period,p.GrossSalary,p.Bonus,p.OvertimePay,
                   p.SocialSecurityDeduction,p.UnemploymentDeduction,p.IncomeTax,p.StampTax,p.OtherDeduction,p.Notes,p.Status
            FROM Payroll p JOIN Employees e ON e.Id=p.EmployeeId WHERE p.Period=$period ORDER BY e.FirstName,e.LastName;
            """;
        cmd.Parameters.AddWithValue("$period",period.ToString("yyyy-MM"));using var r=cmd.ExecuteReader();var result=new List<PayrollRecord>();
        while(r.Read())result.Add(new PayrollRecord{Id=r.GetInt64(0),EmployeeId=r.GetInt64(1),EmployeeName=r.GetString(2),Period=DateTime.Parse(r.GetString(3)+"-01"),GrossSalary=ReadMoney(r,4),Bonus=ReadMoney(r,5),OvertimePay=ReadMoney(r,6),SocialSecurityDeduction=ReadMoney(r,7),UnemploymentDeduction=ReadMoney(r,8),IncomeTax=ReadMoney(r,9),StampTax=ReadMoney(r,10),OtherDeduction=ReadMoney(r,11),Notes=r.GetString(12),Status=r.GetString(13)});return result;
    }
    public void Save(PayrollRecord value)
    {
        if(value.Id!=0&&value.Status!="Taslak")throw new InvalidOperationException("Onaylanan veya ödenen bordro düzenlenemez.");
        var amounts=new[]{value.GrossSalary,value.Bonus,value.OvertimePay,value.SocialSecurityDeduction,value.UnemploymentDeduction,value.IncomeTax,value.StampTax,value.OtherDeduction};if(amounts.Any(x=>x<0))throw new InvalidOperationException("Tutarlar negatif olamaz.");if(value.NetSalary<0)throw new InvalidOperationException("Toplam kesinti toplam kazancı aşamaz.");
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="""
            INSERT INTO Payroll(EmployeeId,Period,GrossSalary,Bonus,OvertimePay,SocialSecurityDeduction,UnemploymentDeduction,IncomeTax,StampTax,OtherDeduction,Notes,Status,UpdatedAtUtc)
            VALUES($employeeId,$period,$gross,$bonus,$overtime,$social,$unemployment,$incomeTax,$stampTax,$other,$notes,'Taslak',$updated)
            ON CONFLICT(EmployeeId,Period) DO UPDATE SET GrossSalary=excluded.GrossSalary,Bonus=excluded.Bonus,OvertimePay=excluded.OvertimePay,
            SocialSecurityDeduction=excluded.SocialSecurityDeduction,UnemploymentDeduction=excluded.UnemploymentDeduction,IncomeTax=excluded.IncomeTax,
            StampTax=excluded.StampTax,OtherDeduction=excluded.OtherDeduction,Notes=excluded.Notes,UpdatedAtUtc=excluded.UpdatedAtUtc;
            """;
        cmd.Parameters.AddWithValue("$employeeId",value.EmployeeId);cmd.Parameters.AddWithValue("$period",value.Period.ToString("yyyy-MM"));cmd.Parameters.AddWithValue("$gross",value.GrossSalary);cmd.Parameters.AddWithValue("$bonus",value.Bonus);cmd.Parameters.AddWithValue("$overtime",value.OvertimePay);cmd.Parameters.AddWithValue("$social",value.SocialSecurityDeduction);cmd.Parameters.AddWithValue("$unemployment",value.UnemploymentDeduction);cmd.Parameters.AddWithValue("$incomeTax",value.IncomeTax);cmd.Parameters.AddWithValue("$stampTax",value.StampTax);cmd.Parameters.AddWithValue("$other",value.OtherDeduction);cmd.Parameters.AddWithValue("$notes",value.Notes.Trim());cmd.Parameters.AddWithValue("$updated",DateTime.UtcNow.ToString("O"));cmd.ExecuteNonQuery();
    }
    public void SetStatus(long id,string status){if(status is not ("Taslak" or "Onaylandı" or "Ödendi"))throw new ArgumentOutOfRangeException(nameof(status));using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE Payroll SET Status=$status,UpdatedAtUtc=$updated WHERE Id=$id;";cmd.Parameters.AddWithValue("$status",status);cmd.Parameters.AddWithValue("$updated",DateTime.UtcNow.ToString("O"));cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
    public (decimal Gross,decimal Net,int Count) GetSummary(DateTime period){var rows=GetMonth(period);return(rows.Sum(x=>x.TotalEarnings),rows.Sum(x=>x.NetSalary),rows.Count);}
    private static decimal ReadMoney(Microsoft.Data.Sqlite.SqliteDataReader reader,int index)=>Convert.ToDecimal(reader.GetDouble(index));
}
