using Microsoft.Win32;using System.Windows;using System.Windows.Controls;using IKDex.Models;using IKDex.Services;
namespace IKDex.Views;
public partial class SettingsPage:UserControl
{
 private readonly SettingsService _settings=new(App.Database);private readonly BackupService _backup=new(App.Database);public event EventHandler? SettingsChanged;
 public SettingsPage(){InitializeComponent();var value=_settings.Get();CompanyNameBox.Text=value.CompanyName;TaxNumberBox.Text=value.TaxNumber;PhoneBox.Text=value.Phone;EmailBox.Text=value.Email;AddressBox.Text=value.Address;UpdateRepositoryBox.Text=string.IsNullOrWhiteSpace(value.UpdateRepositoryUrl)?UpdateConfiguration.GetRepositoryUrl():value.UpdateRepositoryUrl;}
 private void SaveSettings_Click(object sender,RoutedEventArgs e){if(string.IsNullOrWhiteSpace(CompanyNameBox.Text)){ShowSettings("Şirket adı boş bırakılamaz.",true);return;}_settings.Save(new CompanySettings{CompanyName=CompanyNameBox.Text,TaxNumber=TaxNumberBox.Text,Phone=PhoneBox.Text,Email=EmailBox.Text,Address=AddressBox.Text,UpdateRepositoryUrl=UpdateRepositoryBox.Text});ShowSettings("Şirket bilgileri kaydedildi.",false);SettingsChanged?.Invoke(this,EventArgs.Empty);}
 private async void CheckUpdate_Click(object sender,RoutedEventArgs e)
 {
  UpdateStatusText.Text="Güncelleme kontrol ediliyor...";UpdateStatusText.Visibility=Visibility.Visible;
  try
  {
   var result=await new UpdateService().CheckAsync(UpdateRepositoryBox.Text.Trim());UpdateStatusText.Text=result.Message;
   if(result.Update is null||result.Manager is null)return;
   if(MessageBox.Show($"{result.Message}\n\nGüncelleme indirilsin ve uygulama yeniden başlatılsın mı?","IKDex Güncellemesi",MessageBoxButton.YesNo,MessageBoxImage.Information)!=MessageBoxResult.Yes)return;
   UpdateProgress.Visibility=Visibility.Visible;
   await new UpdateService().DownloadAndApplyAsync(result.Manager,result.Update,value=>Dispatcher.Invoke(()=>UpdateProgress.Value=value));
  }
  catch(Exception ex){UpdateStatusText.Text=$"Güncelleme kontrolü başarısız: {ex.Message}";UpdateStatusText.Foreground=System.Windows.Media.Brushes.Firebrick;}
 }
 private void Backup_Click(object sender,RoutedEventArgs e){var d=new SaveFileDialog{Filter="IKDex yedek dosyası|*.ikdexbackup",FileName=$"IKDex_Yedek_{DateTime.Now:yyyyMMdd_HHmm}.ikdexbackup",AddExtension=true};if(d.ShowDialog()!=true)return;try{_backup.Create(d.FileName);ShowBackup($"Yedek oluşturuldu: {d.FileName}",false);}catch(Exception ex){ShowBackup($"Yedek oluşturulamadı: {ex.Message}",true);}}
 private void Restore_Click(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter="IKDex yedek dosyası|*.ikdexbackup"};if(d.ShowDialog()!=true)return;if(MessageBox.Show("Seçilen yedek mevcut verilerin yerine yüklenecek. Devam etmek istiyor musunuz?","Yedekten geri yükle",MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes)return;try{var safety=_backup.Restore(d.FileName);MessageBox.Show($"Geri yükleme tamamlandı. Önceki verilerin güvenlik yedeği:\n{safety}\n\nUygulamayı yeniden açın.","Geri yükleme tamamlandı");Application.Current.Shutdown();}catch(Exception ex){ShowBackup($"Geri yükleme başarısız: {ex.Message}",true);}}
 private void ShowSettings(string text,bool error){SettingsStatusText.Text=text;SettingsStatusText.Foreground=error?System.Windows.Media.Brushes.Firebrick:System.Windows.Media.Brushes.SeaGreen;SettingsStatusText.Visibility=Visibility.Visible;}
 private void ShowBackup(string text,bool error){BackupStatusText.Text=text;BackupStatusText.Foreground=error?System.Windows.Media.Brushes.Firebrick:System.Windows.Media.Brushes.SeaGreen;BackupStatusText.Visibility=Visibility.Visible;}
}
