using IKDex.Models;
using System.IO;

namespace IKDex.Services;

public sealed class DocumentService
{
    private readonly DatabaseService _databaseService;
    private readonly string _storageRoot;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".png", ".jpg", ".jpeg" };
    private const long MaximumFileSize = 25 * 1024 * 1024;

    public DocumentService(DatabaseService databaseService, string? storageRoot=null)
    {
        _databaseService=databaseService;
        _storageRoot=storageRoot??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IKDex","Documents");
        Directory.CreateDirectory(_storageRoot);
    }

    public IReadOnlyList<EmployeeDocument> GetAll(long? employeeId=null,bool includeArchived=false)
    {
        using var c=_databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();
        cmd.CommandText="""
            SELECT d.Id,d.EmployeeId,e.FirstName||' '||e.LastName,d.Category,d.DisplayName,d.FileName,d.StoredPath,d.FileSize,d.UploadedAtUtc,d.ExpiryDate,d.IsArchived
            FROM EmployeeDocuments d JOIN Employees e ON e.Id=d.EmployeeId
            WHERE ($employeeId IS NULL OR d.EmployeeId=$employeeId) AND ($archived=1 OR d.IsArchived=0)
            ORDER BY d.IsArchived,d.UploadedAtUtc DESC;
            """;
        cmd.Parameters.AddWithValue("$employeeId",employeeId is null?DBNull.Value:employeeId.Value);cmd.Parameters.AddWithValue("$archived",includeArchived?1:0);
        using var r=cmd.ExecuteReader();var result=new List<EmployeeDocument>();while(r.Read())result.Add(new EmployeeDocument{Id=r.GetInt64(0),EmployeeId=r.GetInt64(1),EmployeeName=r.GetString(2),Category=r.GetString(3),DisplayName=r.GetString(4),FileName=r.GetString(5),StoredPath=r.GetString(6),FileSize=r.GetInt64(7),UploadedAt=DateTime.Parse(r.GetString(8)),ExpiryDate=r.IsDBNull(9)?null:DateTime.Parse(r.GetString(9)),IsArchived=r.GetInt64(10)==1});return result;
    }

    public void Add(long employeeId,string category,string displayName,string sourcePath,DateTime? expiryDate)
    {
        var source=new FileInfo(sourcePath);if(!source.Exists)throw new FileNotFoundException("Seçilen dosya bulunamadı.");
        if(!AllowedExtensions.Contains(source.Extension))throw new InvalidOperationException("Bu dosya türüne izin verilmiyor.");
        if(source.Length>MaximumFileSize)throw new InvalidOperationException("Dosya boyutu 25 MB sınırını aşıyor.");
        var employeeDirectory=Path.Combine(_storageRoot,employeeId.ToString());Directory.CreateDirectory(employeeDirectory);
        var storedPath=Path.Combine(employeeDirectory,$"{Guid.NewGuid():N}{source.Extension.ToLowerInvariant()}");File.Copy(source.FullName,storedPath,false);
        try
        {
            using var c=_databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="""
                INSERT INTO EmployeeDocuments(EmployeeId,Category,DisplayName,FileName,StoredPath,FileSize,UploadedAtUtc,ExpiryDate,IsArchived)
                VALUES($employeeId,$category,$displayName,$fileName,$path,$size,$uploaded,$expiry,0);
                """;
            cmd.Parameters.AddWithValue("$employeeId",employeeId);cmd.Parameters.AddWithValue("$category",category);cmd.Parameters.AddWithValue("$displayName",displayName.Trim());cmd.Parameters.AddWithValue("$fileName",source.Name);cmd.Parameters.AddWithValue("$path",storedPath);cmd.Parameters.AddWithValue("$size",source.Length);cmd.Parameters.AddWithValue("$uploaded",DateTime.UtcNow.ToString("O"));cmd.Parameters.AddWithValue("$expiry",expiryDate is null?DBNull.Value:expiryDate.Value.ToString("yyyy-MM-dd"));cmd.ExecuteNonQuery();
        }
        catch { File.Delete(storedPath); throw; }
    }

    public void SetArchived(long id,bool archived){using var c=_databaseService.CreateConnection();c.Open();using var cmd=c.CreateCommand();cmd.CommandText="UPDATE EmployeeDocuments SET IsArchived=$archived WHERE Id=$id;";cmd.Parameters.AddWithValue("$archived",archived?1:0);cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}
}
