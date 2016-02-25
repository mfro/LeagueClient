using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;
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

namespace LeagueClient.UI.Selectors {
  /// <summary>
  /// Interaction logic for MasteryIcon.xaml
  /// </summary>
  public partial class MasteryIcon : UserControl {
    public MasteryDto Data { get; private set; }
    public int Points {
      get { return points; }
      set {
        var old = points;
        points = value;
        PointsLabel.Content = points + "/" + Data.ranks;
        if (points == Data.ranks) Border.BorderBrush = App.FocusBrush;
        else Border.BorderBrush = App.ForeBrush;
        if (old != value)
          Row.PointsChanged(this, value);
      }
    }
    public bool Enabled {
      get { return enabled; }
      set {
        enabled = value;
        Icon.Source = DataDragon.GetMasteryImage(Data, !enabled).Load();
        if (!enabled && points > 0) Points = 0;
      }
    }
    public MasteryEditor.MasteryRow Row { get; }

    private int points;
    private bool enabled;

    public MasteryIcon(MasteryDto dto, MasteryEditor.MasteryRow row) {
      InitializeComponent();
      Row = row;
      Data = dto;
      if (Data.ranks == 1) PointsLabel.Visibility = Visibility.Collapsed;
    }

    void Control_MouseWheel(object sender, MouseWheelEventArgs e) {
      int d = (e.Delta > 0) ? 1 : -1;
      //If not enabled or removing any points in higher rows
      if (!enabled || (d < 0 && Row.Tree.Rows.Any(row => row.Row > this.Row.Row && row.Points > 0)))
        return;
      if (points + d <= Data.ranks && points + d >= 0) {
        Points += d;
      }
    }

    public MasteryIcon() {
      InitializeComponent();
    }
  }
}
