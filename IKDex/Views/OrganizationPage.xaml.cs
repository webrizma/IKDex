using System.Windows;
using System.Windows.Controls;
using IKDex.Models;
using IKDex.Services;
using Microsoft.Data.Sqlite;

namespace IKDex.Views;

public partial class OrganizationPage : UserControl
{
    private readonly OrganizationService _service = new(App.Database);
    private Department? _department; private Position? _position;
    public event EventHandler? DataChanged;
    public OrganizationPage() { InitializeComponent(); LoadData(); }

    private void LoadData()
    {
        DepartmentsGrid.ItemsSource = _service.GetDepartments(ShowInactiveDepartments.IsChecked == true);
        PositionsGrid.ItemsSource = _service.GetPositions(null, ShowInactivePositions.IsChecked == true);
        PositionDepartmentBox.ItemsSource = _service.GetDepartments();
    }
    private void FilterChanged(object sender, RoutedEventArgs e) { if (IsLoaded) LoadData(); }
    private void DepartmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (DepartmentsGrid.SelectedItem is not Department d) return; _department=d; DepartmentNameBox.Text=d.Name; DepartmentStatusButton.Content=d.IsActive?"Pasife al":"Aktifleştir"; DepartmentStatusButton.Visibility=Visibility.Visible; }
    private void PositionsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (PositionsGrid.SelectedItem is not Position p) return; _position=p; PositionNameBox.Text=p.Name; PositionDepartmentBox.SelectedValue=p.DepartmentId; PositionStatusButton.Content=p.IsActive?"Pasife al":"Aktifleştir"; PositionStatusButton.Visibility=Visibility.Visible; }
    private void NewDepartment_Click(object sender, RoutedEventArgs e) { _department=null; DepartmentNameBox.Clear(); DepartmentStatusButton.Visibility=Visibility.Collapsed; DepartmentsGrid.SelectedItem=null; }
    private void NewPosition_Click(object sender, RoutedEventArgs e) { _position=null; PositionNameBox.Clear(); PositionDepartmentBox.SelectedItem=null; PositionStatusButton.Visibility=Visibility.Collapsed; PositionsGrid.SelectedItem=null; }
    private void SaveDepartment_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DepartmentNameBox.Text)) { ShowError("Departman adı boş bırakılamaz."); return; }
        try { var d=_department??new Department(); d.Name=DepartmentNameBox.Text; _service.SaveDepartment(d); NewDepartment_Click(sender,e); LoadData(); Changed(); } catch (SqliteException ex) when(ex.SqliteErrorCode==19) { ShowError("Bu departman zaten kayıtlı."); }
    }
    private void SavePosition_Click(object sender, RoutedEventArgs e)
    {
        if (PositionDepartmentBox.SelectedValue is not long departmentId || string.IsNullOrWhiteSpace(PositionNameBox.Text)) { ShowError("Departman seçin ve pozisyon adını girin."); return; }
        try { var p=_position??new Position(); p.DepartmentId=departmentId; p.Name=PositionNameBox.Text; _service.SavePosition(p); NewPosition_Click(sender,e); LoadData(); Changed(); } catch (SqliteException ex) when(ex.SqliteErrorCode==19) { ShowError("Bu pozisyon seçilen departmanda zaten kayıtlı."); }
    }
    private void DepartmentStatus_Click(object sender, RoutedEventArgs e) { if(_department is null)return; _service.SetDepartmentActive(_department.Id,!_department.IsActive); NewDepartment_Click(sender,e); LoadData(); Changed(); }
    private void PositionStatus_Click(object sender, RoutedEventArgs e) { if(_position is null)return; _service.SetPositionActive(_position.Id,!_position.IsActive); NewPosition_Click(sender,e); LoadData(); Changed(); }
    private void Changed() { OrganizationErrorText.Visibility=Visibility.Collapsed; DataChanged?.Invoke(this,EventArgs.Empty); }
    private void ShowError(string message) { OrganizationErrorText.Text=message; OrganizationErrorText.Visibility=Visibility.Visible; }
}
