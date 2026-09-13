using System.Globalization;
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
        NationalIdBox.Text = employee.NationalId; GenderBox.Text = employee.Gender; MaritalStatusBox.Text = employee.MaritalStatus;
        BirthDatePicker.SelectedDate = employee.BirthDate; BirthPlaceBox.Text = employee.BirthPlace;
        MotherNameBox.Text = employee.MotherName; FatherNameBox.Text = employee.FatherName;
        EmailBox.Text = employee.Email; PhoneBox.Text = employee.Phone; AddressBox.Text = employee.Address; CityBox.Text = employee.City;
        EmergencyNameBox.Text = employee.EmergencyContactName; EmergencyPhoneBox.Text = employee.EmergencyContactPhone;
        EducationBox.Text = employee.EducationLevel; BloodTypeBox.Text = employee.BloodType;
        DepartmentBox.SelectedValue = employee.Department; LoadPositions(); PositionBox.SelectedValue = employee.Position;
        EmploymentTypeBox.Text = employee.EmploymentType; StartDatePicker.SelectedDate = employee.StartDate;
        GrossSalaryBox.Text = employee.GrossSalary == 0 ? string.Empty : employee.GrossSalary.ToString("N2"); IbanBox.Text = employee.Iban;
        StatusButton.Content = employee.IsActive ? "Personeli pasife al" : "Personeli yeniden aktifleştir";
        StatusButton.Visibility = Visibility.Visible; ErrorText.Visibility = Visibility.Collapsed;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var error = ValidateForm();
        if (error is not null) { ShowError(error); return; }
        var employee = _selectedEmployee ?? new Employee();
        employee.EmployeeNumber = NumberBox.Text; employee.FirstName = FirstNameBox.Text; employee.LastName = LastNameBox.Text;
        employee.NationalId = NationalIdBox.Text; employee.Gender = GenderBox.Text; employee.MaritalStatus = MaritalStatusBox.Text;
        employee.BirthDate = BirthDatePicker.SelectedDate; employee.BirthPlace = BirthPlaceBox.Text;
        employee.MotherName = MotherNameBox.Text; employee.FatherName = FatherNameBox.Text;
        employee.Email = EmailBox.Text; employee.Phone = PhoneBox.Text; employee.Address = AddressBox.Text; employee.City = CityBox.Text;
        employee.EmergencyContactName = EmergencyNameBox.Text; employee.EmergencyContactPhone = EmergencyPhoneBox.Text;
        employee.EducationLevel = EducationBox.Text; employee.BloodType = BloodTypeBox.Text;
        employee.Department = DepartmentBox.SelectedValue?.ToString() ?? string.Empty;
        employee.Position = PositionBox.SelectedValue?.ToString() ?? string.Empty; employee.EmploymentType = EmploymentTypeBox.Text;
        employee.StartDate = StartDatePicker.SelectedDate!.Value; employee.GrossSalary = ParseSalary(); employee.Iban = IbanBox.Text;
        try { _employeeService.Save(employee); LoadEmployees(); ClearForm(); DataChanged?.Invoke(this, EventArgs.Empty); }
        catch (SqliteException exception) when (exception.SqliteErrorCode == 19) { ShowError("Sicil veya T.C. kimlik numarası başka bir personelde kullanılıyor."); }
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
        var nationalId = NationalIdBox.Text.Trim();
        if (nationalId.Length > 0 && (nationalId.Length != 11 || !nationalId.All(char.IsDigit))) return "T.C. kimlik numarası 11 rakamdan oluşmalıdır.";
        if (BirthDatePicker.SelectedDate > DateTime.Today) return "Doğum tarihi gelecekte olamaz.";
        if (BirthDatePicker.SelectedDate > StartDatePicker.SelectedDate) return "Doğum tarihi işe başlangıç tarihinden sonra olamaz.";
        decimal salary = 0;
        if (!string.IsNullOrWhiteSpace(GrossSalaryBox.Text) && !decimal.TryParse(GrossSalaryBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out salary)) return "Aylık brüt maaş için geçerli bir tutar girin.";
        if (salary < 0) return "Aylık brüt maaş negatif olamaz.";
        var iban = IbanBox.Text.Replace(" ", string.Empty);
        if (iban.Length > 0 && (iban.Length != 26 || !iban.StartsWith("TR", StringComparison.OrdinalIgnoreCase))) return "IBAN, TR ile başlayan 26 karakterden oluşmalıdır.";
        return null;
    }

    private void ClearForm()
    {
        _selectedEmployee = null; FormTitle.Text = "Yeni personel";
        NumberBox.Clear(); FirstNameBox.Clear(); LastNameBox.Clear(); NationalIdBox.Clear(); GenderBox.SelectedIndex = -1; MaritalStatusBox.SelectedIndex = -1;
        BirthDatePicker.SelectedDate = null; BirthPlaceBox.Clear(); MotherNameBox.Clear(); FatherNameBox.Clear();
        EmailBox.Clear(); PhoneBox.Clear(); AddressBox.Clear(); CityBox.Clear(); EmergencyNameBox.Clear(); EmergencyPhoneBox.Clear();
        EducationBox.SelectedIndex = -1; BloodTypeBox.SelectedIndex = -1; DepartmentBox.SelectedItem = null; PositionBox.ItemsSource = null;
        EmploymentTypeBox.SelectedIndex = -1; StartDatePicker.SelectedDate = DateTime.Today; GrossSalaryBox.Clear(); IbanBox.Clear();
        StatusButton.Visibility = Visibility.Collapsed; ErrorText.Visibility = Visibility.Collapsed;
    }

    private void ShowError(string message) { ErrorText.Text = message; ErrorText.Visibility = Visibility.Visible; }
    private void DepartmentBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => LoadPositions();
    private void LoadPositions() => PositionBox.ItemsSource = DepartmentBox.SelectedItem is Department department ? _organizationService.GetPositions(department.Id) : null;
    private decimal ParseSalary() => decimal.TryParse(GrossSalaryBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) ? value : 0;
}
