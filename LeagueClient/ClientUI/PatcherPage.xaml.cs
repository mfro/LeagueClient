using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using LeagueClient.Logic;
using MFroehlich.League.Assets;
using MFroehlich.Parsing.DynamicJSON;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for PatcherPage.xaml
  /// </summary>
  public partial class PatcherPage : Page {
    private const string SettingsKey = "PatcherSettings";
    private static JSONObject settings = Client.LoadSettings(SettingsKey);

    public PatcherPage() {
      InitializeComponent();

      LeagueData.InitalizeProgressed += (s, d, p) =>
        Dispatcher.BeginInvoke(new LeagueData.ProgressHandler(LeagueData_Progress), new object[] { s, d, p });
      if (!LeagueData.IsCurrent) LeagueData.Update();
      else LoginPageUI();
    }

    void LeagueData_Progress(string status, string detail, double progress) {
      OverallStatusText.Text = "Patching Custom Client";
      switch (status) {
        case "Downloading": OverallStatusBar.Value = .0; break;
        case "Extracting": OverallStatusBar.Value = .25; break;
        case "Installing": OverallStatusBar.Value = .50; break;
        case "done": LoginPageUI(); return;
      }
      CurrentStatusText.Text = $"{status} {detail}...";
      CurrentStatusBar.Value = progress;
      if (progress < 0) {
        CurrentStatusProgress.Text = "";
        CurrentStatusBar.IsIndeterminate = true;
      } else {
        CurrentStatusProgress.Text = (progress * 100).ToString("f1") + "%";
        CurrentStatusBar.IsIndeterminate = false;
      }
    }

    void LoginPageUI() {
      OverallStatusBar.Value = .75;
      CurrentStatusText.Text = "Creating Login Animation...";
      CurrentStatusProgress.Text = "";
      CurrentStatusBar.IsIndeterminate = true;
      new System.Threading.Thread(CreateLoginPage).Start();
    }

    void CreateLoginPage() {
      if (!settings.Dictionary.ContainsKey("theme") || !Client.LoginTheme.Equals(settings["theme"]) || !File.Exists(Client.LoginVideoPath)) {
        var info = new ProcessStartInfo {
          FileName = Client.FFMpegPath,
          Arguments = "-i \"" + Path.Combine(Client.AirDirectory, @"mod\lgn\themes",
          Client.LoginTheme, @"flv\login-loop.flv") + "\" \"" + Client.LoginVideoPath + "\"",
          UseShellExecute = false,
          CreateNoWindow = true
        };
        File.Delete(Client.LoginVideoPath);
        Process.Start(info).WaitForExit();

        settings["theme"] = Client.LoginTheme;
        Client.SaveSettings(SettingsKey, settings);
      }

      Dispatcher.Invoke(Client.MainWindow.PatchComplete);
    }

    public static bool NeedsPatch() {
      return !File.Exists(Client.LoginVideoPath) || !LeagueData.IsCurrent || !settings.Dictionary.ContainsKey("theme") || !Client.LoginTheme.Equals(settings["theme"]);
    }
  }
}
