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

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for LoginPage.xaml
  /// </summary>
  public partial class LoginPage : Page {
    private const string SettingsKey = "LoginSettings";
    private static LoginSettings settings = Client.LoadSettings<LoginSettings>(SettingsKey);

    private int tries;
    private string user;
    private string pass;

    public LoginPage(Task preInit = null) {
      InitializeComponent();

      LoadBackground();

      if (preInit == null)
        PatchComplete();
      else
        preInit.ContinueWith(PreInitFinished);
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

    private void PreInitFinished(Task task) {
      Client.Log(LeagueData.CurrentVersion);
      Client.Log($"Air: {Client.Installed.AirVersion} / {Client.Latest.AirVersion}");
      Client.Log($"Game: {Client.Installed.GameVersion} / {Client.Latest.GameVersion}");
      Client.Log($"Solution: {Client.Installed.SolutionVersion} / {Client.Latest.SolutionVersion}");

      ((App) Application.Current).LoadResources();
      if (NeedsPatch()) {
        Dispatcher.MyInvoke(PatchingGrid.BeginStoryboard, App.FadeIn);
        LeagueData.InitalizeProgressed += (s, d, p) => Dispatcher.MyInvoke(LeagueData_Progress, s, d, p);

        if (!LeagueData.IsCurrent) LeagueData.Update();
        else Dispatcher.Invoke(LoginPageUI);
      } else {
        Dispatcher.Invoke(PatchComplete);
      }
    }

    private void PatchComplete() {
      PatchingGrid.BeginStoryboard(App.FadeOut);

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

    private void MediaElement_MediaEnded(object sender, RoutedEventArgs e) {
      BackAnim.Position = TimeSpan.FromSeconds(0);
      BackAnim.Play();
    }
    #endregion

    #region Patching

    private void LeagueData_Progress(string status, string detail, double progress) {
      OverallStatusText.Content = "Patching Custom Client";
      switch (status) {
        case "Downloading": OverallStatusBar.Value = .0; break;
        case "Extracting": OverallStatusBar.Value = .25; break;
        case "Installing": OverallStatusBar.Value = .50; break;
        case "done": LoginPageUI(); return;
      }
      CurrentStatusText.Content = $"{status} {detail}...";
      CurrentStatusBar.Value = progress;
      if (progress < 0) {
        CurrentStatusProgress.Content = "";
        CurrentStatusBar.IsIndeterminate = true;
      } else {
        CurrentStatusProgress.Content = (progress * 100).ToString("f1") + "%";
        CurrentStatusBar.IsIndeterminate = false;
      }
    }

    private void LoginPageUI() {
      OverallStatusBar.Value = .75;
      CurrentStatusText.Content = "Creating Login Animation...";
      CurrentStatusProgress.Content = "";
      CurrentStatusBar.IsIndeterminate = true;
      new Thread(CreateLoginPage).Start();
    }

    private void CreateLoginPage() {
      if (!Client.LoginTheme.Equals(settings.Theme) || !File.Exists(Client.LoginVideoPath) || !File.Exists(Client.LoginStaticPath)) {
        var png = Client.Latest.AirFiles.FirstOrDefault(f => f.Url.AbsolutePath.EndsWith($"/files/mod/lgn/themes/{Client.LoginTheme}/cs_bg_champions.png"));
        using (var web = new WebClient())
          web.DownloadFile(png.Url, Client.LoginStaticPath);

        var file = Path.GetTempFileName();
        using (var web = new WebClient()) {
          var flv = Client.Latest.AirFiles.FirstOrDefault(f => f.Url.AbsolutePath.EndsWith($"/files/mod/lgn/themes/{Client.LoginTheme}/flv/login-loop.flv"));
          web.DownloadFile(flv.Url, file);
        }
        var info = new ProcessStartInfo {
          FileName = Client.FFMpegPath,
          Arguments = $"-i \"{file}\" \"{Client.LoginVideoPath}\"",
          UseShellExecute = false,
          CreateNoWindow = true,
          RedirectStandardError = true,
          RedirectStandardOutput = true,
        };
        File.Delete(Client.LoginVideoPath);
        Process.Start(info).WaitForExit();
        File.Delete(file);

        settings.Theme = Client.LoginTheme;
        Client.SaveSettings(SettingsKey, settings);
        Dispatcher.Invoke(LoadBackground);
      }

      Dispatcher.Invoke(PatchComplete);
    }

    public static bool NeedsPatch() {
      return !File.Exists(Client.LoginVideoPath) || !File.Exists(Client.LoginStaticPath) || !LeagueData.IsCurrent || !Client.LoginTheme.Equals(settings.Theme);
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
