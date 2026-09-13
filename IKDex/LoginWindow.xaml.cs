using System.Windows;
using System.Windows.Input;
using IKDex.Services;

namespace IKDex;

public partial class LoginWindow : Window
{
    private readonly AuthenticationService _authenticationService = new(App.Database);

    public LoginWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => UserNameBox.Focus();
    }

    private void LoginButton_Click(object sender, RoutedEventArgs e) => TryLogin();

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            TryLogin();
    }

    private void TryLogin()
    {
        ErrorText.Visibility = Visibility.Collapsed;

        if (string.IsNullOrWhiteSpace(UserNameBox.Text) || string.IsNullOrWhiteSpace(PasswordBox.Password))
        {
            ShowError("Kullanıcı adı ve şifre alanlarını doldurun.");
            return;
        }

        var session = _authenticationService.Authenticate(UserNameBox.Text, PasswordBox.Password);
        if (session is null)
        {
            ShowError("Kullanıcı adı veya şifre hatalı.");
            PasswordBox.SelectAll();
            PasswordBox.Focus();
            return;
        }

        var shell = new ShellWindow(session);
        Application.Current.MainWindow = shell;
        shell.Show();
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorText.Visibility = Visibility.Visible;
    }
}
