using Microsoft.Data.Sqlite;
using System.IO;
using System.IO.Compression;

namespace IKDex.Services;

public sealed class BackupService(DatabaseService databaseService)
{
    private readonly string _documentsRoot=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"IKDex","Documents");

    public void Create(string destination)
    {
        var tempRoot=Path.Combine(Path.GetTempPath(),"IKDexBackup",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(tempRoot);
        try
        {
            var snapshot=Path.Combine(tempRoot,"ikdex.db");using(var source=databaseService.CreateConnection()){source.Open();using var target=new SqliteConnection($"Data Source={snapshot}");target.Open();source.BackupDatabase(target);}
            using var archive=ZipFile.Open(destination,ZipArchiveMode.Create);archive.CreateEntryFromFile(snapshot,"ikdex.db",CompressionLevel.Optimal);
            if(Directory.Exists(_documentsRoot))foreach(var file in Directory.EnumerateFiles(_documentsRoot,"*",SearchOption.AllDirectories)){var relative=Path.GetRelativePath(_documentsRoot,file).Replace('\\','/');archive.CreateEntryFromFile(file,$"Documents/{relative}",CompressionLevel.Optimal);}
        }
        finally{if(Directory.Exists(tempRoot))Directory.Delete(tempRoot,true);}
    }

    public string Restore(string backupPath)
    {
        var tempRoot=Path.Combine(Path.GetTempPath(),"IKDexRestore",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(tempRoot);
        try
        {
            using(var archive=ZipFile.OpenRead(backupPath))
            {
                foreach(var entry in archive.Entries){var target=Path.GetFullPath(Path.Combine(tempRoot,entry.FullName));if(!target.StartsWith(Path.GetFullPath(tempRoot)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Yedek dosyasında geçersiz bir yol bulundu.");if(string.IsNullOrEmpty(entry.Name)){Directory.CreateDirectory(target);continue;}Directory.CreateDirectory(Path.GetDirectoryName(target)!);entry.ExtractToFile(target,true);}
            }
            var restoredDb=Path.Combine(tempRoot,"ikdex.db");if(!File.Exists(restoredDb))throw new InvalidDataException("Yedek içinde veritabanı bulunamadı.");
            using(var check=new SqliteConnection($"Data Source={restoredDb};Mode=ReadOnly")){check.Open();using var cmd=check.CreateCommand();cmd.CommandText="PRAGMA quick_check;";if(!string.Equals(cmd.ExecuteScalar()?.ToString(),"ok",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Yedek veritabanı bütünlük kontrolünü geçemedi.");}
            var safetyDir=Path.Combine(Path.GetDirectoryName(databaseService.DatabasePath)!,"SafetyBackups");Directory.CreateDirectory(safetyDir);var safety=Path.Combine(safetyDir,$"before_restore_{DateTime.Now:yyyyMMdd_HHmmss}.ikdexbackup");Create(safety);
            File.Copy(restoredDb,databaseService.DatabasePath,true);
            var restoredDocuments=Path.Combine(tempRoot,"Documents");if(Directory.Exists(restoredDocuments)){Directory.CreateDirectory(_documentsRoot);foreach(var file in Directory.EnumerateFiles(restoredDocuments,"*",SearchOption.AllDirectories)){var relative=Path.GetRelativePath(restoredDocuments,file);var target=Path.Combine(_documentsRoot,relative);Directory.CreateDirectory(Path.GetDirectoryName(target)!);File.Copy(file,target,true);}}
            return safety;
        }
        finally{if(Directory.Exists(tempRoot))Directory.Delete(tempRoot,true);}
    }
}
