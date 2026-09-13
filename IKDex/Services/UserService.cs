using IKDex.Models;

namespace IKDex.Services;

public sealed class UserService(DatabaseService databaseService)
{
    public IReadOnlyList<UserAccount> GetAll()
    {
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();
        cmd.CommandText="SELECT u.Id,u.UserName,u.FullName,u.Role,u.EmployeeId,COALESCE(e.FirstName||' '||e.LastName,''),u.IsActive FROM Users u LEFT JOIN Employees e ON e.Id=u.EmployeeId ORDER BY u.IsActive DESC,u.FullName;";
        using var r=cmd.ExecuteReader();var result=new List<UserAccount>();while(r.Read())result.Add(new UserAccount{Id=r.GetInt64(0),UserName=r.GetString(1),FullName=r.GetString(2),Role=r.GetString(3),EmployeeId=r.IsDBNull(4)?null:r.GetInt64(4),EmployeeName=r.GetString(5),IsActive=r.GetInt64(6)==1});return result;
    }
    public void Save(UserAccount user,string? newPassword)
    {
        using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();
        if(user.Id==0){if(string.IsNullOrWhiteSpace(newPassword)||newPassword.Length<6)throw new InvalidOperationException("Yeni kullanıcı parolası en az 6 karakter olmalıdır.");var(hash,salt)=new PasswordHasher().Hash(newPassword);cmd.CommandText="INSERT INTO Users(UserName,PasswordHash,PasswordSalt,FullName,Role,EmployeeId,IsActive,CreatedAtUtc) VALUES($name,$hash,$salt,$fullName,$role,$employeeId,1,$created);";cmd.Parameters.AddWithValue("$hash",hash);cmd.Parameters.AddWithValue("$salt",salt);cmd.Parameters.AddWithValue("$created",DateTime.UtcNow.ToString("O"));}
        else{cmd.CommandText="UPDATE Users SET UserName=$name,FullName=$fullName,Role=$role,EmployeeId=$employeeId WHERE Id=$id;";}
        cmd.Parameters.AddWithValue("$name",user.UserName.Trim());cmd.Parameters.AddWithValue("$fullName",user.FullName.Trim());cmd.Parameters.AddWithValue("$role",user.Role);cmd.Parameters.AddWithValue("$employeeId",user.EmployeeId is null?DBNull.Value:user.EmployeeId.Value);cmd.Parameters.AddWithValue("$id",user.Id);cmd.ExecuteNonQuery();
        if(user.Id!=0&&!string.IsNullOrWhiteSpace(newPassword))ResetPassword(user.Id,newPassword);
    }
    public void ResetPassword(long id,string password){if(password.Length<6)throw new InvalidOperationException("Parola en az 6 karakter olmalıdır.");var(hash,salt)=new PasswordHasher().Hash(password);using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE Users SET PasswordHash=$hash,PasswordSalt=$salt WHERE Id=$id;";cmd.Parameters.AddWithValue("$hash",hash);cmd.Parameters.AddWithValue("$salt",salt);cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
    public void SetActive(long id,bool active){using var c=databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE Users SET IsActive=$active WHERE Id=$id;";cmd.Parameters.AddWithValue("$active",active?1:0);cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
}
