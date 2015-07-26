using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for LoginPage.xaml
  /// </summary>
  public partial class LoginPage : Page {
    private int tries;
    private string user;
    private string pass;

    public LoginPage() {
      InitializeComponent();
      BackAnimation.Source = new Uri(Client.LoginVideoPath);
      BackAnimation.Play();
      UserBox.Focus();
      //BackStatic.Source = new BitmapImage(new Uri(Path.Combine(Client.AirDirectory,
      //  "mod\\lgn\\themes", Client.LoginTheme, "cs_bg_champions.png")));
      if ((AnimationToggle.IsChecked = Client.Settings.Animation).Value)
        BackAnimation.Visibility = System.Windows.Visibility.Visible;
      else
        BackAnimation.Visibility = System.Windows.Visibility.Collapsed;

      UserBox.Text = Client.Settings.Username;
      if (Client.Settings.Username.Length > 0) PassBox.Focus();
      else UserBox.Focus();

      if (Client.Settings.AutoLogin) {
        AutoLoginToggle.IsChecked = true;
        var raw = Client.Settings.Password;
        var decrypted = ProtectedData.Unprotect(raw, null, DataProtectionScope.CurrentUser);
        var junk = "";
        for (int i = 0; i < decrypted.Length; i++) junk += " ";
        PassBox.Password = junk;
        tries = 3;
        Login(user = Client.Settings.Username, pass = Encoding.UTF8.GetString(decrypted));
      }
    }

    private void Button_Click(object sender, RoutedEventArgs e) {
      user = UserBox.Text;
      pass = PassBox.Password;
      Client.Settings.Username = user;

      if (AutoLoginToggle.IsChecked.HasValue && AutoLoginToggle.IsChecked.Value)
        SavePassword(pass);

      tries = 3;
      Login(user, pass);
    }

    private void SavePassword(string pass) {
      Client.Settings.Password = ProtectedData.Protect(Encoding.UTF8.GetBytes(pass),
        null, DataProtectionScope.CurrentUser);
      Client.Settings.AutoLogin = true;
    }

    private void Login(string user, string pass) {
      Progress.Visibility = System.Windows.Visibility.Visible;
      LoginBar.IsIndeterminate = true;
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled
        = AnimationToggle.IsEnabled = AutoLoginToggle.IsEnabled = false;

      new Thread(() => {
        Client.Initialize(user, pass).ContinueWith(t => {
          if (!t.IsFaulted && t.Result) Dispatcher.Invoke(Client.MainWindow.LoginComplete);
          else Dispatcher.Invoke(Reset);
        });
      }).Start();
    }

    private void Reset() {
      if(tries > 0) {
        tries--;
        Login(user, pass);
        return;
      }
      if (Client.ChatManager != null)
        Client.ChatManager.Disconnect();
      Progress.Visibility = System.Windows.Visibility.Hidden;
      LoginBar.IsIndeterminate = false;
      PassBox.Password = "";
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled
        = AnimationToggle.IsEnabled = AutoLoginToggle.IsEnabled = true;
      PassBox.Focus();
    }

    private void BackAnimation_MediaEnded(object sender, RoutedEventArgs e) {
      BackAnimation.Position = TimeSpan.Zero;
      BackAnimation.Play();
    }

    private void AnimationToggled(object sender, RoutedEventArgs e) {
      if (AnimationToggle.IsChecked.HasValue && AnimationToggle.IsChecked.Value) {
        BackAnimation.Visibility = System.Windows.Visibility.Visible;
        Client.Settings.Animation = true;
      }else{
        BackAnimation.Visibility = System.Windows.Visibility.Collapsed;
        BackAnimation_MediaEnded(sender, e);
        Client.Settings.Animation = false;
      }
    }
  }
}
