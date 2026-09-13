using System.Globalization;
using System.Windows;
using IKDex.Models;
using IKDex.Services;

namespace IKDex;

public partial class DashboardWindow : Window
{
    private readonly EmployeeService _employeeService = new(App.Database);
    public DashboardWindow(UserSession session)
    {
        InitializeComponent();
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        WelcomeText.Text = $"Hoş geldin, {session.FullName}. Bugün {DateTime.Now.ToString("d MMMM yyyy, dddd", culture)}.";
        UserFullNameText.Text = session.FullName;
        UserRoleText.Text = session.Role;
        RefreshEmployeeCount();
    }

    private void EmployeesButton_Click(object sender, RoutedEventArgs e)
    {
        new EmployeesWindow { Owner = this }.ShowDialog();
        RefreshEmployeeCount();
    }

    private void RefreshEmployeeCount() => EmployeeCountText.Text = _employeeService.GetActiveCount().ToString();

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }
}
