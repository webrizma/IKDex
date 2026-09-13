using System.Windows;
using IKDex.Services;

namespace IKDex;

public partial class App : Application
{
    public static DatabaseService Database { get; } = new();

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_winsqlite3());
            SQLitePCL.raw.FreezeProvider();
            Database.Initialize();
            base.OnStartup(e);
        }
        catch (Exception exception)
        {
            MessageBox.Show($"Veritabanı başlatılamadı.\n\n{exception.Message}", "IKDex Başlatma Hatası",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
