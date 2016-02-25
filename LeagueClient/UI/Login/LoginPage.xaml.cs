using LeagueClient.Logic;
using LeagueClient.Logic.Riot.Platform;
using LeagueClient.Logic.Settings;
using MFroehlich.League.Assets;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml;

namespace LeagueClient.UI.Login {
  /// <summary>
  /// Interaction logic for LoginPage.xaml
  /// </summary>
  public partial class LoginPage : Page {
    private const string SettingsKey = "LoginSettings";
    private static LoginSettings settings = Client.LoadSettings<LoginSettings>(SettingsKey);

    private int tries;
    private string user;
    private string pass;

    public LoginPage() {
      InitializeComponent();

      LoadBackground();

      if (settings.Accounts.Count > 0) {
        foreach (var name in settings.Accounts) {
          var user = Client.LoadSettings<UserSettings>(name);
          var login = new LoginAccount(user.Username, user.SummonerName, user.ProfileIcon);
          login.Click += Account_Click;
          login.Remove += Account_Remove;
          AccountList.Children.Insert(0, login);
        }
        AccountList.BeginStoryboard(App.FadeIn);
      } else {
        LoginGrid.BeginStoryboard(App.FadeIn);
      }
    }

    private void LoadBackground() {
      if (File.Exists(Client.LoginStaticPath) && BackStatic.Source == null) {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
        image.UriSource = new Uri(Client.LoginStaticPath);
        image.EndInit();
        BackStatic.Source = image;
      }
      if (File.Exists(Client.LoginVideoPath) && BackAnim.Source == null)
        BackAnim.Source = new Uri(Client.LoginVideoPath);
      BackAnim.Play();
    }

    private void VideoLoop() {
      try {
        while (true) {
          var duration = Dispatcher.Invoke(() => {
            int diff = (int) (BackAnim.NaturalDuration.TimeSpan.TotalMilliseconds - BackAnim.Position.TotalMilliseconds);
            if (diff < 100) {
              BackAnim.Position = TimeSpan.FromSeconds(0);
              BackAnim.Play();
            } else if (diff < 2000) return diff - 10;
            return 2000;
          });
          Thread.Sleep(duration);
        }
      } catch { }
    }

    #region UI Handlers
    private void Account_Remove(object sender, EventArgs e) {
      AccountList.Children.Remove(sender as UIElement);
      settings.Accounts.Remove(((LoginAccount) sender).Username);
      Client.SaveSettings(SettingsKey, settings);
    }

    private void Account_Click(object sender, EventArgs e) {
      if (!DataDragon.IsCurrent) return;

      AutoLoginToggle.IsChecked = true;
      var login = (sender as LoginAccount).Username;
      foreach (var obj in AccountList.Children)
        if (obj is LoginAccount) ((LoginAccount) obj).State = LoginAccountState.Readonly;
      (sender as LoginAccount).State = LoginAccountState.Loading;

      var account = Client.LoadSettings<UserSettings>(login);
      var raw = account.Password;
      var decrypted = ProtectedData.Unprotect(Convert.FromBase64String(raw), null, DataProtectionScope.CurrentUser);
      var junk = "";
      for (int i = 0; i < decrypted.Length; i++) junk += " ";
      PassBox.Password = junk;
      tries = 3;

      Login(user = account.Username, pass = Encoding.UTF8.GetString(decrypted));
    }

    private void Login_Click(object sender, RoutedEventArgs e) {
      if (!DataDragon.IsCurrent) return;

      user = UserBox.Text;
      pass = PassBox.Password;

      if (AutoLoginToggle.IsChecked ?? false)
        SaveAccount(user, pass);

      tries = 3;
      Login(user, pass);
    }

    private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      if (e.ChangedButton == MouseButton.Left)
        if (e.ClickCount == 2) Client.MainWindow.Center();
        else Client.MainWindow.DragMove();
    }

    private void AddAccountButt_Click(object sender, RoutedEventArgs e) {
      LoginGrid.BeginStoryboard(App.FadeIn);
      AccountList.BeginStoryboard(App.FadeOut);
      Dispatcher.Invoke(UserBox.Focus, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void ShowSavedAccounts_Click(object sender, RoutedEventArgs e) {
      LoginGrid.BeginStoryboard(App.FadeOut);
      AccountList.BeginStoryboard(App.FadeIn);
    }
    #endregion

    #region Patching

    //private void DataDragon_Progress(string status, string detail, double progress) {
    //  OverallStatusText.Content = "Patching Custom Client";
    //  switch (status) {
    //    case "Downloading": OverallStatusBar.Value = .0; break;
    //    case "Extracting": OverallStatusBar.Value = .25; break;
    //    case "Installing": OverallStatusBar.Value = .50; break;
    //    case "done": LoginPageUI(); return;
    //  }
    //  CurrentStatusText.Content = $"{status} {detail}...";
    //  CurrentStatusBar.Value = progress;
    //  if (progress < 0) {
    //    CurrentStatusProgress.Content = "";
    //    CurrentStatusBar.IsIndeterminate = true;
    //  } else {
    //    CurrentStatusProgress.Content = (progress * 100).ToString("f1") + "%";
    //    CurrentStatusBar.IsIndeterminate = false;
    //  }
    //}

    //public static bool NeedsPatch() {
    //  return !File.Exists(Client.LoginVideoPath) || !File.Exists(Client.LoginStaticPath) || !Client.LoginTheme.Equals(settings.Theme);
    //}

    #endregion

    private void SaveAccount(string name, string pass) {
      var account = Client.LoadSettings<UserSettings>(name);
      var rawPass = ProtectedData.Protect(Encoding.UTF8.GetBytes(pass), null, DataProtectionScope.CurrentUser);
      account.Password = Convert.ToBase64String(rawPass);
      account.Username = name;

      settings.Accounts.Add(name);
      Client.SaveSettings(name, account);
      Client.SaveSettings(SettingsKey, settings);
    }

    private void Login(string user, string pass) {
      Progress.Visibility = Visibility.Visible;
      LoginBar.IsIndeterminate = true;
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled = AutoLoginToggle.IsEnabled = false;

      Client.Login(user, pass).ContinueWith(HandleLogin);
    }

    private void HandleLogin(Task<Client> obj) {
      if (!obj.IsFaulted && obj.Result != null) {
        Client.Session.ChatManager = new Logic.Chat.RiotChat();
        Client.SaveSettings(SettingsKey, settings);
        Patch.Dispose();
        Dispatcher.Invoke(Client.MainWindow.LoginComplete);
        return;
      } else if (obj.IsFaulted && !obj.Exception.InnerException.Message.Contains("SSL error")) {
        var error = obj.Exception.InnerException as InvocationException;
        var cause = error?.RootCause as RiotException;
      }
      Dispatcher.Invoke(Reset);
    }

    private void Reset() {
      if (tries > 0) {
        tries--;
        Login(user, pass);
        return;
      }

      foreach (var obj in AccountList.Children)
        if (obj is LoginAccount) ((LoginAccount) obj).State = LoginAccountState.Normal;
      Progress.Visibility = Visibility.Hidden;
      LoginBar.IsIndeterminate = false;
      PassBox.Password = "";
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled = AutoLoginToggle.IsEnabled = true;
      PassBox.Focus();
    }

    private void BackAnim_MediaOpened(object sender, RoutedEventArgs e) => new Thread(VideoLoop) { IsBackground = true }.Start();
  }
}
