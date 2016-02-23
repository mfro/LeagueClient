using LeagueClient.Logic;
using MFroehlich.League.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for PatchProgressBar.xaml
  /// </summary>
  public partial class PatchProgressBar : UserControl, IDisposable {
    public PatchProgressBar() {
      InitializeComponent();

      Resize();
      SizeChanged += (s, e) => Resize();

      Draw();
      LeagueData.UpdateProgress += LeagueData_UpdateProgress;
    }

    private void Resize() {
      var width = ActualWidth / 15;
      BackgroundBorder.Padding = new Thickness(2 * width);
      BackgroundPath.StrokeThickness = Path.StrokeThickness = width;
    }

    private void Draw() {
      var args = LeagueData.CurrentStatus ?? new LeagueData.ProgressEventArgs(LeagueData.UpdateStatus.Downloading, .66);
      var angle = (Math.PI / 2) - (args.Progress * 2 * Math.PI);
      if (args.Progress < 0) {
        arc.Point = new Point(-.0001, -1);
        arc.IsLargeArc = true; ;
      } else {
        arc.Point = new Point(Math.Cos(angle), -Math.Sin(angle));
        arc.IsLargeArc = args.Progress >= .5;
      }

      switch (args.Status) {
        case LeagueData.UpdateStatus.Downloading:
          StatusLabel.Content = "Downloading"; break;
        case LeagueData.UpdateStatus.Extracting:
          StatusLabel.Content = "Extracting"; break;
        case LeagueData.UpdateStatus.Installing:
          StatusLabel.Content = "Installing"; break;
      }

      if (args.Progress < 0) ProgressLabel.Content = "";
      else if (args.Progress < .01) ProgressLabel.Content = "0%";
      else ProgressLabel.Content = (args.Progress * 100).ToString("#") + "%";
    }
    private void LeagueData_UpdateProgress(object sender, LeagueData.ProgressEventArgs e) => Dispatcher.Invoke(Draw);

    public void Dispose() {
      LeagueData.UpdateProgress -= LeagueData_UpdateProgress;
    }
  }
}
