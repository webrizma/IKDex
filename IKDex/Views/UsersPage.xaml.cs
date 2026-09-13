using System.Windows;using System.Windows.Controls;using IKDex.Models;using IKDex.Services;using Microsoft.Data.Sqlite;
namespace IKDex.Views;
public partial class UsersPage:UserControl
{
 private readonly UserService _service=new(App.Database);private readonly long _currentUserId;private UserAccount? _selected;
 public UsersPage(long currentUserId){_currentUserId=currentUserId;InitializeComponent();EmployeeBox.ItemsSource=new EmployeeService(App.Database).GetAll();LoadData();}
 private void LoadData()=>UsersGrid.ItemsSource=_service.GetAll();
 private void New_Click(object sender,RoutedEventArgs e){_selected=null;UsersGrid.SelectedItem=null;FormTitle.Text="Yeni kullanıcı";UserNameBox.Clear();FullNameBox.Clear();RoleBox.SelectedItem=null;EmployeeBox.SelectedItem=null;PasswordBox.Clear();StatusButton.Visibility=Visibility.Collapsed;ErrorText.Visibility=Visibility.Collapsed;}
 private void UsersGrid_SelectionChanged(object sender,SelectionChangedEventArgs e){if(UsersGrid.SelectedItem is not UserAccount u)return;_selected=u;FormTitle.Text="Kullanıcıyı düzenle";UserNameBox.Text=u.UserName;FullNameBox.Text=u.FullName;SelectRole(u.Role);EmployeeBox.SelectedValue=u.EmployeeId;PasswordBox.Clear();StatusButton.Content=u.IsActive?"Hesabı pasife al":"Hesabı aktifleştir";StatusButton.Visibility=u.Id==_currentUserId?Visibility.Collapsed:Visibility.Visible;}
 private void Save_Click(object sender,RoutedEventArgs e){if(string.IsNullOrWhiteSpace(UserNameBox.Text)||string.IsNullOrWhiteSpace(FullNameBox.Text)||RoleBox.SelectedItem is not ComboBoxItem role){ShowError("Kullanıcı adı, ad soyad ve rol zorunludur.");return;}var u=_selected??new UserAccount();u.UserName=UserNameBox.Text;u.FullName=FullNameBox.Text;u.Role=role.Content.ToString()!;u.EmployeeId=EmployeeBox.SelectedValue is long id?id:null;try{_service.Save(u,string.IsNullOrEmpty(PasswordBox.Password)?null:PasswordBox.Password);New_Click(sender,e);LoadData();}catch(InvalidOperationException ex){ShowError(ex.Message);}catch(SqliteException ex)when(ex.SqliteErrorCode==19){ShowError("Kullanıcı adı zaten kullanılıyor.");}}
 private void Status_Click(object sender,RoutedEventArgs e){if(_selected is null||_selected.Id==_currentUserId)return;_service.SetActive(_selected.Id,!_selected.IsActive);New_Click(sender,e);LoadData();}
 private void SelectRole(string role){foreach(ComboBoxItem item in RoleBox.Items)if(item.Content.ToString()==role){RoleBox.SelectedItem=item;break;}}
 private void ShowError(string message){ErrorText.Text=message;ErrorText.Visibility=Visibility.Visible;}
}
