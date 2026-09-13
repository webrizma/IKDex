using Microsoft.Win32;using System.Windows;using System.Windows.Controls;using IKDex.Services;
namespace IKDex.Views;
public partial class ReportsPage:UserControl
{
 private readonly ReportService _service=new(App.Database);public ReportsPage(){InitializeComponent();MonthPicker.SelectedDate=DateTime.Today;}
 private string ReportType=>(ReportTypeBox.SelectedItem as ComboBoxItem)?.Content.ToString()??"Personel Listesi";
 private void ReportTypeChanged(object sender,SelectionChangedEventArgs e){if(DescriptionText is null)return;DescriptionText.Text=ReportType switch{"Personel Listesi"=>"Aktif ve pasif tüm personelin güncel bilgileri.","İzin Raporu"=>"Seçilen ayda başlayan izin talepleri ve onay durumları.",_=>"Seçilen aya ait personel giriş, çıkış ve çalışma süreleri."};}
 private void Excel_Click(object sender,RoutedEventArgs e)=>Export("Excel çalışma kitabı|*.xlsx",path=>_service.ExportExcel(path,ReportType,MonthPicker.SelectedDate??DateTime.Today));
 private void Pdf_Click(object sender,RoutedEventArgs e)=>Export("PDF belgesi|*.pdf",path=>_service.ExportPdf(path,ReportType,MonthPicker.SelectedDate??DateTime.Today));
 private void Export(string filter,Action<string> exporter){var extension=filter.Contains("xlsx")?"xlsx":"pdf";var dialog=new SaveFileDialog{Filter=filter,FileName=$"IKDex_{ReportType.Replace(' ','_')}_{DateTime.Now:yyyyMMdd}.{extension}",AddExtension=true};if(dialog.ShowDialog()!=true)return;try{exporter(dialog.FileName);StatusText.Text=$"Rapor oluşturuldu: {dialog.FileName}";StatusText.Visibility=Visibility.Visible;}catch(Exception ex){StatusText.Text=$"Rapor oluşturulamadı: {ex.Message}";StatusText.Foreground=System.Windows.Media.Brushes.Firebrick;StatusText.Visibility=Visibility.Visible;}}
}
