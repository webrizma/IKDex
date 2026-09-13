using IKDex.Models;

namespace IKDex.Services;

public sealed class AuthenticationService(DatabaseService databaseService)
{
    private readonly PasswordHasher _passwordHasher = new();

    public UserSession? Authenticate(string userName, string password)
    {
        using var connection = databaseService.CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, UserName, PasswordHash, PasswordSalt, FullName, Role, EmployeeId
            FROM Users
            WHERE UserName = $userName AND IsActive = 1
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$userName", userName.Trim());

        using var reader = command.ExecuteReader();
        if (!reader.Read())
            return null;

        if (!_passwordHasher.Verify(password, reader.GetString(2), reader.GetString(3)))
            return null;

        return new UserSession(reader.GetInt64(0), reader.GetString(1), reader.GetString(4), reader.GetString(5), reader.IsDBNull(6) ? null : reader.GetInt64(6));
    }
}
