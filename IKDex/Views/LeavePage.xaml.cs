using System.Windows;
using System.Windows.Controls;
using IKDex.Models;
using IKDex.Services;

namespace IKDex.Views;

public partial class LeavePage : UserControl
{
    private readonly LeaveService _service = new(App.Database);
    private readonly long _reviewerUserId;
    private readonly long? _employeeId;
    private readonly bool _canReview;
    private LeaveRequest? _selected;
    public event EventHandler? DataChanged;

    public LeavePage(UserSession session)
    {
        _reviewerUserId=session.Id; _employeeId=session.EmployeeId; _canReview=new AuthorizationService(session).CanReviewLeave; InitializeComponent();
        var employees=new EmployeeService(App.Database).GetAll(); EmployeeBox.ItemsSource=_canReview?employees:employees.Where(x=>x.Id==_employeeId).ToList();
        if(!_canReview&&_employeeId is not null){EmployeeBox.SelectedValue=_employeeId;EmployeeBox.IsEnabled=false;}
        StartDatePicker.SelectedDate=DateTime.Today; EndDatePicker.SelectedDate=DateTime.Today; LoadData();
    }
    private void LoadData(){var status=(StatusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();if(status=="Tümü")status=null;var items=_service.GetAll(status,_canReview?null:_employeeId);LeaveGrid.ItemsSource=items;SummaryText.Text=_canReview?$"{items.Count} izin kaydı • {_service.GetPendingCount()} talep onay bekliyor":$"{items.Count} izin kaydınız bulunuyor";}
    private void StatusFilter_SelectionChanged(object sender,SelectionChangedEventArgs e){if(IsLoaded)LoadData();}
    private void DateChanged(object sender,SelectionChangedEventArgs e){if(StartDatePicker?.SelectedDate is DateTime start&&EndDatePicker?.SelectedDate is DateTime end&&end>=start)DayCountText.Text=$"{LeaveService.CountWorkingDays(start,end)} iş günü";else if(DayCountText is not null)DayCountText.Text="0 iş günü";}
    private void NewRequest_Click(object sender,RoutedEventArgs e){_selected=null;LeaveGrid.SelectedItem=null;FormTitle.Text="Yeni izin talebi";EmployeeBox.IsEnabled=LeaveTypeBox.IsEnabled=StartDatePicker.IsEnabled=EndDatePicker.IsEnabled=true;ReasonBox.IsReadOnly=false;SaveButton.Visibility=Visibility.Visible;ReviewButtons.Visibility=Visibility.Collapsed;ErrorText.Visibility=Visibility.Collapsed;}
    private void LeaveGrid_SelectionChanged(object sender,SelectionChangedEventArgs e){if(LeaveGrid.SelectedItem is not LeaveRequest request)return;_selected=request;FormTitle.Text="İzin talebi detayı";EmployeeBox.SelectedValue=request.EmployeeId;SelectLeaveType(request.LeaveType);StartDatePicker.SelectedDate=request.StartDate;EndDatePicker.SelectedDate=request.EndDate;ReasonBox.Text=request.Reason;EmployeeBox.IsEnabled=LeaveTypeBox.IsEnabled=StartDatePicker.IsEnabled=EndDatePicker.IsEnabled=false;ReasonBox.IsReadOnly=true;SaveButton.Visibility=Visibility.Collapsed;ReviewButtons.Visibility=_canReview&&request.Status=="Bekliyor"?Visibility.Visible:Visibility.Collapsed;}
    private void SaveButton_Click(object sender,RoutedEventArgs e){if(EmployeeBox.SelectedValue is not long employeeId||StartDatePicker.SelectedDate is not DateTime start||EndDatePicker.SelectedDate is not DateTime end){ShowError("Personel ve tarih alanlarını doldurun.");return;}if(end<start){ShowError("Bitiş tarihi başlangıç tarihinden önce olamaz.");return;}if(LeaveService.CountWorkingDays(start,end)==0){ShowError("İzin aralığında en az bir iş günü bulunmalıdır.");return;}try{_service.Create(employeeId,(LeaveTypeBox.SelectedItem as ComboBoxItem)!.Content.ToString()!,start,end,ReasonBox.Text);NewRequest_Click(sender,e);LoadData();DataChanged?.Invoke(this,EventArgs.Empty);}catch(InvalidOperationException ex){ShowError(ex.Message);}}
    private void Approve_Click(object sender,RoutedEventArgs e)=>Review("Onaylandı"); private void Reject_Click(object sender,RoutedEventArgs e)=>Review("Reddedildi");
    private void Review(string status){if(_selected is null||!_canReview)return;_service.Review(_selected.Id,status,_reviewerUserId);LoadData();NewRequest_Click(this,new RoutedEventArgs());DataChanged?.Invoke(this,EventArgs.Empty);}
    private void SelectLeaveType(string type){foreach(ComboBoxItem item in LeaveTypeBox.Items)if(item.Content.ToString()==type){LeaveTypeBox.SelectedItem=item;break;}}
    private void ShowError(string message){ErrorText.Text=message;ErrorText.Visibility=Visibility.Visible;}
}
