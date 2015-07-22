using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using MFroehlich.League.Assets;

namespace LeagueClient.ClientUI {
  /// <summary>
  /// Interaction logic for PatcherPage.xaml
  /// </summary>
  public partial class PatcherPage : Page {

    public PatcherPage() {
      InitializeComponent();

      LeagueData.InitalizeProgressed += (s, d, p) =>
        Dispatcher.BeginInvoke(new LeagueData.ProgressHandler(LeagueData_Progress), new object[] { s, d, p });
      LeagueData.Update();
    }

    void LeagueData_Progress(string status, string detail, double progress) {
      OverallStatusText.Text = "Patching Custom Client";
      switch (status) {
        case "Downloading": OverallStatusBar.Value = .0; break;
        case "Extracting": OverallStatusBar.Value = .25; break;
        case "Installing": OverallStatusBar.Value = .50; break;
        case "done":
          OverallStatusBar.Value = .75;
          CurrentStatusText.Text = "Creating Login Animation...";
          CurrentStatusProgress.Text = "";
          CurrentStatusBar.IsIndeterminate = true;
          new System.Threading.Thread(CreateLoginPage).Start();
          return;
      }
      CurrentStatusText.Text = string.Format("{0} {1}...", status, detail);
      if (progress < 0) {
        CurrentStatusProgress.Text = "";
        CurrentStatusBar.IsIndeterminate = true;
      } else {
        CurrentStatusProgress.Text = (progress * 100).ToString("f1") + "%";
        CurrentStatusBar.IsIndeterminate = false;
      }
      CurrentStatusBar.Value = progress;
    }

    void CreateLoginPage() {
      if (!File.Exists(Client.LoginVideoPath)) {
        var info = new ProcessStartInfo {
          FileName = Client.FFMpegPath,
          Arguments = "-i \"" + Path.Combine(Client.AirDirectory, @"mod\lgn\themes",
          Client.LoginTheme, @"flv\login-loop.flv") + "\" \"" + Client.LoginVideoPath + "\"",
          UseShellExecute = false,
          CreateNoWindow = true
        };
        Process.Start(info).WaitForExit();
      }
      Dispatcher.Invoke(Client.MainWindow.PatchComplete);
    }
  }
}
