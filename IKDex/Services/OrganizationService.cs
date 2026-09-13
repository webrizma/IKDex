using IKDex.Models;

namespace IKDex.Services;

public sealed class OrganizationService(DatabaseService databaseService)
{
    public IReadOnlyList<Department> GetDepartments(bool includeInactive = false)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, IsActive FROM Departments WHERE $all=1 OR IsActive=1 ORDER BY IsActive DESC, Name;";
        command.Parameters.AddWithValue("$all", includeInactive ? 1 : 0);
        using var reader = command.ExecuteReader();
        var result = new List<Department>();
        while (reader.Read()) result.Add(new Department { Id = reader.GetInt64(0), Name = reader.GetString(1), IsActive = reader.GetInt64(2) == 1 });
        return result;
    }

    public IReadOnlyList<Position> GetPositions(long? departmentId = null, bool includeInactive = false)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT p.Id, p.DepartmentId, d.Name, p.Name, p.IsActive
            FROM Positions p JOIN Departments d ON d.Id=p.DepartmentId
            WHERE ($departmentId IS NULL OR p.DepartmentId=$departmentId) AND ($all=1 OR (p.IsActive=1 AND d.IsActive=1))
            ORDER BY p.IsActive DESC, d.Name, p.Name;
            """;
        command.Parameters.AddWithValue("$departmentId", departmentId is null ? DBNull.Value : departmentId.Value);
        command.Parameters.AddWithValue("$all", includeInactive ? 1 : 0);
        using var reader = command.ExecuteReader();
        var result = new List<Position>();
        while (reader.Read()) result.Add(new Position { Id = reader.GetInt64(0), DepartmentId = reader.GetInt64(1), DepartmentName = reader.GetString(2), Name = reader.GetString(3), IsActive = reader.GetInt64(4) == 1 });
        return result;
    }

    public void SaveDepartment(Department department)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = department.Id == 0 ? "INSERT INTO Departments(Name, IsActive) VALUES($name, 1);" : "UPDATE Departments SET Name=$name WHERE Id=$id;";
        command.Parameters.AddWithValue("$name", department.Name.Trim()); command.Parameters.AddWithValue("$id", department.Id); command.ExecuteNonQuery();
    }

    public void SavePosition(Position position)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = position.Id == 0 ? "INSERT INTO Positions(DepartmentId, Name, IsActive) VALUES($departmentId, $name, 1);" : "UPDATE Positions SET DepartmentId=$departmentId, Name=$name WHERE Id=$id;";
        command.Parameters.AddWithValue("$departmentId", position.DepartmentId); command.Parameters.AddWithValue("$name", position.Name.Trim()); command.Parameters.AddWithValue("$id", position.Id); command.ExecuteNonQuery();
    }

    public void SetDepartmentActive(long id, bool active) => SetActive("Departments", id, active);
    public void SetPositionActive(long id, bool active) => SetActive("Positions", id, active);

    private void SetActive(string table, long id, bool active)
    {
        using var connection = databaseService.CreateConnection(); connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE {table} SET IsActive=$active WHERE Id=$id;";
        command.Parameters.AddWithValue("$active", active ? 1 : 0); command.Parameters.AddWithValue("$id", id); command.ExecuteNonQuery();
    }
}
