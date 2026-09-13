# IKDex

Windows üzerinde çalışan, WPF ve .NET 10 ile hazırlanmış masaüstü insan kaynakları uygulaması prototipi.

## Çalıştırma

Visual Studio ile `IKDex.slnx` dosyasını açıp projeyi başlatın veya terminalde:

```powershell
dotnet run --project IKDex/IKDex.csproj
```

İlk çalıştırmada SQLite veritabanı otomatik oluşturulur ve başlangıç yöneticisi eklenir:

- Kullanıcı adı: `admin`
- Şifre: `1234`

Veritabanı `%LOCALAPPDATA%\IKDex\ikdex.db` konumunda tutulur. Parolalar PBKDF2-SHA256 ile rastgele salt kullanılarak özetlenir; düz metin parola saklanmaz. Üretim kullanımından önce başlangıç parolası değiştirilmelidir.

## İlk sürümde bulunanlar

- Kullanıcı adı ve şifre ile giriş
- Boş ve hatalı bilgi doğrulaması
- Enter tuşuyla giriş
- Temel İK genel bakış ekranı
- Oturumu kapatma
- SQLite kullanıcı veritabanı
- PBKDF2-SHA256 ile güvenli parola saklama
- Personel ekleme ve düzenleme
- Personel arama ve aktif/pasif filtreleme
- Kayıt silmek yerine personeli pasife alma
- Ana pencere içinde kesintisiz sayfa geçişi
- Departman ve departmana bağlı pozisyon yönetimi
- Personel formunda kontrollü departman/pozisyon seçimi
- İzin talebi oluşturma, tarih çakışması kontrolü ve iş günü hesaplama
- İzin onaylama ve reddetme akışı
- İK Yöneticisi, İK Uzmanı ve Çalışan rolleri
- Rol bazlı menü yetkilendirmesi ve kullanıcı hesap yönetimi
- Günlük giriş-çıkış kaydı ve aylık puantaj görünümü
- Otomatik çalışma süresi hesabı ve role göre puantaj erişimi
- Personel, izin ve puantaj raporlarını Excel/PDF biçiminde dışa aktarma
- Personel özlük belgelerini yükleme, açma, kategorilendirme ve arşivleme
- Şirket bilgileri yönetimi
- Veritabanı ve belgeler için bütünlük kontrollü yedekleme/geri yükleme
- Aylık bordro, kazanç/kesinti, net ücret ve ödeme durumu yönetimi

## Windows sürümü oluşturma

```powershell
.\build-release.ps1
```

Betik, .NET kurulumu gerektirmeyen `win-x64` yayınını ve taşınabilir ZIP paketini `artifacts` klasöründe oluşturur. Inno Setup 6 yüklüyse imzalanmamış Windows kurulum EXE'sini de üretir.

## GitHub üzerinden güncelleme

GitHub Actions, `v1.0.0` biçiminde bir etiket gönderildiğinde Velopack kurulum ve güncelleme paketlerini GitHub Releases'a yükler. Uygulama GitHub depo adresini yayın sırasında otomatik alır, açılışta güncelleme kontrol eder ve kullanıcı onayıyla indirip yeniden başlatır.
