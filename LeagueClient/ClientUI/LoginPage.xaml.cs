using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using LeagueClient.Logic.Riot.Platform;
using LeagueClient.Logic.Settings;
using MFroehlich.League.Assets;
using RtmpSharp.Messaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for LoginPage.xaml
  /// </summary>
  public partial class LoginPage : Page {
    private const string SettingsKey = "LoginSettings";
    private static LoginSettings settings = Client.LoadSettings<LoginSettings>(SettingsKey);

    public Uri BackAnimationURI { get; private set; }

    private int tries;
    private string user;
    private string pass;

    public LoginPage() {
      BackAnimationURI = new Uri(Client.LoginVideoPath);
      InitializeComponent();

      if (settings.Accounts.Count > 0) {
        foreach (var name in settings.Accounts) {
          var user = Client.LoadSettings<UserSettings>(name);
          var login = new LoginAccount(user.Username, user.SummonerName, user.ProfileIcon);
          login.Click += Account_Click;
          login.Remove += Account_Remove;
          AccountList.Children.Insert(0, login);
        }
      } else LoginGrid.BeginStoryboard(App.FadeIn);

      var url = Path.Combine(Client.Region.UpdateBase, $"projects/lol_air_client/releases/{Client.Latest.AirVersion}/files/mod/lgn/themes/{Client.LoginTheme}/cs_bg_champions.png");
      BackStatic.Source = new BitmapImage(new Uri(url));
    }

    #region UI Handlers
    private void Account_Remove(object sender, EventArgs e) {
      AccountList.Children.Remove(sender as UIElement);
      settings.Accounts.Remove(((LoginAccount) sender).Username);
      Client.SaveSettings(SettingsKey, settings);
    }

    private void Account_Click(object sender, EventArgs e) {
      AutoLoginToggle.IsChecked = true;
      var login = (sender as LoginAccount).Username;
      foreach (var obj in AccountList.Children)
        if (obj is LoginAccount) ((LoginAccount) obj).State = LoginAccountState.Readonly;
      (sender as LoginAccount).State = LoginAccountState.Loading;

      Client.Settings = Client.LoadSettings<UserSettings>(login);
      var raw = Client.Settings.Password;
      var decrypted = ProtectedData.Unprotect(Convert.FromBase64String(raw), null, DataProtectionScope.CurrentUser);
      var junk = "";
      for (int i = 0; i < decrypted.Length; i++) junk += " ";
      PassBox.Password = junk;
      tries = 3;
      Login(user = Client.Settings.Username, pass = Encoding.UTF8.GetString(decrypted));
    }

    private void Login_Click(object sender, RoutedEventArgs e) {
      user = UserBox.Text;
      pass = PassBox.Password;

      if (AutoLoginToggle.IsChecked ?? false)
        SaveAccount(user, pass);

      tries = 3;
      Login(user, pass);
    }

    private void Border_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e) {
      Client.MainWindow.DragMove();
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

    private void SaveAccount(string name, string pass) {
      Client.Settings = Client.LoadSettings<UserSettings>(name);
      var rawPass = ProtectedData.Protect(Encoding.UTF8.GetBytes(pass), null, DataProtectionScope.CurrentUser);
      Client.Settings.Password = Convert.ToBase64String(rawPass);
      Client.Settings.Username = name;

      settings.Accounts.Add(name);
      Client.SaveSettings(SettingsKey, settings);
    }

    private void Login(string user, string pass) {
      Progress.Visibility = Visibility.Visible;
      LoginBar.IsIndeterminate = true;
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled = AutoLoginToggle.IsEnabled = false;

      Client.Initialize(user, pass).ContinueWith(HandleLogin);
    }

    private void HandleLogin(Task<bool> task) {
      if (!task.IsFaulted && task.Result) {
        Client.ChatManager = new Logic.Chat.RiotChat(user, pass);
        Client.SaveSettings(SettingsKey, settings);
        Dispatcher.Invoke(Client.MainWindow.LoginComplete);
        return;
      } else if (task.IsFaulted) {
        var error = task.Exception.InnerException as InvocationException;
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
  }
}
