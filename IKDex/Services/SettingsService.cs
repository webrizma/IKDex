using IKDex.Models;

namespace IKDex.Services;

public sealed class SettingsService(DatabaseService databaseService)
{
    public CompanySettings Get()
    {
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT CompanyName,TaxNumber,Phone,Email,Address,UpdateRepositoryUrl FROM CompanySettings WHERE Id=1;";using var r=cmd.ExecuteReader();
        return r.Read()?new CompanySettings{CompanyName=r.GetString(0),TaxNumber=r.GetString(1),Phone=r.GetString(2),Email=r.GetString(3),Address=r.GetString(4),UpdateRepositoryUrl=r.GetString(5)}:new CompanySettings();
    }
    public void Save(CompanySettings value)
    {
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE CompanySettings SET CompanyName=$name,TaxNumber=$tax,Phone=$phone,Email=$email,Address=$address,UpdateRepositoryUrl=$updateUrl WHERE Id=1;";
        cmd.Parameters.AddWithValue("$name",value.CompanyName.Trim());cmd.Parameters.AddWithValue("$tax",value.TaxNumber.Trim());cmd.Parameters.AddWithValue("$phone",value.Phone.Trim());cmd.Parameters.AddWithValue("$email",value.Email.Trim());cmd.Parameters.AddWithValue("$address",value.Address.Trim());cmd.Parameters.AddWithValue("$updateUrl",value.UpdateRepositoryUrl.Trim());cmd.ExecuteNonQuery();
    }
}
