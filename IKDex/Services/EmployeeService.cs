using IKDex.Models;
using Microsoft.Data.Sqlite;

namespace IKDex.Services;

public sealed class EmployeeService(DatabaseService databaseService)
{
    public IReadOnlyList<Employee> GetAll(string? search = null, bool includeInactive = false)
    {
        using var connection = databaseService.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, EmployeeNumber, FirstName, LastName, Email, Phone, Department, Position, StartDate, IsActive
            FROM Employees
            WHERE ($includeInactive = 1 OR IsActive = 1)
              AND ($search = '' OR EmployeeNumber LIKE $pattern OR FirstName LIKE $pattern OR LastName LIKE $pattern
                   OR Department LIKE $pattern OR Position LIKE $pattern)
            ORDER BY IsActive DESC, FirstName, LastName;
            """;
        var normalizedSearch = search?.Trim() ?? string.Empty;
        command.Parameters.AddWithValue("$includeInactive", includeInactive ? 1 : 0);
        command.Parameters.AddWithValue("$search", normalizedSearch);
        command.Parameters.AddWithValue("$pattern", $"%{normalizedSearch}%");

        using var reader = command.ExecuteReader();
        var employees = new List<Employee>();
        while (reader.Read())
        {
            employees.Add(new Employee
            {
                Id = reader.GetInt64(0), EmployeeNumber = reader.GetString(1), FirstName = reader.GetString(2),
                LastName = reader.GetString(3), Email = reader.GetString(4), Phone = reader.GetString(5),
                Department = reader.GetString(6), Position = reader.GetString(7),
                StartDate = DateTime.Parse(reader.GetString(8)), IsActive = reader.GetInt64(9) == 1
            });
        }
        return employees;
    }

    public int GetActiveCount()
    {
        using var connection = databaseService.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Employees WHERE IsActive = 1;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void Save(Employee employee)
    {
        using var connection = databaseService.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = employee.Id == 0
            ? """
              INSERT INTO Employees (EmployeeNumber, FirstName, LastName, Email, Phone, Department, Position, StartDate, IsActive, CreatedAtUtc)
              VALUES ($number, $firstName, $lastName, $email, $phone, $department, $position, $startDate, 1, $createdAt);
              """
            : """
              UPDATE Employees SET EmployeeNumber=$number, FirstName=$firstName, LastName=$lastName, Email=$email,
                  Phone=$phone, Department=$department, Position=$position, StartDate=$startDate
              WHERE Id=$id;
              """;
        AddParameters(command, employee);
        command.Parameters.AddWithValue("$createdAt", DateTime.UtcNow.ToString("O"));
        command.Parameters.AddWithValue("$id", employee.Id);
        command.ExecuteNonQuery();
    }

    public void SetActive(long id, bool isActive)
    {
        using var connection = databaseService.CreateConnection();
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Employees SET IsActive=$isActive WHERE Id=$id;";
        command.Parameters.AddWithValue("$isActive", isActive ? 1 : 0);
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    private static void AddParameters(SqliteCommand command, Employee employee)
    {
        command.Parameters.AddWithValue("$number", employee.EmployeeNumber.Trim());
        command.Parameters.AddWithValue("$firstName", employee.FirstName.Trim());
        command.Parameters.AddWithValue("$lastName", employee.LastName.Trim());
        command.Parameters.AddWithValue("$email", employee.Email.Trim());
        command.Parameters.AddWithValue("$phone", employee.Phone.Trim());
        command.Parameters.AddWithValue("$department", employee.Department.Trim());
        command.Parameters.AddWithValue("$position", employee.Position.Trim());
        command.Parameters.AddWithValue("$startDate", employee.StartDate.ToString("yyyy-MM-dd"));
    }
}
