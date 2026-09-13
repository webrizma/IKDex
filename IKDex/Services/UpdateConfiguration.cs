using System.IO;
using System.Text.Json;

namespace IKDex.Services;

public static class UpdateConfiguration
{
    public static string GetRepositoryUrl()
    {
        var databaseValue=new SettingsService(App.Database).Get().UpdateRepositoryUrl;
        if(!string.IsNullOrWhiteSpace(databaseValue))return databaseValue;
        try
        {
            var path=Path.Combine(AppContext.BaseDirectory,"update.json");
            if(!File.Exists(path))return string.Empty;
            using var document=JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.TryGetProperty("repositoryUrl",out var value)?value.GetString()??string.Empty:string.Empty;
        }
        catch(JsonException){return string.Empty;}
    }
}
