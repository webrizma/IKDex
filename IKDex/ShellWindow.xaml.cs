using System.Globalization;
using System.Windows;
using IKDex.Models;
using IKDex.Services;
using IKDex.Views;

namespace IKDex;

public partial class ShellWindow : Window
{
    private readonly UserSession _session;
    private readonly EmployeeService _employeeService = new(App.Database);
    private readonly OrganizationService _organizationService = new(App.Database);
    private readonly LeaveService _leaveService = new(App.Database);
    private readonly long _userId;
    private readonly AuthorizationService _authorization;

    public ShellWindow(UserSession session)
    {
        InitializeComponent();
        _session = session;
        _userId = session.Id;
        _authorization = new AuthorizationService(session);
        EmployeesNavButton.Visibility = _authorization.CanManageEmployees ? Visibility.Visible : Visibility.Collapsed;
        OrganizationNavButton.Visibility = _authorization.CanManageOrganization ? Visibility.Visible : Visibility.Collapsed;
        UsersNavButton.Visibility = _authorization.CanManageUsers ? Visibility.Visible : Visibility.Collapsed;
        SettingsNavButton.Visibility = _authorization.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        ReportsNavButton.Visibility = _authorization.CanManageEmployees ? Visibility.Visible : Visibility.Collapsed;
        PayrollNavButton.Visibility = _authorization.CanManagePayroll ? Visibility.Visible : Visibility.Collapsed;
        UserNameText.Text = session.FullName;
        UserRoleText.Text = session.Role;
        RefreshCompanyName();
        InitialsText.Text = GetInitials(session.FullName);
        WelcomeText.Text = $"{DateTime.Now.ToString("d MMMM yyyy, dddd", CultureInfo.GetCultureInfo("tr-TR"))} • Ekibinizde bugün neler oluyor?";
        RefreshDashboard();
        Loaded += async (_, _) => await CheckForUpdatesOnStartupAsync();
    }

    private void RefreshDashboard()
    {
        var count = _employeeService.GetActiveCount();
        EmployeeCountText.Text = count.ToString();
        ActiveSummaryText.Text = count.ToString();
        DepartmentCountText.Text = _organizationService.GetDepartments().Count.ToString();
        TodayLeaveCountText.Text = _leaveService.GetTodayApprovedCount().ToString();
        PendingLeaveCountText.Text = _leaveService.GetPendingCount().ToString();
    }

    private void OpenEmployees(bool startNew = false)
    {
        if (!_authorization.CanManageEmployees) return;
        var page = new EmployeesPage(startNew);
        page.DataChanged += (_, _) => RefreshDashboard();
        PageHost.Content = page;
        PageHost.Visibility = Visibility.Visible;
        DashboardView.Visibility = Visibility.Collapsed;
        DashboardNavButton.Style = (Style)FindResource("NavButton");
        EmployeesNavButton.Style = (Style)FindResource("ActiveNavButton");
        OrganizationNavButton.Style = (Style)FindResource("NavButton");
        LeaveNavButton.Style = (Style)FindResource("NavButton");
        UsersNavButton.Style = (Style)FindResource("NavButton");
    }

    private void DashboardNavButton_Click(object sender, RoutedEventArgs e)
    {
        PageHost.Visibility = Visibility.Collapsed;
        DashboardView.Visibility = Visibility.Visible;
        DashboardNavButton.Style = (Style)FindResource("ActiveNavButton");
        EmployeesNavButton.Style = (Style)FindResource("NavButton");
        OrganizationNavButton.Style = (Style)FindResource("NavButton");
        LeaveNavButton.Style = (Style)FindResource("NavButton");
        UsersNavButton.Style = (Style)FindResource("NavButton");
        RefreshDashboard();
    }
    private void OrganizationNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_authorization.CanManageOrganization) return;
        var page = new OrganizationPage();
        page.DataChanged += (_, _) => RefreshDashboard();
        PageHost.Content = page; PageHost.Visibility = Visibility.Visible; DashboardView.Visibility = Visibility.Collapsed;
        DashboardNavButton.Style = (Style)FindResource("NavButton"); EmployeesNavButton.Style = (Style)FindResource("NavButton"); OrganizationNavButton.Style = (Style)FindResource("ActiveNavButton");
        LeaveNavButton.Style = (Style)FindResource("NavButton");
        UsersNavButton.Style = (Style)FindResource("NavButton");
    }
    private void LeaveNavButton_Click(object sender, RoutedEventArgs e)
    {
        var page = new LeavePage(_session);
        page.DataChanged += (_, _) => RefreshDashboard();
        PageHost.Content = page; PageHost.Visibility = Visibility.Visible; DashboardView.Visibility = Visibility.Collapsed;
        DashboardNavButton.Style = (Style)FindResource("NavButton"); EmployeesNavButton.Style = (Style)FindResource("NavButton"); OrganizationNavButton.Style = (Style)FindResource("NavButton"); LeaveNavButton.Style = (Style)FindResource("ActiveNavButton");
        UsersNavButton.Style = (Style)FindResource("NavButton");
    }
    private void UsersNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_authorization.CanManageUsers) return;
        PageHost.Content = new UsersPage(_userId); PageHost.Visibility = Visibility.Visible; DashboardView.Visibility = Visibility.Collapsed;
        DashboardNavButton.Style=(Style)FindResource("NavButton"); EmployeesNavButton.Style=(Style)FindResource("NavButton"); OrganizationNavButton.Style=(Style)FindResource("NavButton"); LeaveNavButton.Style=(Style)FindResource("NavButton"); UsersNavButton.Style=(Style)FindResource("ActiveNavButton");
    }
    private void AttendanceNavButton_Click(object sender, RoutedEventArgs e)
    {
        var page = new AttendancePage(_session);
        page.DataChanged += (_, _) => RefreshDashboard();
        PageHost.Content = page; PageHost.Visibility = Visibility.Visible; DashboardView.Visibility = Visibility.Collapsed;
        DashboardNavButton.Style=(Style)FindResource("NavButton"); EmployeesNavButton.Style=(Style)FindResource("NavButton"); OrganizationNavButton.Style=(Style)FindResource("NavButton"); LeaveNavButton.Style=(Style)FindResource("NavButton"); UsersNavButton.Style=(Style)FindResource("NavButton"); AttendanceNavButton.Style=(Style)FindResource("ActiveNavButton");
    }
    private void ReportsNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_authorization.CanManageEmployees) return;
        PageHost.Content=new ReportsPage();PageHost.Visibility=Visibility.Visible;DashboardView.Visibility=Visibility.Collapsed;
        DashboardNavButton.Style=(Style)FindResource("NavButton");EmployeesNavButton.Style=(Style)FindResource("NavButton");OrganizationNavButton.Style=(Style)FindResource("NavButton");LeaveNavButton.Style=(Style)FindResource("NavButton");AttendanceNavButton.Style=(Style)FindResource("NavButton");UsersNavButton.Style=(Style)FindResource("NavButton");ReportsNavButton.Style=(Style)FindResource("ActiveNavButton");
    }
    private void PayrollNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_authorization.CanManagePayroll) return;
        PageHost.Content=new PayrollPage();PageHost.Visibility=Visibility.Visible;DashboardView.Visibility=Visibility.Collapsed;
        DashboardNavButton.Style=(Style)FindResource("NavButton");EmployeesNavButton.Style=(Style)FindResource("NavButton");OrganizationNavButton.Style=(Style)FindResource("NavButton");LeaveNavButton.Style=(Style)FindResource("NavButton");AttendanceNavButton.Style=(Style)FindResource("NavButton");UsersNavButton.Style=(Style)FindResource("NavButton");ReportsNavButton.Style=(Style)FindResource("NavButton");DocumentsNavButton.Style=(Style)FindResource("NavButton");SettingsNavButton.Style=(Style)FindResource("NavButton");PayrollNavButton.Style=(Style)FindResource("ActiveNavButton");
    }
    private void DocumentsNavButton_Click(object sender, RoutedEventArgs e)
    {
        PageHost.Content=new DocumentsPage(_session);PageHost.Visibility=Visibility.Visible;DashboardView.Visibility=Visibility.Collapsed;
        DashboardNavButton.Style=(Style)FindResource("NavButton");EmployeesNavButton.Style=(Style)FindResource("NavButton");OrganizationNavButton.Style=(Style)FindResource("NavButton");LeaveNavButton.Style=(Style)FindResource("NavButton");AttendanceNavButton.Style=(Style)FindResource("NavButton");UsersNavButton.Style=(Style)FindResource("NavButton");ReportsNavButton.Style=(Style)FindResource("NavButton");DocumentsNavButton.Style=(Style)FindResource("ActiveNavButton");
    }
    private void SettingsNavButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_authorization.IsAdministrator) return;
        var page=new SettingsPage();page.SettingsChanged+=(_,_)=>RefreshCompanyName();PageHost.Content=page;PageHost.Visibility=Visibility.Visible;DashboardView.Visibility=Visibility.Collapsed;
        DashboardNavButton.Style=(Style)FindResource("NavButton");EmployeesNavButton.Style=(Style)FindResource("NavButton");OrganizationNavButton.Style=(Style)FindResource("NavButton");LeaveNavButton.Style=(Style)FindResource("NavButton");AttendanceNavButton.Style=(Style)FindResource("NavButton");UsersNavButton.Style=(Style)FindResource("NavButton");ReportsNavButton.Style=(Style)FindResource("NavButton");DocumentsNavButton.Style=(Style)FindResource("NavButton");SettingsNavButton.Style=(Style)FindResource("ActiveNavButton");
    }
    private void RefreshCompanyName()=>CompanyTitleText.Text=new SettingsService(App.Database).Get().CompanyName;
    private async Task CheckForUpdatesOnStartupAsync()
    {
        var repositoryUrl=UpdateConfiguration.GetRepositoryUrl();
        if(string.IsNullOrWhiteSpace(repositoryUrl))return;
        try
        {
            var service=new UpdateService();var result=await service.CheckAsync(repositoryUrl);
            if(result.Update is null||result.Manager is null)return;
            if(MessageBox.Show($"{result.Message}\n\nŞimdi indirip güncellemek ister misiniz?","Yeni IKDex sürümü",MessageBoxButton.YesNo,MessageBoxImage.Information)!=MessageBoxResult.Yes)return;
            await service.DownloadAndApplyAsync(result.Manager,result.Update,_=>{});
        }
        catch(Exception exception)
        {
            System.Diagnostics.Debug.WriteLine($"Güncelleme kontrolü başarısız: {exception}");
        }
    }
    private void EmployeesNavButton_Click(object sender, RoutedEventArgs e) => OpenEmployees();
    private void NewEmployeeButton_Click(object sender, RoutedEventArgs e) => OpenEmployees(true);
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var login = new LoginWindow();
        Application.Current.MainWindow = login;
        login.Show();
        Close();
    }

    private static string GetInitials(string fullName) => string.Concat(fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(x => char.ToUpperInvariant(x[0])));
}
