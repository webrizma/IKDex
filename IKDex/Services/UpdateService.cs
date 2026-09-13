using Velopack;
using Velopack.Sources;

namespace IKDex.Services;

public sealed record UpdateCheckResult(bool IsInstalled, UpdateManager? Manager, UpdateInfo? Update, string Message);

public sealed class UpdateService
{
    public async Task<UpdateCheckResult> CheckAsync(string repositoryUrl)
    {
        if (!Uri.TryCreate(repositoryUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps || uri.Host != "github.com")
            return new(false,null,null,"Geçerli bir GitHub depo adresi girilmemiş.");
        var manager=new UpdateManager(new GithubSource(repositoryUrl,null,false));
        if(!manager.IsInstalled)return new(false,manager,null,"Güncelleme kontrolü yalnızca Velopack ile kurulmuş sürümde kullanılabilir.");
        var update=await manager.CheckForUpdatesAsync();
        return update is null?new(true,manager,null,"Uygulama güncel."):new(true,manager,update,$"Yeni sürüm bulundu: {update.TargetFullRelease.Version}");
    }

    public async Task DownloadAndApplyAsync(UpdateManager manager,UpdateInfo update,Action<int> progress)
    {
        await manager.DownloadUpdatesAsync(update,progress);
        manager.ApplyUpdatesAndRestart(update.TargetFullRelease);
    }
}
