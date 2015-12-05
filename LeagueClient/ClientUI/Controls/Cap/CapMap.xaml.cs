using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using LeagueClient.Logic;
using LeagueClient.Logic.Cap;
using MFroehlich.League.Assets;
using MFroehlich.League.DataDragon;

namespace LeagueClient.ClientUI.Controls {
  /// <summary>
  /// Interaction logic for TeambuilderMap.xaml
  /// </summary>
  public partial class CapMap : UserControl {
    private static List<PointRef> TopLane = new List<PointRef> {
      new PointRef { { .083, .083 } },
      new PointRef { { .040, .133 }, { .133, .050 } },
      new PointRef { { .023, .200 }, { .083, .083 }, { .200, .023 } },
      new PointRef { { .016, .016 }, { .150, .016 }, { .150, .150 }, { .016, .150 } },
      new PointRef { { .016, .016 }, { .196, .016 }, { .196, .196 }, { .016, .196 }, { .106, .106 } },
    };
    private static List<PointRef> MidLane = new List<PointRef> {
      new PointRef { { .442, .442 } },
      new PointRef { { .394, .490 }, { .490, .394 } },
      new PointRef { { .385, .480 }, { .450, .370 }, { .515, .480 } },
      new PointRef { { .385, .375 }, { .515, .375 }, { .515, .505 }, { .385, 505 } },
      new PointRef { { .360, .360 }, { .538, .360 }, { .538, .538 }, { .360, .538 }, { .448, .448 } },
    };
    private static List<PointRef> BotLane = new List<PointRef> {
      new PointRef { { .800, .790 } },
      new PointRef { { .748, .830 }, { .848, .750 } },
      new PointRef { { .809, .715 }, { .748, .820 }, { .870, .820 } },
      new PointRef { { .748, .598 }, { .870, .598 }, { .870, .820 }, { .748, .820 } },
      new PointRef { { .698, .648 }, { .870, .648 }, { .870, .820 }, { .698, .820 }, { .784, .734 } },
    };
    private static List<PointRef> Jungle = new List<PointRef> {
      new PointRef { { .250, .300 } },
      new PointRef { { .250, .300 }, { .650, .600 } },
      new PointRef { { .200, .260 }, { .650, .600 }, { .300, .340 } },
      new PointRef { { .200, .984 }, { .600, .560 }, { .300, .340 }, { .700, .640 } },
      new PointRef { { .190, .340 }, { .600, .560 }, { .320, .340 }, { .700, .640 }, { .255, .230 } },
    };
    private List<CapPlayer> players = new List<CapPlayer>();

    public CapMap() {
      InitializeComponent();

      GameMap.Source = LeagueData.GetMapImage(11); // Summoner's Rift
    }

    public void UpdateList(IEnumerable<CapPlayer> players) {
      foreach (var player in this.players) player.PropertyChanged -= Cap_PlayerUpdate;
      this.players = new List<CapPlayer>(players.Where(p => p != null));
      foreach (var player in this.players) player.PropertyChanged += Cap_PlayerUpdate;
      Reset();
    }

    private void Cap_PlayerUpdate(object sender, EventArgs e) {
      Dispatcher.Invoke(Reset);
    }

    private static Dictionary<Position, List<PointRef>> onMap = new Dictionary<Position, List<PointRef>> {
      [Position.TOP] = TopLane,
      [Position.MIDDLE] = MidLane,
      [Position.BOTTOM] = BotLane,
      [Position.JUNGLE] = Jungle,
    };
    private void Reset() {
      var positions = new Dictionary<Position, int>();
      var counts = new Dictionary<Position, int>();
      foreach (var pos in onMap.Keys) {
        positions[pos] = (from p in players
                          where p.Position == pos && p.Champion != null
                          select p).Count();
        counts[pos] = 0;
      }

      Body.Children.Clear();
      foreach (var item in players.Where(p => p.Position != null && p.Position != Position.UNSELECTED && p.Champion != null)) {
        var pos = item.Position;
        Point point = onMap[pos][positions[pos] - 1][counts[pos]++];

        var img = new Image {
          Width = ActualWidth * .12,
          Height = ActualHeight * .12,
          Style = (Style) FindResource("Image"),
          Margin = new Thickness(ActualWidth * point.X, ActualHeight * point.Y, 0, 0)
        };
        img.Clip = new EllipseGeometry {
          RadiusX = img.Width / 2,
          RadiusY = img.Height / 2,
          Center = new Point(img.Width / 2, img.Height / 2),
        };
        img.Source = LeagueData.GetChampIconImage(item.Champion);
        Body.Children.Add(img);
      }
    }

    private class PointRef : List<Point> {
      public void Add(double x, double y) {
        Add(new Point(x, y));
      }
    }
  }
}
