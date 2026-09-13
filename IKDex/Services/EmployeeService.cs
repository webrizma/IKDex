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
            SELECT Id, EmployeeNumber, FirstName, LastName, Email, Phone, Department, Position, StartDate, IsActive,
                   NationalId, Gender, MaritalStatus, BirthDate, BirthPlace, MotherName, FatherName, Address, City,
                   EmergencyContactName, EmergencyContactPhone, EducationLevel, BloodType, EmploymentType, GrossSalary, Iban
            FROM Employees
            WHERE ($includeInactive = 1 OR IsActive = 1)
              AND ($search = '' OR EmployeeNumber LIKE $pattern OR FirstName LIKE $pattern OR LastName LIKE $pattern
                   OR Department LIKE $pattern OR Position LIKE $pattern OR NationalId LIKE $pattern OR Phone LIKE $pattern)
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
                StartDate = DateTime.Parse(reader.GetString(8)), IsActive = reader.GetInt64(9) == 1,
                NationalId = reader.GetString(10), Gender = reader.GetString(11), MaritalStatus = reader.GetString(12),
                BirthDate = reader.IsDBNull(13) ? null : DateTime.Parse(reader.GetString(13)), BirthPlace = reader.GetString(14),
                MotherName = reader.GetString(15), FatherName = reader.GetString(16), Address = reader.GetString(17),
                City = reader.GetString(18), EmergencyContactName = reader.GetString(19), EmergencyContactPhone = reader.GetString(20),
                EducationLevel = reader.GetString(21), BloodType = reader.GetString(22), EmploymentType = reader.GetString(23),
                GrossSalary = reader.GetDecimal(24), Iban = reader.GetString(25)
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
              INSERT INTO Employees (EmployeeNumber, FirstName, LastName, Email, Phone, Department, Position, StartDate, IsActive, CreatedAtUtc,
                  NationalId, Gender, MaritalStatus, BirthDate, BirthPlace, MotherName, FatherName, Address, City, EmergencyContactName,
                  EmergencyContactPhone, EducationLevel, BloodType, EmploymentType, GrossSalary, Iban)
              VALUES ($number, $firstName, $lastName, $email, $phone, $department, $position, $startDate, 1, $createdAt,
                  $nationalId, $gender, $maritalStatus, $birthDate, $birthPlace, $motherName, $fatherName, $address, $city,
                  $emergencyName, $emergencyPhone, $education, $bloodType, $employmentType, $grossSalary, $iban);
              """
            : """
              UPDATE Employees SET EmployeeNumber=$number, FirstName=$firstName, LastName=$lastName, Email=$email,
                  Phone=$phone, Department=$department, Position=$position, StartDate=$startDate,
                  NationalId=$nationalId, Gender=$gender, MaritalStatus=$maritalStatus, BirthDate=$birthDate,
                  BirthPlace=$birthPlace, MotherName=$motherName, FatherName=$fatherName, Address=$address, City=$city,
                  EmergencyContactName=$emergencyName, EmergencyContactPhone=$emergencyPhone, EducationLevel=$education,
                  BloodType=$bloodType, EmploymentType=$employmentType, GrossSalary=$grossSalary, Iban=$iban
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
        command.Parameters.AddWithValue("$nationalId", employee.NationalId.Trim());
        command.Parameters.AddWithValue("$gender", employee.Gender.Trim());
        command.Parameters.AddWithValue("$maritalStatus", employee.MaritalStatus.Trim());
        command.Parameters.AddWithValue("$birthDate", employee.BirthDate?.ToString("yyyy-MM-dd") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$birthPlace", employee.BirthPlace.Trim());
        command.Parameters.AddWithValue("$motherName", employee.MotherName.Trim());
        command.Parameters.AddWithValue("$fatherName", employee.FatherName.Trim());
        command.Parameters.AddWithValue("$address", employee.Address.Trim());
        command.Parameters.AddWithValue("$city", employee.City.Trim());
        command.Parameters.AddWithValue("$emergencyName", employee.EmergencyContactName.Trim());
        command.Parameters.AddWithValue("$emergencyPhone", employee.EmergencyContactPhone.Trim());
        command.Parameters.AddWithValue("$education", employee.EducationLevel.Trim());
        command.Parameters.AddWithValue("$bloodType", employee.BloodType.Trim());
        command.Parameters.AddWithValue("$employmentType", employee.EmploymentType.Trim());
        command.Parameters.AddWithValue("$grossSalary", (double)employee.GrossSalary);
        command.Parameters.AddWithValue("$iban", employee.Iban.Replace(" ", string.Empty).ToUpperInvariant());
        command.Parameters.AddWithValue("$department", employee.Department.Trim());
        command.Parameters.AddWithValue("$position", employee.Position.Trim());
        command.Parameters.AddWithValue("$startDate", employee.StartDate.ToString("yyyy-MM-dd"));
    }
}
