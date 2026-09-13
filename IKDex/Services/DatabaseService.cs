using Microsoft.Data.Sqlite;
using System.IO;

namespace IKDex.Services;

public sealed class DatabaseService
{
    private readonly string _connectionString;
    public string DatabasePath { get; }

    public DatabaseService(string? databasePath = null)
    {
        databasePath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "IKDex",
            "ikdex.db");

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        DatabasePath = databasePath;

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true
        }.ToString();
    }

    public SqliteConnection CreateConnection() => new(_connectionString);

    public void Initialize()
    {
        using var connection = CreateConnection();
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserName TEXT NOT NULL COLLATE NOCASE UNIQUE,
                PasswordHash TEXT NOT NULL,
                PasswordSalt TEXT NOT NULL,
                FullName TEXT NOT NULL,
                Role TEXT NOT NULL,
                EmployeeId INTEGER NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS Employees (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeNumber TEXT NOT NULL COLLATE NOCASE UNIQUE,
                FirstName TEXT NOT NULL,
                LastName TEXT NOT NULL,
                Email TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '',
                Department TEXT NOT NULL,
                Position TEXT NOT NULL,
                StartDate TEXT NOT NULL,
                IsActive INTEGER NOT NULL DEFAULT 1,
                CreatedAtUtc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS IX_Employees_Name ON Employees (FirstName, LastName);
            CREATE INDEX IF NOT EXISTS IX_Employees_Department ON Employees (Department);

            CREATE TABLE IF NOT EXISTS Departments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL COLLATE NOCASE UNIQUE,
                IsActive INTEGER NOT NULL DEFAULT 1
            );

            CREATE TABLE IF NOT EXISTS Positions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                DepartmentId INTEGER NOT NULL,
                Name TEXT NOT NULL COLLATE NOCASE,
                IsActive INTEGER NOT NULL DEFAULT 1,
                FOREIGN KEY (DepartmentId) REFERENCES Departments(Id),
                UNIQUE (DepartmentId, Name)
            );

            INSERT OR IGNORE INTO Departments(Name) SELECT DISTINCT Department FROM Employees WHERE TRIM(Department) <> '';
            INSERT OR IGNORE INTO Positions(DepartmentId, Name)
                SELECT d.Id, e.Position FROM Employees e JOIN Departments d ON d.Name=e.Department
                WHERE TRIM(e.Position) <> '';

            CREATE TABLE IF NOT EXISTS LeaveRequests (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                LeaveType TEXT NOT NULL,
                StartDate TEXT NOT NULL,
                EndDate TEXT NOT NULL,
                DayCount INTEGER NOT NULL,
                Reason TEXT NOT NULL DEFAULT '',
                Status TEXT NOT NULL DEFAULT 'Bekliyor',
                RequestedAtUtc TEXT NOT NULL,
                ReviewedAtUtc TEXT NULL,
                ReviewedByUserId INTEGER NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
                FOREIGN KEY(ReviewedByUserId) REFERENCES Users(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_LeaveRequests_Status ON LeaveRequests(Status);
            CREATE INDEX IF NOT EXISTS IX_LeaveRequests_EmployeeDates ON LeaveRequests(EmployeeId, StartDate, EndDate);

            CREATE TABLE IF NOT EXISTS Attendance (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                WorkDate TEXT NOT NULL,
                CheckIn TEXT NULL,
                CheckOut TEXT NULL,
                Note TEXT NOT NULL DEFAULT '',
                UpdatedAtUtc TEXT NOT NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
                UNIQUE(EmployeeId, WorkDate)
            );
            CREATE INDEX IF NOT EXISTS IX_Attendance_WorkDate ON Attendance(WorkDate);

            CREATE TABLE IF NOT EXISTS EmployeeDocuments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                Category TEXT NOT NULL,
                DisplayName TEXT NOT NULL,
                FileName TEXT NOT NULL,
                StoredPath TEXT NOT NULL UNIQUE,
                FileSize INTEGER NOT NULL,
                UploadedAtUtc TEXT NOT NULL,
                ExpiryDate TEXT NULL,
                IsArchived INTEGER NOT NULL DEFAULT 0,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_EmployeeDocuments_Employee ON EmployeeDocuments(EmployeeId,IsArchived);

            CREATE TABLE IF NOT EXISTS CompanySettings (
                Id INTEGER PRIMARY KEY CHECK(Id=1),
                CompanyName TEXT NOT NULL,
                TaxNumber TEXT NOT NULL DEFAULT '',
                Phone TEXT NOT NULL DEFAULT '',
                Email TEXT NOT NULL DEFAULT '',
                Address TEXT NOT NULL DEFAULT ''
            );
            INSERT OR IGNORE INTO CompanySettings(Id,CompanyName) VALUES(1,'IKDex Şirketi');

            CREATE TABLE IF NOT EXISTS Payroll (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                EmployeeId INTEGER NOT NULL,
                Period TEXT NOT NULL,
                GrossSalary REAL NOT NULL DEFAULT 0,
                Bonus REAL NOT NULL DEFAULT 0,
                OvertimePay REAL NOT NULL DEFAULT 0,
                SocialSecurityDeduction REAL NOT NULL DEFAULT 0,
                UnemploymentDeduction REAL NOT NULL DEFAULT 0,
                IncomeTax REAL NOT NULL DEFAULT 0,
                StampTax REAL NOT NULL DEFAULT 0,
                OtherDeduction REAL NOT NULL DEFAULT 0,
                Notes TEXT NOT NULL DEFAULT '',
                Status TEXT NOT NULL DEFAULT 'Taslak',
                UpdatedAtUtc TEXT NOT NULL,
                FOREIGN KEY(EmployeeId) REFERENCES Employees(Id),
                UNIQUE(EmployeeId,Period)
            );
            CREATE INDEX IF NOT EXISTS IX_Payroll_Period ON Payroll(Period,Status);
            """;
        command.ExecuteNonQuery();

        EnsureColumn(connection, "Users", "EmployeeId", "INTEGER NULL REFERENCES Employees(Id)");
        EnsureColumn(connection, "CompanySettings", "UpdateRepositoryUrl", "TEXT NOT NULL DEFAULT ''");

        using var countCommand = connection.CreateCommand();
        countCommand.CommandText = "SELECT COUNT(*) FROM Users;";
        if (Convert.ToInt64(countCommand.ExecuteScalar()) == 0)
            CreateInitialAdmin(connection);
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        using var info = connection.CreateCommand(); info.CommandText = $"PRAGMA table_info({table});";
        using var reader = info.ExecuteReader(); var exists = false;
        while (reader.Read()) if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase)) { exists = true; break; }
        reader.Close(); if (exists) return;
        using var alter = connection.CreateCommand(); alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};"; alter.ExecuteNonQuery();
    }

    private static void CreateInitialAdmin(SqliteConnection connection)
    {
        var (hash, salt) = new PasswordHasher().Hash("1234");
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO Users (UserName, PasswordHash, PasswordSalt, FullName, Role, CreatedAtUtc)
            VALUES ($userName, $hash, $salt, $fullName, $role, $createdAtUtc);
            """;
        command.Parameters.AddWithValue("$userName", "admin");
        command.Parameters.AddWithValue("$hash", hash);
        command.Parameters.AddWithValue("$salt", salt);
        command.Parameters.AddWithValue("$fullName", "Admin Kullanıcı");
        command.Parameters.AddWithValue("$role", "İK Yöneticisi");
        command.Parameters.AddWithValue("$createdAtUtc", DateTime.UtcNow.ToString("O"));
        command.ExecuteNonQuery();
    }
}
