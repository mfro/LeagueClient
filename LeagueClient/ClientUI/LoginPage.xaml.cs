using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Xml;
using LeagueClient.ClientUI.Controls;
using LeagueClient.Logic;
using MFroehlich.Parsing.DynamicJSON;
using MFroehlich.Parsing.MFro;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for LoginPage.xaml
  /// </summary>
  public partial class LoginPage : Page {
    private const string SettingsKey = "LoginSettings";
    private static JSONObject settings = Client.LoadSettings(SettingsKey);

    public Uri BackAnimationURI { get; private set; }

    private int tries;
    private string user;
    private string pass;

    public LoginPage() {
      BackAnimationURI = new Uri(Client.LoginVideoPath);
      InitializeComponent();

      if (!settings.Dictionary.ContainsKey("Accounts"))
        settings["Accounts"] = new JSONArray();
      var accounts = (List<string>) settings["Accounts"];
      if (accounts.Count > 0) {
        foreach (var name in accounts) {
          var user = Client.LoadSettings(name).To<Settings>();
          var login = new LoginAccount(user.Username, user.SummonerName, user.ProfileIcon);
          login.Click += Account_Click;
          login.Remove += Account_Remove;
          AccountList.Children.Insert(0, login);
        }
      } else LoginGrid.BeginStoryboard(App.FadeIn);

      BackStatic.Source = new BitmapImage(new Uri(Path.Combine(Client.AirDirectory, "mod\\lgn\\themes", Client.LoginTheme, "cs_bg_champions.png")));
    }

    private void Account_Remove(object sender, EventArgs e) {
      AccountList.Children.Remove(sender as UIElement);
      settings["Accounts"].Remove(((LoginAccount) sender).Username);
      Client.SaveSettings(SettingsKey, settings);
    }

    private void Account_Click(object sender, EventArgs e) {
      AutoLoginToggle.IsChecked = true;
      var login = (sender as LoginAccount).Username;
      foreach (var obj in AccountList.Children)
        if (obj is LoginAccount) ((LoginAccount) obj).State = LoginAccountState.Readonly;
      (sender as LoginAccount).State = LoginAccountState.Loading;
      Client.Settings = Client.LoadSettings(login).To<Settings>();
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

    private void SaveAccount(string name, string pass) {
      Client.Settings = Client.LoadSettings(name).To<Settings>();
      var rawPass = ProtectedData.Protect(Encoding.UTF8.GetBytes(pass), null, DataProtectionScope.CurrentUser);
      Client.Settings.Password = Convert.ToBase64String(rawPass);
      Client.Settings.Username = name;

      settings["Accounts"].Add(name);
      Client.SaveSettings(SettingsKey, settings);
    }

    private void Login(string user, string pass) {
      Progress.Visibility = Visibility.Visible;
      LoginBar.IsIndeterminate = true;
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled = AutoLoginToggle.IsEnabled = false;

      new Thread(async () => {
        try {
          var success = await Client.Initialize(user, pass);
          if (success) {
            Client.ChatManager = new Logic.Chat.RiotChat(user, pass);
            Client.SaveSettings(SettingsKey, settings);
            Dispatcher.Invoke(Client.MainWindow.LoginComplete);
          } else Dispatcher.Invoke(Reset);
        } catch { }
      }).Start();
    }

    private void Reset() {
      if (tries > 0) {
        tries--;
        Login(user, pass);
        return;
      }
      Progress.Visibility = Visibility.Hidden;
      LoginBar.IsIndeterminate = false;
      PassBox.Password = "";
      LoginButt.IsEnabled = UserBox.IsEnabled = PassBox.IsEnabled = AutoLoginToggle.IsEnabled = true;
      PassBox.Focus();
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
  }
}
