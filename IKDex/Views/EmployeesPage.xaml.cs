using System.Net.Mail;
using System.Windows;
using System.Windows.Controls;
using IKDex.Models;
using IKDex.Services;
using Microsoft.Data.Sqlite;

namespace IKDex.Views;

public partial class EmployeesPage : UserControl
{
    private readonly EmployeeService _employeeService = new(App.Database);
    private readonly OrganizationService _organizationService = new(App.Database);
    private Employee? _selectedEmployee;
    public event EventHandler? DataChanged;

    public EmployeesPage(bool startNew = false)
    {
        InitializeComponent();
        DepartmentBox.ItemsSource = _organizationService.GetDepartments();
        StartDatePicker.SelectedDate = DateTime.Today;
        LoadEmployees();
        if (startNew) ClearForm();
    }

    private void LoadEmployees()
    {
        var employees = _employeeService.GetAll(SearchBox.Text, IncludeInactiveCheck.IsChecked == true);
        EmployeesGrid.ItemsSource = employees;
        CountText.Text = $"{employees.Count} personel listeleniyor";
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) LoadEmployees(); }
    private void Filter_Changed(object sender, RoutedEventArgs e) { if (IsLoaded) LoadEmployees(); }
    private void NewButton_Click(object sender, RoutedEventArgs e) { EmployeesGrid.SelectedItem = null; ClearForm(); }

    private void EmployeesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EmployeesGrid.SelectedItem is not Employee employee) return;
        _selectedEmployee = employee;
        FormTitle.Text = "Personeli düzenle";
        NumberBox.Text = employee.EmployeeNumber; FirstNameBox.Text = employee.FirstName; LastNameBox.Text = employee.LastName;
        EmailBox.Text = employee.Email; PhoneBox.Text = employee.Phone; DepartmentBox.SelectedValue = employee.Department;
        LoadPositions(); PositionBox.SelectedValue = employee.Position; StartDatePicker.SelectedDate = employee.StartDate;
        StatusButton.Content = employee.IsActive ? "Personeli pasife al" : "Personeli yeniden aktifleştir";
        StatusButton.Visibility = Visibility.Visible; ErrorText.Visibility = Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var error = ValidateForm();
        if (error is not null) { ShowError(error); return; }
        var employee = _selectedEmployee ?? new Employee();
        employee.EmployeeNumber = NumberBox.Text; employee.FirstName = FirstNameBox.Text; employee.LastName = LastNameBox.Text;
        employee.Email = EmailBox.Text; employee.Phone = PhoneBox.Text; employee.Department = DepartmentBox.SelectedValue?.ToString() ?? string.Empty;
        employee.Position = PositionBox.SelectedValue?.ToString() ?? string.Empty; employee.StartDate = StartDatePicker.SelectedDate!.Value;
        try
        {
            _employeeService.Save(employee); LoadEmployees(); ClearForm(); DataChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19)
        { ShowError("Bu sicil numarası başka bir personel tarafından kullanılıyor."); }
    }

    private void StatusButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedEmployee is null) return;
        var action = _selectedEmployee.IsActive ? "pasife almak" : "yeniden aktifleştirmek";
        if (MessageBox.Show($"{_selectedEmployee.FullName} adlı personeli {action} istiyor musunuz?", "Durum değişikliği", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _employeeService.SetActive(_selectedEmployee.Id, !_selectedEmployee.IsActive);
        LoadEmployees(); ClearForm(); DataChanged?.Invoke(this, EventArgs.Empty);
    }

    private string? ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(NumberBox.Text) || string.IsNullOrWhiteSpace(FirstNameBox.Text) || string.IsNullOrWhiteSpace(LastNameBox.Text) || DepartmentBox.SelectedValue is null || PositionBox.SelectedValue is null || StartDatePicker.SelectedDate is null)
            return "Yıldızlı alanların tümünü doldurun.";
        if (!string.IsNullOrWhiteSpace(EmailBox.Text)) try { _ = new MailAddress(EmailBox.Text.Trim()); } catch (FormatException) { return "Geçerli bir e-posta adresi girin."; }
        return null;
    }

    private void ClearForm()
    {
        _selectedEmployee = null; FormTitle.Text = "Yeni personel";
        NumberBox.Clear(); FirstNameBox.Clear(); LastNameBox.Clear(); EmailBox.Clear(); PhoneBox.Clear(); DepartmentBox.SelectedItem=null; PositionBox.ItemsSource=null;
        StartDatePicker.SelectedDate = DateTime.Today; StatusButton.Visibility = Visibility.Collapsed; ErrorText.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message) { ErrorText.Text = message; ErrorText.Visibility = Visibility.Visible; }

    private void DepartmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadPositions();

    private void LoadPositions()
    {
        if (DepartmentBox.SelectedItem is Department department)
            PositionBox.ItemsSource = _organizationService.GetPositions(department.Id);
        else
            PositionBox.ItemsSource = null;
    }
}
